using System;

namespace FlightReLive.Core.Database
{
    public class FlightDataPoint
    {
        #region PROPERTIES
        public string Id { get; set; }

        public DateTime Time { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public SerializedGPSCoordinate Coordinate { get; set; }

        public float Distance { get; set; }

        public float RelativeAltitude { get; set; }

        public float AbsoluteAltitude { get; set; }

        public float HorizontalSpeed { get; set; }

        public float VerticalSpeed { get; set; }

        public float Aperture { get; set; }

        public float ShutterSpeed { get; set; }

        public int ISO { get; set; }

        public float Exposure { get; set; }

        public float DigitalZoom { get; set; }

        public float FocalLength { get; set; }

        public string ColorMode { get; set; }
        #endregion
    }
}
