using FlightReLive.Core;
using FlightReLive.Core.Cache;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Pipeline.Download;
using FlightReLive.Core.Settings;
using FlightReLive.Core.ProceduralTerrain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VexTile.Mapbox.VectorTile;
using VexTile.Mapbox.VectorTile.Geometry;
using System.Text;

internal static class MapTilerAPIHelper
{
    #region CONSTANTS
    private const int MAX_CONCURRENT_DOWNLOADS = 8;
    private const int TILE_SIZE = 512;
    #endregion

    #region ATTRIBUTES
    private static readonly HashSet<string> _supportedLanguages = new HashSet<string>
    {
        "fr", "en", "de", "it", "es", "ja", "zh", "pt", "ru"
    };
    #endregion

    #region METHODS
    internal static async Task<bool> IsMapTilerKeyValidAsync(string apiKey, CancellationToken token = default)
    {
        string testUrl = $"https://api.maptiler.com/tiles/satellite-v2/0/0/0.png?key={apiKey}";

        using UnityWebRequest uwr = UnityWebRequest.Head(testUrl);
        UnityWebRequestAsyncOperation operation = uwr.SendWebRequest();

        while (!operation.isDone)
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        return uwr.result == UnityWebRequest.Result.Success && uwr.responseCode == 200;
    }

    internal static async Task<TileDefinition> DownloadTileAsync(TileDefinition tile, CancellationToken token, Action<int, float> onProgress = null)
    {
        try
        {
            //Phase 0 : Satellite
            tile.SatelliteTexture = await DownloadSatelliteAsync(tile, token, p => onProgress?.Invoke(0, p));
            token.ThrowIfCancellationRequested();

            //Phase 1 : Heightmap
            tile.HeightMap = await DownloadHeightmapAsync(tile, token, p => onProgress?.Invoke(1, p));
            token.ThrowIfCancellationRequested();

            //Phase 2 : Buildings
            tile.Buildings = await DownloadBuildingsAsync(tile, token, p => onProgress?.Invoke(2, p));
            token.ThrowIfCancellationRequested();

            //Phase 3 : GeoData
            tile.GeoData = await DownloadGeoDataAsync(tile, token, p => onProgress?.Invoke(3, p));
            token.ThrowIfCancellationRequested();

            return tile;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to download tile {tile.X}/{tile.Y}: {ex.Message}");
            return tile;
        }
    }
    #endregion

