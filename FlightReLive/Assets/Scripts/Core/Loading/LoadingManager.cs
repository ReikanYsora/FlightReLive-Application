using FlightReLive.Core.OpenVectorTile;
using FlightReLive.Core.Environment;
using FlightReLive.Core.Database;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.POI;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using FlightReLive.Core.TimeBar;
using FlightReLive.Core.Library;
using FlightReLive.UI.FlightCharts;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ImGuiNET;
using FlightReLive.UI.Helpers;

namespace FlightReLive.Core.Loading
{
    public class LoadingManager : MonoBehaviour
    {
        #region CONSTANTS
        private const int TILE_PADDING = 5;
        #endregion

        #region ATTRIBUTES
        private CancellationTokenSource _cancellationTokenSource;
        private string _currentTile;
        private int _currentTilePriority;
        private float _tileProgress;
        private int _tilesProcessed;
        private int _tilesTotal;
        private int _filesFromCache;
        private int _filesDownloaded;
        private Texture2D _thumbnail;
        private string _fileName;
        #endregion

        #region PROPERTIES
        internal static LoadingManager Instance { get; private set; }

        internal bool IsLoading { get; private set; }

        internal bool IsLoaded { get; private set; }

        internal RealmFlightItem CurrentFlightFile { get; private set; }

        internal FlightData CurrentFlightData { get; private set; }
        #endregion

        #region EVENTS
        internal event Action OnFlightStartLoading;
        internal event Action OnFlightEndLoading;
        internal event Action OnFlightUnloaded;
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _tileProgress = 0f;
            _tilesProcessed = 0;
            _tilesTotal = 0;

            IsLoaded = false;
            IsLoading = false;
        }

        private void Start()
        {
            LibraryManager.Instance.OnFlightFileSelected += OnFlightFileSelected;
        }

