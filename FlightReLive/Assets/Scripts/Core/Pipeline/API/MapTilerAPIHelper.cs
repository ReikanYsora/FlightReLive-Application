using FlightReLive.Core.Cache;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline.Download;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VexTile.Mapbox.VectorTile;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core.Pipeline.API
{
    internal static class MapTilerAPIHelper
    {
        #region CONSTANTS
        private const int TILE_SIZE = 512;
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

        internal static async Task<TileDefinition> DownloadTileAsync(TileDefinition tile, CancellationToken token, Action<int, float, TileResourceSource?> onProgress = null)
        {
            try
            {
                //Phase 0 : Satellite
                ResourceResult<Texture2D> sat = await DownloadSatelliteAsync(tile, token, p => onProgress?.Invoke(0, p, null));
                if (sat != null)
                {
                    tile.SatelliteTexture = sat.Data;
                    onProgress?.Invoke(0, 1f, sat.Source);
                }
                token.ThrowIfCancellationRequested();

                //Phase 1 : Heightmap
                ResourceResult<float[,]> hm = await DownloadHeightmapAsync(tile, token, p => onProgress?.Invoke(1, p, null));
                if (hm != null)
                {
                    tile.HeightMap = hm.Data;
                    onProgress?.Invoke(1, 1f, hm.Source);
                }
                token.ThrowIfCancellationRequested();

                //Phase 2 : Open Map Tiles
                ResourceResult<List<OpenMapTileFeature>> bld = await DownloadOpenMapTilesAsync(tile, token, p => onProgress?.Invoke(2, p, null));
                if (bld != null)
                {
                    tile.Features = bld.Data;
                    onProgress?.Invoke(2, 1f, bld.Source);
                }
                token.ThrowIfCancellationRequested();

                return tile;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to download tile {tile.X}/{tile.Y}: {ex.Message}");
                return tile;
            }
        }
        #endregion

        #region SATELLITE
        private static async Task<ResourceResult<Texture2D>> DownloadSatelliteAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
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
                default:
                case 2:
                case 3:
                    targetZoom = MapTools.ZOOM_LEVEL_SATELLITE_PRIORITY_OTHER;
                    break;
            }

            HashSet<(int x, int y)> coords = MapTools.GetTilesFromZoomLevel(tile, targetZoom);
            Dictionary<(int, int), Texture2D> downloaded = new Dictionary<(int, int), Texture2D>(coords.Count);

            int total = coords.Count;
            int completed = 0;
            int anyFromCache = 0;

            
            IEnumerable<Task> tasks = coords.Select(async c =>
            {
                var tex = await DownloadSingleSatelliteTileAsync(c.x, c.y, targetZoom, token, p =>
                {
                    onProgress?.Invoke((Interlocked.CompareExchange(ref completed, 0, 0) + p) / total);
                });

                if (tex?.Data != null)
                {
                    lock (downloaded) downloaded[c] = tex.Data;
                    if (tex.Source == TileResourceSource.Cache)
                    {
                        Interlocked.Exchange(ref anyFromCache, 1);
                    }
                }

                Interlocked.Increment(ref completed);
                onProgress?.Invoke((float)completed / total);
            });

            await Task.WhenAll(tasks);

            if (downloaded.Count == 0)
            {
                return null;
            }

            //Main thread
            Texture2D atlas = await UnityMainThreadDispatcher.AwaitOnMainThread(() => CombinePNGTiles(downloaded) );

            return new ResourceResult<Texture2D>(atlas, anyFromCache == 1 ? TileResourceSource.Cache : TileResourceSource.Download);
        }

        private static async Task<ResourceResult<Texture2D>> DownloadSingleSatelliteTileAsync(int x, int y, int zoom, CancellationToken token, Action<float> onProgress)
        {
            //Cache
            if (await CacheManager.SatelliteTileExistsAsync(zoom, x, y))
            {
                Texture2D cached = await CacheManager.LoadSatelliteTileAsync(TILE_SIZE, zoom, x, y);
                return new ResourceResult<Texture2D>(cached, TileResourceSource.Cache);
            }

            string url = $"https://api.maptiler.com/tiles/satellite-v2/{zoom}/{x}/{y}.png?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
            var tcs = new TaskCompletionSource<ResourceResult<Texture2D>>(TaskCreationOptions.RunContinuationsAsynchronously);

            DownloadManager.EnqueueDownload(
                url,
                async data =>
                {
                    if (token.IsCancellationRequested) { tcs.TrySetCanceled(token); return; }

                    try
                    {
                        //Main thread
                        Texture2D tex = await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
                        {
                            var t = new Texture2D(2, 2, TextureFormat.RGB24, false);
                            t.LoadImage(data);
                            t.name = $"{zoom}_{x}_{y}";
                            t.filterMode = FilterMode.Trilinear;
                            return t;
                        });

                        await CacheManager.SaveSatelliteTileAsync(tex, zoom, x, y);
                        tcs.TrySetResult(new ResourceResult<Texture2D>(tex, TileResourceSource.Download));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Tile {zoom}/{x}/{y} failed: {ex.Message}");
                        tcs.TrySetResult(null);
                    }
                },
                error => tcs.TrySetResult(null),
                (received, total) =>
                {
                    if (total > 0) onProgress?.Invoke((float)received / total);
                    else onProgress?.Invoke(0f);
                }
            );

            using (token.Register(() => tcs.TrySetCanceled(token)))
            {
                return await tcs.Task;
            }
        }
        #endregion

        #region STATIC MAP IMAGE
        /// <summary>
        /// Download a static map image from MapTiler with markers at the given GPS points.
        /// Each Vector2 = (X = Latitude, Y = Longitude).
        /// </summary>
        internal static async Task<Texture2D> DownloadStaticMapImageAsync(List<Vector2> gpsPoints, int width, int height, string style = "streets-v2", CancellationToken token = default)
        {
            if (gpsPoints == null || gpsPoints.Count == 0)
            {
                Debug.LogWarning("DownloadStaticMapImageAsync called with empty gpsPoints list.");
                return null;
            }

            string apiKey = SettingsManager.CurrentSettings.MapTilerAPIKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("MapTiler API key is missing.");
                return null;
            }

            // Compute bounding box center
            float minLat = float.MaxValue, maxLat = float.MinValue;
            float minLon = float.MaxValue, maxLon = float.MinValue;

            foreach (var pt in gpsPoints)
            {
                if (pt.x < minLat)
                {
                    minLat = pt.x;
                }

                if (pt.x > maxLat)
                {
                    maxLat = pt.x;
                }

                if (pt.y < minLon)
                {
                    minLon = pt.y;
                }

                if (pt.y > maxLon)
                {
                    maxLon = pt.y;
                }
            }

            float centerLat = (minLat + maxLat) / 2f;
            float centerLon = (minLon + maxLon) / 2f;

            int zoom = GetZoomForBounds(minLat, maxLat, minLon, maxLon, width, height);

            //Build markers param
            List<string> markerList = new List<string>();
            foreach (var pt in gpsPoints)
            {
                //MapTiler format: lon,lat,color
                markerList.Add($"{pt.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},{pt.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},red");
            }
            string markersParam = string.Join("|", markerList);

            //Construct URL
            string url =
                $"https://api.maptiler.com/maps/{style}/static/{centerLon.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{centerLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{zoom}/{width}x{height}.png" +
                $"?key={apiKey}&markers={markersParam}";

            TaskCompletionSource<Texture2D> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Debug.Log(url);

            DownloadManager.EnqueueDownload(
                url,
                async data =>
                {
                    if (token.IsCancellationRequested)
                    { 
                        tcs.TrySetCanceled(token);
                        return;
                    }

                    try
                    {
                        //Create Texture2D on main thread
                        Texture2D tex = await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
                        {
                            var t = new Texture2D(2, 2, TextureFormat.RGB24, false);
                            t.LoadImage(data);
                            t.name = "MapTiler_StaticMap";
                            t.filterMode = FilterMode.Bilinear;
                            return t;
                        });

                        tcs.TrySetResult(tex);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to parse MapTiler static map image: {ex.Message}");
                        tcs.TrySetResult(null);
                    }
                },
                error => tcs.TrySetResult(null),
                (received, total) => { }
            );

            using (token.Register(() => tcs.TrySetCanceled(token)))
            {
                return await tcs.Task;
            }
        }
        #endregion

        #region HEIGHTMAP
        private static async Task<ResourceResult<float[,]>> DownloadHeightmapAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
        {
            if (await CacheManager.HeightmapExistsAsync(tile.X, tile.Y))
            {
                float[,] cached = await CacheManager.LoadHeightmapAsync(tile.X, tile.Y);

                return new ResourceResult<float[,]>(cached, TileResourceSource.Cache);
            }

            string url = $"https://api.maptiler.com/tiles/terrain-rgb-v2/{MapTools.ZOOM_LEVEL_HEIGHTMAP}/{tile.X}/{tile.Y}.webp?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
            TaskCompletionSource<byte[]> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            DownloadManager.EnqueueDownload(
                url,
                data => { if (!token.IsCancellationRequested) tcs.TrySetResult(data); },
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
                return new ResourceResult<float[,]>(map, TileResourceSource.Download);
            }
        }
        #endregion

        #region OPEN MAP TILE FEATURES
        private static async Task<ResourceResult<List<OpenMapTileFeature>>> DownloadOpenMapTilesAsync(TileDefinition tile, CancellationToken token, Action<float> onProgress)
        {
            int zoom = MapTools.ZOOM_LEVEL_OPENTILEMAP;
            List<OpenMapTileFeature> all = new List<OpenMapTileFeature>();
            HashSet<(int, int)> coords = MapTools.GetTilesFromZoomLevel(tile, zoom);

            int i = 0;
            TileResourceSource finalSource = TileResourceSource.Download;

            foreach ((int x, int y) in coords)
            {
                token.ThrowIfCancellationRequested();
                ResourceResult<List<OpenMapTileFeature>> res = await DownloadAndParseOpenMapTileAsync(tile, x, y, zoom, token, p => onProgress?.Invoke((i + p) / coords.Count));
                i++;

                if (res != null && res.Data != null)
                {
                    all.AddRange(res.Data);
                    if (res.Source == TileResourceSource.Cache)
                    {
                        finalSource = TileResourceSource.Cache;
                    }
                }
            }

            return new ResourceResult<List<OpenMapTileFeature>>(all, finalSource);
        }

        private static async Task<ResourceResult<List<OpenMapTileFeature>>> DownloadAndParseOpenMapTileAsync(TileDefinition tile, int x, int y, int zoom, CancellationToken token, Action<float> onProgress)
        {
            if (await CacheManager.OpenMapTileDataExistsAsync(zoom, x, y))
            {
                List<OpenMapTileFeature> cached = await CacheManager.LoadOpenMapTileDataAsync(zoom, x, y);
                cached.ForEach(x => x.TileDefinition = tile);

                return new ResourceResult<List<OpenMapTileFeature>>(cached, TileResourceSource.Cache);
            }

            string url = $"https://api.maptiler.com/tiles/v3-openmaptiles/{zoom}/{x}/{y}.pbf?key={SettingsManager.CurrentSettings.MapTilerAPIKey}";
            TaskCompletionSource<byte[]> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            DownloadManager.EnqueueDownload(
                url,
                data =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        tcs.TrySetResult(data);
                    }
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
                List<OpenMapTileFeature> results = new List<OpenMapTileFeature>();

                //Building
                if (reader.LayerNames().Contains("building"))
                {
                    VectorTileLayer layer = reader.GetLayer("building");
                    for (int i = 0; i < layer.FeatureCount(); i++)
                    {
                        VectorTileFeature feat = layer.GetFeature(i);
                        Dictionary<string, object> props = feat.GetProperties();

                        float renderHeight = props.ContainsKey("render_height") ? Convert.ToSingle(props["render_height"]) : 10f;
                        float renderMinHeight = props.ContainsKey("render_min_height") ? Convert.ToSingle(props["render_min_height"]) : 0f;

                        results.Add(new BuildingFeature
                        {
                            Geometry = ConvertGeometry(feat),
                            RenderHeight = renderHeight,
                            RenderMinHeight = renderMinHeight,
                            LayerName = layer.Name,
                            TileDefinition = tile
                        });
                    }
                }

                //Landuse
                if (reader.LayerNames().Contains("landuse"))
                {
                    VectorTileLayer layer = reader.GetLayer("landuse");
                    for (int i = 0; i < layer.FeatureCount(); i++)
                    {
                        VectorTileFeature feat = layer.GetFeature(i);
                        Dictionary<string, object> props = feat.GetProperties();

                        results.Add(new LanduseFeature
                        {
                            Geometry = ConvertGeometry(feat),
                            LayerName = layer.Name,
                            Class = props.TryGetValue("class", out var v) ? v?.ToString() : null,
                            TileDefinition = tile
                        });
                    }
                }

                //Landcover
                if (reader.LayerNames().Contains("landcover"))
                {
                    VectorTileLayer layer = reader.GetLayer("landcover");
                    for (int i = 0; i < layer.FeatureCount(); i++)
                    {
                        VectorTileFeature feat = layer.GetFeature(i);
                        Dictionary<string, object> props = feat.GetProperties();

                        results.Add(new LandcoverFeature
                        {
                            Geometry = ConvertGeometry(feat),
                            LayerName = layer.Name,
                            Class = props.TryGetValue("class", out var c) ? c?.ToString() : null,
                            Subclass = props.TryGetValue("subclass", out var s) ? s?.ToString() : null,
                            TileDefinition = tile
                        });
                    }
                }

                //Water
                if (reader.LayerNames().Contains("water"))
                {
                    VectorTileLayer layer = reader.GetLayer("water");
                    for (int i = 0; i < layer.FeatureCount(); i++)
                    {
                        VectorTileFeature feat = layer.GetFeature(i);
                        Dictionary<string, object> props = feat.GetProperties();

                        results.Add(new WaterFeature
                        {
                            Geometry = ConvertGeometry(feat),
                            LayerName = layer.Name,
                            Class = props.TryGetValue("class", out var c) ? c?.ToString() : null,
                            IsIntermittent = props.TryGetValue("intermittent", out var it) && it?.ToString() == "1",
                            TileDefinition = tile
                        });
                    }
                }

                //Park
                if (reader.LayerNames().Contains("park"))
                {
                    VectorTileLayer layer = reader.GetLayer("park");
                    for (int i = 0; i < layer.FeatureCount(); i++)
                    {
                        VectorTileFeature feat = layer.GetFeature(i);
                        Dictionary<string, object> props = feat.GetProperties();

                        results.Add(new ParkFeature
                        {
                            Geometry = ConvertGeometry(feat),
                            LayerName = layer.Name,
                            Class = props.TryGetValue("class", out var c) ? c?.ToString() : null,
                            Rank = props.TryGetValue("rank", out var r) ? r?.ToString() : null,
                            TileDefinition = tile
                        });
                    }
                }

                //Aeroway 
                if (reader.LayerNames().Contains("aeroway"))
                {
                    VectorTileLayer layer = reader.GetLayer("aeroway");
                    for (int i = 0; i < layer.FeatureCount(); i++)
                    {
                        VectorTileFeature feat = layer.GetFeature(i);
                        Dictionary<string, object> props = feat.GetProperties();

                        results.Add(new AerowayFeature
                        {
                            Geometry = ConvertGeometry(feat),
                            LayerName = layer.Name,
                            Class = props.TryGetValue("class", out var c) ? c?.ToString() : null,
                            TileDefinition = tile
                        });
                    }
                }

                await CacheManager.SaveOpenMapTileDataAsync(results, zoom, x, y);

                return new ResourceResult<List<OpenMapTileFeature>>(results, TileResourceSource.Download);
            }
        }

        /// <summary>
        /// Helper to convert VexTile geometry to SerializablePoint2D.
        /// </summary>
        private static List<List<SerializablePoint2D>> ConvertGeometry(VectorTileFeature feat)
        {
            List<List<Point2d<int>>> raw = feat.Geometry<int>();

            return raw.Select(ring => ring.Select(pt => SerializablePoint2D.FromPoint2D(pt)).ToList()).ToList();
        }
        #endregion

        #region COMMONS
        private static Texture2D CombinePNGTiles(Dictionary<(int, int), Texture2D> tiles)
        {
            int minX = tiles.Keys.Min(k => k.Item1);
            int maxX = tiles.Keys.Max(k => k.Item1);
            int minY = tiles.Keys.Min(k => k.Item2);
            int maxY = tiles.Keys.Max(k => k.Item2);

            int width = (maxX - minX + 1) * TILE_SIZE;
            int height = (maxY - minY + 1) * TILE_SIZE;

            Texture2D atlas = new Texture2D(width, height, TextureFormat.RGB24, false);

            foreach (var kv in tiles)
            {
                int offsetX = (kv.Key.Item1 - minX) * TILE_SIZE;
                int offsetY = (maxY - kv.Key.Item2) * TILE_SIZE;

                Graphics.CopyTexture(kv.Value, 0, 0, 0, 0, TILE_SIZE, TILE_SIZE, atlas, 0, 0, offsetX, offsetY);
            }

            return atlas;
        }

        private static int GetZoomForBounds(float minLat, float maxLat, float minLon, float maxLon, int width, int height)
        {
            const int TILE_SIZE = 256;
            const int MAX_ZOOM = 20;
            const int MIN_ZOOM = 1;

            //Convert degrees to radians
            double latRadMin = minLat * Mathf.Deg2Rad;
            double latRadMax = maxLat * Mathf.Deg2Rad;

            for (int z = MAX_ZOOM; z >= MIN_ZOOM; z--)
            {
                double mapSize = TILE_SIZE * Math.Pow(2, z);

                //Lon → pixels
                double xMin = (minLon + 180.0) / 360.0 * mapSize;
                double xMax = (maxLon + 180.0) / 360.0 * mapSize;

                //Lat → pixels (Mercator projection)
                double yMin = (1 - Math.Log(Math.Tan(latRadMin) + 1 / Math.Cos(latRadMin)) / Math.PI) / 2 * mapSize;
                double yMax = (1 - Math.Log(Math.Tan(latRadMax) + 1 / Math.Cos(latRadMax)) / Math.PI) / 2 * mapSize;

                double pixelWidth = Math.Abs(xMax - xMin);
                double pixelHeight = Math.Abs(yMax - yMin);

                if (pixelWidth <= width && pixelHeight <= height)
                {
                    return z;
                }
            }

            return MIN_ZOOM;
        }
        #endregion
    }
}
