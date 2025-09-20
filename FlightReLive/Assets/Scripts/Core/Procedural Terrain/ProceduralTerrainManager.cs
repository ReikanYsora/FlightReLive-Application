using FlightReLive.Core.Environment;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace FlightReLive.Core.ProceduralTerrain
{
    public class ProceduralTerrainManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private GameObject _terrain;
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
        private struct MinMaxHeightJob : IJob
        {
            [ReadOnly] public NativeArray<float> heights;
            public NativeArray<float> minMax;

            public void Execute()
            {
                float min = float.MaxValue;
                float max = float.MinValue;

                for (int i = 0; i < heights.Length; i++)
                {
                    float h = heights[i];

                    if (h < min)
                    {
                        min = h;
                    }

                    if (h > max)
                    {
                        max = h;
                    }
                }

                minMax[0] = min;
                minMax[1] = max;
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
        private struct ResampleHeightmapJob : IJobParallelForBatch
        {
            [ReadOnly] public NativeArray<float> src;
            public int srcW;
            public int srcH;
            public int newW;
            public int newH;

            [WriteOnly] public NativeArray<float> dst;

            public void Execute(int startIndex, int count)
            {
                float scaleX = (srcW - 1f) / (newW - 1f);
                float scaleY = (srcH - 1f) / (newH - 1f);

                for (int i = startIndex; i < startIndex + count; i++)
                {
                    int x = i % newW;
                    int y = i / newW;

                    float gx = x * scaleX;
                    float gy = y * scaleY;

                    int x0 = (int)gx;
                    int x1 = math.min(x0 + 1, srcW - 1);
                    int y0 = (int)gy;
                    int y1 = math.min(y0 + 1, srcH - 1);

                    float tx = gx - x0;
                    float ty = gy - y0;

                    float a = src[y0 * srcW + x0];
                    float b = src[y0 * srcW + x1];
                    float c = src[y1 * srcW + x0];
                    float d = src[y1 * srcW + x1];

                    float ab = math.lerp(a, b, tx);
                    float cd = math.lerp(c, d, tx);
                    dst[i] = math.lerp(ab, cd, ty);
                }
            }
        }
        #endregion

        #region METHODS
        internal void Load(FlightData flightData)
        {
            //Retrieve and validate tile data
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

            //Flatten all heightmaps into one array for min/max scan
            NativeArray<float> heightArray = new NativeArray<float>(tiles.Count * resTile * resTile, Allocator.TempJob);
            int index = 0;
            foreach (var tile in tiles)
            {
                float[,] src = tile.HeightMap;
                for (int y = 0; y < resTile; y++)
                {
                    for (int x = 0; x < resTile; x++)
                    {
                        heightArray[index++] = src[x, y];
                    }
                }
            }

            //Scan min/max height values
            NativeArray<float> minMax = new NativeArray<float>(2, Allocator.TempJob);
            minMax[0] = float.MaxValue;
            minMax[1] = float.MinValue;

            MinMaxHeightJob minMaxJob = new MinMaxHeightJob
            {
                heights = heightArray,
                minMax = minMax
            };

            JobHandle minMaxHandle = minMaxJob.Schedule();
            minMaxHandle.Complete();

            float minHVal = minMax[0];
            float maxHVal = minMax[1];
            float heightRange = Mathf.Max(0.001f, maxHVal - minHVal);

            heightArray.Dispose();
            minMax.Dispose();

            //Merge all heightmaps into one flat array
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

            //Convert flat array to 2D heightmap
            float[,] merged = new float[totalH, totalW];
            for (int y = 0; y < totalH; y++)
            {
                for (int x = 0; x < totalW; x++)
                {
                    merged[y, x] = mergedFlat[y * totalW + x];
                }
            }

            mergedFlat.Dispose();

            //Resample heightmap to next power-of-two resolution
            int longest = Mathf.Max(totalW, totalH);
            int terrainRes = Mathf.NextPowerOfTwo(longest - 1) + 1;

            if (terrainRes != totalW || terrainRes != totalH)
            {
                NativeArray<float> srcFlat = new NativeArray<float>(totalW * totalH, Allocator.TempJob);
                for (int y = 0; y < totalH; y++)
                {
                    for (int x = 0; x < totalW; x++)
                    {
                        srcFlat[y * totalW + x] = merged[y, x];
                    }
                }

                NativeArray<float> dstFlat = new NativeArray<float>(terrainRes * terrainRes, Allocator.TempJob);

                ResampleHeightmapJob job = new ResampleHeightmapJob
                {
                    src = srcFlat,
                    srcW = totalW,
                    srcH = totalH,
                    newW = terrainRes,
                    newH = terrainRes,
                    dst = dstFlat
                };

                JobHandle handle = job.ScheduleBatch(dstFlat.Length, 128);
                handle.Complete();

                float[,] resampled = new float[terrainRes, terrainRes];
                for (int y = 0; y < terrainRes; y++)
                {
                    for (int x = 0; x < terrainRes; x++)
                    {
                        resampled[y, x] = dstFlat[y * terrainRes + x];
                    }
                }

                srcFlat.Dispose();
                dstFlat.Dispose();
                merged = resampled;
                totalW = totalH = terrainRes;
            }

            //Compute terrain size in meters
            float tileSizeM = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);
            float sizeX = tilesX * tileSizeM * flightData.GlobalScale;
            float sizeZ = tilesY * tileSizeM * flightData.GlobalScale;
            float sizeY = heightRange * flightData.GlobalScale;

            //Create terrain data
            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = totalW,
                size = new Vector3(sizeX, sizeY, sizeZ)
            };
            terrainData.SetHeightsDelayLOD(0, 0, merged);

            terrainData.SyncHeightmap();

            //Instantiate terrain object
            _terrain = Terrain.CreateTerrainGameObject(terrainData);
            Terrain terrain = _terrain.GetComponent<Terrain>();
            _terrain.name = "Procedural Terrain";
            _terrain.transform.SetParent(transform, false);

            // Position terrain in world space
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

            //Merge satellite textures
            Texture2D globalSatellite = CombineSatelliteTiles(tiles);
            globalSatellite.filterMode = FilterMode.Trilinear;
            globalSatellite.anisoLevel = 2;

            //Setup terrain layers
            TerrainLayer terrainLayer = new TerrainLayer
            {
                diffuseTexture = globalSatellite,
                tileSize = new Vector2(terrainData.size.x, terrainData.size.z)
            };

            //Apply single layer
            terrain.terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };

            int alphaRes = terrain.terrainData.alphamapResolution;
            float[,,] maps = new float[alphaRes, alphaRes, 1];
            for (int y = 0; y < alphaRes; y++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    maps[y, x, 0] = 1.0f;
                }
            }
            terrain.terrainData.SetAlphamaps(0, 0, maps);

            //Cleanup satellite textures to free memory
            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                tile.SatelliteTexture = null;
            }
        }

        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                GameObject.Destroy(_terrain);
            });
        }

        private static Texture2D CombineSatelliteTiles(List<TileDefinition> tiles)
        {
            int minX = tiles.Min(t => t.X);
            int maxX = tiles.Max(t => t.X);
            int minY = tiles.Min(t => t.Y);
            int maxY = tiles.Max(t => t.Y);

            int tileCountX = maxX - minX + 1;
            int tileCountY = maxY - minY + 1;

            //Determine max tile size (largest zoom level tile)
            int maxTileW = tiles.Max(t => t.SatelliteTexture.width);
            int maxTileH = tiles.Max(t => t.SatelliteTexture.height);

            int atlasW = tileCountX * maxTileW;
            int atlasH = tileCountY * maxTileH;

            //Clamp to GPU max texture size
            int maxSize = SystemInfo.maxTextureSize;
            float scaleFactor = 1f;

            if (atlasW > maxSize || atlasH > maxSize)
            {
                float scaleX = (float)maxSize / atlasW;
                float scaleY = (float)maxSize / atlasH;
                scaleFactor = Mathf.Min(scaleX, scaleY);

                atlasW = Mathf.FloorToInt(atlasW * scaleFactor);
                atlasH = Mathf.FloorToInt(atlasH * scaleFactor);
            }

            //Create atlas RT (clamped)
            RenderTexture atlasRT = new RenderTexture(atlasW, atlasH, 0, RenderTextureFormat.ARGB32);
            atlasRT.Create();

            foreach (TileDefinition tile in tiles)
            {
                Texture2D src = tile.SatelliteTexture;
                if (src == null)
                {
                    continue;
                }

                int cellX = tile.X - minX;
                int cellY = maxY - tile.Y;

                int offsetX = Mathf.FloorToInt(cellX * maxTileW * scaleFactor);
                int offsetY = Mathf.FloorToInt(cellY * maxTileH * scaleFactor);

                int targetW = Mathf.FloorToInt(maxTileW * scaleFactor);
                int targetH = Mathf.FloorToInt(maxTileH * scaleFactor);

                //Upscale/downscale to target size
                RenderTexture tempRT = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(src, tempRT);

                Graphics.CopyTexture(tempRT, 0, 0, 0, 0, targetW, targetH, atlasRT, 0, 0, offsetX, offsetY);
                RenderTexture.ReleaseTemporary(tempRT);
            }

            //Convert to Texture2D
            Texture2D finalTex = new Texture2D(atlasW, atlasH, TextureFormat.RGB24, false);
            RenderTexture.active = atlasRT;
            finalTex.ReadPixels(new Rect(0, 0, atlasW, atlasH), 0, 0);
            finalTex.Apply();

            RenderTexture.active = null;
            atlasRT.Release();

            return finalTex;
        }
        #endregion
    }
}
