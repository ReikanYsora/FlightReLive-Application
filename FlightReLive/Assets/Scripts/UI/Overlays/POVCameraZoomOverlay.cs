using FlightReLive.Core.Cameras;
using Fu;
using ImGuiNET;
using UnityEngine;

public class POVCameraZoomOverlay
{
    #region CONSTANTS
    private const int POV_CAMERA_ZOOM_OVERLAY_WIDTH = 40;
    private const int POV_CAMERA_ZOOM_OVERLAY_HEIGHT = 250;
    private const int POV_CAMERA_ZOOM_GRID_PADDING = 5;
    private const float OVERLAY_RADIUS = 8f;
    #endregion

    #region ATTRIBUTES
    private FuOverlay _cameraModeOverlay;
    #endregion

    #region PROPERTIES
    internal bool IsVisible { get; set; }
    #endregion

    #region CONSTRUCTOR
    public POVCameraZoomOverlay()
    {
        IsVisible = false;
    }
    #endregion

    #region UI
    internal void DisplayPOVCameraZoomOverlay(FuWindowDefinition windowsDefinition, FuCameraWindow cameraWindow)
    {
        _cameraModeOverlay = new FuOverlay("povCameraZoomOverlay",
            new Vector2Int(POV_CAMERA_ZOOM_OVERLAY_WIDTH, POV_CAMERA_ZOOM_OVERLAY_HEIGHT),
            (overlay, layout) =>
            {
                DisplayPOVCameraZoomOverlayUI();
            },
            FuOverlayFlags.NoClose | FuOverlayFlags.NoEditAnchor | FuOverlayFlags.NoBackground | FuOverlayFlags.NoMove,
            FuOverlayDragPosition.Right);

        _cameraModeOverlay.AnchorWindowDefinition(windowsDefinition, FuOverlayAnchorLocation.MiddleRight);
        _cameraModeOverlay.SetMinimumWindowSize(new Vector2Int(POV_CAMERA_ZOOM_OVERLAY_WIDTH, POV_CAMERA_ZOOM_OVERLAY_HEIGHT));
    }

    private void DisplayPOVCameraZoomOverlayUI()
    {
        if (!IsVisible || POVCameraManipulator.Instance == null)
        {
            return;
        }

        float scale = Fugui.CurrentContext.Scale;
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector2 cursorPos = ImGui.GetCursorScreenPos();

        // Dimensions fixes (overlay complet)
        float padding = POV_CAMERA_ZOOM_GRID_PADDING * scale;
        uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));

        Vector2 availBefore = ImGui.GetContentRegionAvail();

        //Background rounded rect
        Vector2 globalMin = new Vector2(cursorPos.x, cursorPos.y - (POV_CAMERA_ZOOM_GRID_PADDING * 0.5f * scale));
        Vector2 globalMax = new Vector2(cursorPos.x + availBefore.x, cursorPos.y + (POV_CAMERA_ZOOM_OVERLAY_HEIGHT * scale) - (POV_CAMERA_ZOOM_GRID_PADDING * 2 * scale));
        drawList.AddRectFilled(globalMin, globalMax, bgColor, OVERLAY_RADIUS * scale, ImDrawFlags.RoundCornersAll);

        // Dimensions de la barre en tenant compte des paddings
        float barWidth = globalMax.x - globalMin.x - (padding * 2f);
        float barHeight = globalMax.y - globalMin.y - (padding * 2f);

        Vector2 barPos = new Vector2(globalMin.x + padding, globalMin.y + padding);
        Vector2 barEnd = new Vector2(barPos.x + barWidth, barPos.y + barHeight);

        uint barBgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBg));
        uint zoomValueColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Selected));
        uint cursorColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.Text));
        uint hoverColor = ImGui.ColorConvertFloat4ToU32(Color.white);

        // Fond barre
        drawList.AddRectFilled(barPos, barEnd, barBgColor, 4f * scale);

        // Ratio FOV (0 = bas, 1 = haut)
        float fov = POVCameraManipulator.Instance.CurrentFOV;
        float minFOV = POVCameraManipulator.Instance.MinFOV;
        float maxFOV = POVCameraManipulator.Instance.MaxFOV;
        float ratio = Mathf.InverseLerp(maxFOV, minFOV, fov);
        ratio = Mathf.Clamp01(ratio);

        float progressY = Mathf.Lerp(barEnd.y, barPos.y, ratio);

        // Remplissage zoom
        drawList.AddRectFilled(new Vector2(barPos.x, progressY), barEnd, zoomValueColor, 4f * scale);

        // Curseur principal
        drawList.AddLine(new Vector2(barPos.x, progressY), new Vector2(barEnd.x, progressY), cursorColor, 2f * scale);

        // Hover interaction
        Vector2 mousePos = ImGui.GetMousePos();
        bool isHovering = ImGui.IsMouseHoveringRect(barPos, barEnd);
        if (isHovering)
        {
            float hoverRatio = Mathf.InverseLerp(barEnd.y, barPos.y, mousePos.y);
            hoverRatio = Mathf.Clamp01(hoverRatio);
            float hoverY = Mathf.Lerp(barEnd.y, barPos.y, hoverRatio);

            // Curseur hover blanc
            drawList.AddLine(new Vector2(barPos.x, hoverY), new Vector2(barEnd.x, hoverY), hoverColor, 2f * scale);

            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                float newFOV = Mathf.Lerp(maxFOV, minFOV, hoverRatio);
                POVCameraManipulator.Instance.SetTargetFOV(newFOV);
            }
        }
    }
    #endregion
}