    #region SATELLITE
    private static async Task<Texture2D> DownloadSatelliteAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
    {
        int targetZoom;

        switch (tile.Priority)
        {
            case 0:
                targetZoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_0;
                break;
            case 1:
                targetZoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_1;
                break;
            case 2:
                targetZoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_2;
                break;
            default:
            case 3:
                targetZoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_3;
                break;
        }

        HashSet<(int x, int y)> coords = MapTools.GetTilesFromZoomLevel(tile, targetZoom);
        Dictionary<(int, int), Texture2D> downloaded = new();

        SemaphoreSlim semaphore = new(MAX_CONCURRENT_DOWNLOADS);
        List<Task> tasks = new();
        int total = coords.Count;
        int completed = 0;

        foreach ((int x, int y) in coords)
        {
            await semaphore.WaitAsync(token);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            UnityMainThreadDispatcher.AddActionInMainThread(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    Texture2D texture = await DownloadSingleSatelliteTileAsync(x, y, targetZoom, token, p =>
                    {
                        onProgress?.Invoke((completed + p) / total);
                    });

                    if (texture != null)
                    {
                        lock (downloaded)
                        {
                            downloaded[(x, y)] = texture;
                        }
                    }

                    Interlocked.Increment(ref completed);
                    onProgress?.Invoke((float)completed / total);

                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(tcs.Task);
        }

        await Task.WhenAll(tasks);

        return downloaded.Count == 0 ? null : CombinePNGTiles(downloaded);
    }

    private static async Task<Texture2D> DownloadSingleSatelliteTileAsync(int x, int y, int zoom, CancellationToken token, Action<float> onProgress)
    {
        if (await CacheManager.SatelliteTileExistsAsync(zoom, x, y))
        {
            return await CacheManager.LoadSatelliteTileAsync(zoom, x, y);
        }

        string url = $"https://api.maptiler.com/tiles/satellite-v2/{zoom}/{x}/{y}.png?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
        TaskCompletionSource<Texture2D> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        DownloadManager.EnqueueDownload(
            url,
            async data =>
            {
                if (token.IsCancellationRequested) { tcs.TrySetCanceled(token); return; }

                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    await CacheManager.SaveSatelliteTileAsync(tex.EncodeToPNG(), zoom, x, y);
                    tcs.TrySetResult(tex);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            },
            error => tcs.TrySetResult(null),
            (received, total) => onProgress?.Invoke(total > 0 ? (float)received / total : 0f)
        );

        using (token.Register(() => tcs.TrySetCanceled(token)))
        {
            return await tcs.Task;
        }
    }

    private static Texture2D CombinePNGTiles(Dictionary<(int, int), Texture2D> tiles)
    {
        int minX = tiles.Keys.Min(k => k.Item1);
        int maxX = tiles.Keys.Max(k => k.Item1);
        int minY = tiles.Keys.Min(k => k.Item2);
        int maxY = tiles.Keys.Max(k => k.Item2);

        int width = (maxX - minX + 1) * TILE_SIZE;
        int height = (maxY - minY + 1) * TILE_SIZE;
        Texture2D atlas = new Texture2D(width, height);

        foreach (var kv in tiles)
        {
            int offsetX = (kv.Key.Item1 - minX) * TILE_SIZE;
            int offsetY = (maxY - kv.Key.Item2) * TILE_SIZE;
            atlas.SetPixels(offsetX, offsetY, TILE_SIZE, TILE_SIZE, kv.Value.GetPixels());
        }

        atlas.Apply();
        return atlas;
    }

    private static Texture2D CombinePNGTiles(Dictionary<(int, int), Texture2D> tiles, int finalSize)
    {
        int minX = tiles.Keys.Min(k => k.Item1);
        int maxX = tiles.Keys.Max(k => k.Item1);
        int minY = tiles.Keys.Min(k => k.Item2);
        int maxY = tiles.Keys.Max(k => k.Item2);

        int tileCountX = maxX - minX + 1;
        int tileCountY = maxY - minY + 1;

        int atlasWidth = tileCountX * TILE_SIZE;
        int atlasHeight = tileCountY * TILE_SIZE;

        RenderTexture atlasRT = new RenderTexture(atlasWidth, atlasHeight, 0, RenderTextureFormat.ARGB32);
        atlasRT.Create();

        foreach (var kv in tiles)
        {
            int offsetX = (kv.Key.Item1 - minX) * TILE_SIZE;
            int offsetY = (maxY - kv.Key.Item2) * TILE_SIZE;

            Texture2D src = kv.Value;

            RenderTexture tempRT = RenderTexture.GetTemporary(TILE_SIZE, TILE_SIZE, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, tempRT); // GPU rescale

            // Copy into atlas at correct offset
            Graphics.CopyTexture(tempRT, 0, 0, 0, 0, TILE_SIZE, TILE_SIZE, atlasRT, 0, 0, offsetX, offsetY);

            RenderTexture.ReleaseTemporary(tempRT);
        }

        // Downscale to final texture
        RenderTexture finalRT = new RenderTexture(finalSize, finalSize, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(atlasRT, finalRT); // Bilinear GPU

        Texture2D finalTex = new Texture2D(finalSize, finalSize, TextureFormat.RGB24, false);
        RenderTexture.active = finalRT;
        finalTex.ReadPixels(new Rect(0, 0, finalSize, finalSize), 0, 0);
        finalTex.Apply();

        RenderTexture.active = null;
        atlasRT.Release();
        finalRT.Release();

        return finalTex;
    }
    #endregion

    #region HEIGHTMAP
    private static async Task<float[,]> DownloadHeightmapAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
    {
        if (await CacheManager.HeightmapExistsAsync(tile.X, tile.Y))
        {
            return await CacheManager.LoadHeightmapAsync(tile.X, tile.Y);
        }

        string url = $"https://api.maptiler.com/tiles/terrain-rgb-v2/{MapTools.ZOOM_LEVEL_TOPOGRAPHIC}/{tile.X}/{tile.Y}.webp?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
        TaskCompletionSource<byte[]> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        DownloadManager.EnqueueDownload(
            url,
            data =>
            {
                if (token.IsCancellationRequested) { tcs.TrySetCanceled(token); return; }
                tcs.TrySetResult(data);
            },
            error => tcs.TrySetResult(null),
            (received, total) => onProgress?.Invoke(total > 0 ? (float)received / total : 0f)
        );

        using (token.Register(() => tcs.TrySetCanceled(token)))
        {
            byte[] webp = await tcs.Task;

            if (webp == null)
            {
                return null;
            }

            int w = MapTools.TILE_RESOLUTION;
            int h = MapTools.TILE_RESOLUTION;
            Error err;
            byte[] raw = WebPDecoder.LoadRGBAFromWebP(webp, ref w, ref h, false, out err);

            if (err != Error.Success || raw == null)
            {
                return null;
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(raw);
            tex.Apply();

            float[,] map = new float[w, h];
            Color[] pixels = tex.GetPixels();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    token.ThrowIfCancellationRequested();
                    Color p = pixels[(h - 1 - y) * w + x];
                    float r = p.r * 255f, g = p.g * 255f, b = p.b * 255f;
                    map[x, y] = (r * 256f * 256f + g * 256f + b) * 0.1f - 10000f;
                }
            }

            await CacheManager.SaveHeightmapAsync(map, tile.X, tile.Y);

            return map;
        }
    }
    #endregion

    #region BUILDINGS
    private static async Task<List<BuildingData>> DownloadBuildingsAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
    {
        int zoom = MapTools.ZOOM_LEVEL_BUILDING;
        List<BuildingData> all = new();
        HashSet<(int, int)> coords = MapTools.GetTilesFromZoomLevel(tile, zoom);

        int i = 0;
        foreach ((int x, int y) in coords)
        {
            token.ThrowIfCancellationRequested();
            List<BuildingData> buildings = await DownloadAndParseOsmTileAsync(x, y, zoom, token, p => onProgress?.Invoke((i + p) / coords.Count));
            i++;

            if (buildings != null)
            {
                all.AddRange(buildings);
            }
        }

        return all;
    }

    private static async Task<List<BuildingData>> DownloadAndParseOsmTileAsync(int x, int y, int zoom, CancellationToken token, Action<float> onProgress)
    {
        if (await CacheManager.BuildingTileDataExistsAsync(zoom, x, y))
        {
            return await CacheManager.LoadBuildingTileDataAsync(zoom, x, y);
        }

        string url = $"https://api.maptiler.com/tiles/v3-openmaptiles/{zoom}/{x}/{y}.pbf?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
        TaskCompletionSource<byte[]> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        DownloadManager.EnqueueDownload(
            url,
            data =>
            {
                if (token.IsCancellationRequested) { tcs.TrySetCanceled(token); return; }
                tcs.TrySetResult(data);
            },
            error => tcs.TrySetResult(null),
            (received, total) => onProgress?.Invoke(total > 0 ? (float)received / total : 0f)
        );

        using (token.Register(() => tcs.TrySetCanceled(token)))
        {
            byte[] pbf = await tcs.Task;

            if (pbf == null)
            {
                return null;
            }

            VectorTileReader reader = new VectorTileReader(pbf);
            List<BuildingData> results = new();

            if (reader.LayerNames().Contains("building"))
            {
                VectorTileLayer layer = reader.GetLayer("building");

                for (int i = 0; i < layer.FeatureCount(); i++)
                {
                    VectorTileFeature feat = layer.GetFeature(i);
                    Dictionary<string, object> props = feat.GetProperties();

                    float renderHeight = props.ContainsKey("render_height") ? Convert.ToSingle(props["render_height"]) : 10f;
                    float renderMinHeight = props.ContainsKey("render_min_height") ? Convert.ToSingle(props["render_min_height"]) : 0f;
                    float extrude = renderHeight - renderMinHeight;

                    List<List<Point2d<int>>> rawGeometry = feat.Geometry<int>();
                    List<List<SerializablePoint2D>> convertedGeometry = rawGeometry.Select(ring => ring.Select(pt => SerializablePoint2D.FromPoint2D(pt)).ToList()).ToList();

                    results.Add(new BuildingData
                    {
                        Geometry = convertedGeometry,
                        Height = extrude,
                        Properties = props.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "")
                    });
                }
            }

            if (results.Count > 0)
            {
                await CacheManager.SaveBuildingTileDataAsync(results, zoom, x, y);
            }

            return results;
        }
    }
    #endregion

