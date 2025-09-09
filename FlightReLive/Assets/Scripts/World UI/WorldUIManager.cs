using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.WorldUI
{
    public class WorldUIManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Camera _mainCamera;

        [Header("3D Icons prefabs")]
        [SerializeField] private GameObject _gpsPrefab;
        private List<POIEntity> _pois;

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
            _pois = new List<POIEntity>();
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
        internal void UnloadFlightPOIs()
        {
            foreach (POIEntity poi in _pois)
            {
                Destroy(poi.gameObject);
            }

            _pois.Clear();
        }
        #endregion

        #region CALLBACKS
        private void OnWorldIconScaleChanged(float value)
        {
            if (_pois != null &&  _pois.Count > 0)
            {
                _pois.ForEach(p => p.GetComponent<POIEntity>().ScaleFactor = value / 100f);
            }
        }

        private void On3DIconVisibilityChanged(bool visibility)
        {
            if (_pois != null && _pois.Count > 0)
            {
                _pois.ForEach(p => p.gameObject.SetActive(visibility));
            }
        }

        private void On3DIconHeightChanged(float height)
        {
            if (_pois != null && _pois.Count > 0)
            {
                _pois.ForEach(p => p.ManualElevation = height);
            }
        }

        internal void LoadFlightPOIs(FlightData flightData)
        {
            if (flightData != null)
            {
                ExtractLocation(flightData);
            }
        }

        private void ExtractLocation(FlightData flightData)
        {
            HashSet<string> processedKeys = new HashSet<string>();

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                if (tile.GeoData == null && tile.GeoData.features == null)
                {
                    return;
                }

                foreach (Feature feature in tile.GeoData.features)
                {
                    string name = feature.place_name ?? feature.text ?? "Nom inconnu";

                    if (feature.geometry?.coordinates != null && feature.geometry.coordinates.Count >= 2)
                    {
                        FlightGPSData flightGPSData = new FlightGPSData
                        {
                            Longitude = feature.geometry.coordinates[0],
                            Latitude = feature.geometry.coordinates[1]
                        };

                        //Check if GPS coordinate are in globalBoundingBox
                        GPSBoundingBox bbox = flightData.MapDefinition.MapBoundingBox;
                        bool isInsideBoundingBox =
                            flightGPSData.Latitude >= bbox.MinLatitude &&
                            flightGPSData.Latitude <= bbox.MaxLatitude &&
                            flightGPSData.Longitude >= bbox.MinLongitude &&
                            flightGPSData.Longitude <= bbox.MaxLongitude;

                        string key = $"{name}_{flightGPSData.Latitude}_{flightGPSData.Longitude}";

                        if (!isInsideBoundingBox || processedKeys.Contains(key))
                        {
                            continue;
                        }

                        processedKeys.Add(key);

                        float altitude = TerrainManager.Instance.GetAltitudeAtPosition(flightData, flightGPSData);
                        Vector3 gpsVector3 = new Vector3((float)flightGPSData.Latitude, altitude, (float)flightGPSData.Longitude);
                        Vector3 tempWorldPosition = TerrainManager.Instance.ConvertGPSPositionToWorld(flightData, gpsVector3);
                        GameObject _tempPOI = GameObject.Instantiate(_gpsPrefab, _mainCanvas.transform);
                        _tempPOI.transform.position = tempWorldPosition;
                        POIEntity poiEntityComponent = _tempPOI.GetComponent<POIEntity>();
                        poiEntityComponent.Inialize(POIType.Text, _mainCamera, tempWorldPosition, name, SettingsManager.CurrentSettings.WorldIconHeight);
                        _pois.Add(poiEntityComponent);
                    }
                }
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
