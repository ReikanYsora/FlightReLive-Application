using FlightReLive.Core.Cameras;
using FlightReLive.Core.Paths;
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
        #endregion

        #region METHODS
        protected override void InitializeCameraView()
        {
            POVCameraManipulator.Instance.CameraWindow = CameraWindow;
        }
        #endregion
    }
}
