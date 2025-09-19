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

        //HDRP volume overrides
        private Volume _globalVolume;
        private VolumeProfile _globalVolumeProfile;
        private GameObject _volumeInstance;
        private Exposure _exposure;
        private VisualEnvironment _visualEnvironment;
        private PhysicallyBasedSky _sky;
        private Fog _fog;
        private VolumetricClouds _clouds;
        private DepthOfField _dof;
        private ContactShadows _contactShadows;
        private ColorAdjustments _colorAdjustments;
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

            if (_mainLight != null)
            {
                HDAdditionalLightData hdLight = _mainLight.GetComponent<HDAdditionalLightData>();
                if (hdLight != null)
                {
                    hdLight.volumetricDimmer = 1.0f;
                }
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
                if (flightData != null)
                {
                    TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                    DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                    DateTime flightDateUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);
                    ConfigureSceneRendering(flightDateUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
                }
            });
        }

        private void UninitializedVolumeProfile()
        {
            if (_mainCamera != null)
            {
                HDAdditionalCameraData hdCam = _mainCamera.GetComponent<HDAdditionalCameraData>();
                hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                _mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            if (_globalVolume != null)
            {
                _globalVolume.enabled = false;

                if (_volumeInstance != null)
                {
                    Destroy(_volumeInstance);
                    _volumeInstance = null;
                }

                _globalVolume = null;
            }

            if (_globalVolumeProfile != null)
            {
                Destroy(_globalVolumeProfile);
                _globalVolumeProfile = null;
            }

            _visualEnvironment = null;
            _sky = null;
            _fog = null;
            _clouds = null;
            _dof = null;
            _contactShadows = null;
            _colorAdjustments = null;
            RenderSettings.sun = null;
        }

        /// <summary>
        /// Reset to a flat dark background, remove sky/fog/clouds until next Load.
        /// </summary>
        internal void Unload()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                UninitializedVolumeProfile();
            });
        }

        /// <summary>
        /// Configure scene rendering (sun, volume profile)
        /// </summary>
        public void ConfigureSceneRendering(DateTime utcTime, double latitude, double longitude)
        {
            SunPosition sun = CalculateSunPosition(utcTime, latitude, longitude);
            CreateVolumeProfile(utcTime, sun);
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

        private void CreateVolumeProfile(DateTime utcTime, SunPosition sun)
        {
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, SettingsManager.CurrentSettings.UserTimeZone);
            int hour = localTime.Hour;

            if (_globalVolume == null)
            {
                _volumeInstance = new GameObject("Global Sky & Fog Volume (HDRP)");
                _volumeInstance.transform.SetParent(transform);
                _volumeInstance.layer = 0;
                _globalVolume = _volumeInstance.AddComponent<Volume>();
                _globalVolume.isGlobal = true;
                _globalVolume.priority = 0f;
                _globalVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                _globalVolume.sharedProfile = _globalVolumeProfile;
                _exposure = _globalVolumeProfile.Add<Exposure>(true);
                _visualEnvironment = _globalVolumeProfile.Add<VisualEnvironment>(true);
                _sky = _globalVolumeProfile.Add<PhysicallyBasedSky>(true);
                _fog = _globalVolumeProfile.Add<Fog>(true);
                _clouds = _globalVolumeProfile.Add<VolumetricClouds>(true);
                _dof = _globalVolumeProfile.Add<DepthOfField>(true);
                _contactShadows = _globalVolumeProfile.Add<ContactShadows>(true);
                _colorAdjustments = _globalVolumeProfile.Add<ColorAdjustments>(true);
            }

            if (_mainCamera != null)
            {
                HDAdditionalCameraData hdCam = _mainCamera.GetComponent<HDAdditionalCameraData>();
                hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
            }

            if (_exposure != null)
            {
                _exposure.mode.overrideState = true;
                _exposure.mode.value = ExposureMode.Automatic;
            }

            if (_visualEnvironment != null)
            {
                //Wind speed - stronger in evening/night
                float windSpeed = hour >= 18 || hour < 6 ? 25f : 8f;

                _visualEnvironment.skyType.Override((int)SkyType.PhysicallyBased);
                _visualEnvironment.windOrientation.Override(29);
                _visualEnvironment.windSpeed.Override(windSpeed);
                _visualEnvironment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            }

            if (_sky != null)
            {
                _sky.active = true;
                _sky.type.Override(PhysicallyBasedSkyModel.EarthAdvanced);
                _sky.atmosphericScattering.Override(true);
                _sky.renderingMode.Override(PhysicallyBasedSky.RenderingMode.Default);
                _sky.planetRotation.Override(Vector3.zero);
                _sky.aerosolDensity.Override(0.012f);
                _sky.aerosolTint.Override(Color.white);
                _sky.aerosolAnisotropy.Override(0.8f);
                _sky.aerosolMaximumAltitude.Override(8000f);
                _sky.ozoneDensityDimmer.Override(1f);
                _sky.colorSaturation.Override(1f);
                _sky.alphaSaturation.Override(1f);
                _sky.alphaMultiplier.Override(1f);
                _sky.horizonTint.Override(Color.white);
                _sky.horizonZenithShift.Override(0f);
                _sky.updateMode.Override(EnvironmentUpdateMode.Realtime);
                _sky.updatePeriod.Override(0f);
            }

            if (_fog != null)
            {
                //Fog tint
                Color fogTint;
                //Morning
                if (hour >= 6 && hour < 10)
                {
                    fogTint = new Color(1.0f, 0.9f, 0.6f);
                }
                //Day
                else if (hour >= 10 && hour < 18)
                {
                    fogTint = new Color(0.8f, 0.8f, 0.8f);
                }
                //Evening
                else if (hour >= 18 && hour < 22)
                {
                    fogTint = new Color(1.0f, 0.6f, 0.4f);
                }
                //Night
                else
                {
                    fogTint = new Color(0.5f, 0.6f, 0.8f);
                }

                float horizonFog = Mathf.Clamp01(Mathf.InverseLerp(45f, 0f, sun.Elevation));

                _fog.active = true;
                _fog.enableVolumetricFog.Override(true);
                _fog.meanFreePath.Override(5000f);
                _fog.baseHeight.Override(0f);
                _fog.maximumHeight.Override(500f);
                _fog.maxFogDistance.Override(5000f);
                _fog.colorMode.Override(FogColorMode.SkyColor);
                _fog.tint.Override(fogTint);
                _fog.enableVolumetricFog.Override(true);
                _fog.albedo.Override(fogTint);
                _fog.globalLightProbeDimmer.Override(1f);
                _fog.volumetricFogBudget = 64f;
                _fog.denoisingMode.Override(FogDenoisingMode.Gaussian);
            }

            if (_clouds != null)
            {
                //Scattering tint
                Color scatterTint;
                //Morning
                if (hour >= 6 && hour < 10)
                {
                    scatterTint = new Color(1.0f, 0.85f, 0.6f);
                }
                //Day
                else if (hour >= 10 && hour < 18)
                {
                    scatterTint = new Color(1f, 1f, 1f);
                }
                //Evening
                else if (hour >= 18 && hour < 22)
                {
                    scatterTint = new Color(1.0f, 0.6f, 0.4f);
                }
                //Night
                else
                {
                    scatterTint = new Color(0.6f, 0.7f, 1.0f);
                }

                _clouds.active = true;
                _clouds.enable.overrideState = true;
                _clouds.enable.value = true;
                _clouds.enable.Override(true);
                _clouds.cloudControl.Override(VolumetricClouds.CloudControl.Simple);
                _clouds.cloudSimpleMode.Override(VolumetricClouds.CloudSimpleMode.Performance);
                _clouds.cloudPreset = VolumetricClouds.CloudPresets.Sparse;
                _clouds.shapeFactor.Override(0.95f);
                _clouds.shapeScale.Override(5f);
                _clouds.erosionScale.Override(107f);
                _clouds.bottomAltitude.Override(3000f);
                _clouds.altitudeRange.Override(1000f);
                _clouds.ambientLightProbeDimmer.Override(1f);
                _clouds.sunLightDimmer.Override(1f);
                _clouds.scatteringTint.Override(scatterTint);
            }

            if (_dof != null)
            {
                _dof.focusMode.Override(DepthOfFieldMode.Manual);
                _dof.nearFocusStart.overrideState = false;
                _dof.nearFocusEnd.overrideState = false;
                _dof.farFocusStart.Override(500f);
                _dof.farFocusEnd.Override(5000f);
            }

            if (_contactShadows != null)
            {
                _contactShadows.enable.overrideState = true;
                _contactShadows.active = true;
                _contactShadows.enable.Override(true);
                _contactShadows.length.Override(0.2f);
                _contactShadows.distanceScaleFactor.Override(0.65f);
                _contactShadows.minDistance.Override(0f);
                _contactShadows.maxDistance.Override(1500f);
                _contactShadows.opacity.Override(0.9f);
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.contrast.Override(40f);
                _colorAdjustments.saturation.Override(1.1f);
            }

            _globalVolume.enabled = true;

            if (_mainLight != null)
            {
                RenderSettings.sun = _mainLight;
            }
        }

        private void OrientMainLight(SunPosition sun)
        {
            if (_mainLight == null)
            {
                return;
            }

            float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sun.Elevation;
            Vector3 dir = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad));
            _mainLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
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
