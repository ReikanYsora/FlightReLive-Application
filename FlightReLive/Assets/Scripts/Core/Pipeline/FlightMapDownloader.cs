using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Pipeline.Download;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using FlightReLive.Core.Workspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Pipeline
{
    public class FlightMapDownloader
    {
        #region ATTRIBUTES
        private List<string> _errors = new List<string>();
        private float _globalProgress = 0f;
        private int _totalTileDownloads = 0;
        private int _satelliteCompleted = 0;
        private int _topographicCompleted = 0;
        private int _hillshadeCompleted = 0;
        private int _geoDataCompleted = 0;
        private int _buildingCompleted = 0;
        private int _unknownCompleted = 0;
        #endregion

        #region EVENTS
        public event Action OnDownloadStarted;
        public event Action<float> OnGlobalProgressUpdated;
        public event Action<List<string>> OnDownloadCompleted;
        #endregion

        #region METHODS
        internal static FlightData ConvertFileToFlight(FlightFile file)
        {
            FlightData flightData = new FlightData
            {
                Name = file.Name,
                Date = file.Date,
                Length = file.Length,
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

            flightData.EstimateTakeOffPosition = file.EstimateTakeOffPosition;

            int padding = 1;

            IEnumerable<(double Latitude, double Longitude)> allPoints;

            if (file.HasTakeOffPosition)
            {
                allPoints = file.DataPoints.Select(p => (p.Latitude, p.Longitude)).Append((file.EstimateTakeOffPosition.Latitude, file.EstimateTakeOffPosition.Longitude));
            }
            else
            {
                allPoints = file.DataPoints.Select(p => (p.Latitude, p.Longitude));
            }

            double minLat = allPoints.Min(p => p.Latitude);
            double maxLat = allPoints.Max(p => p.Latitude);
            double minLon = allPoints.Min(p => p.Longitude);
            double maxLon = allPoints.Max(p => p.Longitude);

            //Get inside bounds
            (int baseMinTileX, int baseMaxTileY) = MapTools.GPSToTileXY(minLat, minLon);
            (int baseMaxTileX, int baseMinTileY) = MapTools.GPSToTileXY(maxLat, maxLon);

            //Inside bounds
            int originalMinTileX = baseMinTileX;
            int originalMaxTileX = baseMaxTileX;
            int originalMinTileY = baseMinTileY;
            int originalMaxTileY = baseMaxTileY;

            //Outside bounds (padding)
            int minTileX = originalMinTileX - padding;
            int maxTileX = originalMaxTileX + padding;
            int minTileY = originalMinTileY - padding;
            int maxTileY = originalMaxTileY + padding;

            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    TilePriority priority = (x >= originalMinTileX && x <= originalMaxTileX && y >= originalMinTileY && y <= originalMaxTileY) ? TilePriority.Inside : TilePriority.Outside;

                    TileDefinition tileDefinition = new TileDefinition
                    {
                        BoundingBox = MapTools.GetBoundingBoxFromTileXY(x, y),
                        ZoomLevel = MapTools.ZOOM_LEVEL_TOPOGRAPHIC,
                        X = x,
                        Y = y,
                        SatelliteTexture = null,
                        HeightMap = null,
                        TilePriority = priority
                    };

                    flightData.MapDefinition.AddTile(tileDefinition);
                }
            }

            flightData.MapDefinition.UpdateBoundingBoxFromTiles();

            return flightData;
        }

        internal async void BuildFlightMap(FlightData flightData)
        {
            if (flightData?.MapDefinition?.TileDefinitions == null)
            {
                return;
            }

            _errors.Clear();

            //Reset counts
            _satelliteCompleted = 0;
            _topographicCompleted = 0;
            _hillshadeCompleted = 0;
            _geoDataCompleted = 0;
            _buildingCompleted = 0;
            _unknownCompleted = 0;
            _totalTileDownloads = CalculateTotalTileDownloads(flightData);

            OnDownloadStarted?.Invoke();

            int satelliteZoomLevel = SettingsManager.GetSatelliteTileZoom();            

            List<Task> tasks = new List<Task>
            {
                SafeDownload(() => MapTilerAPIHelper.DownloadSatelliteTilesParallelAsync(flightData,  satelliteZoomLevel, satelliteZoomLevel - 1, () => OnTileDownloaded("Satellite")), "Satellite"),
                SafeDownload(() => MapTilerAPIHelper.DownloadTopographicTilesParallelAsync(flightData, MapTools.ZOOM_LEVEL_TOPOGRAPHIC, () => OnTileDownloaded("Topographic")), "Topographic"),
                SafeDownload(() => MapTilerAPIHelper.DownloadHillShadeTilesParallelAsync(flightData, MapTools.ZOOM_LEVEL_HILLSHADE_RASTER, () => OnTileDownloaded("HillShade")), "HillShade"),
                SafeDownload(() => MapTilerAPIHelper.DownloadGeoDataTilesParallelAsync(flightData, () => OnTileDownloaded("GeoData")), "GeoData"),
                SafeDownload(() => MapTilerAPIHelper.DownloadBuildingTilesParallelAsync(flightData, MapTools.ZOOM_LEVEL_BUILDING, () => OnTileDownloaded("Building")), "Building"),
            };

            await Task.WhenAll(tasks);
        }


        private int CalculateTotalTileDownloads(FlightData flightData)
        {
            int satelliteCount = 0;
            int topographicCount = 0;
            int geoDataCount = 0;
            int buildingCount = 0;
            int hillshadeCount = 0;
            int unknown = 0;

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                if (tile == null)
                {
                    unknown += 1;
                    continue;
                }

                int satelliteZoomLevel = SettingsManager.GetSatelliteTileZoom();
                satelliteCount += tile.TilePriority == TilePriority.Inside ? MapTools.GetTileCountFromZoomLevels(tile.ZoomLevel, satelliteZoomLevel) : MapTools.GetTileCountFromZoomLevels(tile.ZoomLevel, satelliteZoomLevel - 1);

                topographicCount += 1;

                if (tile.ZoomLevel >= MapTools.ZOOM_LEVEL_HILLSHADE_RASTER)
                {
                    hillshadeCount += 1;
                }

                buildingCount += MapTools.GetTileCountFromZoomLevels(tile.ZoomLevel, MapTools.ZOOM_LEVEL_BUILDING);
                geoDataCount += 1;
            }

            int total = satelliteCount + topographicCount + hillshadeCount + geoDataCount + buildingCount + unknown;

            return total;
        }

        private async Task SafeDownload(Func<Task> downloadFunc, string propertyName)
        {
            try
            {
                await downloadFunc();
            }
            catch (Exception ex)
            {
                _errors.Add($"{propertyName} download failed: {ex.Message}");
            }
        }

        private void OnTileDownloaded(string propertyName)
        {
            switch (propertyName)
            {
                case "Satellite":
                    _satelliteCompleted++;
                    break;
                case "Topographic":
                    _topographicCompleted++;
                    break;
                case "HillShade":
                    _hillshadeCompleted++;
                    break;
                case "GeoData":
                    _geoDataCompleted++;
                    break;
                case "Building":
                    _buildingCompleted++;
                    break;
                default:
                    _unknownCompleted++;
                    break;
            }

            int completed = _satelliteCompleted + _topographicCompleted + _hillshadeCompleted +
                            _geoDataCompleted + _buildingCompleted +
                            _unknownCompleted;

            _globalProgress = _totalTileDownloads > 0 ? Mathf.Clamp01((float)completed / _totalTileDownloads) : 0f;

            OnGlobalProgressUpdated?.Invoke(_globalProgress);
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            if (_satelliteCompleted + _topographicCompleted + _hillshadeCompleted + _geoDataCompleted + _buildingCompleted + _unknownCompleted >= _totalTileDownloads)
            {
                OnDownloadCompleted?.Invoke(_errors);
            }
        }
        #endregion
    }
}
