using System;
using UnityEngine;

namespace FlightReLive.Core.Settings
{
    public class Settings
    {
        #region ATTRIBUTES
        public bool DisplayWizard;
        public int ApplicationTargetFPS;
        public int ApplicationIdleFPS;
        public bool DontAskWelcomeVersion;
        public float CameraRotationSpeed;
        public float CameraZoomSpeed;
        public float PanSpeed;
        public TimeZoneInfo UserTimeZone;
        public DateFormatStyle DateFormatStyle;
        public TimeFormatStyle TimeFormatStyle;
        public UnitSystemType UnitSystemType;
        public LibraryLayoutDisposition LibraryLayoutDisposition;
        public string MapTilerAPIKey;
        public float GlobalScale;
        public float Path3DThickness;
        public Color Path3DRemainingColor;
        public bool BuildingVisibility;
        public Color BuildingColor;
        public float BuildingAO;
        public float POIScale;
        public float POIHeight;
        public bool POIVisibility;
        public float POIMinFadeDistance;
        public float POIMaxFadeDistance;
        public float VignettingIntensity;
        public float ContrastOffset;
        public float SaturationOffset;
        public float ExposureOffset;
        public CloudsPreset CloudsPreset;
        public bool CloudShadowsEnabled;
        public float CloudShadowsOpacity;
        public WindType WindType;
        public string CurrentVersion;
        public int CaptureResolution;
        public int CaptureEncoder;
        public int CaptureFramerate;
        public bool CaptureEncodedLogo;
        public string CaptureOutputPath;
        #endregion
    }
}
