using System;
using TND.Upscaling.Framework;
using UnityEngine;

namespace FlightReLive.Core.Settings
{
    public class Settings
    {
        #region ATTRIBUTES
        public UpscalerName UpscalerName;
        public UpscalerQuality UpscalerQuality;
        public bool UpscalerSharpeningEnabled;
        public float UpscalerSharpeness;
        public int ApplicationTargetFPS;
        public int ApplicationIdleFPS;
        public bool DontAskWelcomeVersion;
        public float CameraRotationSpeed;
        public float CameraZoomSpeed;
        public float CameraInertia;
        public TimeZoneInfo UserTimeZone;
        public DateFormatStyle DateFormatStyle;
        public TimeFormatStyle TimeFormatStyle;
        public UnitSystemType UnitSystemType;
        public string WorkspacePath;
        public float WorkspaceZoom;
        public string MapTilerAPIKey;
        public float GlobalScale;
        public Color CameraBackgroundColor;
        public float PathWidth;
        public Color PathRemainingColor1;
        public Color PathRemainingColor2;
        public bool BuildingVisibility;
        public float POIScale;
        public float POIHeight;
        public bool POIVisibility;
        public float VignettingIntensity;
        public string CurrentVersion;
        public float SunIntensity;
        public float PostExposureIntensity;
        public float ContrastIntensity;
        public float SaturationIntensity;
        #endregion
    }
}
