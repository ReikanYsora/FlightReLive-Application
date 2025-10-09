using FlightReLive.Core.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlightReLive.Core.POI
{
    public class POIEntity : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Transform _image;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private float _textYOffsetFromPoint = 2f;
        [SerializeField] private float _minVisibleDistance = 10f;
        [SerializeField] private float _maxVisibleDistance = 2000f;
        [SerializeField] private float _randomElevationRange = 0.5f;

        private float _randomOffset = 0f;
        private LineRenderer _lineRenderer;
        private Vector3 _parentOrigin;
        private Camera _targetCamera;
        private float _scaleFactor = 0.1f;
        private float _manualElevation = -1f;
        #endregion

        #region PROPERTIES
        internal float ScaleFactor
        {
            set 
            { 
                _scaleFactor = value;
            }
            get 
            { 
                return _scaleFactor;
            }
        }

        internal float ManualElevation
        {
            set
            { 
                _manualElevation = value;
            }
            get
            { 
                return _manualElevation;
            }
        }
        #endregion

        #region UNITY METHODS
        private void LateUpdate()
        {
            BillboardToCamera();
            ScaleByDistance();
            UpdateLineRenderer();
            ElevatePOI();
            UpdateTransparencyByDistance();
        }
        #endregion

        #region METHODS
        internal void Initialize(Camera camera, Vector3 parentPosition, string text = "", float height = -1f)
        {
            _targetCamera = camera;
            _parentOrigin = parentPosition;

            if (!string.IsNullOrEmpty(text))
            {
                _text.text = text;
            }

            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
                _lineRenderer.material = _lineMaterial;
                _lineRenderer.textureMode = LineTextureMode.Tile;
                _lineRenderer.alignment = LineAlignment.TransformZ;
                _lineRenderer.startWidth = 0.3f;
                _lineRenderer.endWidth = 0.3f;
                _lineRenderer.numCapVertices = 0;
                _lineRenderer.numCornerVertices = 0;
                _lineRenderer.positionCount = 2;
            }

            ScaleFactor = SettingsManager.CurrentSettings.POIScale / 100f;
            _randomOffset = Random.Range(0, _randomElevationRange);
            ManualElevation = height;
            gameObject.SetActive(SettingsManager.CurrentSettings.POIVisibility);
        }

        private void UpdateTransparencyByDistance()
        {
            if (_targetCamera == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);

            //Normalisation : 0 = near, 1 = far
            float t = Mathf.InverseLerp(_minVisibleDistance, _maxVisibleDistance, distance);
            float alpha = 1f - Mathf.Clamp01(t);

            //LineRenderer
            if (_lineRenderer != null && _lineRenderer.material.HasProperty("_Color"))
            {
                Color color = _lineRenderer.material.color;
                color.a = alpha;
                _lineRenderer.material.color = color;
            }

            //Image
            if (_image != null)
            {
                Image imageRenderer = _image.GetComponent<Image>();
                if (imageRenderer != null)
                {
                    Color iconColor = imageRenderer.color;
                    iconColor.a = alpha;
                    imageRenderer.color = iconColor;
                }
            }

            //Text
            if (_text != null)
            {
                Color textColor = _text.color;
                textColor.a = alpha;
                _text.color = textColor;
            }
        }

        private void BillboardToCamera()
        {
            if (_targetCamera == null)
            {
                return;
            }

            transform.forward = _targetCamera.transform.forward;
        }

        private void ScaleByDistance()
        {
            if (_targetCamera == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);
            float scale = distance * _scaleFactor;
            Vector3 newScale = Vector3.one * scale;
            _image.localScale = newScale;

            if (_text != null)
            {
                _text.transform.localScale = newScale;
            }
        }

        private void ElevatePOI()
        {
            if (_targetCamera == null || _image == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);
            float elevation = (_manualElevation >= 0f ? _manualElevation : distance * 0.2f) + _randomOffset;
            Vector3 imageUpDir = _image.up.normalized;
            Vector3 imagePosition = _parentOrigin + imageUpDir * elevation;
            _image.position = imagePosition;

            if (_text != null)
            {
                float scaleFactor = _image.lossyScale.y;
                Vector3 textOffset = imageUpDir * (_textYOffsetFromPoint * scaleFactor);
                _text.transform.position = imagePosition + textOffset;
            }
        }

        private void UpdateLineRenderer()
        {
            if (_lineRenderer == null || _image == null)
            {
                return;
            }

            RectTransform rect = _image.GetComponent<RectTransform>();
            Vector3 downWorld = rect.transform.rotation * Vector3.down;
            Vector3 start = rect.position + downWorld * rect.rect.height * rect.lossyScale.y * 0.5f;
            Vector3 end = _parentOrigin;

            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            float scale = _image.localScale.x;
            _lineRenderer.startWidth = scale / 5f;
            _lineRenderer.endWidth = scale / 5f;
        }
        #endregion
    }
}
