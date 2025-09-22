using FlightReLive.Core.Building;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.POI;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace FlightReLive.Core.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float BASE_LIGHT_LUX = 10000f;
        #endregion

        #region ATTRIBUTES
        [Header("Light & Camera")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Camera _reliveCamera;
        [SerializeField] private Camera _povCamera;

        //HDRP volume overrides
        private Volume _globalVolume;
        private VolumeProfile _globalVolumeProfile;
        private GameObject _volumeInstance;
        private Exposure _exposure;
        private VisualEnvironment _visualEnvironment;
        private PhysicallyBasedSky _sky;
        private Fog _fog;
        private VolumetricClouds _clouds;
        private ContactShadows _contactShadows;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;
        #endregion

        #region PROPERTIES
        internal static EnvironmentManager Instance { get; private set; }
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
            SettingsManager.OnSunIntensityChanged += OnGlobalIntensityChanged;
            SettingsManager.OnVignettingIntensityChanged += OnVignettingIntensityChanged;
            SettingsManager.OnPostExposureIntensityChanged += OnPostExposureIntensityChanged;
            SettingsManager.OnContrastIntensityChanged += OnContrastIntensityChanged;
            SettingsManager.OnSaturationIntensityChanged += OnSaturationIntensityChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnSunIntensityChanged -= OnGlobalIntensityChanged;
            SettingsManager.OnVignettingIntensityChanged -= OnVignettingIntensityChanged;
            SettingsManager.OnPostExposureIntensityChanged -= OnPostExposureIntensityChanged;
            SettingsManager.OnContrastIntensityChanged -= OnContrastIntensityChanged;
            SettingsManager.OnSaturationIntensityChanged -= OnSaturationIntensityChanged;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Configure and show HDRP sky/fog/clouds, orient the sun, and ensure camera renders the sky and all environment settings
        /// </summary>
        internal void Load(FlightData flightData)
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

        /// <summary>
        /// Configure scene rendering (sun, volume profile)
        /// </summary>
        private void ConfigureSceneRendering(DateTime utcTime, double latitude, double longitude)
        {
            SunPosition sun = CalculateSunPosition(utcTime, latitude, longitude);
            CreateVolumeProfile(utcTime, sun);
            OrientMainLight(sun);
        }

        private void UninitializedVolumeProfile()
        {
            if (_reliveCamera != null)
            {
                HDAdditionalCameraData hdCam = _reliveCamera.GetComponent<HDAdditionalCameraData>();
                hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                _reliveCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            if (_povCamera != null)
            {
                HDAdditionalCameraData hdCam = _povCamera.GetComponent<HDAdditionalCameraData>();
                hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                _povCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
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
            _contactShadows = null;
            _colorAdjustments = null;
            _vignette = null;
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
                _contactShadows = _globalVolumeProfile.Add<ContactShadows>(true);
                _colorAdjustments = _globalVolumeProfile.Add<ColorAdjustments>(true);
                _vignette = _globalVolumeProfile.Add<Vignette>(true);
            }

            if (_reliveCamera != null)
            {
                HDAdditionalCameraData hdCam = _reliveCamera.GetComponent<HDAdditionalCameraData>();
                hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
            }


            if (_povCamera != null)
            {
                HDAdditionalCameraData hdCam = _povCamera.GetComponent<HDAdditionalCameraData>();
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
                _fog.active = true;
                _fog.enabled.Override(true);
                _fog.enableVolumetricFog.Override(true);
                _fog.meanFreePath.Override(600f);
                _fog.baseHeight.overrideState = false;
                _fog.maximumHeight.Override(500f);
                _fog.maxFogDistance.Override(5000f);
                _fog.colorMode.Override(FogColorMode.SkyColor);
                _fog.tint.overrideState = false;
                _fog.albedo.overrideState = false;
                _fog.globalLightProbeDimmer.Override(1f);
                _fog.volumetricFogBudget = 64f;
                _fog.denoisingMode.Override(FogDenoisingMode.Gaussian);
            }

            if (_clouds != null)
            {
                _clouds.active = true;
                _clouds.enable.overrideState = true;
                _clouds.enable.value = true;
                _clouds.enable.Override(true);
                _clouds.cloudControl.Override(VolumetricClouds.CloudControl.Simple);
                _clouds.cloudSimpleMode.Override(VolumetricClouds.CloudSimpleMode.Quality);
                _clouds.cloudPreset = VolumetricClouds.CloudPresets.Sparse;
                _clouds.shadows.Override(true);
                _clouds.shapeFactor.Override(0.95f);
                _clouds.shapeScale.Override(5f);
                _clouds.erosionScale.Override(107f);
                _clouds.bottomAltitude.Override(3000f);
                _clouds.altitudeRange.Override(1000f);
                _clouds.ambientLightProbeDimmer.Override(1f);
                _clouds.sunLightDimmer.Override(1f);
                _clouds.scatteringTint.overrideState = false;
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
                _contactShadows.fadeInDistance.overrideState = false;
                _contactShadows.fadeDistance.overrideState = false;
                _contactShadows.opacity.Override(0.9f);
                _contactShadows.rayBias.overrideState = false;
                _contactShadows.thicknessScale.overrideState = false;
                _contactShadows.sampleCount = 10;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.Override(SettingsManager.CurrentSettings.PostExposureIntensity);
                _colorAdjustments.contrast.Override(SettingsManager.CurrentSettings.ContrastIntensity);
                _colorAdjustments.colorFilter.overrideState = false;
                _colorAdjustments.hueShift.overrideState = false;
                _colorAdjustments.saturation.Override(SettingsManager.CurrentSettings.SaturationIntensity);
            }

            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.mode.Override(VignetteMode.Procedural);
                _vignette.color.overrideState = false;
                _vignette.center.overrideState = false;
                _vignette.intensity.Override(SettingsManager.CurrentSettings.VignettingIntensity);
                _vignette.smoothness.overrideState = false;
                _vignette.roundness.overrideState = false;
                _vignette.rounded.overrideState = false;
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
            _mainLight.intensity = SettingsManager.CurrentSettings.SunIntensity * BASE_LIGHT_LUX;
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

            _mainLight.intensity = SettingsManager.CurrentSettings.SunIntensity * BASE_LIGHT_LUX;
        }

        private void OnPostExposureIntensityChanged(float postExposure)
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.Override(SettingsManager.CurrentSettings.PostExposureIntensity);
            }
        }

        private void OnContrastIntensityChanged(float contrast)
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.contrast.Override(SettingsManager.CurrentSettings.ContrastIntensity);
            }
        }

        private void OnSaturationIntensityChanged(float saturation)
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.saturation.Override(SettingsManager.CurrentSettings.SaturationIntensity);
            }
        }

        private void OnVignettingIntensityChanged(float intensity)
        {
            if (_vignette != null)
            {
                _vignette.intensity.Override(intensity);
                _vignette.active = true;
            }
        }
        #endregion

        #region UI
        internal void DrawPostProcessingSettings(FuLayout layout)
        {
            layout.FramedText("Scene");
            layout.Separator();

            using (FuGrid gridSunIntensity = new FuGrid("gridSunIntensitySettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                float sunIntensity = SettingsManager.CurrentSettings.SunIntensity;

                if (gridSunIntensity.Slider("Sun intensity", ref sunIntensity, 0.6f, 1f, 0.01f))
                {
                    SettingsManager.SaveSunIntensity(sunIntensity);
                }
            }

            layout.Separator();
            layout.FramedText("Vignetting");
            layout.Separator();

            using (FuGrid gridVignetting = new FuGrid("gridVignettingSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {                
                float vignettingIntensity = SettingsManager.CurrentSettings.VignettingIntensity;
                if (gridVignetting.Slider("Vignetting", ref vignettingIntensity, 0f, 1f, 0.01f))
                {
                    SettingsManager.SaveVignettingIntensity(vignettingIntensity);
                }
            }

            layout.Separator();
            layout.FramedText("Light & colors adjustments");
            layout.Separator();

            using (FuGrid gridColorAdjustment = new FuGrid("gridColorAdjustmentSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                float postExposure = SettingsManager.CurrentSettings.PostExposureIntensity;
                if (gridColorAdjustment.Slider("Post-exposure", ref postExposure, 0f, 3f, 0.01f))
                {
                    SettingsManager.SavePostExposureIntensity(postExposure);
                }

                float contrast = SettingsManager.CurrentSettings.ContrastIntensity;
                if (gridColorAdjustment.Slider("Contrast", ref contrast, 0f, 50f, 0.1f, format: "%.1f"))
                {
                    SettingsManager.SaveContrastIntensity(contrast);
                }

                float saturation = SettingsManager.CurrentSettings.SaturationIntensity;
                if (gridColorAdjustment.Slider("Saturation", ref saturation, -20f, 20f, 0.1f, format: "%.1f"))
                {
                    SettingsManager.SaveSaturationIntensity(saturation);
                }
            }
        }

        internal void DrawSceneSettings(FuLayout layout)
        {
            layout.FramedText("POI");
            layout.Separator();

            using (FuGrid gridPOI = new FuGrid("gridPOISettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                POIManager.Instance.DisplayWorldUISettings(gridPOI);
            }

            layout.Separator();
            layout.FramedText("Buildings");
            layout.Separator();

            using (FuGrid gridBuilding = new FuGrid("gridBuildingSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                BuildingManager.Instance.DisplayBuildingsSettings(gridBuilding);
            }
        }
        #endregion
    }
}
