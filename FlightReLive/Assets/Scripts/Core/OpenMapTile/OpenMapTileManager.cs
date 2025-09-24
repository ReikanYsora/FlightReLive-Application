using Clipper2Lib;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using Fu.Framework;
using LibTessDotNet;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core.OpenMapTile
{
    /// <summary>
    /// Manager responsible for creating, merging, displaying and unloading OpenMapTile volumes in the scene.
    /// Uses bilinear mapping between tile corners and extent coordinates to ensure pixel-perfect alignment.
    /// Performs per-zone-type + per-class union to avoid overlaps between tiles.
    /// </summary>
    [RequireComponent(typeof(OpenMapTilePool))]
    internal class OpenMapTileManager : MonoBehaviour
    {
        private enum OpenMapTileZone { LandUse, Water, LandCover, Park, Aeroway }

        #region CONSTANTS
        private const float OPENMAPTILE_EXTENT = 4096f;
        private const float BOTTOM_EXTRUSION = 1f;
        #endregion

        #region ATTRIBUTES
        [SerializeField] private float _zoneAltitude = 100f;
        [SerializeField] private float _minExtrusion = -10f;
        [SerializeField] private float _maxExtrusion = 20f;

        [Header("Materials")]
        [SerializeField] private Material _openMapTileBuildingMaterial;
        [SerializeField] private Material _openMapTileLandUseMaterial;
        [SerializeField] private Material _openMapTileWaterMaterial;
        [SerializeField] private Material _openMapTileLandCoverMaterial;
        [SerializeField] private Material _openMapTileParkMaterial;
        [SerializeField] private Material _openMapTileAerowayMaterial;

        private OpenMapTilePool _openMapTileZonePool;
        private readonly List<GameObject> _openMapTileObjects = new List<GameObject>();
        private readonly Dictionary<(int, int), List<GameObject>> _tileToOpenMapTileZones = new();

        /// <summary>
        /// Store zone contours grouped by zone type + class key.
        /// </summary>
        private readonly Dictionary<OpenMapTileZone, Dictionary<string, List<List<Vector2>>>> _zoneContours = new Dictionary<OpenMapTileZone, Dictionary<string, List<List<Vector2>>>>();
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

            _openMapTileZonePool = GetComponent<OpenMapTilePool>();

            foreach (OpenMapTileZone z in Enum.GetValues(typeof(OpenMapTileZone)))
            {
                _zoneContours[z] = new Dictionary<string, List<List<Vector2>>>();
            }
        }

        private void Start()
        {
            SettingsManager.OnBuildingVisibilityChanged += OnBuildingVisibilityChanged;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
        }

        private void OnDestroy()
        {
            SettingsManager.OnBuildingVisibilityChanged -= OnBuildingVisibilityChanged;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
        }
        #endregion

        #region LOAD / UNLOAD
        internal void LoadTile(TileDefinition tile, FlightData flight)
        {
            if (tile == null || tile.Features == null || tile.Features.Count == 0)
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                AccumulateFeatures(tile, flight);
            });
        }

        /// <summary>
        /// Accumulates features. Buildings are created immediately,
        /// zones are stored (world-space) grouped by type and class.
        /// </summary>
        private void AccumulateFeatures(TileDefinition tile, FlightData flight)
        {
            List<GameObject> createdForTile = new List<GameObject>();

            foreach (OpenMapTileFeature feature in tile.Features)
            {
                if (feature is BuildingFeature building)
                {
                    GenerateBuilding(building, tile, flight, createdForTile);
                }
                else
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

                        List<Point2d<int>> ring = new List<Point2d<int>>(ringRaw.Count);
                        foreach (SerializablePoint2D pt in ringRaw) { ring.Add(pt.ToPoint2D()); }

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

                        // Determine zone type and class key
                        OpenMapTileZone? zoneType = null;
                        string classKey = "default";

                        if (feature is LanduseFeature luf)
                        {
                            zoneType = OpenMapTileZone.LandUse;
                            classKey = luf.Class ?? "default";
                        }
                        else if (feature is WaterFeature wf)
                        {
                            zoneType = OpenMapTileZone.Water;
                            classKey = wf.Class ?? (wf.IsIntermittent ? "intermittent" : "default");
                        }
                        else if (feature is LandcoverFeature lcf)
                        {
                            zoneType = OpenMapTileZone.LandCover;
                            classKey = $"{lcf.Class ?? "default"}_{lcf.Subclass ?? "none"}";
                        }
                        else if (feature is ParkFeature pf)
                        {
                            zoneType = OpenMapTileZone.Park;
                            classKey = pf.Class ?? "default";
                        }
                        else if (feature is AerowayFeature af)
                        {
                            zoneType = OpenMapTileZone.Aeroway;
                            classKey = af.Class ?? "default";
                        }

                        if (zoneType.HasValue)
                        {
                            if (!_zoneContours[zoneType.Value].ContainsKey(classKey))
                            {
                                _zoneContours[zoneType.Value][classKey] = new List<List<Vector2>>();
                            }
                            _zoneContours[zoneType.Value][classKey].Add(contour);
                        }
                    }
                }
            }

            _tileToOpenMapTileZones[(tile.X, tile.Y)] = createdForTile;
            tile.Features = null;
        }

        private void BuildMergedZones(FlightData flight)
        {
            foreach (var zoneEntry in _zoneContours)
            {
                OpenMapTileZone zoneType = zoneEntry.Key;

                foreach (var classEntry in zoneEntry.Value)
                {
                    string classKey = classEntry.Key;
                    List<List<Vector2>> contours = classEntry.Value;

                    if (contours.Count == 0)
                    {
                        continue;
                    }

                    List<List<Vector2>> mergedContours = UnionWithClipperWorld(contours);

                    foreach (List<Vector2> merged in mergedContours)
                    {
                        float baseY = (_zoneAltitude + _minExtrusion) * flight.GlobalScale;
                        float topY = (_zoneAltitude + _maxExtrusion) * flight.GlobalScale;

                        Vector2 center = Vector2.zero;
                        foreach (Vector2 p in merged) { center += p; }
                        center /= merged.Count;

                        Vector3 zonePosition = new Vector3(center.x, 0f, center.y);

                        List<Vector2> localContour = new List<Vector2>(merged.Count);
                        foreach (Vector2 p in merged)
                        {
                            localContour.Add(new Vector2(p.x - center.x, p.y - center.y));
                        }

                        MeshData meshData = TriangulateZoneVolume(flight, localContour, baseY, topY);
                        CreateZone(zoneType, meshData, zonePosition);
                    }
                }
            }
        }

        internal void Unload()
        {
            foreach (GameObject go in _openMapTileObjects)
            {
                _openMapTileZonePool.Return(go);
            }

            _openMapTileObjects.Clear();
            _tileToOpenMapTileZones.Clear();

            foreach (OpenMapTileZone z in _zoneContours.Keys)
            {
                _zoneContours[z].Clear();
            }
        }
        #endregion

        #region BUILDINGS
        private void GenerateBuilding(BuildingFeature building, TileDefinition tile, FlightData flight, List<GameObject> createdForTile)
        {
            foreach (List<SerializablePoint2D> ringRaw in building.Geometry)
            {
                if (ringRaw == null || ringRaw.Count < 3)
                {
                    continue;
                }

                List<Point2d<int>> ring = new(ringRaw.Count);

                foreach (SerializablePoint2D pt in ringRaw)
                {
                    ring.Add(pt.ToPoint2D());
                }

                ring = ClipRingToExtent(ring);

                if (ring.Count < 3)
                {
                    continue;
                }

                List<Vector2> contour = ConvertGeometryToContour(flight, ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);

                if (contour == null || contour.Count == 0)
                {
                    continue;
                }

                Vector2 center = ComputeRingBarycenterWorld(ring, flight, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (float.IsNaN(center.x) || float.IsNaN(center.y))
                {
                    continue;
                }

                FlightGPSData baryGPS = ComputeRingBarycenterGPS(ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                float terrainAltitude = flight.GetAltitudeAtPosition(tile, baryGPS);

                Vector3 position = new(center.x, terrainAltitude * flight.GlobalScale, center.y);
                float estimatedHeight = EstimateHeightFromFootprint(contour, flight);

                MeshData meshData = TriangulateAndExtrude(flight, contour, estimatedHeight);
                GameObject buildingGO = CreateBuilding(meshData, position);
                createdForTile.Add(buildingGO);
            }
        }
        #endregion

        #region CLIPPER
        private List<List<Vector2>> UnionWithClipperWorld(List<List<Vector2>> contours)
        {
            PathsD subject = new PathsD();

            foreach (List<Vector2> contour in contours)
            {
                PathD path = new PathD(contour.Count);

                foreach (Vector2 p in contour)
                {
                    path.Add(new PointD(p.x, p.y));
                }

                subject.Add(path);
            }

            PathsD solution = Clipper.Union(subject, FillRule.NonZero);

            List<List<Vector2>> result = new List<List<Vector2>>();
            foreach (PathD path in solution)
            {
                List<Vector2> contour = new List<Vector2>(path.Count);

                foreach (PointD pt in path)
                {
                    contour.Add(new Vector2((float)pt.x, (float)pt.y));
                }

                if (contour.Count > 2)
                {
                    result.Add(contour);
                }
            }

            return result;
        }
        #endregion

        #region GEOMETRY HELPERS
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

        private void ComputeTileWorldCorners(FlightData flight, int tileX, int tileY, int zoom, out Vector3 worldNW, out Vector3 worldNE, out Vector3 worldSW, out Vector3 worldSE)
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

        private Vector2 ComputeRingBarycenterWorld(List<Point2d<int>> ring, FlightData flight, int tileX, int tileY, int zoom)
        {
            ComputeTileWorldCorners(flight, tileX, tileY, zoom, out Vector3 worldNW, out Vector3 worldNE, out Vector3 worldSW, out Vector3 worldSE);

            float sumX = 0f;
            float sumZ = 0f;
            int count = 0;

            for (int i = 0; i < ring.Count; i++)
            {
                Point2d<int> p = ring[i];

                float u = p.X / OPENMAPTILE_EXTENT;
                float v = p.Y / OPENMAPTILE_EXTENT;

                Vector3 top = Vector3.Lerp(worldNW, worldNE, u);
                Vector3 bottom = Vector3.Lerp(worldSW, worldSE, u);
                Vector3 world = Vector3.Lerp(top, bottom, v);

                if (float.IsNaN(world.x) || float.IsNaN(world.z))
                {
                    continue;
                }

                sumX += world.x;
                sumZ += world.z;
                count++;
            }

            if (count == 0)
            {
                return new Vector2(float.NaN, float.NaN);
            }

            return new Vector2(sumX / count, sumZ / count);
        }

        private FlightGPSData ComputeRingBarycenterGPS(List<Point2d<int>> ring, int tileX, int tileY, int zoom)
        {
            double lonW = (double)tileX / (1 << zoom) * 360.0 - 180.0;
            double lonE = (double)(tileX + 1) / (1 << zoom) * 360.0 - 180.0;
            double latN = TileYToLat(tileY, zoom);
            double latS = TileYToLat(tileY + 1, zoom);

            double sumLat = 0.0;
            double sumLon = 0.0;
            int count = 0;

            for (int i = 0; i < ring.Count; i++)
            {
                Point2d<int> p = ring[i];

                float u = p.X / OPENMAPTILE_EXTENT;
                float v = p.Y / OPENMAPTILE_EXTENT;

                double lon = Mathf.Lerp((float)lonW, (float)lonE, u);
                double lat = Mathf.Lerp((float)latN, (float)latS, v);

                if (double.IsNaN(lat) || double.IsNaN(lon))
                {
                    continue;
                }

                sumLat += lat;
                sumLon += lon;
                count++;
            }

            if (count == 0)
            {
                return new FlightGPSData(0.0, 0.0);
            }

            return new FlightGPSData(sumLat / count, sumLon / count);
        }

        private static List<Point2d<int>> ClipRingToExtent(List<Point2d<int>> ring)
        {
            List<Vector2> input = new List<Vector2>(ring.Count);

            for (int i = 0; i < ring.Count; i++)
            {
                input.Add(new Vector2(ring[i].X, ring[i].Y));
            }

            float minX = 0f;
            float minY = 0f;
            float maxX = OPENMAPTILE_EXTENT;
            float maxY = OPENMAPTILE_EXTENT;

            List<Vector2> ClipAgainst(List<Vector2> subject, Func<Vector2, bool> inside, Func<Vector2, Vector2, Vector2> intersect)
            {
                List<Vector2> output = new List<Vector2>();

                for (int i = 0; i < subject.Count; i++)
                {
                    Vector2 current = subject[i];
                    Vector2 prev = subject[(i - 1 + subject.Count) % subject.Count];

                    bool currInside = inside(current);
                    bool prevInside = inside(prev);

                    if (prevInside && currInside)
                    {
                        output.Add(current);
                    }
                    else if (prevInside && !currInside)
                    {
                        Vector2 inter = intersect(prev, current);
                        if (!float.IsNaN(inter.x) && !float.IsNaN(inter.y))
                        {
                            output.Add(inter);
                        }
                    }
                    else if (!prevInside && currInside)
                    {
                        Vector2 inter = intersect(prev, current);
                        if (!float.IsNaN(inter.x) && !float.IsNaN(inter.y))
                        {
                            output.Add(inter);
                        }
                        output.Add(current);
                    }
                }
                return output;
            }

            //Left
            input = ClipAgainst(input, p => p.x >= minX, (a, b) =>
            {
                float denom = (b.x - a.x);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (minX - a.x) / denom;

                return new Vector2(minX, a.y + t * (b.y - a.y));
            });

            //Right
            input = ClipAgainst(input, p => p.x <= maxX, (a, b) =>
            {
                float denom = (b.x - a.x);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (maxX - a.x) / denom;

                return new Vector2(maxX, a.y + t * (b.y - a.y));
            });

            //Bottom
            input = ClipAgainst(input, p => p.y >= minY, (a, b) =>
            {
                float denom = (b.y - a.y);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (minY - a.y) / denom;

                return new Vector2(a.x + t * (b.x - a.x), minY);
            });

            //Top
            input = ClipAgainst(input, p => p.y <= maxY, (a, b) =>
            {
                float denom = (b.y - a.y);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (maxY - a.y) / denom;

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

        #region MESH HELPERS
        private MeshData TriangulateAndExtrude(FlightData flight, List<Vector2> contour, float topY)
        {
            float baseY = -BOTTOM_EXTRUSION * flight.GlobalScale;

            Tess tess = new Tess();
            ContourVertex[] tessContour = new ContourVertex[contour.Count];

            for (int i = 0; i < contour.Count; i++)
            {
                tessContour[i].Position = new Vec3(contour[i].x, contour[i].y, 0.0f);
            }

            tess.AddContour(tessContour, ContourOrientation.Clockwise);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            int roofVertexCount = tess.Vertices.Length;
            int roofTriangleCount = tess.ElementCount * 3;
            int wallVertexCount = contour.Count * 4;
            int wallTriangleCount = contour.Count * 6;
            int totalVertexCount = roofVertexCount + wallVertexCount;
            int totalTriangleCount = roofTriangleCount + wallTriangleCount;

            MeshData meshData = new MeshData
            {
                vertices = new NativeArray<Vector3>(totalVertexCount, Allocator.Persistent),
                normals = new NativeArray<Vector3>(totalVertexCount, Allocator.Persistent),
                triangles = new NativeArray<int>(totalTriangleCount, Allocator.Persistent),
                uvs = new NativeArray<Vector2>(totalVertexCount, Allocator.Persistent)
            };

            int v = 0;
            int t = 0;

            Vector2 uvScale = new Vector2(0.1f, 0.1f);

            //Roof
            for (int i = 0; i < roofVertexCount; i++)
            {
                Vec3 vertex = tess.Vertices[i].Position;
                meshData.vertices[v] = new Vector3(vertex.X, topY, vertex.Y);
                meshData.normals[v] = Vector3.up;
                meshData.uvs[v] = new Vector2(vertex.X, vertex.Y) * uvScale;
                v++;
            }
            for (int i = 0; i < tess.ElementCount; i++)
            {
                meshData.triangles[t++] = tess.Elements[i * 3 + 2];
                meshData.triangles[t++] = tess.Elements[i * 3 + 1];
                meshData.triangles[t++] = tess.Elements[i * 3 + 0];
            }

            //Walls
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p0 = contour[i];
                Vector2 p1 = contour[(i + 1) % contour.Count];

                int baseIndex = v;

                Vector3 v0p = new Vector3(p0.x, baseY, p0.y);
                Vector3 v1p = new Vector3(p0.x, topY, p0.y);
                Vector3 v2p = new Vector3(p1.x, topY, p1.y);
                Vector3 v3p = new Vector3(p1.x, baseY, p1.y);

                meshData.vertices[v++] = v0p;
                meshData.vertices[v++] = v1p;
                meshData.vertices[v++] = v2p;
                meshData.vertices[v++] = v3p;

                Vector3 edge = v2p - v1p;
                Vector3 normal = Vector3.Cross(Vector3.up, edge).normalized;

                meshData.normals[baseIndex + 0] = normal;
                meshData.normals[baseIndex + 1] = normal;
                meshData.normals[baseIndex + 2] = normal;
                meshData.normals[baseIndex + 3] = normal;

                float wallLength = Vector2.Distance(p0, p1);
                float wallHeight = topY - baseY;

                meshData.uvs[baseIndex + 0] = new Vector2(0f, 0f);
                meshData.uvs[baseIndex + 1] = new Vector2(0f, wallHeight * 0.1f);
                meshData.uvs[baseIndex + 2] = new Vector2(wallLength * 0.1f, wallHeight * 0.1f);
                meshData.uvs[baseIndex + 3] = new Vector2(wallLength * 0.1f, 0f);

                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 1;
                meshData.triangles[t++] = baseIndex + 0;

                meshData.triangles[t++] = baseIndex + 3;
                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 0;
            }

            return meshData;
        }

        private MeshData TriangulateZoneVolume(FlightData flight, List<Vector2> contour, float baseY, float topY)
        {
            Tess tess = new Tess();
            ContourVertex[] tessContour = new ContourVertex[contour.Count];

            for (int i = 0; i < contour.Count; i++)
            {
                tessContour[i].Position = new Vec3(contour[i].x, contour[i].y, 0.0f);
            }

            tess.AddContour(tessContour, ContourOrientation.Clockwise);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            int roofVertexCount = tess.Vertices.Length;
            int roofTriangleCount = tess.ElementCount * 3;
            int wallVertexCount = contour.Count * 4;
            int wallTriangleCount = contour.Count * 6;

            int totalVertexCount = roofVertexCount * 2 + wallVertexCount;
            int totalTriangleCount = roofTriangleCount * 2 + wallTriangleCount;

            MeshData meshData = new MeshData
            {
                vertices = new NativeArray<Vector3>(totalVertexCount, Allocator.Persistent),
                normals = new NativeArray<Vector3>(totalVertexCount, Allocator.Persistent),
                triangles = new NativeArray<int>(totalTriangleCount, Allocator.Persistent),
                uvs = new NativeArray<Vector2>(totalVertexCount, Allocator.Persistent)
            };

            int v = 0;
            int t = 0;
            Vector2 uvScale = new Vector2(0.1f, 0.1f);

            //Top cap
            for (int i = 0; i < roofVertexCount; i++)
            {
                Vec3 vertex = tess.Vertices[i].Position;
                meshData.vertices[v] = new Vector3(vertex.X, topY, vertex.Y);
                meshData.normals[v] = Vector3.up;
                meshData.uvs[v] = new Vector2(vertex.X, vertex.Y) * uvScale;
                v++;
            }
            for (int i = 0; i < tess.ElementCount; i++)
            {
                meshData.triangles[t++] = tess.Elements[i * 3 + 2];
                meshData.triangles[t++] = tess.Elements[i * 3 + 1];
                meshData.triangles[t++] = tess.Elements[i * 3 + 0];
            }

            //Bottom cap
            for (int i = 0; i < roofVertexCount; i++)
            {
                Vec3 vertex = tess.Vertices[i].Position;
                meshData.vertices[v] = new Vector3(vertex.X, baseY, vertex.Y);
                meshData.normals[v] = Vector3.down;
                meshData.uvs[v] = new Vector2(vertex.X, vertex.Y) * uvScale;
                v++;
            }
            for (int i = 0; i < tess.ElementCount; i++)
            {
                meshData.triangles[t++] = tess.Elements[i * 3 + 0] + roofVertexCount;
                meshData.triangles[t++] = tess.Elements[i * 3 + 1] + roofVertexCount;
                meshData.triangles[t++] = tess.Elements[i * 3 + 2] + roofVertexCount;
            }

            //Walls
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p0 = contour[i];
                Vector2 p1 = contour[(i + 1) % contour.Count];

                int baseIndex = v;

                Vector3 v0p = new Vector3(p0.x, baseY, p0.y);
                Vector3 v1p = new Vector3(p0.x, topY, p0.y);
                Vector3 v2p = new Vector3(p1.x, topY, p1.y);
                Vector3 v3p = new Vector3(p1.x, baseY, p1.y);

                meshData.vertices[v++] = v0p;
                meshData.vertices[v++] = v1p;
                meshData.vertices[v++] = v2p;
                meshData.vertices[v++] = v3p;

                Vector3 edge = v2p - v1p;
                Vector3 normal = Vector3.Cross(Vector3.up, edge).normalized;

                meshData.normals[baseIndex + 0] = normal;
                meshData.normals[baseIndex + 1] = normal;
                meshData.normals[baseIndex + 2] = normal;
                meshData.normals[baseIndex + 3] = normal;

                float wallLength = Vector2.Distance(p0, p1);
                float wallHeight = topY - baseY;

                meshData.uvs[baseIndex + 0] = new Vector2(0f, 0f);
                meshData.uvs[baseIndex + 1] = new Vector2(0f, wallHeight * 0.1f);
                meshData.uvs[baseIndex + 2] = new Vector2(wallLength * 0.1f, wallHeight * 0.1f);
                meshData.uvs[baseIndex + 3] = new Vector2(wallLength * 0.1f, 0f);

                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 1;
                meshData.triangles[t++] = baseIndex + 0;

                meshData.triangles[t++] = baseIndex + 3;
                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 0;
            }

            return meshData;
        }

        private GameObject CreateZone(OpenMapTileZone zoneType, MeshData meshData, Vector3 position)
        {
            Mesh mesh = meshData.ConvertToUnityMesh();
            GameObject zone = _openMapTileZonePool.Get();
            MeshFilter meshFilter = zone.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = zone.GetComponent<MeshRenderer>();

            switch (zoneType)
            {
                default:
                case OpenMapTileZone.LandUse:
                    meshRenderer.sharedMaterial = _openMapTileLandUseMaterial;
                    break;
                case OpenMapTileZone.Water:
                    meshRenderer.sharedMaterial = _openMapTileWaterMaterial;
                    break;
                case OpenMapTileZone.LandCover:
                    meshRenderer.sharedMaterial = _openMapTileLandCoverMaterial;
                    break;
                case OpenMapTileZone.Park:
                    meshRenderer.sharedMaterial = _openMapTileParkMaterial;
                    break;
                case OpenMapTileZone.Aeroway:
                    meshRenderer.sharedMaterial = _openMapTileAerowayMaterial;
                    break;
            }

            meshRenderer.enabled = true;
            meshFilter.sharedMesh = mesh;
            zone.transform.SetParent(transform);
            zone.transform.position = position;
            zone.transform.rotation = Quaternion.identity;
            _openMapTileObjects.Add(zone);

            return zone;
        }

        private GameObject CreateBuilding(MeshData meshData, Vector3 position)
        {
            Mesh mesh = meshData.ConvertToUnityMesh();
            GameObject building = _openMapTileZonePool.Get();
            MeshFilter meshFilter = building.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = building.GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            meshFilter.sharedMesh = mesh;
            building.transform.SetParent(transform);
            building.transform.position = position;
            building.transform.rotation = Quaternion.identity;
            _openMapTileObjects.Add(building);

            return building;
        }

        private float EstimateHeightFromFootprint(List<Vector2> contour, FlightData flight)
        {
            if (contour == null || contour.Count < 3)
            {
                return 6f; // Fallback minimal height
            }

            double area = 0.0;
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p1 = contour[i];
                Vector2 p2 = contour[(i + 1) % contour.Count];
                area += (p1.x * p2.y - p2.x * p1.y);
            }
            area = Math.Abs(area) * 0.5;

            float metersPerUnit = flight.GlobalScale;
            if (metersPerUnit <= 0f || float.IsNaN(metersPerUnit))
            {
                metersPerUnit = 1f;
            }

            double areaMeters = area / (metersPerUnit * metersPerUnit);

            float baseHeight;

            if (areaMeters < 80f)
            {
                baseHeight = 4f;
            }
            else if (areaMeters < 300f)
            {
                baseHeight = 8f;
            }
            else if (areaMeters < 2000f)
            {
                baseHeight = 14f;
            }
            else
            {
                baseHeight = 8f;
            }

            float variation = UnityEngine.Random.Range(0.85f, 1.15f);

            return baseHeight * variation * flight.GlobalScale;
        }
        #endregion

        #region CALLBACKS / VISIBILITY
        private void OnBuildingVisibilityChanged(bool buildingVisible)
        {
            DisplayBuildingsFromSettings();
        }

        private void OnFlightEndLoading()
        {
            BuildMergedZones(LoadingManager.Instance.CurrentFlightData);
            DisplayBuildingsFromSettings();
        }

        private void DisplayBuildingsFromSettings()
        {
            bool enabled = SettingsManager.CurrentSettings.BuildingVisibility;
            foreach (GameObject go in _openMapTileObjects)
            {
                MeshRenderer rend = go.GetComponent<MeshRenderer>();

                if (rend != null)
                {
                    rend.enabled = enabled;
                }
            }
        }
        #endregion

        #region UI
        internal void DisplayBuildingsSettings(FuGrid grid)
        {
            bool buildingEnabled = SettingsManager.CurrentSettings.BuildingVisibility;
            grid.EnableNextElements();

            if (grid.Toggle("Show Buildings", ref buildingEnabled))
            {
                SettingsManager.SaveBuildingVisibility(buildingEnabled);
            }
        }
        #endregion
    }
}
