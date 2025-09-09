using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Settings;
using FlightReLive.UI.FlightCharts;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FlightReLive.UI.VideoPlayer
{
    internal class VideoPlayerManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float TOP_BAR_HEIGHT = 26f;
        #endregion

        #region ATTRIBUTES
        private FuVideoPlayer _videoPlayer;
        private int _lastPointIndex = -1;
        #endregion

        #region PROPERTIES
        internal static VideoPlayerManager Instance { get; private set; }

        internal float Progress
        {
            get
            {
                if (_videoPlayer == null || _videoPlayer.Player == null || !_videoPlayer.Player.isPrepared || _videoPlayer.Player.length == 0)
                {
                    return 0f;
                }

                return (float)(_videoPlayer.Player.time / _videoPlayer.Player.length);
            }
        }

        internal long TotalFrameCount
        {
            get
            {
                if (_videoPlayer == null || _videoPlayer.Player == null)
                {
                    return 0;
                }

                return (long)_videoPlayer.FrameCount;
            }
        }

        internal long CurrentFrame
        {
            get
            {
                if (_videoPlayer == null || _videoPlayer.Player == null)
                {
                    return 0;
                }

                return _videoPlayer.CurrentFrame;
            }
        }

        internal double Time
        {
            get
            {
                if (_videoPlayer == null || _videoPlayer.Player == null || !_videoPlayer.Player.isPrepared || _videoPlayer.Player.length == 0)
                {
                    return 0f;
                }

                return _videoPlayer.Player.time;
            }
        }

        internal double Length
        {
            get
            {
                if (_videoPlayer == null || _videoPlayer.Player == null || !_videoPlayer.Player.isPrepared || _videoPlayer.Player.length == 0)
                {
                    return 0f;
                }

                return _videoPlayer.Player.length;
            }
        }

        internal Texture Texture
        {
            get
            {
                if (_videoPlayer == null)
                {
                    return null;
                }

                return _videoPlayer.Texture;
            }
        }
        #endregion

        #region EVENTS
        internal event Action<float, int, FlightDataPoint> OnProgressChanged;
        internal event Action<FlightData> OnVideoLoaded;
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
        }

        private void Start()
        {
            FuLayout layout = new FuLayout();
            _videoPlayer = layout.GetVideoPlayer("VideoPlayer");
            _videoPlayer.SetAutoPlay(true);
            layout.Dispose();
        }

        private void Update()
        {
            FlightData currentFlightData = LoadingManager.Instance.CurrentFlightData;

            if (currentFlightData == null || _videoPlayer?.Player == null || currentFlightData.Points == null || currentFlightData.Points.Count == 0)
            {
                return;
            }

            double currentTime = _videoPlayer.Player.time;
            double duration = _videoPlayer.Player.length;

            if (duration <= 0)
            {
                return;
            }

            float progress = (float)(currentTime / duration);
            TimeSpan currentSpan = TimeSpan.FromSeconds(currentTime);
            TimeSpan first = currentFlightData.Points.First().TimeSpan;
            TimeSpan last = currentFlightData.Points.Last().TimeSpan;
            currentSpan = TimeSpan.FromTicks(Math.Clamp(currentSpan.Ticks, first.Ticks, last.Ticks));

            int index = FindClosestPointIndex(currentFlightData.Points, currentSpan);

            if (index != _lastPointIndex)
            {
                _lastPointIndex = index;
                FlightDataPoint point = currentFlightData.Points[index];
                OnProgressChanged?.Invoke(progress, index, point);
            }
        }
        #endregion

        #region METHODS
        internal void LoadFlightVideo(FlightData flightData)
        {
            if (flightData == null || _videoPlayer == null || !File.Exists(flightData.VideoPath))
            {
                return;
            }

            _videoPlayer.SetFile(flightData.VideoPath);
            OnVideoLoaded?.Invoke(flightData);
        }

        internal void UnloadFlightVideo()
        {
            if (_videoPlayer == null)
            {
                return;
            }

            _videoPlayer.Stop();
        }

        private FlightDataPoint GetSafePoint(int index)
        {
            FlightData currentFlightData = LoadingManager.Instance.CurrentFlightData;

            if (currentFlightData?.Points == null || index < 0 || index >= currentFlightData.Points.Count)
            {
                return null;
            }

            return currentFlightData.Points[index];
        }

        private int FindClosestPointIndex(List<FlightDataPoint> points, TimeSpan currentSpan)
        {
            int low = 0;
            int high = points.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                TimeSpan midSpan = points[mid].TimeSpan;

                if (midSpan < currentSpan)
                {
                    low = mid + 1;
                }
                else if (midSpan > currentSpan)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            int before = Mathf.Clamp(low - 1, 0, points.Count - 1);
            int after = Mathf.Clamp(low, 0, points.Count - 1);

            TimeSpan diffBefore = (points[before].TimeSpan - currentSpan).Duration();
            TimeSpan diffAfter = (points[after].TimeSpan - currentSpan).Duration();

            return diffBefore <= diffAfter ? before : after;
        }


        internal void SetFrame(long frame, bool pause)
        {
            if (_videoPlayer == null)
            {
                return;
            }

            _videoPlayer.SetFrame(frame);

            if (pause)
            {
                _videoPlayer.Pause();
            }
        }
        #endregion

        #region CALLBACK
        /// <summary>
        /// Whenever the window is created, set the camera to the MouseOrbitImproved component
        /// </summary>
        /// <param name="window"> FuWindow instance</param>
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
            float unscaledHeight = size.y / scale;
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

                    layout.CenterNextItem(currentFlightData.Name);
                    layout.Text(currentFlightData.Name);
                    Fugui.PopFont();
                }

                Fugui.PopColor();
            }

            layout.Dispose();
        }

        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            if (LoadingManager.Instance.CurrentFlightData == null || _videoPlayer == null || _videoPlayer.Player == null || !_videoPlayer.Player.isPrepared)
            {
                return;
            }

            FlightData currentFlightData = LoadingManager.Instance.CurrentFlightData;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            using (FuLayout layout = new FuLayout())
            {
                if (ImGui.GetCursorScreenPos().y <= Fugui.MainContainer.Size.y)
                {
                    FlightDataPoint point = GetSafePoint(_lastPointIndex);

                    if (point == null)
                    {
                        return;
                    }

                    Vector2 availableSize = ImGui.GetContentRegionAvail();
                    float scale = Fugui.CurrentContext.Scale;
                    float outerMargin = 12f * scale;
                    float innerMargin = 6f * scale;
                    float timelineHeight = 12f;
                    float spacingBetweenBlocks = 10f * scale;

                    float videoRatio = (float)_videoPlayer.Player.width / _videoPlayer.Player.height;
                    float targetWidth = availableSize.x - 2f * (outerMargin + innerMargin);
                    float targetHeight = targetWidth / videoRatio;

                    float gridRowHeight = 22f * scale;
                    int gridRowCount = 9;
                    float gridHeight = gridRowHeight * gridRowCount;
                    float totalContentHeight = spacingBetweenBlocks + targetHeight + timelineHeight + gridHeight + spacingBetweenBlocks + 4f * innerMargin;

                    if (totalContentHeight > availableSize.y - 2f * outerMargin)
                    {
                        targetHeight = availableSize.y - 2f * outerMargin - 4f * innerMargin - spacingBetweenBlocks * 2f - timelineHeight - gridHeight;
                        targetWidth = targetHeight * videoRatio;
                    }

                    Vector2 cursorPos = ImGui.GetCursorScreenPos();
                    float totalWidth = targetWidth + 2f * innerMargin;
                    float blockPosX = cursorPos.x + MathF.Max((availableSize.x - totalWidth) / 2f, outerMargin);

                    float blockPosY = cursorPos.y + outerMargin;
                    Vector2 blockPos = new Vector2(blockPosX, blockPosY);
                    Vector2 blockSize = new Vector2(targetWidth, targetHeight + timelineHeight);
                    Vector2 blockMax = blockPos + blockSize;

                    Vector2 backgroundMin = blockPos - new Vector2(innerMargin, innerMargin);
                    Vector2 backgroundMax = blockMax + new Vector2(innerMargin, innerMargin);

                    drawList.AddRectFilled(backgroundMin, backgroundMax, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TitleBgCollapsed)), 4f);

                    ImGui.SetCursorScreenPos(blockPos);
                    _videoPlayer.DrawImage(targetWidth, targetHeight);

                    ImGui.SetCursorScreenPos(new Vector2(blockPos.x, blockPos.y + targetHeight));
                    _videoPlayer.DrawTimeLine(timelineHeight, targetWidth);

                    layout.Spacing();

                    float scrollPanelHeight = ImGui.GetContentRegionAvail().y - 20f * Fugui.CurrentContext.Scale;
                    Vector2 scrollPanelSize = new Vector2(ImGui.GetContentRegionAvail().x, scrollPanelHeight);


                    ImGui.BeginChild("DataScrollbalePanel", scrollPanelSize, ImGuiChildFlags.AutoResizeY);

                    layout.Collapsable(FlightReLiveIcons.VideoFile + "  Video##collapsable", () =>
                    {
                        Fugui.PushFont(14, FontType.Regular);

                        using (FuGrid grid = new FuGrid("positionDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                        {
                            string formattedResolution = $"{_videoPlayer.Player.texture.width}x{_videoPlayer.Player.texture.height}";

                            double durationSeconds = _videoPlayer.Player.length;
                            ulong frameCount = _videoPlayer.Player.frameCount;
                            double frameRate = frameCount / durationSeconds;
                            string formattedFramerate = $"{frameRate:F2} FPS";

                            TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                            string formattedDuration = duration.ToString(@"hh\:mm\:ss");

                            Draw(window, "11", grid, layout, FlightReLiveIcons.Resolution, formattedResolution, "Native resolution");
                            Draw(window, "12", grid, layout, FlightReLiveIcons.Framerate, formattedFramerate, "Framerate");
                            Draw(window, "13", grid, layout, FlightReLiveIcons.Duration, formattedDuration, "Duration");
                        }

                        Fugui.PopFont();
                    }, FuButtonStyle.Collapsable, defaultOpen: true);

                    layout.Collapsable(FlightReLiveIcons.Drone + "  Drone##collapsable", () =>
                    {
                        Fugui.PushFont(14, FontType.Regular);

                        using (FuGrid grid = new FuGrid("positionDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                        {
                            //GPS Position
                            string formattedPosition = $"{point.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {point.Longitude.ToString("F5", CultureInfo.InvariantCulture)}";
                            Draw(window, "1", grid, layout, FlightReLiveIcons.GPSMarker, formattedPosition, "Current drone position", FlightReLiveIcons.GoogleMaps, () =>
                            {
                                GoogleAPIHelper.OpenGoogleMapsBrowser(new Vector2((float)point.Latitude, (float)point.Longitude));
                            }, "Display on Google Map");

                            //Altitudes
                            string formattedAbsoluteAltitude = SettingsManager.FormatAltitude(currentFlightData.TakeOffAltitude + point.RelativeAltitude);
                            string formattedRelativeAltitude = SettingsManager.FormatAltitude(point.RelativeAltitude);

                            //Speed
                            double speed = CalculateSpeed((float)point.HorizontalSpeed, (float)point.VerticalSpeed);
                            string formattedSpeed = SettingsManager.FormatSpeed(speed);

                            Draw(window, "2", grid, layout, FlightReLiveIcons.Speed, formattedSpeed, "Current speed", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.Speed;
                            }, "Display speed chart");

                            Draw(window, "3", grid, layout, FlightReLiveIcons.AltitudeRelative, formattedRelativeAltitude, "Relative altitude to take-off position", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.RelativeAltitude;
                            }, "Display relative altitude chart");

                            Draw(window, "4", grid, layout, FlightReLiveIcons.AltitudeAbsolute, formattedAbsoluteAltitude, "Absolute altitude", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.AbsoluteAltitude;
                            }, "Display absolute altitude chart");
                        }

                        Fugui.PopFont();
                    }, FuButtonStyle.Collapsable, defaultOpen: true);

                    layout.Collapsable(FlightReLiveIcons.Camera + "  Camera##collapsable", () =>
                    {
                        Fugui.PushFont(14, FontType.Regular);

                        using (FuGrid grid = new FuGrid("cameraDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                        {
                            Draw(window, "5", grid, layout, FlightReLiveIcons.Aperture, point.CameraSettings.Aperture.ToString(), "Aperture", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.Aperture;
                            }, "Display Aperture chart");

                            Draw(window, "6", grid, layout, FlightReLiveIcons.ShutterSpeed, point.CameraSettings.ShutterSpeed.ToString(), "Shutter Speed", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.ShutterSpeed;
                            }, "Display Shutter speed chart");

                            Draw(window, "7", grid, layout, FlightReLiveIcons.PostProcess, point.CameraSettings.FocalLength.ToString(), "Focal Length", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.Focal;
                            }, "Display Focal length chart");

                            Draw(window, "8", grid, layout, FlightReLiveIcons.ISO, point.CameraSettings.ISO.ToString(), "ISO", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.ISO;
                            }, "Display ISO chart");

                            Draw(window, "9", grid, layout, FlightReLiveIcons.Exposure, point.CameraSettings.Exposure.ToString(), "Exposure", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.Exposure;
                            }, "Display exposure chart");

                            string formattedZoom = $"X{point.CameraSettings.DigitalZoom:F1}";
                            Draw(window, "10", grid, layout, FlightReLiveIcons.DigitalZoom, formattedZoom, "Digital Zoom", FlightReLiveIcons.Charts, () =>
                            {
                                FlightChartsManager.Instance.DisplayedChart = FlightChartType.DigitalZoom;
                            }, "Display digital zoom");
                        }

                        Fugui.PopFont();
                    }, FuButtonStyle.Collapsable, defaultOpen: true);

                    ImGui.EndChild();
                }
            }
        }

        private void Draw(FuWindow window, string actionId, FuGrid grid, FuLayout layout, string icon, string value, string tooltip, string actionText = null, Action actionButton = null, string actionTooltip = null) 
        {
            grid.SetNextElementToolTipWithLabel(tooltip);
            Fugui.PushFont(12, FontType.Regular);
            grid.Text(icon);
            Fugui.PopFont();
            grid.NextColumn();

            layout.FramedText(value, 0.5f);

            if (layout.LastItemHovered)
            {
                DrawContextualMenu(window, value);
            }

            grid.NextColumn();

            if (!string.IsNullOrEmpty(actionText) && actionButton != null)
            {
                if (!string.IsNullOrEmpty(actionTooltip))
                {
                    layout.SetNextElementToolTip(actionTooltip);
                }

                Fugui.PushFont(12, FontType.Regular);

                string uniqueButtonLabel = $"{actionText}##{actionId}";

                if (layout.Button(uniqueButtonLabel, FuElementSize.AutoSize, new Vector2(10f, 4f), Vector2.zero, FuButtonStyle.Default))
                {
                    actionButton?.Invoke();
                }

                Fugui.PopFont();
            }
        }


        private void DrawContextualMenu(FuWindow window, string value)
        {
            if (window.Mouse.IsDown(FuMouseButton.Right))
            {
                FuContextMenuBuilder contextMenuBuilder = FuContextMenuBuilder.Start();

                contextMenuBuilder.AddItem(FlightReLiveIcons.Duplicate + " Copy", () =>
                {
                    ImGui.SetClipboardText(value.ToString());
                    Fugui.Notify("Value copied to clipboard");
                });

                List<FuContextMenuItem> contextMenuItems = contextMenuBuilder.Build();
                Fugui.PushContextMenuItems(contextMenuItems);
                Fugui.TryOpenContextMenu();
                Fugui.PopContextMenuItems();
            }
        }

        private float CalculateSpeed(float horizontalSpeed, float verticalSpeed)
        {
            return Mathf.Sqrt(horizontalSpeed * horizontalSpeed + verticalSpeed * verticalSpeed);
        }
        #endregion
    }
}
