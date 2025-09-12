using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Terrain
{
    public static class MapTools
    {
        #region CONSTANTS
        internal static readonly int TILE_RESOLUTION = 512;
        internal static readonly int ZOOM_LEVEL_TOPOGRAPHIC = 14;
        internal static readonly int ZOOM_LEVEL_BUILDING = 14;
        #endregion

        #region METHODS

        internal static (int tileX, int tileY) GPSToTileXY(double latitude, double longitude)
        {
            latitude = Math.Clamp(latitude, -85.05112878, 85.05112878);

            double latRad = latitude * Math.PI / 180.0;
            int numTiles = 1 << ZOOM_LEVEL_TOPOGRAPHIC;

            double x = (longitude + 180.0) / 360.0 * numTiles;
            double y = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * numTiles;

            return ((int)x, (int)y);
        }

        internal static GPSBoundingBox GetBoundingBoxFromTileXY(int xTile, int yTile)
        {
            int n = 1 << ZOOM_LEVEL_TOPOGRAPHIC;
            double lonPerTile = 360.0 / n;
            double minLon = xTile * lonPerTile - 180.0;
            double maxLon = (xTile + 1) * lonPerTile - 180.0;
            double latRadNorth = Math.Atan(Math.Sinh(Math.PI * (1 - 2.0 * yTile / n)));
            double latRadSouth = Math.Atan(Math.Sinh(Math.PI * (1 - 2.0 * (yTile + 1) / n)));
            double maxLat = latRadNorth * (180.0 / Math.PI);
            double minLat = latRadSouth * (180.0 / Math.PI);

            return new GPSBoundingBox
            {
                MinLatitude = minLat,
                MaxLatitude = maxLat,
                MinLongitude = minLon,
                MaxLongitude = maxLon
            };
        }

        internal static HashSet<(int x, int y)> GetTilesFromZoomLevel(TileDefinition tile, int targetZoom)
        {
            int sourceZoom = tile.ZoomLevel;
            int zoomDiff = targetZoom - sourceZoom;

            HashSet<(int x, int y)> result = new HashSet<(int x, int y)>();

            if (zoomDiff < 0)
            {
                int parentX = tile.X >> -zoomDiff;
                int parentY = tile.Y >> -zoomDiff;
                result.Add((parentX, parentY));

                return result;
            }

            int factor = 1 << zoomDiff;
            int startX = tile.X * factor;
            int startY = tile.Y * factor;

            for (int x = startX; x < startX + factor; x++)
            {
                for (int y = startY; y < startY + factor; y++)
                {
                    result.Add((x, y));
                }
            }

            return result;
        }


        internal static int GetTileCountFromZoomLevels(int currentZoom, int targetZoom)
        {
            int zoomDiff = targetZoom - currentZoom;

            if (zoomDiff < 0)
            {
                return 1;
            }

            int factor = 1 << zoomDiff;

            return factor * factor;
        }


        internal static List<Vector3> PreSmoothGPS(List<Vector3> rawPoints, int radius = 3)
        {
            List<Vector3> smoothed = new List<Vector3>();

            for (int i = 0; i < rawPoints.Count; i++)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;

                for (int j = -radius; j <= radius; j++)
                {
                    int idx = Mathf.Clamp(i + j, 0, rawPoints.Count - 1);
                    sum += rawPoints[idx];
                    count++;
                }

                smoothed.Add(sum / count);
            }

            return smoothed;
        }

        internal static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // rayon de la Terre en mètres
            double dLat = Math.PI / 180 * (lat2 - lat1);
            double dLon = Math.PI / 180 * (lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(Math.PI / 180 * lat1) * Math.Cos(Math.PI / 180 * lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        internal static FlightGPSData GetCenterOfBoundingBox(GPSBoundingBox bbox)
        {
            double centerLatitude = (bbox.MinLatitude + bbox.MaxLatitude) / 2.0;
            double centerLongitude = (bbox.MinLongitude + bbox.MaxLongitude) / 2.0;

            return new FlightGPSData(centerLatitude, centerLongitude);
        }

        internal static GPSBoundingBox GetGlobalBoundingBox(List<TileDefinition> tiles)
        {
            double minLon = double.MaxValue;
            double minLat = double.MaxValue;
            double maxLon = double.MinValue;
            double maxLat = double.MinValue;

            foreach (var tile in tiles)
            {
                minLon = Math.Min(minLon, tile.BoundingBox.MinLongitude);
                minLat = Math.Min(minLat, tile.BoundingBox.MinLatitude);
                maxLon = Math.Max(maxLon, tile.BoundingBox.MaxLongitude);
                maxLat = Math.Max(maxLat, tile.BoundingBox.MaxLatitude);
            }

            return new GPSBoundingBox
            {
                MinLongitude = minLon,
                MinLatitude = minLat,
                MaxLongitude = maxLon,
                MaxLatitude = maxLat
            };
        }

        internal static float GetTileSizeMeters(double latitude)
        {
            const double EarthCircumference = 40075016.68557849;
            double latitudeRad = latitude * Math.PI / 180.0;
            double numTiles = Math.Pow(2, ZOOM_LEVEL_TOPOGRAPHIC);
            double metersPerTileEquator = EarthCircumference / numTiles;
            double metersPerTile = metersPerTileEquator * Math.Cos(latitudeRad);
            double metersPerPixel = metersPerTile / TILE_RESOLUTION;

            return (float)(TILE_RESOLUTION * metersPerPixel);
        }
        #endregion
    }
}
