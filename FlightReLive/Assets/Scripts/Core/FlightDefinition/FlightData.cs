using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Terrain;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightReLive.Core.FlightDefinition
{
    [Serializable]
    internal class FlightData : IDisposable
    {
        #region CONSTANTS
        private const float GLOBAL_SCALE = 0.1f;
        #endregion

        #region ATTRIBUTES
        private Dictionary<(int, int), TileDefinition> _tileLookup;
        #endregion

        #region PROPERTIES
        internal string Name { set; get; }

        internal string VideoPath { set; get; }

        internal DateTime Date { set; get; }

        internal List<FlightDataPoint> Points { set; get; }

        internal MapTilesDefinition MapDefinition { set; get; }

        internal Texture2D Thumbnail { set; get; }

        internal Vector2 SceneCenterGPS { get; set; }

        internal bool HasTakeOffPosition { get; set; }

        internal bool HasExtractionError { get; set; }

        internal bool IsValid { get; set; }

        internal FlightGPSData EstimateTakeOffPosition { set; get; }

        internal float TakeOffAltitude { get; set; }

        internal FlightGPSData GPSOrigin { get; set; }

        internal TimeSpan Length { get; set; }

        internal float GlobalScale
        {
            get
            { 
                return GLOBAL_SCALE;
            }
        }
        #endregion

        #region METHODS
        internal void InitializeMapDefinition()
        {
            MapDefinition = new MapTilesDefinition(GPSOrigin.Latitude, GPSOrigin.Longitude);
        }

        internal void InitializeAltitude()
        {
            TakeOffAltitude = GetAltitudeAtPosition(EstimateTakeOffPosition);
        }

        internal void BuildTileLookup()
        {
            if (MapDefinition?.TileDefinitions == null)
            {
                _tileLookup = new Dictionary<(int, int), TileDefinition>();
                return;
            }

            _tileLookup = MapDefinition.TileDefinitions
                .Where(t => t != null)
                .GroupBy(t => (t.X, t.Y))
                .ToDictionary(g => g.Key, g => g.First());
        }

        internal float GetAltitudeAtPosition(FlightGPSData gps)
        {
            if (_tileLookup == null || _tileLookup.Count == 0)
            {
                BuildTileLookup();
                if (_tileLookup.Count == 0)
                {
                    return 0f;
                }
            }

            (int tx, int ty) = MapTools.GPSToTileXY(gps.Latitude, gps.Longitude);

            if (_tileLookup.TryGetValue((tx, ty), out TileDefinition tile) && tile.HeightMap != null)
            {
                return GetAltitudeAtPosition(tile, gps);
            }

            foreach (var t in _tileLookup.Values)
            {
                if (IsInsideBoundingBox(t.BoundingBox, gps.Latitude, gps.Longitude) && t.HeightMap != null)
                {
                    return GetAltitudeAtPosition(t, gps);
                }
            }

            return 0f;
        }

        internal float GetAltitudeAtPosition(TileDefinition tile, FlightGPSData gps)
        {
            GPSBoundingBox bbox = tile.BoundingBox;
            float[,] heightmap = tile.HeightMap;
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            float nx = Mathf.InverseLerp((float)bbox.MinLongitude, (float)bbox.MaxLongitude, (float)gps.Longitude);
            float ny = Mathf.InverseLerp((float)bbox.MinLatitude, (float)bbox.MaxLatitude, (float)gps.Latitude);

            float fx = nx * (width - 1);
            float fy = (1f - ny) * (height - 1);

            int x0 = Mathf.FloorToInt(fx);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y0 = Mathf.FloorToInt(fy);
            int y1 = Mathf.Min(y0 + 1, height - 1);

            float hx0 = Mathf.Lerp(heightmap[x0, y0], heightmap[x1, y0], fx - x0);
            float hx1 = Mathf.Lerp(heightmap[x0, y1], heightmap[x1, y1], fx - x0);
            float h = Mathf.Lerp(hx0, hx1, fy - y0);

            return h;
        }

        internal Vector3 ConvertGPSPositionToWorld(Vector3 gpsPosition)
        {
            float xMeters = (float)MapTools.HaversineDistance(SceneCenterGPS.x, SceneCenterGPS.y, SceneCenterGPS.x, gpsPosition.z);
            float zMeters = (float)MapTools.HaversineDistance(SceneCenterGPS.x, SceneCenterGPS.y, gpsPosition.x, SceneCenterGPS.y);

            if (gpsPosition.z < SceneCenterGPS.y) xMeters *= -1f;
            if (gpsPosition.x < SceneCenterGPS.x) zMeters *= -1f;

            float yMeters = gpsPosition.y;
            Vector3 result = new Vector3(xMeters, yMeters, zMeters) * GLOBAL_SCALE;

            return result;
        }

        internal List<Vector3> CreateBezierFlightPath(float referenceAltitude, int samplesPerSegment = 10, float controlOffsetFactor = 0.3f)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (Points == null || Points.Count < 2 || MapDefinition.TileDefinitions == null || MapDefinition.TileDefinitions.Count == 0)
            {
                sw.Stop();
                Debug.Log("CreateBezierFlightPath: invalid input (0 ms)");
                return new List<Vector3>();
            }

            List<Vector3> positions = Points
                .OrderBy(x => x.TimeSpan)
                .Select(p =>
                {
                    Vector3 gps = new Vector3((float)p.Latitude, referenceAltitude + (float)p.RelativeAltitude, (float)p.Longitude);
                    return ConvertGPSPositionToWorld(gps);
                })
                .ToList();

            positions = MapTools.PreSmoothGPS(positions, radius: 5);

            sw.Stop();
            Debug.Log("CreateBezierFlightPath: " + sw.ElapsedMilliseconds + " ms");

            return positions;
        }

        private static bool IsInsideBoundingBox(GPSBoundingBox bb, double lat, double lon)
        {
            const double eps = 1e-7;
            return lat >= bb.MinLatitude - eps && lat <= bb.MaxLatitude + eps && lon >= bb.MinLongitude - eps && lon <= bb.MaxLongitude + eps;
        }

        public void Dispose()
        {
            foreach (TileDefinition tileDefinition in MapDefinition.TileDefinitions)
            {
                tileDefinition.SatelliteTexture = null;
                tileDefinition.HeightMap = null;
                tileDefinition.MeshData = null;
            }

            _tileLookup?.Clear();
        }
        #endregion
    }
}
