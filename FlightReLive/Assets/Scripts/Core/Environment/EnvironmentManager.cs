using FlightReLive.Core.Building;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

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
        [SerializeField] private LensFlareComponentSRP _lensFlare;

        //HDRP volume + overrides
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
        private Bloom _bloom;
        private IndirectLightingController _indirectLighting;

        //Baseline values
        private float _baseExposureComp;
        private float _baseContrast;
        private float _baseSaturation;
        private float _baseIndirect;
        private float _baseMainLightIntensity;
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
            SettingsManager.OnExposureOffsetChanged += OnExposureOffsetChanged;
            SettingsManager.OnContrastOffsetChanged += OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged += OnSaturationOffsetChanged;
            SettingsManager.OnIndirectLightningOffsetChanged += OnIndirectOffsetChanged;
            SettingsManager.OnContactShadowsEnabledChanged += OnContactShadowsEnabledChanged;
            SettingsManager.OnContactShadowsMinDistanceChanged += OnContactShadowsMinDistanceChanged;
            SettingsManager.OnContactShadowsMaxDistanceChanged += OnContactShadowsMaxDistanceChanged;
            SettingsManager.OnContactShadowsOpacityChanged += OnContactShadowsOpacityChanged;
            SettingsManager.OnVignettingIntensityChanged += OnVignettingIntensityChanged;
            SettingsManager.OnCloudStyleChanged += OnCloudStyleChanged;

            UninitializedVolumeProfile();
        }

        private void OnDestroy()
        {
            SettingsManager.OnExposureOffsetChanged -= OnExposureOffsetChanged;
            SettingsManager.OnContrastOffsetChanged -= OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged -= OnSaturationOffsetChanged;
            SettingsManager.OnIndirectLightningOffsetChanged -= OnIndirectOffsetChanged;
            SettingsManager.OnContactShadowsEnabledChanged -= OnContactShadowsEnabledChanged;
            SettingsManager.OnContactShadowsMinDistanceChanged -= OnContactShadowsMinDistanceChanged;
            SettingsManager.OnContactShadowsMaxDistanceChanged -= OnContactShadowsMaxDistanceChanged;
            SettingsManager.OnContactShadowsOpacityChanged -= OnContactShadowsOpacityChanged;
            SettingsManager.OnVignettingIntensityChanged -= OnVignettingIntensityChanged;
            SettingsManager.OnCloudStyleChanged -= OnCloudStyleChanged;
        }
        #endregion

        #region METHODS : Public API
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

                ConfigureSceneRendering(flightUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
            });
        }

        /// <summary>
        /// Reset to a flat dark background, remove sky/fog/clouds until next Load.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                UninitializedVolumeProfile();
            });
        }
        #endregion

        #region METHODS : Core
        /// <summary>
        /// Build baseline from sun then apply user offsets.
        /// </summary>
        private void ConfigureSceneRendering(DateTime utcTime, double latitude, double longitude)
        {
            SunPosition sun = CalculateSunPosition(utcTime, latitude, longitude);
            CreateOrUpdateVolumeProfile(utcTime, sun);
            OrientMainLight(sun);

            //Apply user customs offsets
            ApplyVignettingIntensity();
            ApplyExposure();
            ApplyContrast();
            ApplySaturation();
            ApplyIndirectLightning();
            ApplyContactShadowsState();
            ApplyContactShadowsDistances();
            ApplyContactShadowsOpacity();
            ApplyCloudStyle();

        }

        /// <summary>
        /// Sun spherical coordinates for HDRP lighting decisions.
        /// </summary>
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

        /// <summary>
        /// Create (or reuse) the HDRP global volume and compute baseline from sun elevation.
        /// Baseline guarantees vivid, punchy look (quasi-HDR) without washed-out images.
        /// </summary>
        private void CreateOrUpdateVolumeProfile(DateTime utcTime, SunPosition sun)
        {
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, SettingsManager.CurrentSettings.UserTimeZone);
            int hour = localTime.Hour;

            //Processing elevation factor : 0 - horizon/night, 1 - zenith
            float elevationFactor = Mathf.Clamp01(sun.Elevation / 90f);

            //Create volume if needed
            if (_globalVolume == null)
            {
                _volumeInstance = new GameObject("Global Environment Volume (HDRP)");
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
                _bloom = _globalVolumeProfile.Add<Bloom>(true);
                _indirectLighting = _globalVolumeProfile.Add<IndirectLightingController>(true);

                // Cameras render sky (not solid color) in ReLive/POV
                if (_reliveCamera != null)
                {
                    HDAdditionalCameraData hdCam = _reliveCamera.GetComponent<HDAdditionalCameraData>();

                    if (hdCam != null)
                    { 
                        hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
                    }
                }
                if (_povCamera != null)
                {
                    HDAdditionalCameraData hdCam = _povCamera.GetComponent<HDAdditionalCameraData>();

                    if (hdCam != null) 
                    { 
                        hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
                    }
                }
            }

            //Exposure (automatic) + dynamic limits:
            //At low sun: allow darker min (avoid haze wash), cap highlights slightly
            //At high sun: raise max so whites can breathe, min less negative
            if (_exposure != null)
            {
                _exposure.mode.Override(ExposureMode.Fixed);
                _exposure.fixedExposure.overrideState = false;
                _baseExposureComp = Mathf.Lerp(5f, -0.5f, elevationFactor);
                _exposure.compensation.Override(_baseExposureComp);
            }

            //Visual Environment & wind flavor (subtle atmosphere variety)
            if (_visualEnvironment != null)
            {
                float windSpeed = hour >= 18 || hour < 6 ? 25f : 8f;

                _visualEnvironment.skyType.Override((int)SkyType.PhysicallyBased);
                _visualEnvironment.windOrientation.Override(29);
                _visualEnvironment.windSpeed.Override(windSpeed);
                _visualEnvironment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            }

            //Sky (Physically Based)
            if (_sky != null)
            {
                _sky.active = true;
                _sky.type.Override(PhysicallyBasedSkyModel.EarthAdvanced);
                _sky.atmosphericScattering.Override(true);
                _sky.renderingMode.Override(PhysicallyBasedSky.RenderingMode.Default);
                _sky.planetRotation.Override(Vector3.zero);
                _sky.aerosolTint.Override(Color.white);
                _sky.aerosolAnisotropy.Override(0.8f);
                _sky.aerosolMaximumAltitude.Override(8000f);
                _sky.aerosolDensity.Override(Mathf.Lerp(0.012f, 0.02f, elevationFactor));
                _sky.ozoneDensityDimmer.Override(Mathf.Lerp(0.9f, 1.15f, elevationFactor));
                _sky.colorSaturation.Override(1f);
                _sky.alphaSaturation.Override(1f);
                _sky.alphaMultiplier.Override(1f);
                _sky.horizonTint.Override(Color.white);
                _sky.horizonZenithShift.Override(0f);
                _sky.updateMode.Override(EnvironmentUpdateMode.Realtime);
                _sky.updatePeriod.Override(0f);
            }

            //Fog
            if (_fog != null)
            {
                _fog.active = true;
                _fog.enabled.Override(true);
                _fog.enableVolumetricFog.Override(true);
                _fog.meanFreePath.Override(1000f);
                _fog.baseHeight.overrideState = false;
                _fog.maximumHeight.Override(500f);
                _fog.maxFogDistance.overrideState = false;
                _fog.colorMode.Override(FogColorMode.SkyColor);
                _fog.tint.overrideState = false;
                _fog.albedo.overrideState = false;
                _fog.globalLightProbeDimmer.Override(1f);
                _fog.volumetricFogBudget = 64f;
                _fog.denoisingMode.Override(FogDenoisingMode.Gaussian);
            }

            //Clouds: light/quality preset
            if (_clouds != null)
            {
                VolumetricClouds.CloudPresets cloudPreset;

                switch (SettingsManager.CurrentSettings.CloudStyle)
                {
                    case CloudStyle.Sparse:
                        cloudPreset = VolumetricClouds.CloudPresets.Sparse;
                        break;
                    default:
                    case CloudStyle.Cloudy:
                        cloudPreset = VolumetricClouds.CloudPresets.Cloudy;
                        break;
                    case CloudStyle.Overcast:
                        cloudPreset = VolumetricClouds.CloudPresets.Overcast;
                        break;
                    case CloudStyle.Stormy:
                        cloudPreset = VolumetricClouds.CloudPresets.Stormy;
                        break;
                }

                _clouds.active = true;
                _clouds.enable.Override(true);
                _clouds.cloudControl.Override(VolumetricClouds.CloudControl.Simple);
                _clouds.cloudSimpleMode.Override(VolumetricClouds.CloudSimpleMode.Quality);
                _clouds.cloudPreset = cloudPreset;
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

            //Contact shadows
            if (_contactShadows != null)
            {
                bool enabled = SettingsManager.CurrentSettings.ContactShadowsEnabled;
                float minDist = SettingsManager.CurrentSettings.ContactShadowsMinDistance;
                float maxDist = SettingsManager.CurrentSettings.ContactShadowsMaxDistance;
                float opacity = SettingsManager.CurrentSettings.ContactShadowsOpacity;

                _contactShadows.enable.overrideState = enabled;
                _contactShadows.active = enabled;
                _contactShadows.enable.Override(enabled);
                _contactShadows.length.Override(0.2f);
                _contactShadows.distanceScaleFactor.Override(0.65f);
                _contactShadows.minDistance.Override(minDist);
                _contactShadows.maxDistance.Override(maxDist);
                _contactShadows.fadeInDistance.overrideState = false;
                _contactShadows.fadeDistance.overrideState = false;
                _contactShadows.opacity.Override(opacity);
                _contactShadows.rayBias.overrideState = false;
                _contactShadows.thicknessScale.overrideState = false;
                _contactShadows.sampleCount = 10;
            }

            //Color adjustments baseline
            if (_colorAdjustments != null)
            { 
                _baseContrast = 50f;
                _baseSaturation = 20f;
                _colorAdjustments.contrast.Override(_baseContrast);
                _colorAdjustments.saturation.Override(_baseSaturation);

                //We intentionally avoid postExposure here to keep exposure pipeline coherent
                _colorAdjustments.colorFilter.overrideState = false;
                _colorAdjustments.hueShift.overrideState = false;
            }

            //Vignette as an artistic control (absolute)
            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.mode.Override(VignetteMode.Procedural);
                _vignette.intensity.Override(SettingsManager.CurrentSettings.VignettingIntensity);
                _vignette.color.overrideState = false;
                _vignette.center.overrideState = false;
                _vignette.smoothness.overrideState = false;
                _vignette.roundness.overrideState = false;
                _vignette.rounded.overrideState = false;
            }

            //Bloom for highlights
            if (_bloom != null)
            {
                _bloom.threshold.Override(0.25f);
                _bloom.intensity.Override(0.12f);
                _bloom.tint.overrideState = false;
                _bloom.scatter.overrideState = false;
                _bloom.dirtIntensity.overrideState = false;
                _bloom.dirtTexture.overrideState = false;
            }

            //Indirect/Ambient baseline boost
            if (_indirectLighting != null)
            {
                _baseIndirect = Mathf.Lerp(5.5f, 1f, elevationFactor);
                _indirectLighting.indirectDiffuseLightingMultiplier.Override(_baseIndirect);
            }

            _globalVolume.enabled = true;

            //Lens flare baseline from elevation
            if (_lensFlare != null)
            {
                _baseLensFlareIntensity = Mathf.Lerp(2f, 3f, Mathf.Pow(elevationFactor, 3f));
                _baseLensFlareScale = Mathf.Lerp(0.5f, 1.5f, Mathf.Pow(elevationFactor, 0.3f));
                _baseLensFlareOccRadius = Mathf.Lerp(0.3f, 1f, elevationFactor);

                _lensFlare.intensity = _baseLensFlareIntensity;
                _lensFlare.scale = _baseLensFlareScale;
                _lensFlare.occlusionRadius = _baseLensFlareOccRadius;
                _lensFlare.attenuationByLightShape = true;
                _lensFlare.environmentOcclusion = true;
                _lensFlare.enabled = true;
            }
        }

        /// <summary>
        /// Orient the sun and compute baseline intensity from elevation.
        /// </summary>
        private void OrientMainLight(SunPosition sun)
        {
            if (_mainLight == null)
            { 
                return;
            }

            float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sun.Elevation;
            float elevationFactor = Mathf.Clamp01(sun.Elevation / 90f);

            Vector3 dir = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad));

            _mainLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            //Keep a physically plausible baseline, avoid cranking light; ambient will do the lifting
            _baseMainLightIntensity = Mathf.Lerp(0.2f, 4f, Mathf.Pow(elevationFactor, 1.4f));
            _mainLight.intensity = _baseMainLightIntensity;

            RenderSettings.sun = _mainLight;
        }


        /// <summary>
        /// Apply vignetting intensity from user settings
        /// </summary>
        private void ApplyVignettingIntensity()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _vignette == null)
            {
                return;
            }

            _vignette.intensity.Override(SettingsManager.CurrentSettings.VignettingIntensity);
            _vignette.active = true;
        }

        /// <summary>
        /// Apply contact shadows state from user settings
        /// </summary>
        private void ApplyContactShadowsState()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _contactShadows == null)
            {
                return;
            }

            bool state = SettingsManager.CurrentSettings.ContactShadowsEnabled;
            _contactShadows.enable.overrideState = state;
            _contactShadows.active = state;
            _contactShadows.enable.Override(state);
        }

        /// <summary>
        /// Apply contact shadows distances (min / max) from user settings
        /// </summary>
        private void ApplyContactShadowsDistances()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _contactShadows == null)
            {
                return;
            }

            if (_contactShadows != null)
            {
                _contactShadows.minDistance.Override(SettingsManager.CurrentSettings.ContactShadowsMinDistance);
                _contactShadows.maxDistance.Override(SettingsManager.CurrentSettings.ContactShadowsMaxDistance);
            }
        }

        /// <summary>
        /// Apply contact shadows opacity from user settings
        /// </summary>
        private void ApplyContactShadowsOpacity()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _contactShadows == null)
            {
                return;
            }

            if (_contactShadows != null)
            {
                _contactShadows.opacity.Override(SettingsManager.CurrentSettings.ContactShadowsOpacity);
            }
        }

        /// <summary>
        /// Apply exposure custom settings
        /// </summary>
        private void ApplyExposure()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _exposure == null)
            {
                return;
            }

            _exposure.compensation.Override(_baseExposureComp + SettingsManager.CurrentSettings.ExposureOffset);
        }

        /// <summary>
        /// Apply contrast custom settings
        /// </summary>
        private void ApplyContrast()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _colorAdjustments == null)
            {
                return;
            }

            _colorAdjustments.contrast.Override(_baseContrast + (20f * SettingsManager.CurrentSettings.ContrastOffset));
        }

        /// <summary>
        /// Apply saturation custom settings
        /// </summary>
        private void ApplySaturation()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _colorAdjustments == null)
            {
                return;
            }

            _colorAdjustments.saturation.Override(_baseSaturation + (20f * SettingsManager.CurrentSettings.SaturationOffset));
        }

        /// <summary>
        /// Apply indirect lightning custom settings
        /// </summary>
        private void ApplyIndirectLightning()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _indirectLighting == null)
            {
                return;
            }

            float indOffset = SettingsManager.CurrentSettings.IndirectLightningOffset;
            _indirectLighting.indirectDiffuseLightingMultiplier.Override(_baseIndirect + indOffset);
        }

        /// <summary>
        /// Apply cloud style from user settings
        /// </summary>
        private void ApplyCloudStyle()
        {
            if (_globalVolume == null || _globalVolumeProfile == null || _clouds == null)
            {
                return;
            }

            VolumetricClouds.CloudPresets cloudPreset;

            switch (SettingsManager.CurrentSettings.CloudStyle)
            {
                case CloudStyle.Sparse:
                    cloudPreset = VolumetricClouds.CloudPresets.Sparse;
                    break;
                default:
                case CloudStyle.Cloudy:
                    cloudPreset = VolumetricClouds.CloudPresets.Cloudy;
                    break;
                case CloudStyle.Overcast:
                    cloudPreset = VolumetricClouds.CloudPresets.Overcast;
                    break;
                case CloudStyle.Stormy:
                    cloudPreset = VolumetricClouds.CloudPresets.Stormy;
                    break;
            }

            _clouds.cloudPreset = cloudPreset;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Reset cameras to color clear, destroy volume & profile, disable lens flare.
        /// </summary>
        private void UninitializedVolumeProfile()
        {
            if (_reliveCamera != null)
            {
                HDAdditionalCameraData hdCam = _reliveCamera.GetComponent<HDAdditionalCameraData>();

                if (hdCam != null)
                { 
                    hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                }

                _reliveCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            if (_povCamera != null)
            {
                HDAdditionalCameraData hdCam = _povCamera.GetComponent<HDAdditionalCameraData>();

                if (hdCam != null)
                {
                    hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                }

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

            _exposure = null;
            _visualEnvironment = null;
            _sky = null;
            _fog = null;
            _clouds = null;
            _contactShadows = null;
            _colorAdjustments = null;
            _vignette = null;
            _bloom = null;
            _indirectLighting = null;
            _baseExposureComp = 0f;
            _baseContrast = 0f;
            _baseSaturation = 0f;
            _baseIndirect = 0f;
            _baseMainLightIntensity = 0f;

            RenderSettings.sun = null;

            if (_lensFlare != null)
            {
                _lensFlare.enabled = false;
            }
        }
        #endregion

        #region CALLBACKS
        private void OnExposureOffsetChanged(float offset)
        {
            ApplyExposure();
        }

        private void OnContrastOffsetChanged(float offset)
        {
            ApplyContrast();
        }

        private void OnSaturationOffsetChanged(float offset)
        {
            ApplySaturation();
        }

        private void OnIndirectOffsetChanged(float offset)
        {
            ApplyIndirectLightning();
        }

        private void OnVignettingIntensityChanged(float intensity)
        {
            ApplyVignettingIntensity();
        }

        private void OnContactShadowsEnabledChanged(bool state)
        {
            ApplyContactShadowsState();
        }

        private void OnContactShadowsMinDistanceChanged(float min)
        {
            ApplyContactShadowsDistances();
        }

        private void OnContactShadowsMaxDistanceChanged(float max)
        {
            ApplyContactShadowsDistances();
        }

        private void OnContactShadowsOpacityChanged(float opacity)
        {
            ApplyContactShadowsOpacity();
        }

        private void OnCloudStyleChanged(CloudStyle cloudStyle)
        {
            ApplyCloudStyle();
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
            layout.FramedText("Sky");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridEnvOffsets", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                SettingsManager.DisplaySettingsComboboxWithReset<CloudStyle>(
                grid,
                "Sky type",
                "Change the type of sky in the scene.",
                "Reset the type of sky to default value",
                SettingsManager.CurrentSettings.CloudStyle,
                SettingsManager.CLOUD_STYLE_DEFAULT_VALUE,
                SettingsManager.GetEnumLabel,
                Enum.GetValues(typeof(CloudStyle)).Cast<CloudStyle>(),
                (x) => SettingsManager.SaveCloudStyle(x),
                () => SettingsManager.ResetCloudStyle()
                );
            }

            layout.Separator();
            layout.FramedText("Lighting & Color");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridEnvOffsets", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
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

                //Indirect lightning settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Indirect lightning",
                    "Define a custom indirect lightning offset for the scene.",
                    $"Reset indirect lightning offset to default value ({SettingsManager.INDIRECT_LIGHTNING_OFFSET_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.IndirectLightningOffset,
                    -1.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.INDIRECT_LIGHTNING_OFFSET_DEFAULT_VALUE,
                    "%.2f",
                     (x) => SettingsManager.SaveIndirectLightningOffset(x),
                     () => SettingsManager.ResetIndirectLightningOffset());
            }

            layout.Separator();
            layout.FramedText("Contact shadows");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridContact", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                bool enabled = SettingsManager.CurrentSettings.ContactShadowsEnabled;

                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Contact shadows",
                    "Enable or disable contact shadows on terrain.",
                    "Reset contact shadows state to default value.",
                    enabled,
                    SettingsManager.CONTACT_SHADOWS_ENABLED_DEFAULT_VALUE,
                    (x) => SettingsManager.SaveContactShadowsEnabled(x),
                    () => SettingsManager.ResetContactShadowsEnabled());

                if (!enabled)
                {
                    grid.DisableNextElements();
                }

                SettingsManager.DisplaySettingsRangeWithReset(grid,
                    "Min / max distances",
                    "Select minimum / maximum distances of contact shadows.",
                    $"Reset minimum / maximum contact shadows distances to default value (Min: {SettingsManager.CONTACT_SHADOWS_MIN_DISTANCE_DEFAULT_VALUE} / Max:{SettingsManager.CONTACT_SHADOWS_MAX_DISTANCE_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.ContactShadowsMinDistance,
                    SettingsManager.CurrentSettings.ContactShadowsMaxDistance,
                    0f,
                    1000f,
                    1f,
                    SettingsManager.CONTACT_SHADOWS_MIN_DISTANCE_DEFAULT_VALUE,
                    SettingsManager.CONTACT_SHADOWS_MAX_DISTANCE_DEFAULT_VALUE,
                    (min, max) =>
                    {
                        SettingsManager.SaveContactShadowsMinDistance(min);
                        SettingsManager.SaveContactShadowsMaxDistance(max);
                    },
                    () =>
                    {
                        SettingsManager.ResetContactShadowsMinDistance();
                        SettingsManager.ResetContactShadowsMaxDistance();
                    });

                //Contact shadows opacity settings
                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "Opacity",
                    "Define contact shadows opacity.",
                    $"Reset contact shadows opacity to default value ({SettingsManager.CONTACT_SHADOWS_OPACITY_DEFAULT_VALUE}).",
                    SettingsManager.CurrentSettings.ContactShadowsOpacity,
                    0.0f,
                    1.0f,
                    0.01f,
                    SettingsManager.CONTACT_SHADOWS_OPACITY_DEFAULT_VALUE,
                    "%.2f",
                     (x) => SettingsManager.SaveContactShadowsOpacity(x),
                     () => SettingsManager.ResetContactShadowsOpacity());
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