        private void OnDestroy()
        {
            LibraryManager.Instance.OnFlightFileSelected -= OnFlightFileSelected;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Starts loading the scene for the given flight file.
        /// </summary>
        /// <param name="flightFile"></param>
        internal async void StartLoadingScene(RealmFlightItem flightFile)
        {
            await CancelLoading();

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            FlightData flightData = ConvertFileToFlight(flightFile);
            CurrentFlightFile = flightFile;
            CurrentFlightData = flightData;

            _tileProgress = 0f;
            _tilesProcessed = 0;
            _tilesTotal = flightData.MapDefinition.TileDefinitions.Count;
            _filesFromCache = 0;
            _filesDownloaded = 0;
            _currentTile = "";
            _currentTilePriority = -1;
            _fileName = flightFile.Name;
            _thumbnail = flightFile.Thumbnail;

            DisplayLoading();
            IsLoading = true;
            OnFlightStartLoading?.Invoke();
            await Task.Delay(1000);

            try
            {
                string apiKey = SettingsManager.CurrentSettings.MapTilerAPIKey;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    NotifyError("Missing MapTiler API key.");
                    return;
                }

                bool isValid = await MapTilerAPIHelper.IsMapTilerKeyValidAsync(apiKey, token);

                if (!isValid)
                {
                    NotifyError("Invalid MapTiler API key.");
                    return;
                }

                token.ThrowIfCancellationRequested();

                List<int> priorities = flightData.MapDefinition.TileDefinitions
                    .Select(t => t.Priority)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                foreach (int priority in priorities)
                {
                    List<TileDefinition> tiles = flightData.MapDefinition.TileDefinitions
                        .Where(t => t.Priority == priority)
                        .ToList();
                    List<TileDefinition> loadedTiles = new List<TileDefinition>();

                    foreach (TileDefinition tile in tiles)
                    {
                        token.ThrowIfCancellationRequested();

                        _tileProgress = 0f;
                        _currentTile = $"{tile.X}, {tile.Y}";
                        _currentTilePriority = tile.Priority;

                        TileDefinition loaded = await MapTilerAPIHelper.DownloadTileAsync(
                            tile,
                            token,
                            (phase, progress, source) =>
                            {
                                _tileProgress = (phase + progress) / 4f;

                                if (source == TileResourceSource.Cache)
                                {
                                    Interlocked.Increment(ref _filesFromCache);
                                }
                                else if (source == TileResourceSource.Download)
                                {
                                    Interlocked.Increment(ref _filesDownloaded);
                                }
                            });

                        if (loaded != null)
                        {
                            flightData.AddTile(loaded);
                            loadedTiles.Add(loaded);
                        }

                        _tilesProcessed++;
                    }

                    if (priority == 0)
                    {
                        flightData.BuildTileLookup();
                    }

                    //We need to call this method only when BuildTileLookup and InitializeAltitude has been executed
                    foreach (TileDefinition tileDefinition in loadedTiles)
                    {
                        BuildingManager.Instance.LoadTile(tileDefinition, flightData);
                    }
                }

                flightData.InitializeAltitude();
                POIManager.Instance.Load(flightData);
                ProceduralTerrainManager.Instance.Load(flightData);
                BuildingManager.Instance.Load(flightData);
                EnvironmentManager.Instance.Load(flightData);
                FlightChartsManager.Instance.Load(flightData);
                PathManager.Instance.Load(flightData);
                TimeBarManager.Instance.Load(flightData);
                Fugui.CloseModal();
                Fugui.Notify("Flight loaded", $"{flightData.Name} successfully loaded.", StateType.Info, 3f);
                OnFlightEndLoading?.Invoke();
                IsLoaded = true;
            }
            catch (OperationCanceledException)
            {
                Fugui.Notify("Loading cancelled", "The flight loading has been cancelled.", StateType.Warning, 3f);
                await UnloadFlightDataInModules();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Unloads the current flight data and associated resources.
        /// </summary>
        internal async void UnloadFlightData()
        {
            await UnloadFlightDataInModules();
        }

        internal static FlightData ConvertFileToFlight(RealmFlightItem file)
        {
            FlightData flightData = new FlightData
            {
                Name = file.Name,
                Width = file.Width,
                Height = file.Height,
                Frequency = file.Frequency,
                Date = file.CreationDate,
                Length = file.Duration,
                Points = file.DataPoints.ToList(),
                HasTakeOffPosition = file.HasTakeOffPosition,
            };

            if (file.HasTakeOffPosition)
            {
                flightData.GPSOrigin = new RealmDoubleVector2(file.FlightGPSCoordinates.X, file.FlightGPSCoordinates.Y);
            }
            else
            {
                RealmFlightPointItem firstPoint = file.DataPoints.First();
                flightData.GPSOrigin = new RealmDoubleVector2(firstPoint.Latitude, firstPoint.Longitude);
            }

            flightData.InitializeMapDefinition();
            flightData.EstimateTakeOffPosition = file.EstimateTakeOffPosition;


            IEnumerable<(double Latitude, double Longitude)> allPoints;
            if (file.HasTakeOffPosition)
            {
                allPoints = file.DataPoints
                    .Select(p => (p.Latitude, p.Longitude))
                    .Append((file.EstimateTakeOffPosition.X, file.EstimateTakeOffPosition.Y));
            }
            else
            {
                allPoints = file.DataPoints.Select(p => (p.Latitude, p.Longitude));
            }

            double minLat = allPoints.Min(p => p.Latitude);
            double maxLat = allPoints.Max(p => p.Latitude);
            double minLon = allPoints.Min(p => p.Longitude);
            double maxLon = allPoints.Max(p => p.Longitude);

            (int baseMinTileX, int baseMaxTileY) = MapTools.GPSToTileXY(minLat, minLon);
            (int baseMaxTileX, int baseMinTileY) = MapTools.GPSToTileXY(maxLat, maxLon);

            int originalMinTileX = baseMinTileX;
            int originalMaxTileX = baseMaxTileX;
            int originalMinTileY = baseMinTileY;
            int originalMaxTileY = baseMaxTileY;

            int minTileX = originalMinTileX - TILE_PADDING;
            int maxTileX = originalMaxTileX + TILE_PADDING;
            int minTileY = originalMinTileY - TILE_PADDING;
            int maxTileY = originalMaxTileY + TILE_PADDING;

            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    int dx = 0;
                    if (x < originalMinTileX)
                    {
                        dx = originalMinTileX - x;
                    }
                    else if (x > originalMaxTileX)
                    {
                        dx = x - originalMaxTileX;
                    }

                    int dy = 0;
                    if (y < originalMinTileY)
                    {
                        dy = originalMinTileY - y;
                    }
                    else if (y > originalMaxTileY)
                    {
                        dy = y - originalMaxTileY;
                    }

                    //Define tile priority based on distance from original tile
                    int priority = Math.Max(dx, dy);

                    if (priority > 3)
                    {
                        priority = 3;
                    }

                    TileDefinition tileDefinition = new TileDefinition
                    {
                        BoundingBox = MapTools.GetBoundingBoxFromTileXY(x, y),
                        ZoomLevel = MapTools.ZOOM_LEVEL_HEIGHTMAP,
                        X = x,
                        Y = y,
                        SatelliteTexture = null,
                        HeightMap = null,
                        Priority = priority
                    };

                    flightData.MapDefinition.AddTile(tileDefinition);
                }
            }

            return flightData;
        }

