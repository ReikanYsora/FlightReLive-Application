using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using FlightReLive.Core.WorldUI;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace FlightReLive.Core.Terrain
{
    public class TerrainManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float GLOBAL_SCALE = 0.1f;
        #endregion

        #region ATTRIBUTES
        [Header("Geo surface mesh visualization")]
        [SerializeField] internal Material _meshMaterial;
        [SerializeField] internal Material _pointCloudMaterial;
        [SerializeField] internal Gradient _elevateGradient;
        private List<GameObject> _tiles;
        #endregion

        #region EVENTS
        internal event Action<FlightData> OnTerrainLoaded;
        #endregion

        #region PROPERTIES
        internal static TerrainManager Instance { get; private set; }

        internal float GlobalScale
        {
            get
            {
                return GLOBAL_SCALE;
            }
        }
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

            _tiles = new List<GameObject>();
        }
        #endregion

        #region METHODS
        internal void LoadFlightMap(FlightData flightData)
        {
            List<TileDefinition> sortedTiles = flightData.MapDefinition.GetSortedTiles();
            QualityPreset mapQualityPreset = SettingsManager.CurrentSettings.MapQualityPreset;

            //Get min and max altitude for coherent gradient
            float minAltitude = 0f;
            float maxAltitude = 0f;

            GetGlobalAltitudeRange(sortedTiles, out minAltitude, out maxAltitude);
            StitchAdjacentTiles(sortedTiles, mapQualityPreset);
            float tileSize = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);

            Parallel.ForEach(sortedTiles, tileDef =>
            {
                tileDef.MeshData = GenerateTerrainMeshFromHeightmap(mapQualityPreset, tileDef.HeightMap, tileSize, minAltitude, maxAltitude);
            });

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                //Create all tiles needed for map
                CreateMapTiles(flightData, tileSize, GlobalScale);

                try
                {
                    //Mesh is ready, we need the mesh to process altitude with precision
                    WorldUIManager.Instance.LoadFlightPOIs(flightData);
                }
                catch (Exception)
                {

                }

                OnTerrainLoaded?.Invoke(flightData);
            });
        }

        private void CreateMapTiles(FlightData flight, float tileSize, float globalScale)
        {
            int minX = flight.MapDefinition.TileDefinitions.Min(t => t.X);
            int maxX = flight.MapDefinition.TileDefinitions.Max(t => t.X);
            int minY = flight.MapDefinition.TileDefinitions.Min(t => t.Y);
            int maxY = flight.MapDefinition.TileDefinitions.Max(t => t.Y);
            float centerTileX = (minX + maxX) / 2f;
            float centerTileY = (minY + maxY) / 2f;

            foreach (TileDefinition tile in flight.MapDefinition.TileDefinitions)
            {
                float posX = (tile.X - centerTileX) * tileSize * globalScale;
                float posZ = -(tile.Y - centerTileY) * tileSize * globalScale;

                // Create terrain tile
                GameObject tempTile = new GameObject($"Tile_{tile.X}_{tile.Y}");
                tempTile.SetActive(false);
                tempTile.transform.parent = transform;
                tempTile.transform.localPosition = new Vector3(posX, 0f, posZ);
                tempTile.transform.localScale = Vector3.one * globalScale;

                Mesh mesh = tile.MeshData.ConvertToUnityMesh(MeshType.Triangles);
                tempTile.AddComponent<MeshFilter>().mesh = mesh;
                tempTile.AddComponent<MeshCollider>().sharedMesh = mesh;

                Material terrainMaterial = new Material(_meshMaterial);
                terrainMaterial.SetTexture("_Satellite", tile.SatelliteTexture);
                terrainMaterial.SetTexture("_HillShade", tile.HillShadeTexture);
                terrainMaterial.SetFloat("_Contrast", 0.9f);
                tempTile.AddComponent<MeshRenderer>().material = terrainMaterial;
                _tiles.Add(tempTile);
            }

            _tiles.ForEach(x => x.SetActive(true));

            //Tiles and heightmaps are defined, we can now calculate TakeOffAltitude
            flight.TakeOffAltitude = GetAltitudeAtPosition(flight, flight.EstimateTakeOffPosition);
        }

        internal void UnloadFlightMap()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                foreach (GameObject tempTile in _tiles)
                {
                    Destroy(tempTile);
                }
                _tiles.Clear();
            });
        }

        internal static void GetGlobalAltitudeRange(List<TileDefinition> tiles, out float minAltitude, out float maxAltitude)
        {
            minAltitude = float.MaxValue;
            maxAltitude = -float.MaxValue;

            foreach (var tile in tiles)
            {
                float[,] map = tile.HeightMap;

                if (map == null || map.GetLength(0) == 0 || map.GetLength(1) == 0)
                {
                    continue;
                }

                int width = map.GetLength(0);
                int height = map.GetLength(1);

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        float altitude = map[x, z];

                        if (altitude < minAltitude)
                        {
                            minAltitude = altitude;
                        }

                        if (altitude > maxAltitude)
                        {
                            maxAltitude = altitude;
                        }
                    }
                }
            }
        }

        private static MeshData GenerateTerrainMeshFromHeightmap(QualityPreset quality, float[,] heightmap, float tileSize, float minAltitude, float maxAltitude)
        {
            MeshData meshData = new MeshData();

            int sourceWidth = heightmap.GetLength(0);
            int sourceHeight = heightmap.GetLength(1);
            int step;

            switch (quality)
            {
                case QualityPreset.Quality:
                    step = 1;
                    break;
                case QualityPreset.Balanced:
                    step = 2;
                    break;
                case QualityPreset.Performance:
                    step = 4;
                    break;
                default:
                    step = 1;
                    break;
            }

            int width = sourceWidth / step;
            int height = sourceHeight / step;
            int vertexCount = width * height;
            int quadCount = (width - 1) * (height - 1);
            float xSpacing = tileSize / (width - 1);
            float zSpacing = tileSize / (height - 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Vector2[] uvs2 = new Vector2[vertexCount];
            Color[] colors = new Color[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            List<int> triangles = new List<int>(quadCount * 6);

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (height - 1 - y) * width + x;

                    int hx = x * step;
                    int hy = y * step;

                    float altitude = heightmap[hx, hy];
                    float px = x * xSpacing;
                    float pz = (height - 1 - y) * zSpacing;

                    vertices[i] = new Vector3(px, altitude, pz);
                    uvs[i] = new Vector2((float)x / (width - 1), (float)(height - 1 - y) / (height - 1));

                    float absoluteNorm = altitude;
                    float relativeNorm = Mathf.InverseLerp(minAltitude, maxAltitude, altitude);
                    uvs2[i] = new Vector2(absoluteNorm, relativeNorm);

                    float t = relativeNorm;
                    colors[i] = new Color(t, t, t, 1f);
                }
            }

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int i0 = y * width + x;
                    int i1 = (y + 1) * width + x;
                    int i2 = (y + 1) * width + (x + 1);
                    int i3 = y * width + (x + 1);

                    triangles.Add(i0); triangles.Add(i1); triangles.Add(i2);
                    Vector3 normal1 = Vector3.Cross(vertices[i1] - vertices[i0], vertices[i2] - vertices[i0]).normalized;
                    normals[i0] += normal1;
                    normals[i1] += normal1;
                    normals[i2] += normal1;

                    triangles.Add(i0); triangles.Add(i2); triangles.Add(i3);
                    Vector3 normal2 = Vector3.Cross(vertices[i2] - vertices[i0], vertices[i3] - vertices[i0]).normalized;
                    normals[i0] += normal2;
                    normals[i2] += normal2;
                    normals[i3] += normal2;
                }
            }

            for (int i = 0; i < vertexCount; i++)
            {
                normals[i] = normals[i].normalized;
            }

            meshData.vertices = new List<Vector3>(vertices);
            meshData.triangles = triangles;
            meshData.uvs = new List<Vector2>(uvs);
            meshData.uvs2 = new List<Vector2>(uvs2);
            meshData.normals = new List<Vector3>(normals);

            return meshData;
        }

        internal float GetAltitudeAtPosition(FlightData flightData, FlightGPSData gps)
        {
            Vector3 gpsWorldPos = ConvertGPSPositionToWorld(flightData, new Vector3((float)gps.Latitude, 0f, (float)gps.Longitude));
            Vector2 targetXZ = new Vector2(gpsWorldPos.x, gpsWorldPos.z);

            GameObject closestTile = null;
            float minTileDistanceSqr = float.MaxValue;

            foreach (GameObject tile in _tiles)
            {
                Vector3 tilePos = tile.transform.position;
                float dx = tilePos.x - targetXZ.x;
                float dz = tilePos.z - targetXZ.y;
                float distSqr = dx * dx + dz * dz;

                if (distSqr < minTileDistanceSqr)
                {
                    minTileDistanceSqr = distSqr;
                    closestTile = tile;
                }
            }

            if (closestTile == null)
            {
                return 0f;
            }

            MeshFilter meshFilter = closestTile.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return 0f;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv2 = mesh.uv2;

            if (vertices == null || uv2 == null || uv2.Length != vertices.Length)
            {
                return 0f;
            }

            int count = vertices.Length;

            NativeArray<Vector3> nativeVertices = new NativeArray<Vector3>(vertices, Allocator.TempJob);
            NativeArray<Vector2> nativeUV2 = new NativeArray<Vector2>(uv2, Allocator.TempJob);
            NativeArray<float> result = new NativeArray<float>(2, Allocator.TempJob); // [0] = minDist, [1] = altitude

            result[0] = float.MaxValue;
            result[1] = 0f;

            Matrix4x4 localToWorld = closestTile.transform.localToWorldMatrix;

            AltitudeJob job = new AltitudeJob
            {
                vertices = nativeVertices,
                uv2 = nativeUV2,
                localToWorld = localToWorld,
                targetXZ = targetXZ,
                result = result
            };

            job.Schedule().Complete();

            float altitude = result[1];

            nativeVertices.Dispose();
            nativeUV2.Dispose();
            result.Dispose();

            return altitude;
        }

        internal static void StitchAdjacentTiles(List<TileDefinition> tiles, QualityPreset quality)
        {
            int step = 1;

            switch (quality)
            {
                case QualityPreset.Quality:
                    step = 1;
                    break;
                case QualityPreset.Balanced:
                    step = 2;
                    break;
                case QualityPreset.Performance:
                    step = 4;
                    break;
                default:
                    step = 1;
                    break;
            }

            Dictionary<(int x, int y), TileDefinition> tileMap = tiles.ToDictionary(t => (t.X, t.Y));

            foreach (var tile in tiles)
            {
                float[,] heightmap = tile.HeightMap;
                int width = heightmap.GetLength(0);
                int height = heightmap.GetLength(1);

                // Stitch avec la tuile de droite
                if (tileMap.TryGetValue((tile.X + 1, tile.Y), out var rightTile))
                {
                    float[,] rightMap = rightTile.HeightMap;

                    for (int y = 0; y < height; y += step)
                    {
                        int yIndex = y;
                        float avg = (heightmap[width - step, yIndex] + rightMap[0, yIndex]) / 2f;
                        heightmap[width - step, yIndex] = avg;
                        rightMap[0, yIndex] = avg;
                    }
                }

                // Stitch avec la tuile du haut
                if (tileMap.TryGetValue((tile.X, tile.Y + 1), out var topTile))
                {
                    float[,] topMap = topTile.HeightMap;

                    for (int x = 0; x < width; x += step)
                    {
                        int xIndex = x;
                        float avg = (heightmap[xIndex, height - step] + topMap[xIndex, 0]) / 2f;
                        heightmap[xIndex, height - step] = avg;
                        topMap[xIndex, 0] = avg;
                    }
                }
            }
        }

        internal Vector3 ConvertGPSPositionToWorld(FlightData flight, Vector3 gpsPosition)
        {
            // Conversion latitude/longitude → mètres
            float xMeters = (float)MapTools.HaversineDistance(flight.SceneCenterGPS.x, flight.SceneCenterGPS.y, flight.SceneCenterGPS.x, gpsPosition.z); // longitude
            float zMeters = (float)MapTools.HaversineDistance(flight.SceneCenterGPS.x, flight.SceneCenterGPS.y, gpsPosition.x, flight.SceneCenterGPS.y); // latitude

            if (gpsPosition.z < flight.SceneCenterGPS.y)
            {
                xMeters *= -1f;
            }

            if (gpsPosition.x < flight.SceneCenterGPS.x)
            {
                zMeters *= -1f;
            }

            float yMeters = gpsPosition.y;

            return new Vector3(xMeters, yMeters, zMeters) * GLOBAL_SCALE;
        }

        internal static List<Vector3> CreateBezierFlightPath(FlightData flightData, List<FlightDataPoint> flightPoints, List<TileDefinition> tiles, float referenceAltitude, int samplesPerSegment = 10, float controlOffsetFactor = 0.3f)
        {
            if (flightPoints == null || flightPoints.Count < 2 || tiles == null || tiles.Count == 0)
            {
                return new List<Vector3>();
            }

            // Conversion GPS → Unity
            List<Vector3> positions = flightPoints
                .OrderBy(x => x.TimeSpan)
                .Select(p =>
                {
                    Vector3 gps = new Vector3((float)p.Latitude, referenceAltitude + (float)p.Height, (float)p.Longitude);
                    return Instance.ConvertGPSPositionToWorld(flightData, gps);
                })
                .ToList();

            // Lissage
            positions = MapTools.PreSmoothGPS(positions, radius: 5);

            return positions;
        }
        #endregion

        #region UI
        //internal void DrawHeightmapSettings(FuLayout layout)
        //{
        //    using (FuGrid grid = new FuGrid("grdHeightmapSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
        //    {
        //        grid.ButtonsGroup<PointCloudMode>("Heightmap", (value) =>
        //        {
        //            SettingsManager.SavePointCloudMode((PointCloudMode)value);
        //        }, () => SettingsManager.CurrentSettings.PointCloudMode);

        //        if (SettingsManager.CurrentSettings.PointCloudMode == PointCloudMode.Relative)
        //        {
        //            grid.DisableNextElement();
        //        }
        //        else if (SettingsManager.CurrentSettings.PointCloudMode == PointCloudMode.Disabled)
        //        {
        //            grid.DisableNextElements();
        //        }

        //        float minAltAbsolute = SettingsManager.CurrentSettings.AbsoluteAltitudeMin;
        //        float maxAltAbsolute = SettingsManager.CurrentSettings.AbsoluteAltitudeMax;

        //        if (grid.Range("Altitude limits", ref minAltAbsolute, ref maxAltAbsolute, 0, 1000f, 1f))
        //        {
        //            SettingsManager.SaveAbsoluteAltitudeMin(minAltAbsolute);
        //            SettingsManager.SaveAbsoluteAltitudeMax(maxAltAbsolute);
        //        }

        //        float heightPointSize = SettingsManager.CurrentSettings.HeightPointSize;
        //        if (grid.Slider("Point size", ref heightPointSize, 0f, 1f, 0.05f))
        //        {
        //            SettingsManager.SaveHeightPointSize(heightPointSize);
        //        }

        //        float opacity = SettingsManager.CurrentSettings.HeightOpacity;
        //        if (grid.Slider("Opacity", ref opacity, 0f, 1f, 0.05f))
        //        {
        //            SettingsManager.SaveHeightOpacity(opacity);
        //        }
        //    }
        //}
        #endregion
    }
}
