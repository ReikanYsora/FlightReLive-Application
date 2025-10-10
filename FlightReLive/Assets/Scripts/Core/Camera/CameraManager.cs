using UnityEngine;

namespace FlightReLive.Core.Cameras
{
    public class CameraManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] internal Camera ReLiveCamera;
        [SerializeField] internal Camera POVCamera;
        #endregion

        #region PROPERTIES
        public static CameraManager Instance { get; private set; }
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
    }
}
