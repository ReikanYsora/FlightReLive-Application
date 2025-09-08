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
        public double GPSAltitude { get; set; }

        [Key(6)]
        public double Distance { get; set; }

        [Key(7)]
        public double Height { get; set; }

        [Key(8)]
        public double HorizontalSpeed { get; set; }

        [Key(9)]
        public double VerticalSpeed { get; set; }
        public int FrameCounter { get; internal set; }
    }
}
