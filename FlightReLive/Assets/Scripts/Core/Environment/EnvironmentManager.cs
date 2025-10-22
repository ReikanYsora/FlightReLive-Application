using FlightReLive.Core.OpenVectorTile;
using FlightReLive.Core.Database;
using FlightReLive.Core.POI;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace FlightReLive.Core.Environment
{
    /// <summary>
    /// Centralized environment manager (HDRP) that builds a physically-plausible baseline from sun elevation (exposure/contrast/saturation/indirect light) and applies user offsets on top (reset at every Load).
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        #region CONSTANTS
        private const float RUNTIME_VOLUME_PRIORITY = 1000f;
        #endregion

        #region ATTRIBUTES

        [Header("Lights")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Light _moonLight;

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
        private GameObject _runtimeVolumeGO;
        private Volume _runtimeVolume;
        private bool _environmentLoaded;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private Tonemapping _toneMapping;
        private PhysicallyBasedSky _physicallyBasedSky;
        private Fog _fog;
        private VisualEnvironment _visualEnvironment;
        private VolumetricClouds _volumetricClouds;

        //Baseline values
        private float _baseContrast;
        private float _baseSaturation;
        private float _baseExposure;

        //Location
        private double _latitude;
        private double _longitude;

        //Time control
        private float _dayRatio;
        private DateTime _flightTimeUTC;
        private DateTime _sceneTimeUTC;
        private SunTimes _sunTimes;
        #endregion

        #region PROPERTIES
        internal static EnvironmentManager Instance { get; private set; }

        internal DateTime FlightTimeUTC
        {
            get
            {
                return _flightTimeUTC;
            }
        }

        internal float DayRatio
        {
            get
            {
                return _dayRatio;
            }
        }

        internal SunTimes SunTimes
        {
            get
            {
                return _sunTimes;
            }
        }
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
            SettingsManager.OnExposureOffsetChanged += OnExposureOffsetChanged;
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
            SettingsManager.OnExposureOffsetChanged -= OnExposureOffsetChanged;
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
                _volumetricClouds.scatteringTint.overrideState = false;
                _volumetricClouds.perceptualBlending.overrideState = false;
                _volumetricClouds.numLightSteps.overrideState = false;
                _volumetricClouds.fadeInMode.overrideState = false;
            }

            if (_reliveCamera != null)
            {
                _reliveCamera.allowHDR = true;
            }

            if (_povCamera != null)
            {
                _povCamera.allowHDR = true;
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

                TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                DateTime flightUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);

                _flightTimeUTC = flightUtc;
                _sceneTimeUTC = flightUtc;
                _latitude = flightData.GPSOrigin.Latitude;
                _longitude = flightData.GPSOrigin.Longitude;
                _dayRatio = GetNormalizedTimeOfDay(_sceneTimeUTC);

                //Initialize volume profile
                InitializeEnvironment();

                //Use local time to get correct sunrise/sunset hours
                _sunTimes = SunHelper.GetSunriseSunset(localTime, _latitude, _longitude);
                ApplyEnvironment(_sceneTimeUTC, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
            });
        }

        /// <summary>
        /// Reset to a flat dark background, remove sky/fog/clouds until next Load.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                _sceneTimeUTC = DateTime.MinValue;
                _flightTimeUTC = DateTime.MinValue;
                _latitude = 0;
                _longitude = 0;
                _dayRatio = 0f;
                _sunTimes = new SunTimes();
                UninitializeEnvironment();
            });
        }

        /// <summary>
        /// Apply dynamic environment based on current datetime and GPS position.
        /// Soft daylight calibration — reduced brightness and exposure for realistic midday.
        /// </summary>
        private void ApplyEnvironment(DateTime dateTime, double latitude, double longitude)
        {
            if (_volumeProfile == null)
            {
                return;
            }

            SunPosition sun = SunHelper.CalculateSunPosition(dateTime, latitude, longitude);

            //Sun orientation
            float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sun.Elevation;
            Vector3 direction = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad));

            //Perceptual daylight factor
            float daylight = Mathf.InverseLerp(-6f, 60f, sun.Elevation);
            daylight = Mathf.Clamp01(Mathf.Pow(daylight, 0.9f));
            bool isDay = sun.Elevation > 0.0f;
            bool isNight = sun.Elevation < -2.0f;

            //Main light (Sun) base intensity
            float softDay = Mathf.SmoothStep(0f, 1f, daylight);
            float unityIntensity = Mathf.Lerp(0.02f, 6f, softDay);
            float middayFlatten = Mathf.SmoothStep(0.55f, 0.85f, daylight) * 0.5f;
            unityIntensity *= 1f - middayFlatten;
            unityIntensity = Mathf.Max(unityIntensity, 0.05f);

            //Vignette
            _vignette.color.Override(Color.black);
            _vignette.intensity.Override(SettingsManager.CurrentSettings.VignettingIntensity);
            _vignette.smoothness.Override(0.3f);

            //Color adjustments
            _baseContrast = Mathf.Lerp(3f, 25f, softDay);
            _baseSaturation = Mathf.Lerp(-10f, 10f, softDay);

            Color warmTint = new Color(1f, 0.92f, 0.83f);
            Color neutralTint = Color.white;
            _colorAdjustments.colorFilter.Override(Color.Lerp(warmTint, neutralTint, softDay));

            //Tonemapping
            _toneMapping.mode.Override(TonemappingMode.ACES);

            //Physically based sky
            _physicallyBasedSky.type.Override(PhysicallyBasedSky.PhysicallyBasedSkyModel.EarthAdvanced);
            _physicallyBasedSky.atmosphericScattering.Override(true);

            float lstDeg = GetLocalSiderealDegrees(dateTime, longitude);
            _physicallyBasedSky.spaceRotation.Override(new Vector3(0f, lstDeg, 0f));
            _physicallyBasedSky.spaceEmissionTexture.Override(_spaceBackground);

            //Stars
            float starFade = Mathf.InverseLerp(4f, -1f, sun.Elevation);
            float spaceEmission = Mathf.Lerp(0f, 5f, starFade);
            _physicallyBasedSky.spaceEmissionMultiplier.Override(spaceEmission);

            //Exposure
            float exposure;
            if (daylight < 0.15f)
            {
                exposure = Mathf.Lerp(0.82f, 0.95f, daylight / 0.15f);
            }
            else if (daylight > 0.8f)
            {
                float mid = Mathf.InverseLerp(0.8f, 1f, daylight);
                exposure = Mathf.Lerp(0.95f, 0.85f, mid);
            }
            else
            {
                exposure = 0.95f;
            }
            exposure = Mathf.Max(exposure, 0.82f);
            Shader.SetGlobalFloat("_GlobalExposureMultiplier", exposure);
            _baseExposure = exposure;

            Color nightSkyTint = new Color(0.03f, 0.04f, 0.07f);
            Color daySkyTint = new Color(0.9f, 0.95f, 1f);
            Color sunsetTint = new Color(1f, 0.78f, 0.55f);
            Color skyTint;

            if (sun.Elevation < 5f)
            {
                float warmFactor = Mathf.InverseLerp(-5f, 5f, sun.Elevation);
                skyTint = Color.Lerp(nightSkyTint, sunsetTint, warmFactor);
            }
            else
            {
                skyTint = Color.Lerp(sunsetTint, daySkyTint, Mathf.InverseLerp(5f, 45f, sun.Elevation));
            }

            _physicallyBasedSky.horizonTint.Override(skyTint);
            _physicallyBasedSky.zenithTint.Override(skyTint);
            _physicallyBasedSky.skyIntensityMode.Override(PhysicallyBasedSky.SkyIntensityMode.Exposure);

            // Ambient correction: stronger night twilight blend
            float twilightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 0f, sun.Elevation));
            float ambStrength = Mathf.Lerp(0.4f, 1f, daylight + twilightFactor * 0.5f); // min 0.4 la nuit

            if (isNight || daylight < 0.4f)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                Color skyA = Color.Lerp(new Color(0.1f, 0.12f, 0.20f), skyTint, 0.6f);
                Color eqA = Color.Lerp(new Color(0.08f, 0.10f, 0.15f), skyTint, 0.5f);
                Color grdA = new Color(0.04f, 0.04f, 0.05f);
                RenderSettings.ambientSkyColor = skyA * ambStrength;
                RenderSettings.ambientEquatorColor = eqA * ambStrength;
                RenderSettings.ambientGroundColor = grdA * ambStrength;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = Mathf.Clamp(ambStrength, 0.4f, 1.0f);
            }

            _physicallyBasedSky.aerosolDensity.Override(0.03f);
            _physicallyBasedSky.aerosolTint.Override(Color.white);
            _physicallyBasedSky.aerosolAnisotropy.Override(0.85f);
            _physicallyBasedSky.aerosolMaximumAltitude.Override(2000f);
            _physicallyBasedSky.horizonZenithShift.Override(0f);

            _fog.meanFreePath.Override(Mathf.Lerp(700f, 2200f, daylight));
            _fog.baseHeight.Override(0f);
            _fog.maximumHeight.Override(60f);
            _fog.maxFogDistance.Override(10000f);
            _fog.colorMode.Override(Fog.FogColorMode.SkyColor);
            _fog.tint.Override(skyTint);

            _visualEnvironment.skyType.Override((int)VisualEnvironment.SkyType.PhysicallyBased);
            _visualEnvironment.skyAmbientMode.Override(VisualEnvironment.SkyAmbientMode.Dynamic);
            _visualEnvironment.renderingSpace.Override(VisualEnvironment.RenderingSpace.Camera);

            _volumetricClouds.temporalAccumulationFactor.Override(1);
            _volumetricClouds.numPrimarySteps.Override(100);
            float lowLight = 1f - Mathf.SmoothStep(0.05f, 0.2f, daylight);
            float sunDimmer = Mathf.Lerp(0.6f, 1f, 1f - lowLight);
            float ambientProbeDimmer = Mathf.Lerp(0.7f, 1f, 1f - lowLight);
            _volumetricClouds.sunLightDimmer.Override(sunDimmer);
            _volumetricClouds.ambientLightProbeDimmer.Override(ambientProbeDimmer);

            Color sunColor;
            if (sun.Elevation < 8f)
            {
                float warmFactor = Mathf.InverseLerp(-3f, 8f, sun.Elevation);
                sunColor = Color.Lerp(new Color(1f, 0.68f, 0.38f), Color.white, warmFactor);
            }
            else
            {
                sunColor = Color.white;
            }

            float sunBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-10f, 4f, sun.Elevation));
            float moonBlend = 1f - sunBlend;

            //Sun setup
            _mainLight.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            _mainLight.color = sunColor;
            _mainLight.intensity = Mathf.Lerp(0.2f, unityIntensity, sunBlend);

            //Moon setup
            if (_moonLight != null)
            {
                _moonLight.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                float moonIntensity = Mathf.Lerp(0.15f, 0.35f, Mathf.Pow(moonBlend, 1.2f));
                _moonLight.intensity = moonIntensity;
                _moonLight.color = new Color(0.80f, 0.88f, 1.00f);
                _moonLight.shadows = LightShadows.Soft;
            }

            //Main light switch
            RenderSettings.sun = _mainLight;
            Light pipelineMain = (_mainLight.intensity >= (_moonLight != null ? _moonLight.intensity : 0f)) ? _mainLight : _moonLight;

            if (pipelineMain != null)
            {
                Shader.SetGlobalVector("_MainLightPosition", -pipelineMain.transform.forward);
                Shader.SetGlobalVector("_MainLightDirection", -pipelineMain.transform.forward);
                Shader.SetGlobalColor("_MainLightColor", pipelineMain.color * pipelineMain.intensity);
            }

            if (_mainLight != null)
            {
                _mainLight.shadows = LightShadows.Soft;
                _mainLight.shadowStrength = 1f;
                _mainLight.shadowBias = 0.05f;
                _mainLight.shadowNormalBias = 0.4f;
            }

            if (_moonLight != null)
            {
                _moonLight.shadows = LightShadows.Soft;
                _moonLight.shadowStrength = 0.25f;
                _moonLight.shadowBias = 0.2f;
                _moonLight.shadowNormalBias = 0.5f;
            }

            if (pipelineMain != null)
            {
                Shader.SetGlobalVector("_MainLightPosition", -pipelineMain.transform.forward);
                Shader.SetGlobalVector("_MainLightDirection", -pipelineMain.transform.forward);
                Shader.SetGlobalColor("_MainLightColor", pipelineMain.color * pipelineMain.intensity);
            }

            //Reflections
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.8f, 1.0f, daylight);

            //Lens flare
            if (_lensFlare != null)
            {
                _lensFlare.intensity = Mathf.Lerp(0.3f, 1.6f, daylight * daylight);
                _lensFlare.scale = Mathf.Lerp(0.8f, 1.2f, Mathf.Sqrt(daylight));
                _lensFlare.occlusionRadius = Mathf.Lerp(0.3f, 0.9f, daylight);
                _lensFlare.enabled = true;
            }

            //Apply user custom settings
            ApplyVignettingIntensity();
            ApplyContrast();
            ApplySaturation();
            ApplyExposure();
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
            if (_volumeProfile == null)
            {
                return;
            }

            _volumeProfile = ScriptableObject.Instantiate(_volumeProfile);

            //Create runtime volume if not existing
            if (_runtimeVolumeGO == null)
            {
                _runtimeVolumeGO = new GameObject("[Runtime] Environment Volume");
                _runtimeVolumeGO.hideFlags = HideFlags.DontSave;
                _runtimeVolume = _runtimeVolumeGO.AddComponent<Volume>();
                _runtimeVolume.isGlobal = true;
                _runtimeVolume.priority = RUNTIME_VOLUME_PRIORITY;
                _runtimeVolume.weight = 1f;
                _runtimeVolumeGO.layer = 0;
            }

            _runtimeVolume.profile = _volumeProfile;

            InitializePostProcessingComponents();

            //Enable all effects
            _vignette.active = true;
            _colorAdjustments.active = true;
            _toneMapping.active = true;

            _physicallyBasedSky.active = true;
            _fog.active = true;
            _fog.enabled.Override(true);
            _visualEnvironment.active = true;

            _volumetricClouds.state.Override(true);
            _volumetricClouds.active = true;

            //Clone all AnimationCurveParameter to avoid shared references between instances
            CloneAllCurvesInProfile(_volumeProfile);

            //Initialize cameras
            if (_reliveCamera != null)
            {
                _reliveCamera.clearFlags = CameraClearFlags.Skybox;
            }

            if (_povCamera != null)
            {
                _povCamera.clearFlags = CameraClearFlags.Skybox;
            }

            //Initialize lights
            if (_moonLight != null)
            {
                _moonLight.gameObject.hideFlags = HideFlags.DontSave;
                _moonLight.type = LightType.Directional;
                _moonLight.color = new Color(0.8f, 0.85f, 1f);
                _moonLight.intensity = 0f;
                _moonLight.shadows = LightShadows.None;
            }

            _environmentLoaded = true;
        }

        /// <summary>
        /// Deep clone all AnimationCurveParameter in a VolumeProfile to avoid shared references between instances.
        /// </summary>
        /// <param name="profile"></param>
        private static void CloneAllCurvesInProfile(VolumeProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            foreach (var comp in profile.components)
            {
                CloneVolumeCurves(comp);
            }
        }

        /// <summary>
        /// Deep clone all AnimationCurveParameter in a VolumeComponent to avoid shared references between instances.
        /// </summary>
        /// <param name="volume"></param>
        private static void CloneVolumeCurves(VolumeComponent volume)
        {
            if (volume == null)
            {
                return;
            }

            FieldInfo[] fields = volume.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo f in fields)
            {
                if (!typeof(VolumeParameter).IsAssignableFrom(f.FieldType))
                {
                    continue;
                }

                VolumeParameter param = f.GetValue(volume) as VolumeParameter;

                if (param is AnimationCurveParameter curveParam)
                {
                    var src = curveParam.value;
                    if (src == null)
                    {
                        continue;
                    }

                    //Deep clone: keys + wrap modes
                    var dst = new AnimationCurve(src.keys);
                    dst.preWrapMode = src.preWrapMode;
                    dst.postWrapMode = src.postWrapMode;

                    curveParam.value = dst;
                }
            }
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

            if (_moonLight != null)
            {
                _moonLight.intensity = 0f;
                _moonLight.color = Color.white;
            }

            if (_lensFlare != null)
            {
                _lensFlare.enabled = false;
            }

            RenderSettings.sun = null;
            RenderSettings.ambientMode = AmbientMode.Flat;

            _baseContrast = 0f;
            _baseSaturation = 0f;

            if (_runtimeVolume != null)
            {
                _runtimeVolume.profile = null;
            }
            if (_runtimeVolumeGO != null)
            {
                DestroyImmediate(_runtimeVolumeGO);
                _runtimeVolumeGO = null;
                _runtimeVolume = null;
            }
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
        /// Convert a DateTime into a normalized time-of-day value (0 - 1).
        /// </summary>
        /// <param name="ratio"></param>
        internal void ApplyTimeOfDay(float ratio)
        {
            ApplyTimeOfDay(_sceneTimeUTC, _latitude, _longitude, ratio);
        }

        /// <summary>
        /// Update sun position based on normalized time of day (0→1 = 00:00→23:59).
        /// </summary>
        private void ApplyTimeOfDay(DateTime utcDateTime, double latitude, double longitude, float normalized)
        {
            _dayRatio = normalized;
            DateTime newUtc = GetDateTimeFromNormalized(normalized, utcDateTime);
            ApplyEnvironment(newUtc, latitude, longitude);
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

        /// <summary>
        /// Apply exposure custom settings (URP)
        /// </summary>
        private void ApplyExposure()
        {
            if (_physicallyBasedSky != null)
            {
                float exposure = _baseExposure + (2f * SettingsManager.CurrentSettings.ExposureOffset);
                _physicallyBasedSky.exposure.Override(exposure);
            }
        }

        /// <summary>
        /// Apply clouds preset from user settings (URP)
        /// </summary>
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

        /// <summary>
        /// Apply cloud shadows enabled from user settings (URP)
        /// </summary>
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

        /// <summary>
        /// Apply cloud shadows opacity from user settings (URP)
        /// </summary>
        private void ApplyCloudShadowsOpacity()
        {
            if (_volumetricClouds != null)
            {
                float shadowOpacity = SettingsManager.CurrentSettings.CloudShadowsOpacity;
                _volumetricClouds.shadowOpacity.Override(shadowOpacity);
            }
        }

        /// <summary>
        /// Apply wind type from user settings (URP)
        /// </summary>
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
        /// <summary>
        /// Apply contrast custom settings (URP)
        /// </summary>
        /// <param name="offset"></param>
        private void OnContrastOffsetChanged(float offset)
        {
            ApplyContrast();
        }

        /// <summary>
        /// Apply saturation custom settings (URP)
        /// </summary>
        /// <param name="offset"></param>
        private void OnSaturationOffsetChanged(float offset)
        {
            ApplySaturation();
        }

        /// <summary>
        /// Apply exposure custom settings (URP)
        /// </summary>
        /// <param name="offset"></param>
        private void OnExposureOffsetChanged(float offset)
        {
            ApplyExposure();
        }

        /// <summary>
        /// Apply vignetting intensity from user settings (URP)
        /// </summary>
        /// <param name="intensity"></param>
        private void OnVignettingIntensityChanged(float intensity)
        {
            ApplyVignettingIntensity();
        }

        /// <summary>
        /// Apply clouds preset from user settings (URP)
        /// </summary>
        /// <param name="obj"></param>
        private void OnCloudsPresetChanged(CloudsPreset obj)
        {
            ApplyCloudsPreset();
        }

        /// <summary>
        /// Apply cloud shadows enabled from user settings (URP)
        /// </summary>
        /// <param name="obj"></param>
        private void OnCloudShadowsEnabledChanged(bool obj)
        {
            ApplyCloudShadowsEnabled();
        }

        /// <summary>
        /// Apply cloud shadows opacity from user settings (URP)
        /// </summary>
        /// <param name="obj"></param>
        private void OnCloudShadowsOpacityChanged(float obj)
        {
            ApplyCloudShadowsOpacity();
        }

        /// <summary>
        /// Apply wind type from user settings (URP)
        /// </summary>
        /// <param name="obj"></param>
        private void OnWindTypeChanged(WindType obj)
        {
            ApplyWindType();
        }

        /// <summary>
        /// Reset time of day to the original flight time.
        /// </summary>
        internal void ResetTimeOfDay()
        {
            _sceneTimeUTC = _flightTimeUTC;
            _dayRatio = GetNormalizedTimeOfDay(_sceneTimeUTC);
            ApplyEnvironment(_sceneTimeUTC, _latitude, _longitude);
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
        /// <summary>
        /// Draw post-processing settings in the settings window.
        /// </summary>
        /// <param name="layout"></param>
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

                //Display exposure custom settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Exposure",
                    "Define a custom exposure offset for the scene.",
                    $"Reset exposure offset to default value ({SettingsManager.EXPOSURE_OFFSET_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.ExposureOffset,
                    -1.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.EXPOSURE_OFFSET_DEFAULT_VALUE,
                    "%.2f",
                     (x) => SettingsManager.SaveExposureOffset(x),
                     () => SettingsManager.ResetExposureOffset());
            }
        }

        /// <summary>
        /// Draw sky & clouds settings in the settings window.
        /// </summary>
        /// <param name="layout"></param>
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
                    "Define the opacity of cloud shadows.",
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
            }
        }

        /// <summary>
        /// Draw buildings & POI settings in the settings window.
        /// </summary>
        /// <param name="layout"></param>
        internal void DrawSceneSettings(FuLayout layout)
        {
            layout.FramedText("Buildings");
            layout.Separator();

            BuildingManager.Instance.DisplayBuildingsSettings();

            layout.Separator();
            layout.FramedText("POI");
            layout.Separator();

            POIManager.Instance.DisplayPOISettings();
        }
        #endregion
    }
}
