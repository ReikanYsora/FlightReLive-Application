using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static FlightReLive.Core.Rendering.SPACalculator;

namespace FlightReLive.Core.Rendering
{
    public class SunManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Sun position and rotation effect")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private List<SkyboxProfile> _skyboxProfiles;
        private SkyboxProfile _currentSkybox;
        private Material _skyboxMaterial;
        #endregion

        #region PROPERTIES
        public static SunManager Instance { get; private set; }
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
            UnloadFlightRendering();
        }
        #endregion

        #region METHODS
        internal void LoadFlightRendering(FlightData flightData)
        {
            SPAData spa = new SPAData();

            spa.Year = flightData.Date.Year;
            spa.Month = flightData.Date.Month;
            spa.Day = flightData.Date.Day;
            spa.Hour = flightData.Date.Hour;
            spa.Minute = flightData.Date.Minute;
            spa.Second = flightData.Date.Second;

            DateTime flightDate = flightData.Date;
            TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;

            TimeSpan utcOffset = userTimeZone.GetUtcOffset(flightDate);
            spa.Timezone = utcOffset.TotalHours;

            spa.DeltaUt1 = 0;
            spa.DeltaT = 69;
            spa.Longitude = flightData.GPSOrigin.Longitude;
            spa.Latitude = flightData.GPSOrigin.Latitude;

            spa.Elevation = TerrainManager.Instance.GetAltitudeAtPosition(flightData, new FlightGPSData
            {
                Latitude = flightData.GPSOrigin.Latitude,
                Longitude = flightData.GPSOrigin.Longitude
            });

            spa.Function = CalculationMode.SPA_ALL;

            int result = SPACalculate(ref spa);
            Vector3 sunDirection = GetSunDirection((float)spa.Azimuth, (float)spa.E);
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
            UpdateSkyboxForHour(flightData.Date.Hour);
            AnimateSunScene(sunDirection, flightData.Date.Hour, spa.E);
        }

        private void UpdateSkyboxForHour(int hour)
        {
            SkyboxProfile selected = _skyboxProfiles
                .Where(p => p.skyboxMaterial != null && hour >= p.startHour)
                .OrderByDescending(p => p.startHour)
                .FirstOrDefault();

            if (selected != null && selected != _currentSkybox)
            {
                _skyboxMaterial = new Material(selected.skyboxMaterial);
                RenderSettings.skybox = _skyboxMaterial;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.reflectionIntensity = 1f;
                DynamicGI.UpdateEnvironment();
                _currentSkybox = selected;
            }
        }

        private void AnimateSunScene(Vector3 sunDir, int hour, double elevation)
        {
            // Safety check for null references
            if (_mainLight == null || _mainCamera == null || RenderSettings.skybox == null)
            {
                return;
            }

            // Rotate the sun directly
            if (_mainLight.transform != null)
            {
                _mainLight.transform.rotation = Quaternion.LookRotation(-sunDir);

                // Update skybox rotation
                float currentYaw = _mainLight.transform.eulerAngles.y;
                RenderSettings.skybox.SetFloat("_Rotation", currentYaw);
            }

            //Set sun color
            Color targetColor = GetInterpolatedSunColor(hour);
            _mainLight.color = targetColor;

            //Sun intensity
            float targetIntensity = Mathf.Lerp(2.0f, 4.0f, Mathf.InverseLerp(0f, 90f, (float)elevation));
            _mainLight.intensity = targetIntensity;

            //Ambient light
            Color ambientBase = new Color(0.5f, 0.5f, 0.55f);          // Base daylight
            Color ambientScattering = new Color(1f, 0.8f, 0.7f);       // Sunrise/sunset
            Color ambientTarget = elevation < 10f
                ? Color.Lerp(ambientScattering, ambientBase, Mathf.InverseLerp(0f, 10f, (float)elevation))
                : Color.Lerp(ambientBase, targetColor, Mathf.InverseLerp(10f, 90f, (float)elevation));
            RenderSettings.ambientLight = ambientTarget;

            // Apply ambient light
            RenderSettings.ambientLight = ambientTarget;

            // Apply skybox tint
            RenderSettings.skybox.SetColor("_Tint", ambientTarget);

            // Apply fog settings
            RenderSettings.fogColor = ambientTarget;
            RenderSettings.fogDensity = Mathf.Lerp(0.005f, 0.0001f, Mathf.InverseLerp(0f, 90f, (float)elevation));

            // Apply lens flare if present
            LensFlareComponentSRP flare = _mainLight.GetComponent<LensFlareComponentSRP>();
            if (flare != null)
            {
                flare.intensity = Mathf.Lerp(0.2f, 1.0f, Mathf.InverseLerp(10f, 90f, (float)elevation));
            }
        }

        private Vector3 GetSunDirection(float azimuthDeg, float altitudeDeg)
        {
            float azimuthRad = Mathf.Deg2Rad * azimuthDeg;
            float altitudeRad = Mathf.Deg2Rad * altitudeDeg;

            float x = Mathf.Cos(altitudeRad) * Mathf.Sin(azimuthRad);
            float y = Mathf.Sin(altitudeRad);
            float z = Mathf.Cos(altitudeRad) * Mathf.Cos(azimuthRad);

            return new Vector3(x, y, z);
        }

        private Color GetInterpolatedSunColor(int hour)
        {
            SkyboxProfile selected = _skyboxProfiles
                .Where(p => p.skyboxMaterial != null)
                .OrderByDescending(p => p.startHour)
                .FirstOrDefault(p => hour >= p.startHour);

            if (selected != null)
            {
                return selected.sunColor;
            }

            return Color.white;
        }

        internal void UnloadFlightRendering()
        {
            if (_mainCamera != null)
            {
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                _mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            if (_mainLight != null)
            {
                LensFlareComponentSRP flare = _mainLight.GetComponent<LensFlareComponentSRP>();

                if (flare != null)
                {
                    flare.intensity = 0f;
                }
            }
        }
        #endregion
    }
}
