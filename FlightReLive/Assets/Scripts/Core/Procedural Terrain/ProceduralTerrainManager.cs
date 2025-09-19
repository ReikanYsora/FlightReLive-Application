using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace FlightReLive.Core.ProceduralTerrain
{
    public class ProceduralTerrainManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private GameObject _terrain;
        [SerializeField] private Texture2D _detailNormalMap;
        [SerializeField] private Texture2D _detailMaskMap;
        #endregion

        #region PROPERTIES
        internal static ProceduralTerrainManager Instance { get; private set; }
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
        }
        #endregion

        #region JOB STRUCTS

        [BurstCompile]
        private struct MinMaxHeightJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> heights;
            [NativeDisableParallelForRestriction] public NativeArray<float> minMax; // [0] = min, [1] = max

            public void Execute(int index)
            {
                float h = heights[index];
                if (h < minMax[0]) minMax[0] = h;
                if (h > minMax[1]) minMax[1] = h;
            }
        }

        [BurstCompile]
        private struct MergeHeightmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> tileHeights;
            [ReadOnly] public int resTile;
            [ReadOnly] public int totalW;
            [ReadOnly] public int totalH;
            [ReadOnly] public int localX;
            [ReadOnly] public int localY;
            [ReadOnly] public float minH;
            [ReadOnly] public float range;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> merged;

            public void Execute(int index)
            {
                int x = index % resTile;
                int y = index / resTile;
                int destRow = localY + (resTile - 1 - y);
                int destCol = localX + x;
                int dstIndex = destRow * totalW + destCol;

                float normalized = (tileHeights[index] - minH) / range;
                merged[dstIndex] = normalized;
            }
        }

        [BurstCompile]
        private struct TileCopyJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> srcPixels;
            [WriteOnly] public NativeArray<Color32> globalPixels;

            public int texTileW;
            public int texTileH;
            public int totalTexW;
            public int localX;
            public int localY;

            public void Execute(int index)
            {
                int x = index % texTileW;
                int y = index / texTileW;

                int destRow = localY + y;
                int destCol = localX + x;
                int dstIndex = destRow * totalTexW + destCol;
                int srcIndex = y * texTileW + x;

                globalPixels[dstIndex] = srcPixels[srcIndex];
            }
        }
        #endregion

        #region METHODS
        internal void GenerateTerrain(FlightData flightData)
        {
            List<TileDefinition> tiles = flightData.MapDefinition.GetSortedTiles();

            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogError("No tiles available to build terrain.");
                return;
            }

            int resTile = tiles[0].HeightMap.GetLength(0);
            int texTileW = tiles[0].SatelliteTexture.width;
            int texTileH = tiles[0].SatelliteTexture.height;

            int minX = tiles.Min(t => t.X);
            int maxX = tiles.Max(t => t.X);
            int minY = tiles.Min(t => t.Y);
            int maxY = tiles.Max(t => t.Y);
            int tilesX = (maxX - minX) + 1;
            int tilesY = (maxY - minY) + 1;

            int totalW = tilesX * (resTile - 1) + 1;
            int totalH = tilesY * (resTile - 1) + 1;
            int totalTexW = tilesX * (texTileW - 1) + 1;
            int totalTexH = tilesY * (texTileH - 1) + 1;

            //Get min / max altitude
            List<float> allHeights = new List<float>();
            foreach (var tile in tiles)
            {
                float[,] src = tile.HeightMap;

                for (int y = 0; y < resTile; y++)
                {
                    for (int x = 0; x < resTile; x++)
                    {
                        allHeights.Add(src[x, y]);
                    }
                }
            }

            NativeArray<float> heightArray = new NativeArray<float>(allHeights.ToArray(), Allocator.TempJob);
            NativeArray<float> minMax = new NativeArray<float>(2, Allocator.TempJob);
            minMax[0] = float.MaxValue;
            minMax[1] = float.MinValue;

            MinMaxHeightJob minMaxJob = new MinMaxHeightJob
            {
                heights = heightArray,
                minMax = minMax
            };

            JobHandle minMaxHandle = minMaxJob.Schedule(heightArray.Length, 64);
            minMaxHandle.Complete();

            float minHVal = minMax[0];
            float maxHVal = minMax[1];
            float heightRange = Mathf.Max(0.001f, maxHVal - minHVal);

            heightArray.Dispose();
            minMax.Dispose();

            //Merge heightmaps
            NativeArray<float> mergedFlat = new NativeArray<float>(totalW * totalH, Allocator.TempJob);

            foreach (var tile in tiles)
            {
                int localX = (tile.X - minX) * (resTile - 1);
                int localY = (maxY - tile.Y) * (resTile - 1);
                float[,] src = tile.HeightMap;

                NativeArray<float> tileFlat = new NativeArray<float>(resTile * resTile, Allocator.TempJob);
                for (int y = 0; y < resTile; y++)
                {
                    for (int x = 0; x < resTile; x++)
                    {
                        tileFlat[y * resTile + x] = src[x, y];
                    }
                }

                MergeHeightmapJob mergeJob = new MergeHeightmapJob
                {
                    tileHeights = tileFlat,
                    resTile = resTile,
                    totalW = totalW,
                    totalH = totalH,
                    localX = localX,
                    localY = localY,
                    minH = minHVal,
                    range = heightRange,
                    merged = mergedFlat
                };

                JobHandle mergeHandle = mergeJob.Schedule(tileFlat.Length, 64);
                mergeHandle.Complete();
                tileFlat.Dispose();
            }

            //Convert NativeArray<float> to float[,]
            float[,] merged = new float[totalH, totalW];
            for (int y = 0; y < totalH; y++)
            {
                for (int x = 0; x < totalW; x++)
                {
                    merged[y, x] = mergedFlat[y * totalW + x];
                }
            }

            mergedFlat.Dispose();

            //Merge satellite texture
            Color32[] globalPixels = new Color32[totalTexW * totalTexH];
            foreach (TileDefinition tile in tiles)
            {
                int localX = (tile.X - minX) * (texTileW - 1);
                int localY = (maxY - tile.Y) * (texTileH - 1);

                Texture2D temp = tile.SatelliteTexture;
                temp.filterMode = FilterMode.Trilinear;
                temp.Apply(false, false);

                Color32[] srcPixels = temp.GetPixels32();
                for (int y = 0; y < texTileH; y++)
                {
                    int destRow = localY + y;
                    for (int x = 0; x < texTileW; x++)
                    {
                        int destCol = localX + x;
                        int dstIndex = destRow * totalTexW + destCol;
                        int srcIndex = y * texTileW + x;
                        globalPixels[dstIndex] = srcPixels[srcIndex];
                    }
                }

                GameObject.Destroy(temp);
            }

            Texture2D globalSatellite = new Texture2D(totalTexW, totalTexH, TextureFormat.RGB24, false, false);
            globalSatellite.SetPixels32(globalPixels);
            globalSatellite.Apply();

            //Terrain
            int longest = Mathf.Max(totalW, totalH);
            int terrainRes = Mathf.NextPowerOfTwo(longest - 1) + 1;

            if (terrainRes != totalW || terrainRes != totalH)
            {
                merged = ResampleHeightsBilinear(merged, totalW, totalH, terrainRes, terrainRes);
                totalW = totalH = terrainRes;
            }

            float tileSizeM = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);
            float sizeX = tilesX * tileSizeM * flightData.GlobalScale;
            float sizeZ = tilesY * tileSizeM * flightData.GlobalScale;
            float sizeY = heightRange * flightData.GlobalScale;

            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = totalW,
                size = new Vector3(sizeX, sizeY, sizeZ)
            };
            terrainData.SetHeights(0, 0, merged);

            _terrain = Terrain.CreateTerrainGameObject(terrainData);
            Terrain terrain = _terrain.GetComponent<Terrain>();
            _terrain.name = "GlobalTerrain";
            _terrain.transform.SetParent(transform, false);

            float centerTileX = (minX + maxX) / 2.0f;
            float centerTileY = (minY + maxY) / 2.0f;

            float offsetX = (tilesX % 2 == 0)
                ? -(centerTileX - minX + 1.0f) * tileSizeM * flightData.GlobalScale + (tileSizeM * 0.5f * flightData.GlobalScale)
                : -(centerTileX - minX + 0.5f) * tileSizeM * flightData.GlobalScale;

            float offsetZ = (tilesY % 2 == 0)
                ? (centerTileY - minY + 1.0f) * tileSizeM * flightData.GlobalScale - (tileSizeM * 0.5f * flightData.GlobalScale)
                : (centerTileY - minY + 0.5f) * tileSizeM * flightData.GlobalScale;

            float offsetY = minHVal * flightData.GlobalScale;
            _terrain.transform.localPosition = new Vector3(offsetX, offsetY, -offsetZ);
            Texture2D neutralMask = GenerateNeutralMaskMap(globalSatellite.width, globalSatellite.height);

            TerrainLayer satelliteLayer = new TerrainLayer
            {
                diffuseTexture = globalSatellite,
                maskMapTexture = neutralMask,
                tileSize = new Vector2(terrainData.size.x, terrainData.size.z)
            };

            TerrainLayer detailLayer = new TerrainLayer
            {
                diffuseTexture = Texture2D.blackTexture,
                normalMapTexture = _detailNormalMap,
                maskMapTexture = _detailMaskMap,
                tileSize = new Vector2(1000f, 1000f)
            };

            terrain.terrainData.terrainLayers = new TerrainLayer[] { satelliteLayer, detailLayer };

            int alphaRes = terrain.terrainData.alphamapResolution;
            float[,,] maps = new float[alphaRes, alphaRes, 2];

            for (int y = 0; y < alphaRes; y++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    maps[y, x, 0] = 0.80f;
                    maps[y, x, 1] = 0.20f;
                }
            }

            terrain.terrainData.SetAlphamaps(0, 0, maps);

            //Cleanup
            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                tile.SatelliteTexture = null;
            }

            GC.Collect();
        }

        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                GameObject.Destroy(_terrain);
            });
        }

        private Texture2D GenerateNeutralMaskMap(int width, int height)
        {
            Texture2D maskMap = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 255, 255, 0);
            }

            maskMap.SetPixels32(pixels);
            maskMap.Apply();

            return maskMap;
        }

        private static float[,] ResampleHeightsBilinear(float[,] src, int srcW, int srcH, int newW, int newH)
        {
            float[,] dst = new float[newH, newW];

            for (int y = 0; y < newH; y++)
            {
                float gy = (y / (float)(newH - 1)) * (srcH - 1);
                int y0 = Mathf.FloorToInt(gy);
                int y1 = Mathf.Min(y0 + 1, srcH - 1);
                float ty = gy - y0;

                for (int x = 0; x < newW; x++)
                {
                    float gx = (x / (float)(newW - 1)) * (srcW - 1);
                    int x0 = Mathf.FloorToInt(gx);
                    int x1 = Mathf.Min(x0 + 1, srcW - 1);
                    float tx = gx - x0;

                    float a = src[y0, x0];
                    float b = src[y0, x1];
                    float c = src[y1, x0];
                    float d = src[y1, x1];

                    float ab = Mathf.Lerp(a, b, tx);
                    float cd = Mathf.Lerp(c, d, tx);
                    dst[y, x] = Mathf.Lerp(ab, cd, ty);
                }
            }

            return dst;
        }
        #endregion
    }
}




