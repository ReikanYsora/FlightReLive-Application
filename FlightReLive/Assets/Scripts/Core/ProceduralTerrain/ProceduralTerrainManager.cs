using FlightReLive.Core.Database;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.ProceduralTerrain
{
    /// <summary>
    /// Procedural terrain management
    /// </summary>
    public class ProceduralTerrainManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Material _terrainMaterial;
        private List<Terrain> _terrains;
        private Bounds _terrainBounds;
        #endregion

        #region PROPERTIES
        internal static ProceduralTerrainManager Instance { get; private set; }

        internal Bounds TerrainBounds
        {
            get
            {
                return _terrainBounds;
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
            _terrains = new List<Terrain>();
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
            Dictionary<(int, int), Terrain> unityTerrains = new Dictionary<(int, int), Terrain>();

            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogError("No tiles available to build terrain.");
                return;
            }

            double tileSizeM = MapTools.GetTileSizeMeters(flightData.MapDefinition.OriginLatitude);
            float scale = flightData.WorldScale;
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
                terrainData.size = new Vector3(terrainSize, (float)(heightRange * (double)scale), terrainSize);
                terrainData.SetHeights(0, 0, normalized);

                GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = "Terrain_" + tile.X + "_" + tile.Y;
                terrainGO.transform.SetParent(transform, false);

                Terrain terrain = terrainGO.GetComponent<Terrain>();
                terrain.materialTemplate = _terrainMaterial;
                terrain.materialTemplate.enableInstancing = true;
                terrain.drawHeightmap = true;
                terrain.allowAutoConnect = true;
                terrain.drawTreesAndFoliage = false;
                terrain.drawInstanced = true;
                terrain.groupingID = 0;
                terrain.enabled = true;
                terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
                terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                float posX = (tile.X - minX) * terrainSize + centerOffsetX;
                float posZ = (maxY - tile.Y) * terrainSize + centerOffsetZ;
                float posY = (float)minH * scale;
                terrainGO.transform.localPosition = new Vector3(posX, posY, posZ);
                unityTerrains[(tile.X, tile.Y)] = terrain;

                //Layers
                Texture2D satelliteTexture = tile.SatelliteTexture;
                satelliteTexture.filterMode = FilterMode.Bilinear;

                TerrainLayer satelliteLayer = new TerrainLayer();
                satelliteLayer.diffuseTexture = satelliteTexture;
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
                _terrains.Add(terrain);
            }

            foreach (KeyValuePair<(int, int), Terrain> kvp in unityTerrains)
            {
                (int x, int y) = kvp.Key;
                Terrain current = kvp.Value;

                unityTerrains.TryGetValue((x - 1, y), out Terrain left);
                unityTerrains.TryGetValue((x, y + 1), out Terrain top);
                unityTerrains.TryGetValue((x + 1, y), out Terrain right);
                unityTerrains.TryGetValue((x, y - 1), out Terrain bottom);

                current.SetNeighbors(left, top, right, bottom);
            }

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                tile.SatelliteTexture = null;
            }

            //Calculate terrain global bounding box
            float minXWorld = float.MaxValue, maxXWorld = float.MinValue;
            float minYWorld = float.MaxValue, maxYWorld = float.MinValue;
            float minZWorld = float.MaxValue, maxZWorld = float.MinValue;

            foreach (Terrain terrain in _terrains)
            {
                Vector3 pos = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;

                minXWorld = Mathf.Min(minXWorld, pos.x);
                minYWorld = Mathf.Min(minYWorld, pos.y);
                minZWorld = Mathf.Min(minZWorld, pos.z);

                maxXWorld = Mathf.Max(maxXWorld, pos.x + size.x);
                maxYWorld = Mathf.Max(maxYWorld, pos.y + size.y);
                maxZWorld = Mathf.Max(maxZWorld, pos.z + size.z);
            }

            Vector3 center = new Vector3((minXWorld + maxXWorld) * 0.5f, (minYWorld + maxYWorld) * 0.5f, (minZWorld + maxZWorld) * 0.5f);
            Vector3 sizeBounds = new Vector3((maxXWorld - minXWorld), (maxYWorld - minYWorld), (maxZWorld - minZWorld));

            _terrainBounds = new Bounds(center, sizeBounds);
        }

        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                for (int i = _terrains.Count - 1; i >= 0; i--)
                {
                    GameObject.Destroy(_terrains[i].gameObject);
                }

                _terrains.Clear();
            });
        }
        #endregion
    }
}
