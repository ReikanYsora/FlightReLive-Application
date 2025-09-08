using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using MessagePack;
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
        private const string CACHE_WORKSPACE_FOLDER_NAME = ".FlightReLive";
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
        /// Initialize workspace cache folder
        /// </summary>
        /// <param name="forceReload"></param>
        internal static void InitializeWorkspace(bool forceReload)
        {
            _workspaceCacheFolder = Path.Combine(SettingsManager.CurrentSettings.WorkspacePath, CACHE_WORKSPACE_FOLDER_NAME);

            if (forceReload && Directory.Exists(_workspaceCacheFolder))
            {
                Directory.Delete(_workspaceCacheFolder, true);
            }

            if (!Directory.Exists(_workspaceCacheFolder))
            {
                Directory.CreateDirectory(_workspaceCacheFolder);
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

        internal static void ClearWorkspaceCache()
        {
            if (string.IsNullOrEmpty(_workspaceCacheFolder))
            {
                return;
            }

            if (Directory.Exists(_workspaceCacheFolder))
            {
                Directory.Delete(_workspaceCacheFolder, true);
                Directory.CreateDirectory(_workspaceCacheFolder);
            }
        }

        #region VIDEO SRT FILE METHOD
        internal static Task<bool> VideoBinaryDataExistsAsync(string videoPath)
        {
            return Task.FromResult(File.Exists(GetVideoBinaryDataPath(videoPath)));
        }

        internal static string GetVideoBinaryDataPath(string videoPath)
        {
            string videoName = Path.GetFileNameWithoutExtension(videoPath);
            return Path.Combine(_workspaceCacheFolder, $"{videoName}.frl");
        }

        internal static async Task SaveVideoBinaryDataAsync(string videoPath, FlightDataContainer container)
        {
            string filePath = GetVideoBinaryDataPath(videoPath);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                byte[] serialized = MessagePackSerializer.Serialize(container);
                await File.WriteAllBytesAsync(filePath, serialized);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save video binary data {videoPath} : {ex.Message}");
            }
        }

        internal static async Task<FlightDataContainer> LoadVideoBinaryDataAsync(string videoPath)
        {
            if (!await VideoBinaryDataExistsAsync(videoPath))
            {
                return null;
            }

            string filePath = GetVideoBinaryDataPath(videoPath);
            byte[] bytes = File.ReadAllBytes(filePath);

            return MessagePackSerializer.Deserialize<FlightDataContainer>(bytes);
        }
        #endregion

        #region SATELLITE TILE METHODS (ASYNC)
        internal static Task<bool> SatelliteTileExistsAsync(int zoom, int tileX, int tileY)
        {
            string imagePath = GetSatelliteTilePath(zoom, tileX, tileY);
            return Task.FromResult(File.Exists(imagePath));
        }

        internal static string GetSatelliteTilePath(int zoom, int tileX, int tileY)
        {
            string tileName = $"{zoom}_{tileX}_{tileY}";
            string tileFile = $"{tileName}.png";

            return Path.Combine(_cacheFolder, tileFile);
        }

        internal static async Task SaveSatelliteTileAsync(byte[] pngBytes, int zoom, int tileX, int tileY)
        {
            if (pngBytes == null || pngBytes.Length == 0)
            {
                return;
            }

            string savePath = GetSatelliteTilePath(zoom, tileX, tileY);

            try
            {
                using (var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await stream.WriteAsync(pngBytes, 0, pngBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save satellite tile {zoom}_{tileX}_{tileY} : {ex.Message}");
            }
        }

        internal static async Task<Texture2D> LoadSatelliteTileTextureAsync(int zoom, int tileX, int tileY)
        {
            if (!await SatelliteTileExistsAsync(zoom, tileX, tileY))
            {
                return null;
            }

            string imagePath = GetSatelliteTilePath(zoom, tileX, tileY);

            try
            {
                byte[] imageData;
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    imageData = new byte[stream.Length];
                    await stream.ReadAsync(imageData, 0, imageData.Length);
                }

                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(imageData))
                {
                    texture.name = $"{zoom}_{tileX}_{tileY}";
                    texture.filterMode = FilterMode.Trilinear;
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load satellite tile {zoom}_{tileX}_{tileY} : {ex.Message}");
            }

            return null;
        }
        #endregion


        #region HEIGHTMAP TILE METHODS (ASYNC)
        internal static Task<bool> HeightmapExistsAsync(int tileX, int tileY)
        {
            return Task.FromResult(File.Exists(GetHeightmapPath(tileX, tileY)));
        }

        internal static string GetHeightmapPath(int tileX, int tileY)
        {
            string baseName = $"topographic_{tileX}_{tileY}";

            return Path.Combine(_cacheFolder, baseName + ".json");
        }

        internal static async Task SaveHeightmapAsync(float[,] tileHeightmap, int tileX, int tileY)
        {
            string filePath = GetHeightmapPath(tileX, tileY);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                string json = JsonHeightmapUtility.Serialize(tileHeightmap);

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    await writer.WriteAsync(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save heightmap {tileX}_{tileY} : {ex.Message}");
            }
        }

        internal static async Task<float[,]> LoadHeightmapAsync(int tileX, int tileY)
        {
            if (!await HeightmapExistsAsync(tileX, tileY))
            {
                return null;
            }

            string filePath = GetHeightmapPath(tileX, tileY);

            try
            {
                string json;
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    json = await reader.ReadToEndAsync();
                }

                return JsonHeightmapUtility.Deserialize(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load heightmap {tileX}_{tileY} : {ex.Message}");
                return null;
            }
        }
        #endregion

        #region HILLSHADE TILE METHODS (ASYNC)
        internal static Task<bool> HillShadeTileExistsAsync(int tileX, int tileY)
        {
            return Task.FromResult(File.Exists(GetHillShadeTilePath(tileX, tileY)));
        }

        internal static string GetHillShadeTilePath(int tileX, int tileY)
        {
            return Path.Combine(_cacheFolder, $"hillshade_14_{tileX}_{tileY}.png");
        }

        internal static async Task SaveHillShadeTileAsync(Texture2D texture, int tileX, int tileY)
        {
            if (texture == null)
            {
                return;
            }

            string savePath = GetHillShadeTilePath(tileX, tileY);

            try
            {
                byte[] pngBytes = texture.EncodeToPNG();
                using (var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await stream.WriteAsync(pngBytes, 0, pngBytes.Length);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save hillshade {tileX},{tileY} : {e.Message}");
            }
        }

        internal static async Task<Texture2D> LoadHillShadeTileAsync(int tileX, int tileY)
        {
            if (!await HillShadeTileExistsAsync(tileX, tileY))
            {
                return null;
            }

            string imagePath = GetHillShadeTilePath(tileX, tileY);

            try
            {
                byte[] imageData;
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    imageData = new byte[stream.Length];
                    await stream.ReadAsync(imageData, 0, imageData.Length);
                }

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (texture.LoadImage(imageData))
                {
                    texture.name = $"14_{tileX}_{tileY}";
                    texture.filterMode = FilterMode.Trilinear;
                    return texture;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load hillshade {tileX},{tileY} : {e.Message}");
            }

            return null;
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
            string tileName = $"building_tile_{zoom}_{tileX}_{tileY}.frlb";

            return Path.Combine(_cacheFolder, tileName);
        }


        internal static async Task SaveBuildingTileDataAsync(List<BuildingData> buildings, int zoom, int tileX, int tileY)
        {
            if (buildings == null || buildings.Count == 0)
            {
                return;
            }

            string path = GetBuildingTileDataPath(zoom, tileX, tileY);

            try
            {
                byte[] serialized = MessagePack.MessagePackSerializer.Serialize(buildings);
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
    }
    #endregion
}
