using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Terrain;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightReLive.Core.FlightDefinition
{
    /// <summary>
    /// Holds all flight-related data including GPS path, tiles, altitude, and metadata.
    /// Provides lookup helpers for altitude and coordinate conversion.
    /// </summary>
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
            get { return GLOBAL_SCALE; }
        }
        #endregion

        #region METHODS

        /// <summary>
        /// Initializes a new map definition based on GPS origin.
        /// </summary>
        internal void InitializeMapDefinition()
        {
            MapDefinition = new MapTilesDefinition(GPSOrigin.Latitude, GPSOrigin.Longitude);
        }

        /// <summary>
        /// Initializes the altitude of the takeoff position.
        /// </summary>
        internal void InitializeAltitude()
        {
            TakeOffAltitude = GetAltitudeAtPosition(EstimateTakeOffPosition);
        }

        /// <summary>
        /// Adds a single tile and updates the lookup table.
        /// </summary>
        internal void AddTile(TileDefinition tile)
        {
            if (tile == null)
            {
                return;
            }

            if (MapDefinition == null)
            {
                MapDefinition = new MapTilesDefinition(GPSOrigin.Latitude, GPSOrigin.Longitude);
            }

            if (_tileLookup == null)
            {
                _tileLookup = new Dictionary<(int, int), TileDefinition>();
            }

            (int, int) key = (tile.X, tile.Y);
            _tileLookup[key] = tile;
        }

        /// <summary>
        /// Builds the lookup dictionary for fast tile access and computes SceneCenterGPS.
        /// </summary>
        internal void BuildTileLookup()
        {
            if (MapDefinition?.TileDefinitions == null || MapDefinition.TileDefinitions.Count == 0)
            {
                _tileLookup = new Dictionary<(int, int), TileDefinition>();
                return;
            }

            _tileLookup = MapDefinition.TileDefinitions
                .Where(t => t != null)
                .GroupBy(t => (t.X, t.Y))
                .ToDictionary(g => g.Key, g => g.First());

            // Recompute scene center (important for world coordinate conversion)
            double avgLat = MapDefinition.TileDefinitions.Average(
                t => (t.BoundingBox.MinLatitude + t.BoundingBox.MaxLatitude) / 2.0);
            double avgLon = MapDefinition.TileDefinitions.Average(
                t => (t.BoundingBox.MinLongitude + t.BoundingBox.MaxLongitude) / 2.0);
            SceneCenterGPS = new Vector2((float)avgLat, (float)avgLon);

            InitializeAltitude();
        }

        /// <summary>
        /// Returns interpolated altitude at the given GPS position.
        /// </summary>
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

            foreach (TileDefinition t in _tileLookup.Values)
            {
                if (IsInsideBoundingBox(t.BoundingBox, gps.Latitude, gps.Longitude) && t.HeightMap != null)
                {
                    return GetAltitudeAtPosition(t, gps);
                }
            }

            return 0f;
        }

        /// <summary>
        /// Returns altitude from a specific tile using bilinear interpolation.
        /// </summary>
        internal float GetAltitudeAtPosition(TileDefinition tile, FlightGPSData gps)
        {
            GPSBoundingBox bbox = tile.BoundingBox;
            float[,] heightmap = tile.HeightMap;
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            // Normalized [0,1] coordinates
            float nx = Mathf.InverseLerp((float)bbox.MinLongitude, (float)bbox.MaxLongitude, (float)gps.Longitude);
            float ny = Mathf.InverseLerp((float)bbox.MinLatitude, (float)bbox.MaxLatitude, (float)gps.Latitude);

            // Map to heightmap indices
            float fx = nx * (width - 1);
            float fy = (1f - ny) * (height - 1); // Y inverted

            int x0 = Mathf.FloorToInt(fx);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y0 = Mathf.FloorToInt(fy);
            int y1 = Mathf.Min(y0 + 1, height - 1);

            // Bilinear interpolation
            float hx0 = Mathf.Lerp(heightmap[x0, y0], heightmap[x1, y0], fx - x0);
            float hx1 = Mathf.Lerp(heightmap[x0, y1], heightmap[x1, y1], fx - x0);
            float h = Mathf.Lerp(hx0, hx1, fy - y0);

            return h;
        }

        /// <summary>
        /// Converts a GPS position (lat, alt, lon) into world coordinates.
        /// </summary>
        internal Vector3 ConvertGPSPositionToWorld(Vector3 gpsPosition)
        {
            float xMeters = (float)MapTools.HaversineDistance(
                SceneCenterGPS.x, SceneCenterGPS.y, SceneCenterGPS.x, gpsPosition.z);
            float zMeters = (float)MapTools.HaversineDistance(
                SceneCenterGPS.x, SceneCenterGPS.y, gpsPosition.x, SceneCenterGPS.y);

            if (gpsPosition.z < SceneCenterGPS.y)
            {
                xMeters *= -1f;
            }

            if (gpsPosition.x < SceneCenterGPS.x)
            {
                zMeters *= -1f;
            }

            float yMeters = gpsPosition.y;
            return new Vector3(xMeters, yMeters, zMeters) * GLOBAL_SCALE;
        }

        /// <summary>
        /// Generates a smoothed bezier flight path in world space.
        /// </summary>
        internal List<Vector3> CreateBezierFlightPath(float referenceAltitude, int samplesPerSegment = 10, float controlOffsetFactor = 0.3f)
        {
            if (Points == null || Points.Count < 2 || MapDefinition?.TileDefinitions == null || MapDefinition.TileDefinitions.Count == 0)
            {
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
            return positions;
        }

        private static bool IsInsideBoundingBox(GPSBoundingBox bb, double lat, double lon)
        {
            const double eps = 1e-7;
            return lat >= bb.MinLatitude - eps &&
                   lat <= bb.MaxLatitude + eps &&
                   lon >= bb.MinLongitude - eps &&
                   lon <= bb.MaxLongitude + eps;
        }

        /// <summary>
        /// Cleans up resources for this flight.
        /// </summary>
        public void Dispose()
        {
            if (MapDefinition?.TileDefinitions != null)
            {
                foreach (TileDefinition tileDefinition in MapDefinition.TileDefinitions)
                {
                    tileDefinition.SatelliteTexture = null;
                    tileDefinition.HeightMap = null;
                    tileDefinition.GeoData = null;
                    tileDefinition.Buildings = null;
                    tileDefinition.MeshData = null;
                }
            }

            _tileLookup?.Clear();
        }
        #endregion
    }
}
