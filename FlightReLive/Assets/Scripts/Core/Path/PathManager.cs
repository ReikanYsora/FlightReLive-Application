using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using FlightReLive.Core.WorldUI;
using FlightReLive.UI.VideoPlayer;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


namespace FlightReLive.Core.Paths
{
    public class PathManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float MESH_PATH_RADIUS = 0.05f;
        private const int MESH_PATH_RADIAL_SEGMENT = 8;
        #endregion

        #region ATTRIBUTES
        [Header("3D Path settings")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Material _progressionPathMaterial;
        [SerializeField] private LayerMask _raycastMask;
        [SerializeField] private float _glowDuration = 1f;
        [SerializeField] private Transform _droneAnchorTransform;
        
        private List<PathPoint> _fullPath;
        private List<FlightDataPoint> _interpolatedToFlightPoint;

        //3D Path meshes
        private GameObject _progressionPath;
        private MeshFilter _progressionPathFilter;
        private MeshCollider _progressionPathCollider;
        private MeshRenderer _progressionPathRenderer;
        private Material _progressionPathMaterialInstance;
        private PathColliderUpdater _progressionPathColliderUpdater;
        private float _pathBaseThickness;
        private float _glowTimer = 0f;
        private float _currentProgress;
        #endregion

        #region PROPERTIES
        public FuCameraWindow Camera { get; internal set; }

        public static PathManager Instance { get; private set; }

        public float BaseThickness
        {
            get
            {
                return _pathBaseThickness;
            }
            set
            {
                _pathBaseThickness = value;
                _progressionPathMaterialInstance.SetFloat("_BaseThickness", _pathBaseThickness);
                UpdatePathColliderMesh();
                SettingsManager.SavePathWidth(BaseThickness);
            }
        }

        public bool IsPathVisible
        {
            get
            {
                return _progressionPath != null && _progressionPath.GetComponent<MeshFilter>() != null && _progressionPath.GetComponent<MeshFilter>().sharedMesh != null;
            }
        }
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

            _interpolatedToFlightPoint = new List<FlightDataPoint>();

            //Precreate path meshes
            _progressionPath = new GameObject("Progression path");
            _progressionPath.layer = LayerMask.NameToLayer("3DPath");
            _progressionPath.transform.parent = transform;
            _progressionPathFilter = _progressionPath.AddComponent<MeshFilter>();
            _progressionPathCollider = _progressionPath.AddComponent<MeshCollider>();
            _progressionPathRenderer = _progressionPath.AddComponent<MeshRenderer>();
            _progressionPathMaterialInstance = new Material(_progressionPathMaterial);
            _progressionPathColliderUpdater = _progressionPath.AddComponent<PathColliderUpdater>();
            _progressionPathRenderer.material = _progressionPathMaterialInstance;

        }
        private void Start()
        {
            VideoPlayerManager.Instance.OnProgressChanged += OnProgressChanged;
            TerrainManager.Instance.OnTerrainLoaded += OnTerrainLoaded;
            SettingsManager.OnPathRemainingColor1Changed += OnPathRemainingColor1Changed;
            SettingsManager.OnPathRemainingColor2Changed += OnPathRemainingColor2Changed;
            _progressionPathMaterialInstance.SetColor("_ColorC", SettingsManager.CurrentSettings.PathRemainingColor1);
            _progressionPathMaterialInstance.SetColor("_ColorD", SettingsManager.CurrentSettings.PathRemainingColor2);
        }

