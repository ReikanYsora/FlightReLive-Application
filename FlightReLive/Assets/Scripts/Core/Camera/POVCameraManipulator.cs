using FlightReLive.Core.Loading;
using FlightReLive.Core.Settings;
using FlightReLive.Core.UI.Overlays;
using Fu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlightReLive.Core.Cameras
{
    /// <summary>
    /// POV camera manipulator with zoom (FOV) and rotation control.
    /// Matches the same speed/inertia logic as ExternalCameraManipulator.
    /// </summary>
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

        [Header("Zoom Limits (FOV)")]
        [SerializeField] private float _minFOVSerialized = 30f;
        [SerializeField] private float _maxFOVSerialized = 90f;

        private float _zoomSensitivity = 10f;
        private float _rotationSensitivity = 3f;
        private float _inertia = 10f;
        private float _targetX = 0f;
        private float _targetY = 30f;
        private float _currentX = 0f;
        private float _currentY = 30f;
        private float _targetFOV;
        private float _velocityX;
        private float _velocityY;
        private float _zoomVelocity;
        #endregion

        #region PROPERTIES
        public FuCameraWindow CameraWindow { internal set; get; }

        public static POVCameraManipulator Instance { get; private set; }

        internal CameraSensorOverlay SensorOverlay { get; set; }

        internal POVCameraZoomOverlay POVCameraZoomOverlay { get; set; }

        internal float CurrentFOV { get; private set; }

        internal float MinFOV { get; private set; }

        internal float MaxFOV { get; private set; }
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

            MinFOV = _minFOVSerialized;
            MaxFOV = _maxFOVSerialized;
        }

        private void Start()
        {
            _zoomSensitivity = SettingsManager.CurrentSettings.CameraZoomSpeed;
            _rotationSensitivity = SettingsManager.CurrentSettings.CameraRotationSpeed;
            _inertia = SettingsManager.CurrentSettings.CameraInertia;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFlightUnloaded;

            _targetFOV = CurrentFOV = _targetCamera.fieldOfView;
        }

        private void Update()
        {
            if (_targetCamera == null || CameraWindow == null || _followPosition == null)
            {
                return;
            }

            _targetCamera.transform.position = _followPosition.position;

            HandleZoom();
            HandleLook();

            UpdateCameraTransform();
        }

        private void OnDestroy()
        {
            SettingsManager.OnCameraRotationSpeedChanged -= OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged -= OnCameraZoomSpeedChanged;
            SettingsManager.OnCameraInertiaChanged -= OnCameraInertiaChanged;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded -= OnFlightUnloaded;
        }
        #endregion

        #region METHODS
        private void HandleZoom()
        {
            if (CameraWindow.IsHoveredContent)
            {
                float scrollValue = CameraWindow.Mouse.Wheel.y;

                if (Mathf.Abs(scrollValue) > 0.01f)
                {
                    float zoomDelta = scrollValue * _zoomSensitivity * ZOOM_PLATFORM_MULTIPLIER;
                    _targetFOV = Mathf.Clamp(_targetFOV - zoomDelta, MinFOV, MaxFOV);
                }
            }

            float damping = Mathf.Clamp(_inertia, 0.01f, 30f);
            CurrentFOV = Mathf.SmoothDamp(CurrentFOV, _targetFOV, ref _zoomVelocity, damping);
            _targetCamera.fieldOfView = CurrentFOV;
        }

        private void HandleLook()
        {
            if (CameraWindow.Mouse.IsPressed(FuMouseButton.Right))
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _targetX += delta.x * _rotationSensitivity * INPUT_SENSITIVITY_FACTOR;
                _targetY -= delta.y * _rotationSensitivity * INPUT_SENSITIVITY_FACTOR;
            }

            float damping = Mathf.Clamp(_inertia, 0.01f, 30f);
            _currentX = Mathf.SmoothDamp(_currentX, _targetX, ref _velocityX, damping);
            _currentY = Mathf.SmoothDamp(_currentY, _targetY, ref _velocityY, damping);
        }

        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0f);
            _targetCamera.transform.rotation = rotation;
        }
        #endregion

        #region CALLBACKS
        private void OnEnable()
        {
            SettingsManager.OnCameraRotationSpeedChanged += OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged += OnCameraZoomSpeedChanged;
            SettingsManager.OnCameraInertiaChanged += OnCameraInertiaChanged;
        }

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

        private void OnFlightEndLoading()
        {
            if (SensorOverlay != null)
            {
                SensorOverlay.IsVisible = true;
            }

            if (POVCameraZoomOverlay != null)
            {
                POVCameraZoomOverlay.IsVisible = true;
            }
        }

        private void OnFlightUnloaded()
        {
            if (SensorOverlay != null)
            {
                SensorOverlay.IsVisible = false;
            }

            if (POVCameraZoomOverlay != null)
            {
                POVCameraZoomOverlay.IsVisible = false;
            }
        }

        internal void SetTargetFOV(float newFOV)
        {
            _targetFOV = Mathf.Clamp(newFOV, MinFOV, MaxFOV);
        }
        #endregion
    }
}
