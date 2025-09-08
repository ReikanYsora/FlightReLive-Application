using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Loading
{
    internal class LoadingAnimationManager : MonoBehaviour
    {
        #region ATTTRIBUTES
        [Header("Loading Grid")]
        [SerializeField][Range(0f, 1f)] private float _displacementRadius = 0.8f;
        [SerializeField][Range(0f, 1f)] private float _edgeFalloffRadius = 0.8f;
        [SerializeField] private float _maxDisplacementHeight = 10f;
        [SerializeField] private int _gridResolution = 64;
        [SerializeField] private float _gridWidth = 10f;
        [SerializeField][ColorUsage(true, true)] private Color _surfaceColor = new Color(0f, 0.478f, 1f);
        [SerializeField][ColorUsage(true, true)] private Color _wireframeColor = new Color(1f, 0.46f, 0f, 1f);
        [SerializeField] private Material _loadingMaterial;

        [Header("Progress Ring")]
        [SerializeField] private Material _ringMaterial;
        [SerializeField] private float _ringWidth = 0.1f;
        [SerializeField][ColorUsage(true, true)] private Color _progressColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float _ringRadius = 8f;
        [SerializeField] private float _ringHeight = 3f;
        [SerializeField] private int _ringSegments = 64;

        private Material _sharedMaterialInstance;
        private GameObject _loadingSurface;
        private GameObject _loadingWireframe;
        private GameObject _loadingRing;
        private MeshFilter _loadingSurfaceFilter;
        private MeshFilter _loadingWireframeFilter;
        private MeshRenderer _loadingSurfaceRenderer;
        private MeshRenderer _loadingWireframeRenderer;
        private LineRenderer _progressRingRenderer;
        private float _smoothedProgress = 0f;
        #endregion

        #region PROPERTIES
        public static LoadingManager Instance { get; internal set; }

        public Vector3 ProgressPosition { get; internal set; }

        public float Progress { get; internal set; }
        #endregion
        #region UNITY_METHODS
        private void Awake()
        {
            Progress = 0;
        }

        private void Start()
        {
            _sharedMaterialInstance = new Material(_loadingMaterial);

            //Initialize loading grid
            _loadingSurface = new GameObject("Loading Surface");
            _loadingSurface.SetActive(false);
            _loadingSurface.transform.parent = transform;
            _loadingSurfaceFilter = _loadingSurface.AddComponent<MeshFilter>();
            _loadingSurfaceRenderer = _loadingSurface.AddComponent<MeshRenderer>();
            _loadingSurfaceRenderer.sharedMaterial = _sharedMaterialInstance;

            _loadingWireframe = new GameObject("Loading Wireframe");
            _loadingWireframe.SetActive(false);
            _loadingWireframe.transform.parent = transform;
            _loadingWireframe.transform.position = new Vector3(0f, 0.01f, 0f);
            _loadingWireframeFilter = _loadingWireframe.AddComponent<MeshFilter>();
            _loadingWireframeRenderer = _loadingWireframe.AddComponent<MeshRenderer>();
            _loadingWireframeRenderer.sharedMaterial = _sharedMaterialInstance;

            _sharedMaterialInstance.SetColor("_EdgeColor", _surfaceColor);
            _sharedMaterialInstance.SetFloat("_MaxHeight", _maxDisplacementHeight);
            _sharedMaterialInstance.SetFloat("_DisplacementRadius", _displacementRadius);
            _sharedMaterialInstance.SetFloat("_EdgeFalloffRadius", _edgeFalloffRadius);

            Mesh surfaceMesh = new Mesh();
            Mesh wireframeMesh = new Mesh();

            GenerateLoadingMeshes(_gridResolution, _gridWidth, _surfaceColor, _wireframeColor, surfaceMesh, wireframeMesh);

            _loadingSurfaceFilter.mesh = surfaceMesh;
            _loadingWireframeFilter.mesh = wireframeMesh;

            CreateProgressRing();
        }

        private void Update()
        {
            _smoothedProgress = Mathf.Lerp(_smoothedProgress, Progress, Time.deltaTime * 5f);
            _sharedMaterialInstance.SetFloat("_Progress", _smoothedProgress);
            _sharedMaterialInstance.SetFloat("_CustomTime", Time.time);

            UpdateProgressRing();
        }

        #endregion

        #region METHODS
        internal void StartLoadingAnimation()
        {
            ApplicationManager.Instance.DisablePostProcessing();
            _loadingSurface.SetActive(true);
            _loadingWireframe.SetActive(true);
            _loadingRing.SetActive(true);
        }

        internal void StopLoadingAnimation()
        {
            _loadingSurface.SetActive(false);
            _loadingWireframe.SetActive(false);
            _loadingRing.SetActive(false);
            Progress = 0;
            ApplicationManager.Instance.EnablePostProcessing();
        }

        private void GenerateLoadingMeshes(
            int resolution,
            float width,
            Color surfaceColor,
            Color wireFrameColor,
            Mesh surfaceMesh,
            Mesh wireframeMesh)
        {
            int vertCount = (resolution + 1) * (resolution + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Color[] surfaceColors = new Color[vertCount];
            Color[] wireColors = new Color[vertCount];
            int[] surfaceIndices = new int[resolution * resolution * 6];
            List<int> wireIndices = new List<int>();

            float step = width / resolution;
            float halfWidth = width / 2f;

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    int i = x + y * (resolution + 1);
                    float posX = x * step - halfWidth;
                    float posY = y * step - halfWidth;

                    vertices[i] = new Vector3(posX, 0f, posY);
                    uvs[i] = new Vector2((float)x / resolution, (float)y / resolution);
                    surfaceColors[i] = surfaceColor;
                    wireColors[i] = wireFrameColor;

                    if (x < resolution && y < resolution)
                    {
                        int i0 = i;
                        int i1 = i + 1;
                        int i2 = i + resolution + 1;
                        int i3 = i2 + 1;

                        surfaceIndices[(x + y * resolution) * 6 + 0] = i0;
                        surfaceIndices[(x + y * resolution) * 6 + 1] = i2;
                        surfaceIndices[(x + y * resolution) * 6 + 2] = i1;
                        surfaceIndices[(x + y * resolution) * 6 + 3] = i1;
                        surfaceIndices[(x + y * resolution) * 6 + 4] = i2;
                        surfaceIndices[(x + y * resolution) * 6 + 5] = i3;

                        wireIndices.AddRange(new int[] { i0, i1, i0, i2 });
                    }
                }
            }

            surfaceMesh.Clear();
            surfaceMesh.MarkDynamic();
            surfaceMesh.name = "LoadingSurface";
            surfaceMesh.vertices = vertices;
            surfaceMesh.uv = uvs;
            surfaceMesh.colors = surfaceColors;
            surfaceMesh.triangles = surfaceIndices;
            surfaceMesh.RecalculateNormals();

            wireframeMesh.Clear();
            wireframeMesh.MarkDynamic();
            wireframeMesh.name = "LoadingWireframe";
            wireframeMesh.vertices = vertices;
            wireframeMesh.uv = uvs;
            wireframeMesh.colors = wireColors;
            wireframeMesh.SetIndices(wireIndices.ToArray(), MeshTopology.Lines, 0);
        }

        private void CreateProgressRing()
        {
            _loadingRing = new GameObject("ProgressRing");
            _loadingRing.SetActive(false);
            _loadingRing.transform.parent = transform;
            _loadingRing.transform.localPosition = new Vector3(0f, _ringHeight, 0f);
            _progressRingRenderer = _loadingRing.AddComponent<LineRenderer>();
            _progressRingRenderer.positionCount = _ringSegments + 1;
            _progressRingRenderer.useWorldSpace = false;
            _progressRingRenderer.loop = true;
            _progressRingRenderer.widthMultiplier = _ringWidth;

            if (_ringMaterial != null)
            {
                _progressRingRenderer.material = _ringMaterial;
            }

            Vector3[] positions = new Vector3[_ringSegments + 1];

            for (int i = 0; i <= _ringSegments; i++)
            {
                float angle = (float)i / _ringSegments * Mathf.PI * 2f;
                positions[i] = new Vector3(Mathf.Cos(angle) * _ringRadius, 0f, Mathf.Sin(angle) * _ringRadius);
            }

            _progressRingRenderer.SetPositions(positions);
        }

        private void UpdateProgressRing()
        {
            if (_progressRingRenderer == null || _smoothedProgress <= 0f)
            {
                _progressRingRenderer.enabled = false;
                return;
            }
            else
            {
                _progressRingRenderer.enabled = true;
            }

            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys;
            GradientAlphaKey[] alphaKeys;

            if (_smoothedProgress >= 1f)
            {
                colorKeys = new GradientColorKey[1];
                alphaKeys = new GradientAlphaKey[1];

                colorKeys[0] = new GradientColorKey(_progressColor, 0f);
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            }
            else
            {
                float fadeStart = Mathf.Clamp01(_smoothedProgress - 0.05f);
                float fadeEnd = Mathf.Clamp01(_smoothedProgress);

                colorKeys = new GradientColorKey[2];
                alphaKeys = new GradientAlphaKey[3];

                colorKeys[0] = new GradientColorKey(_progressColor, 0f);
                colorKeys[1] = new GradientColorKey(_progressColor, 1f);

                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, fadeStart);
                alphaKeys[2] = new GradientAlphaKey(0f, fadeEnd);
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            _progressRingRenderer.colorGradient = gradient;
        }
        #endregion
    }
}
