using FlightReLive.Core.Building;
using FlightReLive.Core.Environment;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Workspace;
using FlightReLive.Core.POI;
using FlightReLive.UI.FlightCharts;
using FlightReLive.UI.VideoPlayer;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using ImGuiNET;

namespace FlightReLive.Core.Loading
{
    public class LoadingManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private CancellationTokenSource _cancellationTokenSource;
        private string _currentLoadingText;
        private float _tileProgress;
        private int _tilesProcessed;
        private int _tilesTotal;
        private int _filesFromCache;
        private int _filesDownloaded;
        #endregion

        #region PROPERTIES
        internal static LoadingManager Instance { get; private set; }

        internal bool IsLoading { get; private set; }

        internal FlightFile CurrentFlightFile { get; private set; }

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
        }

        private void Start()
        {
            WorkspaceManager.Instance.OnFlightFileSelected += OnFlightFileSelected;
        }

        private void OnDestroy()
        {
            WorkspaceManager.Instance.OnFlightFileSelected -= OnFlightFileSelected;
        }
        #endregion

        #region METHODS
        private async void StartLoadingScene(FlightFile flightFile)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            CancelLoading();
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
            _currentLoadingText = "Preparing resources...";

            DisplayLoading();
            IsLoading = true;
            OnFlightStartLoading?.Invoke();

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
                        _currentLoadingText = $"Create tile resources <{tile.X},{tile.Y}>";

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
                        flightData.InitializeAltitude();
                    }

                    foreach (TileDefinition t in loadedTiles)
                    {
                        if (t.Priority < 2)
                        {
                            BuildingManager.Instance.LoadTile(t, flightData);
                            POIManager.Instance.LoadTile(t, flightData);
                        }
                    }
                }

                sw.Restart();
                ProceduralTerrainManager.Instance.Load(flightData);
                sw.Stop();
                Debug.Log("ProceduralTerrainManager : " + sw.ElapsedMilliseconds);

                VideoPlayerManager.Instance.Load(flightData);
                EnvironmentManager.Instance.Load(flightData);
                FlightChartsManager.Instance.Load(flightData);
                PathManager.Instance.Load(flightData);

                Fugui.CloseModal();
                Fugui.Notify("Flight loaded",
                    $"{flightData.Name} successfully loaded. Cache: {_filesFromCache}, Downloaded: {_filesDownloaded}",
                    StateType.Info);

                OnFlightEndLoading?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Fugui.Notify("Loading cancelled", "The flight loading has been cancelled.", StateType.Warning);
                UnloadFlightDataInModules();
            }
            finally
            {
                IsLoading = false;
            }
        }


        internal static FlightData ConvertFileToFlight(FlightFile file)
        {
            FlightData flightData = new FlightData
            {
                Name = file.Name,
                Date = file.CreationDate,
                Length = file.Duration,
                Points = file.DataPoints,
                IsValid = file.IsValid,
                HasExtractionError = file.HasExtractionError,
                HasTakeOffPosition = file.HasTakeOffPosition,
                VideoPath = file.VideoPath
            };

            if (file.HasTakeOffPosition)
            {
                flightData.GPSOrigin = new FlightGPSData(file.FlightGPSCoordinates.x, file.FlightGPSCoordinates.y);
            }
            else
            {
                FlightDataPoint firstPoint = file.DataPoints.First();
                flightData.GPSOrigin = new FlightGPSData(firstPoint.Latitude, firstPoint.Longitude);
            }

            flightData.InitializeMapDefinition();
            flightData.EstimateTakeOffPosition = file.EstimateTakeOffPosition;

            int padding = 3;

            IEnumerable<(double Latitude, double Longitude)> allPoints;
            if (file.HasTakeOffPosition)
            {
                allPoints = file.DataPoints
                    .Select(p => (p.Latitude, p.Longitude))
                    .Append((file.EstimateTakeOffPosition.Latitude, file.EstimateTakeOffPosition.Longitude));
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

            int minTileX = originalMinTileX - padding;
            int maxTileX = originalMaxTileX + padding;
            int minTileY = originalMinTileY - padding;
            int maxTileY = originalMaxTileY + padding;

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

                    TileDefinition tileDefinition = new TileDefinition
                    {
                        BoundingBox = MapTools.GetBoundingBoxFromTileXY(x, y),
                        ZoomLevel = MapTools.ZOOM_LEVEL_TOPOGRAPHIC,
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

        private void NotifyError(string message)
        {
            Fugui.Notify("Resource loading error", message, StateType.Danger);
            IsLoading = false;
        }

        private void UnloadFlightDataInModules()
        {
            FlightChartsManager.Instance.Unload();
            VideoPlayerManager.Instance.Unload();
            ProceduralTerrainManager.Instance.Unload();
            PathManager.Instance.Unload();
            EnvironmentManager.Instance.Unload();
            POIManager.Instance.Unload();
            BuildingManager.Instance.Unload();
            CurrentFlightData?.Dispose();
            CurrentFlightData = null;
            OnFlightUnloaded?.Invoke();
        }

        private void CancelLoading()
        {
            _tileProgress = 0f;
            _tilesProcessed = 0;
            _tilesTotal = 0;

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                UnloadFlightDataInModules();
            }
        }
        #endregion

        #region CALLBACKS
        private void OnFlightFileSelected(FlightFile flightFile)
        {
            if (CurrentFlightData != null)
            {
                UnloadFlightDataInModules();
            }

            StartLoadingScene(flightFile);
        }
        #endregion

        #region UI
        internal void DisplayLoading()
        {
            float scale = Fugui.CurrentContext.Scale;

            Fugui.ShowModal("  ", (layout) =>
            {
                float paddingX = 10f;
                float combinedProgress = (_tilesTotal > 0) ? (_tilesProcessed + _tileProgress) / _tilesTotal : 0f;
                float availableX = (layout.GetAvailableWidth() / scale) - (paddingX * scale * 2);
                Vector2 progressBarSize = new Vector2(availableX, 20f * scale);
                layout.CenterNextItemH(_currentLoadingText);
                layout.Text(_currentLoadingText);
                layout.CenterNextItemH(availableX);
                layout.ProgressBar("Progress", combinedProgress, new FuElementSize(progressBarSize), ProgressBarTextPosition.Inside);

                layout.Separator();

                using (FuGrid loadingDetailsGrid = new FuGrid("loadingDetailsGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.LinesBackground, 2, 2, 2))
                {
                    loadingDetailsGrid.Text("Tiles processed");
                    loadingDetailsGrid.FramedText($"{_tilesProcessed} / {_tilesTotal}");

                    loadingDetailsGrid.Text("Files from cache");
                    loadingDetailsGrid.FramedText($"{_filesFromCache}");

                    loadingDetailsGrid.Text("Files downloaded");
                    loadingDetailsGrid.FramedText($"{_filesDownloaded}");
                }
            },
            FuModalSize.Medium,
            new FuModalButton("Cancel loading", () => CancelLoading(), FuButtonStyle.Danger, FuKeysCode.Escape));
        }
        #endregion
    }
}
