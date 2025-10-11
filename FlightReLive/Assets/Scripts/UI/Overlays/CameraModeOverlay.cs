using FlightReLive.Core.Cameras;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace FlightReLive.Core.UI.Overlays
{
    internal class CameraModeOverlay
    {
        #region CONSTANTS
        private const int CAMERA_MODE_OVERLAY_WIDTH = 180;
        private const int CAMERA_MODE_OVERLAY_HEIGHT = 40;
        private const int CAMERA_MODE_OVERLAY_GRID_PADDING = 10;
        private const float OVERLAY_RADIUS = 8f;
        #endregion

        #region ATTRIBUTES
        private FuOverlay _cameraModeOverlay;
        #endregion

        #region PROPERTIES
        internal bool IsVisible { get; set; }
        #endregion

        #region CONSTRUCTOR
        public CameraModeOverlay()
        {
            IsVisible = false;
        }
        #endregion

        #region UI
        internal void DisplayCameraModeOverlay(FuWindowDefinition windowsDefinition)
        {
            _cameraModeOverlay = new FuOverlay("cameraModeOverlay",
                new Vector2Int(CAMERA_MODE_OVERLAY_WIDTH, CAMERA_MODE_OVERLAY_HEIGHT),
                (overlay, layout) =>
                {
                    DisplayCameraModeOverlayUI();
                },
                FuOverlayFlags.NoClose | FuOverlayFlags.NoEditAnchor | FuOverlayFlags.NoBackground | FuOverlayFlags.NoMove,
                FuOverlayDragPosition.Top);

            _cameraModeOverlay.AnchorWindowDefinition(windowsDefinition, FuOverlayAnchorLocation.TopCenter);
            _cameraModeOverlay.SetMinimumWindowSize(new Vector2Int(CAMERA_MODE_OVERLAY_WIDTH, CAMERA_MODE_OVERLAY_HEIGHT));
        }

        private void DisplayCameraModeOverlayUI()
        {
            if (!IsVisible)
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 avail = ImGui.GetContentRegionAvail();

            //Theme color
            uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));

            //Background rounded rect
            Vector2 globalMin = new Vector2(cursorPos.x, cursorPos.y - (CAMERA_MODE_OVERLAY_GRID_PADDING * 0.5f * scale));
            Vector2 globalMax = new Vector2(
                cursorPos.x + avail.x,
                cursorPos.y + (CAMERA_MODE_OVERLAY_HEIGHT * scale) - (CAMERA_MODE_OVERLAY_GRID_PADDING * scale)
            );

            drawList.AddRectFilled(globalMin, globalMax, bgColor, OVERLAY_RADIUS * scale, ImDrawFlags.RoundCornersAll);

            //Grid
            using (FuGrid uiGrid = new FuGrid("CameraModeGrid", new FuGridDefinition(1, new float[] { 1f }), FuGridFlag.NoAutoLabels, 2 * scale, 2 * scale, CAMERA_MODE_OVERLAY_GRID_PADDING * scale))
            {
                uiGrid.ButtonsGroup<CameraMode>("Camera mode", (index) => ExternalCameraManipulator.Instance.Mode = (CameraMode)index, () => ExternalCameraManipulator.Instance.Mode, 0f, FuButtonsGroupFlags.Default);
            }

            if (Fugui.GetKeyPressed(FuKeysCode.Space))
            {
                ExternalCameraManipulator.Instance.RecenterCamera();
            }
        }
        #endregion
    }
}
