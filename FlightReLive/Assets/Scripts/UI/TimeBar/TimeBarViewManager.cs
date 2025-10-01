using FlightReLive.Core.TimeBar;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;
using System;
using FlightReLive.Core.Loading;

namespace FlightReLive.UI.TimeBar
{
    public static class TimeBarViewManager
    {
        #region CONSTANTS
        private const int TIME_BAR_OVERLAY_WIDTH = 450;
        private const int TIME_BAR_OVERLAY_HEIGHT = 90;
        private const float SEAK_BAR_HORIZONTAL_PADDING = 12f;
        private const float SEAK_BAR_HEIGHT = 16f;
        private const float MEDIA_BUTTON_HEIGHT = 34f;
        private const float MEDIA_BUTTON_WIDTH = 34f;
        private const float MEDIA_BUTTON_SPACING = 3f;
        private const float MEDIA_BUTTON_RADIUS = 5f;
        #endregion

        #region ATTRIBUTES
        private static FuOverlay _timeBarOverlay;
        #endregion

        #region UI
        internal static void DisplayTimeBarOverlay(FuWindowDefinition windowsDefinition, FuCameraWindow cameraWindow)
        {
            _timeBarOverlay = new FuOverlay("timeBarOverlay",
                new Vector2Int(TIME_BAR_OVERLAY_WIDTH, TIME_BAR_OVERLAY_HEIGHT),
                (overlay, layout) =>
                {
                    DisplayTimeBar(cameraWindow);
                },
               FuOverlayFlags.NoClose,
                FuOverlayDragPosition.Bottom);

            _timeBarOverlay.AnchorWindowDefinition(windowsDefinition, FuOverlayAnchorLocation.BottomCenter);
            _timeBarOverlay.SetMinimumWindowSize(new Vector2Int(TIME_BAR_OVERLAY_WIDTH, TIME_BAR_OVERLAY_HEIGHT));
        }

