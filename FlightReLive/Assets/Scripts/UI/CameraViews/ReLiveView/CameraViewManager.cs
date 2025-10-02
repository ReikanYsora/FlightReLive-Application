using FlightReLive.Core.Cameras;
using FlightReLive.Core.Paths;
using FlightReLive.Core.TimeBar;

namespace FlightReLive.UI.CameraViews
{
    internal class ReLiveViewManager : CameraViewManager
    {
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
            PathManager.Instance.Camera = CameraWindow;
        }
        #endregion
    }
}
