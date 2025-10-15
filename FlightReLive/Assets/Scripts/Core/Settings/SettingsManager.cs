using FlightReLive.Core.Cache;
using FlightReLive.Core.Loading;
using FlightReLive.UI;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FlightReLive.Core.Settings
{
    public static class SettingsManager
    {
        #region ATTRIBUTES
        internal static float CAMERA_ROTATION_SPEED_DEFAULT_VALUE = 0.25f;
        internal static float CAMERA_ZOOM_SPEED_DEFAULT_VALUE = 1f;
        internal static float PAN_SPEED_DEFAULT_VALUE = 0.25f;
        internal static float PATH_3D_THICKNESS_DEFAULT_VALUE = 0.4f;
        internal static Color PATH_3D_REMAINING_COLOR_DEFAULT_VALUE = Color.white;
        internal static bool BUILDING_DISPLAY_STATE_DEFAULT_VALUE = true;
        internal static Color BUILDING_COLOR_DEFAULT_VALUE = Color.antiqueWhite;
        internal static float BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE = 0.9f;
        internal static bool POI_DISPLAY_STATE_DEFAULT_VALUE = true;
        internal static float POI_SCALE_DEFAULT_VALUE = 0.5f;
        internal static float POI_HEIGHT_DEFAULT_VALUE = 1.5f;
        internal static float POI_MIN_FADE_DISTANCE_DEFAULT_VALUE = 100f;
        internal static float POI_MAX_FADE_DISTANCE_DEFAULT_VALUE = 250f;
        internal static float VIGNETTING_DEFAULT_VALUE = 0.3f;
        internal static float CONTRAST_OFFSET_DEFAULT_VALUE = 0f;
        internal static float SATURATION_OFFSET_DEFAULT_VALUE = 0f;
        internal static float EXPOSURE_OFFSET_DEFAULT_VALUE = 0f;
        internal static CloudsPreset CLOUD_PRESET_DEFAULT_VALUE = CloudsPreset.Sparse;
        internal static bool CLOUD_SHADOW_ENABLED_DEFAULT_STATE = true;
        internal static float CLOUD_SHADOW_OPACITY_DEFAULT_STATE = 0.5f;
        internal static WindType WIND_TYPE_DEFAULT_VALUE = WindType.Slow;
        internal static int CAPTURE_RESOLUTION_DEFAULT_VALUE = 1;
        internal static int CAPTURE_ENCODER_DEFAULT_VALUE = 0;
        internal static int CAPTURE_FRAMERATE_DEFAULT_VALUDE = 1;
        internal static bool CAPTURE_ENCODED_LOGO_DEFAULT_VALUE = true;
        internal static string CAPTURE_OUTPUT_PATH_DEFAULT_VALUE = Path.Combine(Application.persistentDataPath, "Captures");

        private static float[] _availableUIScale = new float[] { 1f, 1.25f, 1.50f, 1.75f, 2.0f, 2.25f, 2.5f };
        private static readonly Dictionary<string, string> TimeZoneIdMap = new Dictionary<string, string>
        {
            { "Europe/Paris", "Romance Standard Time" },
            { "Europe/London", "GMT Standard Time" },
            { "America/New_York", "Eastern Standard Time" },
            { "America/Los_Angeles", "Pacific Standard Time" },
            { "Asia/Tokyo", "Tokyo Standard Time" },
            { "Asia/Shanghai", "China Standard Time" },
            { "Asia/Kolkata", "India Standard Time" },
            { "Australia/Sydney", "AUS Eastern Standard Time" },
            { "UTC", "UTC" }
        };

        private static bool _settingsOpened = false;
        #endregion

        #region PROPERTIES
        internal static float[] AvailableUIScale
        {
            get
            {
                return _availableUIScale;
            }
        }

        public static Settings CurrentSettings { get; private set; } = new Settings();

        #endregion

        #region EVENTS
        public static event Action<int> OnApplicationTargetFPSChanged;
        public static event Action<int> OnApplicationIdleFPSChanged;
        public static event Action<bool> OnDontAskWelcomeVersionChanged;
        public static event Action<float> OnCameraRotationSpeedChanged;
        public static event Action<float> OnCameraZoomSpeedChanged;
        public static event Action<float> OnPanSpeedChanged;
        public static event Action<TimeZoneInfo> OnTimeZoneChanged;
        public static event Action<DateFormatStyle> OnDateFormatStyleChanged;
        public static event Action<TimeFormatStyle> OnTimeFormatStyleChanged;
        public static event Action<UnitSystemType> OnUnitSystemTypeChanged;
        public static event Action<string> OnWorkspacePathChanged;
        public static event Action<float> OnWorkspaceZoomChanged;
        public static event Action<string> OnMapTilerApiKeyChanged;
        public static event Action<float> OnGlobalScaleChanged;
        public static event Action<float> OnPath3DWidthChanged;
        public static event Action<Color> OnPath3DRemainingColorChanged;
        public static event Action<bool> OnBuildingVisibilityChanged;
        public static event Action<Color> OnBuildingColorChanged;
        public static event Action<float> OnBuildingAOChanged;
        public static event Action<bool> OnPOIVisibilityChanged;
        public static event Action<float> OnPOIScaleChanged;
        public static event Action<float> OnPOIHeightChanged;
        public static event Action<float> OnPOIMinFadeDistanceChanged;
        public static event Action<float> OnPOIMaxFadeDistanceChanged;
        public static event Action<float> OnVignettingIntensityChanged;
        public static event Action<float> OnContrastOffsetChanged;
        public static event Action<float> OnSaturationOffsetChanged;
        public static event Action<float> OnExposureOffsetChanged;
        public static event Action<CloudsPreset> OnCloudsPresetChanged;
        public static event Action<bool> OnCloudShadowsEnabledChanged;
        public static event Action<float> OnCloudShadowsOpacityChanged;
        public static event Action<WindType> OnWindTypeChanged;
        public static event Action<int> OnCaptureResolutionChanged;
        public static event Action<int> OnCaptureEncoderChanged;
        public static event Action<int> OnCaptureFramerateChanged;
        public static event Action<bool> OnCaptureEncodedLogoChanged;
        public static event Action<string> OnCaptureOutputPathChanged;
        #endregion

        #region METHODS
        /// <summary>
        /// Load the application display wizard setting from PlayerPrefs, defaulting to true if not set.
        /// </summary>
        internal static void LoadDisplayWizard()
        {
            CurrentSettings.DisplayWizard = PlayerPrefs.GetInt(nameof(Settings.DisplayWizard), 1) == 1;
        }

        /// <summary>
        /// Load the application target FPS setting from PlayerPrefs, defaulting to 120 if not set.
        /// </summary>
        internal static void LoadApplicationTargetFPS()
        {
            CurrentSettings.ApplicationTargetFPS = PlayerPrefs.GetInt(nameof(Settings.ApplicationTargetFPS), 120);
        }

        /// <summary>
        /// Load the application idle FPS setting from PlayerPrefs, defaulting to 30 if not set.
        /// </summary>
        internal static void LoadApplicationIdleFPS()
        {
            CurrentSettings.ApplicationIdleFPS = PlayerPrefs.GetInt(nameof(Settings.ApplicationIdleFPS), 30);
        }

        /// <summary>
        /// Load the "Don't Ask Welcome Version" setting from PlayerPrefs, defaulting to false if not set.
        /// </summary>
        internal static void LoadDontAskWelcomeVersion()
        {
            CurrentSettings.DontAskWelcomeVersion = PlayerPrefs.GetInt(nameof(Settings.DontAskWelcomeVersion), 0) == 1;
        }

        /// <summary>
        /// Load the camera rotation speed setting from PlayerPrefs, defaulting to CAMERA_ROTATION_SPEED_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCameraRotationSpeed()
        {
            CurrentSettings.CameraRotationSpeed = PlayerPrefs.GetFloat(nameof(Settings.CameraRotationSpeed), CAMERA_ROTATION_SPEED_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the camera zoom speed setting from PlayerPrefs, defaulting to CAMERA_ZOOM_SPEED_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCameraZoomSpeed()
        {
            CurrentSettings.CameraZoomSpeed = PlayerPrefs.GetFloat(nameof(Settings.CameraZoomSpeed), CAMERA_ZOOM_SPEED_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the pan speed setting from PlayerPrefs, defaulting to PAN_SPEED_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPanSpeed()
        {
            CurrentSettings.PanSpeed = PlayerPrefs.GetFloat(nameof(Settings.PanSpeed), PAN_SPEED_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the time zone setting from PlayerPrefs, defaulting to "UTC" if not set.
        /// </summary>
        internal static void LoadTimeZone()
        {
            string tzId = PlayerPrefs.GetString(nameof(Settings.UserTimeZone), "UTC");
            CurrentSettings.UserTimeZone = ResolveTimeZone(tzId);
        }

        /// <summary>
        /// Resolve the TimeZoneInfo from the given ID, handling platform differences.
        /// </summary>
        internal static void LoadDateFormatStyle()
        {
            CurrentSettings.DateFormatStyle = (DateFormatStyle)PlayerPrefs.GetInt(nameof(Settings.DateFormatStyle), (int)DateFormatStyle.European);
        }

        /// <summary>
        /// Load the time format style setting from PlayerPrefs, defaulting to TwentyFourHour if not set.
        /// </summary>
        internal static void LoadTimeFormatStyle()
        {
            CurrentSettings.TimeFormatStyle = (TimeFormatStyle)PlayerPrefs.GetInt(nameof(Settings.TimeFormatStyle), (int)TimeFormatStyle.TwentyFourHour);
        }

        /// <summary>
        /// Load the unit system type setting from PlayerPrefs, defaulting to Metric if not set.
        /// </summary>
        internal static void LoadUnitSystemType()
        {
            CurrentSettings.UnitSystemType = (UnitSystemType)PlayerPrefs.GetInt(nameof(Settings.UnitSystemType), (int)UnitSystemType.Metric);
        }

        /// <summary>
        /// Load the workspace path setting from PlayerPrefs, defaulting to Application.persistentDataPath if not set.
        /// </summary>
        internal static void LoadWorkspacePath()
        {
            CurrentSettings.WorkspacePath = PlayerPrefs.GetString(nameof(Settings.WorkspacePath), Application.persistentDataPath);
        }

        /// <summary>
        /// Load the workspace zoom setting from PlayerPrefs, defaulting to 1.0f if not set.    
        /// </summary>
        internal static void LoadWorkspaceZoom()
        {
            CurrentSettings.WorkspaceZoom = PlayerPrefs.GetFloat(nameof(Settings.WorkspaceZoom), 1.0f);
        }

        /// <summary>
        /// Load the MapTiler API key setting from PlayerPrefs, defaulting to an empty string if not set.
        /// </summary>
        internal static void LoadMapTilerApiKey()
        {
            CurrentSettings.MapTilerAPIKey = PlayerPrefs.GetString(nameof(Settings.MapTilerAPIKey), "");
        }

        /// <summary>
        /// Load the global scale setting from PlayerPrefs, defaulting to 1f if not set.
        /// </summary>
        internal static void LoadGlobalScale()
        {
            CurrentSettings.GlobalScale = PlayerPrefs.GetFloat(nameof(Settings.GlobalScale), 1f);
        }

        /// <summary>
        /// Load the path 3D thickness setting from PlayerPrefs, defaulting to PATH_3D_THICKNESS_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPath3DThickness()
        {
            CurrentSettings.Path3DThickness = PlayerPrefs.GetFloat(nameof(Settings.Path3DThickness), PATH_3D_THICKNESS_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the path 3D remaining color setting from PlayerPrefs, defaulting to PATH_3D_REMAINING_COLOR_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPath3DRemainingColor()
        {
            Color color = PATH_3D_REMAINING_COLOR_DEFAULT_VALUE;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            string savedColorString = PlayerPrefs.GetString(nameof(Settings.Path3DRemainingColor), colorString);
            string[] rgba = savedColorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.Path3DRemainingColor = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.Path3DRemainingColor = color;
            }
        }

        /// <summary>
        /// Load the building visibility setting from PlayerPrefs, defaulting to BUILDING_DISPLAY_STATE_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadBuildingVisibility()
        {
            int intBool = BUILDING_DISPLAY_STATE_DEFAULT_VALUE ? 1 : 0;
            CurrentSettings.BuildingVisibility = PlayerPrefs.GetInt(nameof(Settings.BuildingVisibility), intBool) == 1;
        }

        /// <summary>
        /// Load the building color setting from PlayerPrefs, defaulting to BUILDING_COLOR_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadBuildingColor()
        {
            Color color = BUILDING_COLOR_DEFAULT_VALUE;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            string savedColorString = PlayerPrefs.GetString(nameof(Settings.BuildingColor), colorString);
            string[] rgba = savedColorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.BuildingColor = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.BuildingColor = color;
            }
        }

        /// <summary>
        /// Load the building ambient occlusion setting from PlayerPrefs, defaulting to BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadBuildingAO()
        {
            CurrentSettings.BuildingAO = PlayerPrefs.GetFloat(nameof(Settings.BuildingAO), BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the POI visibility setting from PlayerPrefs, defaulting to POI_DISPLAY_STATE_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPOIVisibility()
        {
            int intBool = POI_DISPLAY_STATE_DEFAULT_VALUE ? 1 : 0;
            CurrentSettings.POIVisibility = PlayerPrefs.GetInt(nameof(Settings.POIVisibility), intBool) == 1;
        }

        /// <summary>
        /// Load the POI scale setting from PlayerPrefs, defaulting to POI_SCALE_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPOIScale()
        {
            CurrentSettings.POIScale = PlayerPrefs.GetFloat(nameof(Settings.POIScale), POI_SCALE_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the POI height setting from PlayerPrefs, defaulting to POI_HEIGHT_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPOIHeight()
        {
            CurrentSettings.POIHeight = PlayerPrefs.GetFloat(nameof(Settings.POIHeight), POI_HEIGHT_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the POI min fade distance setting from PlayerPrefs, defaulting to POI_MIN_FADE_DISTANCE_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPOIMinFadeDistance()
        {
            CurrentSettings.POIMinFadeDistance = PlayerPrefs.GetFloat(nameof(Settings.POIMinFadeDistance), POI_MIN_FADE_DISTANCE_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the POI max fade distance setting from PlayerPrefs, defaulting to POI_MAX_FADE_DISTANCE_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadPOIMaxFadeDistance()
        {
            CurrentSettings.POIMaxFadeDistance = PlayerPrefs.GetFloat(nameof(Settings.POIMaxFadeDistance), POI_MAX_FADE_DISTANCE_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the current version setting from PlayerPrefs, defaulting to the application version if not set.
        /// </summary>
        internal static void LoadCurrentVersion()
        {
            CurrentSettings.CurrentVersion = PlayerPrefs.GetString(nameof(Settings.CurrentVersion), Application.version);
        }

        /// <summary>
        /// Load the vignetting intensity setting from PlayerPrefs, defaulting to VIGNETTING_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadVignettingIntensity()
        {
            CurrentSettings.VignettingIntensity = PlayerPrefs.GetFloat(nameof(Settings.VignettingIntensity), VIGNETTING_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the contrast offset setting from PlayerPrefs, defaulting to CONTRAST_OFFSET_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadContrastOffset()
        {
            CurrentSettings.ContrastOffset = PlayerPrefs.GetFloat(nameof(Settings.ContrastOffset), CONTRAST_OFFSET_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the saturation offset setting from PlayerPrefs, defaulting to SATURATION_OFFSET_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadSaturationOffset()
        {
            CurrentSettings.SaturationOffset = PlayerPrefs.GetFloat(nameof(Settings.SaturationOffset), SATURATION_OFFSET_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the exposure offset setting from PlayerPrefs, defaulting to EXPOSURE_OFFSET_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadExposureOffset()
        {
            CurrentSettings.SaturationOffset = PlayerPrefs.GetFloat(nameof(Settings.ExposureOffset), EXPOSURE_OFFSET_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the clouds preset setting from PlayerPrefs, defaulting to CLOUD_PRESET_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCloudsPreset()
        {
            CurrentSettings.CloudsPreset = (CloudsPreset)PlayerPrefs.GetInt(nameof(Settings.CloudsPreset), (int)CLOUD_PRESET_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the cloud shadows enabled setting from PlayerPrefs, defaulting to CLOUD_SHADOW_ENABLED_DEFAULT_STATE if not set.
        /// </summary>
        internal static void LoadCloudShadowsEnabled()
        {
            int intBool = CLOUD_SHADOW_ENABLED_DEFAULT_STATE ? 1 : 0;
            CurrentSettings.CloudShadowsEnabled = PlayerPrefs.GetInt(nameof(Settings.CloudShadowsEnabled), intBool) == 1;
        }

        /// <summary>
        /// Load the cloud shadows opacity setting from PlayerPrefs, defaulting to CLOUD_SHADOW_OPACITY_DEFAULT_STATE if not set.
        /// </summary>
        internal static void LoadCloudShadowsOpacity()
        {
            CurrentSettings.CloudShadowsOpacity = PlayerPrefs.GetFloat(nameof(Settings.CloudShadowsOpacity), CLOUD_SHADOW_OPACITY_DEFAULT_STATE);
        }

        /// <summary>
        /// Load the wind type setting from PlayerPrefs, defaulting to WIND_TYPE_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadWindType()
        {
            CurrentSettings.WindType = (WindType)PlayerPrefs.GetInt(nameof(Settings.WindType), (int)WIND_TYPE_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the capture resolution setting from PlayerPrefs, defaulting to CAPTURE_RESOLUTION_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCaptureResolution()
        {
            CurrentSettings.CaptureResolution = PlayerPrefs.GetInt(nameof(Settings.CaptureResolution), CAPTURE_RESOLUTION_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the capture encoder setting from PlayerPrefs, defaulting to CAPTURE_ENCODER_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCaptureEncoder()
        {
            CurrentSettings.CaptureEncoder = PlayerPrefs.GetInt(nameof(Settings.CaptureEncoder), CAPTURE_ENCODER_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the capture framerate setting from PlayerPrefs, defaulting to CAPTURE_FRAMERATE_DEFAULT_VALUDE if not set.
        /// </summary>
        internal static void LoadCaptureFramerate()
        {
            CurrentSettings.CaptureFramerate = PlayerPrefs.GetInt(nameof(Settings.CaptureFramerate), CAPTURE_FRAMERATE_DEFAULT_VALUDE);
        }

        /// <summary>
        /// Load the capture output path setting from PlayerPrefs, defaulting to CAPTURE_OUTPUT_PATH_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCaptureOutputPath()
        {
            CurrentSettings.CaptureOutputPath = PlayerPrefs.GetString(nameof(Settings.CaptureOutputPath), CAPTURE_OUTPUT_PATH_DEFAULT_VALUE);
        }

        /// <summary>
        /// Load the capture encoded logo state setting from PlayerPrefs, defaulting to CAPTURE_ENCODED_LOGO_DEFAULT_VALUE if not set.
        /// </summary>
        internal static void LoadCaptureEncodedLogo()
        {
            int intBool = CAPTURE_ENCODED_LOGO_DEFAULT_VALUE ? 1 : 0;
            CurrentSettings.CaptureEncodedLogo = PlayerPrefs.GetInt(nameof(Settings.CaptureEncodedLogo), intBool) == 1;
        }

        /// <summary>
        /// Save the display wizard setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveDisplayWizard(bool value)
        {
            CurrentSettings.DisplayWizard = value;
            PlayerPrefs.SetInt(nameof(Settings.DisplayWizard), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Save the application target FPS setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveApplicationTargetFPS(int value)
        {
            CurrentSettings.ApplicationTargetFPS = value;
            PlayerPrefs.SetInt(nameof(Settings.ApplicationTargetFPS), value);
            PlayerPrefs.Save();
            OnApplicationTargetFPSChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the "Don't Ask Welcome Version" setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveDontAskWelcomeVersion(bool value)
        {
            CurrentSettings.DontAskWelcomeVersion = value;
            PlayerPrefs.SetInt(nameof(Settings.DontAskWelcomeVersion), value ? 1 : 0);
            PlayerPrefs.Save();
            OnDontAskWelcomeVersionChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the application idle FPS setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveApplicationIdleFPS(int value)
        {
            CurrentSettings.ApplicationIdleFPS = value;
            PlayerPrefs.SetInt(nameof(Settings.ApplicationIdleFPS), value);
            PlayerPrefs.Save();
            OnApplicationIdleFPSChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the camera rotation speed setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCameraRotationSpeed(float value)
        {
            CurrentSettings.CameraRotationSpeed = value;
            PlayerPrefs.SetFloat(nameof(Settings.CameraRotationSpeed), value);
            PlayerPrefs.Save();
            OnCameraRotationSpeedChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the camera zoom speed setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCameraZoomSpeed(float value)
        {
            CurrentSettings.CameraZoomSpeed = value;
            PlayerPrefs.SetFloat(nameof(Settings.CameraZoomSpeed), value);
            PlayerPrefs.Save();
            OnCameraZoomSpeedChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the pan speed setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePanSpeed(float value)
        {
            CurrentSettings.PanSpeed = value;
            PlayerPrefs.SetFloat(nameof(Settings.PanSpeed), value);
            PlayerPrefs.Save();
            OnPanSpeedChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the time zone setting to PlayerPrefs.
        /// </summary>
        /// <param name="timeZone"></param>
        internal static void SaveTimeZone(TimeZoneInfo timeZone)
        {
            CurrentSettings.UserTimeZone = timeZone;
            PlayerPrefs.SetString(nameof(Settings.UserTimeZone), timeZone.Id);
            PlayerPrefs.Save();
            OnTimeZoneChanged?.Invoke(timeZone);
        }

        /// <summary>
        /// Save the date format style setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveDateFormatStyle(DateFormatStyle value)
        {
            CurrentSettings.DateFormatStyle = value;
            PlayerPrefs.SetInt(nameof(Settings.DateFormatStyle), (int)value);
            PlayerPrefs.Save();
            OnDateFormatStyleChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the time format style setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveTimeFormatStyle(TimeFormatStyle value)
        {
            CurrentSettings.TimeFormatStyle = value;
            PlayerPrefs.SetInt(nameof(Settings.TimeFormatStyle), (int)value);
            PlayerPrefs.Save();
            OnTimeFormatStyleChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the unit system type setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveUnitSystemType(UnitSystemType value)
        {
            CurrentSettings.UnitSystemType = value;
            PlayerPrefs.SetInt(nameof(Settings.UnitSystemType), (int)value);
            PlayerPrefs.Save();
            OnUnitSystemTypeChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the workspace path setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveWorkspacePath(string value)
        {
            CurrentSettings.WorkspacePath = value;
            PlayerPrefs.SetString(nameof(Settings.WorkspacePath), value);
            PlayerPrefs.Save();
            OnWorkspacePathChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the workspace zoom setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveWorkspaceZoom(float value)
        {
            CurrentSettings.WorkspaceZoom = value;
            PlayerPrefs.SetFloat(nameof(Settings.WorkspaceZoom), value);
            PlayerPrefs.Save();
            OnWorkspaceZoomChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the MapTiler API key setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveMapTilerApiKey(string value)
        {
            CurrentSettings.MapTilerAPIKey = value;
            PlayerPrefs.SetString(nameof(Settings.MapTilerAPIKey), value);
            PlayerPrefs.Save();
            OnMapTilerApiKeyChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the global scale setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveGlobalScale(float value)
        {
            CurrentSettings.GlobalScale = value;
            PlayerPrefs.SetFloat(nameof(Settings.GlobalScale), value);
            PlayerPrefs.Save();
            OnGlobalScaleChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the current version setting to PlayerPrefs.
        /// </summary>
        /// <param name="currentVersion"></param>
        internal static void SaveCurrentVersion(string currentVersion)
        {
            CurrentSettings.CurrentVersion = currentVersion;
            PlayerPrefs.SetString(nameof(Settings.CurrentVersion), currentVersion);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Save the path 3D thickness setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePath3DThickness(float value)
        {
            CurrentSettings.Path3DThickness = value;
            PlayerPrefs.SetFloat(nameof(Settings.Path3DThickness), value);
            PlayerPrefs.Save();
            OnPath3DWidthChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the path 3D remaining color setting to PlayerPrefs.
        /// </summary>
        /// <param name="color"></param>
        internal static void SavePath3DRemainingColor(Color color)
        {
            CurrentSettings.Path3DRemainingColor = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.Path3DRemainingColor), colorString);
            PlayerPrefs.Save();
            OnPath3DRemainingColorChanged?.Invoke(color);
        }

        /// <summary>
        /// Save the building visibility setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveBuildingVisibility(bool value)
        {
            CurrentSettings.BuildingVisibility = value;
            PlayerPrefs.SetInt(nameof(Settings.BuildingVisibility), value ? 1 : 0);
            PlayerPrefs.Save();
            OnBuildingVisibilityChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the building color setting to PlayerPrefs.
        /// </summary>
        /// <param name="color"></param>
        internal static void SaveBuildingColor(Color color)
        {
            CurrentSettings.BuildingColor = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.BuildingColor), colorString);
            PlayerPrefs.Save();
            OnBuildingColorChanged?.Invoke(color);
        }

        /// <summary>
        /// Save the building ambient occlusion setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveBuildingAO(float value)
        {
            CurrentSettings.BuildingAO = value;
            PlayerPrefs.SetFloat(nameof(Settings.BuildingAO), value);
            PlayerPrefs.Save();
            OnBuildingAOChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the POI scale setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePOIScale(float value)
        {
            CurrentSettings.POIScale = value;
            PlayerPrefs.SetFloat(nameof(Settings.POIScale), value);
            PlayerPrefs.Save();
            OnPOIScaleChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the POI visibility setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePOIVisibility(bool value)
        {
            CurrentSettings.POIVisibility = value;
            PlayerPrefs.SetInt(nameof(Settings.POIVisibility), value ? 1 : 0);
            PlayerPrefs.Save();
            OnPOIVisibilityChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the POI height setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePOIHeight(float value)
        {
            CurrentSettings.POIHeight = value;
            PlayerPrefs.SetFloat(nameof(Settings.POIHeight), value);
            PlayerPrefs.Save();
            OnPOIHeightChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the POI max fade distance setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePOIMinFadeDistance(float value)
        {
            CurrentSettings.POIMinFadeDistance = value;
            PlayerPrefs.SetFloat(nameof(Settings.POIMinFadeDistance), value);
            PlayerPrefs.Save();
            OnPOIMinFadeDistanceChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the POI max fade distance setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SavePOIMaxFadeDistance(float value)
        {
            CurrentSettings.POIMaxFadeDistance = value;
            PlayerPrefs.SetFloat(nameof(Settings.POIMaxFadeDistance), value);
            PlayerPrefs.Save();
            OnPOIMaxFadeDistanceChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the contrast offset setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveContrastOffset(float value)
        {
            CurrentSettings.ContrastOffset = value;
            PlayerPrefs.SetFloat(nameof(Settings.ContrastOffset), value);
            PlayerPrefs.Save();
            OnContrastOffsetChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the saturation offset setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveSaturationOffset(float value)
        {
            CurrentSettings.SaturationOffset = value;
            PlayerPrefs.SetFloat(nameof(Settings.SaturationOffset), value);
            PlayerPrefs.Save();
            OnSaturationOffsetChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the exposure offset setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveExposureOffset(float value)
        {
            CurrentSettings.ExposureOffset = value;
            PlayerPrefs.SetFloat(nameof(Settings.ExposureOffset), value);
            PlayerPrefs.Save();
            OnExposureOffsetChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the vignetting intensity setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveVignettingIntensity(float value)
        {
            CurrentSettings.VignettingIntensity = value;
            PlayerPrefs.SetFloat(nameof(Settings.VignettingIntensity), value);
            PlayerPrefs.Save();
            OnVignettingIntensityChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the clouds preset setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCloudsPreset(CloudsPreset value)
        {
            CurrentSettings.CloudsPreset = value;
            PlayerPrefs.SetInt(nameof(Settings.CloudsPreset), (int)value);
            PlayerPrefs.Save();
            OnCloudsPresetChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the cloud shadows enabled setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCloudShadowsEnabled(bool value)
        {
            CurrentSettings.CloudShadowsEnabled = value;
            PlayerPrefs.SetInt(nameof(Settings.CloudShadowsEnabled), value ? 1 : 0);
            PlayerPrefs.Save();
            OnCloudShadowsEnabledChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the cloud shadows opacity setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCloudShadowsOpacity(float value)
        {
            CurrentSettings.CloudShadowsOpacity = value;
            PlayerPrefs.SetFloat(nameof(Settings.CloudShadowsOpacity), value);
            PlayerPrefs.Save();
            OnCloudShadowsOpacityChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the wind type setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveWindType(WindType value)
        {
            CurrentSettings.WindType = value;
            PlayerPrefs.SetInt(nameof(Settings.WindType), (int)value);
            PlayerPrefs.Save();
            OnWindTypeChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the capture resolution setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCaptureResolution(int value)
        {
            CurrentSettings.CaptureResolution = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureResolution), value);
            PlayerPrefs.Save();
            OnCaptureResolutionChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the capture encoder setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCaptureEncoder(int value)
        {
            CurrentSettings.CaptureEncoder = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureEncoder), value);
            PlayerPrefs.Save();
            OnCaptureEncoderChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the capture framerate setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCaptureFramerate(int value)
        {
            CurrentSettings.CaptureFramerate = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureFramerate), value);
            PlayerPrefs.Save();
            OnCaptureFramerateChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the capture output path setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCaptureOutputPath(string value)
        {
            CurrentSettings.CaptureOutputPath = value;
            PlayerPrefs.SetString(nameof(Settings.CaptureOutputPath), value);
            PlayerPrefs.Save();
            OnCaptureOutputPathChanged?.Invoke(value);
        }

        /// <summary>
        /// Save the capture encoded logo state setting to PlayerPrefs.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveCaptureEncodedLogo(bool value)
        {
            CurrentSettings.CaptureEncodedLogo = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureEncodedLogo), value ? 1 : 0);
            PlayerPrefs.Save();
            OnCaptureEncodedLogoChanged?.Invoke(value);
        }

        /// <summary>
        /// Load all settings from PlayerPrefs, initializing defaults if not already set.
        /// </summary>
        internal static void LoadAll()
        {
            if (!PlayerPrefs.HasKey("SettingsInitialized"))
            {
                LoadDefaultSettings();
            }

            LoadDisplayWizard();
            LoadCurrentVersion();
            LoadApplicationTargetFPS();
            LoadApplicationIdleFPS();
            LoadDontAskWelcomeVersion();
            LoadCameraRotationSpeed();
            LoadCameraZoomSpeed();
            LoadPanSpeed();
            LoadTimeZone();
            LoadDateFormatStyle();
            LoadTimeFormatStyle();
            LoadUnitSystemType();
            LoadGlobalScale();
            LoadWorkspacePath();
            LoadWorkspaceZoom();
            LoadMapTilerApiKey();
            LoadPath3DThickness();
            LoadPath3DRemainingColor();
            LoadBuildingVisibility();
            LoadBuildingColor();
            LoadBuildingAO();
            LoadPOIScale();
            LoadPOIVisibility();
            LoadPOIHeight();
            LoadPOIMinFadeDistance();
            LoadPOIMaxFadeDistance();
            LoadVignettingIntensity();
            LoadContrastOffset();
            LoadSaturationOffset();
            LoadExposureOffset();
            LoadCloudsPreset();
            LoadCloudShadowsEnabled();
            LoadCloudShadowsOpacity();
            LoadWindType();
            LoadCaptureResolution();
            LoadCaptureEncoder();
            LoadCaptureFramerate();
            LoadCaptureOutputPath();
            LoadCaptureEncodedLogo();
        }

        /// <summary>
        /// Initialize all settings to their default values and save them to PlayerPrefs.
        /// </summary>
        internal static void LoadDefaultSettings()
        {
            SaveDisplayWizard(true);
            SaveCurrentVersion(Application.version);
            SaveApplicationTargetFPS(120);
            SaveApplicationIdleFPS(30);
            SaveDontAskWelcomeVersion(false);
            SaveCameraRotationSpeed(CAMERA_ROTATION_SPEED_DEFAULT_VALUE);
            SaveCameraZoomSpeed(CAMERA_ZOOM_SPEED_DEFAULT_VALUE);
            SavePanSpeed(PAN_SPEED_DEFAULT_VALUE);
            string timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Romance Standard Time"
                : "Europe/Paris";
            SaveTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
            SaveDateFormatStyle(DateFormatStyle.European);
            SaveTimeFormatStyle(TimeFormatStyle.TwentyFourHour);
            SaveUnitSystemType(UnitSystemType.Metric);
            SaveGlobalScale(1f);
            SaveWorkspacePath(Application.persistentDataPath);
            SaveWorkspaceZoom(1f);
            SaveMapTilerApiKey("");
            SavePath3DThickness(PATH_3D_THICKNESS_DEFAULT_VALUE);
            SavePath3DRemainingColor(PATH_3D_REMAINING_COLOR_DEFAULT_VALUE);
            SaveBuildingVisibility(BUILDING_DISPLAY_STATE_DEFAULT_VALUE);
            SaveBuildingColor(BUILDING_COLOR_DEFAULT_VALUE);
            SaveBuildingAO(BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE);
            SavePOIVisibility(POI_DISPLAY_STATE_DEFAULT_VALUE);
            SavePOIScale(POI_SCALE_DEFAULT_VALUE);
            SavePOIHeight(POI_HEIGHT_DEFAULT_VALUE);
            SavePOIMinFadeDistance(POI_MIN_FADE_DISTANCE_DEFAULT_VALUE);
            SavePOIMaxFadeDistance(POI_MAX_FADE_DISTANCE_DEFAULT_VALUE);
            SaveContrastOffset(CONTRAST_OFFSET_DEFAULT_VALUE);
            SaveSaturationOffset(SATURATION_OFFSET_DEFAULT_VALUE);
            SaveExposureOffset(EXPOSURE_OFFSET_DEFAULT_VALUE);
            SaveCloudsPreset(CLOUD_PRESET_DEFAULT_VALUE);
            SaveCloudShadowsEnabled(CLOUD_SHADOW_ENABLED_DEFAULT_STATE);
            SaveCloudShadowsOpacity(CLOUD_SHADOW_OPACITY_DEFAULT_STATE);
            SaveWindType(WIND_TYPE_DEFAULT_VALUE);
            SaveCaptureResolution(1);
            SaveCaptureEncoder(0);
            SaveCaptureFramerate(1);
            SaveCaptureOutputPath(Path.Combine(Application.persistentDataPath, "Captures"));
            SaveCaptureEncodedLogo(true);

            PlayerPrefs.SetInt("SettingsInitialized", 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resolve the TimeZoneInfo from the given universal ID, handling platform differences.
        /// </summary>
        /// <param name="universalId"></param>
        /// <returns></returns>
        private static TimeZoneInfo ResolveTimeZone(string universalId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(universalId);
            }
            catch (TimeZoneNotFoundException)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                {
                    if (TimeZoneIdMap.TryGetValue(universalId, out string windowsId))
                    {
                        try
                        {
                            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                        }
                        catch { }
                    }
                }
            }

            return TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Format a TimeSpan offset as a UTC offset string (e.g., "+02:00" or "-05:30").
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal static string FormatUtcOffset(TimeSpan offset)
        {
            string sign = offset.TotalMinutes >= 0 ? "+" : "-";
            int hours = Math.Abs(offset.Hours);
            int minutes = Math.Abs(offset.Minutes);
            return $"{sign}{hours:D2}:{minutes:D2}";
        }

        /// <summary>
        /// Get a date format label string based on the given DateFormatStyle enum value.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        internal static string GetDateFormatLabel(DateFormatStyle style)
        {
            switch (style)
            {
                default:
                case DateFormatStyle.European:
                    return "dd/MM/yyyy";
                case DateFormatStyle.American:
                    return "MM/dd/yyyy";
                case DateFormatStyle.ISO:
                    return "yyyy-MM-dd";
            }
        }

        /// <summary>
        /// Get a time format label string based on the given TimeFormatStyle enum value.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        internal static string GetTimeFormatLabel(TimeFormatStyle style)
        {
            switch (style)
            {
                default:
                case TimeFormatStyle.TwentyFourHour:
                    return "24H";
                case TimeFormatStyle.TwelveHour:
                    return "12H";
            }
        }

        /// <summary>
        /// Format a DateTime according to the current settings for date and time format styles.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static string FormatDateTime(DateTime date)
        {
            var dateFormat = CurrentSettings.DateFormatStyle;
            var timeFormat = CurrentSettings.TimeFormatStyle;
            string datePattern;
            string timePattern;

            switch (dateFormat)
            {
                case DateFormatStyle.European:
                    datePattern = "dd/MM/yyyy";
                    break;
                case DateFormatStyle.American:
                    datePattern = "MM/dd/yyyy";
                    break;
                case DateFormatStyle.ISO:
                    datePattern = "yyyy-MM-dd";
                    break;
                default:
                    datePattern = "dd/MM/yyyy";
                    break;
            }

            switch (timeFormat)
            {
                case TimeFormatStyle.TwelveHour:
                    timePattern = "hh:mm tt";
                    break;
                case TimeFormatStyle.TwentyFourHour:
                    timePattern = "HH:mm";
                    break;
                default:
                    timePattern = "HH:mm";
                    break;
            }

            string fullPattern = $"{datePattern} {timePattern}";

            return date.ToString(fullPattern, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get a unit system label string based on the given UnitSystemType enum value.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static string GetUnitSystemLabel(UnitSystemType type)
        {
            switch (type)
            {
                default:
                case UnitSystemType.Metric:
                    return "Metric (m, m/s)";
                case UnitSystemType.Imperial:
                    return "Imperial (ft, mph)";
                case UnitSystemType.Nautical:
                    return "Nautical (ft, knots)";
                case UnitSystemType.Custom:
                    return "Custom";
            }
        }

        /// <summary>
        /// Get a human-readable label for an enum value by replacing underscores with spaces and converting to title case.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string GetEnumLabel<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            string raw = value.ToString();
            string spaced = System.Text.RegularExpressions.Regex.Replace(raw, "_", " ");
            string titleCase = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());

            return titleCase;
        }

        /// <summary>
        /// Format an altitude value in meters to a string with the appropriate unit based on current settings.
        /// </summary>
        /// <param name="meters"></param>
        /// <returns></returns>
        internal static string FormatAltitude(double meters)
        {
            switch (CurrentSettings.UnitSystemType)
            {
                default:
                case UnitSystemType.Metric:
                    return $"{meters:F2} m";
                case UnitSystemType.Imperial:
                    double feet = meters * 3.28084;
                    return $"{feet:F2} ft";
                case UnitSystemType.Nautical:
                    double feetNautical = meters * 3.28084;
                    return $"{feetNautical:F2} ft";
            }
        }

        /// <summary>
        /// Format a speed value in meters per second to a string with the appropriate unit based on current settings.
        /// </summary>
        /// <param name="metersPerSecond"></param>
        /// <returns></returns>
        internal static string FormatSpeed(double metersPerSecond)
        {
            switch (CurrentSettings.UnitSystemType)
            {
                default:
                case UnitSystemType.Metric:
                    return $"{metersPerSecond:F1} m/s";
                case UnitSystemType.Imperial:
                    double mph = metersPerSecond * 2.23694;
                    return $"{mph:F1} mph";
                case UnitSystemType.Nautical:
                    double knots = metersPerSecond * 1.94384;
                    return $"{knots:F1} knots";
            }
        }

        /// <summary>
        /// Convert an altitude value in meters to the appropriate unit based on current settings.
        /// </summary>
        /// <param name="meters"></param>
        /// <returns></returns>
        internal static float ConvertAltitude(float meters)
        {
            switch (CurrentSettings.UnitSystemType)
            {
                default:
                case UnitSystemType.Metric:
                    return meters;
                case UnitSystemType.Imperial:
                case UnitSystemType.Nautical:
                    return meters * 3.28084f;
            }
        }

        /// <summary>
        /// Convert a speed value in meters per second to the appropriate unit based on current settings.
        /// </summary>
        /// <param name="metersPerSecond"></param>
        /// <returns></returns>
        internal static float ConvertSpeed(float metersPerSecond)
        {
            switch (CurrentSettings.UnitSystemType)
            { // en knots
                default:
                case UnitSystemType.Metric:
                    return metersPerSecond;
                case UnitSystemType.Imperial:
                    return metersPerSecond * 2.23694f;
                case UnitSystemType.Nautical:
                    return metersPerSecond * 1.94384f;
            }
        }
        #endregion

        #region UI
        /// <summary>
        /// Display a color picker with a reset button in the settings UI.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="text"></param>
        /// <param name="tooltipText"></param>
        /// <param name="tooltipReset"></param>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <param name="onChange"></param>
        /// <param name="onReset"></param>
        internal static void DisplaySettingsColorPickerWithReset(FuGrid grid, string text, string tooltipText, string tooltipReset, Color value, Color defaultValue, Action<Color> onChange, Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            Vector4 tempValue = value;

            if (grid.ColorPicker(text, ref tempValue))
            {
                onChange?.Invoke((Color)tempValue);
            }

            if (value == defaultValue)
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }
            Fugui.PopFont();
        }

        /// <summary>
        /// Display a toggle with a reset button in the settings UI.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="text"></param>
        /// <param name="tooltipText"></param>
        /// <param name="tooltipReset"></param>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <param name="onChange"></param>
        /// <param name="onReset"></param>
        internal static void DisplaySettingsToggleWithReset(FuGrid grid, string text, string tooltipText, string tooltipReset, bool value, bool defaultValue, Action<bool> onChange, Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            bool tempValue = value;

            if (grid.Toggle(text, ref tempValue))
            {
                onChange?.Invoke(tempValue);
            }

            if (value == defaultValue)
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }
            Fugui.PopFont();
        }

        /// <summary>
        /// Display a slider with a reset button in the settings UI.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="text"></param>
        /// <param name="tooltipText"></param>
        /// <param name="tooltipReset"></param>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="step"></param>
        /// <param name="defaultValue"></param>
        /// <param name="format"></param>
        /// <param name="onChange"></param>
        /// <param name="onReset"></param>
        internal static void DisplaySettingsSliderWithReset(FuGrid grid, string text, string tooltipText, string tooltipReset, float value, float minValue, float maxValue, float step, float defaultValue, string format, Action<float> onChange, Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            float tempValue = value;
            if (grid.Slider(text, ref tempValue, minValue, maxValue, step, format: format))
            {
                onChange?.Invoke(tempValue);
            }

            if (AreApproximatelyEqual(value, defaultValue))
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }
            Fugui.PopFont();
        }

        /// <summary>
        /// Display a range slider with a reset button in the settings UI.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="text"></param>
        /// <param name="tooltipText"></param>
        /// <param name="tooltipReset"></param>
        /// <param name="valueMin"></param>
        /// <param name="valueMax"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="step"></param>
        /// <param name="defaultValues"></param>
        /// <param name="format"></param>
        /// <param name="onChange"></param>
        /// <param name="onReset"></param>
        internal static void DisplaySettingsRangeWithReset(FuGrid grid, string text, string tooltipText, string tooltipReset, float valueMin, float valueMax, float minValue, float maxValue, float step, Vector2 defaultValues, string format, Action<float, float> onChange, Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            float tempMin = valueMin;
            float tempMax = valueMax;

            if (grid.Range(text, ref tempMin, ref tempMax, minValue, maxValue, step, format: format))
            {
                onChange?.Invoke(tempMin, tempMax);
            }

            bool isAtDefault = AreApproximatelyEqual(valueMin, defaultValues.x) && AreApproximatelyEqual(valueMax, defaultValues.y);
            if (isAtDefault)
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }
            Fugui.PopFont();
        }

        /// <summary>
        /// Display a range slider with a reset button in the settings UI.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="text"></param>
        /// <param name="tooltipText"></param>
        /// <param name="tooltipReset"></param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="step"></param>
        /// <param name="defaultValue1"></param>
        /// <param name="defaultValue2"></param>
        /// <param name="onChange"></param>
        /// <param name="onReset"></param>
        internal static void DisplaySettingsRangeWithReset(FuGrid grid, string text, string tooltipText, string tooltipReset, float value1, float value2, float minValue, float maxValue, float step, float defaultValue1, float defaultValue2, Action<float, float> onChange, Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            float tempValue1 = value1;
            float tempValue2 = value2;
            if (grid.Range(text, ref tempValue1, ref tempValue2, minValue, maxValue, step))
            {
                onChange?.Invoke(tempValue1, tempValue2);
            }

            if (AreApproximatelyEqual(value1, defaultValue1) && AreApproximatelyEqual(value2, defaultValue2))
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }
            Fugui.PopFont();
        }

        /// <summary>
        /// Display a combobox with a reset button in the settings UI.  
        /// Works for Enums, ints or any type with string labels.
        /// </summary>
        /// <typeparam name="T">Type of the stored value (int, enum, string, etc.).</typeparam>
        /// <param name="grid">The Fugui grid layout.</param>
        /// <param name="text">The label of the combobox.</param>
        /// <param name="tooltipText">Tooltip for the combobox.</param>
        /// <param name="tooltipReset">Tooltip for the reset button.</param>
        /// <param name="value">Current value.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <param name="getLabel">Function to get display text for each value.</param>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <param name="onChange">Callback when a new value is selected.</param>
        /// <param name="onReset">Callback when reset is clicked.</param>
        internal static void DisplaySettingsComboboxWithReset<T>(
            FuGrid grid,
            string text,
            string tooltipText,
            string tooltipReset,
            T value,
            T defaultValue,
            Func<T, string> getLabel,
            IEnumerable<T> allowedValues,
            Action<T> onChange,
            Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            string currentLabel = getLabel != null ? getLabel(value) : value?.ToString() ?? "Unknown";

            grid.Combobox($"{text}##Combobox", currentLabel, () =>
            {
                foreach (T option in allowedValues)
                {
                    bool isSelected = EqualityComparer<T>.Default.Equals(option, value);
                    string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {getLabel(option)}";

                    if (ImGui.Selectable(label))
                    {
                        onChange?.Invoke(option);
                    }
                }
            });

            if (EqualityComparer<T>.Default.Equals(value, defaultValue))
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }
            Fugui.PopFont();
        }

        /// <summary>
        /// Display a folder input with a reset button in the settings UI.
        /// </summary>
        /// <param name="grid">The Fugui grid layout.</param>
        /// <param name="text">The label of the input field.</param>
        /// <param name="tooltipText">Tooltip for the folder input.</param>
        /// <param name="tooltipReset">Tooltip for the reset button.</param>
        /// <param name="value">Current folder path.</param>
        /// <param name="defaultValue">Default folder path.</param>
        /// <param name="onChange">Callback when a new folder is selected.</param>
        /// <param name="onReset">Callback when reset is clicked.</param>
        /// <param name="filters">Optional file filters for the folder picker (can be empty).</param>
        internal static void DisplaySettingsFolderInputWithReset(
            FuGrid grid,
            string text,
            string tooltipText,
            string tooltipReset,
            string value,
            string defaultValue,
            Action<string> onChange,
            Action onReset,
            ExtensionFilter[] filters = null)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            string currentValue = value ?? string.Empty;

            grid.InputFolder(text, (selectedPath) =>
            {
                if (!string.Equals(selectedPath, currentValue, StringComparison.Ordinal))
                {
                    onChange?.Invoke(selectedPath);
                }
            }, currentValue, filters ?? Array.Empty<ExtensionFilter>());

            if (onReset == null)
            {
                grid.NextColumn();
                return;
            }

            if (string.Equals(value, defaultValue, StringComparison.Ordinal))
            {
                grid.DisableNextElement();
            }

            Fugui.PushFont(14);
            if (!string.IsNullOrEmpty(tooltipReset))
            {
                grid.SetNextElementToolTip(tooltipReset);
            }

            if (grid.ClickableText(FlightReLiveIcons.Undo, FuTextStyle.Danger))
            {
                onReset?.Invoke();
            }

            Fugui.PopFont();
        }

        /// <summary>
        /// Check if two float values are approximately equal within a small epsilon.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        private static bool AreApproximatelyEqual(float a, float b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// Show the preferences modal dialog.
        /// </summary>
        internal static void ShowPreferencesModal()
        {
            if (_settingsOpened)
            {
                return;
            }

            _settingsOpened = true;
            Fugui.ShowModal(FlightReLiveIcons.Preferences + " Flight ReLive preferences", (layout) =>
            {
                bool isLoading = LoadingManager.Instance.IsLoading;

                layout.Collapsable("FPS settings##collapsable", () =>
                {
                    using (FuGrid fpsSettings = new FuGrid("fpsSettingsGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        fpsSettings.SetNextElementToolTipWithLabel("This parameter defines the refresh rate of the application while it is running.\nAdjusting this value can help balance visual fluidity and system performance.\nHigher values provide smoother animations but may increase GPU usage.\nLower values reduce resource consumption, which can be useful on less powerful machines or during background execution.\nChanges are applied immediately.");
                        int targetFPS = CurrentSettings.ApplicationTargetFPS;
                        if (fpsSettings.Slider("Application Target FPS", ref targetFPS, 30, 160))
                        {
                            SaveApplicationTargetFPS(targetFPS);
                        }

                        fpsSettings.SetNextElementToolTipWithLabel("This parameter defines the refresh rate of the application when it is idle or running in the background.\nLowering this value reduces GPU usage and power consumption when the application is not actively in use.\nIt is particularly useful for minimizing resource load during long sessions or when switching to other tasks.\nChanges are applied immediately.");
                        int idleFPS = CurrentSettings.ApplicationIdleFPS;

                        if (fpsSettings.Slider("Application Idle FPS", ref idleFPS, 1, 160))
                        {
                            SaveApplicationIdleFPS(idleFPS);
                        }
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable("Camera controls##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid rotationSpeedGrid = new FuGrid("rotationSpeedGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        rotationSpeedGrid.SetNextElementToolTipWithLabel("This setting defines the camera rotation speed.");

                        float rotationSpeed = CurrentSettings.CameraRotationSpeed;

                        if (rotationSpeedGrid.Slider("Camera rotation speed", ref rotationSpeed, 0.1f, 5f, 0.1f, format: "%.1f"))
                        {
                            SaveCameraRotationSpeed(rotationSpeed);
                        }
                    }

                    using (FuGrid zoomSpeedGrid = new FuGrid("zoomSpeedGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        zoomSpeedGrid.SetNextElementToolTipWithLabel("This setting defines the camera zoom speed when scrolling.");

                        float zoomSpeed = CurrentSettings.CameraZoomSpeed;

                        if (zoomSpeedGrid.Slider("Camera zoom speed", ref zoomSpeed, 0.1f, 5f, 0.1f, format: "%.1f"))
                        {
                            SaveCameraZoomSpeed(zoomSpeed);
                        }
                    }

                    using (FuGrid panSpeedGrid = new FuGrid("panSpeedGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        panSpeedGrid.SetNextElementToolTipWithLabel("This setting defines the camera pan speed when moving the view.");

                        float panSpeed = CurrentSettings.PanSpeed;

                        if (panSpeedGrid.Slider("Pan speed", ref panSpeed, 0.1f, 5f, 0.1f, format: "%.1f"))
                        {
                            SavePanSpeed(panSpeed);
                        }
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable("Regional settings##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid timeZoneGrid = new FuGrid("timeZoneGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        timeZoneGrid.SetNextElementToolTipWithLabel("The time zone is used to accurately calculate the lighting and position of the sun in the scene.");

                        TimeZoneInfo currentTz = CurrentSettings.UserTimeZone;
                        string currentTzId = currentTz.Id;
                        string comboLabel = currentTz.DisplayName.StartsWith("(UTC") ? currentTz.DisplayName : $"(UTC{FormatUtcOffset(currentTz.BaseUtcOffset)}) {currentTz.DisplayName}";

                        timeZoneGrid.Combobox("TimeZone##TZCombobox", comboLabel, () =>
                        {
                            foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
                            {
                                bool isSelected = tz.Id == currentTzId;

                                string label = tz.DisplayName.StartsWith("(UTC")
                                    ? $"{(isSelected ? FlightReLiveIcons.Check : " ")} {tz.DisplayName}"
                                    : $"{(isSelected ? FlightReLiveIcons.Check : " ")} (UTC{FormatUtcOffset(tz.BaseUtcOffset)}) {tz.DisplayName}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveTimeZone(tz);
                                }
                            }
                        });

                        timeZoneGrid.SetNextElementToolTipWithLabel("Choose how dates are displayed throughout the application.");

                        DateFormatStyle currentFormat = CurrentSettings.DateFormatStyle;
                        string formatLabel = GetDateFormatLabel(currentFormat);

                        timeZoneGrid.Combobox("DateFormat##DateFormatCombobox", formatLabel, () =>
                        {
                            foreach (DateFormatStyle format in Enum.GetValues(typeof(DateFormatStyle)))
                            {
                                bool isSelected = format == currentFormat;
                                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {GetDateFormatLabel(format)}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveDateFormatStyle(format);
                                }
                            }
                        });

                        timeZoneGrid.SetNextElementToolTipWithLabel("Choose between 12-hour or 24-hour time format.");

                        TimeFormatStyle currentTimeFormat = CurrentSettings.TimeFormatStyle;
                        string timeFormatLabel = GetTimeFormatLabel(currentTimeFormat);

                        timeZoneGrid.Combobox("TimeFormat##TimeFormatCombobox", timeFormatLabel, () =>
                        {
                            foreach (TimeFormatStyle format in Enum.GetValues(typeof(TimeFormatStyle)))
                            {
                                bool isSelected = format == currentTimeFormat;
                                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {GetTimeFormatLabel(format)}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveTimeFormatStyle(format);
                                }
                            }
                        });

                        timeZoneGrid.SetNextElementToolTipWithLabel("Select your preferred unit system for altitude and speed display.");

                        UnitSystemType currentUnitSystem = CurrentSettings.UnitSystemType;
                        string unitSystemLabel = GetUnitSystemLabel(currentUnitSystem);

                        timeZoneGrid.Combobox("UnitSystem##UnitSystemCombobox", unitSystemLabel, () =>
                        {
                            foreach (UnitSystemType system in Enum.GetValues(typeof(UnitSystemType)))
                            {
                                bool isSelected = system == currentUnitSystem;
                                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {GetUnitSystemLabel(system)}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveUnitSystemType(system);
                                }
                            }
                        });
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable("MapTiler##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid apiGrid = new FuGrid("apiGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        if (isLoading)
                        {
                            apiGrid.DisableNextElements();
                        }

                        string mapTilerAPIKey = CurrentSettings.MapTilerAPIKey;
                        apiGrid.SetNextElementToolTipWithLabel("MapTiler API key required for downloading satellite, topographic, buildings, hillshade images.\nA MapTiler account is required (free for less than 100,000 tile downloads per month).");

                        if (apiGrid.TextInput("MapTiler API key", ref mapTilerAPIKey, flags: FuInputTextFlags.Password))
                        {
                            SaveMapTilerApiKey(mapTilerAPIKey);
                        }
                        apiGrid.NextColumn();
                        apiGrid.TextURL("Follow this link to create a free MapTiler API Account", "https://www.maptiler.com/", FuTextWrapping.Clip);
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable("UI##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid uiGrid = new FuGrid("uiGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        uiGrid.SetNextElementToolTipWithLabel("Global UI scale");
                        uiGrid.Combobox("UI Scale##UIScaleCombobox", (int)(Fugui.DefaultContext.Scale * 100f) + "%", () =>
                        {
                            foreach (float scale in _availableUIScale)
                            {
                                if (ImGui.Selectable((scale == Fugui.DefaultContext.Scale ? FlightReLiveIcons.Check : " ") + "  " + scale * 100f + "%"))
                                {
                                    SaveGlobalScale(scale);
                                }
                            }
                        });
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable("Clear caches and settings##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Bold);

                    using (FuGrid uiGrid = new FuGrid("actionSettingsGrid", new FuGridDefinition(3, new float[] { 0.3f, 0.3f, 0.3f }), FuGridFlag.Default))
                    {
                        if (isLoading)
                        {
                            uiGrid.DisableNextElements();
                        }

                        uiGrid.SetNextElementToolTipWithLabel("Delete all downloaded tiles stored on this computer.\nVideo files will not be deleted.");

                        if (uiGrid.Button("Clear local cache", FuButtonStyle.Info))
                        {
                            CacheManager.ClearCache();
                        }

                        uiGrid.SetNextElementToolTipWithLabel("Delete all saved flights stored on this computer.\nVideo files will not be deleted.");

                        if (uiGrid.Button("Clear workspace", FuButtonStyle.Info))
                        {
                            CacheManager.ClearWorkspaceCache();
                        }

                        uiGrid.SetNextElementToolTipWithLabel("Restore the entire application configuration (including settings made from the application's global UI).\nVideo files will not be deleted.");

                        if (uiGrid.Button("Restore preferences", FuButtonStyle.Danger))
                        {
                            LoadDefaultSettings();
                            LoadAll();

                            Fugui.Notify("Successful operation", "All user preferences have been reset.", StateType.Info, 3f);
                        }
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

            }, FuModalSize.Medium, new FuModalButton("Close preferences", () => { _settingsOpened = false; }, FuButtonStyle.Default, FuKeysCode.Enter));
        }

        /// <summary>
        /// Reset the path 3D thickness to its default value.
        /// </summary>
        internal static void ResetPath3DThickness()
        {
            SavePath3DThickness(PATH_3D_THICKNESS_DEFAULT_VALUE);
            LoadPath3DThickness();
        }

        /// <summary>
        /// Reset the path 3D remaining color to its default value.
        /// </summary>
        internal static void ResetPath3DRemainingColor()
        {
            SavePath3DRemainingColor(PATH_3D_REMAINING_COLOR_DEFAULT_VALUE);
            LoadPath3DRemainingColor();
        }

        /// <summary>
        /// Reset the building visibility to its default value.
        /// </summary>
        internal static void ResetBuildingVisibility()
        {
            SaveBuildingVisibility(BUILDING_DISPLAY_STATE_DEFAULT_VALUE);
            LoadBuildingVisibility();
        }

        /// <summary>
        /// Reset the building color to its default value.
        /// </summary>
        internal static void ResetBuildingColor()
        {
            SaveBuildingColor(BUILDING_COLOR_DEFAULT_VALUE);
            LoadBuildingColor();
        }

        /// <summary>
        /// Reset the building ambient occlusion to its default value.
        /// </summary>
        internal static void ResetBuildingAO()
        {
            SaveBuildingAO(BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE);
            LoadBuildingAO();
        }

        /// <summary>
        /// Reset the POI visibility to its default value.
        /// </summary>
        internal static void ResetPOIVisibility()
        {
            SavePOIVisibility(POI_DISPLAY_STATE_DEFAULT_VALUE);
            LoadPOIVisibility();
        }

        /// <summary>
        /// Reset the POI scale to its default value.
        /// </summary>
        internal static void ResetPOIScale()
        {
            SavePOIScale(POI_SCALE_DEFAULT_VALUE);
            LoadPOIScale();
        }

        /// <summary>
        /// Reset the POI height to its default value.
        /// </summary>
        internal static void ResetPOIHeight()
        {
            SavePOIHeight(POI_HEIGHT_DEFAULT_VALUE);
            LoadPOIHeight();
        }

        /// <summary>
        /// Reset the POI minimum fade distance to its default value.
        /// </summary>
        internal static void ResetPOIMinFadeDistance()
        {
            SavePOIMinFadeDistance(POI_MIN_FADE_DISTANCE_DEFAULT_VALUE);
            LoadPOIMinFadeDistance();
        }

        /// <summary>
        /// Reset the POI maximum fade distance to its default value.
        /// </summary>
        internal static void ResetPOIMaxFadeDistance()
        {
            SavePOIMaxFadeDistance(POI_MAX_FADE_DISTANCE_DEFAULT_VALUE);
            LoadPOIMaxFadeDistance();
        }

        /// <summary>
        /// Reset the contrast offset to its default value.
        /// </summary>
        internal static void ResetContrastOffset()
        {
            SaveContrastOffset(CONTRAST_OFFSET_DEFAULT_VALUE);
            LoadContrastOffset();
        }

        /// <summary>
        /// Reset the saturation offset to its default value.
        /// </summary>
        internal static void ResetSaturationOffset()
        {
            SaveSaturationOffset(SATURATION_OFFSET_DEFAULT_VALUE);
            LoadSaturationOffset();
        }

        /// <summary>
        /// Reset the exposure offset to its default value.
        /// </summary>
        internal static void ResetExposureOffset()
        {
            SaveExposureOffset(EXPOSURE_OFFSET_DEFAULT_VALUE);
            LoadExposureOffset();
        }

        /// <summary>
        /// Reset the vignetting intensity to its default value.
        /// </summary>
        internal static void ResetVignettingIntensity()
        {
            SaveVignettingIntensity(VIGNETTING_DEFAULT_VALUE);
            LoadVignettingIntensity();
        }

        /// <summary>
        /// Reset the clouds preset to its default value.
        /// </summary>
        internal static void ResetCloudsPreset()
        {
            SaveCloudsPreset(CLOUD_PRESET_DEFAULT_VALUE);
            LoadCloudsPreset();
        }

        /// <summary>
        /// Reset the cloud shadows enabled state to its default value.
        /// </summary>
        internal static void ResetCloudShadowsEnabled()
        {
            SaveCloudShadowsEnabled(CLOUD_SHADOW_ENABLED_DEFAULT_STATE);
            LoadCloudShadowsEnabled();
        }

        /// <summary>
        /// Reset the cloud shadows opacity to its default value.
        /// </summary>
        internal static void ResetCloudShadowsOpacity()
        {
            SaveCloudShadowsOpacity(CLOUD_SHADOW_OPACITY_DEFAULT_STATE);
            LoadCloudShadowsOpacity();
        }

        /// <summary>
        /// Reset the wind type to its default value.
        /// </summary>
        internal static void ResetWindType()
        {
            SaveWindType(WIND_TYPE_DEFAULT_VALUE);
            LoadWindType();
        }

        /// <summary>
        /// Reset the capture resolution to its default value.
        /// </summary>
        internal static void ResetCaptureResolution()
        {
            SaveCaptureResolution(CAPTURE_RESOLUTION_DEFAULT_VALUE);
            LoadCaptureResolution();
        }

        /// <summary>
        /// Reset the capture encoder to its default value.
        /// </summary>
        internal static void ResetCaptureEncoder()
        {
            SaveCaptureEncoder(CAPTURE_ENCODER_DEFAULT_VALUE);
            LoadCaptureEncoder();
        }

        /// <summary>
        /// Reset the capture framerate to its default value.
        /// </summary>
        internal static void ResetCaptureFramerate()
        {
            SaveCaptureFramerate(CAPTURE_FRAMERATE_DEFAULT_VALUDE);
            LoadCaptureFramerate();
        }

        /// <summary>
        /// Reset the capture encoded logo state path to its default value.
        /// </summary>
        internal static void ResetCaptureEncodedLogo()
        {
            SaveCaptureEncodedLogo(CAPTURE_ENCODED_LOGO_DEFAULT_VALUE);
            LoadCaptureEncodedLogo();
        }
        #endregion
    }
}