        internal static void DisplayTimeBar(FuCameraWindow cameraWindow)
        {
            TimeBarManager timeBar = TimeBarManager.Instance;

            if (timeBar == null || !timeBar.IsInitialized)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            float scale = Fugui.CurrentContext.Scale;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 availBefore = ImGui.GetContentRegionAvail();

            //Seek bar
            float barHeight = SEAK_BAR_HEIGHT * scale;
            float barWidth = availBefore.x - SEAK_BAR_HORIZONTAL_PADDING * scale;
            Vector2 barPos = new Vector2(cursorPos.x + (SEAK_BAR_HORIZONTAL_PADDING * 0.5f) * scale, cursorPos.y + 10f * scale);
            Vector2 barSize = new Vector2(barWidth, barHeight);
            Vector2 barEnd = barPos + barSize;

            //Theme colors
            uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint progressColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Highlight));
            uint hoverColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
            uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
            uint textZoneBg = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));
            uint offsetTextCol = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));

            //Background
            drawList.AddRectFilled(barPos, barEnd, bgColor, 4f);

            //Progress bar
            float ratio = (timeBar.Duration > 0.0) ? (float)(timeBar.CurrentTime / timeBar.Duration) : 0f;
            ratio = Mathf.Clamp01(ratio);
            float progressX = barPos.x + barSize.x * ratio;
            drawList.AddRectFilled(barPos, new Vector2(progressX, barEnd.y), progressColor, 4f);

            //Hover feedback
            Vector2 mousePos = ImGui.GetMousePos();
            bool isHovering = ImGui.IsMouseHoveringRect(barPos, barEnd);

            if (isHovering)
            {
                float hoverRatio = Mathf.Clamp01((mousePos.x - barPos.x) / barSize.x);
                float hoverX = barPos.x + barSize.x * hoverRatio;

                timeBar.SetHoverFromSeekBar(hoverRatio);

                float startX = Mathf.Min(progressX, hoverX);
                float endX = Mathf.Max(progressX, hoverX);

                //Hover rectangle
                drawList.AddRectFilled(new Vector2(startX, barPos.y), new Vector2(endX, barEnd.y), hoverColor, 0f);

                //Offset text
                double offsetSeconds = (hoverRatio - ratio) * timeBar.Duration;
                TimeSpan offsetSpan = TimeSpan.FromSeconds(Math.Abs(offsetSeconds));
                string offsetText = (offsetSeconds >= 0 ? "+" : "-") + offsetSpan.ToString(@"mm\:ss");

                Fugui.PushFont(12, FontType.Bold);
                Vector2 textSize = ImGui.CalcTextSize(offsetText);
                Fugui.PopFont();

                float orangeWidth = endX - startX;
                if (orangeWidth > textSize.x + 6f * scale)
                {
                    float textX = startX + (orangeWidth - textSize.x) * 0.5f;
                    float textY = barPos.y + (barHeight - textSize.y) * 0.5f;

                    Fugui.PushFont(12, FontType.Bold);
                    drawList.AddText(new Vector2(textX, textY), offsetTextCol, offsetText);
                    Fugui.PopFont();
                }

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    timeBar.Seek(timeBar.Duration * hoverRatio);
                }
            }
            else
            {
                //Sync hover over Path3D module and only if Path3D is the owner, not the TimeBarViewer
                if (timeBar.IsHovering && timeBar.HoverOwner != HoverOwner.SeekBar)
                {
                    float hoverX = barPos.x + barSize.x * timeBar.HoverRatio;
                    float startX = Mathf.Min(progressX, hoverX);
                    float endX = Mathf.Max(progressX, hoverX);

                    //Hover rectangle
                    drawList.AddRectFilled(new Vector2(startX, barPos.y), new Vector2(endX, barEnd.y), hoverColor, 0f);

                    // Offset text
                    double offsetSeconds = (timeBar.HoverRatio - ratio) * timeBar.Duration;
                    TimeSpan offsetSpan = TimeSpan.FromSeconds(Math.Abs(offsetSeconds));
                    string offsetText = (offsetSeconds >= 0 ? "+" : "-") + offsetSpan.ToString(@"mm\:ss");

                    Fugui.PushFont(12, FontType.Bold);
                    Vector2 textSize = ImGui.CalcTextSize(offsetText);
                    Fugui.PopFont();

                    float orangeWidth = endX - startX;
                    if (orangeWidth > textSize.x + 6f * scale)
                    {
                        float textX = startX + (orangeWidth - textSize.x) * 0.5f;
                        float textY = barPos.y + (barHeight - textSize.y) * 0.5f;

                        Fugui.PushFont(12, FontType.Bold);
                        drawList.AddText(new Vector2(textX, textY), offsetTextCol, offsetText);
                        Fugui.PopFont();
                    }
                }
                else if (timeBar.HoverOwner == HoverOwner.SeekBar)
                {
                    //Release hover
                    timeBar.ClearHover();
                }
            }

            //Reserve vertical space under the bar
            float totalBarHeight = barSize.y + 20f * scale;
            ImGui.Dummy(new Vector2(availBefore.x, totalBarHeight));

            //Buttons and text line
            FuElementSize buttonSize = new FuElementSize(MEDIA_BUTTON_WIDTH, MEDIA_BUTTON_HEIGHT);
            Vector2 btnSizePx = buttonSize.GetSize();
            float btnW = btnSizePx.x;
            float buttonsWidth = (MEDIA_BUTTON_WIDTH * 5 + MEDIA_BUTTON_SPACING * 4) * scale;
            string currentTimeStr = TimeSpan.FromSeconds(timeBar.CurrentTime).ToString(@"hh\:mm\:ss");
            string totalTimeStr = TimeSpan.FromSeconds(timeBar.Length).ToString(@"hh\:mm\:ss");

            Fugui.PushFont(12, FontType.Regular);
            Vector2 timeTextSize = ImGui.CalcTextSize(currentTimeStr);
            Vector2 totalTextSize = ImGui.CalcTextSize(totalTimeStr);
            Fugui.PopFont();

            float availWidth = ImGui.GetContentRegionAvail().x;
            float rowY = ImGui.GetCursorScreenPos().y;
            float padding = MEDIA_BUTTON_SPACING * 2f * scale;
            float radius = MEDIA_BUTTON_RADIUS * scale;
            float buttonsStartX = cursorPos.x + (availWidth - buttonsWidth) * 0.5f;
            float buttonsEndX = buttonsStartX + buttonsWidth;

            //Left zone (Speed + CurrentTime)
            Vector2 leftRectMin = new Vector2(cursorPos.x + padding + btnW + padding, rowY);
            Vector2 leftRectMax = new Vector2(buttonsStartX - padding, rowY + MEDIA_BUTTON_HEIGHT * scale);
            if (leftRectMax.x > leftRectMin.x)
            {
                drawList.AddRectFilled(leftRectMin, leftRectMax, textZoneBg, radius);
            }

            //Right zone (TotalTime + Unload)
            Vector2 rightRectMin = new Vector2(buttonsEndX + padding, rowY);
            Vector2 rightRectMax = new Vector2(cursorPos.x + availWidth - padding - btnW - padding, rowY + MEDIA_BUTTON_HEIGHT * scale);

            if (rightRectMax.x > rightRectMin.x)
            {
                drawList.AddRectFilled(rightRectMin, rightRectMax, textZoneBg, radius);
            }

            //Custom button style (hover = progress color)
            FuButtonStyle customButton = new FuButtonStyle(
                Fugui.Themes.GetColor(FuColors.Button),
                Fugui.Themes.GetColor(FuColors.ButtonHovered),
                Fugui.Themes.GetColor(FuColors.ButtonActive),
                Fugui.Themes.GetColor(FuColors.Button) * 0.5f,
                FuTextStyle.Default,
                new Vector2(8f, 4f)
            );

            //Speed button
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.x + padding, rowY));
            Fugui.PushFont(20, FontType.Regular);
            using (FuLayout layoutSpeed = new FuLayout())
            {
                string speedIcon;
                switch (TimeBarManager.Instance.Speed)
                {
                    case PlaybackSpeed.UltraSlow:
                        speedIcon = FlightReLiveIcons.SpeedUltraSlow;
                        break;
                    case PlaybackSpeed.Slow:
                        speedIcon = FlightReLiveIcons.SpeedSlow;
                        break;
                    default:
                    case PlaybackSpeed.Normal:
                        speedIcon = FlightReLiveIcons.SpeedNormal;
                        break;
                    case PlaybackSpeed.Fast:
                        speedIcon = FlightReLiveIcons.SpeedFast;
                        break;
                    case PlaybackSpeed.UltraFast:
                        speedIcon = FlightReLiveIcons.SpeedUltraFast;
                        break;
                }

                if (layoutSpeed.Button(speedIcon, buttonSize, customButton))
                {
                    TimeBarManager.Instance.ChangeSpeed();
                }
            }
            Fugui.PopFont();

            //Left text (CurrentTime)
            Fugui.PushFont(12, FontType.Regular);
            if (leftRectMax.x > leftRectMin.x)
            {
                float leftTextX = leftRectMin.x + (leftRectMax.x - leftRectMin.x - timeTextSize.x) * 0.5f;
                float leftTextY = rowY + (MEDIA_BUTTON_HEIGHT * scale - timeTextSize.y) * 0.5f;
                ImGui.SetCursorScreenPos(new Vector2(leftTextX, leftTextY));
                ImGui.Text(currentTimeStr);
            }
            Fugui.PopFont();

            //Media buttons
            Fugui.Push(ImGuiStyleVar.FrameRounding, radius * 0.5f);
            Fugui.PushFont(20, FontType.Regular);
            ImGui.SetCursorScreenPos(new Vector2(buttonsStartX, rowY));

            using (FuLayout layout = new FuLayout())
            {
                if (layout.Button(FlightReLiveIcons.BackwardStep, buttonSize, customButton))
                {
                    timeBar.BackwardStep();
                }

                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);

                if (layout.Button(FlightReLiveIcons.Backward, buttonSize, customButton))
                {
                    timeBar.BackwardPoint();
                }

                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);
                string iconPlayOrPause = timeBar.IsPlaying ? FlightReLiveIcons.Pause : FlightReLiveIcons.Play;

                if (layout.Button(iconPlayOrPause, buttonSize, customButton))
                {
                    if (timeBar.IsPlaying) { timeBar.Pause(); }
                    else { timeBar.Play(); }
                }

                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);

                if (layout.Button(FlightReLiveIcons.Forward, buttonSize, customButton))
                {
                    timeBar.ForwardPoint();
                }

                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);

                if (layout.Button(FlightReLiveIcons.ForwardStep, buttonSize, customButton))
                {
                    timeBar.ForwardStep();
                }
            }
            Fugui.PopFont();
            Fugui.PopStyle();

            //Right text
            Fugui.PushFont(12, FontType.Regular);
            if (rightRectMax.x > rightRectMin.x)
            {
                float rightTextX = rightRectMin.x + (rightRectMax.x - rightRectMin.x - totalTextSize.x) * 0.5f;
                float rightTextY = rowY + (MEDIA_BUTTON_HEIGHT * scale - totalTextSize.y) * 0.5f;
                ImGui.SetCursorScreenPos(new Vector2(rightTextX, rightTextY));
                ImGui.Text(totalTimeStr);
            }
            Fugui.PopFont();

            //Unload button
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.x + availWidth - padding - btnW, rowY));
            Fugui.PushFont(20, FontType.Regular);
            using (FuLayout layoutUnload = new FuLayout())
            {
                if (layoutUnload.Button(FlightReLiveIcons.Unload, buttonSize, customButton))
                {
                    LoadingManager.Instance.UnloadFlightData();
                }
            }
            Fugui.PopFont();

            //Draw cursor
            float midY = (barPos.y + barEnd.y) * 0.5f;
            float cursorExtend = barHeight * 0.5f + 4f * scale;

            //Progress cursor
            drawList.AddLine(new Vector2(progressX, midY - cursorExtend), new Vector2(progressX, midY + cursorExtend), cursorColor, 2f * scale);

            //Hover cursor (SeekBar or Path3D)
            if (timeBar.IsHovering && timeBar.HoverRatio >= 0f)
            {
                float hoverX = barPos.x + barSize.x * timeBar.HoverRatio;
                drawList.AddLine(new Vector2(hoverX, midY - cursorExtend), new Vector2(hoverX, midY + cursorExtend),
                                 ImGui.ColorConvertFloat4ToU32(Color.white), 2f * scale);
            }
        }

        #endregion
    }
}
