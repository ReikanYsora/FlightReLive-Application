using FlightReLive.Core.Cache;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Workspace;
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
        public static Settings CurrentSettings { get; private set; } = new Settings();

        #endregion

        #region EVENTS
        public static event Action<QualityPreset> OnHardwareQualityPresetChanged;
        public static event Action<QualityPreset> OnMapQualityPresetChanged;
        public static event Action<int> OnApplicationTargetFPSChanged;
        public static event Action<int> OnApplicationIdleFPSChanged;
        public static event Action<bool> OnDontAskWelcomeVersionChanged;
        public static event Action<float> OnCameraRotationSpeedChanged;
        public static event Action<float> OnCameraZoomSpeedChanged;
        public static event Action<float> OnCameraInertiaChanged;
        public static event Action<TimeZoneInfo> OnTimeZoneChanged;
        public static event Action<DateFormatStyle> OnDateFormatStyleChanged;
        public static event Action<TimeFormatStyle> OnTimeFormatStyleChanged;
        public static event Action<UnitSystemType> OnUnitSystemTypeChanged;
        public static event Action<string> OnWorkspacePathChanged;
        public static event Action<float> OnWorkspaceZoomChanged;
        public static event Action<string> OnMapTilerApiKeyChanged;
        public static event Action<SatelliteTileQualityPreset> OnSatelliteTileQualityPresetChanged;
        public static event Action<int> OnTilePaddingChanged;
        public static event Action<float> OnGlobalScaleChanged;
        //public static event Action<PointCloudMode> OnPointCloudModeChanged;
        //public static event Action<float> OnAbsoluteAltitudeMinChanged;
        //public static event Action<float> OnAbsoluteAltitudeMaxChanged;
        //public static event Action<float> OnHeightPointSizeChanged;
        //public static event Action<float> OnHeightOpacityChanged;
        public static event Action<float> OnPathWidthChanged;
        public static event Action<Color> OnPathRemainingColor1Changed;
        public static event Action<Color> OnPathRemainingColor2Changed;
        public static event Action<float> OnWorldIconScaleChanged;
        public static event Action<float> OnWorldIconHeightChanged;
        public static event Action<bool> OnOutlineVisibilityChanged;
        public static event Action<bool> OnBuildingVisibilityChanged;
        public static event Action<bool> On3DIconVisibilityChanged;
        public static event Action<Color> OnCameraCaptureBackgroundColorChanged;
        public static event Action<int> OnCaptureResolutionChanged;
        public static event Action<int> OnCaptureEncoderChanged;
        public static event Action<int> OnCaptureFramerateChanged;
        public static event Action<bool> OnCaptureEncodedLogoChanged;
        public static event Action<string> OnCaptureOutputPathChanged;
        public static event Action<bool> OnCaptureReplaceBackgroundChanged;
        public static event Action<float> OnVignettingIntensityChanged;
        public static event Action<bool> OnDepthOfFieldEnabledChanged;
        public static event Action<float> OnDepthOfFieldStartChanged;
        public static event Action<float> OnDepthOfFieldEndChanged;
        #endregion

        #region METHODS
        internal static void LoadHardwareQualityPreset() =>
            CurrentSettings.HardwareQualityPreset = (QualityPreset)PlayerPrefs.GetInt(nameof(Settings.HardwareQualityPreset), (int)QualityPreset.Quality);

        internal static void LoadMapQualityPreset() =>
            CurrentSettings.MapQualityPreset = (QualityPreset)PlayerPrefs.GetInt(nameof(Settings.MapQualityPreset), (int)QualityPreset.Quality);

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

        internal static void LoadSatelliteTileQualityPreset() =>
            CurrentSettings.SatelliteTileQualityPreset = (SatelliteTileQualityPreset)PlayerPrefs.GetInt(nameof(Settings.SatelliteTileQualityPreset), (int)SatelliteTileQualityPreset.High);

        internal static void LoadTilePadding() =>
            CurrentSettings.TilePadding = PlayerPrefs.GetInt(nameof(Settings.TilePadding), 1);

        internal static void LoadGlobalScale() =>
            CurrentSettings.GlobalScale = PlayerPrefs.GetFloat(nameof(Settings.GlobalScale), 1f);

        //internal static void LoadPointCloudMode() =>
        //    CurrentSettings.PointCloudMode = (PointCloudMode)PlayerPrefs.GetInt(nameof(Settings.PointCloudMode), (int)PointCloudMode.Relative);

        //internal static void LoadAbsoluteAltitudeMin() =>
        //    CurrentSettings.AbsoluteAltitudeMin = PlayerPrefs.GetFloat(nameof(Settings.AbsoluteAltitudeMin), 0f);

        //internal static void LoadAbsoluteAltitudeMax() =>
        //    CurrentSettings.AbsoluteAltitudeMax = PlayerPrefs.GetFloat(nameof(Settings.AbsoluteAltitudeMax), 1000f);

        //internal static void LoadHeightPointSize() =>
        //    CurrentSettings.HeightPointSize = PlayerPrefs.GetFloat(nameof(Settings.HeightPointSize), 0.3f);

        //internal static void LoadHeightOpacity() =>
        //    CurrentSettings.HeightOpacity = PlayerPrefs.GetFloat(nameof(Settings.HeightOpacity), 0.05f);

        internal static void LoadPathWidth() =>
            CurrentSettings.PathWidth = PlayerPrefs.GetFloat(nameof(Settings.PathWidth), 0.15f);

        internal static void LoadPathRemainingColor1()
        {
            string colorString = PlayerPrefs.GetString(nameof(Settings.PathRemainingColor1), "0.007,0.007,0.007,1");
            string[] rgba = colorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.PathRemainingColor1 = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.PathRemainingColor1 = new Color(0.007f, 0.007f, 0.007f, 1f);
            }
        }
        internal static void LoadPathRemainingColor2()
        {
            string colorString = PlayerPrefs.GetString(nameof(Settings.PathRemainingColor2), "0.141,0.141,0.141,1");
            string[] rgba = colorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.PathRemainingColor2 = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.PathRemainingColor2 = new Color(0.141f, 0.141f, 0.141f, 1f);
            }
        }

        internal static void LoadWorldIconScale() =>
            CurrentSettings.WorldIconScale = PlayerPrefs.GetFloat(nameof(Settings.WorldIconScale), 0.5f);

        internal static void LoadWorldIconHeight() =>
            CurrentSettings.WorldIconHeight = PlayerPrefs.GetFloat(nameof(Settings.WorldIconHeight), 5f);

        internal static void LoadBuildingVisibility() =>
            CurrentSettings.BuildingVisibility = PlayerPrefs.GetInt(nameof(Settings.BuildingVisibility), 1) == 1;

        internal static void LoadOutlineVisibility() =>
            CurrentSettings.OutlineVisibility = PlayerPrefs.GetInt(nameof(Settings.OutlineVisibility), 1) == 1;

        internal static void LoadIcon3DVisibility() =>
            CurrentSettings.Icon3DVisibility = PlayerPrefs.GetInt(nameof(Settings.Icon3DVisibility), 1) == 1;

        internal static void LoadCaptureResolution() =>
            CurrentSettings.CaptureResolution = PlayerPrefs.GetInt(nameof(Settings.CaptureResolution), 1);

        internal static void LoadCaptureEncoder() =>
            CurrentSettings.CaptureEncoder = PlayerPrefs.GetInt(nameof(Settings.CaptureEncoder), 0);

        internal static void LoadCaptureFramerate() =>
            CurrentSettings.CaptureFramerate = PlayerPrefs.GetInt(nameof(Settings.CaptureFramerate), 1);

        internal static void LoadCurrentVersion() =>
            CurrentSettings.CurrentVersion = PlayerPrefs.GetString(nameof(Settings.CurrentVersion), Application.version);

        internal static void LoadCameraCaptureBackgroundColor()
        {
            string colorString = PlayerPrefs.GetString(nameof(Settings.CameraCaptureBackgroundColor), "0,0.694,0.251,1");
            string[] rgba = colorString.Split(',');

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(rgba[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(rgba[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(rgba[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                CurrentSettings.CameraCaptureBackgroundColor = new Color(r, g, b, a);
            }
            else
            {
                CurrentSettings.CameraCaptureBackgroundColor = new Color(0f, 0.694f, 0.251f, 1f);
            }
        }

        internal static void LoadCaptureOutputPath() =>
            CurrentSettings.CaptureOutputPath = PlayerPrefs.GetString(
            nameof(Settings.CaptureOutputPath),
            Path.Combine(Application.persistentDataPath, "Captures")
        );

        internal static void LoadCaptureReplaceBackground() =>
            CurrentSettings.CaptureReplaceBackground = PlayerPrefs.GetInt(nameof(Settings.CaptureReplaceBackground), 0) == 1;

        internal static void LoadCaptureEncodedLogo() =>
            CurrentSettings.CaptureEncodedLogo = PlayerPrefs.GetInt(nameof(Settings.CaptureEncodedLogo), 1) == 1;

        internal static void LoadVignettingIntensity() =>
            CurrentSettings.VignettingIntensity = PlayerPrefs.GetFloat(nameof(Settings.VignettingIntensity), 0.3f);

        internal static void LoadDepthOfFieldEnabled() =>
            CurrentSettings.DepthOfFieldEnabled = PlayerPrefs.GetInt(nameof(Settings.DepthOfFieldEnabled), 0) == 1;

        internal static void LoadDepthOfFieldStart() =>
            CurrentSettings.DepthOfFieldStart = PlayerPrefs.GetFloat(nameof(Settings.DepthOfFieldStart), 200f);

        internal static void LoadDepthOfFieldEnd() =>
            CurrentSettings.DepthOfFieldEnd = PlayerPrefs.GetFloat(nameof(Settings.DepthOfFieldEnd), 400f);

        internal static void SaveHardwareQualityPreset(QualityPreset value)
        {
            CurrentSettings.HardwareQualityPreset = value;
            PlayerPrefs.SetInt(nameof(Settings.HardwareQualityPreset), (int)value);
            PlayerPrefs.Save();
            OnHardwareQualityPresetChanged?.Invoke(value);
        }

        internal static void SaveMapQualityPreset(QualityPreset value)
        {
            CurrentSettings.MapQualityPreset = value;
            PlayerPrefs.SetInt(nameof(Settings.MapQualityPreset), (int)value);
            PlayerPrefs.Save();
            OnMapQualityPresetChanged?.Invoke(value);
        }

        internal static void SaveApplicationTargetFPS(int value)
        {
            CurrentSettings.ApplicationTargetFPS = value;
            PlayerPrefs.SetInt(nameof(Settings.ApplicationTargetFPS), value);
            PlayerPrefs.Save();
            OnApplicationTargetFPSChanged?.Invoke(value);
        }

        internal static void SaveApplicationIdleFPS(int value)
        {
            CurrentSettings.ApplicationIdleFPS = value;
            PlayerPrefs.SetInt(nameof(Settings.ApplicationIdleFPS), value);
            PlayerPrefs.Save();
            OnApplicationIdleFPSChanged?.Invoke(value);
        }

        internal static void SaveDontAskWelcomeVersion(bool value)
        {
            CurrentSettings.DontAskWelcomeVersion = value;
            PlayerPrefs.SetInt(nameof(Settings.DontAskWelcomeVersion), value ? 1 : 0);
            PlayerPrefs.Save();
            OnDontAskWelcomeVersionChanged?.Invoke(value);
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

        internal static void SaveSatelliteTileQualityPreset(SatelliteTileQualityPreset value)
        {
            CurrentSettings.SatelliteTileQualityPreset = value;
            PlayerPrefs.SetInt(nameof(Settings.SatelliteTileQualityPreset), (int)value);
            PlayerPrefs.Save();
            OnSatelliteTileQualityPresetChanged?.Invoke(value);
        }

        internal static void SaveTilePadding(int value)
        {
            CurrentSettings.TilePadding = value;
            PlayerPrefs.SetInt(nameof(Settings.TilePadding), value);
            PlayerPrefs.Save();
            OnTilePaddingChanged?.Invoke(value);
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

        //internal static void SavePointCloudMode(PointCloudMode value)
        //{
        //    CurrentSettings.PointCloudMode = value;
        //    PlayerPrefs.SetInt(nameof(Settings.PointCloudMode), (int)value);
        //    PlayerPrefs.Save();
        //    OnPointCloudModeChanged?.Invoke(value);
        //}

        //internal static void SaveAbsoluteAltitudeMin(float value)
        //{
        //    CurrentSettings.AbsoluteAltitudeMin = value;
        //    PlayerPrefs.SetFloat(nameof(Settings.AbsoluteAltitudeMin), value);
        //    PlayerPrefs.Save();
        //    OnAbsoluteAltitudeMinChanged?.Invoke(value);
        //}

        //internal static void SaveAbsoluteAltitudeMax(float value)
        //{
        //    CurrentSettings.AbsoluteAltitudeMax = value;
        //    PlayerPrefs.SetFloat(nameof(Settings.AbsoluteAltitudeMax), value);
        //    PlayerPrefs.Save();
        //    OnAbsoluteAltitudeMaxChanged?.Invoke(value);
        //}

        //internal static void SaveHeightPointSize(float value)
        //{
        //    CurrentSettings.HeightPointSize = value;
        //    PlayerPrefs.SetFloat(nameof(Settings.HeightPointSize), value);
        //    PlayerPrefs.Save();
        //    OnHeightPointSizeChanged?.Invoke(value);
        //}

        //internal static void SaveHeightOpacity(float value)
        //{
        //    CurrentSettings.HeightOpacity = value;
        //    PlayerPrefs.SetFloat(nameof(Settings.HeightOpacity), value);
        //    PlayerPrefs.Save();
        //    OnHeightOpacityChanged?.Invoke(value);
        //}

        internal static void SavePathWidth(float value)
        {
            CurrentSettings.PathWidth = value;
            PlayerPrefs.SetFloat(nameof(Settings.PathWidth), value);
            PlayerPrefs.Save();
            OnPathWidthChanged?.Invoke(value);
        }

        internal static void SavePathRemainingColor1(Color color)
        {
            CurrentSettings.PathRemainingColor1 = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.PathRemainingColor1), colorString);
            PlayerPrefs.Save();
            OnPathRemainingColor1Changed?.Invoke(color);
        }

        internal static void SavePathRemainingColor2(Color color)
        {
            CurrentSettings.PathRemainingColor2 = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.PathRemainingColor2), colorString);
            PlayerPrefs.Save();
            OnPathRemainingColor2Changed?.Invoke(color);
        }

        internal static void SaveWorldIconScale(float value)
        {
            CurrentSettings.WorldIconScale = value;
            PlayerPrefs.SetFloat(nameof(Settings.WorldIconScale), value);
            PlayerPrefs.Save();
            OnWorldIconScaleChanged?.Invoke(value);
        }

        internal static void SaveWorldIconHeight(float value)
        {
            CurrentSettings.WorldIconHeight = value;
            PlayerPrefs.SetFloat(nameof(Settings.WorldIconHeight), value);
            PlayerPrefs.Save();
            OnWorldIconHeightChanged?.Invoke(value);
        }

        internal static void SaveBuildingVisibility(bool value)
        {
            CurrentSettings.BuildingVisibility = value;
            PlayerPrefs.SetInt(nameof(Settings.BuildingVisibility), value ? 1 : 0);
            PlayerPrefs.Save();
            OnBuildingVisibilityChanged?.Invoke(value);
        }

        internal static void SaveOutlineVisibility(bool value)
        {
            CurrentSettings.OutlineVisibility = value;
            PlayerPrefs.SetInt(nameof(Settings.OutlineVisibility), value ? 1 : 0);
            PlayerPrefs.Save();
            OnOutlineVisibilityChanged?.Invoke(value);
        }

        internal static void Save3DIconVisibility(bool value)
        {
            CurrentSettings.Icon3DVisibility = value;
            PlayerPrefs.SetInt(nameof(Settings.Icon3DVisibility), value ? 1 : 0);
            PlayerPrefs.Save();
            On3DIconVisibilityChanged?.Invoke(value);
        }

        internal static void SaveCaptureResolution(int value)
        {
            CurrentSettings.CaptureResolution = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureResolution), value);
            PlayerPrefs.Save();
            OnCaptureResolutionChanged?.Invoke(value);
        }

        internal static void SaveCaptureEncoder(int value)
        {
            CurrentSettings.CaptureEncoder = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureEncoder), value);
            PlayerPrefs.Save();
            OnCaptureEncoderChanged?.Invoke(value);
        }

        internal static void SaveCaptureFramerate(int value)
        {
            CurrentSettings.CaptureFramerate = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureFramerate), value);
            PlayerPrefs.Save();
            OnCaptureFramerateChanged?.Invoke(value);
        }

        internal static void SaveCaptureOutputPath(string value)
        {
            CurrentSettings.CaptureOutputPath = value;
            PlayerPrefs.SetString(nameof(Settings.CaptureOutputPath), value);
            PlayerPrefs.Save();
            OnCaptureOutputPathChanged?.Invoke(value);
        }

        internal static void SaveCameraCaptureBackgroundColor(Color color)
        {
            CurrentSettings.CameraCaptureBackgroundColor = color;
            string colorString = $"{color.r.ToString(CultureInfo.InvariantCulture)},{color.g.ToString(CultureInfo.InvariantCulture)},{color.b.ToString(CultureInfo.InvariantCulture)},{color.a.ToString(CultureInfo.InvariantCulture)}";
            PlayerPrefs.SetString(nameof(Settings.CameraCaptureBackgroundColor), colorString);
            PlayerPrefs.Save();
            OnCameraCaptureBackgroundColorChanged?.Invoke(color);
        }

        internal static void SaveCaptureReplaceBackground(bool value)
        {
            CurrentSettings.CaptureReplaceBackground = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureReplaceBackground), value ? 1 : 0);
            PlayerPrefs.Save();
            OnCaptureReplaceBackgroundChanged?.Invoke(value);
        }

        internal static void SaveCaptureEncodedLogo(bool value)
        {
            CurrentSettings.CaptureEncodedLogo = value;
            PlayerPrefs.SetInt(nameof(Settings.CaptureEncodedLogo), value ? 1 : 0);
            PlayerPrefs.Save();
            OnCaptureEncodedLogoChanged?.Invoke(value);
        }

        internal static void SaveVignettingIntensity(float value)
        {
            CurrentSettings.VignettingIntensity = value;
            PlayerPrefs.SetFloat(nameof(Settings.VignettingIntensity), value);
            PlayerPrefs.Save();
            OnVignettingIntensityChanged?.Invoke(value);
        }

        internal static void SaveDepthOfFieldEnabled(bool value)
        {
            CurrentSettings.DepthOfFieldEnabled = value;
            PlayerPrefs.SetInt(nameof(Settings.DepthOfFieldEnabled), value ? 1 : 0);
            PlayerPrefs.Save();
            OnDepthOfFieldEnabledChanged?.Invoke(value);
        }

        internal static void SaveDepthOfFieldStart(float value)
        {
            CurrentSettings.DepthOfFieldStart = value;
            PlayerPrefs.SetFloat(nameof(Settings.DepthOfFieldStart), value);
            PlayerPrefs.Save();
            OnDepthOfFieldStartChanged?.Invoke(value);
        }

        internal static void SaveDepthOfFieldEnd(float value)
        {
            CurrentSettings.DepthOfFieldEnd = value;
            PlayerPrefs.SetFloat(nameof(Settings.DepthOfFieldEnd), value);
            PlayerPrefs.Save();
            OnDepthOfFieldEndChanged?.Invoke(value);
        }

        internal static void LoadAll()
        {
            if (!PlayerPrefs.HasKey("SettingsInitialized"))
            {
                LoadDefaultSettings();
            }

            LoadCurrentVersion();
            LoadHardwareQualityPreset();
            LoadMapQualityPreset();
            LoadApplicationTargetFPS();
            LoadApplicationIdleFPS();
            LoadDontAskWelcomeVersion();
            LoadCameraRotationSpeed();
            LoadCameraZoomSpeed();
            LoadCameraInertia();
            LoadTimeZone();
            LoadDateFormatStyle();
            LoadTimeFormatStyle();
            LoadUnitSystemType();
            LoadGlobalScale();
            LoadWorkspacePath();
            LoadWorkspaceZoom();
            LoadMapTilerApiKey();
            LoadSatelliteTileQualityPreset();
            LoadTilePadding();
            //LoadPointCloudMode();
            //LoadAbsoluteAltitudeMin();
            //LoadAbsoluteAltitudeMax();
            //LoadHeightPointSize();
            //LoadHeightOpacity();
            LoadPathWidth();
            LoadPathRemainingColor1();
            LoadPathRemainingColor2();
            LoadWorldIconScale();
            LoadWorldIconHeight();
            LoadIcon3DVisibility();
            LoadBuildingVisibility();
            LoadOutlineVisibility();
            LoadCaptureResolution();
            LoadCaptureEncoder();
            LoadCaptureFramerate();
            LoadCaptureOutputPath();
            LoadCameraCaptureBackgroundColor();
            LoadCaptureReplaceBackground();
            LoadCaptureEncodedLogo();
            LoadVignettingIntensity();
            LoadDepthOfFieldEnabled();
            LoadDepthOfFieldStart();
            LoadDepthOfFieldEnd();
        }

        internal static void LoadDefaultSettings()
        {
            SaveCurrentVersion(Application.version);
            SaveHardwareQualityPreset(QualityPreset.Quality);
            SaveMapQualityPreset(QualityPreset.Quality);
            SaveApplicationTargetFPS(120);
            SaveApplicationIdleFPS(30);
            SaveDontAskWelcomeVersion(false);
            SaveCameraRotationSpeed(1f);
            SaveCameraZoomSpeed(1f);
            SaveCameraInertia(0.1f);
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
            SaveSatelliteTileQualityPreset(SatelliteTileQualityPreset.High);
            SaveTilePadding(1);
            //SavePointCloudMode(PointCloudMode.Disabled);
            //SaveAbsoluteAltitudeMin(0f);
            //SaveAbsoluteAltitudeMax(1000f);
            //SaveHeightPointSize(0.3f);
            //SaveHeightOpacity(0.05f);
            SavePathWidth(0.15f);
            SavePathRemainingColor1(new Color(0.007f, 0.007f, 0.007f, 1f));
            SavePathRemainingColor2(new Color(0.141f, 0.141f, 0.141f, 1f));
            SaveWorldIconScale(0.5f);
            SaveWorldIconHeight(5f);
            SaveBuildingVisibility(true);
            SaveOutlineVisibility(true);
            Save3DIconVisibility(true);
            SaveCaptureResolution(1);
            SaveCaptureEncoder(0);
            SaveCaptureFramerate(1);
            SaveCaptureOutputPath(Path.Combine(Application.persistentDataPath, "Captures"));
            SaveCameraCaptureBackgroundColor(new Color(0f, 0.694f, 0.251f, 1f));
            SaveCaptureReplaceBackground(false);
            SaveCaptureEncodedLogo(true);
            SaveVignettingIntensity(0.3f);
            SaveDepthOfFieldEnabled(true);
            SaveDepthOfFieldStart(200f);
            SaveDepthOfFieldEnd(400f);

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

        private static string FormatUtcOffset(TimeSpan offset)
        {
            string sign = offset.TotalMinutes >= 0 ? "+" : "-";
            int hours = Math.Abs(offset.Hours);
            int minutes = Math.Abs(offset.Minutes);
            return $"{sign}{hours:D2}:{minutes:D2}";
        }

        private static string GetDateFormatLabel(DateFormatStyle style)
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

        private static string GetTimeFormatLabel(TimeFormatStyle style)
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

        private static string FormatSatellitePresetLabel(SatelliteTileQualityPreset preset)
        {
            switch (preset)
            {
                case SatelliteTileQualityPreset.VeryLow:
                    return "Very Low";
                case SatelliteTileQualityPreset.Low:
                    return "Low";
                case SatelliteTileQualityPreset.Normal:
                    return "Normal";
                case SatelliteTileQualityPreset.High:
                    return "High";
                case SatelliteTileQualityPreset.VeryHigh:
                    return "Very High";
                case SatelliteTileQualityPreset.Extreme:
                    return "Extreme";
                default:
                    return preset.ToString();
            }
        }

        internal static int GetSatelliteTileZoom()
        {
            int satelliteZoomLevel;

            switch (CurrentSettings.SatelliteTileQualityPreset)
            {
                case SatelliteTileQualityPreset.VeryLow:
                    satelliteZoomLevel = 14;
                    break;
                case SatelliteTileQualityPreset.Low:
                    satelliteZoomLevel = 15;
                    break;
                default:
                case SatelliteTileQualityPreset.Normal:
                    satelliteZoomLevel = 16;
                    break;
                case SatelliteTileQualityPreset.High:
                    satelliteZoomLevel = 17;
                    break;
                case SatelliteTileQualityPreset.VeryHigh:
                    satelliteZoomLevel = 18;
                    break;
                case SatelliteTileQualityPreset.Extreme:
                    satelliteZoomLevel = 19;
                    break;
            }

            return satelliteZoomLevel;
        }
        #endregion

        #region UI
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

                layout.Collapsable("Quality settings##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid hardwareQualityGrid = new FuGrid("hardwareQualityGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        hardwareQualityGrid.SetNextElementToolTipWithLabel("This parameter defines the graphic quality level of the scene. This includes: texture detail levels, shadows, lighting, and post-processing effects.");

                        QualityPreset currentPreset = CurrentSettings.HardwareQualityPreset;
                        string comboLabel = currentPreset.ToString();

                        hardwareQualityGrid.Combobox("HardwareQualityPreset##HardwareQualityCombobox", comboLabel, () =>
                        {
                            foreach (QualityPreset preset in Enum.GetValues(typeof(QualityPreset)))
                            {
                                bool isSelected = preset == currentPreset;
                                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {preset}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveHardwareQualityPreset(preset);
                                }
                            }
                        });
                    }

                    using (FuGrid mapQualityGrid = new FuGrid("mapQualityGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        if (isLoading)
                        {
                            mapQualityGrid.DisableNextElements();
                        }

                        mapQualityGrid.SetNextElementToolTipWithLabel("This parameter allows you to set the resolution of the topography.\nChanging this parameter affects the amount of RAM/VRAM that will be used to display the topography.\nThe higher the quality setting, the longer it will take to load the scene.\nThis setting does not affect the quality/quantity of images needed to load the scene, only the 3D mesh.\nThe change will only be applied the next time a flight is loaded.");

                        QualityPreset currentPreset = CurrentSettings.MapQualityPreset;
                        string comboLabel = currentPreset.ToString();

                        mapQualityGrid.Combobox("MapQualityPreset##MapQualityCombobox", comboLabel, () =>
                        {
                            foreach (QualityPreset preset in Enum.GetValues(typeof(QualityPreset)))
                            {
                                bool isSelected = preset == currentPreset;
                                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {preset}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveMapQualityPreset(preset);
                                }
                            }
                        });
                    }

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
                        apiGrid.TextURL("Follow this link to create a MapTiler API Account", "https://www.maptiler.com/", FuTextWrapping.Clip);
                    }

                    using (FuGrid satelliteTileQualityPresetGrid = new FuGrid("satelliteTileQualityGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        if (isLoading)
                        {
                            satelliteTileQualityPresetGrid.DisableNextElements();
                        }

                        satelliteTileQualityPresetGrid.SetNextElementToolTipWithLabel("This parameter determines the zoom level of satellite images.\nThe higher the zoom level, the more tiles need to be downloaded to reproduce the scene, but the more accurate the final image will be.\nThis has a significant impact on your MapTiler account usage.\nTo cover the same area:\n- Very low: 1 tile\n- Low: 4 tiles\n- Normal: 16 tiles\n- High: 64 tiles\n- Very high: 256 tiles\n- Extreme: 1024 tiles");

                        SatelliteTileQualityPreset satelliteQualityPreset = CurrentSettings.SatelliteTileQualityPreset;
                        string comboLabel = FormatSatellitePresetLabel(satelliteQualityPreset);

                        satelliteTileQualityPresetGrid.Combobox("SatelliteTileQualityPreset##SatelliteTileQualityCombobox", comboLabel, () =>
                        {
                            foreach (SatelliteTileQualityPreset preset in Enum.GetValues(typeof(SatelliteTileQualityPreset)))
                            {
                                bool isSelected = preset == satelliteQualityPreset;
                                string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {FormatSatellitePresetLabel(preset)}";

                                if (ImGui.Selectable(label))
                                {
                                    SaveSatelliteTileQualityPreset(preset);
                                }
                            }
                        });
                    }

                    using (FuGrid tilePaddingGrid = new FuGrid("tilePaddingGrid", new FuGridDefinition(2, new int[] { 150, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        if (isLoading)
                        {
                            tilePaddingGrid.DisableNextElements();
                        }

                        int tilePadding = CurrentSettings.TilePadding;
                        tilePaddingGrid.SetNextElementToolTipWithLabel("Defines the number of additional tile rows around the flight area.\nIncreases the realism of the scene but affects performance and the amount of resources downloaded.");
                        if (tilePaddingGrid.Slider("Tile padding", ref tilePadding, 0, 2))
                        {
                            SaveTilePadding(tilePadding);
                        }
                        tilePaddingGrid.NextColumn();
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

                layout.Collapsable("Clear cache and settings##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Bold);

                    using (FuGrid uiGrid = new FuGrid("actionSettingsGrid", new FuGridDefinition(3, new float[] { 0.3f, 0.3f, 0.3f }), FuGridFlag.Default))
                    {
                        if (isLoading)
                        {
                            uiGrid.DisableNextElements();
                        }

                        uiGrid.SetNextElementToolTipWithLabel("Delete all downloaded tiles stored on this computer.\nVideo files will not be deleted.");

                        if (uiGrid.Button("Clear local cache", FuButtonStyle.Danger))
                        {
                            CacheManager.ClearCache();
                        }

                        uiGrid.SetNextElementToolTipWithLabel("Restore the entire application configuration (including settings made from the application's global UI).\nVideo files will not be deleted.");
                        if (uiGrid.Button("Restore preferences", FuButtonStyle.Danger))
                        {
                            LoadDefaultSettings();
                            LoadAll();

                            Fugui.Notify("Successful operation", "All user preferences have been reset.", StateType.Info);
                        }
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

            }, FuModalSize.Medium, new FuModalButton("Close preferences", () => { _settingsOpened = false; }, FuButtonStyle.Default, FuKeysCode.Enter));
        }
        #endregion
    }
}
