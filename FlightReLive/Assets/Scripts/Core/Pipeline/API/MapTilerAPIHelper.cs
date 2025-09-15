using FlightReLive.Core;
using FlightReLive.Core.Cache;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Pipeline.API;
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

internal static class MapTilerAPIHelper
{
    #region CONSTANTS
    private const int TILE_SIZE = 512;
    #endregion

    #region ATTRIBUTES
    private static readonly HashSet<string> _supportedLanguages = new HashSet<string>
    {
        "fr", "en", "de", "it", "es", "ja", "zh", "pt", "ru"
    };
    #endregion

    #region METHODS
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

    internal static async Task<TileDefinition> DownloadTileAsync(TileDefinition tile, int satelliteZoom, int topographicZoom, int buildingZoom)
    {
        try
        {
            tile.SatelliteTexture = await DownloadSatelliteAtlasAsync(tile, satelliteZoom);
            tile.HeightMap = await DownloadHeightmapAsync(tile, topographicZoom);
            tile.Buildings = await DownloadBuildingsAsync(tile, buildingZoom);
            tile.GeoData = await DownloadGeoDataAsync(tile);

            return tile;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to download tile {tile.X}/{tile.Y}: {ex.Message}");
            return tile;
        }
    }
    #endregion

    #region SATELLITE
    private static async Task<Texture2D> DownloadSatelliteAtlasAsync(TileDefinition tile, int zoom)
    {
        HashSet<(int x, int y)> coords = MapTools.GetTilesFromZoomLevel(tile, zoom);
        Dictionary<(int, int), Texture2D> downloaded = new();

        foreach ((int x, int y) in coords)
        {
            Texture2D tex = await DownloadSingleSatelliteTileAsync(x, y, zoom);

            if (tex != null)
            {
                downloaded[(x, y)] = tex;
            }
        }

        if (downloaded.Count == 0)
        {
            return null;
        }

        return CombinePNGTiles(downloaded);
    }

    private static async Task<Texture2D> DownloadSingleSatelliteTileAsync(int x, int y, int zoom)
    {
        if (await CacheManager.SatelliteTileExistsAsync(zoom, x, y))
        {
            return await CacheManager.LoadSatelliteTileTextureAsync(zoom, x, y);
        }

        string url = $"https://api.maptiler.com/tiles/satellite-v2/{zoom}/{x}/{y}.png?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
        TaskCompletionSource<Texture2D> tcs = new();

        DownloadManager.EnqueueDownload(url,
            async data =>
            {
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    await CacheManager.SaveSatelliteTileAsync(tex.EncodeToPNG(), zoom, x, y);
                    tcs.SetResult(tex);
                }
                else
                {
                    tcs.SetResult(null);
                }
            },
            error => tcs.SetResult(null));

        return await tcs.Task;
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
    #endregion

    #region HEIGHTMAP
    private static async Task<float[,]> DownloadHeightmapAsync(TileDefinition tile, int zoom)
    {
        if (await CacheManager.HeightmapExistsAsync(tile.X, tile.Y))
        {
            return await CacheManager.LoadHeightmapAsync(tile.X, tile.Y);
        }

        string url = $"https://api.maptiler.com/tiles/terrain-rgb-v2/{zoom}/{tile.X}/{tile.Y}.webp?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
        TaskCompletionSource<byte[]> tcs = new();
        DownloadManager.EnqueueDownload(url, data => tcs.SetResult(data), error => tcs.SetResult(null));

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
                Color p = pixels[(h - 1 - y) * w + x];
                float r = p.r * 255f, g = p.g * 255f, b = p.b * 255f;
                map[x, y] = (r * 256f * 256f + g * 256f + b) * 0.1f - 10000f;
            }
        }

        await CacheManager.SaveHeightmapAsync(map, tile.X, tile.Y);
        return map;
    }
    #endregion

    #region BUILDINGS
    private static async Task<List<BuildingData>> DownloadBuildingsAsync(TileDefinition tile, int zoom)
    {
        List<BuildingData> all = new();
        HashSet<(int, int)> coords = MapTools.GetTilesFromZoomLevel(tile, zoom);

        foreach ((int x, int y) in coords)
        {
            List<BuildingData> buildings = await DownloadAndParseBuildingTileAsync(x, y, zoom);

            if (buildings != null)
            {
                all.AddRange(buildings);
            }
        }

        return all;
    }

    private static async Task<List<BuildingData>> DownloadAndParseBuildingTileAsync(int x, int y, int zoom)
    {
        if (await CacheManager.BuildingTileDataExistsAsync(zoom, x, y))
        {
            return await CacheManager.LoadBuildingTileDataAsync(zoom, x, y);
        }

        string url = $"https://api.maptiler.com/tiles/v3-openmaptiles/{zoom}/{x}/{y}.pbf?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
        TaskCompletionSource<byte[]> tcs = new();
        DownloadManager.EnqueueDownload(url, data => tcs.SetResult(data), error => tcs.SetResult(null));

        byte[] pbf = await tcs.Task;

        if (pbf == null)
        {
            return null;
        }

        VectorTileReader reader = new VectorTileReader(pbf);
        List<BuildingData> buildings = new();

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

                buildings.Add(new BuildingData
                {
                    Geometry = convertedGeometry,
                    Height = extrude,
                    Properties = props.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "")
                });
            }
        }

        if (buildings.Count > 0)
        {
            await CacheManager.SaveBuildingTileDataAsync(buildings, zoom, x, y);
        }

        return buildings;
    }
    #endregion

    #region GEODATA
    private static async Task<FeatureCollection> DownloadGeoDataAsync(TileDefinition tile)
    {
        FlightGPSData center = MapTools.GetCenterOfBoundingBox(tile.BoundingBox);

        string url = string.Format(CultureInfo.InvariantCulture, "https://api.maptiler.com/geocoding/{0},{1}.json?key={2}&bbox={3},{4},{5},{6}&language={7}", center.Longitude, center.Latitude, SettingsManager.CurrentSettings.MapTilerAPIKey, tile.BoundingBox.MinLongitude, tile.BoundingBox.MinLatitude, tile.BoundingBox.MaxLongitude, tile.BoundingBox.MaxLatitude, GetPreferredLanguage());
        TaskCompletionSource<string> tcs = new();
        DownloadManager.EnqueueDownload(url, data => tcs.SetResult(System.Text.Encoding.UTF8.GetString(data)),error => tcs.SetResult(null));

        string json = await tcs.Task;
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<FeatureCollection>(json);
        }
        catch
        { 
            return null;
        }
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
