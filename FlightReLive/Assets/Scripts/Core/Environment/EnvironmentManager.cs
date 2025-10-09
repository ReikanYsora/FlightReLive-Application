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
        [Header("Sun")]
        [SerializeField, Range(0f, 50f)] private float _sunMaxIntensity = 18f;
        [SerializeField] private AnimationCurve _sunIntensityByElevation;
        [SerializeField] private Light _mainLight;

        [Header("Camera")]
        [SerializeField] private Camera _reliveCamera;
        [SerializeField] private Camera _povCamera;
        [SerializeField] private Color _cameraBackground;

        [Header("Post-processing")]
        [SerializeField] private LensFlareComponentSRP _lensFlare;
        [SerializeField] private VolumeProfile _volumeProfile;

        [Header("Sky")]
        [SerializeField] private Cubemap _spaceBackground;

        //Post-processing elements
        private bool _environmentLoaded;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private Tonemapping _toneMapping;
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
            SettingsManager.OnCloudShadowsOpacityChanged += OnCloudShadowsOpacityChanged;
            SettingsManager.OnWindTypeChanged += OnWindTypeChanged;

            UninitializeEnvironment();
        }

        private void OnDestroy()
        {
            UninitializeEnvironment();

            SettingsManager.OnContrastOffsetChanged -= OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged -= OnSaturationOffsetChanged;
            SettingsManager.OnVignettingIntensityChanged -= OnVignettingIntensityChanged;
            SettingsManager.OnCloudsPresetChanged -= OnCloudsPresetChanged;
            SettingsManager.OnCloudShadowsEnabledChanged -= OnCloudShadowsEnabledChanged;
            SettingsManager.OnCloudShadowsOpacityChanged -= OnCloudShadowsOpacityChanged;
            SettingsManager.OnWindTypeChanged -= OnWindTypeChanged;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Initialize all post-processing components
        /// </summary>
        private void InitializePostProcessingComponents()
        {
            if (_volumeProfile.TryGet(out Vignette vignette))
            {
                _vignette = vignette;
                _vignette.active = false;
                _vignette.center.overrideState = false;
                _vignette.rounded.overrideState = false;
            }

            if (_volumeProfile.TryGet(out ColorAdjustments colorAdjustments))
            {
                _colorAdjustments = colorAdjustments;
                _colorAdjustments.active = false;
                _colorAdjustments.postExposure.overrideState = false;
                _colorAdjustments.hueShift.overrideState = false;
            }

            if (_volumeProfile.TryGet(out Tonemapping toneMapping))
            {
                _toneMapping = toneMapping;
                _toneMapping.active = false;
            }

            if (_volumeProfile.TryGet(out PhysicallyBasedSky physicallyBasedSky))
            {
                _physicallyBasedSky = physicallyBasedSky;
                _physicallyBasedSky.active = false;
                _physicallyBasedSky.planetRotation.overrideState = false;
                _physicallyBasedSky.groundColorTexture.overrideState = false;
                _physicallyBasedSky.groundTint.overrideState = false;
                _physicallyBasedSky.groundEmissionTexture.overrideState = false;
                _physicallyBasedSky.groundEmissionMultiplier.overrideState = false;
                _physicallyBasedSky.colorSaturation.overrideState = false;
                _physicallyBasedSky.alphaSaturation.overrideState = false;
                _physicallyBasedSky.alphaMultiplier.overrideState = false;
            }

            if (_volumeProfile.TryGet(out Fog fog))
            {
                _fog = fog;
                _fog.enabled.Override(false);
                _fog.active = false;
                _fog.tint.overrideState = false;
                _fog.underWater.overrideState = false;
            }

            if (_volumeProfile.TryGet(out VisualEnvironment visualEnvironment))
            {
                _visualEnvironment = visualEnvironment;
                _visualEnvironment.active = false;
                _visualEnvironment.planetRadius.overrideState = false;
            }

            if (_volumeProfile.TryGet(out VolumetricClouds volumetricClouds))
            {
                _volumetricClouds = volumetricClouds;
                _volumetricClouds.active = false;
                _volumetricClouds.state.Override(false);
                _volumetricClouds.localClouds.overrideState = false;
                _volumetricClouds.shapeOffset.overrideState = false;
                _volumetricClouds.earthCurvature.overrideState = false;
                _volumetricClouds.verticalErosionWindSpeed.overrideState = false;
                _volumetricClouds.verticalShapeWindSpeed.overrideState = false;
                _volumetricClouds.ambientLightProbeDimmer.overrideState = false;
                _volumetricClouds.sunLightDimmer.overrideState = false;
                _volumetricClouds.scatteringTint.overrideState = false;
                _volumetricClouds.perceptualBlending.overrideState = false;
                _volumetricClouds.numLightSteps.overrideState = false;
                _volumetricClouds.fadeInMode.overrideState = false;
            }
        }

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

                //Initialize volume profile
                InitializeEnvironment();

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

                UninitializeEnvironment();
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

            //Sun orientation and intensity processing
            float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sun.Elevation;
            Vector3 direction = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad));
            float factor01 = Mathf.Clamp01(sun.ElevationFactor);
            float sunFactor = Mathf.Clamp01(_sunIntensityByElevation.Evaluate(factor01));
            float unityIntensity = Mathf.Clamp(_sunMaxIntensity * sunFactor, 0f, _sunMaxIntensity);

            //Vignetting
            _vignette.color.Override(Color.black);
            _vignette.intensity.Override(SettingsManager.CurrentSettings.VignettingIntensity);
            _vignette.smoothness.Override(0.3f);

            //Color Adjustments (Contrast: low at twilight, high at zenith. Saturation: warmer tones at low sun, neutral at zenith)
            _baseContrast = sunFactor * 40f;
            Color lowSunTint = new Color(1f, 0.93f, 0.85f);
            Color highSunTint = Color.white;
            _colorAdjustments.colorFilter.Override(Color.Lerp(lowSunTint, highSunTint, Mathf.SmoothStep(0f, 1f, sun.ElevationFactor)));
            _baseSaturation = Mathf.Lerp(-10f, 10f, sun.ElevationFactor);

            //Tonemapping
            _toneMapping.mode.Override(TonemappingMode.ACES);

            //Physically Based Sky
            _physicallyBasedSky.type.Override(PhysicallyBasedSky.PhysicallyBasedSkyModel.EarthAdvanced);
            _physicallyBasedSky.atmosphericScattering.Override(true);
            float lstDeg = GetLocalSiderealDegrees(dateTime, longitude);
            _physicallyBasedSky.spaceRotation.Override(new Vector3(0f, lstDeg, 0f));
            _physicallyBasedSky.spaceEmissionTexture.Override(_spaceBackground);
            float nightFactor = 1.0f / (1.0f + Mathf.Exp(0.28f * (sun.Elevation - -6.0f)));
            nightFactor = Mathf.Pow(nightFactor, 1.2f);
            float spaceEmission = 8f * nightFactor;
            _physicallyBasedSky.spaceEmissionMultiplier.Override(spaceEmission);
            _physicallyBasedSky.aerosolDensity.Override(0.025f);
            _physicallyBasedSky.aerosolTint.Override(Color.white);
            _physicallyBasedSky.aerosolAnisotropy.Override(0.85f);
            _physicallyBasedSky.aerosolMaximumAltitude.Override(2000f);
            _physicallyBasedSky.horizonZenithShift.Override(0f);
            Color nightSkyTint = new Color(0.02f, 0.03f, 0.05f);
            Color daySkyTint = Color.white;
            Color skyTint = Color.Lerp(nightSkyTint, daySkyTint, Mathf.SmoothStep(-5f, 5f, sun.Elevation));
            _physicallyBasedSky.horizonTint.Override(skyTint);
            _physicallyBasedSky.zenithTint.Override(skyTint);
            _physicallyBasedSky.skyIntensityMode.Override(PhysicallyBasedSky.SkyIntensityMode.Exposure);
            _physicallyBasedSky.exposure.Override(0.5f);

            //Fog
            _fog.meanFreePath.Override(1500f);
            _fog.baseHeight.Override(0f);
            _fog.maximumHeight.Override(50f);
            _fog.maxFogDistance.Override(10000f);
            _fog.colorMode.Override(Fog.FogColorMode.SkyColor);
            _fog.tint.Override(skyTint);

            //Visual Environment
            _visualEnvironment.skyType.Override((int)VisualEnvironment.SkyType.PhysicallyBased);
            _visualEnvironment.skyAmbientMode.Override(VisualEnvironment.SkyAmbientMode.Dynamic);
            _visualEnvironment.renderingSpace.Override(VisualEnvironment.RenderingSpace.Camera);

            //Volumetric Clouds
            _volumetricClouds.temporalAccumulationFactor.Override(1);
            _volumetricClouds.numPrimarySteps.Override(100);

            //Sun color
            Color sunColor = Color.white;
            if (sun.Elevation < 15f)
            {
                float warmFactor = Mathf.InverseLerp(-5f, 15f, sun.Elevation);
                sunColor = Color.Lerp(new Color(1f, 0.55f, 0.28f), Color.white, warmFactor);
            }

            //Main lights settings
            if (_mainLight != null)
            {
                _mainLight.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
                _mainLight.intensity = unityIntensity;
                _mainLight.color = sunColor;
                RenderSettings.sun = _mainLight;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                float ambientBoost = Mathf.Clamp01(1f - sun.ElevationFactor);
                Color nightAmbient = new Color(0.02f, 0.025f, 0.03f);
                RenderSettings.ambientLight += nightAmbient * ambientBoost;

                if (RenderSettings.defaultReflectionMode != DefaultReflectionMode.Skybox)
                {
                    RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                }

                RenderSettings.defaultReflectionResolution = 128;
                RenderSettings.reflectionIntensity = 1f;
            }

            //Lens flare baseline from elevation
            if (_lensFlare != null)
            {
                _lensFlare.intensity = Mathf.Lerp(0.5f, 3f, sunFactor * sunFactor);
                _lensFlare.scale = Mathf.Lerp(0.6f, 1.5f, Mathf.Sqrt(sunFactor));
                _lensFlare.occlusionRadius = Mathf.Lerp(0.3f, 1f, sunFactor);
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
            ApplyCloudShadowsOpacity();
            ApplyWindType();
        }

        /// <summary>
        /// Add all post-processing components on the serialized VolumeProfile, initialize camera and light properties
        /// </summary>
        private void InitializeEnvironment()
        {
            //Initialize post-processing components
            InitializePostProcessingComponents();

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

            _vignette.active = true;
            _colorAdjustments.active = true;
            _toneMapping.active = true;
            _physicallyBasedSky.active = true;
            _fog.active = true;
            _fog.enabled.Override(true);
            _visualEnvironment.active = true;
            _volumetricClouds.state.Override(true);
            _volumetricClouds.active = true;
            _environmentLoaded = true;
        }

        /// <summary>
        /// Remove all post-processing components from the serialized VolumeProfile, reset cameras and light properties.
        /// Ensures each effect are deleted.
        /// </summary>
        private void UninitializeEnvironment()
        {
            _environmentLoaded = false;

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

            if (_vignette != null)
            {
                _vignette.active = false;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.active = false;
            }

            if (_toneMapping != null)
            {
                _toneMapping.active = false;
            }

            if (_physicallyBasedSky != null)
            {
                _physicallyBasedSky.active = false;
            }

            if (_fog != null)
            {
                _fog.active = false;
            }

            if (_visualEnvironment != null)
            {
                _visualEnvironment.active = false;
            }

            if (_volumetricClouds != null)
            {
                _volumetricClouds.state.Override(false);
                _volumetricClouds.active = false;
            }

            if (_mainLight != null)
            {
                _mainLight.intensity = 0f;
                _mainLight.color = Color.white;
            }

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
        /// Returns local sidereal time (degrees 0..360) for a given UTC datetime and longitude (east positive).
        /// </summary>
        private static float GetLocalSiderealDegrees(DateTime utc, double longitude)
        {
            if (utc.Kind != DateTimeKind.Utc)
            {
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            DateTime j2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            double d = (utc - j2000).TotalDays;
            double gmst = 280.46061837 + 360.98564736629 * d;
            double lst = gmst + longitude;
            lst = lst % 360.0;

            if (lst < 0.0)
            {
                lst += 360.0;
            }

            return (float)lst;
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
                float contrast = _baseContrast + (30f * SettingsManager.CurrentSettings.ContrastOffset);
                _colorAdjustments.contrast.Override(contrast);
            }
        }

        /// <summary>
        /// Apply saturation custom settings (URP)
        /// </summary>
        private void ApplySaturation()
        {
            if (_colorAdjustments != null)
            {
                float saturation = _baseSaturation + (30f * SettingsManager.CurrentSettings.SaturationOffset);
                _colorAdjustments.saturation.Override(saturation);
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

        private void ApplyCloudShadowsOpacity()
        {
            if (_volumetricClouds != null)
            {
                float shadowOpacity = SettingsManager.CurrentSettings.CloudShadowsOpacity;
                _volumetricClouds.shadowOpacity.Override(shadowOpacity);
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

        private void OnCloudShadowsOpacityChanged(float obj)
        {
            ApplyCloudShadowsOpacity();
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
                if (!_environmentLoaded)
                {
                    grid.DisableNextElements();
                }

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
            layout.FramedText("Color adjustments");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridEnvOffsets", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                if (!_environmentLoaded)
                {
                    grid.DisableNextElements();
                }

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
        }

        internal void DrawSunCloudsSettings(FuLayout layout)
        {
            layout.FramedText("Sky & Clouds");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridSkyCloud", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                if (!_environmentLoaded)
                {
                    grid.DisableNextElements();
                }

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

                if (!shadowsEnabled)
                {
                    grid.DisableNextElements();
                }

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Shadows opacity",
                    "Define the opacity of cloud shadows",
                    $"Reset default cloud shadows opacity to default value ({SettingsManager.CLOUD_SHADOW_OPACITY_DEFAULT_STATE}).",
                    SettingsManager.CurrentSettings.CloudShadowsOpacity,
                    0.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.CLOUD_SHADOW_OPACITY_DEFAULT_STATE,
                    "%.2f",
                     (x) => SettingsManager.SaveCloudShadowsOpacity(x),
                     () => SettingsManager.ResetCloudShadowsOpacity());

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

                grid.EnableNextElements();

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Day time",
                    "Define a custom day time",
                    $"Reset time to default time value.",
                    _dayTime,
                    0.0f,
                    1.0f,
                    0.001f,
                    GetNormalizedTimeOfDay(_originalTimeUTC),
                    "%.3f",
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
