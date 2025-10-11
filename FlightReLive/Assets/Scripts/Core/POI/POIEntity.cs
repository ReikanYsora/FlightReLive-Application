using FlightReLive.Core.Settings;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlightReLive.Core.POI
{
    public class POIEntity : MonoBehaviour
    {
        #region CONSTANTS
        private const float START_FADING_DISTANCE = 100f;
        private const float STOP_FADING_DISTANCE = 400f;
        private const float MAX_BACKGROUND_ALPHA = 0.8f;
        #endregion

        #region ATTRIBUTES
        [SerializeField] private Transform _point;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _background;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private float _textYOffsetFromPoint = 2f;

        private LineRenderer _lineRenderer;
        private Image _pointImage;
        private RectTransform _pointRect;
        private RectTransform _backgroundRect;
        private Transform _linkedTransform;
        private Camera _targetCamera;
        private float _scaleFactor = 0.1f;
        private float _heightFixedOffset = -1f;
        private Vector3? _fixedWorldPosition = null;
        private Vector3 _targetImagePosition;
        private Vector3 _targetTextPosition;
        private Vector3 _targetScale;
        private Color _color;
        private float _lerpSpeed = 10f;
        private bool _hasLinkedTransform;
        #endregion

        #region PROPERTIES
        internal float ScaleFactor
        {
            get
            {
                return _scaleFactor;
            }
            set
            {
                _scaleFactor = value;
                if (_lineRenderer != null)
                {
                    bool shouldBeVisible = _scaleFactor > Mathf.Epsilon && Mathf.Abs(_heightFixedOffset) > Mathf.Epsilon;
                    _lineRenderer.enabled = shouldBeVisible;
                }
            }
        }

        internal float ElevationFactor { get; set; }

        internal bool IsVisible { get; set; }

        internal bool IgnoreDistanceFade { get; set; }
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            if (_point != null)
            {
                _pointImage = _point.GetComponent<Image>();
                _pointRect = _point as RectTransform;
            }

            if (_background != null)
            {
                _backgroundRect = _background.rectTransform;
            }
        }

        private void Update()
        {
            if (_targetCamera == null || !IsVisible)
            {
                return;
            }

            FollowLinkedTransform();
            ElevatePOI();
            ApplyLerpedVisuals();
            BillboardToCamera();
        }

        private void LateUpdate()
        {
            if (_targetCamera == null || !IsVisible)
            {
                return;
            }

            ScaleByDistance();
            UpdateTransparencyByDistance();
            UpdateLineRenderer();
        }
        #endregion

        #region INITIALIZATION
        internal void Initialize(Camera camera, Vector3 worldPosition, Color color, string text = "", float height = -1f, bool ignoreDistanceFade = false)
        {
            _targetCamera = camera;
            _fixedWorldPosition = worldPosition;
            _hasLinkedTransform = false;
            _heightFixedOffset = height;
            _color = color;
            IgnoreDistanceFade = ignoreDistanceFade;
            IsVisible = SettingsManager.CurrentSettings.POIVisibility;

            if (!string.IsNullOrEmpty(text))
            {
                SetText(text);
            }

            if (height > Mathf.Epsilon)
            {
                EnsureLineRenderer();
            }

            ApplyColor(color);
            ScaleFactor = SettingsManager.CurrentSettings.POIScale / 100f;
            BillboardToCamera();
            gameObject.SetActive(true);
        }

        internal void Initialize(Camera camera, Transform linkedTransform, Color color, string text = "", float height = -1f, bool ignoreDistanceFade = false)
        {
            _targetCamera = camera;
            _linkedTransform = linkedTransform;
            _hasLinkedTransform = linkedTransform != null;
            _heightFixedOffset = height;
            _color = color;
            IgnoreDistanceFade = ignoreDistanceFade;
            IsVisible = SettingsManager.CurrentSettings.POIVisibility;

            if (!string.IsNullOrEmpty(text))
            {
                SetText(text);
            }

            if (height > Mathf.Epsilon)
            {
                EnsureLineRenderer();
            }

            ApplyColor(color);
            ScaleFactor = SettingsManager.CurrentSettings.POIScale / 100f;
            BillboardToCamera();
            gameObject.SetActive(true);
        }
        #endregion

        #region VISUALS
        private void EnsureLineRenderer()
        {
            if (_lineRenderer != null)
                return;

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = _lineMaterial;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.startWidth = 0.3f;
            _lineRenderer.endWidth = 0.3f;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startColor = _color;
            _lineRenderer.endColor = _color;
            _lineRenderer.enabled = _scaleFactor > Mathf.Epsilon && Mathf.Abs(_heightFixedOffset) > Mathf.Epsilon;
        }

        private void ApplyColor(Color color)
        {
            if (_pointImage != null)
            {
                _pointImage.color = color;
            }

            if (_background != null)
            {
                Color alphaColor = color;
                alphaColor.a = MAX_BACKGROUND_ALPHA;
                _background.color = alphaColor;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FollowLinkedTransform()
        {
            if (_hasLinkedTransform)
            {
                transform.position = _linkedTransform.position;
            }
            else if (_fixedWorldPosition.HasValue)
            {
                transform.position = _fixedWorldPosition.Value;
            }
        }

        private void UpdateTransparencyByDistance()
        {
            float alpha = 1f;

            if (!IgnoreDistanceFade)
            {
                float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);
                alpha = 1f - Mathf.Clamp01(Mathf.InverseLerp(START_FADING_DISTANCE, STOP_FADING_DISTANCE, distance));
            }

            SetAlpha(alpha);
        }

        private void SetAlpha(float alpha)
        {
            if (_lineRenderer != null && _lineRenderer.material.HasProperty("_Color"))
            {
                Color lc = _lineRenderer.material.color;
                lc.a = alpha;
                _lineRenderer.material.color = lc;
            }

            if (_pointImage != null)
            {
                Color pc = _pointImage.color;
                pc.a = alpha;
                _pointImage.color = pc;
            }

            if (_background != null)
            {
                Color bc = _background.color;
                bc.a = alpha * MAX_BACKGROUND_ALPHA;
                _background.color = bc;
            }

            if (_text != null)
            {
                Color tc = _text.color;
                tc.a = alpha;
                _text.color = tc;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BillboardToCamera()
        {
            if (_targetCamera != null)
            {
                transform.forward = _targetCamera.transform.forward;
            }
        }

        private void ScaleByDistance()
        {
            Vector3 camPos = _targetCamera.transform.position;
            Vector3 toPoi = transform.position - camPos;
            float distance = toPoi.magnitude;
            Vector3 forward = _targetCamera.transform.forward;
            float angleCos = Vector3.Dot(forward, toPoi.normalized);
            angleCos = Mathf.Max(angleCos, 0.35f);
            float adjustedDistance = distance * angleCos;
            _targetScale = Vector3.one * (adjustedDistance * _scaleFactor);
        }

        private void ApplyLerpedVisuals()
        {
            if (_point != null)
            {
                _point.position = Vector3.Lerp(_point.position, _targetImagePosition, Time.deltaTime * _lerpSpeed);
                _point.localScale = Vector3.Lerp(_point.localScale, _targetScale, Time.deltaTime * _lerpSpeed);
            }

            if (_text != null)
            {
                Transform textTransform = _text.transform;
                Vector3 newPos = Vector3.Lerp(textTransform.position, _targetTextPosition, Time.deltaTime * _lerpSpeed);
                Vector3 newScale = Vector3.Lerp(textTransform.localScale, _targetScale, Time.deltaTime * _lerpSpeed);

                textTransform.position = newPos;
                textTransform.localScale = newScale;

                if (_backgroundRect != null)
                {
                    _backgroundRect.position = newPos;
                    _backgroundRect.localScale = newScale;
                    _backgroundRect.sizeDelta = _text.rectTransform.sizeDelta;
                }
            }
        }

        private void ElevatePOI()
        {
            float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);
            float elevation = _heightFixedOffset >= 0f
                ? _heightFixedOffset * (2f * distance * Mathf.Tan(_targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)) / Screen.height
                : distance * 0.2f;

            elevation *= ElevationFactor;

            Vector3 imageUpDir = _point.up;
            Vector3 basePosition = _hasLinkedTransform ? _linkedTransform.position : (_fixedWorldPosition ?? transform.position);
            _targetImagePosition = basePosition + imageUpDir * elevation;

            if (_text != null)
            {
                Vector3 textOffset = imageUpDir * (_textYOffsetFromPoint * _point.lossyScale.y);
                _targetTextPosition = _targetImagePosition + textOffset;
            }
        }

        private void UpdateLineRenderer()
        {
            if (_lineRenderer == null || Mathf.Abs(_heightFixedOffset) < Mathf.Epsilon)
            {
                return;
            }

            bool active = _scaleFactor > Mathf.Epsilon;
            if (_lineRenderer.enabled != active)
            {
                _lineRenderer.enabled = active;
            }

            if (!active || _pointRect == null)
            {
                return;
            }

            Vector3 downWorld = _pointRect.rotation * Vector3.down;
            Vector3 start = _pointRect.position + downWorld * _pointRect.rect.height * _pointRect.lossyScale.y * 0.5f;
            Vector3 end = _hasLinkedTransform ? _linkedTransform.position : (_fixedWorldPosition ?? transform.position);

            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            float distance = Vector3.Distance(_targetCamera.transform.position, start);
            float worldLineWidth = 2f * distance * Mathf.Tan(_targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height * 2f;

            _lineRenderer.startWidth = worldLineWidth;
            _lineRenderer.endWidth = worldLineWidth;
        }
        #endregion

        #region TEXT / RESET
        internal void SetText(string text)
        {
            if (_text == null)
            {
                return;
            }

            _text.text = text;
            _text.ForceMeshUpdate();

            RectTransform textRect = _text.rectTransform;
            if (textRect != null)
            {
                float textWidth = _text.preferredWidth + 10f;
                textRect.sizeDelta = new Vector2(textWidth, textRect.sizeDelta.y);

                if (_backgroundRect != null)
                    _backgroundRect.sizeDelta = textRect.sizeDelta;
            }
        }

        internal void Reset()
        {
            IsVisible = false;
            IgnoreDistanceFade = false;
            _linkedTransform = null;
            _fixedWorldPosition = null;
            _targetCamera = null;
            _color = Color.white;
            _scaleFactor = 0.1f;
            _heightFixedOffset = -1f;
        }
        #endregion
    }
}
