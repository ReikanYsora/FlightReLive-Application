using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using FlightReLive.Core.ProceduralTerrain;
using Fu.Framework;
using LibTessDotNet;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;
using FlightReLive.Core.FlightDefinition;
using System;
using FlightReLive.Core.Loading;

namespace FlightReLive.Core.OpenMapTile
{
    /// <summary>
    /// Manager responsible for creating, displaying and unloading OpenMapTile volumes in the scene.
    /// Uses bilinear mapping between tile corners and extent coordinates to ensure pixel-perfect alignment.
    /// </summary>
    [RequireComponent(typeof(OpenMapTilePool))]
    internal class OpenMapTileManager : MonoBehaviour
    {
        private enum OpenMapTileZone
        {
            LandUse, Water, LandCover, Park, Aeroway
        }

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
            _openMapTileZonePool = GetComponent<OpenMapTilePool>();
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

        #region METHODS
        /// <summary>
        /// Loads all features for a given tile and generates their meshes.
        /// </summary>
        internal void LoadTile(TileDefinition tile, FlightData flight)
        {
            if (tile == null || tile.Features == null || tile.Features.Count == 0)
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                GenerateFeaturesFromVectorTile(tile, flight);
            });
        }

        /// <summary>
        /// Generates meshes from vector tile features.
        /// </summary>
        private void GenerateFeaturesFromVectorTile(TileDefinition tile, FlightData flight)
        {
            List<OpenMapTileFeature> features = tile.Features;
            List<GameObject> createdForTile = new List<GameObject>();

            for (int i = 0; i < features.Count; i++)
            {
                OpenMapTileFeature feature = features[i];

                switch (feature)
                {
                    case BuildingFeature building:
                        GenerateBuilding(building, tile, flight, createdForTile);
                        break;
                    case LanduseFeature landuse:
                        GenerateZone(OpenMapTileZone.LandUse, feature, tile, flight, createdForTile);
                        break;
                    case LandcoverFeature landcover:
                        //GenerateZone(OpenMapTileZone.LandCover, feature, tile, flight, createdForTile);
                        break;
                    case WaterFeature water:
                        GenerateZone(OpenMapTileZone.Water, feature, tile, flight, createdForTile);
                        break;
                    case ParkFeature park:
                        //GenerateZone(OpenMapTileZone.Park, feature, tile, flight, createdForTile);
                        break;
                    case AerowayFeature aeroway:
                        GenerateZone(OpenMapTileZone.Aeroway, feature, tile, flight, createdForTile);
                        break;
                    default:
                        break;
                }
            }

            _tileToOpenMapTileZones[(tile.X, tile.Y)] = createdForTile;

            // Cleanup to free memory after streaming
            tile.Features = null;
        }

        /// <summary>
        /// Generates a debug zone mesh where all vertices are placed at a fixed altitude.
        /// Useful for verifying alignment with terrain tiles.
        /// </summary>
        private void GenerateZone(OpenMapTileZone zoneType, OpenMapTileFeature zone, TileDefinition tile, FlightData flight, List<GameObject> createdForTile)
        {
            for (int j = 0; j < zone.Geometry.Count; j++)
            {
                List<SerializablePoint2D> ringRaw = zone.Geometry[j];
                if (ringRaw == null || ringRaw.Count < 3)
                {
                    continue;
                }

                List<Point2d<int>> ring = new List<Point2d<int>>(ringRaw.Count);
                for (int k = 0; k < ringRaw.Count; k++)
                {
                    ring.Add(ringRaw[k].ToPoint2D());
                }
                if (ring.Count < 3)
                {
                    continue;
                }

                //Clip to tile extent
                ring = ClipRingToExtent(ring, (int)OPENMAPTILE_EXTENT);
                if (ring.Count < 3)
                {
                    continue;
                }

                //Convert raw geometry into Unity world-space contour
                List<Vector2> contourFlat = ConvertGeometryToContour(flight, ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (contourFlat == null || contourFlat.Count == 0)
                {
                    continue;
                }

                //World-space barycenter for positioning
                Vector2 centerFlat = ComputeRingBarycenterWorld(ring, flight, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (float.IsNaN(centerFlat.x) || float.IsNaN(centerFlat.y))
                {
                    Debug.LogWarning($"[OMT] Invalid barycenter for zone in tile {tile.X},{tile.Y}");
                    continue;
                }

                // Fixed altitude placement (no per-vertex sampling)
                float baseY = (_zoneAltitude + _minExtrusion) * flight.GlobalScale;
                float topY = (_zoneAltitude + _maxExtrusion) * flight.GlobalScale;

                Vector3 zonePosition = new Vector3(centerFlat.x, 0f, centerFlat.y);

                // Relative contour in XZ
                List<Vector2> contour2D = new List<Vector2>(contourFlat.Count);
                for (int k = 0; k < contourFlat.Count; k++)
                {
                    Vector2 pt = contourFlat[k];
                    contour2D.Add(new Vector2(pt.x - zonePosition.x, pt.y - zonePosition.z));
                }

                // Build the vertical volume between baseY and topY
                MeshData meshData = TriangulateZoneVolume(flight, contour2D, baseY, topY);
                GameObject zoneGO = CreateZone(zoneType, meshData, zonePosition);
                createdForTile.Add(zoneGO);
            }
        }

        /// <summary>
        /// Generates a single building mesh from its footprint geometry.
        /// </summary>
        private void GenerateBuilding(BuildingFeature building, TileDefinition tile, FlightData flight, List<GameObject> createdForTile)
        {
            for (int j = 0; j < building.Geometry.Count; j++)
            {
                List<SerializablePoint2D> ringRaw = building.Geometry[j];
                if (ringRaw == null || ringRaw.Count < 3)
                {
                    continue;
                }

                List<Point2d<int>> ring = new List<Point2d<int>>(ringRaw.Count);
                for (int k = 0; k < ringRaw.Count; k++)
                {
                    ring.Add(ringRaw[k].ToPoint2D());
                }
                if (ring.Count < 3)
                {
                    continue;
                }

                // Clip to tile extent
                ring = ClipRingToExtent(ring, (int)OPENMAPTILE_EXTENT);
                if (ring.Count < 3)
                {
                    continue;
                }

                // Convert ring to world contour (XZ)
                List<Vector2> contour = ConvertGeometryToContour(flight, ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (contour == null || contour.Count == 0)
                {
                    continue;
                }

                // World center for horizontal placement
                Vector2 center = ComputeRingBarycenterWorld(ring, flight, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                if (float.IsNaN(center.x) || float.IsNaN(center.y))
                {
                    Debug.LogWarning($"[OMT] Invalid building center for tile {tile.X},{tile.Y}");
                    continue;
                }

                // GPS barycenter for sampling terrain altitude
                FlightGPSData barycenterGPS = ComputeRingBarycenterGPS(ring, tile.X, tile.Y, MapTools.ZOOM_LEVEL_OPENTILEMAP);
                float terrainAltitude = flight.GetAltitudeAtPosition(tile, barycenterGPS);

                Vector3 position = new Vector3(center.x, terrainAltitude * flight.GlobalScale, center.y);

                // Height estimation from footprint
                float estimatedHeight = EstimateHeightFromFootprint(contour, flight);
                MeshData meshData = TriangulateAndExtrude(flight, contour, estimatedHeight);

                GameObject buildingGO = CreateBuilding(meshData, position);
                createdForTile.Add(buildingGO);
            }
        }

        /// <summary>
        /// Computes the barycenter of a ring directly in world space using bilinear mapping.
        /// </summary>
        private Vector2 ComputeRingBarycenterWorld(List<Point2d<int>> ring, FlightData flight, int tileX, int tileY, int zoom)
        {
            Vector3 worldNW;
            Vector3 worldNE;
            Vector3 worldSW;
            Vector3 worldSE;
            ComputeTileWorldCorners(flight, tileX, tileY, zoom, out worldNW, out worldNE, out worldSW, out worldSE);

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

        /// <summary>
        /// Converts a ring from OMT space to Unity world space using bilinear interpolation of the tile corners.
        /// Output is in the XZ plane (Unity ground plane).
        /// </summary>
        private List<Vector2> ConvertGeometryToContour(FlightData flight, List<Point2d<int>> ring, int tileX, int tileY, int zoom)
        {
            Vector3 worldNW;
            Vector3 worldNE;
            Vector3 worldSW;
            Vector3 worldSE;
            ComputeTileWorldCorners(flight, tileX, tileY, zoom, out worldNW, out worldNE, out worldSW, out worldSE);

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


        /// <summary>
        /// Computes the 4 Unity world corners of a tile.
        /// </summary>
        private void ComputeTileWorldCorners(FlightData flight, int tileX, int tileY, int zoom,
                                             out Vector3 worldNW, out Vector3 worldNE,
                                             out Vector3 worldSW, out Vector3 worldSE)
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

        /// <summary>
        /// Computes barycenter of a ring in GPS coordinates (degrees), using the same bilinear mapping (no 0.5 shift).
        /// </summary>
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
                // Return something safe; caller should handle if needed
                return new FlightGPSData(0.0, 0.0);
            }

            return new FlightGPSData(sumLat / count, sumLon / count);
        }

        /// <summary>
        /// Clip a polygon to [0..extent] square using the Sutherland–Hodgman algorithm.
        /// Works in integer OMT coordinates without half-pixel shifts.
        /// </summary>
        private static List<Point2d<int>> ClipRingToExtent(List<Point2d<int>> ring, int extent = 4096)
        {
            List<Vector2> input = new List<Vector2>(ring.Count);
            for (int i = 0; i < ring.Count; i++)
            {
                input.Add(new Vector2(ring[i].X, ring[i].Y));
            }

            float minX = 0f;
            float minY = 0f;
            float maxX = extent;
            float maxY = extent;

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

            // Left
            input = ClipAgainst(input, p => p.x >= minX, (a, b) =>
            {
                float denom = (b.x - a.x);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (minX - a.x) / denom;
                return new Vector2(minX, a.y + t * (b.y - a.y));
            });

            // Right
            input = ClipAgainst(input, p => p.x <= maxX, (a, b) =>
            {
                float denom = (b.x - a.x);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (maxX - a.x) / denom;
                return new Vector2(maxX, a.y + t * (b.y - a.y));
            });

            // Bottom
            input = ClipAgainst(input, p => p.y >= minY, (a, b) =>
            {
                float denom = (b.y - a.y);
                float t = Mathf.Approximately(denom, 0f) ? 0f : (minY - a.y) / denom;
                return new Vector2(a.x + t * (b.x - a.x), minY);
            });

            // Top
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

        /// <summary>
        /// Convert tile Y index to latitude in degrees (Web Mercator inverse).
        /// </summary>
        private double TileYToLat(int tileY, int zoom)
        {
            double n = Math.PI - 2.0 * Math.PI * tileY / (1 << zoom);
            return Math.Atan(Math.Sinh(n)) * (180.0 / Math.PI);
        }

        /// <summary>
        /// Extrudes a 2D contour into a 3D mesh (roof + walls). Top is at local Y=topY, base is at -BOTTOM_EXTRUSION*scale.
        /// </summary>
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

            // Roof (top cap)
            Vector2 uvScale = new Vector2(0.1f, 0.1f);
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
                int index0 = tess.Elements[i * 3 + 0];
                int index1 = tess.Elements[i * 3 + 1];
                int index2 = tess.Elements[i * 3 + 2];

                meshData.triangles[t++] = index2;
                meshData.triangles[t++] = index1;
                meshData.triangles[t++] = index0;
            }

            // Walls (sides)
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
                float uvScaleX = 0.1f;
                float uvScaleY = 0.1f;

                meshData.uvs[baseIndex + 0] = new Vector2(0f, 0f);
                meshData.uvs[baseIndex + 1] = new Vector2(0f, wallHeight * uvScaleY);
                meshData.uvs[baseIndex + 2] = new Vector2(wallLength * uvScaleX, wallHeight * uvScaleY);
                meshData.uvs[baseIndex + 3] = new Vector2(wallLength * uvScaleX, 0f);

                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 1;
                meshData.triangles[t++] = baseIndex + 0;

                meshData.triangles[t++] = baseIndex + 3;
                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 0;
            }

            return meshData;
        }

        /// <summary>
        /// Extrudes a 2D contour into a vertical volume between baseY and topY.
        /// Used for zones (landuse, water, etc.).
        /// </summary>
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

            // Top cap
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

            // Bottom cap
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

            // Walls
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
                float uvScaleX = 0.1f;
                float uvScaleY = 0.1f;

                meshData.uvs[baseIndex + 0] = new Vector2(0f, 0f);
                meshData.uvs[baseIndex + 1] = new Vector2(0f, wallHeight * uvScaleY);
                meshData.uvs[baseIndex + 2] = new Vector2(wallLength * uvScaleX, wallHeight * uvScaleY);
                meshData.uvs[baseIndex + 3] = new Vector2(wallLength * uvScaleX, 0f);

                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 1;
                meshData.triangles[t++] = baseIndex + 0;

                meshData.triangles[t++] = baseIndex + 3;
                meshData.triangles[t++] = baseIndex + 2;
                meshData.triangles[t++] = baseIndex + 0;
            }

            return meshData;
        }


        /// <summary>
        /// Instantiates a zone GameObject from a mesh.
        /// </summary>
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

        /// <summary>
        /// Instantiates a building GameObject from a mesh.
        /// </summary>
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

        /// <summary>
        /// Estimate building height from its footprint area.
        /// </summary>
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

        /// <summary>
        /// Clears all loaded features.
        /// </summary>
        internal void Unload()
        {
            for (int i = 0; i < _openMapTileObjects.Count; i++)
            {
                _openMapTileZonePool.Return(_openMapTileObjects[i]);
            }

            _openMapTileObjects.Clear();
            _tileToOpenMapTileZones.Clear();
        }

        /// <summary>
        /// Applies visibility from settings to all spawned renderers.
        /// </summary>
        private void DisplayBuildingsFromSettings()
        {
            bool enabled = SettingsManager.CurrentSettings.BuildingVisibility;

            for (int i = 0; i < _openMapTileObjects.Count; i++)
            {
                MeshRenderer tempBuildingRenderer = _openMapTileObjects[i].GetComponent<MeshRenderer>();
                if (tempBuildingRenderer != null)
                {
                    tempBuildingRenderer.enabled = enabled;
                }
            }
        }
        #endregion

        #region CALLBACKS
        private void OnBuildingVisibilityChanged(bool buildingVisible)
        {
            DisplayBuildingsFromSettings();
        }

        private void OnFlightEndLoading()
        {
            DisplayBuildingsFromSettings();
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