        /// <summary>
        /// Notifies the user of an error during loading and stops the loading process.
        /// </summary>
        /// <param name="message"></param>
        private void NotifyError(string message)
        {
            Fugui.Notify("Resource loading error", message, StateType.Danger, 3f);
            IsLoading = false;
        }

        /// <summary>
        /// Unloads flight data in all modules.
        /// </summary>
        /// <returns></returns>
        private async Task UnloadFlightDataInModules()
        {
            List<Task> unloadTasks = new List<Task>
            {
                TimeBarManager.Instance.Unload(),
                FlightChartsManager.Instance.Unload(),
                ProceduralTerrainManager.Instance.Unload(),
                PathManager.Instance.Unload(),
                EnvironmentManager.Instance.Unload(),
                BuildingManager.Instance.Unload(),
                POIManager.Instance.Unload()
            };

            try
            {
                await Task.WhenAll(unloadTasks);
            }
            catch (Exception ex)
            {
                Debug.LogError($"UnloadFlightDataInModules: Exception during unload {ex}");
            }

            CurrentFlightData?.Dispose();
            CurrentFlightData = null;
            IsLoaded = false;
            OnFlightUnloaded?.Invoke();
        }

        /// <summary>
        /// Cancels the current loading operation.
        /// </summary>
        /// <returns></returns>
        private async Task CancelLoading()
        {
            _tileProgress = 0f;
            _tilesProcessed = 0;
            _tilesTotal = 0;

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                await UnloadFlightDataInModules();
            }
        }
        #endregion

        #region CALLBACKS
        /// <summary>
        /// Callback when a flight file is selected in the workspace.
        /// </summary>
        /// <param name="flightFile"></param>
        private async void OnFlightFileSelected(RealmFlightItem flightFile)
        {
            if (CurrentFlightData != null)
            {
                await UnloadFlightDataInModules();
            }

            StartLoadingScene(flightFile);
        }
        #endregion

        #region UI
        /// <summary>
        /// Displays the loading modal with progress information.
        /// </summary>
        internal void DisplayLoading()
        {
            float scale = Fugui.CurrentContext.Scale;

            Fugui.ShowModal($"Loading resources for {_fileName}", (layout) =>
            {
                float paddingX = 10f;
                float combinedProgress = (_tilesTotal > 0) ? (_tilesProcessed + _tileProgress) / _tilesTotal : 0f;
                layout.Spacing();

                // Compute available width (logical, unscaled)
                float availableX = layout.GetAvailableWidth() / scale - (paddingX * 2f);
                float contentWidth = _thumbnail.width;
                float offsetX = Mathf.Floor((availableX - contentWidth) * 0.5f);
                Fugui.MoveX(offsetX);

                // Compute orange area (scaled drawing zone)
                float targetWidth = Mathf.Floor(_thumbnail.width * scale);
                float targetHeight = Mathf.Floor(_thumbnail.height * scale * 0.85f);
                float cornerRadius = 6f * scale;
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                uint frameColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));

                // Orange rounded background zone
                Vector2 zonePos = ImGui.GetCursorScreenPos();
                zonePos.x = Mathf.Floor(zonePos.x);
                zonePos.y = Mathf.Floor(zonePos.y);
                Vector2 zoneSize = new Vector2(targetWidth, targetHeight);
                drawList.AddRectFilled(zonePos, zonePos + zoneSize, frameColor, cornerRadius, ImDrawFlags.RoundCornersAll);

                // Compute thumbnail placement (centered inside zone with very thin uniform padding)
                float thumbPadding = 2f * scale;
                Vector2 thumbPos = zonePos + new Vector2(thumbPadding, thumbPadding);
                Vector2 thumbSize = new Vector2(zoneSize.x - thumbPadding * 2f, zoneSize.y - thumbPadding * 2f);

                // Clip to rounded corners before drawing image
                ImGui.PushClipRect(zonePos, zonePos + zoneSize, true);
                ImGui.SetCursorScreenPos(thumbPos);
                layout.Image("thumbnailLoading", _thumbnail, new FuElementSize(thumbSize.x / scale, thumbSize.y / scale), true, false);
                ImGui.PopClipRect();

                // Progress bar aligned below
                layout.Spacing();
                Vector2 progressBarSize = new Vector2(_thumbnail.width, 6f);
                Fugui.MoveX(offsetX);
                layout.ProgressBar("Progress", combinedProgress, new FuElementSize(progressBarSize), ProgressBarTextPosition.None);
                layout.Spacing();

                layout.Collapsable("Loading details##collapsable", () =>
                {
                    string priorityStr = "";

                    switch (_currentTilePriority)
                    {
                        case 0:
                            priorityStr = "Critical";
                            break;
                        case 1:
                            priorityStr = "High";
                            break;
                        case 2:
                            priorityStr = "Normal";
                            break;
                        case 3:
                            priorityStr = "Low";
                            break;
                        default:
                            priorityStr = "-";
                            break;
                    }

                    using (FuGrid loadingDetailsGrid = new FuGrid("loadingDetailsGrid", new FuGridDefinition(2, new float[] { 0.4f, 0.6f }), FuGridFlag.LinesBackground, 2, 2, paddingX))
                    {
                        loadingDetailsGrid.Text("Tiles processed");
                        loadingDetailsGrid.FramedText($"{_tilesProcessed} / {_tilesTotal}");

                        loadingDetailsGrid.Text("Current tile");
                        loadingDetailsGrid.FramedText($"{_currentTile}");

                        loadingDetailsGrid.Text("Current tile priority");
                        loadingDetailsGrid.FramedText($"{priorityStr}");

                        loadingDetailsGrid.Text("Files from cache");
                        loadingDetailsGrid.FramedText($"{_filesFromCache}");

                        loadingDetailsGrid.Text("Files downloaded");
                        loadingDetailsGrid.FramedText($"{_filesDownloaded}");
                    }
                }, FuButtonStyle.Collapsable, defaultOpen: true);

            },
            FuModalSize.Medium,
            new FuModalButton("Cancel loading", async () => await CancelLoading(), FuButtonStyle.Danger, FuKeysCode.Escape));
        }
        #endregion
    }
}
