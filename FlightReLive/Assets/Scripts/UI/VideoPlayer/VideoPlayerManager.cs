using FlightReLive.Core;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.TimeBar;
using FlightReLive.UI.Metadata;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

namespace FlightReLive.UI.Video
{
    /// <summary>
    /// Manager responsible for handling Unity VideoPlayer playback,
    /// synchronized with the TimeBar. Handles downscaling, DX12 safety,
    /// and resilient sync logic for problematic DJI video streams.
    /// </summary>
    internal class VideoPlayerManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float TOP_BAR_HEIGHT = 26f;
        private const float TIMELINE_HEIGHT = 18f;

        //Sync thresholds (seconds)
        private const double SYNC_THRESHOLD = 0.08;         //Playback speed correction beyond this drift
        private const double HARD_SYNC_THRESHOLD = 0.45;    //Hard seek beyond this drift
        private const double SEEK_COOLDOWN = 0.10;          //Avoid seek spam (seconds)

        //Output RenderTexture max size
        private const int MAX_OUTPUT_WIDTH = 1920;
        private const int MAX_OUTPUT_HEIGHT = 1080;
        #endregion

        #region ATTRIBUTES
        [SerializeField] private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;

        //Sync state
        private double _lastHardSeekRealtime;
        private float _lastAppliedPlaybackSpeed = 1f;
        #endregion

        #region PROPERTIES
        internal static VideoPlayerManager Instance { get; private set; }
        private bool IsPrepared => _videoPlayer != null && _videoPlayer.isPrepared && _renderTexture != null;
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

