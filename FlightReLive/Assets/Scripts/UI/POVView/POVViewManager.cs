using FlightReLive.Core.Cameras;
using FlightReLive.Core.TimeBar;
using FlightReLive.UI.ReLiveView;

namespace FlightReLive.UI.POVView
{
    internal class POVViewManager : FlightReLiveCameraView
    {
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
        }
        #endregion
    }
}