        private void Update()
        {
            if (LoadingManager.Instance.CurrentFlightData == null || _fullPath == null || _fullPath.Count < 2 || _interpolatedToFlightPoint.Count == 0)
            {
                return;
            }

            //3D path ray interaction
            Ray ray = Camera.GetCameraRay();

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _raycastMask))
            {
                float hoverProgress = GetProgressFromHit(hit.point);
                _progressionPathMaterialInstance.SetFloat("_HoverProgress", hoverProgress);

                if (Camera.Mouse.IsPressed(FuMouseButton.Left))
                {
                    long totalFrames = VideoPlayerManager.Instance.TotalFrameCount;
                    long targetFrame = Mathf.FloorToInt(hoverProgress * totalFrames);
                    VideoPlayerManager.Instance.SetFrame(targetFrame, false);
                    _glowTimer = _glowDuration;
                }
            }
            else
            {
                _progressionPathMaterialInstance.SetFloat("_HoverProgress", -1f);
            }

            //Interpolate UV
            float targetProgress = GetProgressAtTime(_interpolatedToFlightPoint[0].Time + TimeSpan.FromSeconds(VideoPlayerManager.Instance.Time));
            _currentProgress = Mathf.Lerp(_currentProgress, targetProgress, Time.deltaTime * 5f);
            _progressionPathMaterialInstance.SetFloat("_Progress", _currentProgress);

            //Refresh "drone" position
            Vector3 position = GetWorldPositionAtUVProgress(_currentProgress);
            Vector3 nextPosition = GetWorldPositionAtUVProgress(_currentProgress + 0.001f);
            Vector3 direction = (nextPosition - position).normalized;
            Quaternion targetRotation;

            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                Quaternion.LookRotation(direction, Vector3.up);
            }
            else
            {
                targetRotation = transform.rotation;
            }

            _droneAnchorTransform.position = position;
            _droneAnchorTransform.rotation = Quaternion.Slerp(_droneAnchorTransform.rotation, targetRotation, Time.deltaTime * 5f);

            //Glow
            if (_glowTimer > 0f)
            {
                _glowTimer -= Time.deltaTime;
                float glowProgress = Mathf.Clamp01(_glowTimer / _glowDuration);
                _progressionPathMaterialInstance.SetFloat("_GlowProgress", glowProgress);
            }
            else
            {
                _progressionPathMaterialInstance.SetFloat("_GlowProgress", 0f);
            }
        }

        private void OnDisable()
        {
            VideoPlayerManager.Instance.OnProgressChanged -= OnProgressChanged;
            TerrainManager.Instance.OnTerrainLoaded -= OnTerrainLoaded;
            SettingsManager.OnPathRemainingColor1Changed -= OnPathRemainingColor1Changed;
            SettingsManager.OnPathRemainingColor2Changed -= OnPathRemainingColor2Changed;
            Destroy(_progressionPath);
        }
        #endregion

        #region METHODS
        private void LoadFlightPath(FlightData flightData)
        {
            if (flightData == null || flightData.Points == null || flightData.Points.Count < 2)
            {
                return;
            }

            // Récupère les tuiles
            List<TileDefinition> tiles = flightData.MapDefinition.TileDefinitions;
            if (tiles == null || tiles.Count == 0)
            {
                return;
            }

            //Estimate altitude at takeoff point
            Vector3 positionGPS = new Vector3((float)flightData.EstimateTakeOffPosition.Latitude, flightData.TakeOffAltitude, (float)flightData.EstimateTakeOffPosition.Longitude);

            //Create take off position
            if (flightData.HasTakeOffPosition)
            {
                WorldUIManager.Instance.SetHomePOI(TerrainManager.Instance.ConvertGPSPositionToWorld(flightData, positionGPS));
            }

            //Create bezier path
            List<Vector3> bezierPath = TerrainManager.CreateBezierFlightPath(flightData, flightData.Points, tiles, positionGPS.y, samplesPerSegment: 10, controlOffsetFactor: 0.5f);

            //Apply path
            SetFlightPaths(flightData, bezierPath, samplesPerSegment: 20, controlOffsetFactor: 0.3f, minUVStep: 0.0005f, smoothUVs: false);

            if (_progressionPathCollider != null)
            {
                _progressionPathCollider.enabled = true;
            }
        }
        private Vector3 GetWorldPositionAtUVProgress(float progress)
        {
            for (int i = 0; i < _fullPath.Count - 1; i++)
            {
                float uv0 = _fullPath[i].UvY;
                float uv1 = _fullPath[i + 1].UvY;

                if (progress >= uv0 && progress <= uv1)
                {
                    float t = Mathf.InverseLerp(uv0, uv1, progress);
                    return Vector3.Lerp(_fullPath[i].Position, _fullPath[i + 1].Position, t);
                }
            }

            return _fullPath[_fullPath.Count - 1].Position;
        }


        internal void UnloadFlightPath()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                WorldUIManager.Instance.UnloadFlightPOIs();
                _interpolatedToFlightPoint.Clear();
                _progressionPathFilter.sharedMesh = null;
                _progressionPathCollider.enabled = false;
                _progressionPathCollider.sharedMesh = null;
            });
        }
        #endregion

        #region CALLBACKS
        private void OnProgressChanged(float progress, int flightIndex, FlightDataPoint dataPoint)
        {
            _progressionPathMaterialInstance.SetFloat("_Progress", progress);
        }
        #endregion

        #region METHODS
        private void SetFlightPaths(FlightData flightData, List<Vector3> worldPositions, int samplesPerSegment = 10, float controlOffsetFactor = 0.3f, float minUVStep = 0.0001f, bool smoothUVs = true)
        {
            var rawPath = new List<Vector3>();
            _interpolatedToFlightPoint = new List<FlightDataPoint>();

            float totalTicks = flightData.Points.Last().Time.Ticks - flightData.Points.First().Time.Ticks;

            //Bezier interpolation
            for (int i = 0; i < worldPositions.Count - 1; i++)
            {
                Vector3 p0 = worldPositions[i];
                Vector3 p1 = worldPositions[i + 1];
                Vector3 dir = (p1 - p0).normalized;

                Vector3 c0 = p0 + dir * Vector3.Distance(p0, p1) * controlOffsetFactor;
                Vector3 c1 = p1 - dir * Vector3.Distance(p0, p1) * controlOffsetFactor;

                DateTime t0 = flightData.Points[i].Time;
                DateTime t1 = flightData.Points[i + 1].Time;

                for (int j = 0; j <= samplesPerSegment; j++)
                {
                    float t = j / (float)samplesPerSegment;
                    Vector3 bezier = CubicBezier(p0, c0, c1, p1, t);
                    rawPath.Add(bezier);

                    DateTime interpolatedTime = t0 + TimeSpan.FromTicks((long)((t1 - t0).Ticks * t));
                    _interpolatedToFlightPoint.Add(new FlightDataPoint { Time = interpolatedTime });
                }
            }

            //UV Progress + cleanup
            float totalLength = 0f;
            for (int i = 0; i < rawPath.Count - 1; i++)
            {
                totalLength += Vector3.Distance(rawPath[i], rawPath[i + 1]);
            }

            float adaptiveMinDistance = Mathf.Max(0.01f, totalLength / rawPath.Count * 0.5f);
            List<Vector3> cleanedPath = new List<Vector3>();
            List<float> uvProgress = new List<float>();

            cleanedPath.Add(rawPath[0]);
            uvProgress.Add(0f);

            long startTicks = _interpolatedToFlightPoint[0].Time.Ticks;

            for (int i = 1; i < rawPath.Count; i++)
            {
                float dist = Vector3.Distance(rawPath[i], cleanedPath.Last());
                if (dist < adaptiveMinDistance) continue;

                cleanedPath.Add(rawPath[i]);

                long currentTicks = _interpolatedToFlightPoint[i].Time.Ticks;
                float uv = (currentTicks - startTicks) / (float)totalTicks;
                uvProgress.Add(uv);
            }

            //UV order correction
            for (int i = 1; i < uvProgress.Count; i++)
            {
                if (uvProgress[i] <= uvProgress[i - 1])
                    uvProgress[i] = uvProgress[i - 1] + minUVStep;
            }

            //UV smoothing
            if (smoothUVs && uvProgress.Count > 2)
            {
                var smoothed = new List<float> { uvProgress[0] };
                for (int i = 1; i < uvProgress.Count - 1; i++)
                {
                    float avg = (uvProgress[i - 1] + uvProgress[i] + uvProgress[i + 1]) / 3f;
                    smoothed.Add(avg);
                }
                smoothed.Add(uvProgress.Last());
                uvProgress = smoothed;
            }

            //Construct 3D path mesh
            _fullPath = new List<PathPoint>();
            for (int i = 0; i < cleanedPath.Count; i++)
            {
                _fullPath.Add(new PathPoint(cleanedPath[i], uvProgress[i]));
            }

            //Generate mesh with UV based on video steps
            Mesh pathMesh = GeneratePathMesh(cleanedPath, uvProgress, MESH_PATH_RADIUS, MESH_PATH_RADIAL_SEGMENT);
            _progressionPathFilter.mesh = pathMesh;
            _progressionPathCollider.sharedMesh = pathMesh;
        }

        private Mesh GeneratePathMesh(List<Vector3> pathPoints, List<float> uvProgression, float radius = 2f, int radialSegments = 4)
        {
            if (pathPoints == null || pathPoints.Count < 2 || uvProgression == null || uvProgression.Count != pathPoints.Count)
            {
                return null;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            int ringCount = pathPoints.Count;
            Vector3 up = Vector3.up;
            List<int> firstRingIndices = new List<int>();
            List<int> lastRingIndices = new List<int>();

            for (int i = 0; i < ringCount; i++)
            {
                Vector3 center = pathPoints[i];

                // Tangente sécurisée
                Vector3 tangent;
                if (i == 0)
                {
                    tangent = (pathPoints[i + 1] - center);
                }
                else if (i == ringCount - 1)
                {
                    tangent = (center - pathPoints[i - 1]);
                }
                else
                {
                    tangent = (pathPoints[i + 1] - pathPoints[i - 1]);
                }

                if (tangent.magnitude < 0.001f)
                {
                    tangent = Vector3.forward;
                }
                else
                {
                    tangent.Normalize();
                }

                // Sécuriser le vecteur up pour éviter les artefacts
                if (Mathf.Abs(Vector3.Dot(up, tangent)) > 0.99f)
                {
                    up = Vector3.Cross(tangent, Vector3.right).magnitude > 0.001f ? Vector3.right : Vector3.up;
                }

                Vector3 binormal = Vector3.Cross(up, tangent).normalized;
                Vector3 normal = Vector3.Cross(tangent, binormal).normalized;

                float v = uvProgression[i];

                for (int j = 0; j < radialSegments; j++)
                {
                    float angle = 2 * Mathf.PI * j / radialSegments;
                    Vector3 offset = Mathf.Cos(angle) * normal * radius + Mathf.Sin(angle) * binormal * radius;
                    vertices.Add(center + offset);
                    normals.Add(offset.normalized);
                    float u = (float)j / (radialSegments - 1);
                    uvs.Add(new Vector2(u, v));

                    if (i == 0) firstRingIndices.Add(vertices.Count - 1);
                    if (i == ringCount - 1) lastRingIndices.Add(vertices.Count - 1);
                }

                up = normal;
            }

            // Construction des triangles
            for (int i = 0; i < ringCount - 1; i++)
            {
                int ringStart = i * radialSegments;
                int nextRingStart = (i + 1) * radialSegments;

                for (int j = 0; j < radialSegments; j++)
                {
                    int current = ringStart + j;
                    int next = ringStart + (j + 1) % radialSegments;
                    int currentNext = nextRingStart + j;
                    int nextNext = nextRingStart + (j + 1) % radialSegments;

                    triangles.Add(currentNext);
                    triangles.Add(next);
                    triangles.Add(current);

                    triangles.Add(currentNext);
                    triangles.Add(nextNext);
                    triangles.Add(next);
                }
            }

            // Cap start
            Vector3 startCenter = pathPoints[0];
            Vector3 startTangent = (pathPoints[1] - startCenter).normalized;
            int startCenterIndex = vertices.Count;
            vertices.Add(startCenter);
            normals.Add(-startTangent);
            uvs.Add(new Vector2(0.5f, uvProgression[0]));

            for (int i = 0; i < radialSegments; i++)
            {
                int current = firstRingIndices[i];
                int next = firstRingIndices[(i + 1) % radialSegments];
                triangles.Add(startCenterIndex);
                triangles.Add(current);
                triangles.Add(next);
            }

            // Cap end
            Vector3 endCenter = pathPoints[ringCount - 1];
            Vector3 endTangent = (endCenter - pathPoints[ringCount - 2]).normalized;
            int endCenterIndex = vertices.Count;
            vertices.Add(endCenter);
            normals.Add(endTangent);
            uvs.Add(new Vector2(0.5f, uvProgression.Last()));

            for (int i = 0; i < radialSegments; i++)
            {
                int current = lastRingIndices[i];
                int next = lastRingIndices[(i + 1) % radialSegments];
                triangles.Add(endCenterIndex);
                triangles.Add(next);
                triangles.Add(current);
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }


        private float GetProgressAtTime(DateTime targetTime)
        {
            if (_interpolatedToFlightPoint == null || _interpolatedToFlightPoint.Count < 2)
            {
                return 0f;
            }

            DateTime start = _interpolatedToFlightPoint[0].Time;
            DateTime end = _interpolatedToFlightPoint[_interpolatedToFlightPoint.Count - 1].Time;

            if (targetTime <= start)
            {
                return 0f;
            }

            if (targetTime >= end)
            {
                return 1f;
            }

            for (int i = 0; i < _interpolatedToFlightPoint.Count - 1; i++)
            {
                DateTime t0 = _interpolatedToFlightPoint[i].Time;
                DateTime t1 = _interpolatedToFlightPoint[i + 1].Time;

                if (targetTime >= t0 && targetTime <= t1)
                {
                    float segmentProgress = (float)((targetTime - t0).TotalSeconds / (t1 - t0).TotalSeconds);
                    float overallProgress = (i + segmentProgress) / (_interpolatedToFlightPoint.Count - 1);
                    return overallProgress;
                }
            }

            return 0f;
        }
        private float GetProgressFromHit(Vector3 hitPoint)
        {
            if (_fullPath == null || _fullPath.Count == 0)
            {
                return -1f;
            }

            float minDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < _fullPath.Count; i++)
            {
                float dist = Vector3.Distance(hitPoint, _fullPath[i].Position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestIndex = i;
                }
            }

            return closestIndex >= 0 ? _fullPath[closestIndex].UvY : -1f;
        }

        private Vector3 CubicBezier(Vector3 p0, Vector3 c0, Vector3 c1, Vector3 p1, float t)
        {
            float u = 1 - t;
            return
                u * u * u * p0 +
                3 * u * u * t * c0 +
                3 * u * t * t * c1 +
                t * t * t * p1;
        }

        internal void UpdatePathColliderMesh()
        {
            if (_progressionPathColliderUpdater != null)
            {
                _progressionPathColliderUpdater.UpdateColliderMesh();
            }
        }

        public Vector3 GetWorldPositionAtTime(DateTime targetTime)
        {
            if (_interpolatedToFlightPoint == null || _fullPath == null || _interpolatedToFlightPoint.Count != _fullPath.Count)
            {
                return _fullPath.Count > 0 ? _fullPath[0].Position : Vector3.zero;
            }

            for (int i = 0; i < _interpolatedToFlightPoint.Count - 1; i++)
            {
                DateTime t0 = _interpolatedToFlightPoint[i].Time;
                DateTime t1 = _interpolatedToFlightPoint[i + 1].Time;

                if (targetTime >= t0 && targetTime <= t1)
                {
                    float segmentProgress = (float)((targetTime - t0).TotalSeconds / (t1 - t0).TotalSeconds);

                    return Vector3.Lerp(_fullPath[i].Position, _fullPath[i + 1].Position, segmentProgress);
                }
            }

            return _fullPath[_fullPath.Count - 1].Position;
        }
        #endregion

        #region CALLBACKS
        private void OnTerrainLoaded(FlightData flightData)
        {
            LoadFlightPath(flightData);
            BaseThickness = SettingsManager.CurrentSettings.PathWidth;
        }

        private void OnPathRemainingColor1Changed(Color color)
        {
            _progressionPathMaterialInstance.SetColor("_ColorC", SettingsManager.CurrentSettings.PathRemainingColor1);
        }

        private void OnPathRemainingColor2Changed(Color color)
        {
            _progressionPathMaterialInstance.SetColor("_ColorD", SettingsManager.CurrentSettings.PathRemainingColor2);
        }
        #endregion

        #region UI
        internal void DrawPathSettings(FuLayout layout)
        {
            using (FuGrid grid = new FuGrid("grdSceneSettings", new FuGridDefinition(2, new float[2] { 0.4f, 0.6f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                if (!IsPathVisible)
                {
                    grid.DisableNextElements();
                }

                // Convert BaseThickness to normalized value
                float normalizedThickness = Mathf.InverseLerp(-MESH_PATH_RADIUS, 1f - MESH_PATH_RADIUS, BaseThickness);

                if (grid.Slider("3D path thickness", ref normalizedThickness, 0.01f, 0.4f, 0.01f))
                {
                    // Remap normalized value back to actual thickness
                    BaseThickness = Mathf.Lerp(-MESH_PATH_RADIUS, 1f - MESH_PATH_RADIUS, normalizedThickness);
                }

                // Path remaining color 1
                Vector4 pathRemainingColor1 = (Vector4) SettingsManager.CurrentSettings.PathRemainingColor1;

                if (grid.ColorPicker("Remaining color 1", ref pathRemainingColor1))
                {
                    SettingsManager.SavePathRemainingColor1((Color)  pathRemainingColor1);
                }

                // Path remaining color 2
                Vector4 pathRemainingColor2 = (Vector4)SettingsManager.CurrentSettings.PathRemainingColor2;

                if (grid.ColorPicker("Remaining color 2", ref pathRemainingColor2))
                {
                    SettingsManager.SavePathRemainingColor2((Color)pathRemainingColor2);
                }

                grid.EnableNextElements();
            }
        }
        #endregion

        private struct PathPoint
        {
            #region ATTRIBUTES
            public Vector3 Position;
            public float UvY;
            #endregion

            #region CONSTRUCTOR
            public PathPoint(Vector3 position, float uvY)
            {
                Position = position;
                UvY = uvY;
            }
            #endregion
        }
    }

}
