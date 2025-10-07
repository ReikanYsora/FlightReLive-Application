using FlightReLive.Core.Building;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlightReLive.Core.Environment
{
    /// <summary>
    /// Centralized environment manager (HDRP) that builds a physically-plausible baseline from sun elevation (exposure/contrast/saturation/indirect light) and applies user offsets on top (reset at every Load).
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Light & Camera")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Camera _reliveCamera;
        [SerializeField] private Camera _povCamera;
        [SerializeField] private Color _cameraBackground;
        [SerializeField] private LensFlareComponentSRP _lensFlare;
        [SerializeField] private VolumeProfile _volumeProfile;
        [SerializeField] private Cubemap _spaceBackground;

        //Post-processing elements
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private Tonemapping _tonemapping;
        private PhysicallyBasedSky _physicallyBasedSky;
        private Fog _fog;
        private VisualEnvironment _visualEnvironment;
        private VolumetricClouds _volumetricClouds;

        //Baseline values
        private float _dayTime;
        private DateTime _originalTimeUTC;
        private DateTime _dateTimeUTC;
        private double _latitude;
        private double _longitude;
        private float _baseContrast;
        private float _baseSaturation;
        private float _baseLensFlareIntensity;
        private float _baseLensFlareScale;
        private float _baseLensFlareOccRadius;
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

            _baseContrast = 0.0f;
            _baseSaturation = 0.0f;
        }

        private void Start()
        {
            SettingsManager.OnContrastOffsetChanged += OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged += OnSaturationOffsetChanged;
            SettingsManager.OnVignettingIntensityChanged += OnVignettingIntensityChanged;
            SettingsManager.OnCloudsPresetChanged += OnCloudsPresetChanged;
            SettingsManager.OnCloudShadowsEnabledChanged += OnCloudShadowsEnabledChanged;
            SettingsManager.OnWindTypeChanged += OnWindTypeChanged;
            UnitializeEnvironment();
        }

        private void OnDestroy()
        {
            SettingsManager.OnContrastOffsetChanged -= OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged -= OnSaturationOffsetChanged;
            SettingsManager.OnVignettingIntensityChanged -= OnVignettingIntensityChanged;
            SettingsManager.OnCloudsPresetChanged -= OnCloudsPresetChanged;
            SettingsManager.OnCloudShadowsEnabledChanged -= OnCloudShadowsEnabledChanged;
            SettingsManager.OnWindTypeChanged -= OnWindTypeChanged;
            UnitializeEnvironment();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Configure sky/fog/clouds, sun direction and baseline tonemapping from flight date/location.
        /// Resets user offsets so each flight starts fresh.
        /// </summary>
        internal void Load(FlightData flightData)
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                if (flightData == null)
                {
                    return;
                }


                //Calculate sun position
                TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                DateTime flightUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);

                //Saved mandatory attributes
                _dateTimeUTC = flightUtc;
                _originalTimeUTC = flightUtc;
                _latitude = flightData.GPSOrigin.Latitude;
                _longitude = flightData.GPSOrigin.Longitude;
                _dayTime = GetNormalizedTimeOfDay(_dateTimeUTC);

                //Apply environment base on current flightdata and datetime
                ApplyEnvironment(_dateTimeUTC, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
            });
        }

        /// <summary>
        /// Reset to a flat dark background, remove sky/fog/clouds until next Load.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                _dateTimeUTC = DateTime.MinValue;
                _originalTimeUTC = DateTime.MinValue;
                _latitude = 0;
                _longitude = 0;

                UnitializeEnvironment();
            });
        }

        /// <summary>
        /// Apply dynamic environment based on current datetime and GPS position
        /// </summary>
        /// <param name="flightData"></param>
        /// <param name="dateTime"></param>
        private void ApplyEnvironment(DateTime dateTime, double latitude, double longitude)
        {
            if (_volumeProfile == null)
            {
                return;
            }
            SunPosition sun = SunHelper.CalculateSunPosition(dateTime, latitude, longitude);

            //Vignetting
            _vignette = GetOrAddVolumeComponent<Vignette>();
            _vignette.color.Override(Color.black);
            _vignette.center.Override(new Vector2(0.5f, 0.5f));
            _vignette.intensity.Override(SettingsManager.CurrentSettings.VignettingIntensity);
            _vignette.smoothness.Override(0.3f);
            _vignette.rounded.overrideState = false;

            //Color Adjustments (Contrast: low at twilight, high at zenith. Saturation: warmer tones at low sun, neutral at zenith)
            _colorAdjustments = GetOrAddVolumeComponent<ColorAdjustments>();
            _colorAdjustments.active = true;
            _baseContrast = Mathf.Lerp(-20f, 20f, sun.ElevationFactor);
            _colorAdjustments.postExposure.overrideState = false;
            _colorAdjustments.contrast.value = _baseContrast + (20f * SettingsManager.CurrentSettings.ContrastOffset);
            Color lowSunTint = new Color(1f, 0.93f, 0.85f);
            Color highSunTint = Color.white;
            _colorAdjustments.colorFilter.value = Color.Lerp(lowSunTint, highSunTint, Mathf.SmoothStep(0f, 1f, sun.ElevationFactor));
            _colorAdjustments.hueShift.overrideState = false;
            _baseSaturation = Mathf.Lerp(-10f, 10f, sun.ElevationFactor);
            _colorAdjustments.saturation.value = _baseSaturation + (20f * SettingsManager.CurrentSettings.SaturationOffset);

            //Tonemapping
            _tonemapping = GetOrAddVolumeComponent<Tonemapping>();
            _tonemapping.active = true;
            _tonemapping.mode.value = TonemappingMode.ACES;

            //Physically Based Sky
            _physicallyBasedSky = GetOrAddVolumeComponent<PhysicallyBasedSky>();
            _physicallyBasedSky.active = true;
            _physicallyBasedSky.type.Override(PhysicallyBasedSky.PhysicallyBasedSkyModel.EarthAdvanced);
            _physicallyBasedSky.atmosphericScattering.Override(true);
            _physicallyBasedSky.planetRotation.overrideState = false;
            _physicallyBasedSky.groundColorTexture.overrideState = false;
            _physicallyBasedSky.groundTint.overrideState = false;
            _physicallyBasedSky.groundEmissionTexture.overrideState = false;
            _physicallyBasedSky.groundEmissionMultiplier.overrideState = false;
            _physicallyBasedSky.spaceRotation.overrideState = false;
            _physicallyBasedSky.spaceEmissionTexture.Override(_spaceBackground);
            float spaceEmission = 5f * Mathf.Clamp01(1f - sun.ElevationFactor);
            _physicallyBasedSky.spaceEmissionMultiplier.Override(spaceEmission);
            _physicallyBasedSky.aerosolDensity.Override(0.05f);
            _physicallyBasedSky.aerosolTint.Override(Color.white);
            _physicallyBasedSky.aerosolAnisotropy.Override(0.85f);
            _physicallyBasedSky.aerosolMaximumAltitude.Override(2000f);
            _physicallyBasedSky.ozoneDensityDimmer.Override(1f);
            _physicallyBasedSky.colorSaturation.overrideState = false;
            _physicallyBasedSky.alphaSaturation.overrideState = false;
            _physicallyBasedSky.alphaMultiplier.overrideState = false;
            _physicallyBasedSky.horizonTint.Override(Color.white);
            _physicallyBasedSky.horizonZenithShift.Override(0f);
            _physicallyBasedSky.zenithTint.Override(Color.white);
            _physicallyBasedSky.skyIntensityMode.overrideState = false;
            _physicallyBasedSky.exposure.overrideState = false;
            _physicallyBasedSky.skyIntensityMode.Override(PhysicallyBasedSky.SkyIntensityMode.Exposure);

            //Fog
            _fog = GetOrAddVolumeComponent<Fog>();
            _fog.enabled.Override(true);
            _fog.active = true;
            _fog.meanFreePath.Override(500f);
            _fog.baseHeight.Override(0f);
            _fog.maximumHeight.Override(250f);
            _fog.maxFogDistance.Override(5000f);
            _fog.colorMode.Override(Fog.FogColorMode.SkyColor);
            _fog.tint.overrideState = false;
            _fog.underWater.overrideState = false;

            //Visual Environment
            _visualEnvironment = GetOrAddVolumeComponent<VisualEnvironment>();
            _visualEnvironment.active = true; 
            _visualEnvironment.skyType.Override((int)VisualEnvironment.SkyType.PhysicallyBased);
            _visualEnvironment.skyAmbientMode.Override(VisualEnvironment.SkyAmbientMode.Dynamic);
            _visualEnvironment.planetRadius.overrideState = false;
            _visualEnvironment.renderingSpace.overrideState = false;

            //Volumetric Clouds
            _volumetricClouds = GetOrAddVolumeComponent<VolumetricClouds>();
            _volumetricClouds.state.Override(true);
            _volumetricClouds.active = true;
            _volumetricClouds.localClouds.overrideState = false;
            _volumetricClouds.shapeOffset.overrideState = false;
            _volumetricClouds.earthCurvature.overrideState = false;
            _volumetricClouds.verticalErosionWindSpeed.overrideState = false;
            _volumetricClouds.verticalShapeWindSpeed.overrideState = false;
            _volumetricClouds.ambientLightProbeDimmer.overrideState = false;
            _volumetricClouds.sunLightDimmer.overrideState = false;
            _volumetricClouds.scatteringTint.overrideState = false;
            _volumetricClouds.temporalAccumulationFactor.Override(1);
            _volumetricClouds.perceptualBlending.overrideState = false;
            _volumetricClouds.numPrimarySteps.Override(100);
            _volumetricClouds.numLightSteps.overrideState = false;
            _volumetricClouds.fadeInMode.overrideState = false;

            //Relive camera
            if (_reliveCamera != null)
            {
                _reliveCamera.clearFlags = CameraClearFlags.Skybox;
            }

            //POV camera
            if (_povCamera != null)
            {
                _povCamera.clearFlags = CameraClearFlags.Skybox;
            }

            if (_mainLight == null)
            {
                return;
            }

            //Sun orientation
            float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sun.Elevation;
            Vector3 direction = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad));
            _mainLight.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            float elevation = Mathf.Max(sun.Elevation, -5f);
            float t = Mathf.Clamp01((elevation + 5f) / 95f);
            float lux = Mathf.Max(0f, 120000f * Mathf.Sin(elevationRad));
            float unityIntensity = Mathf.Pow(lux / 10000f, 0.5f);
            unityIntensity = Mathf.Clamp(unityIntensity, 0.001f, 12f);
            Color sunColor = Color.white;

            if (sun.Elevation < 10f)
            {
                float warmFactor = Mathf.InverseLerp(-5f, 10f, sun.Elevation);
                sunColor = Color.Lerp(new Color(1f, 0.6f, 0.3f), Color.white, warmFactor);
            }

            //Main lights settings
            _mainLight.intensity = unityIntensity;
            _mainLight.color = sunColor;
            RenderSettings.sun = _mainLight;

            //Lens flare baseline from elevation
            if (_lensFlare != null)
            {
                _baseLensFlareIntensity = Mathf.Lerp(2f, 3f, Mathf.Pow(sun.ElevationFactor, 3f));
                _baseLensFlareScale = Mathf.Lerp(0.5f, 1.5f, Mathf.Pow(sun.ElevationFactor, 0.3f));
                _baseLensFlareOccRadius = Mathf.Lerp(0.3f, 1f, sun.ElevationFactor);
                _lensFlare.intensity = _baseLensFlareIntensity;
                _lensFlare.scale = _baseLensFlareScale;
                _lensFlare.occlusionRadius = _baseLensFlareOccRadius;
                _lensFlare.attenuationByLightShape = true;
                _lensFlare.environmentOcclusion = true;
                _lensFlare.enabled = true;
            }

            //Apply user customs settings
            ApplyVignettingIntensity();
            ApplyContrast();
            ApplySaturation();
            ApplyCloudsPreset();
            ApplyCloudShadowsEnabled();
            ApplyWindType();
        }

        /// <summary>
        /// Remove all post-processing components from the serialized VolumeProfile, reset cameras and light properties.
        /// Ensures each effect are deleted.
        /// </summary>
        private void UnitializeEnvironment()
        {
            if (_reliveCamera != null)
            {
                _reliveCamera.clearFlags = CameraClearFlags.SolidColor;
                _reliveCamera.backgroundColor = _cameraBackground;
            }

            if (_povCamera != null)
            {
                _povCamera.clearFlags = CameraClearFlags.SolidColor;
                _povCamera.backgroundColor = _cameraBackground;
            }

            _vignette = null;
            _colorAdjustments = null;
            _tonemapping = null;
            _physicallyBasedSky = null;
            _visualEnvironment = null;
            _fog = null;
            _volumetricClouds = null;

            _volumeProfile.Remove<Vignette>();
            _volumeProfile.Remove<ColorAdjustments>();
            _volumeProfile.Remove<Tonemapping>();
            _volumeProfile.Remove<PhysicallyBasedSky>();
            _volumeProfile.Remove<VisualEnvironment>();
            _volumeProfile.Remove<Fog>();
            _volumeProfile.Remove<VolumetricClouds>();

            _mainLight.intensity = 0f;
            _mainLight.color = Color.white;

            if (_lensFlare != null)
            {
                _lensFlare.enabled = false;
            }

            RenderSettings.sun = null;
            RenderSettings.ambientMode = AmbientMode.Flat;

            _baseContrast = 0f;
            _baseSaturation = 0f;
        }

        /// <summary>
        /// Generic method to add or get post-processing volume component
        /// </summary>
        /// <typeparam name="T">VolumeComponent</typeparam>
        /// <returns>VolumeComponent</returns>
        private T GetOrAddVolumeComponent<T>() where T : VolumeComponent, new()
        {
            if (_volumeProfile == null)
            {
                return null;
            }

            if (!_volumeProfile.TryGet<T>(out var component))
            {
                component = _volumeProfile.Add<T>(true);
            }

            component.active = true;
            return component;
        }

        /// <summary>
        /// Apply vignetting intensity from user settings (URP)
        /// </summary>
        private void ApplyVignettingIntensity()
        {
            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.intensity.value = SettingsManager.CurrentSettings.VignettingIntensity;
            }
        }

        /// <summary>
        /// Apply contrast custom settings (URP)
        /// </summary>
        private void ApplyContrast()
        {
            if (_colorAdjustments != null)
            {
                float contrast = _baseContrast + (20f * SettingsManager.CurrentSettings.ContrastOffset);
                _colorAdjustments.active = true;
                _colorAdjustments.contrast.value = contrast;
            }
        }

        /// <summary>
        /// Apply saturation custom settings (URP)
        /// </summary>
        private void ApplySaturation()
        {
            if (_colorAdjustments != null)
            {
                float saturation = _baseSaturation + (20f * SettingsManager.CurrentSettings.SaturationOffset);
                _colorAdjustments.active = true;
                _colorAdjustments.saturation.value = saturation;
            }
        }

        private void ApplyCloudsPreset()
        {
            if (_volumetricClouds != null)
            {
                switch (SettingsManager.CurrentSettings.CloudsPreset)
                {
                    case CloudsPreset.None:
                        _volumetricClouds.state.Override(false);
                        _volumetricClouds.active = false;
                        break;
                    default:
                    case CloudsPreset.Sparse:
                        _volumetricClouds.state.Override(true);
                        _volumetricClouds.active = true;
                        _volumetricClouds.cloudPreset = VolumetricClouds.CloudPresets.Sparse;
                        break;
                    case CloudsPreset.Cloudy:
                        _volumetricClouds.state.Override(true);
                        _volumetricClouds.active = true;
                        _volumetricClouds.cloudPreset = VolumetricClouds.CloudPresets.Cloudy;
                        break;
                    case CloudsPreset.Overcast:
                        _volumetricClouds.state.Override(true);
                        _volumetricClouds.active = true;
                        _volumetricClouds.cloudPreset = VolumetricClouds.CloudPresets.Overcast;
                        break;
                    case CloudsPreset.Stormy:
                        _volumetricClouds.state.Override(true);
                        _volumetricClouds.active = true;
                        _volumetricClouds.cloudPreset = VolumetricClouds.CloudPresets.Stormy;
                        break;
                }
            }
        }

        private void ApplyCloudShadowsEnabled()
        {
            if (_volumetricClouds != null)
            {
                bool enabled = SettingsManager.CurrentSettings.CloudShadowsEnabled;
                _volumetricClouds.shadows.Override(enabled);

                if (enabled)
                {
                    _volumetricClouds.shadowResolution.Override(VolumetricClouds.CloudShadowResolution.High512);
                }
            }
        }

        private void ApplyWindType()
        {
            if (_volumetricClouds != null)
            {
                switch (SettingsManager.CurrentSettings.WindType)
                {
                    case WindType.None:
                        _volumetricClouds.verticalShapeWindSpeed.Override(0f);
                        _volumetricClouds.verticalErosionWindSpeed.Override(0f);
                        break;
                    default:
                    case WindType.Slow:
                        _volumetricClouds.verticalShapeWindSpeed.Override(250f);
                        _volumetricClouds.verticalErosionWindSpeed.Override(250f);
                        break;
                    case WindType.Normal:
                        _volumetricClouds.verticalShapeWindSpeed.Override(500f);
                        _volumetricClouds.verticalErosionWindSpeed.Override(500f);
                        break;
                    case WindType.Fast:
                        _volumetricClouds.verticalShapeWindSpeed.Override(1000f);
                        _volumetricClouds.verticalErosionWindSpeed.Override(1000f);
                        break;
                }
            }
        }
        #endregion

        #region CALLBACKS

        private void OnContrastOffsetChanged(float offset)
        {
            ApplyContrast();
        }

        private void OnSaturationOffsetChanged(float offset)
        {
            ApplySaturation();
        }

        private void OnVignettingIntensityChanged(float intensity)
        {
            ApplyVignettingIntensity();
        }

        private void OnCloudsPresetChanged(CloudsPreset obj)
        {
            ApplyCloudsPreset();
        }

        private void OnCloudShadowsEnabledChanged(bool obj)
        {
            ApplyCloudShadowsEnabled();
        }

        private void OnWindTypeChanged(WindType obj)
        {
            ApplyWindType();
        }

        /// <summary>
        /// Update sun position based on normalized time of day (0→1 = 00:00→23:59).
        /// </summary>
        private void ApplyTimeOfDay(DateTime utcDateTime, double latitude, double longitude, float normalized)
        {
            _dayTime = normalized;
            DateTime newUtc = GetDateTimeFromNormalized(normalized, utcDateTime);
            ApplyEnvironment(newUtc, latitude, longitude);
        }

        /// <summary>
        /// Convert a normalized time-of-day value (0 - 1) into a DateTime for a given base date.
        /// </summary>
        private DateTime GetDateTimeFromNormalized(float normalized, DateTime baseDate)
        {
            normalized = Mathf.Clamp01(normalized);

            double totalMinutes = 1440.0 * normalized;
            int hours = (int)(totalMinutes / 60.0);
            int minutes = (int)(totalMinutes % 60.0);

            return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hours, minutes, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Convert a DateTime into a normalized time-of-day value between 0 and 1 (00:00 → 0, 23:59 → 1).
        /// </summary>
        private float GetNormalizedTimeOfDay(DateTime dateTime)
        {
            double totalMinutes = dateTime.TimeOfDay.TotalMinutes;

            return Mathf.Clamp01((float)(totalMinutes / 1440.0));
        }
        #endregion

        #region UI
        internal void DrawPostProcessingSettings(FuLayout layout)
        {
            layout.FramedText("Vignetting");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridVignettingOffset", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                //Display vignetting custom settings
                float vignetting = SettingsManager.CurrentSettings.VignettingIntensity;

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Vignetting",
                    "Define a vignetting value for the scene.",
                    $"Reset vignetting to default value ({SettingsManager.VIGNETTING_DEFAULT_VALUE}).",
                    vignetting,
                    0.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.VIGNETTING_DEFAULT_VALUE,
                    "%.2f",
                     (x) => SettingsManager.SaveVignettingIntensity(x),
                     () => SettingsManager.ResetVignettingIntensity());
            }

            layout.Separator();
            layout.FramedText("Lighting & Color");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridEnvOffsets", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                //Display contrast custom settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Contrast",
                    "Define a custom contrast offset for the scene.",
                    $"Reset contrast offset to default value ({SettingsManager.CONTRAST_OFFSET_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.ContrastOffset,
                    -1.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.CONTRAST_OFFSET_DEFAULT_VALUE,
                    "%.2f",
                     (x) => SettingsManager.SaveContrastOffset(x),
                     () => SettingsManager.ResetContrastOffset());

                //Display saturation custom settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Saturation",
                    "Define a custom saturation offset for the scene.",
                    $"Reset saturation offset to default value ({SettingsManager.SATURATION_OFFSET_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.SaturationOffset,
                    -1.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.SATURATION_OFFSET_DEFAULT_VALUE,
                    "%.2f",
                     (x) => SettingsManager.SaveSaturationOffset(x),
                     () => SettingsManager.ResetSaturationOffset());
            }

            layout.Separator();
            layout.FramedText("Sky & Clouds");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridSkyCloud", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                CloudsPreset savedCloudsPreset = SettingsManager.CurrentSettings.CloudsPreset;
                SettingsManager.DisplaySettingsComboboxWithReset<CloudsPreset>(grid,
                    "Clouds type",
                    "Define the current clouds type.",
                    "Reset current clouds type to default value.",
                    savedCloudsPreset,
                    SettingsManager.CLOUD_PRESET_DEFAULT_VALUE,
                    (x) => x.ToString(),
                    Enum.GetValues(typeof(CloudsPreset)).Cast<CloudsPreset>(),
                    (x) => SettingsManager.SaveCloudsPreset(x),
                    () => SettingsManager.ResetCloudsPreset());

                bool shadowsEnabled = SettingsManager.CurrentSettings.CloudShadowsEnabled;
                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Clouds shadows",
                    "Display or hide clouds shadows.",
                    $"Reset clouds shadows display state to default value.",
                    shadowsEnabled,
                    SettingsManager.CLOUD_SHADOW_ENABLED_DEFAULT_STATE,
                     (x) => SettingsManager.SaveCloudShadowsEnabled(x),
                     () => SettingsManager.ResetCloudShadowsEnabled());

                WindType savedWindType = SettingsManager.CurrentSettings.WindType;
                SettingsManager.DisplaySettingsComboboxWithReset<WindType>(grid,
                    "Wind speed",
                    "Define the wind speed for clouds animation.",
                    "Reset current wind speed type to default value.",
                    savedWindType,
                    SettingsManager.WIND_TYPE_DEFAULT_VALUE,
                    (x) => x.ToString(),
                    Enum.GetValues(typeof(WindType)).Cast<WindType>(),
                    (x) => SettingsManager.SaveWindType(x),
                    () => SettingsManager.ResetWindType());

                if (_originalTimeUTC == DateTime.MinValue || _latitude == 0 || _longitude == 0)
                {
                    grid.DisableNextElements();
                }

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Day cycle",
                    "Define a custom saturation offset for the scene.",
                    $"Reset saturation offset to default value ({SettingsManager.SATURATION_OFFSET_DEFAULT_VALUE}).",
                    _dayTime,
                    0.0f,
                    1.0f,
                    0.01f,
                    GetNormalizedTimeOfDay(_originalTimeUTC),
                    "%.2f",
                     (x) => ApplyTimeOfDay(_dateTimeUTC, _latitude, _longitude, x),
                     () =>
                     {
                         _dateTimeUTC = _originalTimeUTC;
                         ApplyEnvironment(_originalTimeUTC, _latitude, _longitude);
                     });
            }
        }

        internal void DrawSceneSettings(FuLayout layout)
        {
            layout.FramedText("Buildings");
            layout.Separator();

            BuildingManager.Instance.DisplayBuildingsSettings();
        }
        #endregion
    }
}
