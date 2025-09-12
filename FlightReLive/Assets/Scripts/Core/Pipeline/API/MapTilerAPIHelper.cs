using FlightReLive.Core.Cache;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline.Download;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VexTile.Mapbox.VectorTile;
using VexTile.Mapbox.VectorTile.Geometry;


namespace FlightReLive.Core.Pipeline.API
{
    public static class MapTilerAPIHelper
    {
        #region CONSTANTS
        private const int MAX_CONCURRENT_REQUEST = 8;
        #endregion

        #region ATTRIBUTES
        public static int REQUEST_COUNT = 0;
        private static readonly HashSet<string> _supportedLanguages = new HashSet<string>
        {
            "fr", "en", "de", "it", "es", "ja", "zh", "pt", "ru"
        };
        #endregion

        #region METHODS
        #region CHECK MAPTILER
        internal static async Task<bool> IsMapTilerKeyValidAsync(string apiKey)
        {
            string testUrl = $"https://api.maptiler.com/tiles/satellite-v2/0/0/0.png?key={apiKey}";

            using UnityWebRequest uwr = UnityWebRequest.Head(testUrl);
            UnityWebRequestAsyncOperation operation = uwr.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            return uwr.result == UnityWebRequest.Result.Success && uwr.responseCode == 200;
        }
        #endregion


        #region SATELLITE TILES
        internal static async Task DownloadSatelliteTilesParallelAsync(FlightData flightData, int zoomLevelInside, int zoomLevelOutside, Action onTileDownloaded)
        {
            Dictionary<TileDefinition, HashSet<(int x, int y)>> tileMap = new();

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                int zoom = tile.TilePriority == TilePriority.Inside ? zoomLevelInside : zoomLevelOutside;
                HashSet<(int x, int y)> tileCoords = MapTools.GetTilesFromZoomLevel(tile, zoom);
                tileMap[tile] = tileCoords;
            }

            foreach (KeyValuePair<TileDefinition, HashSet<(int x, int y)>> kvp in tileMap)
            {
                TileDefinition tile = kvp.Key;
                HashSet<(int x, int y)> coords = kvp.Value;
                Dictionary<(int x, int y), Texture2D> downloadedTiles = new Dictionary<(int x, int y), Texture2D>();
                int zoom = tile.TilePriority == TilePriority.Inside ? zoomLevelInside : zoomLevelOutside;

                foreach (var (x, y) in coords)
                {
                    try
                    {
                        Texture2D texture = await DownloadSingleSatelliteTileAsync(x, y, zoom, onTileDownloaded);

                        if (texture != null)
                        {
                            downloadedTiles[(x, y)] = texture;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Satellite tile {zoom}/{x}/{y} failed: {ex.Message}");
                        onTileDownloaded?.Invoke();
                    }
                }

                tile.SatelliteTexture = CombinePNGTiles(downloadedTiles, zoom);
            }
        }

        private static async Task<Texture2D> DownloadSingleSatelliteTileAsync(int x, int y, int zoom, Action onTileDownloaded)
        {
            if (await CacheManager.SatelliteTileExistsAsync(zoom, x, y))
            {
                var cached = await CacheManager.LoadSatelliteTileTextureAsync(zoom, x, y);
                onTileDownloaded?.Invoke();
                return cached;
            }

            string url = $"https://api.maptiler.com/tiles/satellite-v2/{zoom}/{x}/{y}.png?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
            TaskCompletionSource<Texture2D> tcs = new TaskCompletionSource<Texture2D>();

            DownloadManager.EnqueueDownload(url,
                async data =>
                {
                    try
                    {
                        var texture = new Texture2D(2, 2);
                        if (texture.LoadImage(data))
                        {
                            await CacheManager.SaveSatelliteTileAsync(texture.EncodeToPNG(), zoom, x, y);
                            tcs.SetResult(texture);
                        }
                        else
                        {
                            Debug.LogWarning($"Satellite tile {zoom}/{x}/{y} failed to decode.");
                            tcs.SetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Exception while saving satellite tile {zoom}/{x}/{y}: {ex.Message}");
                        tcs.SetResult(null);
                    }

                    onTileDownloaded?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"Satellite tile {zoom}/{x}/{y} failed: {error}");
                    onTileDownloaded?.Invoke();
                    tcs.SetResult(null);
                });

            return await tcs.Task;
        }

        #endregion