            if (_videoPlayer != null)
            {
                //Initialize VideoPlayer settings (safe for DX12 and DJI files)

                try
                {
                    _videoPlayer.playOnAwake = false;
                    _videoPlayer.isLooping = false;
                    _videoPlayer.skipOnDrop = true;
                    _videoPlayer.waitForFirstFrame = true;
                    _videoPlayer.sendFrameReadyEvents = false;
                    _videoPlayer.renderMode = VideoRenderMode.RenderTexture;

                    //Disable all audio (avoid DJI corrupted audio tracks)
                    _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                    _videoPlayer.controlledAudioTrackCount = 0;
                    try { _videoPlayer.EnableAudioTrack(0, false); } catch { }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[VideoPlayerManager] Setup VideoPlayer failed: {ex.Message}");
                }

                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.errorReceived += OnVideoError;
                _videoPlayer.seekCompleted += OnSeekCompleted;
            }
        }

        private void Start()
        {
            TimeBarManager.Instance.RegisterWindowName(_windowName);
        }

        private void LateUpdate()
        {
            try
            {
                if (_videoPlayer == null || !IsPrepared || TimeBarManager.Instance == null || !TimeBarManager.Instance.IsInitialized)
                {
                    return;
                }

                SyncWithTimeBar();
                EnsurePlayingState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VideoPlayerManager] Fatal LateUpdate error: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                try
                {
                    _videoPlayer.Stop();
                } 
                catch { }

                if (_renderTexture != null)
                {
                    _renderTexture.Release();
                    Destroy(_renderTexture);
                    _renderTexture = null;
                }

                _videoPlayer.prepareCompleted -= OnVideoPrepared;
                _videoPlayer.errorReceived -= OnVideoError;
                _videoPlayer.seekCompleted -= OnSeekCompleted;

                try
                { 
                    Destroy(_videoPlayer);
                } 
                catch { }

                _videoPlayer = null;
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Load video from FlightData and prepare the VideoPlayer.
        /// </summary>
        internal void Load(FlightData flightData)
        {
            if (flightData == null || _videoPlayer == null || string.IsNullOrEmpty(flightData.VideoPath) || !File.Exists(flightData.VideoPath))
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                try
                {
                    // Reset state
                    _lastHardSeekRealtime = 0;
                    _lastAppliedPlaybackSpeed = 1f;

                    if (_renderTexture != null)
                    {
                        _renderTexture.Release();
                        Destroy(_renderTexture);
                        _renderTexture = null;
                    }

                    _videoPlayer.source = VideoSource.Url;
                    _videoPlayer.url = flightData.VideoPath;

                    //Double safety: disable all audio
                    try
                    { 
                        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None; 
                    } 
                    catch { }

                    try
                    { 
                        _videoPlayer.controlledAudioTrackCount = 0;
                    } 
                    catch { }

                    try
                    { 
                        _videoPlayer.EnableAudioTrack(0, false);
                    } 
                    catch { }

                    _videoPlayer.Prepare();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[VideoPlayerManager] Load failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Unload video and cleanup resources.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                if (_videoPlayer == null)
                {
                    return;
                }

                try
                { 
                    _videoPlayer.Stop();
                } 
                catch { }

                if (_renderTexture != null)
                {
                    _renderTexture.Release();
                    Destroy(_renderTexture);
                    _renderTexture = null;
                }
            });
        }
        #endregion

        #region SYNC LOGIC
        /// <summary>
        /// Sync: adjust speed or perform hard seek if drift is too large.
        /// </summary>
        private void SyncWithTimeBar()
        {
            double targetTime = TimeBarManager.Instance.CurrentTime;
            double videoTime = SafeGetTime();
            double drift = targetTime - videoTime;

            //Hard seek if drift exceeds threshold (with cooldown)
            if (Math.Abs(drift) > HARD_SYNC_THRESHOLD)
            {
                double now = Time.realtimeSinceStartupAsDouble;
                if (now - _lastHardSeekRealtime > SEEK_COOLDOWN)
                {
                    TrySetTime(targetTime);
                    _lastHardSeekRealtime = now;
                }
                return;
            }

            //Adjust playback speed to smoothly catch up
            if (Math.Abs(drift) > SYNC_THRESHOLD)
            {
                double correctionFactor = 1.0 + (drift * 0.45);
                float wantedSpeed = Mathf.Clamp((float)(TimeBarManager.Instance.SpeedFactor * correctionFactor), 0.25f, 3.5f);
                TrySetPlaybackSpeed(wantedSpeed);
            }
            else
            {
                //Return to TimeBar speed
                TrySetPlaybackSpeed((float)TimeBarManager.Instance.SpeedFactor);
            }
        }

        /// <summary>
        /// Ensure VideoPlayer playing/paused state matches TimeBar.
        /// </summary>
        private void EnsurePlayingState()
        {
            bool wantPlaying = TimeBarManager.Instance.IsPlaying;

            if (wantPlaying)
            {
                if (!_videoPlayer.isPlaying)
                {
                    TryPlay();
                }
            }
            else
            {
                if (_videoPlayer.isPlaying)
                {
                    TryPause();
                }
            }
        }
        #endregion

        #region SAFE WRAPPERS
        private double SafeGetTime()
        {
            try
            {
                return _videoPlayer.time;
            }
            catch
            { 
                return TimeBarManager.Instance != null ? TimeBarManager.Instance.CurrentTime : 0.0;
            }
        }

        private double SafeGetLength()
        {
            try
            { 
                return _videoPlayer.length;
            }
            catch 
            { 
                return TimeBarManager.Instance != null ? TimeBarManager.Instance.Duration : 0.0;
            }
        }

        private long SafeGetFrameCount()
        {
            try
            { 
                return (long)_videoPlayer.frameCount;
            }
            catch
            { 
                return 0;
            }
        }

        private double SafeGetFrameRate()
        {
            try
            {
                if (_videoPlayer.frameRate > 0.0)
                {
                    return _videoPlayer.frameRate;
                }

                double len = SafeGetLength();
                long fc = SafeGetFrameCount();
                return (len > 0.0001 && fc > 0) ? (fc / len) : 0.0;
            }
            catch
            { 
                return 0.0;
            }
        }

        private int SafeGetWidth()
        {
            try
            { 
                return (int)_videoPlayer.width;
            }
            catch
            { 
                return 1;
            }
        }

        private int SafeGetHeight()
        {
            try
            { 
                return (int)_videoPlayer.height; 
            }
            catch
            {
                return 1; 
            }
        }

        private void TrySetTime(double t)
        {
            try
            { 
                _videoPlayer.time = t;
            }
            catch (Exception ex)
            { 
                Debug.LogWarning($"[VideoPlayerManager] Set time failed: {ex.Message}");
            }
        }

        private void TrySetPlaybackSpeed(float s)
        {
            if (Mathf.Abs(s - _lastAppliedPlaybackSpeed) < 0.001f) return;
            try
            {
                _videoPlayer.playbackSpeed = Mathf.Clamp(s, 0.25f, 3.5f);
                _lastAppliedPlaybackSpeed = _videoPlayer.playbackSpeed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VideoPlayerManager] Set speed failed: {ex.Message}");
            }
        }

        private void TryPlay()
        {
            try
            {
                _videoPlayer.Play();
            } 
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void TryPause()
        {
            try
            {
                _videoPlayer.Pause();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        #endregion

        #region CALLBACKS
        private void OnVideoPrepared(VideoPlayer source)
        {
            try
            {
                if (_renderTexture != null)
                {
                    _renderTexture.Release();
                    Destroy(_renderTexture);
                }

                //Compute RT size <= 1080p (preserve aspect ratio)
                int oW = Mathf.Max(1, SafeGetWidth());
                int oH = Mathf.Max(1, SafeGetHeight());
                float scaleW = (float)MAX_OUTPUT_WIDTH / oW;
                float scaleH = (float)MAX_OUTPUT_HEIGHT / oH;
                float scale = Mathf.Min(1f, Mathf.Min(scaleW, scaleH));
                int w = Mathf.Max(1, Mathf.RoundToInt(oW * scale));
                int h = Mathf.Max(1, Mathf.RoundToInt(oH * scale));

                _renderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    useDynamicScale = false,
                    antiAliasing = 1
                };
                _renderTexture.Create();

                _videoPlayer.targetTexture = _renderTexture;
                _videoPlayer.aspectRatio = VideoAspectRatio.NoScaling;
                _videoPlayer.sendFrameReadyEvents = false;

                // Prime player
                TryPlay();
                TryPause();

                //Seek to TimeBar current time
                double startT = (TimeBarManager.Instance != null) ? TimeBarManager.Instance.CurrentTime : 0.0;
                TrySetTime(startT);

                //Start if TimeBar is playing
                if (TimeBarManager.Instance != null && TimeBarManager.Instance.IsPlaying)
                {
                    TrySetPlaybackSpeed((float)TimeBarManager.Instance.SpeedFactor);
                    TryPlay();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VideoPlayerManager] OnVideoPrepared failed: {ex.Message}");
            }
        }

        private void OnSeekCompleted(VideoPlayer source)
        {
            //Reset playback speed after a seek
            _lastAppliedPlaybackSpeed = -999f;
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"[VideoPlayerManager] Video error: {message}");

            //Attempt recovery: short pause + relaunch
            try
            {
                TryPause();

                UnityMainThreadDispatcher.AddActionInMainThread(() =>
                {
                    TrySetPlaybackSpeed((float)(TimeBarManager.Instance != null ? TimeBarManager.Instance.SpeedFactor : 1.0));
                    TryPlay();
                });
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        #endregion

        #region UI
        public override void OnWindowCreated(FuWindow window)
        {
            window.HeaderHeight = TOP_BAR_HEIGHT;
            window.HeaderUI = DrawVideoPlayerHeader;
            window.UI = OnUI;
        }

        private void DrawVideoPlayerHeader(FuWindow window, Vector2 size)
        {
            FlightData currentFlightData = LoadingManager.Instance.CurrentFlightData;
            float scale = Fugui.CurrentContext.Scale;
            size.y = TOP_BAR_HEIGHT * scale;
            FuLayout layout = new FuLayout();

            FuStyle customStyle = new FuStyle(
                FuTextStyle.Default,
                FuFrameStyle.Default,
                new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)),
                FuStyle.Unpadded.FramePadding,
                FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("videoPlayerTopPanel", customStyle, false, window.HeaderHeight, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));
                layout.Spacing();
                layout.SameLine();

                if (currentFlightData != null)
                {
                    Fugui.PushFont(12, FontType.Bold);
                    Vector2 textSize = ImGui.CalcTextSize(currentFlightData.Name);
                    float verticalOffset = (size.y - textSize.y) / 2f;
                    Fugui.MoveY(verticalOffset);
                    layout.CenterNextItemH(currentFlightData.Name);
                    layout.Text(currentFlightData.Name);
                    Fugui.PopFont();
                }

                Fugui.PopColor();
            }

            layout.Dispose();
        }

        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            if (LoadingManager.Instance.CurrentFlightData == null || _videoPlayer == null || !IsPrepared)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            using (FuLayout layout = new FuLayout())
            {
                if (ImGui.GetCursorScreenPos().y <= Fugui.MainContainer.Size.y)
                {
                    Vector2 availableSize = ImGui.GetContentRegionAvail();
                    float scale = Fugui.CurrentContext.Scale;
                    float outerMargin = 12f * scale;
                    float innerMargin = 6f * scale;
                    float timelineHeight = TIMELINE_HEIGHT;
                    float spacingBetweenBlocks = 10f * scale;
                    float videoRatio = (SafeGetHeight() > 0) ? (float)SafeGetWidth() / SafeGetHeight() : (16f / 9f);
                    float targetWidth = availableSize.x - 2f * (outerMargin + innerMargin);
                    float targetHeight = targetWidth / videoRatio;

                    Vector2 cursorPos = ImGui.GetCursorScreenPos();
                    float totalWidth = targetWidth + 2f * innerMargin;
                    float blockPosX = cursorPos.x + MathF.Max((availableSize.x - totalWidth) / 2f, outerMargin);
                    float blockPosY = cursorPos.y + outerMargin;
                    Vector2 blockPos = new Vector2(blockPosX, blockPosY);
                    Vector2 blockEnd = blockPos + new Vector2(targetWidth, targetHeight);

                    //Draw video
                    ImGui.SetCursorScreenPos(blockPos);
                    DrawVideoImage(targetWidth, targetHeight);

                    //Add orange border around the video
                    uint borderColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
                    drawList.AddRect(blockPos, blockEnd, borderColor, 0f, ImDrawFlags.None, 1f * scale);

                    //New overlays (bottom-left & bottom-right)
                    float margin = 5f * scale;
                    float radius = 5f * scale;

                    //Background style
                    Vector4 bgCol = Fugui.Themes.GetColor(FuColors.FrameBg);
                    bgCol.w = 0.65f; // ~65% opacity
                    uint bgColor = ImGui.ColorConvertFloat4ToU32(bgCol);
                    uint borderCol = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Border));
                    uint textCol = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));

                    //Left text (playback speed)
                    string speedText = $"x{TimeBarManager.Instance.SpeedFactor:0.##}";
                    Fugui.PushFont(12, FontType.Bold);
                    Vector2 speedSize = ImGui.CalcTextSize(speedText);
                    Fugui.PopFont();

                    Vector2 speedMin = new Vector2(blockPos.x + margin, blockEnd.y - margin - speedSize.y - 4f * scale);
                    Vector2 speedMax = new Vector2(speedMin.x + speedSize.x + 10f * scale, speedMin.y + speedSize.y + 4f * scale);
                    drawList.AddRectFilled(speedMin, speedMax, bgColor, radius);
                    drawList.AddRect(speedMin, speedMax, borderCol, radius);
                    Fugui.PushFont(12, FontType.Bold);
                    drawList.AddText(new Vector2(speedMin.x + 5f * scale, speedMin.y + 2f * scale), textCol, speedText);
                    Fugui.PopFont();

                    //Right text (current time)
                    double curTime = SafeGetTime();
                    TimeSpan ts = TimeSpan.FromSeconds(curTime);
                    string timeText = ts.ToString(@"hh\:mm\:ss");
                    Fugui.PushFont(12, FontType.Bold);
                    Vector2 timeSize = ImGui.CalcTextSize(timeText);
                    Fugui.PopFont();

                    Vector2 timeMax = new Vector2(blockEnd.x - margin, blockEnd.y - margin);
                    Vector2 timeMin = new Vector2(timeMax.x - (timeSize.x + 10f * scale), timeMax.y - (timeSize.y + 4f * scale));
                    drawList.AddRectFilled(timeMin, timeMax, bgColor, radius);
                    drawList.AddRect(timeMin, timeMax, borderCol, radius);
                    Fugui.PushFont(12, FontType.Bold);
                    drawList.AddText(new Vector2(timeMin.x + 5f * scale, timeMin.y + 2f * scale), textCol, timeText);
                    Fugui.PopFont();

                    // Add spacing between video and timeline
                    ImGui.SetCursorScreenPos(new Vector2(blockPos.x, blockPos.y + targetHeight + 12f * scale));
                    DrawTimeLine(timelineHeight, targetWidth);

                    layout.Spacing();

                    // Metadata
                    float scrollPanelHeight = ImGui.GetContentRegionAvail().y - 20f * Fugui.CurrentContext.Scale;
                    Vector2 scrollPanelSize = new Vector2(ImGui.GetContentRegionAvail().x, scrollPanelHeight);

                    ImGui.BeginChild("DataScrollbalePanel", scrollPanelSize, ImGuiChildFlags.AutoResizeY);
                    int rw = _renderTexture != null ? _renderTexture.width : SafeGetWidth();
                    int rh = _renderTexture != null ? _renderTexture.height : SafeGetHeight();
                    double durationSeconds = Math.Max(0.0, SafeGetLength());
                    double frameRate = SafeGetFrameRate();

                    MetadataViewManager.Instance.DrawVideoMetadata(window, layout, rw, rh, frameRate, durationSeconds);
                    MetadataViewManager.Instance.DrawMetadata(window, layout);
                    ImGui.EndChild();
                }
            }
        }

        private void DrawVideoImage(float width, float height)
        {
            IFuWindowContainer container = FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Container : Fugui.MainContainer;

            if (!IsPrepared)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                Vector2 pos = ImGui.GetCursorScreenPos();
                Vector2 size = new Vector2(width, height);
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(new Vector4(1f, 0f, 0f, 0.5f)));
                ImGui.Dummy(size);
                return;
            }

            container.ImGuiImage(_renderTexture, new Vector2(width, height));
        }

        private void DrawTimeLine(float height, float width)
        {
            IFuWindowContainer container = FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Container : Fugui.MainContainer;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 size = new Vector2(width, height * Fugui.CurrentContext.Scale);
            TimeBarManager timeBar = TimeBarManager.Instance;

            if (timeBar == null || !timeBar.IsInitialized)
            {
                ImGui.Dummy(size);
                return;
            }

            //Colors
            uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));
            uint seakBgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint progressColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Highlight));
            uint hoverColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
            uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
            uint offsetTextCol = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));

            //Background
            drawList.AddRectFilled(pos, pos + size, seakBgColor, 4f);

            //Progress bar
            float ratio = (timeBar.Duration > 0.0) ? (float)(timeBar.CurrentTime / timeBar.Duration) : 0f;
            ratio = Mathf.Clamp01(ratio);
            float progressX = pos.x + size.x * ratio;
            drawList.AddRectFilled(pos, new Vector2(progressX, pos.y + size.y), progressColor, 4f);

            Vector2 mousePos = ImGui.GetMousePos();
            bool isHovering = ImGui.IsMouseHoveringRect(pos, pos + size);

            if (isHovering)
            {
                float hoverRatio = Mathf.Clamp01((mousePos.x - pos.x) / size.x);
                float hoverX = pos.x + size.x * hoverRatio;
                timeBar.SetHover(_windowName.Name, hoverRatio);

                DrawHoverFeedback(drawList, pos, size, ratio, progressX, hoverX, timeBar.Duration, offsetTextCol);

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    double newTime = hoverRatio * timeBar.Duration;
                    timeBar.Seek(newTime);
                    TrySetTime(newTime);
                }
            }
            else
            {
                if (timeBar.IsHovering && timeBar.HoverSourceID != _windowName.Name)
                {
                    float hoverX = pos.x + size.x * timeBar.HoverRatio;
                    DrawHoverFeedback(drawList, pos, size, ratio, progressX, hoverX, timeBar.Duration, offsetTextCol);
                }
                else if (timeBar.HoverSourceID == _windowName.Name)
                {
                    timeBar.ClearHover(_windowName.Name);
                }
            }

            //Border
            drawList.AddRect(pos, pos + size, bgColor, 4f);

            //Progress cursor
            float midY = (pos.y + pos.y + size.y) * 0.5f;
            float cursorExtend = size.y * 0.5f + 4f * Fugui.CurrentContext.Scale;
            drawList.AddLine(new Vector2(progressX, midY - cursorExtend), new Vector2(progressX, midY + cursorExtend), cursorColor, 2f * Fugui.CurrentContext.Scale);

            if (timeBar.IsHovering && timeBar.HoverRatio >= 0f)
            {
                float hoverX = pos.x + size.x * timeBar.HoverRatio;
                drawList.AddLine(new Vector2(hoverX, midY - cursorExtend), new Vector2(hoverX, midY + cursorExtend), ImGui.ColorConvertFloat4ToU32(Color.white), 2f * Fugui.CurrentContext.Scale);
            }

            ImGui.Dummy(size);
        }

        private void DrawHoverFeedback(ImDrawListPtr drawList, Vector2 pos, Vector2 size, float ratio, float progressX, float hoverX, double duration, uint offsetTextCol)
        {
            float startX = Mathf.Min(progressX, hoverX);
            float endX = Mathf.Max(progressX, hoverX);

            //Rectangle
            drawList.AddRectFilled(new Vector2(startX, pos.y), new Vector2(endX, pos.y + size.y), ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered)), 0f);

            //Offset text
            double offsetSeconds = ((hoverX - progressX) / size.x) * duration;
            TimeSpan offsetSpan = TimeSpan.FromSeconds(Math.Abs(offsetSeconds));
            string offsetText = (offsetSeconds >= 0 ? "+" : "-") + offsetSpan.ToString(@"mm\:ss");

            Fugui.PushFont(12, FontType.Bold);
            Vector2 textSize = ImGui.CalcTextSize(offsetText);
            Fugui.PopFont();

            float orangeWidth = endX - startX;

            if (orangeWidth > textSize.x + 6f * Fugui.CurrentContext.Scale)
            {
                float textX = startX + (orangeWidth - textSize.x) * 0.5f;
                float textY = pos.y + (size.y - textSize.y) * 0.5f;

                Fugui.PushFont(12, FontType.Bold);
                drawList.AddText(new Vector2(textX, textY), offsetTextCol, offsetText);
                Fugui.PopFont();
            }
        }
        #endregion
    }
}
