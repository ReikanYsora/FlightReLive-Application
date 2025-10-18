using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Workspace;
using Fu;
using Fu.Framework;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Cache
{
    public static class CacheManager
    {
        #region CONSTANTS
        private const string CACHE_FOLDER_NAME = "Cache";
        private const string WORKSPACE_CACHE_FOLDER_NAME = "Workspace";
        #endregion

        #region ATTRIBUTES
        private static string _cacheFolder;
        private static string _flightsCacheFolder;
        private static readonly MessagePackSerializerOptions _messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(StandardResolverAllowPrivate.Instance).WithCompression(MessagePackCompression.Lz4BlockArray);
        #endregion

        #region METHODS
        /// <summary>
        /// Initialize cache folder
        /// </summary>
        internal static void InitializeCache()
        {
            _cacheFolder = Path.Combine(Application.persistentDataPath, CACHE_FOLDER_NAME);

            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }
        }

        /// <summary>
        /// Initialize workspace cache for FlightFiles
        /// </summary>
        internal static void InitializeWorkspaceCache()
        {
            _flightsCacheFolder = Path.Combine(_cacheFolder, WORKSPACE_CACHE_FOLDER_NAME);

            if (!Directory.Exists(_flightsCacheFolder))
            {
                Directory.CreateDirectory(_flightsCacheFolder);
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <summary>
        /// Clear cache except WorkspaceCache folder
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
                    //We don't want to delete WorkspaceCache folder
                    foreach (string dir in Directory.GetDirectories(_cacheFolder))
                    {
                        if (!dir.Equals(_flightsCacheFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            Directory.Delete(dir, true);
                        }
                    }

                    foreach (string file in Directory.GetFiles(_cacheFolder))
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    Directory.CreateDirectory(_cacheFolder);
                }

                InitializeCache();
                InitializeWorkspaceCache();

                Fugui.Notify("Successful operation", "The local cache has been cleared successfully (workspace preserved).", StateType.Info, 3f);
            }
            catch (Exception ex)
            {
                Fugui.Notify("Operation failed", $"Unable to clear local cache.\n{ex.GetBaseException().Message}.", StateType.Danger, 3f);
            }
        }

        /// <summary>
        /// Clears the FlightFile workspace cache (.Flights folder)
        /// </summary>
        internal static void ClearWorkspaceCache()
        {
            try
            {
                if (Directory.Exists(_flightsCacheFolder))
                {
                    Directory.Delete(_flightsCacheFolder, true);
                }

                InitializeWorkspaceCache();

                Fugui.Notify("Successful operation", "The flight workspace cache has been cleared successfully.", StateType.Info, 3f);
            }
            catch (Exception ex)
            {
                Fugui.Notify("Operation failed", $"Unable to clear flight workspace cache.\n{ex.GetBaseException().Message}.", StateType.Danger, 3f);
            }
        }

        /// <summary>
        /// Generates a unique cache filename based on file name (not full path) + file size
        /// </summary>
        private static string ComputeFlightCacheKey(string fileName, long fileSize)
        {
            string input = fileName.ToLowerInvariant() + "_" + fileSize;

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        #region FLIGHT FILE METHODS (ASYNC)
        /// <summary>
        /// Returns full path for the cached FlightFile
        /// </summary>
        private static string GetFlightFileCachePath(string filePath, long fileSize)
        {
            string fileNameOnly = Path.GetFileName(filePath);
            string key = ComputeFlightCacheKey(fileNameOnly, fileSize);

            return Path.Combine(_flightsCacheFolder, $"{key}.mpack");
        }

        /// <summary>
        /// Check if a FlightFile exists in cache (by absolute path + file size)
        /// </summary>
        internal static Task<bool> FlightFileExistsAsync(string absolutePath, long fileSize)
        {
            string path = GetFlightFileCachePath(absolutePath, fileSize);

            return Task.FromResult(File.Exists(path));
        }

        /// <summary>
        /// Check if any cached FlightFile exists for a given video file name (not full path).
        /// Useful when checking duplicates with same name but different locations.
        /// </summary>
        internal static bool GetFlightFilePath(string fileNameOnly, out string cachePath)
        {
            cachePath = null;

            if (string.IsNullOrEmpty(fileNameOnly) || !Directory.Exists(_flightsCacheFolder))
            {
                return false;
            }

            string[] files = Directory.GetFiles(_flightsCacheFolder, "*.mpack");

            foreach (string f in files)
            {
                if (Path.GetFileName(f).Contains(fileNameOnly.ToLowerInvariant()))
                {
                    cachePath = f;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Save a FlightFile to cache
        /// </summary>
        internal static async Task SaveFlightFileAsync(FlightFile flightFile)
        {
            if (flightFile == null || string.IsNullOrEmpty(flightFile.VideoPath))
            {
                return;
            }

            try
            {
                FileInfo fi = new FileInfo(flightFile.VideoPath);

                if (!fi.Exists)
                {
                    return;
                }

                string savePath = GetFlightFileCachePath(flightFile.VideoPath, fi.Length);
                byte[] serialized = MessagePackSerializer.Serialize(flightFile, _messagePackOptions);

                await File.WriteAllBytesAsync(savePath, serialized);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CacheManager] Failed to save FlightFile cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a FlightFile from cache
        /// </summary>
        internal static async Task<FlightFile> LoadFlightFileAsync(string absolutePath, long fileSize)
        {
            try
            {
                string loadPath = GetFlightFileCachePath(absolutePath, fileSize);

                if (!File.Exists(loadPath))
                {
                    return null;
                }

                byte[] bytes = await File.ReadAllBytesAsync(loadPath);
                FlightFile file = MessagePackSerializer.Deserialize<FlightFile>(bytes, _messagePackOptions);

                return file;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CacheManager] Failed to load FlightFile cache: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region FLIGHT FILE IMPORT/EXPORT (ASYNC)
        /// <summary>
        /// Export a FlightFile to a .FRS file at the given path.
        /// </summary>
        internal static async Task<bool> ExportFlightFileAsync(FlightFile flightFile, string exportPath)
        {
            if (flightFile == null)
            {
                Debug.LogWarning("[CacheManager] Cannot export null FlightFile.");
                return false;
            }

            try
            {
                //Encode textures before saving (ensure byte[] are filled)
                flightFile.EncodeTextures();
                flightFile.VideoPath = string.Empty;
                flightFile.ErrorMessages = new List<string>();

                byte[] serialized = MessagePackSerializer.Serialize(flightFile, _messagePackOptions);
                string safePath = Path.ChangeExtension(exportPath, ".frs");

                using (FileStream fs = new FileStream(safePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await fs.WriteAsync(serialized, 0, serialized.Length);
                }

                Fugui.Notify("Successful operation", "The FRS file has been exported successfully.", StateType.Info, 3f);
                return true;
            }
            catch (Exception ex)
            {
                Fugui.Notify("Operation failed", $"Unable to export FRS filee.\n{ex.Message}.", StateType.Danger, 3f);
                Debug.LogWarning($"[CacheManager] Failed to export FlightFile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import a FlightFile from a .FRS file.
        /// </summary>
        internal static async Task<FlightFile> ImportFlightFileAsync(string importPath)
        {
            if (string.IsNullOrEmpty(importPath) || !File.Exists(importPath))
            {
                Debug.LogWarning($"[CacheManager] Import failed: invalid path {importPath}");
                return null;
            }

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(importPath);

                FlightFile file = MessagePackSerializer.Deserialize<FlightFile>(bytes, _messagePackOptions);

                // Rebuild textures from stored byte[] if needed
                file.DecodeTextures();
                return file;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CacheManager] Failed to import FlightFile: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region MAP ASYNC
        internal static Task<bool> MapTileExistsAsync(string mapStyle, int zoom, int tileX, int tileY)
        {
            string imagePath = GetMapTilePath(mapStyle, zoom, tileX, tileY);
            return Task.FromResult(File.Exists(imagePath));
        }

        internal static string GetMapTilePath(string mapStyle, int zoom, int tileX, int tileY)
        {
            string tileFile = $"m_{mapStyle}_{zoom}_{tileX}_{tileY}.raw";
            return Path.Combine(_cacheFolder, tileFile);
        }

        internal static async Task<Texture2D> LoadMapTileAsync(int tileSize, string mapStyle, int zoom, int tileX, int tileY)
        {
            if (!await MapTileExistsAsync(mapStyle, zoom, tileX, tileY))
            {
                return null;
            }

            string path = GetMapTilePath(mapStyle, zoom, tileX, tileY);

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
                texture.name = $"{mapStyle}_{zoom}_{tileX}_{tileY}";
                texture.filterMode = FilterMode.Trilinear;

                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load map tile {zoom}_{tileX}_{tileY} : {ex.Message}");
                return null;
            }
        }

        internal static async Task SaveMapTileAsync(Texture2D final, string mapStyle, int zoom, int tileX, int tileY)
        {
            if (final == null)
            {
                return;
            }

            string savePath = GetSatelliteTilePath(zoom, tileX, tileY);

            try
            {
                //Get raw bytes
                byte[] rawBytes = final.GetRawTextureData();

                using (FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await stream.WriteAsync(rawBytes, 0, rawBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save map tile {mapStyle}_{zoom}_{tileX}_{tileY} : {ex.Message}");
            }
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
        internal static Task<bool> BuildingExistsAsync(int zoom, int tileX, int tileY)
        {
            string path = GetBuildingPath(zoom, tileX, tileY);
            return Task.FromResult(File.Exists(path));
        }

        internal static string GetBuildingPath(int zoom, int tileX, int tileY)
        {
            string tileName = $"bld_{zoom}_{tileX}_{tileY}.mpack";
            return Path.Combine(_cacheFolder, tileName);
        }

        internal static async Task SaveBuildingAsync(List<BuildingFeature> features, int zoom, int tileX, int tileY)
        {
            if (features == null)
            {
                return;
            }

            string path = GetBuildingPath(zoom, tileX, tileY);

            try
            {
                byte[] serialized = MessagePackSerializer.Serialize(features, _messagePackOptions);
                await File.WriteAllBytesAsync(path, serialized);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save building data {zoom}_{tileX}_{tileY} : {ex.Message}");
            }
        }

        internal static async Task<List<BuildingFeature>> LoadBuildingAsync(int zoom, int tileX, int tileY)
        {
            if (!await BuildingExistsAsync(zoom, tileX, tileY))
            {
                return null;
            }

            string path = GetBuildingPath(zoom, tileX, tileY);

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                return MessagePackSerializer.Deserialize<List<BuildingFeature>>(bytes, _messagePackOptions);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load building data {zoom}_{tileX}_{tileY} : {ex.Message}");
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
            string tileName = $"geo_tile_{tileX}_{tileY}_{safeLang}.frlg";
            return Path.Combine(_cacheFolder, tileName);
        }

        internal static async Task SaveGeoTileDataAsync(FeatureCollection geoData, int tileX, int tileY, string lang)
        {
            if (geoData == null || geoData.features == null)
            {
                return;
            }

            string path = GetGeoTileDataPath(tileX, tileY, lang);

            try
            {
                string json = JsonConvert.SerializeObject(geoData);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save geo tile data {tileX}_{tileY}_{lang} : {ex.Message}");
            }
        }

        internal static async Task<FeatureCollection> LoadGeoTileDataAsync(int tileX, int tileY, string lang)
        {
            if (!await GeoTileDataExistsAsync(tileX, tileY, lang))
            {
                return null;
            }

            string path = GetGeoTileDataPath(tileX, tileY, lang);

            try
            {
                string json = await File.ReadAllTextAsync(path);
                return JsonConvert.DeserializeObject<FeatureCollection>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load geo tile data {tileX}_{tileY}_{lang} : {ex.Message}");
                return null;
            }
        }
        #endregion
        #endregion
    }
}
