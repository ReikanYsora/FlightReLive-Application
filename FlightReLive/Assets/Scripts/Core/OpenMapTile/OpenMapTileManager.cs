using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using System;
using System.Collections.Generic;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core.OpenMapTile
{
    /// <summary>
    /// Manager responsible for collecting OpenMapTile features (zones except buildings).
    /// </summary>
    internal class OpenMapTileManager : MonoBehaviour
    {
        #region CONSTANTS
        private const double CLIP_SCALE = 1000.0;
        private const float OPENMAPTILE_EXTENT = 4096f;
        #endregion

        #region ENUMS
        internal enum OpenMapTileZone { LandUse, Water, LandCover, Park, Aeroway }
        #endregion

        #region ATTRIBUTES
        private readonly Dictionary<OpenMapTileZone, Dictionary<OpenMapTileFeature, List<List<Vector2>>>> _zoneContours = new Dictionary<OpenMapTileZone, Dictionary<OpenMapTileFeature, List<List<Vector2>>>>();
        private readonly Dictionary<(int, int), List<GameObject>> _tileToOpenMapTileZones = new Dictionary<(int, int), List<GameObject>>();
        #endregion

        #region PROPERTIES
        public static OpenMapTileManager Instance { get; private set; }
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

            foreach (OpenMapTileZone z in Enum.GetValues(typeof(OpenMapTileZone)))
            {
                _zoneContours[z] = new Dictionary<OpenMapTileFeature, List<List<Vector2>>>();
            }
        }
        #endregion

        #region LOAD / UNLOAD
        internal void LoadTile(TileDefinition tile, FlightData flight)
        {
            if (tile == null || tile.Features == null || tile.Features.Count == 0)
            {
                return;
            }

            AccumulateFeatures(tile, flight);
        }

        internal void Unload()
        {
            foreach (OpenMapTileZone z in _zoneContours.Keys)
            {
                _zoneContours[z].Clear();
            }

            _tileToOpenMapTileZones.Clear();
        }
        #endregion

        #region ACCUMULATION
        /// <summary>
        /// Accumulates features. Buildings are created immediately.
        /// Zones are only stored (contours).
        /// </summary>
        private void AccumulateFeatures(TileDefinition tile, FlightData flight)
        {
            List<GameObject> createdForTile = new List<GameObject>();

            //For now, we need only water feature to create ARM texture (reflects on water area)
            foreach (OpenMapTileFeature feature in tile.Features)
            {
                if (feature is WaterFeature)
                {
                    List<List<SerializablePoint2D>> geometries = feature.Geometry;

                    if (geometries == null)
                    {
                        continue;
                    }

                    foreach (List<SerializablePoint2D> ringRaw in geometries)
                    {
                        if (ringRaw == null || ringRaw.Count < 3)
                        {
                            continue;
                        }

                        List<Point2d<int>> ring = new(ringRaw.Count);
                        foreach (SerializablePoint2D pt in ringRaw) ring.Add(pt.ToPoint2D());

                        ring = ClipRingToExtent(ring);

                        if (ring.Count < 3)
                        {
                            continue;
                        }

                        List<Vector2> contour = ConvertGeometryToContour(flight, ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);

                        if (contour == null || contour.Count < 3)
                        {
                            continue;
                        }

                        OpenMapTileZone zoneType  = OpenMapTileZone.Water;


                        if (!_zoneContours[zoneType].ContainsKey(feature))
                        {
                            _zoneContours[zoneType][feature] = new List<List<Vector2>>();
                        }

                        Debug.Log("Add water feature");
                        _zoneContours[zoneType][feature].Add(contour);
                    }
                }
            }

            _tileToOpenMapTileZones[(tile.X, tile.Y)] = createdForTile;
        }

        private List<Vector2> ConvertGeometryToContour(FlightData flight, List<Point2d<int>> ring, int tileX, int tileY, int zoom)
        {
            ComputeTileWorldCorners(flight, tileX, tileY, zoom, out Vector3 worldNW, out Vector3 worldNE, out Vector3 worldSW, out Vector3 worldSE);
            List<Vector2> contour = new List<Vector2>(ring.Count);

            for (int i = 0; i < ring.Count; i++)
            {
                Point2d<int> p = ring[i];
                float u = p.X / OPENMAPTILE_EXTENT;
                float v = p.Y / OPENMAPTILE_EXTENT;

                Vector3 top = Vector3.Lerp(worldNW, worldNE, u);
                Vector3 bottom = Vector3.Lerp(worldSW, worldSE, u);
                Vector3 world = Vector3.Lerp(top, bottom, v);

                if (!float.IsNaN(world.x) && !float.IsNaN(world.z))
                {
                    contour.Add(new Vector2(world.x, world.z));
                }
            }

            return contour;
        }

        private void ComputeTileWorldCorners(FlightData flight, int tileX, int tileY, int zoom,out Vector3 worldNW, out Vector3 worldNE, out Vector3 worldSW, out Vector3 worldSE)
        {
            double lonW = (double)tileX / (1 << zoom) * 360.0 - 180.0;
            double lonE = (double)(tileX + 1) / (1 << zoom) * 360.0 - 180.0;
            double latN = TileYToLat(tileY, zoom);
            double latS = TileYToLat(tileY + 1, zoom);

            worldNW = flight.ConvertGPSPositionToWorld(new Vector3((float)latN, 0f, (float)lonW));
            worldNE = flight.ConvertGPSPositionToWorld(new Vector3((float)latN, 0f, (float)lonE));
            worldSW = flight.ConvertGPSPositionToWorld(new Vector3((float)latS, 0f, (float)lonW));
            worldSE = flight.ConvertGPSPositionToWorld(new Vector3((float)latS, 0f, (float)lonE));
        }


        private static List<Point2d<int>> ClipRingToExtent(List<Point2d<int>> ring)
        {
            List<Vector2> input = new List<Vector2>(ring.Count);

            for (int i = 0; i < ring.Count; i++)
            {
                input.Add(new Vector2(ring[i].X, ring[i].Y));
            }

            float minX = 0f, minY = 0f, maxX = OPENMAPTILE_EXTENT, maxY = OPENMAPTILE_EXTENT;

            List<Vector2> ClipAgainst(List<Vector2> subject, Func<Vector2, bool> inside, Func<Vector2, Vector2, Vector2> intersect)
            {
                List<Vector2> output = new List<Vector2>();

                for (int i = 0; i < subject.Count; i++)
                {
                    Vector2 current = subject[i];
                    Vector2 prev = subject[(i - 1 + subject.Count) % subject.Count];
                    bool currInside = inside(current), prevInside = inside(prev);

                    if (prevInside && currInside)
                    {
                        output.Add(current);
                    }
                    else if (prevInside && !currInside)
                    {
                        output.Add(intersect(prev, current));
                    }
                    else if (!prevInside && currInside)
                    {
                        output.Add(intersect(prev, current));
                        output.Add(current);
                    }
                }
                return output;
            }

            //Clip left
            input = ClipAgainst(input, p => p.x >= minX, (a, b) =>
            {
                float t = (minX - a.x) / (b.x - a.x + 1e-6f);

                return new Vector2(minX, a.y + t * (b.y - a.y));
            });
            //Clip right
            input = ClipAgainst(input, p => p.x <= maxX, (a, b) =>
            {
                float t = (maxX - a.x) / (b.x - a.x + 1e-6f);

                return new Vector2(maxX, a.y + t * (b.y - a.y));
            });
            //Clip bottom
            input = ClipAgainst(input, p => p.y >= minY, (a, b) =>
            {
                float t = (minY - a.y) / (b.y - a.y + 1e-6f);

                return new Vector2(a.x + t * (b.x - a.x), minY);
            });
            //Clip top
            input = ClipAgainst(input, p => p.y <= maxY, (a, b) =>
            {
                float t = (maxY - a.y) / (b.y - a.y + 1e-6f);

                return new Vector2(a.x + t * (b.x - a.x), maxY);
            });

            List<Point2d<int>> clipped = new List<Point2d<int>>(input.Count);

            for (int i = 0; i < input.Count; i++)
            {
                clipped.Add(new Point2d<int>((int)input[i].x, (int)input[i].y));
            }

            return clipped;
        }

        private double TileYToLat(int tileY, int zoom)
        {
            double n = Math.PI - 2.0 * Math.PI * tileY / (1 << zoom);

            return Math.Atan(Math.Sinh(n)) * (180.0 / Math.PI);
        }
        #endregion
    }
}
