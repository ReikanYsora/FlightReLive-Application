using FlightReLive.Core.Settings;
using Fu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlightReLive.Core.Cameras
{
    public class POVCameraManipulator : MonoBehaviour
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
        [SerializeField] private Transform _followPosition;

        private float _zoomSensitivity = 10f;
        private float zoomSpeed = 5f;
        private float sensitivity = 2f;
        private float _rotationSensitivity = 3f;
        private float minFOV = 30f;
        private float maxFOV = 90f;
        private bool isLooking = false;
        private float yaw = 0f;
        private float pitch = 0f;
        private float _targetX = 0f;
        private float _targetY = 30f;
        private float _currentX = 0f;
        private float _currentY = 30f;
        #endregion

        #region PROPERTIES

        public FuCameraWindow CameraWindow { internal set; get; }

        public static POVCameraManipulator Instance { get; private set; }
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
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;

            _zoomSensitivity = SettingsManager.CurrentSettings.CameraZoomSpeed;
            _rotationSensitivity = SettingsManager.CurrentSettings.CameraRotationSpeed;
            SettingsManager.OnCameraRotationSpeedChanged += OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged += OnCameraZoomSpeedChanged;
        }

        private void Update()
        {
            _targetCamera.transform.position = _followPosition.position;

            if (_targetCamera == null || CameraWindow == null)
            {
                return;
            }

            HandleZoom();
            HandleLook();
        }

        private void OnDestroy()
        {
            SettingsManager.OnCameraRotationSpeedChanged -= OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged -= OnCameraZoomSpeedChanged;
        }
        #endregion

        #region METHODS
        private void HandleZoom()
        {
            if (CameraWindow.IsHoveredContent)
            {
                float scroll = CameraWindow.Mouse.Wheel.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _targetCamera.fieldOfView = Mathf.Clamp(_targetCamera.fieldOfView - scroll * zoomSpeed, minFOV, maxFOV);
                }
            }
        }

        private void HandleLook()
        {
            if (!CameraWindow.IsHoveredContent)
            {
                return;
            }

            if (CameraWindow.Mouse.IsPressed(FuMouseButton.Right) == true)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _targetX += delta.x * _rotationSensitivity * INPUT_SENSITIVITY_FACTOR;
                _targetY -= delta.y * _rotationSensitivity * INPUT_SENSITIVITY_FACTOR;
            }

            _targetCamera.transform.rotation = Quaternion.Euler(_targetY, _targetX, 0f);
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
        #endregion
    }
}
