using System;

namespace FlightReLive.Core.FlightDefinition
{
    public class FlightDataPoint
    {
        public DateTime Time { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public FlightDataPointCameraSettings CameraSettings { get; set; }

        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public double Distance { get; set; }

        public double RelativeAltitude { get; set; }

        public double AbsoluteAltitude { get; set; }

        public double HorizontalSpeed { get; set; }

        public double VerticalSpeed { get; set; }
    }
}
