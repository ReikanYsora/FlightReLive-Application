using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.ProceduralTerrain
{
    /// <summary>
    /// Procedural terrain management
    /// </summary>
    [RequireComponent(typeof(ProceduralTreeManager))]
    public class ProceduralTerrainManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private List<Terrain> _terrains;

        [Header("Material Settings")]
        [SerializeField] private Material _hdrpTerrainMaterial;
        #endregion

        #region PROPERTIES
        internal static ProceduralTerrainManager Instance { get; private set; }

        internal ProceduralTreeManager TreeManager { get; private set; }
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

            TreeManager = GetComponent<ProceduralTreeManager>();
            _terrains = new List<Terrain>();
        }

        private void Start()
        {
            SettingsManager.OnTreeVisibilityChanged += OnTreeVisibilityChanged;
            SettingsManager.OnTreeQualityChanged += OnTreeQualityChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnTreeVisibilityChanged -= OnTreeVisibilityChanged;
            SettingsManager.OnTreeQualityChanged -= OnTreeQualityChanged;
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
            Dictionary<(int, int), Terrain> unityTerrains = new Dictionary<(int, int), Terrain>();

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
                terrainData.size = new Vector3(terrainSize, (float)(heightRange * (double)scale), terrainSize);
                terrainData.SetHeights(0, 0, normalized);

                GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = "Terrain_" + tile.X + "_" + tile.Y;
                terrainGO.transform.SetParent(transform, false);

                Terrain terrain = terrainGO.GetComponent<Terrain>();
                terrain.materialTemplate = _hdrpTerrainMaterial;
                terrain.drawHeightmap = true;
                terrain.allowAutoConnect = true;
                terrain.groupingID = 0;
                terrain.enabled = true;

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

            //Terrain is now ready, we can now start to paint tree
            ApplyTreeQualitySettings();
            TreeManager.LoadTrees(flightData, _terrains);
        }

        private Texture2D CreateARMTexture(int size = 64)
        {
            //Small texture (4x4) since it is uniform, no need for full res
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            Color arm = new Color(0f, 0f, 0f, 0f);
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
                for (int i = _terrains.Count - 1; i >= 0; i--)
                {
                    GameObject.Destroy(_terrains[i].gameObject);
                }

                _terrains.Clear();
            });
        }

        /// <summary>
        /// Apply terrain tree quality settings (distance, LODs, billboard, fade length).
        /// Values are tuned for Low → Ultra quality levels.
        /// </summary>
        private void ApplyTreeQualitySettings()
        {
            Settings.QualitySettings qualitySettings = SettingsManager.CurrentSettings.TreeQuality;

            foreach (Terrain terrain in _terrains)
            {
                terrain.drawTreesAndFoliage = SettingsManager.CurrentSettings.TreeVisibility;

                switch (qualitySettings)
                {
                    case Settings.QualitySettings.Low:
                        terrain.treeDistance = 250f;
                        terrain.treeBillboardDistance = 150f;
                        terrain.treeCrossFadeLength = 5f;
                        terrain.treeMaximumFullLODCount = 200;
                        break;
                    default:
                    case Settings.QualitySettings.Normal:
                        terrain.treeDistance = 1000f;
                        terrain.treeBillboardDistance = 400f;
                        terrain.treeCrossFadeLength = 20f;
                        terrain.treeMaximumFullLODCount = 5000;
                        break;

                    case Settings.QualitySettings.High:
                        terrain.treeDistance = 2000f;
                        terrain.treeBillboardDistance = 800f;
                        terrain.treeCrossFadeLength = 50f;
                        terrain.treeMaximumFullLODCount = 20000;
                        break;
                }
            }
        }
        #endregion

        #region CALLBACKS
        private void OnTreeVisibilityChanged(bool visibility)
        {
            bool treesEnabled = SettingsManager.CurrentSettings.TreeVisibility;

            foreach (Terrain tempTerrain in _terrains)
            {
                tempTerrain.drawTreesAndFoliage = treesEnabled;
            }
        }

        private void OnTreeQualityChanged(Settings.QualitySettings obj)
        {
            ApplyTreeQualitySettings();
        }

        #endregion

        #region UI
        internal void DisplayTreeSettings(FuGrid grid)
        {
            bool treesEnabled = SettingsManager.CurrentSettings.TreeVisibility;
            grid.EnableNextElements();

            if (grid.Toggle("Display trees", ref treesEnabled))
            {
                SettingsManager.SaveTreeVisibility(treesEnabled);
            }

            if (!treesEnabled)
            {
                grid.DisableNextElement();
            }

            grid.ButtonsGroup<Settings.QualitySettings>("Tree quality", (int newValue) =>
            {
                Settings.QualitySettings quality = (Settings.QualitySettings)newValue;
                SettingsManager.SaveTreeQuality(quality);
            }, () => SettingsManager.CurrentSettings.TreeQuality);
        }
        #endregion
    }
}
