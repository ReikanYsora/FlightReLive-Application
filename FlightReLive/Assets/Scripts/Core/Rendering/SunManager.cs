using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace FlightReLive.Core.Rendering
{
    public class SunManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Scene refs")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Camera _mainCamera;

        [Header("Volumes (Global)")]
        private Volume _globalVolume;
        private VolumeProfile _profile;

        // HDRP volume overrides
        private VisualEnvironment _visualEnvironment;
        private PhysicallyBasedSky _sky;
        private Fog _fog;
        private VolumetricClouds _clouds;
        #endregion

        #region PROPERTIES
        internal static SunManager Instance { get; private set; }
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

            //Create a global volume if none is provided
            if (_globalVolume == null)
            {
                GameObject go = new GameObject("Global Sky & Fog Volume (HDRP)");
                go.layer = 0;
                _globalVolume = go.AddComponent<Volume>();
                _globalVolume.isGlobal = true;
                _globalVolume.priority = 999f;
                _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _globalVolume.sharedProfile = _profile;
            }
            else
            {
                _profile = _globalVolume.sharedProfile ?? _globalVolume.profile;
                if (_profile == null)
                {
                    _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    _globalVolume.sharedProfile = _profile;
                }
            }

            //Ensure overrides exist
            if (!_profile.TryGet(out _visualEnvironment))
            {
                _visualEnvironment = _profile.Add<VisualEnvironment>(true);
            }

            if (!_profile.TryGet(out _sky))
            {
                _sky = _profile.Add<PhysicallyBasedSky>(true);
            }

            if (!_profile.TryGet(out _fog))
            {
                _fog = _profile.Add<Fog>(true);
            }

            if (!_profile.TryGet(out _clouds))
            {
                _clouds = _profile.Add<VolumetricClouds>(false);
            }
        }

        private void Start()
        {
            SettingsManager.OnGlobalIntensityChanged += OnGlobalIntensityChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnGlobalIntensityChanged -= OnGlobalIntensityChanged;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Configure and show HDRP sky/fog/clouds, orient the sun, and ensure camera renders the sky.
        /// </summary>
        internal void LoadFlightRendering(FlightData flightData)
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                if (_mainCamera != null)
                {
                    _mainCamera.clearFlags = CameraClearFlags.Skybox;
                }

                if (_visualEnvironment != null)
                {
                    _visualEnvironment.skyType.Override((int)SkyType.PhysicallyBased);
                }

                if (_sky != null)
                {
                    _sky.active = true;
                    _sky.exposure.Override(0f);
                    _sky.multiplier.Override(1f);
                }

                if (_fog != null)
                {
                    _fog.active = true;
                    _fog.enableVolumetricFog.Override(true);
                    _fog.meanFreePath.Override(15000f);
                    _fog.baseHeight.Override(0f);
                    _fog.maximumHeight.Override(2000f);
                }

                if (_clouds != null)
                {
                    _clouds.active = true;
                    _clouds.enable.Override(true);
                    _clouds.bottomAltitude.Override(1500f);
                    _clouds.altitudeRange.Override(1200f);
                    _clouds.densityMultiplier.Override(0.25f);
                    _clouds.sunLightDimmer.Override(1.0f);
                }

                if (flightData != null)
                {
                    TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                    DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                    DateTime flightDateUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);
                    UpdateAtmosphere(flightDateUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
                }
            });
        }

        /// <summary>
        /// Reset to a flat dark background, remove sky/fog/clouds until next Load.
        /// </summary>
        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                if (_mainCamera != null)
                {
                    _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    _mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
                }

                if (_visualEnvironment != null)
                {
                    _visualEnvironment.skyType.Override((int)SkyType.Gradient);
                }

                if (_sky != null)
                {
                    _sky.active = false;
                }

                if (_fog != null)
                {
                    _fog.active = false;
                }

                if (_clouds != null)
                {
                    _clouds.active = false;
                }

                RenderSettings.sun = null;
            });
        }


        /// <summary>
        /// Orient the sun and update overrides.
        /// </summary>
        public void UpdateAtmosphere(DateTime utcTime, double latitude, double longitude)
        {
            SunPosition sun = CalculateSunPosition(utcTime, latitude, longitude);
            OrientMainLight(sun);
        }

        public struct SunPosition
        {
            public float Elevation;
            public float Azimuth;
            public float AzimuthPhysical;
        }

        public static SunPosition CalculateSunPosition(DateTime utcTime, double latitude, double longitude)
        {
            double julianDay = utcTime.ToOADate() + 2415018.5;
            double solarDeclination = 23.44 * Math.Cos((360.0 / 365.25) * (julianDay - 172.0) * Math.PI / 180.0);
            double solarTime = utcTime.TimeOfDay.TotalHours + (longitude / 15.0);
            double hourAngle = (solarTime - 12.0) * 15.0;

            double latRad = latitude * Math.PI / 180.0;
            double declRad = solarDeclination * Math.PI / 180.0;
            double haRad = hourAngle * Math.PI / 180.0;

            double elevationRad = Math.Asin(Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad));
            float elevation = (float)(elevationRad * 180.0 / Math.PI);

            double azimuthRad = Math.Atan2(-Math.Sin(haRad), Math.Tan(declRad) * Math.Cos(latRad) - Math.Sin(latRad) * Math.Cos(haRad));
            double azimuthDeg = (azimuthRad * 180.0 / Math.PI + 360.0) % 360.0;
            float unityAzimuth = (float)((360.0 - azimuthDeg) % 360.0);

            return new SunPosition { Elevation = elevation, Azimuth = unityAzimuth, AzimuthPhysical = (float)azimuthDeg };
        }

        private void OrientMainLight(SunPosition sun)
        {
            if (_mainLight == null)
            {
                return;
            }

            float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sun.Elevation;
            Vector3 dir = new Vector3(
                Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad),
                Mathf.Sin(elevationRad),
                Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad)
            );
            _mainLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            float t = Mathf.Clamp01(Mathf.InverseLerp(0f, 35f, sun.Elevation));
            _mainLight.intensity = SettingsManager.CurrentSettings.GlobalIntensity * 100000f;

            RenderSettings.sun = _mainLight;
        }
        #endregion

        #region CALLBACKS
        private void OnGlobalIntensityChanged(float globalIntensity)
        {
            if (_mainLight == null)
            {
                return;
            }

            _mainLight.intensity = SettingsManager.CurrentSettings.GlobalIntensity * 10000f;
        }
        #endregion

        #region UI
        internal void DisplaySunSettings(FuGrid gridLight)
        {
            float globalIntensity = SettingsManager.CurrentSettings.GlobalIntensity;

            if (gridLight.Slider("Global intensity", ref globalIntensity, 0.6f, 1f, 0.01f))
            {
                SettingsManager.SaveGlobalIntensity(globalIntensity);
            }
        }
        #endregion
    }
}
