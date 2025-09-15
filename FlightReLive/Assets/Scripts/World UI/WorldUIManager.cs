using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.WorldUI
{
    /// <summary>
    /// Manager responsible for handling 3D world-space UI icons (POIs).
    /// Supports tile-by-tile loading/unloading for dynamic streaming.
    /// </summary>
    public class WorldUIManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Camera _mainCamera;

        [Header("3D Icons prefabs")]
        [SerializeField] private GameObject _gpsPrefab;

        private readonly List<POIEntity> _allPOIs = new List<POIEntity>();
        private readonly Dictionary<(int, int), List<POIEntity>> _tileToPOIs = new Dictionary<(int, int), List<POIEntity>>();
        #endregion

        #region PROPERTIES
        public static WorldUIManager Instance { get; private set; }
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
        }

        private void Start()
        {
            SettingsManager.OnWorldIconScaleChanged += OnWorldIconScaleChanged;
            SettingsManager.OnWorldIconHeightChanged += On3DIconHeightChanged;
            SettingsManager.On3DIconVisibilityChanged += On3DIconVisibilityChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnWorldIconScaleChanged -= OnWorldIconScaleChanged;
            SettingsManager.OnWorldIconHeightChanged -= On3DIconHeightChanged;
            SettingsManager.On3DIconVisibilityChanged -= On3DIconVisibilityChanged;
        }
        #endregion

        #region METHODS

        /// <summary>
        /// Unloads all POIs from the scene.
        /// </summary>
        internal void Unload()
        {
            foreach (POIEntity poi in _allPOIs)
            {
                Destroy(poi.gameObject);
            }

            _allPOIs.Clear();
            _tileToPOIs.Clear();
        }

        /// <summary>
        /// Unloads only POIs from a specific tile.
        /// </summary>
        internal void UnloadTile(TileDefinition tile)
        {
            (int, int) key = (tile.X, tile.Y);

            if (_tileToPOIs.TryGetValue(key, out List<POIEntity> pois))
            {
                foreach (POIEntity poi in pois)
                {
                    Destroy(poi.gameObject);
                    _allPOIs.Remove(poi);
                }

                _tileToPOIs.Remove(key);
            }
        }

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

                processedKeys.Add(key);

                float altitude = flightData.GetAltitudeAtPosition(tile, gpsData);
                Vector3 gpsVector3 = new Vector3((float)gpsData.Latitude, altitude, (float)gpsData.Longitude);
                Vector3 worldPos = flightData.ConvertGPSPositionToWorld(gpsVector3);

                GameObject poiGO = GameObject.Instantiate(_gpsPrefab, _mainCanvas.transform);
                poiGO.transform.position = worldPos;
                POIEntity poiEntity = poiGO.GetComponent<POIEntity>();
                poiEntity.Inialize(_mainCamera, worldPos, name, SettingsManager.CurrentSettings.WorldIconHeight);
                _allPOIs.Add(poiEntity);
                createdForTile.Add(poiEntity);
            }

            _tileToPOIs[(tile.X, tile.Y)] = createdForTile;
        }
        #endregion

        #region CALLBACKS
        private void OnWorldIconScaleChanged(float value)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.ScaleFactor = value / 100f;
            }
        }

        private void On3DIconVisibilityChanged(bool visibility)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.gameObject.SetActive(visibility);
            }
        }

        private void On3DIconHeightChanged(float height)
        {
            foreach (POIEntity poi in _allPOIs)
            {
                poi.ManualElevation = height;
            }
        }
        #endregion

        #region UI
        internal void DisplayWorldUISettings(FuGrid grid)
        {
            bool icon3DEnabled = SettingsManager.CurrentSettings.Icon3DVisibility;

            if (grid.Toggle("Show 3D Icons", ref icon3DEnabled))
            {
                SettingsManager.Save3DIconVisibility(icon3DEnabled);
            }

            if (!icon3DEnabled)
            {
                grid.DisableNextElements();
            }

            float worldUiScale = SettingsManager.CurrentSettings.WorldIconScale;
            if (grid.Slider("3D Icons", ref worldUiScale, 0.5f, 1f, 0.01f))
            {
                SettingsManager.SaveWorldIconScale(worldUiScale);
            }

            float worldIconHeight = SettingsManager.CurrentSettings.WorldIconHeight;
            if (grid.Slider("3D Icons Height", ref worldIconHeight, 0f, 10f, 0.1f))
            {
                SettingsManager.SaveWorldIconHeight(worldIconHeight);
            }
        }
        #endregion
    }
}
