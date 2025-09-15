using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace FlightReLive.Core.Terrain
{
    public class TerrainManager : MonoBehaviour
    {
        #region CONSTANTS
        private const int MESH_LINE_PERF_FRAME = 64;
        #endregion

        #region ATTRIBUTES
        [SerializeField] internal Material _meshMaterial;
        private List<GameObject> _tiles;
        private MaterialPropertyBlock _materialPropertyBlock;
        #endregion

        #region EVENTS
        internal event Action<FlightData> OnTerrainLoaded;
        #endregion

        #region PROPERTIES
        internal static TerrainManager Instance { get; private set; }
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
            _materialPropertyBlock = new MaterialPropertyBlock();
        }
        #endregion

        #region METHODS
        internal void LoadFlightMap(FlightData flightData)
        {
            List<TileDefinition> sortedTiles = flightData.MapDefinition.GetSortedTiles();
            StitchAdjacentTiles(sortedTiles);

            float tileSize = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);
            StartCoroutine(GenerateAndBuildTilesCoroutine(flightData, sortedTiles, tileSize, flightData.GlobalScale, 1, 1));
        }

        private IEnumerator GenerateAndBuildTilesCoroutine(FlightData flight, List<TileDefinition> tiles, float tileSize, float globalScale, int meshPerFrame, int goPerFrame)
        {
            float minAltitude = 0f, maxAltitude = 0f;
            GetGlobalAltitudeRange(tiles, out minAltitude, out maxAltitude);

            int meshDone = 0;
            foreach (TileDefinition tile in tiles)
            {
                bool done = false;
                yield return StartCoroutine(GenerateTerrainMeshFromHeightmapAsync(tile.HeightMap, tileSize, minAltitude, maxAltitude, (md) => { tile.MeshData = md; done = true; }, MESH_LINE_PERF_FRAME));

                while (!done)
                {
                    yield return null;
                }

                meshDone++;
                if (meshDone >= meshPerFrame)
                {
                    meshDone = 0;
                    yield return null;
                }
            }

            int minX = tiles.Min(t => t.X);
            int maxX = tiles.Max(t => t.X);
            int minY = tiles.Min(t => t.Y);
            int maxY = tiles.Max(t => t.Y);
            float centerTileX = (minX + maxX) / 2f;
            float centerTileY = (minY + maxY) / 2f;

            int goDone = 0;
            foreach (TileDefinition tile in tiles)
            {
                CreateSingleTileGameObject(tile, tileSize, globalScale, centerTileX, centerTileY);
                goDone++;
                if (goDone >= goPerFrame)
                {
                    goDone = 0;
                    yield return null;
                }
            }

            OnTerrainLoaded?.Invoke(flight);
        }

        private void CreateSingleTileGameObject(TileDefinition tile, float tileSize, float globalScale, float centerTileX, float centerTileY)
        {
            float posX = (tile.X - centerTileX) * tileSize * globalScale;
            float posZ = -(tile.Y - centerTileY) * tileSize * globalScale;

            GameObject tempTile = new GameObject($"Tile_{tile.X}_{tile.Y}");
            tempTile.SetActive(false);
            tempTile.transform.parent = transform;
            tempTile.transform.localPosition = new Vector3(posX, 0f, posZ);
            tempTile.transform.localScale = Vector3.one * globalScale;

            Mesh mesh = tile.MeshData.ConvertToUnityMesh();
            tempTile.AddComponent<MeshFilter>().mesh = mesh;
            tempTile.AddComponent<MeshCollider>().sharedMesh = mesh;
            MeshRenderer meshRenderer = tempTile.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _meshMaterial;
            _materialPropertyBlock.Clear();
            _materialPropertyBlock.SetTexture("_Satellite", tile.SatelliteTexture);
            meshRenderer.SetPropertyBlock(_materialPropertyBlock);
            _tiles.Add(tempTile);
            tempTile.SetActive(true);
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
                if (map == null || map.GetLength(0) == 0 || map.GetLength(1) == 0) continue;

                int width = map.GetLength(0);
                int height = map.GetLength(1);
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        float altitude = map[x, z];
                        if (altitude < minAltitude) minAltitude = altitude;
                        if (altitude > maxAltitude) maxAltitude = altitude;
                    }
                }
            }
        }

        private IEnumerator GenerateTerrainMeshFromHeightmapAsync(float[,] heightmap, float tileSize, float minAltitude, float maxAltitude, Action<MeshData> onCompleted, int linesPerFrame = 64)              
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            int vertexCount = width * height;
            int quadCount = (width - 1) * (height - 1);

            float xSpacing = tileSize / (width - 1);
            float zSpacing = tileSize / (height - 1);

            MeshData meshData = new MeshData
            {
                vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent),
                uvs = new NativeArray<Vector2>(vertexCount, Allocator.Persistent),
                uvs2 = new NativeArray<Vector2>(vertexCount, Allocator.Persistent),
                normals = new NativeArray<Vector3>(vertexCount, Allocator.Persistent),
                triangles = new NativeArray<int>(quadCount * 6, Allocator.Persistent)
            };

            // Step 1 : fill vertices & UVs progressively
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (height - 1 - y) * width + x;

                    float altitude = heightmap[x, y];
                    float px = x * xSpacing;
                    float pz = (height - 1 - y) * zSpacing;

                    meshData.vertices[i] = new Vector3(px, altitude, pz);
                    meshData.uvs[i] = new Vector2((float)x / (width - 1), (float)(height - 1 - y) / (height - 1));

                    float relativeNorm = Mathf.InverseLerp(minAltitude, maxAltitude, altitude);
                    meshData.uvs2[i] = new Vector2(altitude, relativeNorm);
                }

                // Toutes les X lignes → on laisse respirer la frame
                if (y % linesPerFrame == 0)
                {
                    yield return null;
                }
            }

            // Step 2 : build triangles & normals progressively
            int triIndex = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int i0 = y * width + x;
                    int i1 = (y + 1) * width + x;
                    int i2 = (y + 1) * width + (x + 1);
                    int i3 = y * width + (x + 1);

                    meshData.triangles[triIndex++] = i0;
                    meshData.triangles[triIndex++] = i1;
                    meshData.triangles[triIndex++] = i2;

                    Vector3 normal1 = Vector3.Cross(meshData.vertices[i1] - meshData.vertices[i0], meshData.vertices[i2] - meshData.vertices[i0]).normalized;
                    meshData.normals[i0] += normal1;
                    meshData.normals[i1] += normal1;
                    meshData.normals[i2] += normal1;

                    meshData.triangles[triIndex++] = i0;
                    meshData.triangles[triIndex++] = i2;
                    meshData.triangles[triIndex++] = i3;

                    Vector3 normal2 = Vector3.Cross(meshData.vertices[i2] - meshData.vertices[i0], meshData.vertices[i3] - meshData.vertices[i0]).normalized;
                    meshData.normals[i0] += normal2;
                    meshData.normals[i2] += normal2;
                    meshData.normals[i3] += normal2;
                }

                if (y % linesPerFrame == 0)
                {
                    yield return null;
                }
            }

            // Step 3 : normalize normals
            for (int i = 0; i < vertexCount; i++)
            {
                meshData.normals[i] = meshData.normals[i].normalized;
            }

            // Mesh ready
            onCompleted?.Invoke(meshData);
        }

        internal static void StitchAdjacentTiles(List<TileDefinition> tiles)
        {
            Dictionary<(int x, int y), TileDefinition> tileMap = tiles.ToDictionary(t => (t.X, t.Y));

            foreach (var tile in tiles)
            {
                float[,] heightmap = tile.HeightMap;
                int width = heightmap.GetLength(0);
                int height = heightmap.GetLength(1);

                //Right neighbor
                if (tileMap.TryGetValue((tile.X + 1, tile.Y), out var rightTile))
                {
                    float[,] rightMap = rightTile.HeightMap;

                    for (int y = 0; y < height; y++)
                    {
                        float avg = (heightmap[width - 1, y] + rightMap[0, y]) / 2f;
                        heightmap[width - 1, y] = avg;
                        rightMap[0, y] = avg;
                    }
                }

                //Top neighbor
                if (tileMap.TryGetValue((tile.X, tile.Y + 1), out var topTile))
                {
                    float[,] topMap = topTile.HeightMap;

                    for (int x = 0; x < width; x++)
                    {
                        float avg = (heightmap[x, height - 1] + topMap[x, 0]) / 2f;
                        heightmap[x, height - 1] = avg;
                        topMap[x, 0] = avg;
                    }
                }
            }
        }
        #endregion
    }
}
