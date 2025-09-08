namespace FlightReLive.Core.FlightDefinition
{
    public class FlightDataPointCameraSettings
    {
        #region PROPERTIES
        public float Aperture { get; set; }

        public float ShutterSpeed { get; set; }

        public int ISO { get; set; }

        public float Exposure { get; set; }

        public float DigitalZoom { get; set; }

        public float FocalLength { get; internal set; }

        public string ColorMode { get; internal set; }
        #endregion
    }
}
