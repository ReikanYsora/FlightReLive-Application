using MessagePack;
using System;

namespace FlightReLive.Core.FlightDefinition
{
    [MessagePackObject]
    public class FlightDataPoint
    {
        [Key(0)]
        public DateTime Time { get; set; }

        [Key(1)]
        public TimeSpan TimeSpan { get; set; }

        [Key(2)]
        public FlightDataPointCameraSettings CameraSettings { get; set; }

        [Key(3)]
        public double Longitude { get; set; }

        [Key(4)]
        public double Latitude { get; set; }

        [Key(5)]
        public double Satellites { get; set; }

        [Key(6)]
        public double Distance { get; set; }

        [Key(7)]
        public double RelativeAltitude { get; set; }

        [Key(8)]
        public double AbsoluteAltitude { get; set; }

        [Key(9)]
        public double HorizontalSpeed { get; set; }

        [Key(10)]
        public double VerticalSpeed { get; set; }

        [Key(11)]
        public int FrameCounter { get; set; }
    }
}
