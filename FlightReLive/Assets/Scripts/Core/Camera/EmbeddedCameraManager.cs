using FlightReLive.Core.Loading;
using FlightReLive.Core.Settings;
using Fu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlightReLive.Core.Cameras
{
    public class EmbeddedCameraManager : MonoBehaviour
    {
        #region PLATFORM FACTORS
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const float INPUT_SENSITIVITY_FACTOR = 0.25f;
        private const float ZOOM_PLATFORM_MULTIPLIER = 0.01f;
#else
        private const float INPUT_SENSITIVITY_FACTOR = 0.1f;
        private const float ZOOM_PLATFORM_MULTIPLIER = 1.5f;
#endif
        #endregion

        #region ATTRIBUTES

        [Header("Camera")]
        [SerializeField] private Camera _targetCamera;

        [Header("Camera Settings")]
        [SerializeField] private float _distance = 5f;
        [SerializeField] private Transform _droneAnchorTransform;

        [Header("Y Angle Limits")]
        [SerializeField] private float _minYAngle = 5f;
        [SerializeField] private float _maxYAngle = 85f;

        [Header("Zoom Limits")]
        [SerializeField] private float _minDistance = 1f;
        [SerializeField] private float _maxDistance = 20f;

        private float _initialDistance;
        private float _initialX;
        private float _initialY;
        private float _zoomSensitivity = 10f;
        private float _rotationSensitivity = 3f;
        private float _inertia = 10f;
        private float _targetDistance;
        private float _currentX = 0f;
        private float _currentY = 30f;
        private float _targetX = 0f;
        private float _targetY = 30f;
        private float _velocityX;
        private float _velocityY;
        private float _zoomVelocity;
        #endregion

        #region PROPERTIES

        public FuCameraWindow CameraWindow { internal set; get; }

        public static EmbeddedCameraManager Instance { get; private set; }
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
            _targetDistance = _distance;
            _targetX = _currentX;
            _targetY = _currentY;

            _initialDistance = _distance;
            _initialX = _currentX;
            _initialY = _currentY;
        }

        private void Start()
        {
            SettingsManager.OnCameraRotationSpeedChanged += OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged += OnCameraZoomSpeedChanged;
            SettingsManager.OnCameraInertiaChanged += OnCameraInertiaChanged;

            _zoomSensitivity = SettingsManager.CurrentSettings.CameraZoomSpeed;
            _rotationSensitivity = SettingsManager.CurrentSettings.CameraRotationSpeed;
            _inertia = SettingsManager.CurrentSettings.CameraInertia;
        }

        private void LateUpdate()
        {
            if (_targetCamera == null || CameraWindow == null)
            {
                return;
            }

            if (LoadingManager.Instance.IsLoading)
            {
                LookAtSceneCenter();
                return;
            }

            if (_droneAnchorTransform != null)
            {
                HandleZoom();
                HandleRotationInput();
                UpdateCameraTransform();
            }
        }

        private void OnDestroy()
        {
            SettingsManager.OnCameraRotationSpeedChanged -= OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged -= OnCameraZoomSpeedChanged;
            SettingsManager.OnCameraInertiaChanged -= OnCameraInertiaChanged;
        }

        #endregion

        #region METHODS
        private void HandleZoom()
        {
            if (CameraWindow.IsHoveredContent)
            {
                float scrollValue = Mouse.current?.scroll.y.ReadValue() ?? 0f;

                if (Mathf.Abs(scrollValue) > 0.01f)
                {
                    float zoomDelta = scrollValue * _zoomSensitivity * ZOOM_PLATFORM_MULTIPLIER;
                    _targetDistance = Mathf.Clamp(_targetDistance - zoomDelta, _minDistance, _maxDistance);
                }
            }

            float damping = Mathf.Clamp(_inertia, 0.01f, 30f);
            _distance = Mathf.SmoothDamp(_distance, _targetDistance, ref _zoomVelocity, damping);
        }

        private void HandleRotationInput()
        {
            if (Mouse.current?.rightButton.isPressed == true)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _targetX += delta.x * _rotationSensitivity * INPUT_SENSITIVITY_FACTOR;
                _targetY -= delta.y * _rotationSensitivity * INPUT_SENSITIVITY_FACTOR;
                _targetY = Mathf.Clamp(_targetY, _minYAngle, _maxYAngle);
            }

            float damping = Mathf.Clamp(_inertia, 0.01f, 30f);
            _currentX = Mathf.SmoothDamp(_currentX, _targetX, ref _velocityX, damping);
            _currentY = Mathf.SmoothDamp(_currentY, _targetY, ref _velocityY, damping);
        }

        private void UpdateCameraTransform()
        {
            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 offset = rot * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = _droneAnchorTransform.position + offset;

            _targetCamera.transform.position = desiredPosition;
            _targetCamera.transform.rotation = rot;
        }

        private void LookAtSceneCenter()
        {
            _distance = _initialDistance;
            _targetDistance = _initialDistance;

            _currentX = _initialX;
            _currentY = _initialY;
            _targetX = _initialX;
            _targetY = _initialY;

            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 offset = rot * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = Vector3.zero + offset;

            _targetCamera.transform.position = desiredPosition;
            _targetCamera.transform.rotation = rot;
        }

        #endregion

        #region CALLBACKS

        private void OnCameraZoomSpeedChanged(float zoomSpeed)
        {
            _zoomSensitivity = zoomSpeed;
        }

        private void OnCameraRotationSpeedChanged(float rotationSpeed)
        {
            _rotationSensitivity = rotationSpeed;
        }

        private void OnCameraInertiaChanged(float inertia)
        {
            _inertia = inertia;
        }
        #endregion
    }
}
