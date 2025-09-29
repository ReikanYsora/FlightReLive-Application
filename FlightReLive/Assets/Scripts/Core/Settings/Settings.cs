using System;
using TND.Upscaling.Framework;
using UnityEngine;

namespace FlightReLive.Core.Settings
{
    public class Settings
    {
        #region ATTRIBUTES
        public bool DisplayWizard;
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
        public int TilePadding;
        public TimeZoneInfo UserTimeZone;
        public DateFormatStyle DateFormatStyle;
        public TimeFormatStyle TimeFormatStyle;
        public UnitSystemType UnitSystemType;
        public string WorkspacePath;
        public float WorkspaceZoom;
        public string MapTilerAPIKey;
        public float GlobalScale;
        public float Path3DThickness;
        public Color Path3DRemainingColor1;
        public Color Path3DRemainingColor2;
        public bool BuildingVisibility;
        public Color BuildingColor;
        public float BuildingAO;
        public bool ContactShadowsEnabled;
        public float ContactShadowsMinDistance;
        public float ContactShadowsMaxDistance;
        public float ContactShadowsOpacity;
        public float VignettingIntensity;
        public float ContrastOffset;
        public float ExposureOffset;
        public float SaturationOffset;
        public float IndirectLightningOffset;
        public string CurrentVersion;
        public CloudStyle CloudStyle;
        #endregion
    }
}
