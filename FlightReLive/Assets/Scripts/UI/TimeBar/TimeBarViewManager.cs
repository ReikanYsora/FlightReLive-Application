using FlightReLive.Core.TimeBar;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;
using System;

namespace FlightReLive.UI.TimeBar
{
    public static class TimeBarViewManager
    {
        #region CONSTANTS
        private const int TIME_BAR_OVERLAY_WIDTH = 600;
        private const int TIME_BAR_OVERLAY_HEIGHT = 105;
        private const float SEAK_BAR_HORIZONTAL_PADDING = 20f;
        private const float SEAK_BAR_HEIGHT = 20f;
        private const float MEDIA_BUTTON_HEIGHT = 40f;
        private const float MEDIA_BUTTON_WIDTH = 40f;
        private const float MEDIA_BUTTON_SPACING = 5f;
        private const float MEDIA_BUTTON_RADIUS = 5f;
        #endregion

        #region UI
        internal static void DisplayTimeBarOverlay(FuWindowDefinition windowsDefinition, FuCameraWindow cameraWindow)
        {
            FuOverlay fps1 = new FuOverlay("timeBarOverlay",
                new Vector2Int(TIME_BAR_OVERLAY_WIDTH, TIME_BAR_OVERLAY_HEIGHT),
                (overlay, layout) => { DisplayTimeBar(cameraWindow); },
                FuOverlayFlags.Default,
                FuOverlayDragPosition.Bottom);

            fps1.AnchorWindowDefinition(windowsDefinition, FuOverlayAnchorLocation.BottomCenter);
        }

