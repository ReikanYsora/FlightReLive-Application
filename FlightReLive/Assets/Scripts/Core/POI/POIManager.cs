using FlightReLive.Core.Cameras;
using FlightReLive.Core.OpenVectorTile;
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
        private const float UPDATE_INTERVAL = 0.5f;         // seconds between visibility updates
        #endregion

        #region ATTRIBUTES
        [SerializeField] private float _poiMaxDistance;
        [SerializeField] private float _poiMaxViewAngle;
        private POIPool _poiPool;
        private Camera _camera;

        private readonly Dictionary<POIData, POIEntity> _activePOIs = new Dictionary<POIData, POIEntity>();
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

        #region FIXED POI
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

        internal void DeleteFixedPOI(POIEntity entity)
        {
            if (entity != null)
            {
                _poiPool.Return(entity.gameObject);
                _fixedPOIs.Remove(entity);
            }
        }
        #endregion

        #region DYNAMIC LOGIC
        private void UpdateDynamicPOIs()
        {
            IReadOnlyList<POIData> baked = OpenVectorTileManager.Instance?.BakedPOIs;

            if (baked == null || baked.Count == 0)
            {
                HideAllDynamicPOIs();
                return;
            }

            Vector3 camPos = _camera.transform.position;
            Vector3 camForward = _camera.transform.forward;
            float currentZoom = ComputeCameraZoom();

            HashSet<POIData> visibleNow = new HashSet<POIData>();

            foreach (var poi in baked)
            {
                if (string.IsNullOrEmpty(poi.Name))
                {
                    continue;
                }

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

                //Zoom / rank filtering
                if (!IsPOIVisibleAtZoom(poi.Rank, currentZoom))
                {
                    DeactivatePOI(poi);
                    continue;
                }

                //Activate if needed
                if (!_activePOIs.ContainsKey(poi))
                {
                    GameObject go = _poiPool.Get();
                    go.transform.position = poi.WorldPosition;

                    POIEntity entity = go.GetComponent<POIEntity>();
                    entity.Initialize(_camera, poi.WorldPosition, Color.red, poi.Name, 50f);
                    entity.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;

                    _activePOIs.Add(poi, entity);
                }

                visibleNow.Add(poi);
            }

            //Remove POIs that are no longer visible

            List<POIData> toRemove = _activePOIs.Keys.Except(visibleNow).ToList();
            foreach (POIData removed in toRemove)
            {
                DeactivatePOI(removed);
            }
        }

        private void DeactivatePOI(POIData poi)
        {
            if (_activePOIs.TryGetValue(poi, out var entity))
            {
                _poiPool.Return(entity.gameObject);
                _activePOIs.Remove(poi);
            }
        }

        private void HideAllDynamicPOIs()
        {
            foreach (var kv in _activePOIs)
            {
                if (kv.Value != null)
                    _poiPool.Return(kv.Value.gameObject);
            }

            _activePOIs.Clear();
        }

        private float ComputeCameraZoom()
        {
            // Adapté à ta logique de zoom (ex: altitude relative)
            float height = _camera.transform.position.y;
            return Mathf.Lerp(4f, 14f, Mathf.InverseLerp(50f, 1000f, height)); // simplifié
        }

        private bool IsPOIVisibleAtZoom(int rank, float zoom)
        {
            if (zoom <= 6) return rank <= 3;
            if (zoom <= 8) return rank <= 5;
            if (zoom <= 10) return rank <= 8;
            return true;
        }
        #endregion

        #region UNLOAD
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                foreach (var poi in _activePOIs.Values)
                    _poiPool.Return(poi.gameObject);
                _activePOIs.Clear();

                foreach (var poi in _fixedPOIs)
                    _poiPool.Return(poi.gameObject);
                _fixedPOIs.Clear();
            });
        }
        #endregion

        #region CALLBACKS
        private void OnPOIScaleChanged(float value)
        {
            float scale = value / 100f;

            foreach (var poi in _fixedPOIs)
                poi.ScaleFactor = scale;
            foreach (var kv in _activePOIs)
                kv.Value.ScaleFactor = scale;
        }

        private void OnPOIHeightChanged(float factor)
        {
            foreach (var poi in _fixedPOIs)
                poi.ElevationFactor = factor;
            foreach (var kv in _activePOIs)
                kv.Value.ElevationFactor = factor;
        }

        private void OnPOIVisibilityChanged(bool visible)
        {
            if (!visible)
            {
                HideAllDynamicPOIs();
                foreach (var poi in _fixedPOIs)
                    poi.gameObject.SetActive(false);
            }
            else
            {
                foreach (var poi in _fixedPOIs)
                    poi.gameObject.SetActive(true);
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