        #region TOPOGRAPHIC TILES
        internal static async Task DownloadTopographicTilesParallelAsync(FlightData flightData, int zoomLevel, Action onTileDownloaded)
        {
            List<TileDefinition> tiles = flightData.MapDefinition.TileDefinitions;

            Queue<TileDefinition> queue = new Queue<TileDefinition>(tiles);
            List<Task> activeTasks = new List<Task>();

            while (queue.Count > 0 || activeTasks.Count > 0)
            {
                while (activeTasks.Count < MAX_CONCURRENT_REQUEST && queue.Count > 0)
                {
                    TileDefinition tile = queue.Dequeue();
                    Task task = DownloadTopographicTileAsync(tile, zoomLevel, onTileDownloaded);
                    activeTasks.Add(task);
                }

                Task finished = await Task.WhenAny(activeTasks);
                activeTasks.Remove(finished);
            }
        }

        private static async Task DownloadTopographicTileAsync(TileDefinition tile, int zoomLevel, Action onTileDownloaded)
        {
            if (await CacheManager.HeightmapExistsAsync(tile.X, tile.Y))
            {
                tile.HeightMap = await CacheManager.LoadHeightmapAsync(tile.X, tile.Y);
                onTileDownloaded?.Invoke();
                return;
            }

            string url = $"https://api.maptiler.com/tiles/terrain-rgb-v2/{zoomLevel}/{tile.X}/{tile.Y}.webp?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";

            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();

            DownloadManager.EnqueueDownload(url,
                data =>
                {
                    tcs.SetResult(data);
                },
                error =>
                {
                    Debug.LogWarning($"Topographic tile {tile.X}_{tile.Y} failed: {error}");
                    tcs.SetResult(null);
                });

            byte[] webpData = await tcs.Task;

            if (webpData == null)
            {
                Debug.LogWarning($"No data received for tile {tile.X}_{tile.Y}");
                onTileDownloaded?.Invoke();
                return;
            }

            int width = MapTools.TILE_RESOLUTION;
            int height = MapTools.TILE_RESOLUTION;
            Error errorCode;
            byte[] rawData;

            try
            {
                rawData = WebPDecoder.LoadRGBAFromWebP(webpData, ref width, ref height, false, out errorCode);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Exception while decoding WebP tile {tile.X}_{tile.Y}: {ex.Message}");
                onTileDownloaded?.Invoke();
                return;
            }

            if (errorCode != Error.Success || rawData == null)
            {
                Debug.LogWarning($"WebP decoding failed for tile {tile.X}_{tile.Y} (ErrorCode: {errorCode})");
                onTileDownloaded?.Invoke();
                return;
            }

            Texture2D textureWebP = new Texture2D(width, height, TextureFormat.RGBA32, false);
            textureWebP.LoadRawTextureData(rawData);
            textureWebP.Apply();

            float[,] heightMap = new float[width, height];
            Color[] pixels = textureWebP.GetPixels();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int flippedY = height - 1 - y;
                    Color pixel = pixels[flippedY * width + x];

                    float r = pixel.r * 255f;
                    float g = pixel.g * 255f;
                    float b = pixel.b * 255f;

                    float elevation = (r * 256f * 256f + g * 256f + b) * 0.1f - 10000f;
                    heightMap[x, y] = elevation;
                }
            }

            tile.HeightMap = heightMap;

            try
            {
                await CacheManager.SaveHeightmapAsync(heightMap, tile.X, tile.Y);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save heightmap for tile {tile.X}_{tile.Y}: {ex.Message}");
            }

