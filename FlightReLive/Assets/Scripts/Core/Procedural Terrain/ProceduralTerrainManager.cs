using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.ProceduralTerrain
{
    public class ProceduralTerrainManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private List<GameObject> _terrainsList;
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

            _terrainsList = new List<GameObject>();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Build Unity Terrain tiles from FlightReLive data.
        /// Border samples are taken from neighbor tiles (right / bottom) instead of duplicating,
        /// ensuring perfect continuity without stitching.
        /// </summary>
        internal void Load(FlightData flightData)
        {
            List<TileDefinition> tiles = flightData.MapDefinition.GetSortedTiles();
            Texture2D armTexture = CreateARMTexture();

            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogError("No tiles available to build terrain.");
                return;
            }

            double tileSizeM = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);
            float scale = flightData.GlobalScale;
            int resTile = tiles[0].HeightMap.GetLength(0);

            // Adjust tile size to match Unity's terrain compression
            float correctedTileSize = (float)(tileSizeM * ((resTile - 1.0) / resTile));
            float terrainSize = correctedTileSize * scale;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (TileDefinition tile in tiles)
            {
                if (tile.X < minX) { minX = tile.X; }
                if (tile.X > maxX) { maxX = tile.X; }
                if (tile.Y < minY) { minY = tile.Y; }
                if (tile.Y > maxY) { maxY = tile.Y; }
            }

            int tilesX = (maxX - minX) + 1;
            int tilesY = (maxY - minY) + 1;

            float centerOffsetX = -(tilesX * terrainSize) * 0.5f;
            float centerOffsetZ = -(tilesY * terrainSize) * 0.5f;

            Dictionary<(int, int), TileDefinition> tileMap = new Dictionary<(int, int), TileDefinition>();
            foreach (TileDefinition t in tiles)
            {
                tileMap[(t.X, t.Y)] = t;
            }

            double minH = double.MaxValue;
            double maxH = double.MinValue;

            foreach (TileDefinition tile in tiles)
            {
                float[,] src = tile.HeightMap;
                for (int y = 0; y < resTile; y++)
                {
                    for (int x = 0; x < resTile; x++)
                    {
                        double h = (double)src[x, y];
                        if (h < minH) { minH = h; }
                        if (h > maxH) { maxH = h; }
                    }
                }
            }

            double heightRange = Math.Max(0.001, maxH - minH);

            foreach (TileDefinition tile in tiles)
            {
                int unityRes = resTile + 1;
                float[,] normalized = new float[unityRes, unityRes];

                for (int y = 0; y < unityRes; y++)
                {
                    int srcY = Mathf.Min(y, resTile - 1);
                    int flippedY = unityRes - 1 - y;

                    for (int x = 0; x < unityRes; x++)
                    {
                        int srcX = Mathf.Min(x, resTile - 1);
                        float rawHeight;

                        if (x == resTile && tileMap.TryGetValue((tile.X + 1, tile.Y), out TileDefinition right))
                        {
                            rawHeight = right.HeightMap[0, srcY];
                        }
                        else if (y == resTile && tileMap.TryGetValue((tile.X, tile.Y + 1), out TileDefinition bottom))
                        {
                            rawHeight = bottom.HeightMap[srcX, 0];
                        }
                        else if (x == resTile && y == resTile &&
                                 tileMap.TryGetValue((tile.X + 1, tile.Y + 1), out TileDefinition bottomRight))
                        {
                            rawHeight = bottomRight.HeightMap[0, 0];
                        }
                        else
                        {
                            rawHeight = tile.HeightMap[srcX, srcY];
                        }

                        normalized[flippedY, x] = (float)(((double)rawHeight - minH) / heightRange);
                    }
                }

                TerrainData terrainData = new TerrainData();
                terrainData.heightmapResolution = unityRes;
                terrainData.size = new Vector3(
                    terrainSize,
                    (float)(heightRange * (double)scale),
                    terrainSize
                );
                terrainData.SetHeights(0, 0, normalized);

                GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = "Terrain_" + tile.X + "_" + tile.Y;
                terrainGO.transform.SetParent(transform, false);

                Terrain terrain = terrainGO.GetComponent<Terrain>();
                terrain.drawHeightmap = true;
                terrain.drawTreesAndFoliage = false;
                terrain.enabled = true;
                terrain.allowAutoConnect = true;
                terrain.groupingID = 0;

                float posX = (tile.X - minX) * terrainSize + centerOffsetX;
                float posZ = (maxY - tile.Y) * terrainSize + centerOffsetZ;
                float posY = (float)minH * scale;
                terrainGO.transform.localPosition = new Vector3(posX, posY, posZ);

                //Layers
                Texture2D tex = tile.SatelliteTexture;
                tex.filterMode = FilterMode.Bilinear;

                TerrainLayer satelliteLayer = new TerrainLayer();
                satelliteLayer.diffuseTexture = tex;
                satelliteLayer.maskMapTexture = armTexture;
                satelliteLayer.tileSize = new Vector2(terrainSize, terrainSize);

                terrainData.terrainLayers = new TerrainLayer[] { satelliteLayer };

                //Blend
                int alphaRes = terrainData.alphamapResolution;
                float[,,] alpha = new float[alphaRes, alphaRes, 1];

                for (int ay = 0; ay < alphaRes; ay++)
                {
                    for (int ax = 0; ax < alphaRes; ax++)
                    {
                        alpha[ay, ax, 0] = 1f;
                    }
                }

                terrainData.SetAlphamaps(0, 0, alpha);
                _terrainsList.Add(terrainGO);
            }

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                tile.SatelliteTexture = null;
            }
        }

        private Texture2D CreateARMTexture(int size = 4)
        {
            //Small texture (4x4) since it is uniform, no need for full res
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            Color arm = new Color(0.3f, 0.2f, 0.1f, 1.0f);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = arm;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return tex;
        }

        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                for (int i = _terrainsList.Count - 1; i >= 0; i--)
                {
                    GameObject.Destroy(_terrainsList[i]);
                }
            });
        }
        #endregion
    }
}
