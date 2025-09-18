using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using System.Collections.Generic;
using System.Linq;
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

        #region METHODS
        internal void GenerateTerrain(FlightData flightData)
        {
            List<TileDefinition> tiles = flightData.MapDefinition.GetSortedTiles();
            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogError("No tiles available to build terrain.");
                return;
            }

            //Grid info
            int resTile = tiles[0].HeightMap.GetLength(0);
            int texTileW = tiles[0].SatelliteTexture.width;
            int texTileH = tiles[0].SatelliteTexture.height;

            int minX = tiles.Min(t => t.X);
            int maxX = tiles.Max(t => t.X);
            int minY = tiles.Min(t => t.Y);
            int maxY = tiles.Max(t => t.Y);
            int tilesX = (maxX - minX) + 1;
            int tilesY = (maxY - minY) + 1;

            //Final resolutions (shared borders = -1)
            int totalW = tilesX * (resTile - 1) + 1;
            int totalH = tilesY * (resTile - 1) + 1;
            int totalTexW = tilesX * (texTileW - 1) + 1;
            int totalTexH = tilesY * (texTileH - 1) + 1;

            //Altitude range
            float minHVal = float.MaxValue;
            float maxHVal = float.MinValue;
            foreach (TileDefinition t in tiles)
            {
                float[,] src = t.HeightMap;
                int sx = src.GetLength(0);
                int sy = src.GetLength(1);

                for (int y = 0; y < sy; y++)
                {
                    for (int x = 0; x < sx; x++)
                    {
                        float h = src[x, y];
                        if (h < minHVal) { minHVal = h; }
                        if (h > maxHVal) { maxHVal = h; }
                    }
                }
            }

            float heightRange = Mathf.Max(0.001f, maxHVal - minHVal);

            //Merge heightmaps
            float[,] merged = new float[totalH, totalW];

            foreach (TileDefinition tile in tiles)
            {
                int localX = (tile.X - minX) * (resTile - 1);
                int localY = (maxY - tile.Y) * (resTile - 1);

                float[,] src = tile.HeightMap;
                for (int y = 0; y < resTile; y++)
                {
                    int destRow = localY + (resTile - 1 - y);
                    for (int x = 0; x < resTile; x++)
                    {
                        int destCol = localX + x;
                        float normalized = (src[x, y] - minHVal) / heightRange;
                        merged[destRow, destCol] = normalized;
                    }
                }
            }

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

            //Process terrain
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

            //Create terrain data
            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = totalW,
                size = new Vector3(sizeX, sizeY, sizeZ)
            };
            terrainData.SetHeights(0, 0, merged);

            //Create terrain gameobject
            _terrain = Terrain.CreateTerrainGameObject(terrainData);
            Terrain terrain = _terrain.GetComponent<Terrain>();
            _terrain.name = "GlobalTerrain";
            _terrain.transform.SetParent(transform, false);

            //Position terrain
            float centerTileX = (minX + maxX) / 2.0f;
            float centerTileY = (minY + maxY) / 2.0f;
            float offsetX;

            if (tilesX % 2 == 0)
            {
                offsetX = -(centerTileX - minX + 1.0f) * tileSizeM * flightData.GlobalScale + (tileSizeM * 0.5f * flightData.GlobalScale);
            }
            else
            {
                offsetX = -(centerTileX - minX + 0.5f) * tileSizeM * flightData.GlobalScale;
            }

            float offsetZ;
            if (tilesY % 2 == 0)
            {
                offsetZ = (centerTileY - minY + 1.0f) * tileSizeM * flightData.GlobalScale - (tileSizeM * 0.5f * flightData.GlobalScale);
            }
            else
            {
                offsetZ = (centerTileY - minY + 0.5f) * tileSizeM * flightData.GlobalScale;
            }

            float offsetY = minHVal * flightData.GlobalScale;
            _terrain.transform.localPosition = new Vector3(offsetX, offsetY, -offsetZ);

            //Satellite
            TerrainLayer satelliteLayer = new TerrainLayer
            {
                diffuseTexture = globalSatellite,
                tileSize = new Vector2(terrainData.size.x, terrainData.size.z)
            };

            //Micro details
            TerrainLayer detailLayer = new TerrainLayer
            {
                diffuseTexture = Texture2D.blackTexture,   //Neutral
                normalMapTexture = _detailNormalMap,       //Normal map HDRP
                maskMapTexture = _detailMaskMap,           //Maskmap (AO/Rough/Metal)
                tileSize = new Vector2(100f, 100f)         //Tiling
            };

            terrain.terrainData.terrainLayers = new TerrainLayer[] { satelliteLayer, detailLayer };

            //Alphamap, blend 2 layers
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

        }

        /// <summary>
        /// Unload all tiles
        /// </summary>
        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                GameObject.Destroy(_terrain);
            });
        }

        /// <summary>
        /// Bilinear resample of a heightmap [rows,cols] into [newH,newW]
        /// </summary>
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
