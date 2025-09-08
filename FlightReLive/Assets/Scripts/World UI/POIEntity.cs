using FlightReLive.Core.Settings;
using TMPro;
using UnityEngine;

namespace FlightReLive.Core.WorldUI
{
    public class POIEntity : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Transform _image;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private float _textYOffsetFromPoint = 2f;
        [SerializeField] private float _minVisibleDistance = 10f;
        [SerializeField] private float _maxVisibleDistance = 100f;
        [SerializeField] private float _manualElevation = -1f;
        [SerializeField] private float _randomElevationRange = 0.5f;
        private POIType _type;
        private float _randomOffset = 0f;
        private LineRenderer _lineRenderer;
        private Vector3 _parentOrigin;
        private Camera _targetCamera;
        private float _scaleFactor = 0.1f;
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
                if (_type == POIType.HomePoint)
                {
                    return;
                }

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
        internal void Inialize(POIType poiType, Camera camera, Vector3 parentPosition)
        {
            _type = poiType;
            _targetCamera = camera;
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = _lineMaterial;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.numCapVertices = 0;
            _lineRenderer.numCornerVertices = 0;
            _lineRenderer.positionCount = 2;
            _parentOrigin = parentPosition;
            ScaleFactor = SettingsManager.CurrentSettings.WorldIconScale / 100f;
            gameObject.SetActive(SettingsManager.CurrentSettings.Icon3DVisibility);
            ManualElevation = -1;
        }

        internal void Inialize(POIType poiType, Camera camera, Vector3 parentPosition, string text, float height = -1f)
        {
            _type = poiType;
            _text.text = text;
            _targetCamera = camera;
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = _lineMaterial;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.numCapVertices = 0;
            _lineRenderer.numCornerVertices = 0;
            _lineRenderer.positionCount = 2;
            _parentOrigin = parentPosition;
            ScaleFactor = SettingsManager.CurrentSettings.WorldIconScale / 100f;
            _randomOffset = Random.Range(0, _randomElevationRange);
            gameObject.SetActive(SettingsManager.CurrentSettings.Icon3DVisibility);
            ManualElevation = height;
        }

        private void UpdateTransparencyByDistance()
        {
            if (_targetCamera == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetCamera.transform.position);
            float t = Mathf.InverseLerp(_maxVisibleDistance, _minVisibleDistance, distance); // 0 = loin, 1 = proche
            float alpha = Mathf.Clamp01(t);

            // LineRenderer
            if (_lineRenderer != null && _lineRenderer.material.HasProperty("_Color"))
            {
                Color color = _lineRenderer.material.color;
                color.a = alpha;
                _lineRenderer.material.color = color;
            }

            // Image
            if (_image != null)
            {
                Renderer iconRenderer = _image.GetComponent<Renderer>();
                if (iconRenderer != null && iconRenderer.material.HasProperty("_Color"))
                {
                    Color iconColor = iconRenderer.material.color;
                    iconColor.a = alpha;
                    iconRenderer.material.color = iconColor;
                }
            }

            // Text
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
                // Utiliser l'échelle réelle dans le monde
                float scaleFactor = _image.lossyScale.y;
                Vector3 textOffset = imageUpDir * (_textYOffsetFromPoint * scaleFactor);
                _text.transform.position = imagePosition + textOffset;
            }
        }

        private void UpdateLineRenderer()
        {
            if (_lineRenderer == null || _image == null || transform.parent == null)
            {
                return;
            }

            RectTransform rect = _image.GetComponent<RectTransform>();

            Vector3 downWorld = rect.transform.rotation * Vector3.down;
            Vector3 start = rect.position + downWorld * rect.rect.height * rect.lossyScale.y * 0.5f;
            Vector3 end = _parentOrigin;

            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            // Adapter l'épaisseur au scale
            float scale = _image.localScale.x;
            _lineRenderer.startWidth = scale / 5f;
            _lineRenderer.endWidth = scale / 5f;
        }

        #endregion
    }

    public enum POIType
    {
        HomePoint,
        Text
    }
}