        internal static void DisplayTimeBar(FuCameraWindow cameraWindow)
        {
            TimeBarManager timeBar = TimeBarManager.Instance;
            if (timeBar == null)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            float scale = Fugui.CurrentContext.Scale;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 availableSize = ImGui.GetContentRegionAvail();

            // === Seek bar ===
            float barHeight = SEAK_BAR_HEIGHT * scale;
            float barWidth = availableSize.x - SEAK_BAR_HORIZONTAL_PADDING * scale;
            Vector2 barPos = new Vector2(cursorPos.x + (SEAK_BAR_HORIZONTAL_PADDING / 2f) * scale, cursorPos.y + 10f * scale);
            Vector2 barSize = new Vector2(barWidth, barHeight);
            Vector2 barEnd = barPos + barSize;

            uint backgroundColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
            uint progressColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Highlight));
            uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
            uint darkBgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered)); // gris sombre du thème

            // Background
            drawList.AddRectFilled(barPos, barEnd, backgroundColor, 4f);

            // Progress
            float ratio = (timeBar.Duration > 0) ? (float)(timeBar.CurrentTime / timeBar.Duration) : 0f;
            ratio = Mathf.Clamp01(ratio);

            float filledWidth = barSize.x * ratio;
            Vector2 filledEnd = new Vector2(barPos.x + filledWidth, barEnd.y);
            drawList.AddRectFilled(barPos, filledEnd, progressColor, 4f);

            // Cursor (symétrique et centré verticalement)
            float cursorX = barPos.x + filledWidth;
            float midY = (barPos.y + barEnd.y) * 0.5f;
            float cursorExtend = barHeight * 0.5f + 4f * scale;
            drawList.AddLine(
                new Vector2(cursorX, midY - cursorExtend),
                new Vector2(cursorX, midY + cursorExtend),
                cursorColor,
                2f * scale
            );

            // Interaction
            Vector2 mousePos = ImGui.GetMousePos();
            bool isHovering = ImGui.IsMouseHoveringRect(barPos, barEnd);
            if (isHovering)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    float clickRatio = (mousePos.x - barPos.x) / barSize.x;
                    clickRatio = Mathf.Clamp01(clickRatio);
                    timeBar.SeekRatio(clickRatio);
                }
            }

            float totalBarHeight = barSize.y + 20f * scale;
            ImGui.Dummy(new Vector2(availableSize.x, totalBarHeight));

            // === Ligne boutons + textes ===
            FuElementSize buttonSize = new FuElementSize(MEDIA_BUTTON_WIDTH, MEDIA_BUTTON_HEIGHT);
            float buttonsWidth = (MEDIA_BUTTON_WIDTH * 6 + MEDIA_BUTTON_SPACING * 5) * scale;

            string currentTimeStr = TimeSpan.FromSeconds(timeBar.CurrentTime).ToString(@"mm\:ss\.fff");
            string pointLabel = $"Point {timeBar.CurrentFrame} / {timeBar.TotalFrameCount}";

            Vector2 timeTextSize = ImGui.CalcTextSize(currentTimeStr);
            Vector2 pointTextSize = ImGui.CalcTextSize(pointLabel);

            float availWidth = ImGui.GetContentRegionAvail().x;

            float buttonsStartX = cursorPos.x + (availWidth - buttonsWidth) * 0.5f;
            float buttonsEndX = buttonsStartX + buttonsWidth;

            float y = ImGui.GetCursorScreenPos().y;
            float padding = MEDIA_BUTTON_SPACING * 2f * scale; // padding doublé

            // Zone gauche (fond sombre étendu)
            Vector2 leftRectMin = new Vector2(cursorPos.x + padding, y);
            Vector2 leftRectMax = new Vector2(buttonsStartX - padding, y + MEDIA_BUTTON_HEIGHT * scale);
            drawList.AddRectFilled(leftRectMin, leftRectMax, darkBgColor, MEDIA_BUTTON_RADIUS * scale);

            Fugui.PushFont(14, FontType.Regular);
            float leftTextX = leftRectMin.x + (leftRectMax.x - leftRectMin.x - timeTextSize.x) * 0.5f;
            float leftTextY = y + (MEDIA_BUTTON_HEIGHT * scale - timeTextSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(leftTextX, leftTextY));
            ImGui.Text(currentTimeStr);
            Fugui.PopFont();

            // Boutons (centrés)
            Fugui.Push(ImGuiStyleVar.FrameRounding, MEDIA_BUTTON_RADIUS * scale);
            Fugui.PushFont(20, FontType.Regular);

            // Style boutons personnalisé (hover = couleur progression)
            FuButtonStyle customButton = new FuButtonStyle(
                Fugui.Themes.GetColor(FuColors.Button),
                Fugui.Themes.GetColor(FuColors.Highlight), // hover = bleu progress
                Fugui.Themes.GetColor(FuColors.ButtonActive),
                Fugui.Themes.GetColor(FuColors.Button) * 0.5f,
                FuTextStyle.Default,
                new Vector2(8f, 4f)
            );

            ImGui.SetCursorScreenPos(new Vector2(buttonsStartX, y));
            using (FuLayout layout = new FuLayout())
            {
                if (layout.Button(FlightReLiveIcons.BackwardStep, buttonSize, customButton)) { }
                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);
                if (layout.Button(FlightReLiveIcons.Backward, buttonSize, customButton)) { }
                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);
                if (layout.Button(FlightReLiveIcons.Play, buttonSize, customButton)) { }
                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);
                if (layout.Button(FlightReLiveIcons.Stop, buttonSize, customButton)) { }
                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);
                if (layout.Button(FlightReLiveIcons.Forward, buttonSize, customButton)) { }
                ImGui.SameLine(0, MEDIA_BUTTON_SPACING * scale);
                if (layout.Button(FlightReLiveIcons.ForwardStep, buttonSize, customButton)) { }
            }

            Fugui.PopFont();
            Fugui.PopStyle();

            // Zone droite (fond sombre étendu)
            Vector2 rightRectMin = new Vector2(buttonsEndX + padding, y);
            Vector2 rightRectMax = new Vector2(cursorPos.x + availWidth - padding, y + MEDIA_BUTTON_HEIGHT * scale);
            drawList.AddRectFilled(rightRectMin, rightRectMax, darkBgColor, MEDIA_BUTTON_RADIUS * scale);

            Fugui.PushFont(14, FontType.Regular);
            float rightTextX = rightRectMin.x + (rightRectMax.x - rightRectMin.x - pointTextSize.x) * 0.5f;
            float rightTextY = y + (MEDIA_BUTTON_HEIGHT * scale - pointTextSize.y) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(rightTextX, rightTextY));
            ImGui.Text(pointLabel);
            Fugui.PopFont();
        }




        #endregion
    }
}
