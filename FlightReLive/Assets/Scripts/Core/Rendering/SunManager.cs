using DG.Tweening;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
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
        [SerializeField] private float _sunSpeedEffect = 3f;
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
            TimeZoneInfo tz = SettingsManager.CurrentSettings.UserTimeZone;
            bool isDst = tz.IsDaylightSavingTime(flightDate);
            double timezoneOffset = tz.BaseUtcOffset.TotalHours + (isDst ? 1.0 : 0.0);
            spa.Timezone = timezoneOffset;

            spa.DeltaUt1 = 0;
            spa.DeltaT = 69;
            spa.Longitude = flightData.GPSOrigin.Longitude;
            spa.Latitude = flightData.GPSOrigin.Latitude;
            spa.Elevation = flightData.Points.Min(x => x.Satellites);
            spa.Function = CalculationMode.SPA_ALL;

            int result = SPACalculate(ref spa);
            Vector3 sunDirection = GetSunDirection((float)spa.Azimuth, (float)spa.E);
            _mainCamera.clearFlags = CameraClearFlags.Skybox;
            UpdateSkyboxForHour(flightData.Date.Hour);
            AnimateSunScene(sunDirection, flightData.Date.Hour, spa.E, _sunSpeedEffect);
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

        private void AnimateSunScene(Vector3 sunDir, int hour, double elevation, float duration)
        {
            Sequence sunSequence = DOTween.Sequence();

            sunSequence.Join(_mainLight.transform
                .DORotateQuaternion(Quaternion.LookRotation(-sunDir), duration)
                .SetEase(Ease.InOutSine)
                .OnUpdate(() =>
                {
                    float currentYaw = _mainLight.transform.eulerAngles.y;
                    RenderSettings.skybox.SetFloat("_Rotation", currentYaw);
                }));

            Color targetColor = GetInterpolatedSunColor(hour);
            sunSequence.Join(_mainLight
                .DOColor(targetColor, duration)
                .SetEase(Ease.InOutSine));

            float targetIntensity = Mathf.Lerp(0.6f, 1.2f, Mathf.InverseLerp(0f, 90f, (float)elevation));
            sunSequence.Join(DOTween.To(
                () => _mainLight.intensity,
                x => _mainLight.intensity = x,
                targetIntensity,
                duration
            ).SetEase(Ease.InOutSine));

            Color ambientBase = new Color(0.2f, 0.2f, 0.3f);
            Color ambientScattering = new Color(0.4f, 0.2f, 0.5f);
            Color ambientTarget;

            if (elevation < 10f)
            {
                float t = Mathf.InverseLerp(0f, 10f, (float)elevation);
                ambientTarget = Color.Lerp(ambientScattering, ambientBase, t);
            }
            else
            {
                ambientTarget = Color.Lerp(ambientBase, targetColor, Mathf.InverseLerp(10f, 90f, (float)elevation));
            }

            sunSequence.Join(DOTween.To(
                () => RenderSettings.ambientLight,
                x => RenderSettings.ambientLight = x,
                ambientTarget,
                duration
            ).SetEase(Ease.InOutSine));

            sunSequence.Join(DOTween.To(
                () => RenderSettings.skybox.GetColor("_Tint"),
                x => RenderSettings.skybox.SetColor("_Tint", x),
                ambientTarget,
                duration
            ).SetEase(Ease.InOutSine));

            RenderSettings.fogColor = ambientTarget;
            RenderSettings.fogDensity = Mathf.Lerp(0.01f, 0.0005f, Mathf.InverseLerp(0f, 90f, (float)elevation));

            LensFlareComponentSRP flare = _mainLight.GetComponent<LensFlareComponentSRP>();
            if (flare != null)
            {
                flare.intensity = Mathf.Lerp(0.2f, 1.0f, Mathf.InverseLerp(10f, 90f, (float)elevation));
            }

            sunSequence.Play();
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
