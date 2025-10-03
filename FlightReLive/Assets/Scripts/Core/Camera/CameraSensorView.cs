using FlightReLive.UI;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Cameras
{
    internal class CameraSensorView
    {
        #region CONSTANTS
        private const int SENSOR_OVERLAY_WIDTH = 260;
        private const int SENSOR_OVERLAY_HEIGHT = 40;
        private const int SENSOR_OVERLAY_GRID_PADDING = 10;
        private const float OVERLAY_RADIUS = 8f;
        private const float OVERLAY_PADDING = 6f;
        #endregion

        #region ATTRIBUTES
        /// <summary>
        /// Mapping of enum values to human-readable names for UI display.
        /// </summary>
        internal readonly Dictionary<DroneSensorType, string> DisplayNames =
            new Dictionary<DroneSensorType, string>
            {
                { DroneSensorType.OneOver2_3, "1/2.3 inch" },
                { DroneSensorType.OneOver1_3, "1/1.3 inch" },
                { DroneSensorType.OneInch, "1 inch" },
                { DroneSensorType.FourThirds, "4/3 inch" }
            };

        private FuOverlay _sensorOverlay;
        private DroneSensorType _currentSensor = DroneSensorType.OneOver1_3;
        private Camera _camera;
        #endregion

        #region CONSTRUCTOR
        public CameraSensorView(Camera camera)
        {
            _camera = camera;
        }
        #endregion

        #region METHODS
        internal void DisplaySensorOverlay(FuWindowDefinition windowsDefinition, FuCameraWindow cameraWindow)
        {
            _sensorOverlay = new FuOverlay("sensorOverlay",
                new Vector2Int(SENSOR_OVERLAY_WIDTH, SENSOR_OVERLAY_HEIGHT),
                (overlay, layout) =>
                {
                    DisplaySensorOverlayUI();
                },
                FuOverlayFlags.NoClose | FuOverlayFlags.NoEditAnchor | FuOverlayFlags.NoBackground | FuOverlayFlags.NoMove,
                FuOverlayDragPosition.Top);

            _sensorOverlay.AnchorWindowDefinition(windowsDefinition, FuOverlayAnchorLocation.TopCenter);
            _sensorOverlay.SetMinimumWindowSize(new Vector2Int(SENSOR_OVERLAY_WIDTH, SENSOR_OVERLAY_HEIGHT));
        }

        /// <summary>
        /// Apply physical camera settings based on the selected drone sensor type.
        /// Applies only if change is required (avoids projection matrix flash).
        /// </summary>
        internal void ApplyDroneSensorSettings(DroneSensorType sensorType)
        {
            if (_camera == null)
            {
                Debug.LogError("CameraSensorView.ApplyDroneSensorSettings called with null camera.");
                return;
            }

            if (_currentSensor == sensorType)
            {
                return;
            }

            _camera.usePhysicalProperties = true;

            switch (sensorType)
            {
                case DroneSensorType.OneOver2_3:
                    _camera.sensorSize = new Vector2(6.4f, 4.8f);
                    _camera.focalLength = 3.6f;
                    break;

                case DroneSensorType.OneOver1_3:
                    _camera.sensorSize = new Vector2(9.6f, 7.2f);
                    _camera.focalLength = 4.5f;
                    break;

                case DroneSensorType.OneInch:
                    _camera.sensorSize = new Vector2(13.2f, 8.8f);
                    _camera.focalLength = 8.8f;
                    break;

                case DroneSensorType.FourThirds:
                    _camera.sensorSize = new Vector2(17.3f, 13.0f);
                    _camera.focalLength = 12.0f;
                    break;
            }

            _camera.gateFit = Camera.GateFitMode.Horizontal;
            _camera.aperture = 2.8f;

            _currentSensor = sensorType;
        }
        #endregion

        #region UI
        private void DisplaySensorOverlayUI()
        {
            if (_camera == null)
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector2 avail = ImGui.GetContentRegionAvail();

            //Theme color (identique au TimeBar, sans bordure)
            uint bgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.GetColor(FuColors.FrameBgHovered));

            //Background rounded rect
            Vector2 globalMin = new Vector2(cursorPos.x, cursorPos.y - (SENSOR_OVERLAY_GRID_PADDING / 2f));
            Vector2 globalMax = new Vector2(cursorPos.x + avail.x, cursorPos.y + SENSOR_OVERLAY_HEIGHT - SENSOR_OVERLAY_GRID_PADDING);
            drawList.AddRectFilled(globalMin, globalMax, bgColor, OVERLAY_RADIUS * scale, ImDrawFlags.RoundCornersAll);

            //Grid + Combobox
            using (FuGrid uiGrid = new FuGrid("SensorGrid", new FuGridDefinition(2, new float[] { 0.5f, 0.5f }), FuGridFlag.Default, 2, 2, SENSOR_OVERLAY_GRID_PADDING))
            {
                uiGrid.SetNextElementToolTipWithLabel("Camera sensor type");
                uiGrid.Combobox("Camera sensor size##SensorCombobox", DisplayNames[_currentSensor], () =>
                {
                    foreach (KeyValuePair<DroneSensorType, string> kv in DisplayNames)
                    {
                        bool selected = kv.Key == _currentSensor;
                        if (ImGui.Selectable((selected ? FlightReLiveIcons.Check : " ") + "  " + kv.Value))
                        {
                            ApplyDroneSensorSettings(kv.Key);
                        }
                    }
                });
            }
        }
        #endregion
    }
}
