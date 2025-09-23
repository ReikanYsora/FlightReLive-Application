using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using Fu;
using Fu.Framework;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Cache
{
    public static class CacheManager
    {
        #region CONSTANTS
        private const string CACHE_FOLDER_NAME = "Cache";
        #endregion

        #region ATTRIBUTES
        private static string _cacheFolder;
        private static string _workspaceCacheFolder;
        #endregion

        #region METHODS
        /// <summary>
        /// Initialize cache folder
        /// </summary>
        internal static void Initialize()
        {
            _cacheFolder = Path.Combine(Application.persistentDataPath, CACHE_FOLDER_NAME);

            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        internal static void ClearCache()
        {
            if (string.IsNullOrEmpty(_cacheFolder))
            {
                return;
            }

            try
            {
                if (Directory.Exists(_cacheFolder))
                {
                    Directory.Delete(_cacheFolder, true);
                    Directory.CreateDirectory(_cacheFolder);
                }

                Fugui.Notify("Successful operation", "The local cache has been cleared successfully.", StateType.Info);
            }
            catch (Exception ex)
            {
                Fugui.Notify("Operation failed", $"Unable to clear local cache.\n{ex.GetBaseException().Message}.", StateType.Danger);
            }
        }

        #region SATELLITE TILE METHODS (ASYNC)
        internal static Task<bool> SatelliteTileExistsAsync(int zoom, int tileX, int tileY)
        {
            string imagePath = GetSatelliteTilePath(zoom, tileX, tileY);
            return Task.FromResult(File.Exists(imagePath));
        }

        internal static string GetSatelliteTilePath(int zoom, int tileX, int tileY)
        {
            string tileFile = $"s_{zoom}_{tileX}_{tileY}.raw";
            return Path.Combine(_cacheFolder, tileFile);
        }

        /// <summary>
        /// Save a satellite tile in RAW format (much faster than PNG).
        /// </summary>
        internal static async Task SaveSatelliteTileAsync(Texture2D tex, int zoom, int tileX, int tileY)
        {
            if (tex == null)
            {
                return;
            }

            string savePath = GetSatelliteTilePath(zoom, tileX, tileY);

            try
            {
                //Get raw bytes
                byte[] rawBytes = tex.GetRawTextureData();

                using (FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await stream.WriteAsync(rawBytes, 0, rawBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save satellite tile {zoom}_{tileX}_{tileY} : {ex.Message}");
            }
        }

        /// <summary>
        /// Load a satellite tile from RAW format.
        /// </summary>
        internal static async Task<Texture2D> LoadSatelliteTileAsync(int tileSize, int zoom, int tileX, int tileY)
        {
            if (!await SatelliteTileExistsAsync(zoom, tileX, tileY))
            {
                return null;
            }

            string path = GetSatelliteTilePath(zoom, tileX, tileY);

            try
            {
                byte[] rawData;
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    rawData = new byte[stream.Length];
                    int read = 0;

                    while (read < rawData.Length)
                    {
                        int r = await stream.ReadAsync(rawData, read, rawData.Length - read);

                        if (r == 0)
                        {
                            break;
                        }

                        read += r;
                    }
                }

                Texture2D texture = new Texture2D(tileSize, tileSize, TextureFormat.RGB24, false);
                texture.LoadRawTextureData(rawData);
                texture.Apply(false, false);
                texture.name = $"{zoom}_{tileX}_{tileY}";
                texture.filterMode = FilterMode.Trilinear;

                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load satellite tile {zoom}_{tileX}_{tileY} : {ex.Message}");
                return null;
            }
        }
        #endregion

        #region HEIGHTMAP TILE METHODS (ASYNC)
        internal static Task<bool> HeightmapExistsAsync(int tileX, int tileY)
        {
            return Task.FromResult(File.Exists(GetHeightmapPath(tileX, tileY)));
        }

        internal static string GetHeightmapPath(int tileX, int tileY)
        {
            string baseName = $"h_{tileX}_{tileY}";

            return Path.Combine(_cacheFolder, baseName + ".raw");
        }

        internal static async Task SaveHeightmapAsync(float[,] tileHeightmap, int tileX, int tileY)
        {
            string path = GetHeightmapPath(tileX, tileY);

            try
            {
                int w = tileHeightmap.GetLength(0);
                int h = tileHeightmap.GetLength(1);
                int length = w * h;

                byte[] buffer = new byte[length * sizeof(float)];
                Buffer.BlockCopy(tileHeightmap, 0, buffer, 0, buffer.Length);

                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save heightmap {tileX}_{tileY} : {ex.Message}");
            }
        }

        internal static async Task<float[,]> LoadHeightmapAsync(int tileX, int tileY, int resolution = 512)
        {
            string path = GetHeightmapPath(tileX, tileY);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                byte[] buffer;

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, buffer.Length);
                }

                float[,] map = new float[resolution, resolution];
                Buffer.BlockCopy(buffer, 0, map, 0, buffer.Length);

                return map;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load heightmap {tileX}_{tileY} : {ex.Message}");
                return null;
            }
        }
        #endregion

        #region BUILDINGS (ASYNC)
        internal static Task<bool> BuildingTileDataExistsAsync(int zoom, int tileX, int tileY)
        {
            string path = GetBuildingTileDataPath(zoom, tileX, tileY);
            return Task.FromResult(File.Exists(path));
        }


        internal static string GetBuildingTileDataPath(int zoom, int tileX, int tileY)
        {
            string tileName = $"b_{zoom}_{tileX}_{tileY}.mpack";

            return Path.Combine(_cacheFolder, tileName);
        }


        internal static async Task SaveBuildingTileDataAsync(List<BuildingData> buildings, int zoom, int tileX, int tileY)
        {
            if (buildings == null)
            {
                return;
            }

            string path = GetBuildingTileDataPath(zoom, tileX, tileY);

            try
            {
                byte[] serialized = MessagePackSerializer.Serialize(buildings);
                await File.WriteAllBytesAsync(path, serialized);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save building tile data {zoom}_{tileX}_{tileY} : {ex.Message}");
            }
        }

        internal static async Task<List<BuildingData>> LoadBuildingTileDataAsync(int zoom, int tileX, int tileY)
        {
            if (!await BuildingTileDataExistsAsync(zoom, tileX, tileY))
            {
                return null;
            }

            string path = GetBuildingTileDataPath(zoom, tileX, tileY);

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                return MessagePackSerializer.Deserialize<List<BuildingData>>(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load building tile data {zoom}_{tileX}_{tileY} : {ex.Message}");
                return null;
            }
        }
        #endregion

        #region GEODATA (ASYNC)
        internal static Task<bool> GeoTileDataExistsAsync(int tileX, int tileY, string lang)
        {
            string path = GetGeoTileDataPath(tileX, tileY, lang);
            return Task.FromResult(File.Exists(path));
        }

        internal static string GetGeoTileDataPath(int tileX, int tileY, string lang)
        {
            string safeLang = string.IsNullOrEmpty(lang) ? "en" : lang;
            string tileName = $"g_{tileX}_{tileY}_{safeLang}.mpack";
            return Path.Combine(_cacheFolder, tileName);
        }

        internal static async Task SaveGeoTileDataAsync(FeatureCollection geoData, int tileX, int tileY, string lang)
        {
            if (geoData == null)
            {
                return;
            }

            string path = GetGeoTileDataPath(tileX, tileY, lang);

            try
            {
                byte[] serialized = MessagePackSerializer.Serialize(geoData);
                await File.WriteAllBytesAsync(path, serialized);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save geo tile {tileX}_{tileY}_{lang} : {ex.Message}");
            }
        }

        internal static async Task<FeatureCollection> LoadGeoTileDataAsync(int tileX, int tileY, string lang)
        {
            string path = GetGeoTileDataPath(tileX, tileY, lang);

            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                return MessagePackSerializer.Deserialize<FeatureCollection>(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load geo tile {tileX}_{tileY}_{lang} : {ex.Message}");
                return null;
            }
        }
        #endregion
    }
    #endregion
}
