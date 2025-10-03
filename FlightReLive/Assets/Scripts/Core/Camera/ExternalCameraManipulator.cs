using FlightReLive.Core.Loading;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Settings;
using FlightReLive.Core.UI.Overlays;
using FlightReLive.UI;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace FlightReLive.Core.Cameras
{
    public class ExternalCameraManipulator : MonoBehaviour
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
        [SerializeField] private CameraMode _mode = CameraMode.Tracking;

        [Header("Y Angle Limits")]
        [SerializeField] private float _minYAngle = 5f;
        [SerializeField] private float _maxYAngle = 85f;

        [Header("Zoom Limits")]
        [SerializeField] private float _minDistance = 1f;
        [SerializeField] private float _maxDistance = 200f;

        [Header("Free Camera Settings")]
        [SerializeField] private float _panSensitivity = 1f;

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

        // Free mode position
        private Vector3 _freePosition = Vector3.zero;
        private Vector3 _targetFreePosition = Vector3.zero;
        private Vector3 _freeVelocity;
        #endregion

        #region PROPERTIES
        public CameraMode Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                if (_mode != value)
                {
                    _mode = value;

                    if (_mode == CameraMode.Free)
                    {
                        InitializeFreeCamera();
                    }
                }
            }
        }

        public FuCameraWindow CameraWindow { internal set; get; }

        public static ExternalCameraManipulator Instance { get; private set; }

        internal CameraModeOverlay CameraModeOverlay { get; set; }
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

            _freePosition = Vector3.zero;
            _targetFreePosition = Vector3.zero;
        }

        private void Start()
        {
            SettingsManager.OnCameraRotationSpeedChanged += OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged += OnCameraZoomSpeedChanged;
            SettingsManager.OnCameraInertiaChanged += OnCameraInertiaChanged;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFlightUnloaded;

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

            if (_mode == CameraMode.Tracking && _droneAnchorTransform != null)
            {
                HandleZoom();
                HandleRotationInput();
                UpdateCameraTransformTracking();
            }
            else if (_mode == CameraMode.Free)
            {
                HandleZoom();
                HandleRotationInput();
                HandlePanInput();
                UpdateCameraTransformFree();
            }
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

        /// <summary>
        /// Called when switching to Free mode: centers the camera on the Path3D bounding box.
        /// </summary>
        private void InitializeFreeCamera()
        {
            if (_targetCamera == null || PathManager.Instance == null || !PathManager.Instance.IsPathVisible)
            {
                return;
            }

            Bounds bounds = PathManager.Instance.GetPathBoundingBox();
            if (bounds.size == Vector3.zero)
            {
                return;
            }

            //Center horizontally, but use max Y for vertical pivot
            Vector3 center = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            _freePosition = center;
            _targetFreePosition = center;

            //Compute distance to fit bounds
            float fovRad = _targetCamera.fieldOfView * Mathf.Deg2Rad;

            float halfHeight = bounds.extents.y;
            float halfWidth = bounds.extents.magnitude;

            float requiredDistanceByHeight = halfHeight / Mathf.Tan(fovRad * 0.5f);
            float requiredDistanceByWidth = halfWidth / Mathf.Tan(fovRad * 0.5f * _targetCamera.aspect);

            float requiredDistance = Mathf.Max(requiredDistanceByHeight, requiredDistanceByWidth);

            _distance = requiredDistance * 1.2f;
            _targetDistance = _distance;

            //Initial point of view
            _currentX = _targetX = 0f;
            _currentY = _targetY = 45f;
        }


        private void HandleZoom()
        {
            if (CameraWindow.IsHoveredContent)
            {
                float scrollValue = CameraWindow.Mouse.Wheel.y;

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
            if (CameraWindow.Mouse.IsPressed(FuMouseButton.Right))
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

        private void HandlePanInput()
        {
            if (CameraWindow.Mouse.IsPressed(FuMouseButton.Center))
            {
                Vector2 delta = Mouse.current.delta.ReadValue();

                Vector3 right = _targetCamera.transform.right;
                Vector3 up = _targetCamera.transform.up;

                Vector3 panDelta = (-right * delta.x + -up * delta.y) * _panSensitivity * INPUT_SENSITIVITY_FACTOR;
                _targetFreePosition += panDelta;
            }

            float damping = Mathf.Clamp(_inertia, 0.01f, 30f);
            _freePosition = Vector3.SmoothDamp(_freePosition, _targetFreePosition, ref _freeVelocity, damping);
        }

        private void UpdateCameraTransformTracking()
        {
            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 offset = rot * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = _droneAnchorTransform.position + offset;

            _targetCamera.transform.position = desiredPosition;
            _targetCamera.transform.rotation = rot;
        }

        private void UpdateCameraTransformFree()
        {
            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 offset = rot * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = _freePosition + offset;

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
            => _zoomSensitivity = zoomSpeed;

        private void OnCameraRotationSpeedChanged(float rotationSpeed)
            => _rotationSensitivity = rotationSpeed;

        private void OnCameraInertiaChanged(float inertia)
            => _inertia = inertia;

        private void OnFlightEndLoading()
        {
            InitializeFreeCamera();

            if (CameraModeOverlay != null)
            {
                CameraModeOverlay.IsVisible = true;
            }
        }

        private void OnFlightUnloaded()
        {
            if (CameraModeOverlay != null)
            {
                CameraModeOverlay.IsVisible = false;
            }
        }
        #endregion
    }
}
