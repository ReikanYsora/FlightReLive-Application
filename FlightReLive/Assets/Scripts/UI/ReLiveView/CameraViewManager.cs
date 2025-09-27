using FlightReLive.Core.Cameras;
using FlightReLive.Core.Paths;

namespace FlightReLive.UI.ReLiveView
{
    internal class ReLiveViewManager : FlightReLiveCameraView
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