    #region GEODATA
    private static async Task<FeatureCollection> DownloadGeoDataAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
    {
        string lang = GetPreferredLanguage();

        if (await CacheManager.GeoTileDataExistsAsync(tile.X, tile.Y, lang))
        {
            return await CacheManager.LoadGeoTileDataAsync(tile.X, tile.Y, lang);
        }

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
            lang
        );

        TaskCompletionSource<string> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        DownloadManager.EnqueueDownload(
            url,
            data =>
            {
                if (token.IsCancellationRequested) { tcs.TrySetCanceled(token); return; }
                tcs.TrySetResult(Encoding.UTF8.GetString(data));
            },
            error => tcs.TrySetResult(null),
            (received, total) => onProgress?.Invoke(total > 0 ? (float)received / total : 0f)
        );

        using (token.Register(() => tcs.TrySetCanceled(token)))
        {
            string json;
            try
            {
                json = await tcs.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                FeatureCollection raw = JsonConvert.DeserializeObject<FeatureCollection>(json);
                FeatureCollection filtered = FilterByBoundingBox(raw, tile.BoundingBox);

                if (filtered != null && filtered.features != null && filtered.features.Count > 0)
                {
                    await CacheManager.SaveGeoTileDataAsync(filtered, tile.X, tile.Y, lang);
                }

                return filtered;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse GeoData for tile {tile.X}_{tile.Y}_{lang} : {ex.Message}");
                return null;
            }
        }
    }

    private static FeatureCollection FilterByBoundingBox(FeatureCollection collection, GPSBoundingBox bbox)
    {
        if (collection == null || collection.features == null)
        {
            return collection;
        }

        collection.features = collection.features
            .Where(f =>
            {
                if (f.geometry?.coordinates == null || f.geometry.coordinates.Count < 2) { return false; }

                double lon = f.geometry.coordinates[0];
                double lat = f.geometry.coordinates[1];
                return lon >= bbox.MinLongitude && lon <= bbox.MaxLongitude && lat >= bbox.MinLatitude && lat <= bbox.MaxLatitude;
            })
            .ToList();

        return collection;
    }
    #endregion

    #region COMMONS
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
}
