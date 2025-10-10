using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.POI
{
    /// <summary>
    /// Manager responsible for handling 3D world-space UI icons (POIs).
    /// Supports tile-by-tile loading/unloading for dynamic streaming.
    /// </summary>
    [RequireComponent(typeof(POIPool))]
    public class POIManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Camera _mainCamera;
        private POIPool _poiPool;

        private readonly List<POIEntity> _allPOIs = new List<POIEntity>();
        private readonly Dictionary<(int, int), List<POIEntity>> _tileToPOIs = new Dictionary<(int, int), List<POIEntity>>();
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
            SettingsManager.OnPOIScaleChanged += OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged += OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityChanged += OnPOIVisibilityChanged;
        }

        private void LateUpdate()
        {
            UpdatePOIVisibility();
        }

        private void OnDestroy()
        {
            SettingsManager.OnPOIScaleChanged -= OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged -= OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityChanged -= OnPOIVisibilityChanged;
        }
        #endregion

        #region METHODS
        internal POIEntity CreatePOIText(string text, Transform linkedTransform, Color color, float height = 1f)
        {
            GameObject poiGO = _poiPool.Get();
            poiGO.transform.position = linkedTransform.position;
            POIEntity poiEntity = poiGO.GetComponent<POIEntity>();
            poiEntity.Initialize(_mainCamera, linkedTransform, color, text, height);
            poiEntity.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;
            _allPOIs.Add(poiEntity);
            return poiEntity;
        }

        internal POIEntity CreatePOIText(string text, Vector3 position, Color color, float height = 1f)
        {
            GameObject poiGO = _poiPool.Get();
            poiGO.transform.position = position;
            POIEntity poiEntity = poiGO.GetComponent<POIEntity>();
            poiEntity.Initialize(_mainCamera, position, color, text, height);
            poiEntity.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;
            _allPOIs.Add(poiEntity);
            return poiEntity;
        }

        /// <summary>
        /// Updates visibility of POIs based on global toggle.
        /// </summary>
        internal void UpdatePOIVisibility()
        {
            bool globalVisibility = SettingsManager.CurrentSettings.POIVisibility;

            foreach (POIEntity poi in _allPOIs)
            {
                if (poi != null && poi.gameObject.activeSelf != globalVisibility)
                {
                    poi.gameObject.SetActive(globalVisibility);
                }
            }
        }

        /// <summary>
        /// Unloads all POIs from the scene.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                foreach (POIEntity poi in _allPOIs)
                {
                    _poiPool.Return(poi.gameObject);
                }

                _allPOIs.Clear();
                _tileToPOIs.Clear();
            });
        }

        /// <summary>
        /// Delete a specific POI point
        /// </summary>
        /// <param name="pivotPointPOI"></param>
        internal void Delete(POIEntity pivotPointPOI)
        {
            bool poiFounded = false;

            foreach (POIEntity poi in _allPOIs)
            {
                if (poi == pivotPointPOI)
                {
                    _poiPool.Return(poi.gameObject);
                    poiFounded = true;
                    break;
                }
            }

            if (poiFounded)
            {
                _allPOIs.Remove(pivotPointPOI);
            }
        }
        #endregion

        #region CALLBACKS
        private void OnPOIScaleChanged(float value)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.ScaleFactor = value / 100f;
            }
        }

        private void OnPOIHeightChanged(float factor)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.ElevationFactor = factor;
            }
        }

        private void OnPOIVisibilityChanged(bool visibility)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.gameObject.SetActive(visibility);
            }
        }
        #endregion

        #region UI
        internal void DisplayPOISettings()
        {
            using (FuGrid grid = new FuGrid("gridPOISettings", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                if (_allPOIs.Count == 0)
                {
                    grid.DisableNextElements();
                }

                bool poiEnabled = SettingsManager.CurrentSettings.POIVisibility;

                //Display POI settings
                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Display POI",
                    "Display or hide POI.",
                    $"Reset POI display state to default value.",
                    poiEnabled,
                    SettingsManager.POI_DISPLAY_STATE_DEFAULT_VALUE,
                     (x) => SettingsManager.SavePOIVisibility(x),
                     () => SettingsManager.ResetPOIVisibility());

                if (!poiEnabled)
                {
                    grid.DisableNextElements();
                }

                //POI scale settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "POI scale",
                    "Define POI scale value.",
                    $"Reset POI scale  to default value ({SettingsManager.POI_SCALE_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.POIScale,
                    0.1f,
                    1.0f,
                    0.1f,
                    SettingsManager.POI_SCALE_DEFAULT_VALUE,
                    "%.1f",
                     (x) => SettingsManager.SavePOIScale(x),
                     () => SettingsManager.ResetPOIScale());

                //POI height settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "POI height",
                    "Define POI height value.",
                    $"Reset POI height  to default value ({SettingsManager.POI_HEIGHT_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.POIHeight,
                    0f,
                    3f,
                    0.1f,
                    SettingsManager.POI_HEIGHT_DEFAULT_VALUE,
                    "%.1f",
                     (x) => SettingsManager.SavePOIHeight(x),
                     () => SettingsManager.ResetPOIHeight());
            }
        }
        #endregion
    }
}
