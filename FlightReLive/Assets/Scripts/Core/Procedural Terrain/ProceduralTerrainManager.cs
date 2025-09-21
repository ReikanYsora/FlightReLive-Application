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
        [SerializeField] private Terrain _terrain;
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
            _terrain.gameObject.SetActive(false);
        }
        #endregion

        #region METHODS
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

            //Adjust tile size to match Unity's terrain compression
            float correctedTileSize = (float)(tileSizeM * ((resTile - 1.0) / resTile));
            float terrainSize = correctedTileSize * scale;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (TileDefinition tile in tiles)
            {
                if (tile.X < minX)
                { 
                    minX = tile.X;
                }

                if (tile.X > maxX)
                {
                    maxX = tile.X; 
                }

                if (tile.Y < minY)
                { 
                    minY = tile.Y;
                }

                if (tile.Y > maxY)
                { 
                    maxY = tile.Y;
                }
            }

            int tilesX = (maxX - minX) + 1;
            int tilesY = (maxY - minY) + 1;

            float centerOffsetX = -(tilesX * terrainSize) * 0.5f;
            float centerOffsetZ = -(tilesY * terrainSize) * 0.5f;

            //Scan global min/max height across all tiles
            float minH = float.MaxValue;
            float maxH = float.MinValue;

            foreach (TileDefinition tile in tiles)
            {
                float[,] src = tile.HeightMap;

                for (int y = 0; y < resTile; y++)
                {
                    for (int x = 0; x < resTile; x++)
                    {
                        float h = src[x, y];

                        if (h < minH)
                        {
                            minH = h;
                        }

                        if (h > maxH)
                        {
                            maxH = h;
                        }
                    }
                }
            }

            float heightRange = Mathf.Max(0.001f, maxH - minH);

            foreach (TileDefinition tile in tiles)
            {
                //Unity expects (resTile + 1) resolution for terrain heightmap
                int unityRes = resTile + 1;
                float[,] normalized = new float[unityRes, unityRes];

                for (int y = 0; y < unityRes; y++)
                {
                    int srcY = Mathf.Min(y, resTile - 1);
                    int flippedY = unityRes - 1 - y;

                    for (int x = 0; x < unityRes; x++)
                    {
                        int srcX = Mathf.Min(x, resTile - 1);
                        float rawHeight = tile.HeightMap[srcX, srcY];
                        normalized[flippedY, x] = (rawHeight - minH) / heightRange;
                    }
                }

                TerrainData terrainData = new TerrainData();
                terrainData.heightmapResolution = unityRes;
                terrainData.size = new Vector3(terrainSize, heightRange * scale, terrainSize);
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
                float posY = minH * scale;

                terrainGO.transform.localPosition = new Vector3(posX, posY, posZ);

                //Apply satellite texture
                Texture2D tex = tile.SatelliteTexture;
                tex.filterMode = FilterMode.Trilinear;
                tex.anisoLevel = 2;

                TerrainLayer layer = new TerrainLayer();
                layer.diffuseTexture = tex;
                layer.tileSize = new Vector2(terrainSize, terrainSize);
                layer.maskMapTexture = armTexture;

                terrainData.terrainLayers = new TerrainLayer[] { layer };

                //Fill alpha map with full opacity
                int alphaRes = terrainData.alphamapResolution;
                float[,,] alpha = new float[alphaRes, alphaRes, 1];

                for (int y = 0; y < alphaRes; y++)
                {
                    for (int x = 0; x < alphaRes; x++)
                    {
                        alpha[y, x, 0] = 1.0f;
                    }
                }

                terrainData.SetAlphamaps(0, 0, alpha);
            }

            //Release satellite textures from memory
            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                tile.SatelliteTexture = null;
            }
        }

        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                _terrain.gameObject.SetActive(false);
                _terrain.drawHeightmap = false;
                _terrain.drawTreesAndFoliage = false;
                _terrain.enabled = false;
                _terrain.terrainData = new TerrainData();
            });
        }
        
        /// <summary>
         /// Create a single ARM texture for all terrain layers.
         /// AO = 0.9, Smoothness = 0 (so Roughness = 1), Metallic = 0
         /// </summary>
        private Texture2D CreateARMTexture(int size = 4)
        {
            // Small texture (4x4) since it is uniform, no need for full res
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color arm = new Color(0.9f, 1.0f, 0.0f, 1.0f);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = arm;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return tex;
        }
        #endregion
    }
}
