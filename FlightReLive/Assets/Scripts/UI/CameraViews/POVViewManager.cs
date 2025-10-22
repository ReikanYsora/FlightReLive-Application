using FlightReLive.Core.Cameras;
using FlightReLive.Core.TimeBar;
using FlightReLive.Core.UI.Overlays;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace FlightReLive.UI.CameraViews
{
    internal class POVViewManager : FuCameraWindowBehaviour
    {
        #region CONSTANTS
        protected const float HEADER_BAR_HEIGHT = 26f;
        protected const float FOOTER_BAR_HEIGHT = 26f;
        #endregion

        #region ATTRIBUTES
        private CameraSensorOverlay _sensorOverlay;
        private POVCameraZoomOverlay _povCameraZoomOverlay;
        #endregion

        #region PROPERTIES
        public static POVViewManager Instance { get; private set; }
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
        protected void InitializeCameraView()
        {
            POVCameraManipulator.Instance.CameraWindow = CameraWindow;
            POVCameraManipulator.Instance.SensorOverlay = _sensorOverlay;
            POVCameraManipulator.Instance.POVCameraZoomOverlay = _povCameraZoomOverlay;
        }

        public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
            windowDefinition.SetHeaderUI(DrawHeaderBar, HEADER_BAR_HEIGHT);
            windowDefinition.SetUI(OnUI);

            _sensorOverlay = new CameraSensorOverlay(Camera);
            _sensorOverlay.DisplaySensorOverlay(windowDefinition, CameraWindow);
            _povCameraZoomOverlay = new POVCameraZoomOverlay();
            _povCameraZoomOverlay.DisplayPOVCameraZoomOverlay(windowDefinition, CameraWindow);
        }

        public override void OnWindowCreated(FuWindow window)
        {
            InitializeCameraView();
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
                Fugui.PopColor();
            }
            layout.Dispose();
        }
        #endregion
    }
}
