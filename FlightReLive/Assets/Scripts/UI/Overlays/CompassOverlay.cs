using FlightReLive.Core.Compass;
using Fu;
using ImGuiNET;
using System;
using UnityEngine;

namespace FlightReLive.Core.UI.Overlays
{
    internal class CompassOverlay
    {
        #region CONSTANTS
        private const int COMPASS_OVERLAY_WIDTH = 128;
        private const int COMPASS_OVERLAY_HEIGHT = 128;
        private const int COMPASS_OVERLAY_RIGHT_PADDING = 30;
        #endregion

        #region ATTRIBUTES
        private FuOverlay _compassOverlay;
        private Camera _syncCamera;
        #endregion

        #region CONSTRUCTOR
        public CompassOverlay()
        {

        }
        #endregion

        #region UI
        internal void DisplayCompassOverlay(FuWindowDefinition windowsDefinition, Camera camera)
        {
            _compassOverlay = new FuOverlay("compassOverlay",
                new Vector2Int(COMPASS_OVERLAY_WIDTH + COMPASS_OVERLAY_RIGHT_PADDING, COMPASS_OVERLAY_HEIGHT),
                (overlay, layout) =>
                {
                    DisplayCompassOverlayUI();
                },
                FuOverlayFlags.NoClose | FuOverlayFlags.NoEditAnchor | FuOverlayFlags.NoBackground | FuOverlayFlags.NoMove,
                FuOverlayDragPosition.Top);

            _compassOverlay.AnchorWindowDefinition(windowsDefinition, FuOverlayAnchorLocation.TopRight);
            _compassOverlay.SetMinimumWindowSize(new Vector2Int(COMPASS_OVERLAY_WIDTH, COMPASS_OVERLAY_HEIGHT));
            _syncCamera = camera;
        }

        private void DisplayCompassOverlayUI()
        {
            if (CompassManager.Instance == null || CompassManager.Instance.CompassTexture == null || _syncCamera == null)
            {
                return;
            }

            CompassManager.Instance.TargetCamera = _syncCamera;

            float scale = Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 globalMin = new Vector2(cursorPos.x, cursorPos.y);
            Vector2 globalMax = new Vector2(cursorPos.x + (COMPASS_OVERLAY_WIDTH * scale), cursorPos.y + (COMPASS_OVERLAY_HEIGHT * scale));
            IntPtr textureId = Fugui.CurrentContext.TextureManager.GetTextureId(CompassManager.Instance.CompassTexture);
            drawList.AddImage(textureId, globalMin, globalMax, new Vector2(0, 0), new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(Vector4.one));
        }

        #endregion
    }
}
