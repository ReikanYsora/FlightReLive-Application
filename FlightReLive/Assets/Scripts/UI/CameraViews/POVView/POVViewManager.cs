using FlightReLive.Core.Cameras;
using FlightReLive.Core.TimeBar;
using FlightReLive.Core.UI.Overlays;
using FlightReLive.UI.Overlays;
using Fu;

namespace FlightReLive.UI.CameraViews
{
    internal class POVViewManager : CameraViewManager
    {
        #region ATTRIBUTES
        private TimeBarOverlay _timeBarViewOverlay;
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
        protected override void InitializeCameraView()
        {
            POVCameraManipulator.Instance.CameraWindow = CameraWindow;
            POVCameraManipulator.Instance.SensorOverlay = _sensorOverlay;
            POVCameraManipulator.Instance.POVCameraZoomOverlay = _povCameraZoomOverlay;
        }

        public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
            _timeBarViewOverlay = new TimeBarOverlay();
            _timeBarViewOverlay.DisplayTimeBarOverlay(windowDefinition, CameraWindow);

            _sensorOverlay = new CameraSensorOverlay(Camera);
            _sensorOverlay.DisplaySensorOverlay(windowDefinition, CameraWindow);

            _povCameraZoomOverlay = new POVCameraZoomOverlay();
            _povCameraZoomOverlay.DisplayPOVCameraZoomOverlay(windowDefinition, CameraWindow);
        }
        #endregion
    }
}
