using FlightReLive.Core.Loading;
using UnityEngine;

namespace FlightReLive.Core.Compass
{
    public class CompassManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Light _compassLight;
        [SerializeField] private Camera _compassCamera;
        [SerializeField] private Transform _compass;
        [SerializeField] private Transform _rotatingPart;
        [SerializeField] private float _cameraDistance = 5f;
        [SerializeField, Range(0f, 90f)] private float _pitchAngle = 45f;
        private RenderTexture _compassRenderTexture;
        #endregion

        #region PROPERTIES
        internal static CompassManager Instance { get; private set; }

        internal Camera TargetCamera { get;  set; }

        internal RenderTexture CompassTexture { get; private set; }
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

            SetupCompass();
            SetupCompassCamera();
            SetupCompassLight();
        }

        private void Start()
        {
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFlightUnloaded;
        }

        private void LateUpdate()
        {
            UpdateCompass();
        }

        private void OnDestroy()
        {
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded -= OnFlightUnloaded;
        }
        #endregion

        #region METHODS
        private void SetupCompass()
        {
            if (_compass == null)
            {
                return;
            }

            _compass.gameObject.SetActive(false);
        }

        private void SetupCompassCamera()
        {
            if (_compassCamera == null)
            {
                return;
            }

            _compassRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            _compassRenderTexture.name = "CompassRenderTexture";
            _compassRenderTexture.Create();
            _compassCamera.targetTexture = _compassRenderTexture;
            _compassCamera.clearFlags = CameraClearFlags.Depth;
            _compassCamera.backgroundColor = Color.clear;
            CompassTexture = _compassRenderTexture;
        }
        private void SetupCompassLight()
        {
            if (_compassLight == null)
            {
                return;
            }

            _compassLight.enabled = false;
        }

        private void UpdateCompass()
        {
            if (_compassCamera == null || _compass == null || _rotatingPart == null || TargetCamera == null)
            {
                return;
            }

            float cameraYaw = TargetCamera.transform.eulerAngles.y;
            _rotatingPart.localRotation = Quaternion.Euler(0f, -cameraYaw, 0f);
            float pitchRad = _pitchAngle * Mathf.Deg2Rad;
            Vector3 cameraOffset = new Vector3(0f, Mathf.Sin(pitchRad), -Mathf.Cos(pitchRad)) * _cameraDistance;
            _compassCamera.transform.position = _compass.position + cameraOffset;
            _compassCamera.transform.LookAt(_compass.position, Vector3.up);
        }
        #endregion

        #region CALLBACKS
        private void OnFlightUnloaded()
        {
            if (_compassLight == null)
            {
                return;
            }

            _compassLight.enabled = false;
            _compass.gameObject.SetActive(false);
        }

        private void OnFlightEndLoading()
        {
            if (_compassLight == null || _compass == null)
            {
                return;
            }

            _compassLight.enabled = true;
            _compass.gameObject.SetActive(true);
        }
        #endregion
    }
}
