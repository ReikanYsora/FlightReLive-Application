using FlightReLive.Core.Building;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Rendering;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using FlightReLive.Core.Workspace;
using FlightReLive.Core.WorldUI;
using FlightReLive.UI.FlightCharts;
using FlightReLive.UI.VideoPlayer;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace FlightReLive.Core.Loading
{
    public class LoadingManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private CancellationTokenSource _cancellationTokenSource;
        private int _totalTilesToLoad;
        private int _currentTilesLoaded;
        private string _currentLoadingText;
        private float _displayedProgress;
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

            _displayedProgress = 0;
            _currentTilesLoaded = 0;
            _totalTilesToLoad = 0;
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
        /// <summary>
        /// Start loading the flight scene asynchronously.
        /// Supports cancellation via CancelLoading().
        /// </summary>
        private async void StartLoadingScene(FlightFile flightFile)
        {
            CancelLoading();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            FlightData flightData = ConvertFileToFlight(flightFile);
            CurrentFlightFile = flightFile;
            CurrentFlightData = flightData;

            _totalTilesToLoad = flightData.MapDefinition.TileDefinitions.Count;
            _displayedProgress = 0;
            _currentTilesLoaded = 0;
            _currentLoadingText = "Preparing resources...";

            DisplayLoading();
            IsLoading = true;
            OnFlightStartLoading?.Invoke();

            try
            {
                //Check MapTiler API Key
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

                // Charger les tuiles par priorité croissante
                List<int> priorities = flightData.MapDefinition.TileDefinitions
                    .Select(t => t.Priority)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                foreach (int priority in priorities)
                {
                    List<TileDefinition> tiles = flightData.MapDefinition.TileDefinitions.Where(t => t.Priority == priority).ToList();

                    if (priority == 0)
                    {
                        //For the priority 0 tiles, we need to dowload all resource before call submodules (we need to initialize SceneCenterGPS, Altitude et TileLookup to ensure altitude processing is correct
                        List<TileDefinition> loadedTiles = new List<TileDefinition>();

                        foreach (TileDefinition tile in tiles)
                        {
                            token.ThrowIfCancellationRequested();

                            _currentLoadingText = $"Creating {tile.X}-{tile.Y} tile...";
                            _currentTilesLoaded++;
                            TileDefinition loaded = await MapTilerAPIHelper.DownloadTileAsync(tile, SettingsManager.GetSatelliteTileZoom(), MapTools.ZOOM_LEVEL_TOPOGRAPHIC, MapTools.ZOOM_LEVEL_BUILDING, token);

                            if (loaded != null)
                            {
                                flightData.AddTile(loaded);
                                loadedTiles.Add(loaded);
                            }
                        }

                        flightData.BuildTileLookup();
                        flightData.InitializeAltitude();

                        foreach (TileDefinition tile in loadedTiles)
                        {
                            TerrainManager.Instance.LoadTile(tile, flightData);
                            BuildingManager.Instance.LoadTile(tile, flightData);
                            WorldUIManager.Instance.LoadTile(tile, flightData);
                        }
                    }
                    else
                    {
                        foreach (TileDefinition tile in tiles)
                        {
                            token.ThrowIfCancellationRequested();

                            _currentLoadingText = $"Creating {tile.X}-{tile.Y} tile...";
                            _currentTilesLoaded++;
                            await LoadTile(tile.X, tile.Y, token);
                        }
                    }
                }

                //Load "one-shot" modules
                VideoPlayerManager.Instance.LoadFlightVideo(flightData);
                SunManager.Instance.LoadFlightRendering(flightData);
                FlightChartsManager.Instance.Load(flightData);
                PathManager.Instance.LoadFlightPath(flightData);

                Fugui.CloseModal();
                Fugui.Notify("Flight loaded", $"{flightData.Name} successfully loaded.", StateType.Info);

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

        private void NotifyError(string message)
        {
            Fugui.Notify("Resource loading error", message, StateType.Danger);
            IsLoading = false;
        }

        /// <summary>
        /// Load a specific tile asynchronously, with cancellation support.
        /// </summary>
        public async Task LoadTile(int x, int y, CancellationToken token = default)
        {
            if (CurrentFlightData == null)
            {
                return;
            }

            TileDefinition tile = CurrentFlightData.MapDefinition.TileDefinitions.FirstOrDefault(t => t.X == x && t.Y == y);

            if (tile == null)
            {
                Debug.LogWarning($"Tile {x},{y} not found in FlightData.");
                return;
            }

            int satZoom = SettingsManager.GetSatelliteTileZoom();
            int topoZoom = MapTools.ZOOM_LEVEL_TOPOGRAPHIC;
            int buildingZoom = MapTools.ZOOM_LEVEL_BUILDING;

            token.ThrowIfCancellationRequested();
            tile = await MapTilerAPIHelper.DownloadTileAsync(tile, satZoom, topoZoom, buildingZoom, token);
            token.ThrowIfCancellationRequested();

            if (tile != null)
            {
                CurrentFlightData.AddTile(tile);

                TerrainManager.Instance.LoadTile(tile, CurrentFlightData);
                BuildingManager.Instance.LoadTile(tile, CurrentFlightData);
                WorldUIManager.Instance.LoadTile(tile, CurrentFlightData);
            }
        }


        public void UnloadTile(int x, int y)
        {
            if (CurrentFlightData == null)
            {
                return;
            }

            TileDefinition tile = CurrentFlightData.MapDefinition.TileDefinitions.FirstOrDefault(t => t.X == x && t.Y == y);

            if (tile == null)
            {
                return;
            }

            TerrainManager.Instance.UnloadTile(tile);
            BuildingManager.Instance.UnloadTile(tile);
            WorldUIManager.Instance.UnloadTile(tile);

            tile.SatelliteTexture = null;
            tile.HeightMap = null;
            tile.Buildings = null;
            tile.MeshData = null;
        }

        private void UnloadFlightDataInModules()
        {
            FlightChartsManager.Instance.Unload();
            VideoPlayerManager.Instance.Unload();
            TerrainManager.Instance.Unload();
            PathManager.Instance.Unload();
            SunManager.Instance.Unload();
            WorldUIManager.Instance.Unload();
            BuildingManager.Instance.Unload();

            CurrentFlightData?.Dispose();
            CurrentFlightData = null;

            OnFlightUnloaded?.Invoke();
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

            int padding = 2;

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
                    int dx = Math.Max(0, Math.Max(originalMinTileX - x, x - originalMaxTileX));
                    int dy = Math.Max(0, Math.Max(originalMinTileY - y, y - originalMaxTileY));
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

        /// <summary>
        /// Cancel the current loading process if one is running.
        /// </summary>
        private void CancelLoading()
        {
            _displayedProgress = 0;
            _totalTilesToLoad = 0;
            _currentTilesLoaded = 0;

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
                float targetProgress = (float)_currentTilesLoaded / (float)_totalTilesToLoad;
                _displayedProgress = Mathf.MoveTowards(_displayedProgress, targetProgress, Time.deltaTime * 0.25f);
                float availableX = (layout.GetAvailableWidth() / scale) - (paddingX * scale * 2);
                Vector2 progressBarSize = new Vector2(availableX, 20f * scale);
                layout.CenterNextItemHV(_currentLoadingText);
                layout.Text(_currentLoadingText);
                layout.CenterNextItemH(availableX);
                layout.ProgressBar("Global", _displayedProgress, new FuElementSize(progressBarSize), ProgressBarTextPosition.Inside);
            },
            FuModalSize.Medium,
            new FuModalButton("Cancel loading", () => CancelLoading(), FuButtonStyle.Danger, FuKeysCode.Escape));
        }
        #endregion
    }
}
