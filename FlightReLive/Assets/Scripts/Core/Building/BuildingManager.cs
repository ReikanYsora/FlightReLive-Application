using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using Fu.Framework;
using LibTessDotNet;
using System.Collections.Generic;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core.Building
{
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
        private List<GameObject> _buildings = new List<GameObject>();
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

            _buildings = new List<GameObject>();
            _buildingPool = GetComponent<BuildingPool>();
        }

        private void Start()
        {
            TerrainManager.Instance.OnTerrainLoaded += OnTerrainLoaded;
            SettingsManager.OnBuildingVisibilityChanged += OnBuildingVisibilityChanged;
        }

        private void OnDestroy()
        {
            TerrainManager.Instance.OnTerrainLoaded -= OnTerrainLoaded;
            SettingsManager.OnBuildingVisibilityChanged -= OnBuildingVisibilityChanged;
        }
        #endregion

        #region METHODS
        private void LoadFlightBuildings(FlightData flighData)
        {
            if (flighData == null)
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                foreach (TileDefinition tile in flighData.MapDefinition.TileDefinitions)
                {
                    GenerateBuildingsFromVectorTile(tile, flighData);
                }
            });
        }

        internal void UnloadFLightBuildings()
        {
            foreach (GameObject building in _buildings)
            {
                _buildingPool.Return(building);
            }

            _buildings.Clear();
        }

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
                Vector3 worldPos = TerrainManager.Instance.ConvertGPSPositionToWorld(flight, gps);

                contour.Add(new Vector2(worldPos.x, worldPos.z));
            }

            return contour;
        }

        private void GenerateBuildingsFromVectorTile(TileDefinition tile, FlightData flight)
        {
            List<BuildingData> buildings = tile.Buildings;

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

                    Vector2 center = Vector2.zero;
                    for (int k = 0; k < contour.Count; k++)
                    {
                        center += contour[k];
                    }
                    center /= contour.Count;

                    FlightGPSData barycenterGPS = ComputeRingBarycenterGPS(ring, tile.X, tile.Y);
                    float terrainAltitude = TerrainManager.Instance.GetAltitudeAtPosition(flight, barycenterGPS);
                    Vector3 position = new Vector3(center.x, terrainAltitude * TerrainManager.Instance.GlobalScale, center.y);
                    MeshData meshData = TriangulateAndExtrude(contour, building.Height);
                    CreateBuilding(meshData, position);
                }
            }
        }

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


        private MeshData TriangulateAndExtrude(List<Vector2> contour, float buildingHeight)
        {
            MeshData meshData = new MeshData();
            float baseY = -BOTTOM_EXTRUSION * TerrainManager.Instance.GlobalScale;
            float topY = Random.Range(MIN_BUILDING_HEIGHT, MAX_BUILDING_HEIGHT) * TerrainManager.Instance.GlobalScale;
            Tess tess = new Tess();
            ContourVertex[] tessContour = new ContourVertex[contour.Count];

            for (int i = 0; i < contour.Count; i++)
            {
                tessContour[i].Position = new Vec3(contour[i].x, contour[i].y, 0.0f);
            }

            tess.AddContour(tessContour, ContourOrientation.Clockwise);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            for (int i = 0; i < tess.Vertices.Length; i++)
            {
                Vec3 vertex = tess.Vertices[i].Position;
                meshData.vertices.Add(new Vector3(vertex.X, topY, vertex.Y));
                meshData.normals.Add(Vector3.up);
            }

            for (int i = 0; i < tess.ElementCount; i++)
            {
                int index2 = tess.Elements[i * 3 + 2];
                int index1 = tess.Elements[i * 3 + 1];
                int index0 = tess.Elements[i * 3 + 0];

                meshData.triangles.Add(index2);
                meshData.triangles.Add(index1);
                meshData.triangles.Add(index0);

            }

            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p0 = contour[i];
                Vector2 p1 = contour[(i + 1) % contour.Count];

                int baseIndex = meshData.vertices.Count;

                Vector3 v0 = new Vector3(p0.x, baseY, p0.y);
                Vector3 v1 = new Vector3(p0.x, topY, p0.y);
                Vector3 v2 = new Vector3(p1.x, topY, p1.y);
                Vector3 v3 = new Vector3(p1.x, baseY, p1.y);

                meshData.vertices.Add(v0);
                meshData.vertices.Add(v1);
                meshData.vertices.Add(v2);
                meshData.vertices.Add(v3);

                Vector3 normal = Vector3.Cross(v2 - v1, v0 - v1).normalized;

                meshData.normals.Add(normal);
                meshData.normals.Add(normal);
                meshData.normals.Add(normal);
                meshData.normals.Add(normal);

                meshData.triangles.Add(baseIndex + 2);
                meshData.triangles.Add(baseIndex + 1);
                meshData.triangles.Add(baseIndex + 0);

                meshData.triangles.Add(baseIndex + 3);
                meshData.triangles.Add(baseIndex + 2);
                meshData.triangles.Add(baseIndex + 0);
            }

            return meshData;
        }

        private void CreateBuilding(MeshData meshData, Vector3 position)
        {
            Mesh mesh = meshData.ConvertToUnityMesh(MeshType.Triangles);
            GameObject building = _buildingPool.Get();

            MeshFilter meshFilter = building.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = building.GetComponent<MeshRenderer>();

            meshRenderer.enabled = SettingsManager.CurrentSettings.BuildingVisibility;
            meshFilter.sharedMesh = mesh;

            building.transform.position = position;
            building.transform.rotation = Quaternion.identity;

            _buildings.Add(building);
        }
        #endregion

        #region CALLBACKS
        private void OnBuildingVisibilityChanged(bool buildingVisible)
        {
            _buildings.ForEach(x => x.GetComponent<MeshRenderer>().enabled = buildingVisible);
        }

        private void OnTerrainLoaded(FlightData flightData)
        {
            LoadFlightBuildings(flightData);
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
