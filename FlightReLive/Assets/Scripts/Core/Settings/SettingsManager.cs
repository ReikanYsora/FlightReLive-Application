using FlightReLive.Core.Cache;
using FlightReLive.Core.Loading;
using FlightReLive.UI;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FlightReLive.Core.Settings
{
    public static class SettingsManager
    {
        #region CONSTANTS
        internal static float PATH_3D_THICKNESS_DEFAULT_VALUE = 0.2f;
        internal static Color PATH_3D_REMAINING_COLOR_1_DEFAULT_VALUE = Color.white;
        internal static Color PATH_3D_REMAINING_COLOR_2_DEFAULT_VALUE = Color.black;
        internal static bool BUILDING_DISPLAY_STATE_DEFAULT_VALUE = true;
        internal static Color BUILDING_COLOR_DEFAULT_VALUE = Color.antiqueWhite;
        internal static float BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE = 0.9f;
        internal static float VIGNETTING_DEFAULT_VALUE = 0.3f;
        internal static float CONTRAST_OFFSET_DEFAULT_VALUE = 0f;
        internal static float SATURATION_OFFSET_DEFAULT_VALUE = 0f;
        internal static bool OUTLINE_DISPLAY_STATE_DEFAULT_VALUE = true;
        #endregion

        #region ATTRIBUTES
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
        public static event Action<float> OnCameraInertiaChanged;
        public static event Action<int> OnTilePaddingChanged;
        public static event Action<TimeZoneInfo> OnTimeZoneChanged;
        public static event Action<DateFormatStyle> OnDateFormatStyleChanged;
        public static event Action<TimeFormatStyle> OnTimeFormatStyleChanged;
        public static event Action<UnitSystemType> OnUnitSystemTypeChanged;
        public static event Action<string> OnWorkspacePathChanged;
        public static event Action<float> OnWorkspaceZoomChanged;
        public static event Action<string> OnMapTilerApiKeyChanged;
        public static event Action<float> OnGlobalScaleChanged;
        public static event Action<float> OnPath3DWidthChanged;
        public static event Action<Color> OnPath3DRemainingColor1Changed;
        public static event Action<Color> OnPath3DRemainingColor2Changed;
        public static event Action<bool> OnBuildingVisibilityChanged;
        public static event Action<Color> OnBuildingColorChanged;
        public static event Action<float> OnBuildingAOChanged;
        public static event Action<float> OnVignettingIntensityChanged;
        public static event Action<float> OnContrastOffsetChanged;
        public static event Action<float> OnSaturationOffsetChanged;
        public static event Action<bool> OnOutlineEnabledChanged;
        #endregion

        #region METHODS
        internal static void LoadDisplayWizard() =>
            CurrentSettings.DisplayWizard = PlayerPrefs.GetInt(nameof(Settings.DisplayWizard), 1) == 1;

        internal static void LoadApplicationTargetFPS() =>
            CurrentSettings.ApplicationTargetFPS = PlayerPrefs.GetInt(nameof(Settings.ApplicationTargetFPS), 120);

        internal static void LoadApplicationIdleFPS() =>
            CurrentSettings.ApplicationIdleFPS = PlayerPrefs.GetInt(nameof(Settings.ApplicationIdleFPS), 30);

        internal static void LoadDontAskWelcomeVersion() =>
            CurrentSettings.DontAskWelcomeVersion = PlayerPrefs.GetInt(nameof(Settings.DontAskWelcomeVersion), 0) == 1;

        internal static void LoadCameraRotationSpeed()
        {
            CurrentSettings.CameraRotationSpeed = PlayerPrefs.GetFloat(nameof(Settings.CameraRotationSpeed), 1f);
        }

        internal static void LoadCameraZoomSpeed()
        {
            CurrentSettings.CameraZoomSpeed = PlayerPrefs.GetFloat(nameof(Settings.CameraZoomSpeed), 1f);
        }

        internal static void LoadCameraInertia()
        {
            CurrentSettings.CameraInertia = PlayerPrefs.GetFloat(nameof(Settings.CameraInertia), 0.1f);
        }

        internal static void LoadTilePadding()
        {
            CurrentSettings.TilePadding = PlayerPrefs.GetInt(nameof(Settings.TilePadding), 3);
        }

        internal static void LoadTimeZone()
        {
            string tzId = PlayerPrefs.GetString(nameof(Settings.UserTimeZone), "UTC");
            CurrentSettings.UserTimeZone = ResolveTimeZone(tzId);
        }

        internal static void LoadDateFormatStyle() =>
            CurrentSettings.DateFormatStyle = (DateFormatStyle)PlayerPrefs.GetInt(nameof(Settings.DateFormatStyle), (int)DateFormatStyle.European);

        internal static void LoadTimeFormatStyle() =>
            CurrentSettings.TimeFormatStyle = (TimeFormatStyle)PlayerPrefs.GetInt(nameof(Settings.TimeFormatStyle), (int)TimeFormatStyle.TwentyFourHour);

        internal static void LoadUnitSystemType() =>
            CurrentSettings.UnitSystemType = (UnitSystemType)PlayerPrefs.GetInt(nameof(Settings.UnitSystemType), (int)UnitSystemType.Metric);

        internal static void LoadWorkspacePath() =>
            CurrentSettings.WorkspacePath = PlayerPrefs.GetString(nameof(Settings.WorkspacePath), Application.persistentDataPath);

        internal static void LoadWorkspaceZoom() =>
            CurrentSettings.WorkspaceZoom = PlayerPrefs.GetFloat(nameof(Settings.WorkspaceZoom), 1.0f);

        internal static void LoadMapTilerApiKey() =>
            CurrentSettings.MapTilerAPIKey = PlayerPrefs.GetString(nameof(Settings.MapTilerAPIKey), "");

        internal static void LoadGlobalScale() =>
            CurrentSettings.GlobalScale = PlayerPrefs.GetFloat(nameof(Settings.GlobalScale), 1f);

        internal static void LoadPath3DThickness() =>
            CurrentSettings.Path3DThickness = PlayerPrefs.GetFloat(nameof(Settings.Path3DThickness), PATH_3D_THICKNESS_DEFAULT_VALUE);

        internal static void LoadPath3DRemainingColor1()
        {
            Color color = PATH_3D_REMAINING_COLOR_1_DEFAULT_VALUE;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            string savedColorString = PlayerPrefs.GetString(nameof(Settings.Path3DRemainingColor1), colorString);
            string[] rgba = savedColorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.Path3DRemainingColor1 = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.Path3DRemainingColor1 = color;
            }
        }

        internal static void LoadPath3DRemainingColor2()
        {
            Color color = PATH_3D_REMAINING_COLOR_2_DEFAULT_VALUE;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            string savedColorString = PlayerPrefs.GetString(nameof(Settings.Path3DRemainingColor2), colorString);
            string[] rgba = savedColorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.Path3DRemainingColor2 = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.Path3DRemainingColor2 = color;
            }
        }

        internal static void LoadBuildingVisibility()
        {
            int intBool = BUILDING_DISPLAY_STATE_DEFAULT_VALUE ? 1 : 0;
            CurrentSettings.BuildingVisibility = PlayerPrefs.GetInt(nameof(Settings.BuildingVisibility), intBool) == 1;
        }

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

        internal static void LoadBuildingAO() =>
            CurrentSettings.BuildingAO = PlayerPrefs.GetFloat(nameof(Settings.BuildingAO), BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE);

        internal static void LoadCurrentVersion() =>
            CurrentSettings.CurrentVersion = PlayerPrefs.GetString(nameof(Settings.CurrentVersion), Application.version);

        internal static void LoadVignettingIntensity() =>
            CurrentSettings.VignettingIntensity = PlayerPrefs.GetFloat(nameof(Settings.VignettingIntensity), VIGNETTING_DEFAULT_VALUE);

        internal static void LoadContrastOffset() =>
            CurrentSettings.ContrastOffset = PlayerPrefs.GetFloat(nameof(Settings.ContrastOffset), CONTRAST_OFFSET_DEFAULT_VALUE);

        internal static void LoadSaturationOffset() =>
            CurrentSettings.SaturationOffset = PlayerPrefs.GetFloat(nameof(Settings.SaturationOffset), SATURATION_OFFSET_DEFAULT_VALUE);

        internal static void LoadOutlineEnabled()
        {
            int intBool = OUTLINE_DISPLAY_STATE_DEFAULT_VALUE ? 1 : 0;
            CurrentSettings.OutlineEnabled = PlayerPrefs.GetInt(nameof(Settings.OutlineEnabled), intBool) == 1;
        }

        internal static void SaveDisplayWizard(bool value)
        {
            CurrentSettings.DisplayWizard = value;
            PlayerPrefs.SetInt(nameof(Settings.DisplayWizard), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        internal static void SaveApplicationTargetFPS(int value)
        {
            CurrentSettings.ApplicationTargetFPS = value;
            PlayerPrefs.SetInt(nameof(Settings.ApplicationTargetFPS), value);
            PlayerPrefs.Save();
            OnApplicationTargetFPSChanged?.Invoke(value);
        }
        internal static void SaveDontAskWelcomeVersion(bool value)
        {
            CurrentSettings.DontAskWelcomeVersion = value;
            PlayerPrefs.SetInt(nameof(Settings.DontAskWelcomeVersion), value ? 1 : 0);
            PlayerPrefs.Save();
            OnDontAskWelcomeVersionChanged?.Invoke(value);
        }

        internal static void SaveApplicationIdleFPS(int value)
        {
            CurrentSettings.ApplicationIdleFPS = value;
            PlayerPrefs.SetInt(nameof(Settings.ApplicationIdleFPS), value);
            PlayerPrefs.Save();
            OnApplicationIdleFPSChanged?.Invoke(value);
        }

        internal static void SaveCameraRotationSpeed(float value)
        {
            CurrentSettings.CameraRotationSpeed = value;
            PlayerPrefs.SetFloat(nameof(Settings.CameraRotationSpeed), value);
            PlayerPrefs.Save();
            OnCameraRotationSpeedChanged?.Invoke(value);
        }

        internal static void SaveCameraZoomSpeed(float value)
        {
            CurrentSettings.CameraZoomSpeed = value;
            PlayerPrefs.SetFloat(nameof(Settings.CameraZoomSpeed), value);
            PlayerPrefs.Save();
            OnCameraZoomSpeedChanged?.Invoke(value);
        }

        internal static void SaveCameraInertia(float value)
        {
            CurrentSettings.CameraInertia = value;
            PlayerPrefs.SetFloat(nameof(Settings.CameraInertia), value);
            PlayerPrefs.Save();
            OnCameraInertiaChanged?.Invoke(value);
        }

        internal static void SaveTilePadding(int value)
        {
            CurrentSettings.TilePadding = value;
            PlayerPrefs.SetFloat(nameof(Settings.TilePadding), value);
            PlayerPrefs.Save();
            OnTilePaddingChanged?.Invoke(value);
        }

        internal static void SaveTimeZone(TimeZoneInfo timeZone)
        {
            CurrentSettings.UserTimeZone = timeZone;
            PlayerPrefs.SetString(nameof(Settings.UserTimeZone), timeZone.Id);
            PlayerPrefs.Save();
            OnTimeZoneChanged?.Invoke(timeZone);
        }

        internal static void SaveDateFormatStyle(DateFormatStyle value)
        {
            CurrentSettings.DateFormatStyle = value;
            PlayerPrefs.SetInt(nameof(Settings.DateFormatStyle), (int)value);
            PlayerPrefs.Save();
            OnDateFormatStyleChanged?.Invoke(value);
        }

        internal static void SaveTimeFormatStyle(TimeFormatStyle value)
        {
            CurrentSettings.TimeFormatStyle = value;
            PlayerPrefs.SetInt(nameof(Settings.TimeFormatStyle), (int)value);
            PlayerPrefs.Save();
            OnTimeFormatStyleChanged?.Invoke(value);
        }

        internal static void SaveUnitSystemType(UnitSystemType value)
        {
            CurrentSettings.UnitSystemType = value;
            PlayerPrefs.SetInt(nameof(Settings.UnitSystemType), (int)value);
            PlayerPrefs.Save();
            OnUnitSystemTypeChanged?.Invoke(value);
        }

        internal static void SaveWorkspacePath(string value)
        {
            CurrentSettings.WorkspacePath = value;
            PlayerPrefs.SetString(nameof(Settings.WorkspacePath), value);
            PlayerPrefs.Save();
            OnWorkspacePathChanged?.Invoke(value);
        }

        internal static void SaveWorkspaceZoom(float value)
        {
            CurrentSettings.WorkspaceZoom = value;
            PlayerPrefs.SetFloat(nameof(Settings.WorkspaceZoom), value);
            PlayerPrefs.Save();
            OnWorkspaceZoomChanged?.Invoke(value);
        }

        internal static void SaveMapTilerApiKey(string value)
        {
            CurrentSettings.MapTilerAPIKey = value;
            PlayerPrefs.SetString(nameof(Settings.MapTilerAPIKey), value);
            PlayerPrefs.Save();
            OnMapTilerApiKeyChanged?.Invoke(value);
        }

        internal static void SaveGlobalScale(float value)
        {
            CurrentSettings.GlobalScale = value;
            PlayerPrefs.SetFloat(nameof(Settings.GlobalScale), value);
            PlayerPrefs.Save();
            OnGlobalScaleChanged?.Invoke(value);
        }

        internal static void SaveCurrentVersion(string currentVersion)
        {
            CurrentSettings.CurrentVersion = currentVersion;
            PlayerPrefs.SetString(nameof(Settings.CurrentVersion), currentVersion);
            PlayerPrefs.Save();
        }

        internal static void SavePath3DThickness(float value)
        {
            CurrentSettings.Path3DThickness = value;
            PlayerPrefs.SetFloat(nameof(Settings.Path3DThickness), value);
            PlayerPrefs.Save();
            OnPath3DWidthChanged?.Invoke(value);
        }

        internal static void SavePath3DRemainingColor1(Color color)
        {
            CurrentSettings.Path3DRemainingColor1 = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.Path3DRemainingColor1), colorString);
            PlayerPrefs.Save();
            OnPath3DRemainingColor1Changed?.Invoke(color);
        }

        internal static void SavePath3DRemainingColor2(Color color)
        {
            CurrentSettings.Path3DRemainingColor2 = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.Path3DRemainingColor2), colorString);
            PlayerPrefs.Save();
            OnPath3DRemainingColor2Changed?.Invoke(color);
        }

        internal static void SaveBuildingVisibility(bool value)
        {
            CurrentSettings.BuildingVisibility = value;
            PlayerPrefs.SetInt(nameof(Settings.BuildingVisibility), value ? 1 : 0);
            PlayerPrefs.Save();
            OnBuildingVisibilityChanged?.Invoke(value);
        }

        internal static void SaveBuildingColor(Color color)
        {
            CurrentSettings.BuildingColor = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.BuildingColor), colorString);
            PlayerPrefs.Save();
            OnBuildingColorChanged?.Invoke(color);
        }

        internal static void SaveBuildingAO(float value)
        {
            CurrentSettings.BuildingAO = value;
            PlayerPrefs.SetFloat(nameof(Settings.BuildingAO), value);
            PlayerPrefs.Save();
            OnBuildingAOChanged?.Invoke(value);
        }
        
        internal static void SaveContrastOffset(float value)
        {
            CurrentSettings.ContrastOffset = value;
            PlayerPrefs.SetFloat(nameof(Settings.ContrastOffset), value);
            PlayerPrefs.Save();
            OnContrastOffsetChanged?.Invoke(value);
        }

        internal static void SaveSaturationOffset(float value)
        {
            CurrentSettings.SaturationOffset = value;
            PlayerPrefs.SetFloat(nameof(Settings.SaturationOffset), value);
            PlayerPrefs.Save();
            OnSaturationOffsetChanged?.Invoke(value);
        }

        internal static void SaveVignettingIntensity(float value)
        {
            CurrentSettings.VignettingIntensity = value;
            PlayerPrefs.SetFloat(nameof(Settings.VignettingIntensity), value);
            PlayerPrefs.Save();
            OnVignettingIntensityChanged?.Invoke(value);
        }

        internal static void SaveOutlineEnabled(bool value)
        {
            CurrentSettings.OutlineEnabled = value;
            PlayerPrefs.SetInt(nameof(Settings.OutlineEnabled), value ? 1 : 0);
            PlayerPrefs.Save();
            OnOutlineEnabledChanged?.Invoke(value);
        }

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
            LoadCameraInertia();
            LoadTilePadding();
            LoadTimeZone();
            LoadDateFormatStyle();
            LoadTimeFormatStyle();
            LoadUnitSystemType();
            LoadGlobalScale();
            LoadWorkspacePath();
            LoadWorkspaceZoom();
            LoadMapTilerApiKey();
            LoadPath3DThickness();
            LoadPath3DRemainingColor1();
            LoadPath3DRemainingColor2();
            LoadBuildingVisibility();
            LoadBuildingColor();
            LoadBuildingAO();
            LoadVignettingIntensity();
            LoadContrastOffset();
            LoadSaturationOffset();
            LoadOutlineEnabled();
        }

        internal static void LoadDefaultSettings()
        {
            SaveDisplayWizard(true);
            SaveCurrentVersion(Application.version);
            SaveApplicationTargetFPS(120);
            SaveApplicationIdleFPS(30);
            SaveDontAskWelcomeVersion(false);
            SaveCameraRotationSpeed(1f);
            SaveCameraZoomSpeed(1f);
            SaveCameraInertia(0.1f);
            SaveTilePadding(3);
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
            SavePath3DRemainingColor1(PATH_3D_REMAINING_COLOR_1_DEFAULT_VALUE);
            SavePath3DRemainingColor2(PATH_3D_REMAINING_COLOR_2_DEFAULT_VALUE);
            SaveBuildingVisibility(BUILDING_DISPLAY_STATE_DEFAULT_VALUE);
            SaveBuildingColor(BUILDING_COLOR_DEFAULT_VALUE);
            SaveBuildingAO(BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE);
            SaveContrastOffset(CONTRAST_OFFSET_DEFAULT_VALUE);
            SaveSaturationOffset(SATURATION_OFFSET_DEFAULT_VALUE);
            SaveOutlineEnabled(OUTLINE_DISPLAY_STATE_DEFAULT_VALUE);

            PlayerPrefs.SetInt("SettingsInitialized", 1);
            PlayerPrefs.Save();
        }

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

        internal static string FormatUtcOffset(TimeSpan offset)
        {
            string sign = offset.TotalMinutes >= 0 ? "+" : "-";
            int hours = Math.Abs(offset.Hours);
            int minutes = Math.Abs(offset.Minutes);
            return $"{sign}{hours:D2}:{minutes:D2}";
        }

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

        internal static string GetEnumLabel<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            string raw = value.ToString();
            string spaced = System.Text.RegularExpressions.Regex.Replace(raw, "_", " ");
            string titleCase = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());

            return titleCase;
        }

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
        internal static void DisplaySettingsColorPickerWithReset(FuGrid grid, string text, string tooltipText, string tooltipReset, Color value, Color defaultValue, Action<Color> onChange, Action onReset)
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            Vector4 tempValue = value;

            if (grid.ColorPicker(text, ref tempValue))
            {
                onChange?.Invoke((Color) tempValue);
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

        internal static void DisplaySettingsComboboxWithReset<TEnum>(FuGrid grid,string text, string tooltipText, string tooltipReset, TEnum value, TEnum defaultValue, Func<TEnum, string> getLabel, IEnumerable<TEnum> allowedValues, Action<TEnum> onChange, Action onReset) where TEnum : struct, Enum
        {
            if (!string.IsNullOrEmpty(tooltipText))
            {
                grid.SetNextElementToolTip(tooltipText);
            }

            string currentLabel = getLabel(value);

            grid.Combobox($"{text}##Combobox", currentLabel, () =>
            {
                foreach (TEnum option in allowedValues)
                {
                    bool isSelected = EqualityComparer<TEnum>.Default.Equals(option, value);
                    string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {getLabel(option)}";

                    if (ImGui.Selectable(label))
                    {
                        onChange?.Invoke(option);
                    }
                }
            });

            if (EqualityComparer<TEnum>.Default.Equals(value, defaultValue))
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

        private static bool AreApproximatelyEqual(float a, float b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }

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

                //layout.Collapsable("Upscaler settings##collapsable", () =>
                //{
                //    using (FuGrid upscalerGrid = new FuGrid("upscalerGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                //    {
                //        upscalerGrid.SetNextElementToolTipWithLabel("Choose your upscaling method for rendering performance and quality.");

                //        UpscalerName currentUpscaler = CurrentSettings.UpscalerName;
                //        string upscalerLabel = GetUpscalerLabel(currentUpscaler);

                //        upscalerGrid.Combobox("Upscaler##UpscalerCombobox", upscalerLabel, () =>
                //        {
                //            List<UpscalerName> allowedUpscalers = new List<UpscalerName>() { UpscalerName.None };
                //            allowedUpscalers.AddRange(TNDUpscaler.GetSupported());

                //            foreach (UpscalerName upscaler in allowedUpscalers)
                //            {
                //                bool isSelected = upscaler == currentUpscaler;
                //                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {GetUpscalerLabel(upscaler)}";

                //                if (ImGui.Selectable(label))
                //                {
                //                    SaveUpscalerName(upscaler);
                //                }
                //            }
                //        });

                //        if (upscalerLabel == "None")
                //        {
                //            upscalerGrid.DisableNextElements();
                //        }

                //        upscalerGrid.SetNextElementToolTipWithLabel("Select the upscaling quality level (higher = better visuals, lower = better performance) for the ReLive camera scene.");

                //        UpscalerQuality currentQuality = CurrentSettings.UpscalerQuality;
                //        string qualityLabel = GetUpscalerQualityLabel(currentQuality);

                //        upscalerGrid.Combobox("Upscaler quality##UpscalerQualityCombobox", qualityLabel, () =>
                //        {
                //            UpscalerQuality[] allowedQualities = new[]
                //            {
                //                UpscalerQuality.NativeAA,
                //                UpscalerQuality.UltraQuality,
                //                UpscalerQuality.Quality,
                //                UpscalerQuality.Balanced,
                //                UpscalerQuality.Performance,
                //                UpscalerQuality.UltraPerformance,
                //                UpscalerQuality.Off
                //            };

                //            foreach (UpscalerQuality quality in allowedQualities)
                //            {
                //                bool isSelected = quality == currentQuality;
                //                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {GetUpscalerQualityLabel(quality)}";

                //                if (ImGui.Selectable(label))
                //                {
                //                    SaveUpscalerQuality(quality);
                //                }
                //            }
                //        });

                //        upscalerGrid.SetNextElementToolTip("Enhances edge clarity and texture detail after upscaling. Recommended to preserve visual sharpness.");
                //        bool sharpeningEnabled = CurrentSettings.UpscalerSharpeningEnabled;

                //        if (upscalerGrid.Toggle("Upscaler sharpening", ref sharpeningEnabled))
                //        {
                //            SaveUpscalerSharpeningEnabled(sharpeningEnabled);
                //        }

                //        if (!sharpeningEnabled)
                //        {
                //            upscalerGrid.DisableNextElement();
                //        }

                //        upscalerGrid.SetNextElementToolTipWithLabel("Adjust how sharp the image appears after upscaling. Higher values increase detail, but may introduce noise.");
                //        float sharpeness = CurrentSettings.UpscalerSharpeness;

                //        if (upscalerGrid.Slider("Upscaler sharpeness", ref sharpeness, 0f, 1f, 0.01f, format: "%.2f"))
                //        {
                //            SaveUpscalerSharpeness(sharpeness);
                //        }
                //    }

                //    Fugui.PopFont();
                //}, FuButtonStyle.Collapsable, defaultOpen: true);

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

                        if (rotationSpeedGrid.Slider("Camera rotation speed", ref rotationSpeed, 1, 5f, 0.1f, format: "%.1f"))
                        {
                            SaveCameraRotationSpeed(rotationSpeed);
                        }
                    }

                    using (FuGrid zoomSpeedGrid = new FuGrid("zoomSpeedGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        zoomSpeedGrid.SetNextElementToolTipWithLabel("This setting defines the camera zoom speed when scrolling.");

                        float zoomSpeed = CurrentSettings.CameraZoomSpeed;

                        if (zoomSpeedGrid.Slider("Camera zoom speed", ref zoomSpeed, 1f, 5f, 0.1f, format: "%.1f"))
                        {
                            SaveCameraZoomSpeed(zoomSpeed);
                        }
                    }

                    using (FuGrid inertiaGrid = new FuGrid("inertiaSpeedGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        inertiaGrid.SetNextElementToolTipWithLabel("This setting allows you to define the inertia of the camera during rotation & zoom.");

                        float inertiaSpeed = CurrentSettings.CameraInertia;

                        if (inertiaGrid.Slider("Camera inertia", ref inertiaSpeed, 0f, 1f, 0.01f, format: "%.2f"))
                        {
                            SaveCameraInertia(inertiaSpeed);
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

                        int tilePadding = CurrentSettings.TilePadding;
                        apiGrid.SetNextElementToolTipWithLabel("Defines the number of additional tile rows around the flight area. Increases the realism of the scene but affects performance and the amount of resources downloaded.");
                        
                        if (apiGrid.Slider("TilePadding", ref tilePadding, 1, 5))
                        {
                            SaveTilePadding(tilePadding);
                        }
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

        internal static void ResetPath3DThickness()
        {
            SavePath3DThickness(PATH_3D_THICKNESS_DEFAULT_VALUE);
            LoadPath3DThickness();
        }

        internal static void ResetPath3DRemainingColor1()
        {
            SavePath3DRemainingColor1(PATH_3D_REMAINING_COLOR_1_DEFAULT_VALUE);
            LoadPath3DRemainingColor1();
        }

        internal static void ResetPath3DRemainingColor2()
        {
            SavePath3DRemainingColor2(PATH_3D_REMAINING_COLOR_2_DEFAULT_VALUE);
            LoadPath3DRemainingColor2();
        }

        internal static void ResetBuildingVisibility()
        {
            SaveBuildingVisibility(BUILDING_DISPLAY_STATE_DEFAULT_VALUE);
            LoadBuildingVisibility();
        }

        internal static void ResetBuildingColor()
        {
            SaveBuildingColor(BUILDING_COLOR_DEFAULT_VALUE);
            LoadBuildingColor();
        }

        internal static void ResetBuildingAO()
        {
            SaveBuildingAO(BUILDING_AMBIENT_OCCLUSION_DEFAULT_VALUE);
            LoadBuildingAO();
        }

        internal static void ResetContrastOffset()
        {
            SaveContrastOffset(CONTRAST_OFFSET_DEFAULT_VALUE);
            LoadContrastOffset();
        }

        internal static void ResetSaturationOffset()
        {
            SaveSaturationOffset(SATURATION_OFFSET_DEFAULT_VALUE);
            LoadSaturationOffset();
        }

        internal static void ResetVignettingIntensity()
        {
            SaveVignettingIntensity(VIGNETTING_DEFAULT_VALUE);
            LoadVignettingIntensity();
        }

        internal static void ResetOutlineEnabled()
        {
            SaveOutlineEnabled(OUTLINE_DISPLAY_STATE_DEFAULT_VALUE);
            LoadOutlineEnabled();
        }
        #endregion
    }
}
