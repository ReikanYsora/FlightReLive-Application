using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using Fu.Framework;
using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;
using Clipper2Lib;

namespace FlightReLive.Core.OpenMapTile
{
    /// <summary>
    /// Manager responsible for collecting OpenMapTile features (buildings + zones).
    /// Buildings are extruded and displayed immediately.
    /// Zones are collected, then fused and extruded on demand via GenerateZones().
    /// </summary>
    [RequireComponent(typeof(OpenMapTilePool))]
    internal class OpenMapTileManager : MonoBehaviour
    {
        internal enum OpenMapTileZone { LandUse, Water, LandCover, Park, Aeroway }

        #region CONSTANTS
        private const float OPENMAPTILE_EXTENT = 4096f;
        private const float BOTTOM_EXTRUSION = 1f;
        private const double CLIP_SCALE = 1000.0; // scale factor for Clipper2 (int coordinates)
        #endregion

        #region ATTRIBUTES
        [Header("Zone settings")]
        [Tooltip("Constant extrusion height (in world units) for all zones except buildings.")]
        [SerializeField] private float _zoneExtrusionHeight = 2f;

        [Header("Materials")]
        [SerializeField] private Material _buildingMaterial;
        [SerializeField] private Material _landUseMaterial;
        [SerializeField] private Material _landCoverMaterial;
        [SerializeField] private Material _waterMaterial;
        [SerializeField] private Material _parkMaterial;
        [SerializeField] private Material _aerowayMaterial;