            onTileDownloaded?.Invoke();
        }
        #endregion

        #region BUILDING TILES (Vector PBF)
        internal static async Task DownloadBuildingTilesParallelAsync(FlightData flightData, int zoomLevel, Action onTileDownloaded)
        {
            Dictionary<TileDefinition, HashSet<(int x, int y)>> tileMap = new();

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                HashSet<(int x, int y)> vectorTiles = MapTools.GetTilesFromZoomLevel(tile, zoomLevel);
                tileMap[tile] = vectorTiles;
            }

            foreach (var kvp in tileMap)
            {
                TileDefinition tile = kvp.Key;
                HashSet<(int x, int y)> vectorTiles = kvp.Value;
                List<BuildingData> buildings = new();

                foreach ((int x, int y) in vectorTiles)
                {
                    try
                    {
                        List<BuildingData> tileBuildings;

                        if (await CacheManager.BuildingTileDataExistsAsync(zoomLevel, x, y))
                        {
                            tileBuildings = await CacheManager.LoadBuildingTileDataAsync(zoomLevel, x, y);
                            onTileDownloaded?.Invoke();
                        }
                        else
                        {
                            tileBuildings = await DownloadAndParseBuildingTileAsync(x, y, zoomLevel, onTileDownloaded);

                            if (tileBuildings != null && tileBuildings.Count > 0)
                            {
                                await CacheManager.SaveBuildingTileDataAsync(tileBuildings, zoomLevel, x, y);
                            }
                        }

                        if (tileBuildings != null)
                        {
                            buildings.AddRange(tileBuildings);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Exception while parsing building tile {zoomLevel}/{x}/{y}: {ex.Message}");
                        onTileDownloaded?.Invoke();
                    }
                }

                tile.Buildings = buildings;
            }
        }

        private static async Task<List<BuildingData>> DownloadAndParseBuildingTileAsync(int x, int y, int zoom, Action onTileDownloaded)
        {
            string url = $"https://api.maptiler.com/tiles/v3-openmaptiles/{zoom}/{x}/{y}.pbf?key=" + SettingsManager.CurrentSettings.MapTilerAPIKey;

            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();

            DownloadManager.EnqueueDownload(url,
                data => tcs.SetResult(data),
                error =>
                {
                    Debug.LogWarning($"Building tile {zoom}/{x}/{y} failed: {error}");
                    tcs.SetResult(null);
                });

            byte[] tileData = await tcs.Task;

            if (tileData == null)
            {
                Debug.LogWarning($"No data received for building tile {zoom}/{x}/{y}");
                onTileDownloaded?.Invoke();
                return null;
            }

            List<BuildingData> buildings = new List<BuildingData>();

            try
            {
                VectorTileReader reader = new VectorTileReader(tileData);
                IReadOnlyCollection<string> layerNames = reader.LayerNames();

                foreach (string layerName in layerNames)
                {
                    if (layerName != "building")
                    {
                        continue;
                    }

                    VectorTileLayer layer = reader.GetLayer(layerName);
                    int featureCount = layer.FeatureCount();

                    for (int i = 0; i < featureCount; i++)
                    {
                        VectorTileFeature feature = layer.GetFeature(i);
                        Dictionary<string, object> props = feature.GetProperties();

                        float renderHeight = props.ContainsKey("render_height") ? ConvertToFloat(props["render_height"]) : 10.0f;
                        float renderMinHeight = props.ContainsKey("render_min_height") ? ConvertToFloat(props["render_min_height"]) : 0.0f;
                        float extrusionHeight = renderHeight - renderMinHeight;

                        List<List<Point2d<int>>> rawGeometry = feature.Geometry<int>();
                        List<List<SerializablePoint2D>> convertedGeometry = new List<List<SerializablePoint2D>>(rawGeometry.Count);

                        foreach (var ring in rawGeometry)
                        {
                            List<SerializablePoint2D> convertedRing = new List<SerializablePoint2D>(ring.Count);
                            foreach (var pt in ring)
                            {
                                convertedRing.Add(SerializablePoint2D.FromPoint2D(pt));
                            }
                            convertedGeometry.Add(convertedRing);
                        }

                        BuildingData building = new BuildingData
                        {
                            Geometry = convertedGeometry,
                            Height = extrusionHeight,
                            Properties = ConvertPropertiesToStringDictionary(props)
                        };

                        buildings.Add(building);
                    }
                }

                if (buildings.Count > 0)
                {
                    await CacheManager.SaveBuildingTileDataAsync(buildings, zoom, x, y);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error parsing PBF tile {zoom}/{x}/{y}: {ex.Message}");
            }

            onTileDownloaded?.Invoke();
            return buildings;
        }

        private static Dictionary<string, string> ConvertPropertiesToStringDictionary(Dictionary<string, object> props)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var kvp in props)
            {
                result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }

            return result;
        }

        private static float ConvertToFloat(object value)
        {
            if (value is float)
            {
                return (float)value;
            }

            if (value is double)
            {
                return (float)(double)value;
            }

            if (value is int)
            {
                return (float)(int)value;
            }

            string stringValue = value.ToString();

            float parsed;
            bool success = float.TryParse(stringValue, out parsed);

            if (success)
            {
                return parsed;
            }

            return 10.0f;
        }
        #endregion

        #region GEODATA
        internal static async Task DownloadGeoDataTilesParallelAsync(FlightData flightData, Action onTileDownloaded)
        {
            List<TileDefinition> tiles = flightData.MapDefinition.TileDefinitions;

            if (tiles.Count == 0)
            {
                Debug.LogWarning("No 'Inside' tiles found for GeoData.");
                return;
            }

            Queue<TileDefinition> queue = new Queue<TileDefinition>(tiles);
            List<Task> activeTasks = new List<Task>();

            while (queue.Count > 0 || activeTasks.Count > 0)
            {
                while (activeTasks.Count<MAX_CONCURRENT_REQUEST && queue.Count> 0)
                {
                    TileDefinition tile = queue.Dequeue();
                    Task task = DownloadGeoDataForTileAsync(tile, onTileDownloaded);
                    activeTasks.Add(task);
                }

                Task finished = await Task.WhenAny(activeTasks);
                activeTasks.Remove(finished);
            }
        }

        private static async Task DownloadGeoDataForTileAsync(TileDefinition tile, Action onTileDownloaded)
        {
            FlightGPSData center = MapTools.GetCenterOfBoundingBox(tile.BoundingBox);

            string url = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.maptiler.com/geocoding/{0},{1}.json?key={2}&bbox={3},{4},{5},{6}&language={7}",
                center.Longitude,
                center.Latitude,
                SettingsManager.CurrentSettings.MapTilerAPIKey,
                tile.BoundingBox.MinLongitude,
                tile.BoundingBox.MinLatitude,
                tile.BoundingBox.MaxLongitude,
                tile.BoundingBox.MaxLatitude,
                GetPreferredLanguage()
            );

            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            DownloadManager.EnqueueDownload(url,
                data =>
                {
                    string json = System.Text.Encoding.UTF8.GetString(data);
                    tcs.SetResult(json);
                },
                error =>
                {
                    Debug.LogWarning($"GeoData tile {tile.X}_{tile.Y} failed: {error}");
                    tcs.SetResult(null);
                });

            string jsonText = await tcs.Task;

            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogWarning($"GeoData tile {tile.X}_{tile.Y} returned empty response.");
                onTileDownloaded?.Invoke();
                return;
            }

            FeatureCollection geoData = null;

            try
            {
                geoData = JsonUtility.FromJson<FeatureCollection>(jsonText);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GeoJSON deserialization failed for tile {tile.X}_{tile.Y}: {ex.GetBaseException().Message}");
                onTileDownloaded?.Invoke();
                return;
            }

            if (geoData == null || geoData.features == null || geoData.features.Count == 0)
            {
                Debug.LogWarning($"GeoJSON tile {tile.X}_{tile.Y} returned no data.");
                onTileDownloaded?.Invoke();
                return;
            }

            tile.GeoData = geoData;
            onTileDownloaded?.Invoke();
        }
        #endregion

        #region COMMONS
        private static Texture2D CombinePNGTiles(Dictionary<(int x, int y), Texture2D> tiles, int zoom)
        {
            int tileSize = 512;
            int minX = tiles.Keys.Min(k => k.x);
            int maxX = tiles.Keys.Max(k => k.x);
            int minY = tiles.Keys.Min(k => k.y);
            int maxY = tiles.Keys.Max(k => k.y);

            int width = (maxX - minX + 1) * tileSize;
            int height = (maxY - minY + 1) * tileSize;

            Texture2D atlas = new Texture2D(width, height);
            foreach (var kvp in tiles)
            {
                int offsetX = (kvp.Key.x - minX) * tileSize;
                int offsetY = (maxY - kvp.Key.y) * tileSize;

                atlas.SetPixels(offsetX, offsetY, tileSize, tileSize, kvp.Value.GetPixels());
            }

            atlas.Apply();
            return atlas;
        }

        private static string GetPreferredLanguage()
        {
            SystemLanguage lang = Application.systemLanguage;
            string isoLang = ConvertToIsoCode(lang);

            return _supportedLanguages.Contains(isoLang) ? isoLang : "en";
        }

        private static string ConvertToIsoCode(SystemLanguage lang)
        {
            switch (lang)
            {
                default:
                case SystemLanguage.English:
                    return "en";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.Italian:
                    return "it";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Japanese:
                    return "ja";
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                case SystemLanguage.Chinese:
                    return "zh";
                case SystemLanguage.Portuguese:
                    return "pt";
                case SystemLanguage.Russian:
                    return "ru";
            }
        }
        #endregion
        #endregion
    }
}
