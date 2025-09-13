using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.PlayerSettings;

namespace FlightReLive.Core.Rendering
{
    public class SunManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Sun position and rotation effect")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private SkyboxPreset _dawnSkybox;
        [SerializeField] private SkyboxPreset _daySkybox;
        [SerializeField] private SkyboxPreset _twilightSkybox;
        [SerializeField] private SkyboxPreset _nightSkybox;
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
            SettingsManager.OnGlobalIntensityChanged += OnGlobalIntensityChanged;
            UnloadFlightRendering();
        }

        private void OnDestroy()
        {
            SettingsManager.OnGlobalIntensityChanged -= OnGlobalIntensityChanged;
        }
        #endregion

        #region METHODS
        internal void LoadFlightRendering(FlightData flightData)
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                DateTime flightDateUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);
                UpdateSkybox(flightDateUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
            });
        }

        internal void UpdateSkybox(DateTime utcTime, double latitude, double longitude)
        {
            SunPosition sunPosition = CalculateSunPosition(utcTime, latitude, longitude);
            SkyPhase phase = GetSkyPhase(sunPosition.Elevation);

            SkyboxPreset preset;
            switch (phase)
            {
                case SkyPhase.Dawn:
                    preset = _dawnSkybox;
                    break;
                default:
                case SkyPhase.Day:
                    preset = _daySkybox;
                    break;
                case SkyPhase.Twilight:
                    preset = _twilightSkybox;
                    break;
                case SkyPhase.Night:
                    preset = _nightSkybox;
                    break;
            }

            _mainCamera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = preset.Material;
            RenderSettings.sun = _mainLight;
            RenderSettings.skybox.SetColor("_Tint", ComputeSkyboxTintRayleigh(sunPosition));
            RenderSettings.skybox.SetFloat("_Rotation", (sunPosition.Azimuth + preset.Offset) % 360f);
            RenderSettings.skybox.SetFloat("_Exposure", ComputeSkyboxExposure(sunPosition.Elevation));
            ComputeFogSettings(sunPosition, out Color fogColor, out float fogDensity);
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.reflectionIntensity = ComputeReflectionIntensity(sunPosition.Elevation);
            LensFlareComponentSRP flare = _mainLight.GetComponent<LensFlareComponentSRP>();
            UpdateLensFlare(sunPosition, flare);
            OrientMainLight(sunPosition);
            ComputeAmbientIntensity(sunPosition.Elevation, fogDensity);
            DynamicGI.UpdateEnvironment();
        }

        internal static SunPosition CalculateSunPosition(DateTime localTime, double latitude, double longitude)
        {
            //Convert UTC to Julian Day
            double julianDay = localTime.ToOADate() + 2415018.5;
            double julianCentury = (julianDay - 2451545.0) / 36525.0;

            //Approximate solar declination and hour angle
            double solarDeclination = 23.44 * Math.Cos((360.0 / 365.25) * (julianDay - 172.0) * Math.PI / 180.0);
            double solarTime = localTime.TimeOfDay.TotalHours + (longitude / 15.0);
            double hourAngle = (solarTime - 12.0) * 15.0;

            //Convert to radians
            double latRad = latitude * Math.PI / 180.0;
            double declRad = solarDeclination * Math.PI / 180.0;
            double haRad = hourAngle * Math.PI / 180.0;

            //Elevation angle
            double elevationRad = Math.Asin(Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad));
            float elevation = (float)(elevationRad * 180.0 / Math.PI);

            //Azimuth angle (astronomical convention: from north, clockwise)
            double azimuthRad = Math.Atan2(-Math.Sin(haRad), Math.Tan(declRad) * Math.Cos(latRad) - Math.Sin(latRad) * Math.Cos(haRad));
            double azimuthDeg = azimuthRad * 180.0 / Math.PI;
            azimuthDeg = (azimuthDeg + 360.0) % 360.0;

            //Convert azimuth to Unity-compatible rotation (from north, clockwise → Unity Y rotation)
            float unityAzimuth = (float)((360.0 - azimuthDeg) % 360.0);

            return new SunPosition
            {
                Elevation = elevation,
                Azimuth = unityAzimuth,
                AzimuthPhysical = (float)azimuthDeg
            };
        }

        internal static SkyPhase GetSkyPhase(float elevation)
        {
            if (elevation > 10f)
            {
                return SkyPhase.Day;
            }

            if (elevation > 0f)
            {
                return SkyPhase.Twilight;
            }

            if (elevation > -6f)
            {
                return SkyPhase.Dawn;
            }

            return SkyPhase.Night;
        }
        private void OrientMainLight(SunPosition sunPosition)
        {
            //Convert azimuth (astronomical) and elevation to a directional vector
            float azimuthRad = Mathf.Deg2Rad * sunPosition.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sunPosition.Elevation;

            //Spherical to Cartesian conversion
            Vector3 sunDirection = new Vector3(
                Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad),
                Mathf.Sin(elevationRad),
                Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad)
            );

            //Orient the directional light to point in the opposite direction (light shines *from* the sun)
            _mainLight.transform.rotation = Quaternion.LookRotation(-sunDirection, Vector3.up);

            //Adjust intensity and color
            _mainLight.intensity = ComputeSunIntensity(sunPosition.Elevation) * SettingsManager.CurrentSettings.GlobalIntensity;
            _mainLight.color = ComputeSkyboxTintRayleigh(sunPosition);
        }

        private void UpdateLensFlare(SunPosition sunPosition, LensFlareComponentSRP flare)
        {
            if (flare == null)
            {
                return;
            }

            if (sunPosition.Elevation <= 0f)
            {
                flare.intensity = 0f;
                return;
            }

            float elevation = Mathf.Clamp(sunPosition.Elevation, 0f, 90f);
            float t = elevation / 90f;
            flare.intensity = Mathf.Clamp(Mathf.Pow(t, 0.6f) * 1.5f + 0.3f, 0.3f, 2f);
        }

        private float ComputeSkyboxExposure(double solarElevation)
        {
            float elevation = Mathf.Clamp((float)solarElevation, -6f, 90f);
            float t = (elevation + 6f) / 96f;
            float low = Mathf.Pow(t, 0.6f) * 1.4f;
            float high = Mathf.Pow(t, 2f) * 0.6f;

            return Mathf.Clamp(low + high, 0.4f, 1.2f) * SettingsManager.CurrentSettings.GlobalIntensity;
        }

        private float ComputeSunIntensity(double solarElevation)
        {
            float elevation = Mathf.Clamp((float)solarElevation, 0f, 90f);
            float t = elevation / 90f;
            float intensity = Mathf.Pow(t, 0.8f) * (1f - 0.3f * t) * 1.0f;
            return Mathf.Clamp(intensity, 0.3f, 1.0f);
        }

        private Color ComputeSkyboxTintRayleigh(SunPosition sunPosition)
        {
            float elevation = Mathf.Clamp(sunPosition.Elevation, 0.1f, 90f);
            float normalizedElevation = Mathf.Clamp01(elevation / 90f);
            float elevationFactor = 1f - normalizedElevation;

            float airMass = 1f / (Mathf.Cos(Mathf.Deg2Rad * elevation) + 0.50572f * Mathf.Pow(96.07995f - elevation, -1.6364f));
            float scatteringFactor = Mathf.Clamp01((airMass - 1f) / 39f);

            float azimuth = sunPosition.AzimuthPhysical;
            float sunriseBoost = Mathf.Clamp01((azimuth - 60f) / 60f);
            float sunsetBoost = Mathf.Clamp01((azimuth - 240f) / 60f);
            float directionalWarmth = Mathf.Max(sunriseBoost, sunsetBoost);

            float adjustedWarmth = directionalWarmth * elevationFactor;
            float warmthFactor = Mathf.Clamp01(scatteringFactor * 0.6f + adjustedWarmth * 0.4f);

            Color zenithColor = new Color(0.85f, 0.9f, 1f);
            Color horizonColor = new Color(1f, 0.5f, 0.2f);

            Color tint = new Color(
                Mathf.Clamp01(zenithColor.r + (horizonColor.r - zenithColor.r) * warmthFactor),
                Mathf.Clamp01(zenithColor.g + (horizonColor.g - zenithColor.g) * warmthFactor),
                Mathf.Clamp01(zenithColor.b + (horizonColor.b - zenithColor.b) * warmthFactor)
            );

            float saturationBoost = 1f + 0.6f * warmthFactor * elevationFactor;
            saturationBoost = Mathf.Lerp(saturationBoost, 1f, normalizedElevation * 0.8f);
            tint.r = Mathf.Clamp01(tint.r * saturationBoost);
            tint.g = Mathf.Clamp01(tint.g * saturationBoost);
            tint.b = Mathf.Clamp01(tint.b * saturationBoost);

            float desaturation = normalizedElevation * 0.3f;
            float gray = (tint.r + tint.g + tint.b) / 3f;
            tint.r = Mathf.Lerp(tint.r, gray, desaturation);
            tint.g = Mathf.Lerp(tint.g, gray, desaturation);
            tint.b = Mathf.Lerp(tint.b, gray, desaturation);

            if (elevationFactor > 0.6f)
            {
                tint.r = Mathf.Clamp01(tint.r * 1.2f);
                tint.g = Mathf.Clamp01(tint.g * 1.1f);
                tint.b = Mathf.Clamp01(tint.b * 1.05f);
            }

            return tint;
        }

        private float ComputeReflectionIntensity(double solarElevation)
        {
            float elevation = Mathf.Clamp((float)solarElevation, 0f, 90f);
            float t = elevation / 90f;

            return Mathf.Clamp(Mathf.Pow(t, 1.2f) * 0.8f, 0.3f, 0.8f) * SettingsManager.CurrentSettings.GlobalIntensity;
        }
        private float ComputeAmbientIntensity(double solarElevation, float fogDensity = 0f)
        {
            float elevation = Mathf.Clamp((float)solarElevation, 0f, 90f);
            float t = elevation / 90f;
            float baseIntensity = Mathf.Pow(t, 0.9f) * 1.0f;
            float fogFactor = Mathf.Clamp01(1f - fogDensity * 12f);
            float ambientClamp = Mathf.Lerp(1.0f, 0.6f, t);

            return Mathf.Clamp(baseIntensity * fogFactor, 0.3f, ambientClamp);
        }

        private void ComputeFogSettings(SunPosition sunPosition, out Color fogColor, out float fogDensity)
        {
            fogColor = ComputeSkyboxTintRayleigh(sunPosition);
            SkyPhase phase = GetSkyPhase(sunPosition.Elevation);

            switch (phase)
            {
                case SkyPhase.Day:
                    fogDensity = 0.0005f;
                    break;
                case SkyPhase.Twilight:
                    fogDensity = 0.0012f;
                    break;
                case SkyPhase.Dawn:
                    fogDensity = 0.0018f;
                    break;
                case SkyPhase.Night:
                    fogDensity = 0.0025f;
                    break;
                default:
                    fogDensity = 0.001f;
                    break;
            }

            float t = Mathf.Clamp01(sunPosition.Elevation / 90f);
            fogDensity *= Mathf.Lerp(1f, 0.4f, t);
        }

        internal void UnloadFlightRendering()
        {
            if (_mainCamera == null)
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                _mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                DynamicGI.UpdateEnvironment();
            });
        }
        #endregion

        #region CALLBACKS
        private void OnGlobalIntensityChanged(float globalIntensity)
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                FlightData flightData = LoadingManager.Instance.CurrentFlightData;

                if (flightData == null)
                {
                    return;
                }

                TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                DateTime flightDateUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);
                UpdateSkybox(flightDateUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
            });
        }
        #endregion

        #region UI
        internal void DisplaySunSettings(FuGrid gridLight)
        {
            float globalIntensity = SettingsManager.CurrentSettings.GlobalIntensity;
            if (gridLight.Slider("GLobal intensity", ref globalIntensity, 0.6f, 1f, 0.01f))
            {
                SettingsManager.SaveGlobalIntensity(globalIntensity);
            }
        }
        #endregion
    }
}
