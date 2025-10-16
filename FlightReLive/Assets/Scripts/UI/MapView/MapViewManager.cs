using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightReLive.Core.Cache;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Workspace;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;
using UnityEngine.Networking;

namespace FlightReLive.UI.MapView
{
    /// <summary>
    /// Manager responsible for displaying the 2D world map (MapTiler-based) and drawing persistent GPS markers.
    /// </summary>
    internal class MapViewManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const int TILE_SIZE = 256;
        private const int MAX_CONCURRENT_DOWNLOADS = 12;
        private const int MIN_ZOOM = 3;
        private const int MAX_ZOOM_DEFAULT = 20;
        private const float INERTIA_DAMPING = 0.90f;
        private const float MARKER_RADIUS = 4f; // bigger visible markers
        private const float ZOOM_SPEED = 2.5f; // speed of auto zoom (lerp)
        #endregion

        #region ATTRIBUTES
        private int _zoom = 3;
        private int _maxZoom = MAX_ZOOM_DEFAULT;
        private Vector2 _center01 = new Vector2(0.5f, 0.5f);
        private bool _isPanning = false;
        private Vector2 _lastMouse;
        private Vector2 _panVelocity;
        private readonly Dictionary<TileId, TileEntry> _tiles = new Dictionary<TileId, TileEntry>(1024);
        private int _inFlight = 0;

        private string _mapStyle = "satellite-v2";

        // Markers
        private readonly List<MapMarker> _markers = new List<MapMarker>();

        // For smooth auto-zoom
        private bool _isAutoZooming = false;
        private float _targetZoom = 3f;
        private Vector2 _targetCenter01;
        #endregion

