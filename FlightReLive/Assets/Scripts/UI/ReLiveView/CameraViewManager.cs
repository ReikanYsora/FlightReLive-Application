using FlightReLive.Core;
using FlightReLive.Core.Cameras;
using FlightReLive.Core.Capture;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Scene;
using FlightReLive.Core.Terrain;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace FlightReLive.UI.ReLiveView
{
    public class ReLiveViewManager : FuCameraWindowBehaviour
    {
        #region CONSTANTS
        private const float TOP_BAR_HEIGHT = 26f;
        private const float SETTINGS_POPUP_BUTTON_WIDTH = 42f;
        private const float SETTINGS_POPUP_WIDTH = 300f;
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
        #endregion

        #region UI
        private void DrawCameraWindowSettingBar(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            size.y = TOP_BAR_HEIGHT * scale;
            float unscaledHeight = size.y / scale;
            FuLayout layout = new FuLayout();

            FuStyle customStyle = new FuStyle(
                FuTextStyle.Default,
                FuFrameStyle.Default,
                new FuPanelStyle((Color)Fugui.Themes.GetColor(FuColors.MenuBarBg), (Color)Fugui.Themes.GetColor(FuColors.Border)),
                FuStyle.Unpadded.FramePadding,
                FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("SceneSettings", customStyle, false, window.HeaderHeight, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));
                Fugui.MoveX(4f);
                Fugui.MoveY(5f);
                ImGui.BeginGroup();
                layout.Spacing();
                layout.SameLine();

                float totalWidth = ImGui.GetContentRegionAvail().x;
                float rightGroupWidth = 4 * (SETTINGS_POPUP_BUTTON_WIDTH + Fugui.Themes.CurrentTheme.ItemSpacing.x) * scale;

                //Capture toggle
                layout.SetNextElementToolTip("Start / Stop a recording");
                ImGui.SameLine();

                bool isCapturing = CaptureManager.Instance.IsCapturing;
                bool wasCapturing = isCapturing;

                if (layout.Toggle("Screen capture mode", ref isCapturing, "Start recording", "Stop recording", FuToggleFlags.AlignLeft))
                {
                    if (isCapturing && !wasCapturing)
                    {
                        CaptureManager.Instance.StartCapture();
                    }
                    else if (!isCapturing && wasCapturing)
                    {
                        CaptureManager.Instance.StopCapture();
                    }
                }
                ImGui.SameLine();

                if (isCapturing && CaptureManager.Instance.ElapsedTime != null)
                {
                    Fugui.MoveX(30f);
                    layout.Text(CaptureManager.Instance.ElapsedTime, FuTextStyle.Danger);
                    ImGui.SameLine();

                    Fugui.Layouts.OnDockLayoutReloaded += FuDockingLayoutManager_OnDockLayoutReloaded;
                }

                //3DView settings menus
                float minRightRequiredWidth = rightGroupWidth * scale;
                bool showRightButtons = totalWidth > minRightRequiredWidth;

                Fugui.MoveXUnscaled(layout.GetAvailableWidth());
                float popUpWidth = SETTINGS_POPUP_WIDTH * scale;

                if (showRightButtons)
                {
                    Fugui.MoveXUnscaled(layout.GetAvailableWidth() - rightGroupWidth);
                    Fugui.PushFont(14, FontType.Regular);

                    Fugui.MoveY(-4f);
                    layout.SetNextElementToolTip("Capture mode settings");
                    PopupButton(FlightReLiveIcons.CaptureMode, () => DrawCaptureModeSettings(SETTINGS_POPUP_BUTTON_WIDTH, layout), new Vector2(popUpWidth, 0f));
                    layout.SameLine();

                    Fugui.MoveY(-4f);
                    layout.SetNextElementToolTip("Post-processing settings");
                    PopupButton(FlightReLiveIcons.PostProcess, () => DrawPostProcessingSettings(SETTINGS_POPUP_BUTTON_WIDTH, layout), new Vector2(popUpWidth, 0f));
                    layout.SameLine();

                    Fugui.MoveY(-4f);
                    layout.SetNextElementToolTip("Open path settings");
                    PopupButton(FlightReLiveIcons.Path, () => DrawPathSettings(SETTINGS_POPUP_BUTTON_WIDTH, layout), new Vector2(popUpWidth, 0f));
                    layout.SameLine();

                    Fugui.MoveY(-4f);
                    layout.SetNextElementToolTip("Open scene settings");
                    PopupButton(FlightReLiveIcons.AltitudeRelative, () => DrawSceneSettings(SETTINGS_POPUP_BUTTON_WIDTH, layout), new Vector2(popUpWidth, 0f));
                    //layout.SameLine();

                    //Fugui.MoveY(-4f);
                    //layout.SetNextElementToolTip("Open heightmap display settings");
                    //PopupButton(FlightReLiveIcons.HeightMap, () => DrawHeightSettings(SETTINGS_POPUP_BUTTON_WIDTH, layout), new Vector2(popUpWidth, 0f));

                    Fugui.PopFont();
                }

                ImGui.EndGroup();
                Fugui.PopColor();
            }
            layout.Dispose();

            void PopupButton(string text, Action popupUI, Vector2 popupSize)
            {
                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                if (layout.Button(text, new FuElementSize(SETTINGS_POPUP_BUTTON_WIDTH, unscaledHeight - 6f), new Vector2(6f, 0f) * Fugui.CurrentContext.Scale, new Vector2(0f, 0f), Fugui.Themes.CurrentTheme.ButtonsGradientStrenght, FuButtonStyle.Default, false, 0f))
                {
                    Fugui.OpenPopUp("PopUp" + text, popupUI, () => { });
                }
                Fugui.DrawCarret_Down(ImGui.GetWindowDrawList(), cursorPos + new Vector2((SETTINGS_POPUP_BUTTON_WIDTH * scale) - (size.y / 2f), 0f), (size.y - 4f) / 3f, size.y - 4f, Fugui.Themes.GetColor(FuColors.Text) * 0.8f);

                Vector2 popupPos = new Vector2(ImGui.GetItemRectMax().x - popupSize.x, ImGui.GetItemRectMax().y + (4f * scale));
                Fugui.DrawPopup("PopUp" + text, popupSize, popupPos);
            }
        }

        private void FuDockingLayoutManager_OnDockLayoutReloaded()
        {
            Debug.Log("TEST");
        }

        private void DrawCaptureModeSettings(float popupButtonWidth, FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            Fugui.PushDefaultFont();
            layout.FramedText("Capture Mode");
            layout.Separator();

            CaptureManager.Instance.DrawCaptureModeSettings(layout);

            Fugui.PopFont();
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawPostProcessingSettings(float popupButtonWidth, FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            Fugui.PushDefaultFont();
            layout.FramedText("Post-Processing");
            layout.Separator();

            ApplicationManager.Instance.DrawPostProcessingSettings(layout);

            Fugui.PopFont();
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawPathSettings(float popupButtonWidth, FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            Fugui.PushDefaultFont();
            layout.FramedText("Path Settings");
            layout.Separator();

            PathManager.Instance.DrawPathSettings(layout);

            Fugui.PopFont();
            ImGui.Dummy(Vector2.zero);
        }

        private void DrawSceneSettings(float popupButtonWidth, FuLayout layout)
        {
            ImGui.Dummy(Vector2.zero);
            Fugui.PushDefaultFont();
            layout.FramedText("Scene Settings");
            layout.Separator();

            SceneManager.Instance.DrawSceneSettings(layout);

            Fugui.PopFont();
            ImGui.Dummy(Vector2.zero);
        }

        //private void DrawHeightSettings(float popupButtonWidth, FuLayout layout)
        //{
        //    ImGui.Dummy(Vector2.zero);
        //    Fugui.PushDefaultFont();
        //    layout.FramedText("Heightmap Settings");
        //    layout.Separator();

        //    TerrainManager.Instance.DrawHeightmapSettings(layout);

        //    Fugui.PopFont();
        //    ImGui.Dummy(Vector2.zero);
        //}
        #endregion

        #region CALLBACKS
        public override void OnWindowCreated(FuWindow window)
        {
            EmbeddedCameraManager.Instance.CameraWindow = CameraWindow;
            PathManager.Instance.Camera = CameraWindow;
            window.HeaderHeight = TOP_BAR_HEIGHT;
            window.HeaderUI = DrawCameraWindowSettingBar;
        }
        #endregion
    }
}