        private OpenMapTilePool _openMapTileZonePool;
        private readonly List<GameObject> _openMapTileObjects = new List<GameObject>();
        private readonly Dictionary<(int, int), List<GameObject>> _tileToOpenMapTileZones =
            new Dictionary<(int, int), List<GameObject>>();
        private readonly Dictionary<OpenMapTileZone, Dictionary<OpenMapTileFeature, List<List<Vector2>>>> _zoneContours =
            new Dictionary<OpenMapTileZone, Dictionary<OpenMapTileFeature, List<List<Vector2>>>>();
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
                _zoneContours[z] = new Dictionary<OpenMapTileFeature, List<List<Vector2>>>();
            }
        }

        private void Start()
        {
            SettingsManager.OnBuildingVisibilityChanged += OnBuildingVisibilityChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnBuildingVisibilityChanged -= OnBuildingVisibilityChanged;
        }
        #endregion

        #region LOAD / UNLOAD
        internal void LoadTile(TileDefinition tile, FlightData flight)
        {
            if (tile == null || tile.Features == null || tile.Features.Count == 0)
                return;

            AccumulateFeatures(tile, flight);
        }

        internal void Load(FlightData data)
        {
            DisplayBuildingsFromSettings();
            GenerateZones(data);
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

        #region ACCUMULATION
        /// <summary>
        /// Accumulates features. Buildings are created immediately.
        /// Zones are only stored (contours).
        /// </summary>
        private void AccumulateFeatures(TileDefinition tile, FlightData flight)
        {
            List<GameObject> createdForTile = new List<GameObject>();

            foreach (OpenMapTileFeature feature in tile.Features)
            {
                if (feature is BuildingFeature building)
                {
                    UnityMainThreadDispatcher.AddActionInMainThread(() =>
                    {
                        GenerateBuilding(building, tile, flight, createdForTile);
                    });
                }
                else
                {
                    List<List<SerializablePoint2D>> geometries = feature.Geometry;
                    if (geometries == null) continue;

                    foreach (List<SerializablePoint2D> ringRaw in geometries)
                    {
                        if (ringRaw == null || ringRaw.Count < 3) continue;

                        List<Point2d<int>> ring = new(ringRaw.Count);
                        foreach (SerializablePoint2D pt in ringRaw) ring.Add(pt.ToPoint2D());

                        ring = ClipRingToExtent(ring);
                        if (ring.Count < 3) continue;

                        List<Vector2> contour = ConvertGeometryToContour(
                            flight, ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);

                        if (contour == null || contour.Count < 3) continue;

                        OpenMapTileZone? zoneType = null;
                        if (feature is LanduseFeature) zoneType = OpenMapTileZone.LandUse;
                        else if (feature is WaterFeature) zoneType = OpenMapTileZone.Water;
                        else if (feature is LandcoverFeature) zoneType = OpenMapTileZone.LandCover;
                        else if (feature is ParkFeature) zoneType = OpenMapTileZone.Park;
                        else if (feature is AerowayFeature) zoneType = OpenMapTileZone.Aeroway;

                        if (zoneType.HasValue)
                        {
                            if (!_zoneContours[zoneType.Value].ContainsKey(feature))
                            {
                                _zoneContours[zoneType.Value][feature] = new List<List<Vector2>>();
                            }
                            _zoneContours[zoneType.Value][feature].Add(contour);
                        }
                    }
                }
            }

            _tileToOpenMapTileZones[(tile.X, tile.Y)] = createdForTile;
            tile.Features = null;
        }
        #endregion

        #region GENERATION
        /// <summary>
        /// Generates extruded meshes for all accumulated zones (after all tiles are loaded).
        /// </summary>
        internal void GenerateZones(FlightData flight)
        {
            foreach (var kvp in _zoneContours)
            {
                OpenMapTileZone zoneType = kvp.Key;
                Material mat = GetMaterialForZone(zoneType);
                if (mat == null) continue;

                // Collect all contours for this zone type
                List<List<Vector2>> allContours = new List<List<Vector2>>();
                foreach (var featureContours in kvp.Value.Values)
                    allContours.AddRange(featureContours);

                if (allContours.Count == 0) continue;

                // Convert to Clipper paths
                Paths64 clipperPaths = new Paths64();
                foreach (var contour in allContours)
                {
                    Path64 path = new Path64(contour.Count);
                    foreach (var v in contour)
                    {
                        path.Add(new Point64(
                            (long)(v.x * CLIP_SCALE),
                            (long)(v.y * CLIP_SCALE)
                        ));
                    }
                    clipperPaths.Add(path);
                }

                // Fusion via Clipper2
                Paths64 solution = Clipper.Union(clipperPaths, FillRule.NonZero);

                foreach (var poly in solution)
                {
                    List<Vector2> contour = poly
                        .Select(p => new Vector2((float)(p.X / CLIP_SCALE), (float)(p.Y / CLIP_SCALE)))
                        .ToList();

                    if (contour.Count < 3) continue;

                    // Barycentre world
                    float cx = contour.Average(v => v.x);
                    float cz = contour.Average(v => v.y);

                    // Altitude via GPS conversion
                    FlightGPSData gps = flight.ConvertWorldToGPSPosition(new Vector3(cx, 0, cz));
                    float alt = flight.GetAltitudeAtPosition(gps) * flight.GlobalScale;
                    Vector3 pos = new Vector3(cx, alt, cz);

                    // Extrusion
                    MeshData meshData = TriangulateAndExtrude(flight, contour, _zoneExtrusionHeight * flight.GlobalScale);
                    GameObject zoneGO = CreateMeshGO(meshData, pos, mat);
                    _openMapTileObjects.Add(zoneGO);
                }
            }
        }


        private Material GetMaterialForZone(OpenMapTileZone zoneType)
        {
            return zoneType switch
            {
                OpenMapTileZone.LandUse => _landUseMaterial,
                OpenMapTileZone.LandCover => _landCoverMaterial,
                OpenMapTileZone.Water => _waterMaterial,
                OpenMapTileZone.Park => _parkMaterial,
                OpenMapTileZone.Aeroway => _aerowayMaterial,
                _ => null
            };
        }
        #endregion

        #region BUILDINGS
        private void GenerateBuilding(BuildingFeature building, TileDefinition tile, FlightData flight, List<GameObject> createdForTile)
        {
            foreach (List<SerializablePoint2D> ringRaw in building.Geometry)
            {
                if (ringRaw == null || ringRaw.Count < 3) continue;

                List<Point2d<int>> ring = new(ringRaw.Count);
                foreach (SerializablePoint2D pt in ringRaw) ring.Add(pt.ToPoint2D());

                ring = ClipRingToExtent(ring);
                if (ring.Count < 3) continue;

                List<Vector2> contour = ConvertGeometryToContour(flight, ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (contour == null || contour.Count == 0) continue;

                Vector2 center = ComputeRingBarycenterWorld(ring, flight, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (float.IsNaN(center.x) || float.IsNaN(center.y)) continue;

                FlightGPSData baryGPS = ComputeRingBarycenterGPS(ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                float terrainAltitude = flight.GetAltitudeAtPosition(tile, baryGPS);

                Vector3 position = new(center.x, terrainAltitude * flight.GlobalScale, center.y);
                float estimatedHeight = EstimateHeightFromFootprint(contour, flight);

                MeshData meshData = TriangulateAndExtrude(flight, contour, estimatedHeight);
                GameObject buildingGO = CreateMeshGO(meshData, position, _buildingMaterial);
                createdForTile.Add(buildingGO);
            }
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

        private void ComputeTileWorldCorners(FlightData flight, int tileX, int tileY, int zoom,
            out Vector3 worldNW, out Vector3 worldNE, out Vector3 worldSW, out Vector3 worldSE)
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

            float sumX = 0f, sumZ = 0f;
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

            return count == 0 ? new Vector2(float.NaN, float.NaN) : new Vector2(sumX / count, sumZ / count);
        }

        private FlightGPSData ComputeRingBarycenterGPS(List<Point2d<int>> ring, int tileX, int tileY, int zoom)
        {
            double lonW = (double)tileX / (1 << zoom) * 360.0 - 180.0;
            double lonE = (double)(tileX + 1) / (1 << zoom) * 360.0 - 180.0;
            double latN = TileYToLat(tileY, zoom);
            double latS = TileYToLat(tileY + 1, zoom);

            double sumLat = 0.0, sumLon = 0.0;
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

            return count == 0 ? new FlightGPSData(0.0, 0.0) : new FlightGPSData(sumLat / count, sumLon / count);
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

        private float EstimateHeightFromFootprint(List<Vector2> contour, FlightData flight)
        {
            if (contour == null || contour.Count < 3)
            {
                return 6f;
            }

            double area = 0.0;

            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p1 = contour[i], p2 = contour[(i + 1) % contour.Count];
                area += (p1.x * p2.y - p2.x * p1.y);
            }
            area = Math.Abs(area) * 0.5;

            float metersPerUnit = flight.GlobalScale;
            double areaMeters = area / (metersPerUnit * metersPerUnit);

            float baseHeight = 8f;

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

            float variation = UnityEngine.Random.Range(0.85f, 1.15f);
            return baseHeight * variation * flight.GlobalScale;
        }
        #endregion

        #region MESH HELPERS
        private MeshData TriangulateAndExtrude(FlightData flight, List<Vector2> contour, float topY)
        {
            float baseY = -BOTTOM_EXTRUSION * flight.GlobalScale;
            Tess tess = new Tess();
            ContourVertex[] tessContour = new ContourVertex[contour.Count];

            for (int i = 0; i < contour.Count; i++)
                tessContour[i].Position = new Vec3(contour[i].x, contour[i].y, 0.0f);

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

            int v = 0, t = 0;
            Vector2 uvScale = new Vector2(0.1f, 0.1f);

            // Roof
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

            // Walls
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p0 = contour[i], p1 = contour[(i + 1) % contour.Count];
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

        private GameObject CreateMeshGO(MeshData meshData, Vector3 position, Material mat)
        {
            Mesh mesh = meshData.ConvertToUnityMesh();
            GameObject go = _openMapTileZonePool.Get();
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = mat;
            meshRenderer.enabled = true;
            meshFilter.sharedMesh = mesh;
            go.transform.SetParent(transform);
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            _openMapTileObjects.Add(go);
            return go;
        }
        #endregion

        #region CALLBACKS + UI
        private void DisplayBuildingsFromSettings()
        {
            bool enabled = SettingsManager.CurrentSettings.BuildingVisibility;
            foreach (GameObject go in _openMapTileObjects)
            {
                MeshRenderer rend = go.GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = enabled;
            }
        }

        private void OnBuildingVisibilityChanged(bool buildingVisible)
        {
            DisplayBuildingsFromSettings();
        }

        internal void DisplayBuildingsSettings(FuGrid grid)
        {
            bool buildingEnabled = SettingsManager.CurrentSettings.BuildingVisibility;
            grid.EnableNextElements();
            if (grid.Toggle("Display buildings", ref buildingEnabled))
            {
                SettingsManager.SaveBuildingVisibility(buildingEnabled);
            }
        }
        #endregion
    }
}
