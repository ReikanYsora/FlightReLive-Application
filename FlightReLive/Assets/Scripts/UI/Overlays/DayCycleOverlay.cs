using FlightReLive.Core.Environment;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace FlightReLive.UI.Overlays
{
    /// <summary>
    /// Day/Night cycle overlay — identical layout and metrics to TimeBarOverlay,
    /// with sunrise/sunset icons on the bar and separated info zones.
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

            //Define minimum size to avoir collision with time bar overlay
            _overlay.SetMinimumWindowSize(new Vector2Int(OVERLAY_WIDTH * 3 - 8, OVERLAY_HEIGHT));
        }

        private void DisplayDayCycle()
        {
            if (EnvironmentManager.Instance == null || !LoadingManager.Instance.IsLoaded)
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            //Colors
            uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));
            uint barBgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint progressColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Highlight));
            uint hoverColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
            uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
            uint textZoneBg = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint textZoneBorder = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Border));
            uint textColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
            uint semiTransparentBg = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg) * new Color(1f, 1f, 1f, 0.4f));

            //Global background 
            Vector2 globalMin = new Vector2(cursorPos.x, cursorPos.y - (SEAK_BAR_HORIZONTAL_PADDING * 0.5f * scale));
            Vector2 globalMax = new Vector2(cursorPos.x + avail.x, cursorPos.y + (OVERLAY_HEIGHT * scale) - (SEAK_BAR_HORIZONTAL_PADDING * scale));
            drawList.AddRectFilled(globalMin, globalMax, bgColor, BUTTON_RADIUS * scale, ImDrawFlags.RoundCornersAll);

            //Seek bar
            float barHeight = SEAK_BAR_HEIGHT * scale;
            float barWidth = avail.x - SEAK_BAR_HORIZONTAL_PADDING * scale;
            Vector2 barPos = new Vector2(cursorPos.x + (SEAK_BAR_HORIZONTAL_PADDING * 0.5f) * scale, cursorPos.y + 5f * scale);
            Vector2 barEnd = new Vector2(barPos.x + barWidth, barPos.y + barHeight);
            drawList.AddRectFilled(barPos, barEnd, barBgColor, 4f);

            float targetProgress = EnvironmentManager.Instance.DayTime;
            if (!_isHovering)
            {
                _displayedProgress = Mathf.Lerp(_displayedProgress, targetProgress, Time.deltaTime * 4f);
            }

            float progressX = Mathf.Lerp(barPos.x, barEnd.x, _displayedProgress);
            drawList.AddRectFilled(barPos, new Vector2(progressX, barEnd.y), progressColor, 4f);

            //Hover
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

                if (endX - startX > textSize.x + 6f * scale)
                {
                    float textX = startX + ((endX - startX - textSize.x) * 0.5f);
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

            //Main cursor
            float midY = (barPos.y + barEnd.y) * 0.5f;
            float cursorExtend = barHeight * 0.5f + 4f * scale;
            drawList.AddLine(new Vector2(progressX, midY - cursorExtend), new Vector2(progressX, midY + cursorExtend), cursorColor, 2f * scale);

            if (_hoveredProgress >= 0f)
            {
                float hoverX = Mathf.Lerp(barPos.x, barEnd.x, _hoveredProgress);
                drawList.AddLine(new Vector2(hoverX, midY - cursorExtend), new Vector2(hoverX, midY + cursorExtend), ImGui.ColorConvertFloat4ToU32(Color.white), 2f * scale);
            }

            //Sunrise / Sunset icons
            float iconOffsetY = barPos.y + (barHeight * 0.5f) - (8f * scale);
            Vector2 cursorBackup = ImGui.GetCursorScreenPos();
            Fugui.PushFont(16, FontType.Regular);

            if (EnvironmentManager.Instance.SunTimes.HasSunrise)
            {
                float sunriseRatio = (float)(EnvironmentManager.Instance.SunTimes.SunriseUTC.TimeOfDay.TotalSeconds / 86400.0);
                float sunriseX = Mathf.Lerp(barPos.x, barEnd.x, sunriseRatio);
                ImGui.SetCursorScreenPos(new Vector2(sunriseX - 8f * scale, iconOffsetY));
                ImGui.Text(FlightReLiveIcons.Sunrise);
            }

            if (EnvironmentManager.Instance.SunTimes.HasSunset)
            {
                float sunsetRatio = (float)(EnvironmentManager.Instance.SunTimes.SunsetUTC.TimeOfDay.TotalSeconds / 86400.0);
                float sunsetX = Mathf.Lerp(barPos.x, barEnd.x, sunsetRatio);
                ImGui.SetCursorScreenPos(new Vector2(sunsetX - 8f * scale, iconOffsetY));
                ImGui.Text(FlightReLiveIcons.Sunset);
            }
            Fugui.PopFont();
            ImGui.SetCursorScreenPos(cursorBackup);

            //Second row 
            float seakBarHeight = barHeight + 15f * scale;
            ImGui.Dummy(new Vector2(avail.x, seakBarHeight));

            float rowY = ImGui.GetCursorScreenPos().y;
            float rowHeight = BUTTON_HEIGHT * scale;
            float padding = SEAK_BAR_HORIZONTAL_PADDING * scale;
            float radius = BUTTON_RADIUS * scale;

            //Times
            DateTime currentDate = new DateTime(2024, 1, 1, 0, 0, 0).AddMinutes(1440.0 * _displayedProgress);
            string currentTimeText = currentDate.ToString("HH:mm");
            string originalTimeText = EnvironmentManager.Instance.OriginalTimeUTC.ToLocalTime().ToString("HH:mm");
            string sunriseText = EnvironmentManager.Instance.SunTimes.HasSunrise
                ? $"Sunrise: {EnvironmentManager.Instance.SunTimes.SunriseUTC.ToLocalTime():HH:mm}"
                : "Sunrise: --:--";
            string sunsetText = EnvironmentManager.Instance.SunTimes.HasSunset
                ? $"Sunset: {EnvironmentManager.Instance.SunTimes.SunsetUTC.ToLocalTime():HH:mm}"
                : "Sunset: --:--";

            Fugui.PushFont(12, FontType.Regular);
            Vector2 currentSize = ImGui.CalcTextSize(currentTimeText);
            Vector2 originalSize = ImGui.CalcTextSize(originalTimeText);
            Fugui.PopFont();

            //Left zone (current)
            float reducedPaddingLeft = padding * 0.5f;
            Vector2 leftRectMin = new Vector2(cursorPos.x + reducedPaddingLeft, rowY);
            Vector2 leftRectMax = new Vector2(leftRectMin.x + TEXT_ZONE_WIDTH * scale, rowY + rowHeight);
            drawList.AddRectFilled(leftRectMin, leftRectMax, textZoneBg, radius);
            drawList.AddRect(leftRectMin, leftRectMax, textZoneBorder, radius);
            Fugui.PushFont(12, FontType.Regular);
            float leftTextX = leftRectMin.x + (TEXT_ZONE_WIDTH * scale - currentSize.x) * 0.5f;
            float leftTextY = rowY + (rowHeight - currentSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(leftTextX, leftTextY));

            ImGui.Text(currentTimeText);
            Fugui.PopFont();

            //Middle zone (Sunrise & Sunset combined)
            float reducedPaddingSides = padding * 0.5f;
            float totalMidWidth = avail.x - ((TEXT_ZONE_WIDTH * 2 + BUTTON_WIDTH) * scale + (padding * 2.5f));
            Vector2 middleRectMin = new Vector2(leftRectMax.x + reducedPaddingSides, rowY);
            Vector2 middleRectMax = new Vector2(middleRectMin.x + totalMidWidth, rowY + rowHeight);
            drawList.AddRectFilled(middleRectMin, middleRectMax, semiTransparentBg, radius);
            string middleText = $"{sunriseText}   |   {sunsetText}";
            Fugui.PushFont(12, FontType.Regular);
            Vector2 middleSize = ImGui.CalcTextSize(middleText);
            float middleTextX = middleRectMin.x + (totalMidWidth - middleSize.x) * 0.5f;
            float middleTextY = rowY + (rowHeight - middleSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(middleTextX, middleTextY));
            ImGui.Text(middleText);
            Fugui.PopFont();

            //Right zone (original time)
            Vector2 rightRectMin = new Vector2(middleRectMax.x + reducedPaddingSides, rowY);
            Vector2 rightRectMax = new Vector2(rightRectMin.x + TEXT_ZONE_WIDTH * scale, rowY + rowHeight);
            drawList.AddRectFilled(rightRectMin, rightRectMax, textZoneBg, radius);
            drawList.AddRect(rightRectMin, rightRectMax, textZoneBorder, radius);

            Fugui.PushFont(12, FontType.Regular);
            float rightTextX = rightRectMin.x + (TEXT_ZONE_WIDTH * scale - originalSize.x) * 0.5f;
            float rightTextY = rowY + (rowHeight - originalSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(rightTextX, rightTextY));
            ImGui.Text(originalTimeText);
            Fugui.PopFont();

            //Reset button
            Vector2 resetBtnMin = new Vector2(rightRectMax.x + padding, rowY);
            Vector2 resetBtnMax = new Vector2(resetBtnMin.x + BUTTON_WIDTH * scale, rowY + rowHeight);

            resetBtnMin.x -= padding * 0.5f;
            resetBtnMax.x -= padding * 0.5f;

            FuButtonStyle customButton = new FuButtonStyle(
                Fugui.Themes.GetColor(FuColors.Button),
                Fugui.Themes.GetColor(FuColors.ButtonHovered),
                Fugui.Themes.GetColor(FuColors.ButtonActive),
                Fugui.Themes.GetColor(FuColors.Button) * 0.5f,
                FuTextStyle.Default,
                new Vector2(8f, 4f)
            );

            ImGui.SetCursorScreenPos(resetBtnMin);
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