        #region TYPES
        private struct TileId : IEquatable<TileId>
        {
            public int Z;
            public int X;
            public int Y;
            public TileId(int z, int x, int y) { Z = z; X = x; Y = y; }
            public bool Equals(TileId other) => Z == other.Z && X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is TileId other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Z, X, Y);
        }

        private class TileEntry
        {
            public enum State { Empty, Loading, Ready, Failed }
            public State CurrentState = State.Empty;
            public Texture2D Texture;
            public IntPtr TextureId = IntPtr.Zero;
        }

        private class MapMarker
        {
            public double Latitude;
            public double Longitude;
            public Color Color;
            public MapMarker(double latitude, double longitude, Color color)
            {
                Latitude = latitude;
                Longitude = longitude;
                Color = color;
            }
        }
        #endregion

        #region UNITY METHODS
        public override void OnWindowCreated(FuWindow window)
        {
            window.HeaderHeight = 26f;
            window.FooterHeight = 0f;
            window.UI = OnUI;
        }

        private void Start()
        {
            WorkspaceManager.Instance.OnWorkspaceEndLoading += OnWorkspaceLoaded;
        }

        private void OnDestroy()
        {
            WorkspaceManager.Instance.OnWorkspaceEndLoading -= OnWorkspaceLoaded;
        }
        #endregion

        #region INPUT
        private void HandleInput(Vector2 viewTopLeft, Vector2 viewSize)
        {
            if (_isAutoZooming)
                return; // disable manual input during auto zoom

            ImGuiIOPtr io = ImGui.GetIO();
            Vector2 mouse = io.MousePos;
            bool hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.None);

            // --- Mouse wheel zoom centered on cursor ---
            if (hovered)
            {
                float wheel = io.MouseWheel;
                if (Mathf.Abs(wheel) > float.Epsilon)
                {
                    int oldZoom = _zoom;
                    int newZoom = Mathf.Clamp(_zoom + (wheel > 0f ? 1 : -1), MIN_ZOOM, _maxZoom);
                    if (newZoom != oldZoom)
                    {
                        Vector2 worldCenterPxOld = Mercator01ToWorldPixels(_center01, oldZoom);
                        float worldSizeOld = TILE_SIZE * (1 << oldZoom);
                        float worldSizeNew = TILE_SIZE * (1 << newZoom);
                        float zoomFactor = worldSizeNew / worldSizeOld;
                        Vector2 viewCenter = viewTopLeft + viewSize * 0.5f;
                        Vector2 mouseDeltaFromCenter = mouse - viewCenter;
                        Vector2 worldCenterPxNew = (worldCenterPxOld + mouseDeltaFromCenter) * zoomFactor - mouseDeltaFromCenter;
                        _center01 = WorldPixelsToMercator01(worldCenterPxNew, newZoom);
                        _center01.y = Mathf.Clamp(_center01.y, 1e-6f, 1f - 1e-6f);
                        _center01.x = Frac01(_center01.x);
                        _zoom = newZoom;
                        _panVelocity = Vector2.zero;
                    }
                }
            }

            // --- Panning ---
            if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _isPanning = true;
                _lastMouse = mouse;
                _panVelocity = Vector2.zero;
            }

            if (_isPanning)
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    Vector2 delta = mouse - _lastMouse;
                    _lastMouse = mouse;
                    Vector2 worldCenterPx = Mercator01ToWorldPixels(_center01, _zoom);
                    worldCenterPx -= delta;
                    _center01 = WorldPixelsToMercator01(worldCenterPx, _zoom);
                    ApplyBounds(viewSize);
                    _panVelocity = delta;
                }
                else
                    _isPanning = false;
            }
            else if (_panVelocity.sqrMagnitude > 0.01f)
            {
                Vector2 worldCenterPx = Mercator01ToWorldPixels(_center01, _zoom);
                worldCenterPx -= _panVelocity;
                _center01 = WorldPixelsToMercator01(worldCenterPx, _zoom);
                ApplyBounds(viewSize);
                _panVelocity *= INERTIA_DAMPING;
            }
        }
        #endregion

        #region AUTO ZOOM
        private void UpdateAutoZoom(Vector2 viewTopLeft, Vector2 viewSize)
        {
            if (!_isAutoZooming)
                return;

            // interpolate zoom
            float oldZoom = _zoom;
            float newZoom = Mathf.Lerp(_zoom, _targetZoom, Time.deltaTime * ZOOM_SPEED);

            if (Mathf.Abs(newZoom - _targetZoom) < 0.05f)
            {
                _zoom = Mathf.RoundToInt(_targetZoom);
                _center01 = _targetCenter01;
                _isAutoZooming = false;
                return;
            }

            // Interpolate center smoothly
            _center01 = Vector2.Lerp(_center01, _targetCenter01, Time.deltaTime * 3f);
            _zoom = Mathf.RoundToInt(newZoom);
        }

        private void StartAutoZoom(Vector2 targetCenter)
        {
            _targetZoom = _maxZoom;
            _targetCenter01 = targetCenter;
            _isAutoZooming = true;
        }
        #endregion

        #region DRAW
        private void DrawVisibleTiles(Vector2 viewTopLeft, Vector2 viewSize)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            int n = 1 << _zoom;
            float worldPxPerTile = TILE_SIZE;
            Vector2 worldCenterPx = Mercator01ToWorldPixels(_center01, _zoom);
            Vector2 viewHalf = viewSize * 0.5f;
            Vector2 worldTopLeftPx = worldCenterPx - viewHalf;
            int firstTileX = Mathf.FloorToInt(worldTopLeftPx.x / worldPxPerTile) - 1;
            int firstTileY = Mathf.FloorToInt(worldTopLeftPx.y / worldPxPerTile) - 1;
            int tilesX = Mathf.CeilToInt(viewSize.x / worldPxPerTile) + 3;
            int tilesY = Mathf.CeilToInt(viewSize.y / worldPxPerTile) + 3;

            for (int ty = firstTileY; ty < firstTileY + tilesY; ty++)
            {
                if (ty < 0 || ty >= n) continue;
                for (int tx = firstTileX; tx < firstTileX + tilesX; tx++)
                {
                    int wrappedTx = Mod(tx, n);
                    Vector2 tileWorldPx = new Vector2(tx * worldPxPerTile, ty * worldPxPerTile);
                    Vector2 tileOnScreen = viewTopLeft + (tileWorldPx - worldTopLeftPx);
                    float worldWidthPx = n * worldPxPerTile;

                    for (int wrap = -1; wrap <= 1; wrap++)
                    {
                        Vector2 drawPos = tileOnScreen + new Vector2(wrap * worldWidthPx, 0f);
                        if (drawPos.x + TILE_SIZE < viewTopLeft.x - TILE_SIZE || drawPos.x > viewTopLeft.x + viewSize.x + TILE_SIZE)
                            continue;

                        TileId id = new TileId(_zoom, wrappedTx, ty);
                        TileEntry entry = GetOrRequestTile(id);

                        if (entry.CurrentState == TileEntry.State.Ready && entry.Texture != null)
                        {
                            if (entry.TextureId == IntPtr.Zero)
                                entry.TextureId = FuWindow.CurrentDrawingWindow.Container.GetTextureID(entry.Texture);
                            ImGui.SetCursorScreenPos(drawPos);
                            ImGui.Image(entry.TextureId, new Vector2(TILE_SIZE, TILE_SIZE));
                        }
                    }
                }
            }

            // Draw markers above tiles
            foreach (MapMarker marker in _markers)
            {
                DrawMarker(dl, viewTopLeft, viewSize, marker);
            }
        }

        private void DrawMarker(ImDrawListPtr dl, Vector2 viewTopLeft, Vector2 viewSize, MapMarker marker)
        {
            Vector2 mercator = LatLonToMercator01(marker.Latitude, marker.Longitude);
            Vector2 worldPosPx = Mercator01ToWorldPixels(mercator, _zoom);
            Vector2 worldCenterPx = Mercator01ToWorldPixels(_center01, _zoom);
            Vector2 viewHalf = viewSize * 0.5f;
            Vector2 worldTopLeftPx = worldCenterPx - viewHalf;
            Vector2 screenPos = viewTopLeft + (worldPosPx - worldTopLeftPx);
            float worldWidthPx = TILE_SIZE * (1 << _zoom);
            if (screenPos.x < viewTopLeft.x - 50f) screenPos.x += worldWidthPx;
            if (screenPos.x > viewTopLeft.x + viewSize.x + 50f) screenPos.x -= worldWidthPx;

            uint col = ImGui.ColorConvertFloat4ToU32(new Vector4(marker.Color.r, marker.Color.g, marker.Color.b, 1f));
            dl.AddCircleFilled(screenPos, MARKER_RADIUS, col);

            // clickable area
            Vector2 markerMin = screenPos - new Vector2(MARKER_RADIUS, MARKER_RADIUS);
            Vector2 markerMax = screenPos + new Vector2(MARKER_RADIUS, MARKER_RADIUS);
            ImGui.SetCursorScreenPos(markerMin);
            ImGui.InvisibleButton($"marker_{marker.Latitude}_{marker.Longitude}", markerMax - markerMin);
            if (ImGui.IsItemClicked())
            {
                StartAutoZoom(mercator);
            }
        }

        private static Vector2 LatLonToMercator01(double lat, double lon)
        {
            double x = (lon + 180.0) / 360.0;
            double sinLat = Math.Sin(lat * Math.PI / 180.0);
            double y = 0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI);
            return new Vector2((float)x, (float)y);
        }

        private void DrawOverlayInfo(Vector2 viewTopLeft, float scale)
        {
            ImGui.SetCursorScreenPos(viewTopLeft + new Vector2(8f * scale, 8f * scale));
            Fugui.PushFont(12, FontType.Regular);
            ImGui.Text($"Zoom: {_zoom}  Center01: {_center01.x:F3}, {_center01.y:F3}");
            Fugui.PopFont();
        }
        #endregion

        #region TILES
        private TileEntry GetOrRequestTile(TileId id)
        {
            if (_tiles.TryGetValue(id, out TileEntry existing))
                return existing;

            TileEntry entry = new TileEntry();
            _tiles[id] = entry;
            if (_inFlight < MAX_CONCURRENT_DOWNLOADS)
                _ = LoadTileAsync(id, entry);
            return entry;
        }

        private async Task LoadTileAsync(TileId id, TileEntry entry)
        {
            if (entry.CurrentState != TileEntry.State.Empty)
                return;

            entry.CurrentState = TileEntry.State.Loading;
            _inFlight++;

            try
            {
                Texture2D cached = await CacheManager.LoadSatelliteTileAsync(TILE_SIZE, id.Z, id.X, id.Y);
                if (cached != null)
                {
                    cached.filterMode = FilterMode.Trilinear;
                    cached.wrapMode = TextureWrapMode.Clamp;
                    entry.Texture = cached;
                    entry.CurrentState = TileEntry.State.Ready;
                    return;
                }

                string key = SettingsManager.CurrentSettings.MapTilerAPIKey;
                if (string.IsNullOrEmpty(key))
                {
                    entry.CurrentState = TileEntry.State.Failed;
                    return;
                }

                string url = $"https://api.maptiler.com/tiles/{_mapStyle}/{id.Z}/{id.X}/{id.Y}.jpg?key={key}";
                using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url, nonReadable: false);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    entry.CurrentState = TileEntry.State.Failed;
                }
                else
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(req);
                    if (tex == null)
                    {
                        entry.CurrentState = TileEntry.State.Failed;
                    }
                    else
                    {
                        Texture2D final = new Texture2D(TILE_SIZE, TILE_SIZE, TextureFormat.RGB24, false);
                        if (tex.width != TILE_SIZE || tex.height != TILE_SIZE)
                        {
                            RenderTexture rt = RenderTexture.GetTemporary(TILE_SIZE, TILE_SIZE, 0, RenderTextureFormat.ARGB32);
                            Graphics.Blit(tex, rt);
                            RenderTexture prev = RenderTexture.active;
                            RenderTexture.active = rt;
                            final.ReadPixels(new Rect(0, 0, TILE_SIZE, TILE_SIZE), 0, 0, false);
                            final.Apply(false, false);
                            RenderTexture.active = prev;
                            RenderTexture.ReleaseTemporary(rt);
                        }
                        else
                        {
                            final.SetPixels(tex.GetPixels());
                            final.Apply(false, false);
                        }

                        UnityEngine.Object.Destroy(tex);
                        final.filterMode = FilterMode.Trilinear;
                        final.wrapMode = TextureWrapMode.Clamp;
                        entry.Texture = final;
                        entry.CurrentState = TileEntry.State.Ready;
                        _ = CacheManager.SaveSatelliteTileAsync(final, id.Z, id.X, id.Y);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MapViewManager] Failed to load tile {id.Z}/{id.X}/{id.Y}: {ex.Message}");
                entry.CurrentState = TileEntry.State.Failed;
            }
            finally
            {
                _inFlight = Mathf.Max(0, _inFlight - 1);
                foreach (var kvp in _tiles)
                {
                    if (_inFlight >= MAX_CONCURRENT_DOWNLOADS)
                        break;
                    if (kvp.Value.CurrentState == TileEntry.State.Empty)
                        _ = LoadTileAsync(kvp.Key, kvp.Value);
                }
            }
        }
        private void ApplyBounds(Vector2 viewSize)
        {
            float worldSize = TILE_SIZE * (1 << _zoom);
            float viewHalfWorldY = viewSize.y * 0.5f;

            Vector2 worldCenterPx = Mercator01ToWorldPixels(_center01, _zoom);
            float minY = viewHalfWorldY;
            float maxY = worldSize - viewHalfWorldY;
            worldCenterPx.y = Mathf.Clamp(worldCenterPx.y, minY, maxY);
            worldCenterPx.x = (worldCenterPx.x % worldSize + worldSize) % worldSize;

            _center01 = WorldPixelsToMercator01(worldCenterPx, _zoom);
        }

        #endregion

        #region MATH
        private static int Mod(int a, int n) { int r = a % n; if (r < 0) r += n; return r; }
        private static float Frac01(float x) { x -= Mathf.Floor(x); if (x < 0f) x += 1f; if (x >= 1f) x -= 1f; return x; }
        private static Vector2 Mercator01ToWorldPixels(Vector2 merc01, int zoom)
        {
            float worldSize = TILE_SIZE * (1 << zoom);
            return new Vector2(merc01.x * worldSize, merc01.y * worldSize);
        }
        private static Vector2 WorldPixelsToMercator01(Vector2 worldPx, int zoom)
        {
            float worldSize = TILE_SIZE * (1 << zoom);
            return new Vector2(worldPx.x / worldSize, worldPx.y / worldSize);
        }
        public void AddMarker(double lat, double lon, Color color) => _markers.Add(new MapMarker(lat, lon, color));
        #endregion

        #region CALLBACKS
        private void OnWorkspaceLoaded()
        {
            if (WorkspaceManager.Instance.LoadedFlights == null)
                return;

            foreach (var kvp in WorkspaceManager.Instance.LoadedFlights)
            {
                if (kvp.Value != null && kvp.Value.IsValid && kvp.Value.DataPoints.Count > 0)
                {
                    FlightDataPoint p = kvp.Value.DataPoints[0];
                    AddMarker(p.Latitude, p.Longitude, Color.red);
                }
            }
        }
        #endregion

        #region UI
        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            float scale = Fugui.CurrentContext.Scale;
            using (FuPanel panel = new FuPanel("mapViewPanel", flags: FuPanelFlags.Default))
            {
                Vector2 avail = ImGui.GetContentRegionAvail();
                Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
                ImGui.BeginChild("mapViewChild", avail, ImGuiChildFlags.None,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                HandleInput(cursorScreenPos, avail);
                UpdateAutoZoom(cursorScreenPos, avail);
                DrawVisibleTiles(cursorScreenPos, avail);
                DrawOverlayInfo(cursorScreenPos, scale);

                ImGui.EndChild();
            }
        }
        #endregion
    }
}


