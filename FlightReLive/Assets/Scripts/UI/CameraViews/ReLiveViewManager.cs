using System;
using FlightReLive.Core.Cameras;
using FlightReLive.Core.Capture;
using FlightReLive.Core.Environment;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Settings;
using FlightReLive.Core.TimeBar;
using FlightReLive.Core.UI.Overlays;
using FlightReLive.UI.Overlays;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace FlightReLive.UI.CameraViews
{
    internal class ReLiveViewManager : FuCameraWindowBehaviour
    {
        #region CONSTANTS
        protected const float HEADER_BAR_HEIGHT = 26f;
        protected const float FOOTER_BAR_HEIGHT = 26f;
        private const float CAMERA_MODE_WIDTH = 150f;
        private const float TOGGLE_CAPTURE_WIDTH = 103f;
        private const float SETTINGS_POPUP_BUTTON_WIDTH = 42f;
        private const float SETTINGS_POPUP_WIDTH = 300f;
        private const float DAY_CYCLE_BAR_WIDTH = 432f;
        private const float DAY_CYCLE_BAR_HEIGHT = 10f;
        private const float DAY_CYCLE_TEXT_AREA_WIDTH = 50f;
        #endregion

        #region ATTRIBUTES
        private TimeBarOverlay _timeBarOverlay;
        private CompassOverlay _compassOverlay;
        #endregion

        #region PROPERTIES
        public static ReLiveViewManager Instance { get; private set; }
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
            TimeBarManager.Instance.RegisterWindowName(_windowName);
        }

        private void OnDestroy()
        {
            TimeBarManager.Instance.UnregisterWindowName(_windowName);
        }
        #endregion

        #region METHODS
        public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
            windowDefinition.SetHeaderUI(DrawHeaderBar, HEADER_BAR_HEIGHT);
            windowDefinition.SetFooterUI(DrawFooterBar, FOOTER_BAR_HEIGHT);
            windowDefinition.SetUI(OnUI);

            _timeBarOverlay = new TimeBarOverlay();
            _timeBarOverlay.DisplayTimeBarOverlay(windowDefinition, CameraWindow);
            _compassOverlay = new CompassOverlay();
            _compassOverlay.DisplayCompassOverlay(windowDefinition, Camera);
        }

        public override void OnWindowCreated(FuWindow window)
        {
            ExternalCameraManipulator.Instance.CameraWindow = CameraWindow;
            PathManager.Instance.Camera = CameraWindow;
        }

        protected void DrawHeaderBar(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            size.y = HEADER_BAR_HEIGHT * scale;
            float unscaledHeight = size.y / scale;
            FuLayout layout = new FuLayout();
            FuStyle customStyle = new FuStyle(FuTextStyle.Default, FuFrameStyle.Default, new FuPanelStyle((Color)Fugui.Themes.GetColor(FuColors.MenuBarBg), (Color)Fugui.Themes.GetColor(FuColors.Border)), FuStyle.Unpadded.FramePadding, FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("panelHeader", customStyle, false, window.HeaderHeight, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));
                Fugui.MoveX(4f);
                Fugui.MoveY(5f);

                float totalWidth = layout.GetAvailableWidth();

                float leftToggleWidth = TOGGLE_CAPTURE_WIDTH * scale;
                float rightGroupWidth = 5 * (SETTINGS_POPUP_BUTTON_WIDTH + Fugui.Themes.CurrentTheme.ItemSpacing.x) * scale;
                float cameraGroupWidth = CAMERA_MODE_WIDTH * scale;

                bool canShowAll = rightGroupWidth < (totalWidth / 2f) - (cameraGroupWidth / 2f);
                bool canShowToggleAndCamera = leftToggleWidth < (totalWidth / 2f) - (cameraGroupWidth / 2f);
                bool canShowCameraOnly = totalWidth >= (cameraGroupWidth + 8f * scale);

                //Toggle Capture
                if (canShowToggleAndCamera)
                {
                    if (!LoadingManager.Instance.IsLoaded)
                    {
                        layout.DisableNextElement();
                    }

                    bool captureState = CaptureManager.Instance.IsCapturing;
                    layout.SetNextElementToolTip(captureState ? "Stop the current video capture" : "Start a new video capture");

                    if (layout.Toggle("recToggle", ref captureState, "Start capture", "Stop capture", FuToggleFlags.AlignLeft))
                    {
                        CaptureManager.Instance.ToggleCapture();
                    }

                    layout.SameLine();
                }

                //Camera Mode Buttons
                if (canShowCameraOnly)
                {
                    FuButtonsGroupStyle buttonStyle = FuButtonsGroupStyle.Default;
                    Vector2 iconPadding = new Vector2(6f * scale, 4f * scale);

                    //Centered buttongroup Camera mode
                    float cameraGroupX = (totalWidth - cameraGroupWidth) * 0.5f;
                    ImGui.SetCursorPosX(cameraGroupX);

                    if (canShowToggleAndCamera)
                    {
                        Fugui.MoveY(-4f);
                    }
                    else
                    {
                        Fugui.MoveY(-2f);
                    }

                    layout.SetNextElementToolTip("Change camera mode.");
                    layout.ButtonsGroup<CameraMode>(
                        "CameraMode",
                        (value) => { ExternalCameraManipulator.Instance.Mode = (CameraMode)value; },
                        () => ExternalCameraManipulator.Instance.Mode,
                        width: cameraGroupWidth,
                        padding: iconPadding,
                        flags: FuButtonsGroupFlags.AlignLeft,
                        style: buttonStyle
                    );

                    layout.SameLine();
                }

                //Options buttons
                if (canShowAll)
                {
                    float popUpWidth = SETTINGS_POPUP_WIDTH * scale;
                    Fugui.PushFont(14, FontType.Regular);
                    Fugui.MoveXUnscaled(layout.GetAvailableWidth() - rightGroupWidth);

                    layout.SetNextElementToolTip("Capture settings");
                    PopupButton(layout, FlightReLiveIcons.Camera, () => DrawCaptureSettings(layout), new Vector2(popUpWidth, 0f), size, unscaledHeight, scale);
                    layout.SameLine();

                    layout.SetNextElementToolTip("Sun / clouds settings");
                    PopupButton(layout, FlightReLiveIcons.SunClouds, () => DrawSunCloudsSettings(layout), new Vector2(popUpWidth, 0f), size, unscaledHeight, scale);
                    layout.SameLine();

                    layout.SetNextElementToolTip("Post-processing settings");
                    PopupButton(layout, FlightReLiveIcons.PostProcess, () => DrawPostProcessingSettings(layout), new Vector2(popUpWidth, 0f), size, unscaledHeight, scale);
                    layout.SameLine();

                    layout.SetNextElementToolTip("Open path settings");
                    PopupButton(layout, FlightReLiveIcons.Path, () => DrawPathSettings(layout), new Vector2(popUpWidth, 0f), size, unscaledHeight, scale);
                    layout.SameLine();

                    layout.SetNextElementToolTip("Open scene settings");
                    PopupButton(layout, FlightReLiveIcons.AltitudeRelative, () => DrawSceneSettings(layout), new Vector2(popUpWidth, 0f), size, unscaledHeight, scale);

                    Fugui.PopFont();
                }

                ImGui.Dummy(Vector2.zero);
                Fugui.PopColor();
            }

            layout.Dispose();
        }

        private void PopupButton(FuLayout layout, string text, Action popupUI, Vector2 popupSize, Vector2 size, float height, float scale)
        {
            Vector2 cursorPos = ImGui.GetCursorScreenPos();

            if (layout.Button(text, new FuElementSize(SETTINGS_POPUP_BUTTON_WIDTH, height - 6f), new Vector2(6f, 0f) * Fugui.CurrentContext.Scale, new Vector2(0f, 0f), Fugui.Themes.CurrentTheme.ButtonsGradientStrenght, FuButtonStyle.Default, false, 0f))
            {
                Fugui.OpenPopUp("PopUp" + text, popupUI, () => { });
            }

            Fugui.DrawCarret_Down(ImGui.GetWindowDrawList(), cursorPos + new Vector2((SETTINGS_POPUP_BUTTON_WIDTH * scale) - (size.y / 2f), 0f), (size.y - 4f) / 3f, size.y - 4f, Fugui.Themes.GetColor(FuColors.Text) * 0.8f);
            Vector2 popupPos = new Vector2(ImGui.GetItemRectMax().x - popupSize.x, ImGui.GetItemRectMax().y + (4f * scale));
            Fugui.DrawPopup("PopUp" + text, popupSize, popupPos);
        }

        protected void DrawFooterBar(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            size.y = FOOTER_BAR_HEIGHT * scale;
            FuLayout layout = new FuLayout();

            FuStyle customStyle = new FuStyle(FuTextStyle.Default, FuFrameStyle.Default, new FuPanelStyle((Color)Fugui.Themes.GetColor(FuColors.MenuBarBg), (Color)Fugui.Themes.GetColor(FuColors.Border)), FuStyle.Unpadded.FramePadding, FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("panelFooter", customStyle, false, window.FooterHeight, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));

                float availableWidth = layout.GetAvailableWidth();
                float totalWidth = availableWidth;
                float textZoneWidth = DAY_CYCLE_TEXT_AREA_WIDTH * scale;
                float barWidth = DAY_CYCLE_BAR_WIDTH * scale;
                float barHeight = (DAY_CYCLE_BAR_HEIGHT * scale) + (4f * scale);
                float padding = Fugui.Themes.CurrentTheme.ItemSpacing.x * scale;

                float requiredWidth = (textZoneWidth * 2f) + (padding * 2f) + barWidth;
                if (totalWidth < requiredWidth)
                {
                    Fugui.PopColor();
                    layout.Dispose();
                    return;
                }

                if (EnvironmentManager.Instance != null && LoadingManager.Instance.IsLoaded)
                {
                    TimeZoneInfo userTz = SettingsManager.CurrentSettings.UserTimeZone;
                    DateTime utcBase = EnvironmentManager.Instance.FlightTimeUTC.Date;
                    DateTime currentUtc = utcBase.AddMinutes(1440.0 * EnvironmentManager.Instance.DayRatio);
                    DateTime currentLocal = TimeZoneInfo.ConvertTimeFromUtc(currentUtc, userTz);
                    string currentTime = currentLocal.ToString("HH:mm");

                    DateTime originalLocal = TimeZoneInfo.ConvertTimeFromUtc(EnvironmentManager.Instance.FlightTimeUTC, userTz);
                    string originalTime = originalLocal.ToString("HH:mm");

                    Fugui.PushFont(12, FontType.Regular);
                    Vector2 currentSize = ImGui.CalcTextSize(currentTime);
                    Vector2 originalSize = ImGui.CalcTextSize(originalTime);
                    Fugui.PopFont();

                    Vector2 cursor = ImGui.GetCursorScreenPos();
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                    uint textBg = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Tab));
                    uint textBorder = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.TabSelectedOverline));
                    uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
                    uint markerColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));

                    //Central horizontal position
                    float centerX = cursor.x + (totalWidth * 0.5f);
                    float barMinX = centerX - (barWidth * 0.5f);
                    float barMaxX = centerX + (barWidth * 0.5f);

                    //Vertical coords
                    float barY = cursor.y + (size.y - barHeight) * 0.4f;
                    Vector2 barMin = new Vector2(barMinX, barY);
                    Vector2 barMax = new Vector2(barMaxX, barY + barHeight);

                    //Left text zone
                    Vector2 leftRectMax = new Vector2(barMin.x - padding, barMax.y);
                    Vector2 leftRectMin = new Vector2(leftRectMax.x - textZoneWidth, barMin.y);
                    drawList.AddRectFilled(leftRectMin, leftRectMax, textBg, 4f);
                    drawList.AddRect(leftRectMin, leftRectMax, textBorder, 4f);

                    float leftTextX = leftRectMin.x + (textZoneWidth - currentSize.x) * 0.5f;
                    float leftTextY = barMin.y + ((barHeight - currentSize.y) * 0.5f);
                    ImGui.SetCursorScreenPos(new Vector2(leftTextX, leftTextY));
                    Fugui.PushFont(12, FontType.Regular);
                    layout.SetNextElementToolTip("Current scene time (local time).");
                    layout.Text(currentTime);
                    Fugui.PopFont();

                    //Right text zone
                    Vector2 rightRectMin = new Vector2(barMax.x + padding, barMin.y);
                    Vector2 rightRectMax = new Vector2(rightRectMin.x + textZoneWidth, barMax.y);
                    drawList.AddRectFilled(rightRectMin, rightRectMax, textBg, 4f);
                    drawList.AddRect(rightRectMin, rightRectMax, textBorder, 4f);

                    float rightTextX = rightRectMin.x + (textZoneWidth - originalSize.x) * 0.5f;
                    float rightTextY = barMin.y + ((barHeight - originalSize.y) * 0.5f);
                    ImGui.SetCursorScreenPos(new Vector2(rightTextX, rightTextY));
                    Fugui.PushFont(12, FontType.Regular);
                    layout.SetNextElementToolTip("Original flight time(local time).");
                    layout.Text(originalTime);
                    Fugui.PopFont();

                    //Day cycle bar
                    float progressRatio = EnvironmentManager.Instance.DayRatio;
                    float progressX = Mathf.Lerp(barMin.x, barMax.x, progressRatio);

                    if (EnvironmentManager.Instance.SunTimes.HasSunrise && EnvironmentManager.Instance.SunTimes.HasSunset)
                    {
                        double sunriseSeconds = EnvironmentManager.Instance.SunTimes.SunriseUTC.TimeOfDay.TotalSeconds;
                        double sunsetSeconds = EnvironmentManager.Instance.SunTimes.SunsetUTC.TimeOfDay.TotalSeconds;
                        float sunriseRatio = Mathf.Clamp01((float)(sunriseSeconds / 86400.0));
                        float sunsetRatio = Mathf.Clamp01((float)(sunsetSeconds / 86400.0));

                        float sunriseX = Mathf.Lerp(barMin.x, barMax.x, sunriseRatio);
                        float sunsetX = Mathf.Lerp(barMin.x, barMax.x, sunsetRatio);

                        //Fixed colors
                        Vector4 nightColor = new Vector4(0.114f, 0.224f, 0.667f, 1f);   // #1D39AA deep blue night
                        Vector4 orangeColor = new Vector4(1.0f, 0.55f, 0.2f, 1f);       // #FF8C33 warm orange sunrise/sunset
                        Vector4 dayColor = new Vector4(0.96f, 0.85f, 0.66f, 1f);        // #F5D9A8 soft beige daylight

                        float radius = 3f * scale;
                        float fadeWidth = (barMax.x - barMin.x) * 0.04f;
                        float orangeWidth = (barMax.x - barMin.x) * 0.06f;

                        float preDawnStart = Mathf.Max(barMin.x, sunriseX - fadeWidth);    // Night => Sunrise
                        float dawnEnd = Mathf.Min(barMax.x, sunriseX + orangeWidth);       // Sunrise => Day
                        float duskStart = Mathf.Max(barMin.x, sunsetX - orangeWidth);      // Day => Sunset
                        float postDuskEnd = Mathf.Min(barMax.x, sunsetX + fadeWidth);      // Sunset => Night

                        //Night
                        if (preDawnStart > barMin.x)
                        {
                            drawList.AddRectFilled(barMin, new Vector2(preDawnStart, barMax.y), ImGui.ColorConvertFloat4ToU32(nightColor), radius, ImDrawFlags.RoundCornersLeft);
                        }

                        //Night => Sunrise
                        drawList.AddRectFilledMultiColor(new Vector2(preDawnStart, barMin.y), new Vector2(sunriseX, barMax.y), ImGui.ColorConvertFloat4ToU32(nightColor), ImGui.ColorConvertFloat4ToU32(orangeColor), ImGui.ColorConvertFloat4ToU32(orangeColor), ImGui.ColorConvertFloat4ToU32(nightColor));

                        //Sunrise
                        drawList.AddRectFilled(new Vector2(sunriseX, barMin.y), new Vector2(dawnEnd, barMax.y), ImGui.ColorConvertFloat4ToU32(orangeColor));

                        //Sunrise => Day
                        drawList.AddRectFilledMultiColor(new Vector2(dawnEnd, barMin.y), new Vector2(dawnEnd + fadeWidth, barMax.y), ImGui.ColorConvertFloat4ToU32(orangeColor), ImGui.ColorConvertFloat4ToU32(dayColor), ImGui.ColorConvertFloat4ToU32(dayColor), ImGui.ColorConvertFloat4ToU32(orangeColor));

                        //Day
                        float dayZoneStart = dawnEnd + fadeWidth;
                        float dayZoneEnd = duskStart - fadeWidth;

                        if (dayZoneEnd > dayZoneStart)
                        {
                            drawList.AddRectFilled(new Vector2(dayZoneStart, barMin.y), new Vector2(dayZoneEnd, barMax.y), ImGui.ColorConvertFloat4ToU32(dayColor));
                        }

                        //Day => Sunset
                        drawList.AddRectFilledMultiColor(new Vector2(dayZoneEnd, barMin.y), new Vector2(duskStart, barMax.y), ImGui.ColorConvertFloat4ToU32(dayColor), ImGui.ColorConvertFloat4ToU32(orangeColor), ImGui.ColorConvertFloat4ToU32(orangeColor), ImGui.ColorConvertFloat4ToU32(dayColor));

                        //Sunset
                        drawList.AddRectFilled(new Vector2(duskStart, barMin.y), new Vector2(sunsetX, barMax.y), ImGui.ColorConvertFloat4ToU32(orangeColor));

                        //Sunset => Night
                        drawList.AddRectFilledMultiColor(new Vector2(sunsetX, barMin.y), new Vector2(postDuskEnd, barMax.y), ImGui.ColorConvertFloat4ToU32(orangeColor), ImGui.ColorConvertFloat4ToU32(nightColor), ImGui.ColorConvertFloat4ToU32(nightColor), ImGui.ColorConvertFloat4ToU32(orangeColor));

                        //Night
                        if (postDuskEnd < barMax.x)
                        {
                            drawList.AddRectFilled(new Vector2(postDuskEnd, barMin.y), barMax, ImGui.ColorConvertFloat4ToU32(nightColor), radius, ImDrawFlags.RoundCornersRight);
                        }

                        //Bar outline
                        drawList.AddRect(barMin, barMax, textBorder, radius);

                        //Draw sunset / sunrise cursors
                        drawList.AddLine(new Vector2(sunriseX, barMin.y - 2f * scale), new Vector2(sunriseX, barMax.y + 2 * scale), markerColor, 1f * scale);
                        drawList.AddLine(new Vector2(sunsetX, barMin.y - 2f * scale), new Vector2(sunsetX, barMax.y + 2 * scale), markerColor, 1f * scale);
                    }
                    else
                    {
                        Vector4 nightColor = new Vector4(0.114f, 0.224f, 0.667f, 1f);
                        float radius = 3f * scale;
                        drawList.AddRectFilled(barMin, barMax, ImGui.ColorConvertFloat4ToU32(nightColor), radius, ImDrawFlags.RoundCornersAll);
                        drawList.AddRect(barMin, barMax, textBorder, radius);
                    }

                    //Draw cursor
                    drawList.AddLine(new Vector2(progressX, barMin.y - 4f * scale), new Vector2(progressX, barMax.y + 4f * scale), textBorder, 5f * scale);
                    drawList.AddLine(new Vector2(progressX, barMin.y - 2f * scale), new Vector2(progressX, barMax.y + 2f * scale), cursorColor, 3f * scale);
                    drawList.AddLine(new Vector2(progressX, barMin.y * scale), new Vector2(progressX, barMax.y * scale), textBorder, 1f * scale);

                    //Hover interaction
                    Vector2 mouse = ImGui.GetMousePos();
                    if (ImGui.IsMouseHoveringRect(barMin, barMax))
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            float ratio = Mathf.Clamp01((mouse.x - barMin.x) / (barMax.x - barMin.x));
                            EnvironmentManager.Instance.ApplyTimeOfDay(ratio);
                        }
                    }
                }

                Fugui.PopColor();
            }

            ImGui.Dummy(Vector2.zero);
            layout.Dispose();
        }


        private void DrawCaptureSettings(FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            CaptureManager.Instance.DrawCaptureModeSettings(layout);
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawSunCloudsSettings(FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            EnvironmentManager.Instance.DrawSunCloudsSettings(layout);
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawPostProcessingSettings(FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            EnvironmentManager.Instance.DrawPostProcessingSettings(layout);
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawPathSettings(FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            PathManager.Instance.DrawPathSettings(layout);
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawSceneSettings(FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            EnvironmentManager.Instance.DrawSceneSettings(layout);
            ImGui.Dummy(Vector2.zero);
        }
        #endregion
    }
}
