using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.ProceduralTerrain;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System.Collections.Generic;
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

        [Header("Visibility")]
        private float _maxVisibleDistance;

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
            _maxVisibleDistance = SettingsManager.CurrentSettings.POIVisibilityDistance;
            SettingsManager.OnPOIScaleChanged += OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged += OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityDistanceChanged += OnPOIVisibilityDistanceChanged;
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
            SettingsManager.OnPOIVisibilityDistanceChanged -= OnPOIVisibilityDistanceChanged;
            SettingsManager.OnPOIVisibilityChanged -= OnPOIVisibilityChanged;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Loads all POIs from a specific tile.
        /// </summary>
        internal void LoadTile(TileDefinition tile, FlightData flightData)
        {
            if (tile.GeoData == null || tile.GeoData.features == null)
            {
                return;
            }

            HashSet<string> processedKeys = new HashSet<string>();
            List<POIEntity> createdForTile = new List<POIEntity>();

            foreach (Feature feature in tile.GeoData.features)
            {
                string name = feature.place_name ?? feature.text ?? "Unknown name";

                if (feature.geometry?.coordinates == null || feature.geometry.coordinates.Count < 2)
                {
                    continue;
                }

                FlightGPSData gpsData = new FlightGPSData
                {
                    Longitude = feature.geometry.coordinates[0],
                    Latitude = feature.geometry.coordinates[1]
                };

                string key = $"{name}_{gpsData.Latitude}_{gpsData.Longitude}";

                if (processedKeys.Contains(name))
                {
                    continue;
                }

                processedKeys.Add(key);

                float altitude = flightData.GetAltitudeAtPosition(tile, gpsData);
                Vector3 gpsVector3 = new Vector3((float)gpsData.Latitude, altitude, (float)gpsData.Longitude);
                Vector3 worldPos = flightData.ConvertGPSPositionToWorld(gpsVector3);

                GameObject poiGO = _poiPool.Get();
                poiGO.transform.position = worldPos;
                POIEntity poiEntity = poiGO.GetComponent<POIEntity>();
                poiEntity.Inialize(_mainCamera, worldPos, name, SettingsManager.CurrentSettings.POIHeight);
                _allPOIs.Add(poiEntity);
                createdForTile.Add(poiEntity);
            }

            _tileToPOIs[(tile.X, tile.Y)] = createdForTile;

            //Cleanup
            tile.GeoData = null;
        }

        /// <summary>
        /// Updates visibility of POIs based on distance to camera.
        /// </summary>
        internal void UpdatePOIVisibility()
        {
            if (_mainCamera == null)
            {
                return;
            }

            Vector3 camPos = _mainCamera.transform.position;

            foreach (POIEntity poi in _allPOIs)
            {
                float distance = Vector3.Distance(camPos, poi.transform.position);
                bool shouldBeVisible = distance <= _maxVisibleDistance;

                if (poi.gameObject.activeSelf != shouldBeVisible)
                {
                    poi.gameObject.SetActive(shouldBeVisible);
                }
            }
        }

        /// <summary>
        /// Unloads all POIs from the scene.
        /// </summary>
        internal void Unload()
        {
            foreach (POIEntity poi in _allPOIs)
            {
                _poiPool.Return(poi.gameObject);
            }

            _allPOIs.Clear();
            _tileToPOIs.Clear();
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

        private void OnPOIHeightChanged(float height)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.ManualElevation = height;
            }
        }

        private void OnPOIVisibilityDistanceChanged(float distance)
        {
            _maxVisibleDistance = SettingsManager.CurrentSettings.POIVisibilityDistance;
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
        internal void DisplayWorldUISettings(FuGrid grid)
        {
            bool poiVisibility = SettingsManager.CurrentSettings.POIVisibility;

            if (grid.Toggle("Show POI", ref poiVisibility))
            {
                SettingsManager.SavePOIVisibility(poiVisibility);
            }

            if (!poiVisibility)
            {
                grid.DisableNextElements();
            }

            float poiScale = SettingsManager.CurrentSettings.POIScale;
            if (grid.Slider("POI scale", ref poiScale, 0.5f, 1f, 0.01f, format: "%.01f"))
            {
                SettingsManager.SavePOIScale(poiScale);
            }

            float poiHeight = SettingsManager.CurrentSettings.POIHeight;
            if (grid.Slider("POI height", ref poiHeight, 0f, 10f, 0.1f, format: "%.1f"))
            {
                SettingsManager.SavePOIHeight(poiHeight);
            }

            float poiVisibilityDistance = SettingsManager.CurrentSettings.POIVisibilityDistance;
            if (grid.Slider("POI distance", ref poiVisibilityDistance, 100f, 2000f, 0.1f, format: "%.1f"))
            {
                SettingsManager.SavePOIVisibilityDistance(poiVisibilityDistance);
            }
        }
        #endregion
    }
}
