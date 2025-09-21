using FlightReLive.Core.Cameras;
using Fu;
using Fu.Framework;

namespace FlightReLive.UI.POVView
{
    public class POVViewManager : FuCameraWindowBehaviour
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

        #region CALLBACKS
        public override void OnWindowCreated(FuWindow window)
        {
            POVCameraManipulator.Instance.CameraWindow = CameraWindow;
        }
        #endregion
    }
}
