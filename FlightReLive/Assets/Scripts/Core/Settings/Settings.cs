using FlightReLive.Core.Terrain;
using MessagePack.Resolvers;
using System;
using UnityEngine;

namespace FlightReLive.Core.Settings
{
    public class Settings
    {
        #region ATTRIBUTES
        public QualityPreset HardwareQualityPreset;
        public int ApplicationTargetFPS;
        public int ApplicationIdleFPS;
        public bool DontAskWelcomeVersion;
        public QualityPreset MapQualityPreset;
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
        public SatelliteTileQualityPreset SatelliteTileQualityPreset;
        public float GlobalScale;
        public PointCloudMode PointCloudMode;
        public float AbsoluteAltitudeMin;
        public float AbsoluteAltitudeMax;
        public float HeightPointSize;
        public float HeightOpacity;
        public Color CameraBackgroundColor;
        public Color CameraCaptureBackgroundColor;
        public float PathWidth;
        public Color PathRemainingColor1;
        public Color PathRemainingColor2;
        public bool BuildingVisibility;
        public bool OutlineVisibility;
        public float WorldIconScale;
        public float WorldIconHeight;
        public bool Icon3DVisibility;
        public int CaptureResolution;
        public int CaptureEncoder;
        public int CaptureFramerate;
        public bool CaptureEncodedLogo;
        public string CaptureOutputPath;
        public bool CaptureReplaceBackground;
        public float VignettingIntensity;
        public bool DepthOfFieldEnabled;
        public float DepthOfFieldStart;
        public float DepthOfFieldEnd;
        public string CurrentVersion;
        #endregion
    }
}
