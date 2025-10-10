using FlightReLive.Core.Environment;
using FlightReLive.Core.Loading;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace FlightReLive.UI.Overlays
{
    /// <summary>
    /// Day/Night cycle overlay — identical layout and metrics to TimeBarOverlay,
    /// with sunrise/sunset markers and info zone.
    /// </summary>
    public class DayCycleOverlay
    {
        #region CONSTANTS
        private const int OVERLAY_WIDTH = 450;
        private const int OVERLAY_HEIGHT = 90;
        private const float SEAK_BAR_HORIZONTAL_PADDING = 12f;
        private const float SEAK_BAR_HEIGHT = 16f;
        private const float BUTTON_HEIGHT = 34f;
        private const float BUTTON_WIDTH = 34f;
        private const float BUTTON_RADIUS = 5f;
        private const float TEXT_ZONE_WIDTH = 80f;
        #endregion

        #region ATTRIBUTES
        private FuOverlay _overlay;
        private float _displayedProgress;
        private float _hoveredProgress = -1f;
        private float _preHoverProgress;
        private bool _isHovering;
        #endregion

        #region UI
        internal void DisplayDayCycleOverlay(FuWindowDefinition windowDefinition)
        {
            _overlay = new FuOverlay("dayCycleOverlay",
                new Vector2Int(OVERLAY_WIDTH, OVERLAY_HEIGHT),
                (overlay, layout) => { DisplayDayCycle(); },
                FuOverlayFlags.NoClose | FuOverlayFlags.NoEditAnchor | FuOverlayFlags.NoBackground | FuOverlayFlags.NoMove,
                FuOverlayDragPosition.Bottom);

            _overlay.AnchorWindowDefinition(windowDefinition, FuOverlayAnchorLocation.BottomLeft);
            _overlay.SetMinimumWindowSize(new Vector2Int(OVERLAY_WIDTH * 2 + 20, OVERLAY_HEIGHT));
        }

        private void DisplayDayCycle()
        {
            if (EnvironmentManager.Instance == null || !LoadingManager.Instance.IsLoaded)
                return;

            float scale = Fugui.CurrentContext.Scale;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            // === Colors ===
            uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));
            uint barBgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint progressColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Highlight));
            uint hoverColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
            uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
            uint textZoneBg = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint textZoneBorder = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Border));
            uint textColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));

            // === Global background ===
            Vector2 globalMin = new Vector2(cursorPos.x, cursorPos.y - (SEAK_BAR_HORIZONTAL_PADDING * 0.5f * scale));
            Vector2 globalMax = new Vector2(cursorPos.x + avail.x, cursorPos.y + (OVERLAY_HEIGHT * scale) - (SEAK_BAR_HORIZONTAL_PADDING * scale));
            drawList.AddRectFilled(globalMin, globalMax, bgColor, BUTTON_RADIUS * scale, ImDrawFlags.RoundCornersAll);

            // === SEEK BAR ===
            float barHeight = SEAK_BAR_HEIGHT * scale;
            float barWidth = avail.x - SEAK_BAR_HORIZONTAL_PADDING * scale;
            Vector2 barPos = new Vector2(cursorPos.x + (SEAK_BAR_HORIZONTAL_PADDING * 0.5f) * scale, cursorPos.y + 5f * scale);
            Vector2 barEnd = new Vector2(barPos.x + barWidth, barPos.y + barHeight);

            drawList.AddRectFilled(barPos, barEnd, barBgColor, 4f);

            float targetProgress = EnvironmentManager.Instance.DayTime;
            if (!_isHovering)
                _displayedProgress = Mathf.Lerp(_displayedProgress, targetProgress, Time.deltaTime * 4f);

            float progressX = Mathf.Lerp(barPos.x, barEnd.x, _displayedProgress);
            drawList.AddRectFilled(barPos, new Vector2(progressX, barEnd.y), progressColor, 4f);

            // === Hover behavior ===
            Vector2 mousePos = ImGui.GetMousePos();
            bool isHovering = ImGui.IsMouseHoveringRect(barPos, barEnd);

            if (isHovering)
            {
                float hoverRatio = Mathf.Clamp01((mousePos.x - barPos.x) / barWidth);
                float hoverX = barPos.x + barWidth * hoverRatio;

                if (!_isHovering)
                {
                    _isHovering = true;
                    _preHoverProgress = EnvironmentManager.Instance.DayTime;
                }

                _hoveredProgress = hoverRatio;
                EnvironmentManager.Instance.ApplyTimeOfDay(_hoveredProgress);

                float startX = Mathf.Min(progressX, hoverX);
                float endX = Mathf.Max(progressX, hoverX);

                drawList.AddRectFilled(new Vector2(startX, barPos.y), new Vector2(endX, barEnd.y), hoverColor, 0f);

                double offsetSeconds = (hoverRatio - _displayedProgress) * 86400.0;
                TimeSpan offsetSpan = TimeSpan.FromSeconds(Math.Abs(offsetSeconds));
                string offsetText = (offsetSeconds >= 0 ? "+" : "-") + offsetSpan.ToString(@"hh\:mm");

                Fugui.PushFont(12, FontType.Bold);
                Vector2 textSize = ImGui.CalcTextSize(offsetText);
                Fugui.PopFont();

                float orangeWidth = endX - startX;
                if (orangeWidth > textSize.x + 6f * scale)
                {
                    float textX = startX + (orangeWidth - textSize.x) * 0.5f;
                    float textY = barPos.y + (barHeight - textSize.y) * 0.5f;
                    Fugui.PushFont(12, FontType.Bold);
                    drawList.AddText(new Vector2(textX, textY), textColor, offsetText);
                    Fugui.PopFont();
                }

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _displayedProgress = hoverRatio;
                    EnvironmentManager.Instance.ApplyTimeOfDay(hoverRatio);
                    _isHovering = false;
                    _hoveredProgress = -1f;
                }
            }
            else if (_isHovering)
            {
                EnvironmentManager.Instance.ApplyTimeOfDay(_preHoverProgress);
                _isHovering = false;
                _hoveredProgress = -1f;
            }

            // === Main cursor ===
            float midY = (barPos.y + barEnd.y) * 0.5f;
            float cursorExtend = barHeight * 0.5f + 4f * scale;

            drawList.AddLine(new Vector2(progressX, midY - cursorExtend), new Vector2(progressX, midY + cursorExtend), cursorColor, 2f * scale);

            // === Hover cursor ===
            if (_hoveredProgress >= 0f)
            {
                float hoverX = Mathf.Lerp(barPos.x, barEnd.x, _hoveredProgress);
                drawList.AddLine(new Vector2(hoverX, midY - cursorExtend), new Vector2(hoverX, midY + cursorExtend), ImGui.ColorConvertFloat4ToU32(Color.white), 2f * scale);
            }

            // === Sunrise/Sunset markers ===
            if (EnvironmentManager.Instance.SunTimes.HasSunrise)
            {
                float sunriseRatio = (float)(EnvironmentManager.Instance.SunTimes.SunriseUTC.TimeOfDay.TotalSeconds / 86400.0);
                float sunriseX = Mathf.Lerp(barPos.x, barEnd.x, sunriseRatio);
                drawList.AddLine(new Vector2(sunriseX, barPos.y), new Vector2(sunriseX, barEnd.y), ImGui.ColorConvertFloat4ToU32(new Color(1f, 0.8f, 0.3f)), 1.5f * scale);
            }

            if (EnvironmentManager.Instance.SunTimes.HasSunset)
            {
                float sunsetRatio = (float)(EnvironmentManager.Instance.SunTimes.SunsetUTC.TimeOfDay.TotalSeconds / 86400.0);
                float sunsetX = Mathf.Lerp(barPos.x, barEnd.x, sunsetRatio);
                drawList.AddLine(new Vector2(sunsetX, barPos.y), new Vector2(sunsetX, barEnd.y), ImGui.ColorConvertFloat4ToU32(new Color(1f, 0.6f, 0.2f)), 1.5f * scale);
            }

            // === Row below ===
            float seakBarHeight = barHeight + 15f * scale;
            ImGui.Dummy(new Vector2(avail.x, seakBarHeight));

            float rowY = ImGui.GetCursorScreenPos().y;
            float rowHeight = BUTTON_HEIGHT * scale;
            float padding = SEAK_BAR_HORIZONTAL_PADDING * scale;
            float radius = BUTTON_RADIUS * scale;

            // Times
            DateTime currentDate = new DateTime(2024, 1, 1, 0, 0, 0).AddMinutes(1440.0 * _displayedProgress);
            string currentTimeText = currentDate.ToString("HH:mm");
            string originalTimeText = EnvironmentManager.Instance.OriginalTimeUTC.ToLocalTime().ToString("HH:mm");

            string sunriseText = EnvironmentManager.Instance.SunTimes.HasSunrise
                ? EnvironmentManager.Instance.SunTimes.SunriseUTC.ToLocalTime().ToString("HH:mm")
                : "--:--";
            string sunsetText = EnvironmentManager.Instance.SunTimes.HasSunset
                ? EnvironmentManager.Instance.SunTimes.SunsetUTC.ToLocalTime().ToString("HH:mm")
                : "--:--";
            string middleText = $"☀ {sunriseText}  |  🌙 {sunsetText}";

            Fugui.PushFont(12, FontType.Regular);
            Vector2 currentSize = ImGui.CalcTextSize(currentTimeText);
            Vector2 originalSize = ImGui.CalcTextSize(originalTimeText);
            Vector2 middleSize = ImGui.CalcTextSize(middleText);
            Fugui.PopFont();

            // Left zone
            Vector2 leftRectMin = new Vector2(cursorPos.x + padding, rowY);
            Vector2 leftRectMax = new Vector2(leftRectMin.x + TEXT_ZONE_WIDTH * scale, rowY + rowHeight);
            drawList.AddRectFilled(leftRectMin, leftRectMax, textZoneBg, radius);
            drawList.AddRect(leftRectMin, leftRectMax, textZoneBorder, radius);

            Fugui.PushFont(12, FontType.Regular);
            float leftTextX = leftRectMin.x + (leftRectMax.x - leftRectMin.x - currentSize.x) * 0.5f;
            float leftTextY = rowY + (BUTTON_HEIGHT * scale - currentSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(leftTextX, leftTextY));
            ImGui.Text(currentTimeText);
            Fugui.PopFont();

            // Middle zone (sunrise/sunset info)
            Vector2 middleRectMin = new Vector2(leftRectMax.x + padding, rowY);
            Vector2 middleRectMax = new Vector2(cursorPos.x + avail.x - (TEXT_ZONE_WIDTH + BUTTON_WIDTH + padding * 3f) * scale, rowY + rowHeight);
            drawList.AddRectFilled(middleRectMin, middleRectMax, textZoneBg, radius);
            drawList.AddRect(middleRectMin, middleRectMax, textZoneBorder, radius);

            Fugui.PushFont(12, FontType.Regular);
            float middleTextX = middleRectMin.x + (middleRectMax.x - middleRectMin.x - middleSize.x) * 0.5f;
            float middleTextY = rowY + (BUTTON_HEIGHT * scale - middleSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(middleTextX, middleTextY));
            ImGui.Text(middleText);
            Fugui.PopFont();

            // Right zone (original time)
            Vector2 rightRectMin = new Vector2(middleRectMax.x + padding, rowY);
            Vector2 rightRectMax = new Vector2(rightRectMin.x + TEXT_ZONE_WIDTH * scale, rowY + rowHeight);
            drawList.AddRectFilled(rightRectMin, rightRectMax, textZoneBg, radius);
            drawList.AddRect(rightRectMin, rightRectMax, textZoneBorder, radius);

            Fugui.PushFont(12, FontType.Regular);
            float rightTextX = rightRectMin.x + (rightRectMax.x - rightRectMin.x - originalSize.x) * 0.5f;
            float rightTextY = rowY + (BUTTON_HEIGHT * scale - originalSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(rightTextX, rightTextY));
            ImGui.Text(originalTimeText);
            Fugui.PopFont();

            // Reset button (right)
            Vector2 resetBtnMin = new Vector2(rightRectMax.x + padding, rowY);
            Vector2 resetBtnMax = new Vector2(resetBtnMin.x + BUTTON_WIDTH * scale, rowY + rowHeight);

            FuButtonStyle customButton = new FuButtonStyle(
                Fugui.Themes.GetColor(FuColors.Button),
                Fugui.Themes.GetColor(FuColors.ButtonHovered),
                Fugui.Themes.GetColor(FuColors.ButtonActive),
                Fugui.Themes.GetColor(FuColors.Button) * 0.5f,
                FuTextStyle.Default,
                new Vector2(8f, 4f)
            );

            ImGui.SetCursorScreenPos(new Vector2(resetBtnMin.x, rowY));
            Fugui.PushFont(20, FontType.Regular);
            using (FuLayout layout = new FuLayout())
            {
                layout.SetNextElementToolTip("Reset to original flight time");
                if (layout.Button(FlightReLiveIcons.Undo, new FuElementSize(BUTTON_WIDTH, BUTTON_HEIGHT), customButton))
                {
                    EnvironmentManager.Instance.ResetTimeOfDay();
                    _displayedProgress = EnvironmentManager.Instance.DayTime;
                }
            }
            Fugui.PopFont();
        }
        #endregion
    }
}
