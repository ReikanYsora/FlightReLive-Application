using FlightReLive.Core.Cameras;
using FlightReLive.Core.Paths;
using FlightReLive.Core.TimeBar;
using FlightReLive.Core.UI.Overlays;
using FlightReLive.UI.Overlays;
using Fu;

namespace FlightReLive.UI.CameraViews
{
    internal class ReLiveViewManager : CameraViewManager
    {
        #region ATTRIBUTES
        private TimeBarOverlay _timeBarOverlay;
        private CameraModeOverlay _cameraModeOverlay;
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
        protected override void InitializeCameraView()
        {
            ExternalCameraManipulator.Instance.CameraWindow = CameraWindow;
            ExternalCameraManipulator.Instance.CameraModeOverlay = _cameraModeOverlay;
            PathManager.Instance.Camera = CameraWindow;
        }

        public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
            _timeBarOverlay = new TimeBarOverlay();
            _timeBarOverlay.DisplayTimeBarOverlay(windowDefinition, CameraWindow);

            _cameraModeOverlay = new CameraModeOverlay();
            _cameraModeOverlay.DisplayCameraModeOverlay(windowDefinition);

            _compassOverlay = new CompassOverlay();
            _compassOverlay.DisplayCompassOverlay(windowDefinition, Camera);
        }
        #endregion
    }
}
