using FlightReLive.Core.Settings;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlightReLive.Core.POI
{
    public class POIEntity : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Transform _point;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _background;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private float _textYOffsetFromPoint = 2f;
        [SerializeField] private float _minVisibleDistance = 10f;
        [SerializeField] private float _maxVisibleDistance = 2000f;

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

        internal float ElevationFactor { set; get; }

        internal Vector3 WorldPosition
        {
            get
            {
                return _hasLinkedTransform ? _linkedTransform.position : (_fixedWorldPosition ?? transform.position);
            }
        }

        internal string Text
        {
            get
            {
                return _text != null ? _text.text : string.Empty;
            }
        }
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
            FollowLinkedTransform();
            ElevatePOI();
            ApplyLerpedVisuals();
            BillboardToCamera();
        }

        private void LateUpdate()
        {
            ScaleByDistance();
            UpdateTransparencyByDistance();
            UpdateLineRenderer();
        }
        #endregion

        #region METHODS
        internal void Initialize(Camera camera, Transform linkedTransform, Color color, string text = "", float height = -1f)
        {
            _targetCamera = camera;
            _linkedTransform = linkedTransform;
            _hasLinkedTransform = linkedTransform != null;
            _heightFixedOffset = height;
            _color = color;

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
            gameObject.SetActive(SettingsManager.CurrentSettings.POIVisibility);
        }

        internal void Initialize(Camera camera, Vector3 worldPosition, Color color, string text = "", float height = -1f)
        {
            _targetCamera = camera;
            _fixedWorldPosition = worldPosition;
            _hasLinkedTransform = false;
            _heightFixedOffset = height;
            _color = color;

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
            gameObject.SetActive(SettingsManager.CurrentSettings.POIVisibility);
        }

        private void EnsureLineRenderer()
        {
            if (_lineRenderer != null)
            {
                return;
            }

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = _lineMaterial;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.startWidth = 0.3f;
            _lineRenderer.endWidth = 0.3f;
            _lineRenderer.numCapVertices = 0;
            _lineRenderer.numCornerVertices = 0;
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

            if (_lineRenderer != null && _lineRenderer.material.HasProperty("_Color"))
            {
                _lineRenderer.material.color = color;
            }

            if (_background != null)
            {
                Color alphaColor = color;
                alphaColor.a = 0.4f;
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
            if (_targetCamera == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);
            float alpha = 1f - Mathf.Clamp01(Mathf.InverseLerp(_minVisibleDistance, _maxVisibleDistance, distance));

            if (_lineRenderer != null && _lineRenderer.material.HasProperty("_Color"))
            {
                Color lineColor = _lineRenderer.material.color;
                lineColor.a = alpha;
                _lineRenderer.material.color = lineColor;
            }

            if (_pointImage != null)
            {
                Color iconColor = _pointImage.color;
                iconColor.a = alpha;
                _pointImage.color = iconColor;
            }

            if (_text != null)
            {
                Color textColor = _text.color;
                textColor.a = alpha;
                _text.color = textColor;
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
            if (_targetCamera == null)
            {
                return;
            }

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
            if (_targetCamera == null || _point == null)
            {
                return;
            }

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
            if (_lineRenderer == null || Mathf.Abs(_heightFixedOffset) < Mathf.Epsilon || _targetCamera == null)
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
                {
                    _backgroundRect.sizeDelta = textRect.sizeDelta;
                }
            }
        }
        #endregion
    }
}
