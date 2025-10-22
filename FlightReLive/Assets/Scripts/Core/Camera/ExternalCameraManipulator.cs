using System;
using FlightReLive.Core.Database;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Paths;
using FlightReLive.Core.POI;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using FlightReLive.Core.UI.Overlays;
using Fu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlightReLive.Core.Cameras
{
    public class ExternalCameraManipulator : MonoBehaviour
    {
        #region CONSTANTS
        private const float PIVOT_LERP_DURATION = 0.5f;
        private const float DOUBLE_CLICK_MAX_DELAY = 0.3f;
        private const float INERTIA_DAMPING = 0.01f;
        private const float RECENTER_PADDING = 1.2f;
        #endregion

        #region ATTRIBUTES

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
        [SerializeField] private LayerMask _collisionMask = ~0;

        private Camera _targetCamera;
        private float _initialDistance;
        private float _initialX;
        private float _initialY;
        private float _zoomSensitivity = 10f;
        private float _rotationSensitivity = 3f;
        private float _targetDistance;
        private float _currentX = 0f;
        private float _currentY = 30f;
        private float _targetX = 0f;
        private float _targetY = 30f;
        private float _velocityX;
        private float _velocityY;
        private float _zoomVelocity;
        private float _panSpeed;
        private Vector3 _freePosition = Vector3.zero;
        private Vector3 _targetFreePosition = Vector3.zero;
        private Vector3 _freeVelocity;
        private Vector3 _pivotLerpStart;
        private Vector3 _pivotLerpTarget;
        private float _pivotLerpProgress = 1f;
        private int _clickCount = 0;
        private float _lastClickTime = 0f;
        private POIEntity _pivotPointPOI;
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
                    RecenterCamera();
                }
            }
        }

        public FuCameraWindow CameraWindow { internal set; get; }

        public static ExternalCameraManipulator Instance { get; private set; }
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
            SettingsManager.OnPanSpeedChanged += OnPanSpeedChanged;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;

            _zoomSensitivity = SettingsManager.CurrentSettings.CameraZoomSpeed;
            _rotationSensitivity = SettingsManager.CurrentSettings.CameraRotationSpeed;
            _targetCamera = CameraManager.Instance.ReLiveCamera;
            _panSpeed = SettingsManager.CurrentSettings.PanSpeed;
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

            HandleZoom();
            HandleRotationInput();

            if (_mode == CameraMode.Free)
            {
                HandlePanInput();
                HandleDoubleClickPivot();
            }

            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 pivot = (_mode == CameraMode.Tracking && _droneAnchorTransform != null) ? _droneAnchorTransform.position : _freePosition;

            float correctedDistance = ClampCameraDistanceToObstacle(pivot, rot, _targetDistance);
            _distance = correctedDistance;

            if (_mode == CameraMode.Tracking)
            {
                if (_pivotPointPOI != null)
                {
                    POIManager.Instance.DeletePOI(_pivotPointPOI);
                }

                UpdateCameraTransformTracking();
            }
            else
            {
                UpdateCameraTransformFree();
            }
        }

        private void OnDestroy()
        {
            SettingsManager.OnCameraRotationSpeedChanged -= OnCameraRotationSpeedChanged;
            SettingsManager.OnCameraZoomSpeedChanged -= OnCameraZoomSpeedChanged;
            SettingsManager.OnPanSpeedChanged -= OnPanSpeedChanged;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
        }
        #endregion

        #region METHODS

        /// <summary>
        /// Recenter the camera (common for Free and Tracking modes).
        /// Faces the longest side of the path bounding box and fits it entirely in view.
        /// </summary>
        public void RecenterCamera()
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

            // Determine dominant axis (X or Z)
            bool isXLonger = bounds.size.x >= bounds.size.z;

            // Set orientation so path goes left→right
            _currentX = _targetX = isXLonger ? 90f : 0f;
            _currentY = _targetY = 45f;

            Vector3 center = bounds.center;
            _freePosition = center;
            _targetFreePosition = center;

            if (_mode == CameraMode.Free)
            {
                // --- Free Mode: show entire path ---
                float fovRad = _targetCamera.fieldOfView * Mathf.Deg2Rad;
                float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
                float requiredDistance = (maxExtent / Mathf.Tan(fovRad * 0.5f)) * 1.2f;
                _distance = _targetDistance = Mathf.Clamp(requiredDistance, _minDistance, _maxDistance);
            }
            else
            {
                // --- Tracking Mode: tighter framing ---
                float closeDistance = Mathf.Lerp(_minDistance, _maxDistance, 0.33f);
                _distance = _targetDistance = closeDistance;

                if (_droneAnchorTransform != null)
                {
                    _droneAnchorTransform.position = bounds.center;
                }
            }
        }


        /// <summary>
        /// Kept for compatibility, now redirects to RecenterCamera().
        /// </summary>
        private void InitializeFreeCamera()
        {
            RecenterCamera();
        }

        private float ClampCameraDistanceToObstacle(Vector3 pivot, Quaternion rotation, float desiredDistance)
        {
            Vector3 direction = rotation * Vector3.back;
            Ray ray = new Ray(pivot, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, desiredDistance, _collisionMask))
            {
                float effectiveMin = GetEffectiveMinDistance();
                float safeDistance = Mathf.Max(hit.distance - 0.5f, effectiveMin);

                float minAltitude = pivot.y + 1f;
                if (hit.point.y < minAltitude)
                {
                    safeDistance = Mathf.Min(safeDistance, desiredDistance);
                }

                return safeDistance;
            }

            return desiredDistance;
        }

        private void HandleZoom()
        {
            if (CameraWindow.IsHoveredContent)
            {
                float scrollValue = CameraWindow.Mouse.Wheel.y;

                if (Mathf.Abs(scrollValue) > 0.01f)
                {
                    float zoomDelta = scrollValue * _zoomSensitivity;
                    float effectiveMin = GetEffectiveMinDistance();
                    _targetDistance = Mathf.Clamp(_targetDistance - zoomDelta, effectiveMin, _maxDistance);
                }
            }

            float desiredDistance = Mathf.SmoothDamp(_distance, _targetDistance, ref _zoomVelocity, INERTIA_DAMPING);

            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 pivot = (_mode == CameraMode.Tracking && _droneAnchorTransform != null) ? _droneAnchorTransform.position : _freePosition;
            _distance = ClampCameraDistanceToObstacle(pivot, rot, desiredDistance);
        }

        private void HandleDoubleClickPivot()
        {
            if (_mode != CameraMode.Free || !CameraWindow.IsHoveredContent)
            {
                return;
            }

            if (CameraWindow.Mouse.IsClicked(FuMouseButton.Left))
            {
                float currentTime = Time.time;

                if (currentTime - _lastClickTime > DOUBLE_CLICK_MAX_DELAY)
                {
                    _clickCount = 0;
                }

                _clickCount++;
                _lastClickTime = currentTime;

                if (_clickCount == 2)
                {
                    _clickCount = 0;

                    Ray ray = CameraWindow.GetCameraRay();

                    if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
                    {
                        int hitLayerMask = 1 << hit.collider.gameObject.layer;
                        if ((_collisionMask.value & hitLayerMask) == 0)
                        {
                            return;
                        }

                        Vector3 newPivot = hit.point;

                        _pivotLerpStart = _freePosition;
                        _pivotLerpTarget = newPivot;
                        _pivotLerpProgress = 0f;

                        if (_pivotPointPOI != null)
                        {
                            POIManager.Instance.DeletePOI(_pivotPointPOI);
                        }

                        if (LoadingManager.Instance.CurrentFlightData != null)
                        {
                            SerializedGPSCoordinate data = LoadingManager.Instance.CurrentFlightData.ConvertWorldToGPSPosition(newPivot);
                            _pivotPointPOI = POIManager.Instance.AddPOI($"Custom point", newPivot, Color.blueViolet, 50f);
                        }
                    }
                }
            }
        }

        private void HandleRotationInput()
        {
            if (CameraWindow.Mouse.IsPressed(FuMouseButton.Right))
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                float proposedX = _targetX + delta.x * _rotationSensitivity;
                float proposedY = Mathf.Clamp(_targetY - delta.y * _rotationSensitivity, _minYAngle, _maxYAngle);

                Quaternion proposedRot = Quaternion.Euler(proposedY, proposedX, 0);
                Vector3 pivot = (_mode == CameraMode.Tracking) ? _droneAnchorTransform.position : _freePosition;
                Vector3 proposedPosition = pivot + proposedRot * new Vector3(0, 0, -_distance);

                Vector3 direction = (proposedPosition - pivot).normalized;
                Ray forwardRay = new Ray(pivot, direction);
                if (Physics.Raycast(forwardRay, out RaycastHit hit, _distance, _collisionMask))
                {
                    float safeHeight = hit.point.y + 5f;
                    proposedPosition.y = Mathf.Max(proposedPosition.y, safeHeight);
                }

                _targetX = proposedX;
                _targetY = proposedY;

                if (_mode == CameraMode.Free)
                {
                    _targetFreePosition = pivot;
                }
            }

            _currentX = Mathf.SmoothDamp(_currentX, _targetX, ref _velocityX, INERTIA_DAMPING);
            _currentY = Mathf.SmoothDamp(_currentY, _targetY, ref _velocityY, INERTIA_DAMPING);
        }

        private void HandlePanInput()
        {
            if (CameraWindow.Mouse.IsPressed(FuMouseButton.Center))
            {
                Vector2 delta = Mouse.current.delta.ReadValue();

                Vector3 right = _targetCamera.transform.right;
                Vector3 up = _targetCamera.transform.up;

                Vector3 panDelta = (-right * delta.x + -up * delta.y) * _panSpeed;
                Vector3 proposedTarget = _targetFreePosition + panDelta;

                Ray downRay = new Ray(proposedTarget + Vector3.up * 100f, Vector3.down);
                if (Physics.Raycast(downRay, out RaycastHit hit, 200f, _collisionMask))
                {
                    float minAltitude = hit.point.y + 5f;
                    proposedTarget.y = Mathf.Max(proposedTarget.y, minAltitude);
                }

                _targetFreePosition = proposedTarget;
            }

            _freePosition = Vector3.SmoothDamp(_freePosition, _targetFreePosition, ref _freeVelocity, INERTIA_DAMPING);
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
            if (_pivotLerpProgress < 1f)
            {
                _pivotLerpProgress += Time.deltaTime / PIVOT_LERP_DURATION;
                _pivotLerpProgress = Mathf.Clamp01(_pivotLerpProgress);
                float easedProgress = Mathf.SmoothStep(0f, 1f, _pivotLerpProgress);
                _freePosition = Vector3.Lerp(_pivotLerpStart, _pivotLerpTarget, easedProgress);
                _targetFreePosition = _freePosition;
            }

            Quaternion rot = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 offset = rot * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = _freePosition + offset;

            if (ProceduralTerrainManager.Instance != null)
            {
                Bounds terrainBounds = ProceduralTerrainManager.Instance.TerrainBounds;
                float margin = 10f;
                float maxAltitudeExtra = 500f;
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, terrainBounds.min.x + margin, terrainBounds.max.x - margin);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, terrainBounds.min.y + 5f, terrainBounds.max.y + maxAltitudeExtra);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, terrainBounds.min.z + margin, terrainBounds.max.z - margin);
            }

            _targetCamera.transform.position = desiredPosition;
            _targetCamera.transform.rotation = rot;
        }

        private float GetEffectiveMinDistance()
        {
            if (_mode == CameraMode.Free)
            {
                float visualDistance = Vector3.Distance(_targetCamera.transform.position, _freePosition);
                return Mathf.Min(_minDistance, visualDistance * 0.5f);
            }

            return _minDistance;
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
            _zoomSensitivity = SettingsManager.CurrentSettings.CameraZoomSpeed;
        }

        private void OnCameraRotationSpeedChanged(float rotationSpeed)
        {
            _rotationSensitivity = SettingsManager.CurrentSettings.CameraRotationSpeed;
        }

        private void OnPanSpeedChanged(float obj)
        {
            _panSpeed = SettingsManager.CurrentSettings.PanSpeed;
        }

        private void OnFlightEndLoading(SerializedFlightData flight)
        {
            RecenterCamera();
        }
        #endregion
    }
}
