using FlightReLive.Core.Building;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MKEdgeDetection = MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection;

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
        [SerializeField] private VolumeProfile _volumeProfile;
        [SerializeField] private MKEdgeDetection _edgeDetection;

        //Sun
        private Gradient _sunColorGradient;
        private Gradient _ambientGradient;

        //Post-processing elements
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;

        //Baseline values
        private float _baseContrast;
        private float _baseSaturation;
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

            _baseContrast = 0.0f;
            _baseSaturation = 0.0f;

            if (_volumeProfile != null && _volumeProfile.TryGet(out Vignette vignette))
            {
                _vignette = vignette;
                _vignette.active = false;
            }

            if (_volumeProfile != null && _volumeProfile.TryGet(out ColorAdjustments colorAdjusments))
            {
                _colorAdjustments = colorAdjusments;
                _colorAdjustments.active = false;
            }

            if (_volumeProfile != null && _volumeProfile.TryGet(out MKEdgeDetection edgeDetection))
            {
                _edgeDetection = edgeDetection;
                _edgeDetection.active = false;
            }

            //Initialize gradient
            InitializeSunGradients();
        }

        private void Start()
        {
            SettingsManager.OnContrastOffsetChanged += OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged += OnSaturationOffsetChanged;
            SettingsManager.OnVignettingIntensityChanged += OnVignettingIntensityChanged;
            SettingsManager.OnOutlineEnabledChanged += OnOutlineEnabledChanged;

            UninitializedVolumeProfile();
        }

        private void OnDestroy()
        {
            SettingsManager.OnContrastOffsetChanged -= OnContrastOffsetChanged;
            SettingsManager.OnSaturationOffsetChanged -= OnSaturationOffsetChanged;
            SettingsManager.OnVignettingIntensityChanged -= OnVignettingIntensityChanged;
            SettingsManager.OnOutlineEnabledChanged -= OnOutlineEnabledChanged;
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

                TimeZoneInfo userTimeZone = SettingsManager.CurrentSettings.UserTimeZone;
                DateTime localTime = DateTime.SpecifyKind(flightData.Date, DateTimeKind.Unspecified);
                DateTime flightUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, userTimeZone);

                ConfigureSceneRendering(flightUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
                UpdateLighting(flightUtc, flightData.GPSOrigin.Latitude, flightData.GPSOrigin.Longitude);
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
            ApplyContrast();
            ApplySaturation();
            ApplyOutline();
        }

        /// <summary>
        /// Computes precise sun position (azimuth/elevation) from UTC date, latitude and longitude using NOAA algorithm.
        /// </summary>
        public static SunPosition CalculateSunPosition(DateTime utcTime, double latitude, double longitude)
        {
            //Convert to Julian Day
            double julianDay = utcTime.ToOADate() + 2415018.5;
            double julianCentury = (julianDay - 2451545.0) / 36525.0;

            //Mean longitude, anomaly, eccentricity
            double geomMeanLongSun = (280.46646 + julianCentury * (36000.76983 + julianCentury * 0.0003032)) % 360.0;
            double geomMeanAnomSun = 357.52911 + julianCentury * (35999.05029 - 0.0001537 * julianCentury);
            double eccentEarthOrbit = 0.016708634 - julianCentury * (0.000042037 + 0.0000001267 * julianCentury);

            //Sun equation of center
            double sunEqOfCenter = Math.Sin(Mathf.Deg2Rad * (float)geomMeanAnomSun) * (1.914602 - julianCentury * (0.004817 + 0.000014 * julianCentury))
                                 + Math.Sin(Mathf.Deg2Rad * (float)(2 * geomMeanAnomSun)) * (0.019993 - 0.000101 * julianCentury)
                                 + Math.Sin(Mathf.Deg2Rad * (float)(3 * geomMeanAnomSun)) * 0.000289;

            //True longitude
            double sunTrueLong = geomMeanLongSun + sunEqOfCenter;

            //Apparent longitude (correction nutation + aberration)
            double omega = 125.04 - 1934.136 * julianCentury;
            double sunAppLong = sunTrueLong - 0.00569 - 0.00478 * Math.Sin(Mathf.Deg2Rad * (float)omega);

            //Mean obliquity of ecliptic
            double meanObliqEcliptic = 23.0 + (26.0 + ((21.448 - julianCentury * (46.815 + julianCentury * (0.00059 - julianCentury * 0.001813)))) / 60.0) / 60.0;
            double obliqCorr = meanObliqEcliptic + 0.00256 * Math.Cos(Mathf.Deg2Rad * (float)omega);

            //Declination
            double declination = Math.Asin(Math.Sin(Mathf.Deg2Rad * (float)obliqCorr) * Math.Sin(Mathf.Deg2Rad * (float)sunAppLong));

            //Equation of time (in minutes)
            double y = Math.Tan(Mathf.Deg2Rad * (float)(obliqCorr / 2.0)) * Math.Tan(Mathf.Deg2Rad * (float)(obliqCorr / 2.0));
            double eqTime = 4.0 * (y * Math.Sin(2.0 * Mathf.Deg2Rad * (float)geomMeanLongSun)
                - 2.0 * eccentEarthOrbit * Math.Sin(Mathf.Deg2Rad * (float)geomMeanAnomSun)
                + 4.0 * eccentEarthOrbit * y * Math.Sin(Mathf.Deg2Rad * (float)geomMeanAnomSun) * Math.Cos(2.0 * Mathf.Deg2Rad * (float)geomMeanLongSun)
                - 0.5 * y * y * Math.Sin(4.0 * Mathf.Deg2Rad * (float)geomMeanLongSun)
                - 1.25 * eccentEarthOrbit * eccentEarthOrbit * Math.Sin(2.0 * Mathf.Deg2Rad * (float)geomMeanAnomSun));

            //True solar time (degrees)
            double timeOffset = eqTime + 4.0 * longitude - 0.0; // UTC offset already zero
            double trueSolarTime = (utcTime.TimeOfDay.TotalMinutes + timeOffset) % 1440.0;

            //Hour angle
            double hourAngle = (trueSolarTime / 4.0 < 0) ? trueSolarTime / 4.0 + 180.0 : trueSolarTime / 4.0 - 180.0;

            //Elevation
            double haRad = Mathf.Deg2Rad * (float)hourAngle;
            double latRad = Mathf.Deg2Rad * (float)latitude;
            double declRad = declination;
            double elevationRad = Math.Asin(Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad));

            double elevation = elevationRad * Mathf.Rad2Deg;

            //Azimuth
            double azimuth = (Math.Atan2(Math.Sin(haRad), Math.Cos(haRad) * Math.Sin(latRad) - Math.Tan(declRad) * Math.Cos(latRad)) * Mathf.Rad2Deg + 180.0) % 360.0;
            float unityAzimuth = (float)((360.0 - azimuth) % 360.0);

            float factor = Mathf.Clamp01((float)((elevation + 6.0) / 96.0)); // -6°=twilight start, 90°=zenith

            return new SunPosition
            {
                Elevation = (float)elevation,
                Azimuth = unityAzimuth,
                AzimuthPhysical = (float)azimuth,
                DistanceFactor = factor
            };
        }

        private void InitializeSunGradients()
        {
            //Sun color
            _sunColorGradient = new Gradient();
            _sunColorGradient.SetKeys(new GradientColorKey[] 
                {
                    new GradientColorKey(new Color(0.05f, 0.1f, 0.3f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.35f, 0.1f), 0.25f),
                    new GradientColorKey(new Color(1.0f, 0.95f, 0.8f), 0.6f),
                    new GradientColorKey(new Color(1.0f, 0.9f, 0.7f), 1f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            //Sun intensity
            _ambientGradient = new Gradient();
            _ambientGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.01f, 0.02f, 0.05f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.25f, 0.35f), 0.25f),
                    new GradientColorKey(new Color(0.5f, 0.6f, 0.7f), 0.6f),
                    new GradientColorKey(new Color(0.8f, 0.85f, 0.9f), 1f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
        }

        private void UpdateLighting(DateTime currentDateUTC, double latitude, double longitude)
        {
            SunPosition sunPosition = CalculateSunPosition(currentDateUTC, latitude, longitude);

            OrientSun(sunPosition);
            UpdateLightingColors(sunPosition);
        }

        private void OrientSun(SunPosition sunPosition)
        {
            if (_mainLight == null)
            {
                return;
            }

            float azimuthRad = Mathf.Deg2Rad * sunPosition.AzimuthPhysical;
            float elevationRad = Mathf.Deg2Rad * sunPosition.Elevation;

            Vector3 dir = new Vector3(
                Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad),
                Mathf.Sin(elevationRad),
                Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad)
            );

            _mainLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            float intensity = Mathf.Lerp(0.02f, 3.5f, Mathf.SmoothStep(0f, 1f, sunPosition.DistanceFactor));
            _mainLight.intensity = intensity;
        }

        private void UpdateLightingColors(SunPosition sunPosition)
        {
            float f = sunPosition.DistanceFactor;

            _mainLight.color = _sunColorGradient.Evaluate(f);

            RenderSettings.ambientLight = _ambientGradient.Evaluate(f);
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetColor("_Tint", _ambientGradient.Evaluate(f));
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(0.5f, 1.2f, f));
            }

            DynamicGI.UpdateEnvironment();
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

            if (_mainLight != null)
            {
                float azimuthRad = Mathf.Deg2Rad * sun.AzimuthPhysical;
                float elevationRad = Mathf.Deg2Rad * sun.Elevation;
                float elevationFactor = Mathf.Clamp01(sun.Elevation / 90f);

                Vector3 dir = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad));
                _mainLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                float cappedFactor = Mathf.Pow(elevationFactor, 1.2f);
                _baseMainLightIntensity = Mathf.Lerp(0.2f, 3.5f, cappedFactor);
                _mainLight.intensity = _baseMainLightIntensity;
                RenderSettings.sun = _mainLight;
            }
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

        /// <summary>
        /// Apply saturation custom settings (URP)
        /// </summary>
        private void ApplyOutline()
        {
            if (_edgeDetection != null)
            {
                _edgeDetection.active = SettingsManager.CurrentSettings.OutlineEnabled;
            }
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
                _reliveCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            if (_povCamera != null)
            {
                _povCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            RenderSettings.sun = null;

            if (_lensFlare != null)
            {
                _lensFlare.enabled = false;
            }

            _baseContrast = 0f;
            _baseSaturation = 0f;
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

        private void OnOutlineEnabledChanged(bool obj)
        {
            ApplyOutline();
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
            layout.FramedText("Outline");
            layout.Separator();

            using (FuGrid grid = new FuGrid("gridVignettingOffset", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                //Display outline custom settings
                bool outline = SettingsManager.CurrentSettings.OutlineEnabled;

                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Outline",
                    "Enabled or disabled outline on 3D elements.",
                    $"Reset outline state to default value ({SettingsManager.OUTLINE_DISPLAY_STATE_DEFAULT_VALUE}).",
                    outline,
                    SettingsManager.OUTLINE_DISPLAY_STATE_DEFAULT_VALUE,
                     (x) => SettingsManager.SaveOutlineEnabled(x),
                     () => SettingsManager.ResetOutlineEnabled());
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
