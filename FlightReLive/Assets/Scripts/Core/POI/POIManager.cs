using FlightReLive.Core.Cameras;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.POI
{
    /// <summary>
    /// Optimized POI Manager: dynamically activates POIs from OpenVectorTile based on distance, angle and zoom.
    /// Uses pooled entities and only renders what’s visible in front of the camera.
    /// </summary>
    [RequireComponent(typeof(POIPool))]
    public class POIManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float UPDATE_INTERVAL = 0.5f;
        #endregion

        #region ATTRIBUTES
        [SerializeField] private float _poiMaxDistance;
        [SerializeField] private float _poiMaxViewAngle;
        private POIPool _poiPool;
        private Camera _camera;
        private List<POIEntity> _smartPOIs = new List<POIEntity>();
        private readonly List<POIEntity> _activePOIs = new List<POIEntity>();
        private readonly List<POIEntity> _fixedPOIs = new List<POIEntity>();
        private float _lastUpdate;
        #endregion

        #region PROPERTIES
        public static POIManager Instance { get; private set; }
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
            _poiPool = GetComponent<POIPool>();
        }

        private void Start()
        {
            _camera = CameraManager.Instance.ReLiveCamera;

            SettingsManager.OnPOIScaleChanged += OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged += OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityChanged += OnPOIVisibilityChanged;
        }

        private void LateUpdate()
        {
            if (_camera == null)
                _camera = CameraManager.Instance.ReLiveCamera;

            if (_camera == null)
                return;

            if (!SettingsManager.CurrentSettings.POIVisibility)
            {
                HideAllDynamicPOIs();
                return;
            }

            if (Time.time - _lastUpdate > UPDATE_INTERVAL)
            {
                _lastUpdate = Time.time;
                UpdateDynamicPOIs();
            }
        }

        private void OnDestroy()
        {
            SettingsManager.OnPOIScaleChanged -= OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged -= OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityChanged -= OnPOIVisibilityChanged;
        }
        #endregion

        #region METHODS
        internal POIEntity AddFixedPOI(string name, Transform linkedTransform, Color color, float height = 1f)
        {
            GameObject go = _poiPool.Get();
            go.transform.position = linkedTransform.position;

            POIEntity poi = go.GetComponent<POIEntity>();
            poi.Initialize(_camera, linkedTransform, color, name, height);
            poi.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;

            _fixedPOIs.Add(poi);
            return poi;
        }

        internal POIEntity AddFixedPOI(string name, Vector3 position, Color color, float height = 1f)
        {
            GameObject go = _poiPool.Get();
            go.transform.position = position;

            POIEntity poi = go.GetComponent<POIEntity>();
            poi.Initialize(_camera, position, color, name, height);
            poi.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;

            _fixedPOIs.Add(poi);
            return poi;
        }

        internal POIEntity AddSmartPOI(string name, Vector3 position, Color color, float height = 1f)
        {
            GameObject go = _poiPool.Get();
            go.transform.position = position;

            POIEntity poi = go.GetComponent<POIEntity>();
            poi.Initialize(_camera, position, color, name, height);
            poi.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;

            _smartPOIs.Add(poi);
            return poi;
        }

        internal void DeleteFixedPOI(POIEntity entity)
        {
            if (entity != null)
            {
                _poiPool.Return(entity.gameObject);
                _fixedPOIs.Remove(entity);
            }
        }

        internal void DeleteSmartPOI(POIEntity entity)
        {
            if (entity != null)
            {
                _poiPool.Return(entity.gameObject);
                _smartPOIs.Remove(entity);
            }
        }

        private void UpdateDynamicPOIs()
        {
            if (_smartPOIs == null || _smartPOIs.Count == 0)
            {
                HideAllDynamicPOIs();
                return;
            }

            Vector3 camPos = _camera.transform.position;
            Vector3 camForward = _camera.transform.forward;

            HashSet<POIEntity> visibleNow = new HashSet<POIEntity>();

            foreach (POIEntity poi in _smartPOIs)
            {
                Vector3 dir = poi.WorldPosition - camPos;
                float dist = dir.magnitude;

                //Distance culling
                if (dist > _poiMaxDistance)
                {
                    DeactivatePOI(poi);
                    continue;
                }

                //Angle culling (FOV)
                float angle = Vector3.Angle(camForward, dir.normalized);
                if (angle > _poiMaxViewAngle)
                {
                    DeactivatePOI(poi);
                    continue;
                }

                //Activate if needed
                if (!_activePOIs.Contains(poi))
                {
                    GameObject go = _poiPool.Get();
                    go.transform.position = poi.WorldPosition;
                    POIEntity poiEntity = go.GetComponent<POIEntity>();
                    poiEntity.Initialize(_camera, poi.WorldPosition, Color.red, poi.Text, 50f);
                    poiEntity.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;
                    _activePOIs.Add(poiEntity);
                }

                visibleNow.Add(poi);
            }

            //Remove POIs that are no longer visible
            List<POIEntity> toRemove = _activePOIs.Except(visibleNow).ToList();

            foreach (POIEntity removed in toRemove)
            {
                DeactivatePOI(removed);
            }
        }

        private void DeactivatePOI(POIEntity poi)
        {
            if (poi != null && _activePOIs.Contains(poi))
            {
                _poiPool.Return(poi.gameObject);
                _activePOIs.Remove(poi);
            }
        }

        private void HideAllDynamicPOIs()
        {
            foreach (POIEntity poi in _activePOIs)
            {
                if (poi != null)
                {
                    _poiPool.Return(poi.gameObject);
                }
            }

            _activePOIs.Clear();
        }
        #endregion

        #region UNLOAD
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                foreach (POIEntity poi in _activePOIs)
                {
                    _poiPool.Return(poi.gameObject);
                }

                foreach (POIEntity poi in _fixedPOIs)
                {
                    _poiPool.Return(poi.gameObject);
                }

                _activePOIs.Clear();
                _fixedPOIs.Clear();
            });
        }
        #endregion

        #region CALLBACKS
        private void OnPOIScaleChanged(float value)
        {
            float scale = value / 100f;

            foreach (POIEntity poi in _fixedPOIs)
            {
                poi.ScaleFactor = scale;
            }

            foreach (POIEntity poi in _activePOIs)
            {
                poi.ScaleFactor = scale;
            }
        }

        private void OnPOIHeightChanged(float factor)
        {
            foreach (POIEntity poi in _fixedPOIs)
            {
                poi.ElevationFactor = factor;
            }

            foreach (POIEntity poi in _activePOIs)
            {
                poi.ElevationFactor = factor;
            }
        }

        private void OnPOIVisibilityChanged(bool visible)
        {
            if (!visible)
            {
                HideAllDynamicPOIs();

                foreach (var poi in _fixedPOIs)
                {
                    poi.gameObject.SetActive(false);
                }
            }
            else
            {
                foreach (var poi in _fixedPOIs)
                {
                    poi.gameObject.SetActive(true);
                }
            }
        }
        #endregion

        #region UI
        internal void DisplayPOISettings()
        {
            using (FuGrid grid = new FuGrid("gridPOISettings",
                new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }),
                FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                bool poiEnabled = SettingsManager.CurrentSettings.POIVisibility;

                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Display POI",
                    "Display or hide all POIs.",
                    "Reset POI visibility to default.",
                    poiEnabled,
                    SettingsManager.POI_DISPLAY_STATE_DEFAULT_VALUE,
                    (x) => SettingsManager.SavePOIVisibility(x),
                    () => SettingsManager.ResetPOIVisibility());

                if (!poiEnabled)
                    grid.DisableNextElements();

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "POI scale",
                    "Adjust POI global scale.",
                    $"Reset to {SettingsManager.POI_SCALE_DEFAULT_VALUE}.",
                    SettingsManager.CurrentSettings.POIScale,
                    0.1f, 1.0f, 0.1f,
                    SettingsManager.POI_SCALE_DEFAULT_VALUE,
                    "%.1f",
                    (x) => SettingsManager.SavePOIScale(x),
                    () => SettingsManager.ResetPOIScale());

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "POI height",
                    "Vertical offset of POIs.",
                    $"Reset to {SettingsManager.POI_HEIGHT_DEFAULT_VALUE}.",
                    SettingsManager.CurrentSettings.POIHeight,
                    0f, 3f, 0.1f,
                    SettingsManager.POI_HEIGHT_DEFAULT_VALUE,
                    "%.1f",
                    (x) => SettingsManager.SavePOIHeight(x),
                    () => SettingsManager.ResetPOIHeight());
            }
        }
        #endregion
    }
}
