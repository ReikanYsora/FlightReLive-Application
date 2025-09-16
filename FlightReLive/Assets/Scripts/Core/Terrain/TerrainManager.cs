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
        private readonly Dictionary<(int, int), GameObject> _tileObjects = new Dictionary<(int, int), GameObject>();
        private MaterialPropertyBlock _materialPropertyBlock;
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
            _materialPropertyBlock = new MaterialPropertyBlock();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Construit et instancie une seule tuile dans la scène.
        /// </summary>
        internal void LoadTile(TileDefinition tile, FlightData flightData)
        {
            if (tile.HeightMap == null)
            {
                Debug.LogWarning($"Tile {tile.X},{tile.Y} has no heightmap, cannot build terrain.");
                return;
            }

            // Calculer min/max altitude pour cette tuile
            float minAltitude = float.MaxValue;
            float maxAltitude = -float.MaxValue;
            int w = tile.HeightMap.GetLength(0);
            int h = tile.HeightMap.GetLength(1);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float a = tile.HeightMap[x, y];
                    if (a < minAltitude)
                    {
                        minAltitude = a;
                    }

                    if (a > maxAltitude)
                    {
                        maxAltitude = a;
                    }
                }
            }

            float tileSize = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);

            // Génération du mesh en coroutine
            StartCoroutine(GenerateTerrainMeshFromHeightmapAsync(tile.HeightMap, tileSize, minAltitude, maxAltitude,
                (meshData) =>
                {
                    tile.MeshData = meshData;

                    // Calcul du centre global pour le positionnement
                    int minX = flightData.MapDefinition.TileDefinitions.Min(t => t.X);
                    int maxX = flightData.MapDefinition.TileDefinitions.Max(t => t.X);
                    int minY = flightData.MapDefinition.TileDefinitions.Min(t => t.Y);
                    int maxY = flightData.MapDefinition.TileDefinitions.Max(t => t.Y);

                    float centerTileX = (minX + maxX) / 2f;
                    float centerTileY = (minY + maxY) / 2f;

                    CreateSingleTileGameObject(tile, tileSize, flightData.GlobalScale, centerTileX, centerTileY);
                }, MESH_LINE_PERF_FRAME));
        }

        /// <summary>
        /// Unload a loaded tile
        /// </summary>
        internal void UnloadTile(TileDefinition tile)
        {
            if (_tileObjects.TryGetValue((tile.X, tile.Y), out GameObject go))
            {
                Destroy(go);
                _tileObjects.Remove((tile.X, tile.Y));
            }

            tile.MeshData = null;
            tile.SatelliteTexture = null;
        }

        /// <summary>
        /// Unload all tiles
        /// </summary>
        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                foreach (var go in _tileObjects.Values)
                {
                    Destroy(go);
                }
                _tileObjects.Clear();
            });
        }

        /// <summary>
        /// Create tile gameobject
        /// </summary>
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

            _tileObjects[(tile.X, tile.Y)] = tempTile;
            tempTile.SetActive(true);
        }

        /// <summary>
        /// Coroutine for generate mesh from heightmap
        /// </summary>
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

            //Step 1 : vertices & UV
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

                if (y % linesPerFrame == 0)
                {
                    yield return null;
                }
            }

            //Step 2 : triangles & normales
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

            //Step 3 : normalization
            for (int i = 0; i < vertexCount; i++)
            {
                meshData.normals[i] = meshData.normals[i].normalized;
            }

            onCompleted?.Invoke(meshData);
        }

        #endregion
    }
}
