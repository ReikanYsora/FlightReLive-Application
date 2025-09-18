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

namespace FlightReLive.Core.Building
{
    /// <summary>
    /// Manager responsible for creating, displaying and unloading buildings in the scene.
    /// Preserves original triangulation and placement logic, with per-tile streaming support.
    /// </summary>
    [RequireComponent(typeof(BuildingPool))]
    internal class BuildingManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float MIN_BUILDING_HEIGHT = 2.5f;
        private const float MAX_BUILDING_HEIGHT = 4f;
        private const float BOTTOM_EXTRUSION = 1f;
        #endregion

        #region ATTRIBUTES
        [SerializeField] private Material _buildingMaterial;
        private BuildingPool _buildingPool;
        private readonly List<GameObject> _buildings = new List<GameObject>();
        private readonly Dictionary<(int, int), List<GameObject>> _tileToBuildings = new Dictionary<(int, int), List<GameObject>>();
        #endregion

        #region PROPERTIES
        public static BuildingManager Instance { get; private set; }
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
            _buildingPool = GetComponent<BuildingPool>();
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

        #region METHODS

        /// <summary>
        /// Builds all buildings for a single tile.
        /// </summary>
        internal void LoadTile(TileDefinition tile, FlightData flight)
        {
            if (tile == null || tile.Buildings == null || tile.Buildings.Count == 0)
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                GenerateBuildingsFromVectorTile(tile, flight);
            });
        }

        /// <summary>
        /// Clears all buildings currently loaded.
        /// </summary>
        internal void Unload()
        {
            foreach (GameObject building in _buildings)
            {
                _buildingPool.Return(building);
            }

            _buildings.Clear();
            _tileToBuildings.Clear();
        }

        /// <summary>
        /// Unloads only the buildings from a specific tile.
        /// </summary>
        internal void UnloadTile(TileDefinition tile)
        {
            (int, int) key = (tile.X, tile.Y);

            if (_tileToBuildings.TryGetValue(key, out List<GameObject> buildings))
            {
                foreach (GameObject building in buildings)
                {
                    _buildingPool.Return(building);
                    _buildings.Remove(building);
                }

                _tileToBuildings.Remove(key);
            }
        }

        /// <summary>
        /// Generates all buildings for the given tile using LibTessDotNet triangulation.
        /// Preserves original extrusion and triangle winding order.
        /// </summary>
        private void GenerateBuildingsFromVectorTile(TileDefinition tile, FlightData flight)
        {
            List<BuildingData> buildings = tile.Buildings;
            List<GameObject> createdForTile = new List<GameObject>();

            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingData building = buildings[i];

                for (int j = 0; j < building.Geometry.Count; j++)
                {
                    List<SerializablePoint2D> ringRaw = building.Geometry[j];

                    if (ringRaw.Count < 3)
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

                    List<Vector2> contour = ConvertGeometryToContour(flight, ring, tile.X, tile.Y);

                    // Compute barycenter (2D world-space)
                    Vector2 center = Vector2.zero;
                    for (int k = 0; k < contour.Count; k++)
                    {
                        center += contour[k];
                    }
                    center /= contour.Count;

                    // Altitude at barycenter (GPS space)
                    FlightGPSData barycenterGPS = ComputeRingBarycenterGPS(ring, tile.X, tile.Y);
                    float terrainAltitude = flight.GetAltitudeAtPosition(tile, barycenterGPS);

                    Vector3 position = new Vector3(center.x, terrainAltitude * flight.GlobalScale, center.y);
                    MeshData meshData = TriangulateAndExtrude(flight, contour, building.Height);

                    GameObject buildingGO = CreateBuilding(meshData, position);
                    createdForTile.Add(buildingGO);
                }
            }

            _tileToBuildings[(tile.X, tile.Y)] = createdForTile;

            //Cleanup
            tile.Buildings = null;
            GC.Collect();
        }

        /// <summary>
        /// Converts a raw geometry ring into a contour in Unity world space.
        /// </summary>
        private List<Vector2> ConvertGeometryToContour(FlightData flight, List<Point2d<int>> ring, int tileX, int tileY)
        {
            List<Vector2> contour = new List<Vector2>();
            const ulong extent = 4096;

            GPSBoundingBox bbox = MapTools.GetBoundingBoxFromTileXY(tileX, tileY);

            for (int i = 0; i < ring.Count; i++)
            {
                Point2d<int> point = ring[i];

                float normalizedX = point.X / (float)extent;
                float normalizedY = point.Y / (float)extent;

                double lat = bbox.MaxLatitude - normalizedY * (bbox.MaxLatitude - bbox.MinLatitude);
                double lon = bbox.MinLongitude + normalizedX * (bbox.MaxLongitude - bbox.MinLongitude);

                Vector3 gps = new Vector3((float)lat, 0f, (float)lon);
                Vector3 worldPos = flight.ConvertGPSPositionToWorld(gps);

                contour.Add(new Vector2(worldPos.x, worldPos.z));
            }

            return contour;
        }

        /// <summary>
        /// Computes barycenter of a ring in GPS coordinates.
        /// </summary>
        private FlightGPSData ComputeRingBarycenterGPS(List<Point2d<int>> ring, int tileX, int tileY)
        {
            const ulong extent = 4096;
            GPSBoundingBox bbox = MapTools.GetBoundingBoxFromTileXY(tileX, tileY);

            double sumLat = 0.0;
            double sumLon = 0.0;

            for (int i = 0; i < ring.Count; i++)
            {
                Point2d<int> point = ring[i];

                float normalizedX = point.X / (float)extent;
                float normalizedY = point.Y / (float)extent;

                double lat = bbox.MaxLatitude - normalizedY * (bbox.MaxLatitude - bbox.MinLatitude);
                double lon = bbox.MinLongitude + normalizedX * (bbox.MaxLongitude - bbox.MinLongitude);

                sumLat += lat;
                sumLon += lon;
            }

            double avgLat = sumLat / ring.Count;
            double avgLon = sumLon / ring.Count;

            return new FlightGPSData(avgLat, avgLon);
        }

        /// <summary>
        /// Extrudes a 2D contour into a 3D building mesh (roof + walls).
        /// Preserves original winding order.
        /// </summary>
        private MeshData TriangulateAndExtrude(FlightData flight, List<Vector2> contour, float buildingHeight)
        {
            float baseY = -BOTTOM_EXTRUSION * flight.GlobalScale;
            float topY = Mathf.Clamp(buildingHeight, MIN_BUILDING_HEIGHT, MAX_BUILDING_HEIGHT) * flight.GlobalScale;

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

            // Roof vertices
            for (int i = 0; i < roofVertexCount; i++)
            {
                Vec3 vertex = tess.Vertices[i].Position;
                meshData.vertices[v] = new Vector3(vertex.X, topY, vertex.Y);
                meshData.normals[v] = Vector3.up;
                meshData.uvs[v] = Vector2.zero;
                v++;
            }

            // Roof triangles (keep original winding order)
            for (int i = 0; i < tess.ElementCount; i++)
            {
                int index0 = tess.Elements[i * 3 + 0];
                int index1 = tess.Elements[i * 3 + 1];
                int index2 = tess.Elements[i * 3 + 2];

                meshData.triangles[t++] = index2;
                meshData.triangles[t++] = index1;
                meshData.triangles[t++] = index0;
            }

            // Walls
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p0 = contour[i];
                Vector2 p1 = contour[(i + 1) % contour.Count];

                int baseIndex = v;

                Vector3 v0 = new Vector3(p0.x, baseY, p0.y);
                Vector3 v1 = new Vector3(p0.x, topY, p0.y);
                Vector3 v2 = new Vector3(p1.x, topY, p1.y);
                Vector3 v3 = new Vector3(p1.x, baseY, p1.y);

                meshData.vertices[v++] = v0;
                meshData.vertices[v++] = v1;
                meshData.vertices[v++] = v2;
                meshData.vertices[v++] = v3;

                Vector3 normal = Vector3.Cross(v2 - v1, v0 - v1).normalized;

                meshData.normals[baseIndex + 0] = normal;
                meshData.normals[baseIndex + 1] = normal;
                meshData.normals[baseIndex + 2] = normal;
                meshData.normals[baseIndex + 3] = normal;

                meshData.uvs[baseIndex + 0] = Vector2.zero;
                meshData.uvs[baseIndex + 1] = Vector2.zero;
                meshData.uvs[baseIndex + 2] = Vector2.zero;
                meshData.uvs[baseIndex + 3] = Vector2.zero;

                // Keep original triangle winding
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
        /// Instantiates a building GameObject from a mesh.
        /// </summary>
        private GameObject CreateBuilding(MeshData meshData, Vector3 position)
        {
            Mesh mesh = meshData.ConvertToUnityMesh();
            GameObject building = _buildingPool.Get();

            MeshFilter meshFilter = building.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = building.GetComponent<MeshRenderer>();

            meshRenderer.enabled = SettingsManager.CurrentSettings.BuildingVisibility;
            meshFilter.sharedMesh = mesh;

            building.transform.SetParent(transform);
            building.transform.position = position;
            building.transform.rotation = Quaternion.identity;

            _buildings.Add(building);
            return building;
        }
        #endregion

        #region CALLBACKS
        private void OnBuildingVisibilityChanged(bool buildingVisible)
        {
            foreach (GameObject go in _buildings)
            {
                go.GetComponent<MeshRenderer>().enabled = buildingVisible;
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
