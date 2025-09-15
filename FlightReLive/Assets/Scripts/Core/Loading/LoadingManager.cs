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
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Loading
{
    public class LoadingManager : MonoBehaviour
    {
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
            //Convert FlightFile into FlightData
            FlightData flightData = ConvertFileToFlight(flightFile);
            CurrentFlightFile = flightFile;
            CurrentFlightData = flightData;
            IsLoading = true;
            OnFlightStartLoading?.Invoke();

            //Check MapTiler API Key
            string apiKey = SettingsManager.CurrentSettings.MapTilerAPIKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                NotifyError("Missing MapTiler API key.");
                return;
            }

            bool isValid = await MapTilerAPIHelper.IsMapTilerKeyValidAsync(apiKey);
            if (!isValid)
            {
                NotifyError("Invalid MapTiler API key.");
                return;
            }

            //Load only priority 0 tiles
            List<TileDefinition> priorityZeroTiles = flightData.MapDefinition.TileDefinitions.Where(t => t.Priority == 0).ToList();

            foreach (var tile in priorityZeroTiles)
            {
                await LoadTile(tile.X, tile.Y);
            }

            //Load "one-shot" module
            flightData.BuildTileLookup();
            FlightChartsManager.Instance.Load(flightData);
            VideoPlayerManager.Instance.LoadFlightVideo(flightData);
            SunManager.Instance.LoadFlightRendering(flightData);

            IsLoading = false;
            OnFlightEndLoading?.Invoke();
            flightData.InitializeAltitude();
            PathManager.Instance.LoadFlightPath(flightData);
            Fugui.Notify("Flight loaded", $"{flightData.Name} successfully loaded.", StateType.Info);

            // --- TEST: Now chain-load priority 1 tiles ---
            List<TileDefinition> priorityOneTiles = flightData.MapDefinition.TileDefinitions
                .Where(t => t.Priority == 1)
                .ToList();

            foreach (TileDefinition tile in priorityOneTiles)
            {
                await LoadTile(tile.X, tile.Y);
            }

            Fugui.Notify("Streaming update", $"Priority 1 tiles loaded for {flightData.Name}.", StateType.Info);

            List<TileDefinition> priority2Tiles = flightData.MapDefinition.TileDefinitions
                .Where(t => t.Priority == 2)
                .ToList();

            foreach (TileDefinition tile in priority2Tiles)
            {
                await LoadTile(tile.X, tile.Y);
            }

            Fugui.Notify("Streaming update", $"Priority 1 tiles loaded for {flightData.Name}.", StateType.Info);
        }

        private void NotifyError(string message)
        {
            Fugui.Notify("Resource loading error", message, StateType.Danger);
            IsLoading = false;
        }
        #endregion

        #region METHODS
        public async Task LoadTile(int x, int y)
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

            tile = await MapTilerAPIHelper.DownloadTileAsync(tile, satZoom, topoZoom, buildingZoom);

            if (tile != null)
            {
                CurrentFlightData.AddTile(tile);

                //Distribute tile to sub-modules
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

            if (tile == null) return;

            // Décharger des modules
            TerrainManager.Instance.UnloadTile(tile);
            BuildingManager.Instance.UnloadTile(tile);
            WorldUIManager.Instance.UnloadTile(tile);

            //Clean resources
            tile.SatelliteTexture = null;
            tile.HeightMap = null;
            tile.Buildings = null;
            tile.GeoData = null;
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

            // Set GPS origin
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

            //Configurable padding
            int padding = 2;

            // Collect all GPS points (including estimated takeoff)
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

            // Get inside bounds
            (int baseMinTileX, int baseMaxTileY) = MapTools.GPSToTileXY(minLat, minLon);
            (int baseMaxTileX, int baseMinTileY) = MapTools.GPSToTileXY(maxLat, maxLon);

            // Inside bounds (priority 0)
            int originalMinTileX = baseMinTileX;
            int originalMaxTileX = baseMaxTileX;
            int originalMinTileY = baseMinTileY;
            int originalMaxTileY = baseMaxTileY;

            // Outside bounds (with padding)
            int minTileX = originalMinTileX - padding;
            int maxTileX = originalMaxTileX + padding;
            int minTileY = originalMinTileY - padding;
            int maxTileY = originalMaxTileY + padding;

            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    // Compute padding level for this tile
                    int dx = Math.Max(0, Math.Max(originalMinTileX - x, x - originalMaxTileX));
                    int dy = Math.Max(0, Math.Max(originalMinTileY - y, y - originalMaxTileY));
                    int priority = Math.Max(dx, dy); // distance in "rings"

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
    }
}
