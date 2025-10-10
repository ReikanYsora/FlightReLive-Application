using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using Fu.Framework;
using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core.OpenVectorTile
{
    public class BuildingManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float BOTTOM_EXTRUSION = 1f;
        private const float OPENMAPTILE_EXTENT = 4096f;
        #endregion

        #region ATTRIBUTES
        [Header("Materials")]
        [SerializeField] private Material _buildingMaterial;
        private Material _buildingMaterialInstance;
        private CombinedMeshBuilder _combinedBuilder;
        private GameObject _combinedBuildings;
        private bool _isBaked;
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

            _combinedBuilder = new CombinedMeshBuilder();
            _buildingMaterialInstance = new Material(_buildingMaterial);
        }

        private void Start()
        {
            SettingsManager.OnBuildingVisibilityChanged += OnBuildingVisibilityChanged;
            SettingsManager.OnBuildingColorChanged += OnBuildingColorChanged;
            SettingsManager.OnBuildingAOChanged += OnBuildingAOChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnBuildingVisibilityChanged -= OnBuildingVisibilityChanged;
            SettingsManager.OnBuildingColorChanged -= OnBuildingColorChanged;
            SettingsManager.OnBuildingAOChanged -= OnBuildingAOChanged;
        }
        #endregion

        #region METHODS
        internal void LoadTile(TileDefinition tile, FlightData flight)
        {
            if (tile.Buildings != null)
            {
                foreach (BuildingFeature building in tile.Buildings)
                {
                    UnityMainThreadDispatcher.AddActionInMainThread(() =>
                    {
                        GenerateBuilding(building, tile, flight);
                    });
                }
            }
        }

        internal void Load(FlightData flight)
        {
            bool displayBuilding = SettingsManager.CurrentSettings.BuildingVisibility;

            if (_isBaked)
            {
                if (_combinedBuildings != null)
                {
                    MeshRenderer mr = _combinedBuildings.GetComponent<MeshRenderer>();

                    if (mr != null)
                    {
                        mr.enabled = displayBuilding;
                    }
                }

                return;
            }

            //Bake buildings meshes
            if (_combinedBuilder.HasData)
            {
                Mesh combined = _combinedBuilder.ToMesh();

                _combinedBuildings = new GameObject("Buildings (Combined)");
                _combinedBuildings.transform.SetParent(transform, worldPositionStays: true);
                _combinedBuildings.transform.position = Vector3.zero;
                _combinedBuildings.transform.rotation = Quaternion.identity;

                MeshFilter mf = _combinedBuildings.AddComponent<MeshFilter>();
                MeshRenderer mr = _combinedBuildings.AddComponent<MeshRenderer>();
                mf.sharedMesh = combined;
                mr.sharedMaterial = _buildingMaterialInstance;
                mr.enabled = displayBuilding;
                _isBaked = true;
                _combinedBuilder.Clear();
            }
        }

        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                _combinedBuilder.Clear();
                _isBaked = false;

                if (_combinedBuildings != null)
                {
                    Destroy(_combinedBuildings);
                }
            });
        }

        private void GenerateBuilding(BuildingFeature building, TileDefinition tile, FlightData flight)
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
                Mesh unityMesh = meshData.ConvertToUnityMesh();
                _combinedBuilder.AddUnityMesh(unityMesh, position);
                Destroy(unityMesh);
            }
        }

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

            int v = 0, t = 0;
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


        #region CALLBACKS
        private void DisplayBuildingsFromSettings()
        {
            bool enabled = SettingsManager.CurrentSettings.BuildingVisibility;

            if (_combinedBuildings != null)
            {
                var rend = _combinedBuildings.GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = enabled;
            }
        }

        private void OnBuildingVisibilityChanged(bool buildingVisible)
        {
            DisplayBuildingsFromSettings();
        }

        private void OnBuildingAOChanged(float ao)
        {
            float ambientOcclusion = SettingsManager.CurrentSettings.BuildingAO;
            _buildingMaterialInstance.SetFloat("_AmbientOcclusion", ambientOcclusion);
        }

        private void OnBuildingColorChanged(Color color)
        {
            Color buildingColor = SettingsManager.CurrentSettings.BuildingColor;
            _buildingMaterialInstance.SetColor("_Color", buildingColor);
        }
        #endregion

        #region UI
        internal void DisplayBuildingsSettings()
        {
            using (FuGrid grid = new FuGrid("gridBuildingsSettings", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                if (_combinedBuildings == null)
                {
                    grid.DisableNextElements();
                }

                bool buildingEnabled = SettingsManager.CurrentSettings.BuildingVisibility;

                //Display buildings settings
                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Display buildings",
                    "Display or hide buildings.",
                    $"Reset building display state to default value.",
                    buildingEnabled,
                    SettingsManager.BUILDING_DISPLAY_STATE_DEFAULT_VALUE,
                     (x) => SettingsManager.SaveBuildingVisibility(x),
                     () => SettingsManager.ResetBuildingVisibility());

                if (!buildingEnabled)
                {
                    grid.DisableNextElements();
                }

                //Buildings color
                SettingsManager.DisplaySettingsColorPickerWithReset(grid,
                    "Buildings color",
                    "Change buildings color.",
                    "Reset buildings color to default value.",
                    SettingsManager.CurrentSettings.BuildingColor,
                    SettingsManager.BUILDING_COLOR_DEFAULT_VALUE,
                    (x) => SettingsManager.SaveBuildingColor(x),
                    () => SettingsManager.ResetBuildingColor());

                //Building ambient occlusion settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Ambient occlusion",
                    "Define buildings ambient occlusion value.",
                    $"Reset buildings ambient occlusion to default value ({SettingsManager.BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.BuildingAO,
                    0.0f,
                    1.0f,
                    0.1f,
                    SettingsManager.BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE,
                    "%.1f",
                     (x) => SettingsManager.SaveBuildingAO(x),
                     () => SettingsManager.ResetBuildingAO());
            }
        }
        #endregion
    }
}
