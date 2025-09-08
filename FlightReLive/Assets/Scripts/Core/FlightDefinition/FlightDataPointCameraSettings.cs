using MessagePack;

namespace FlightReLive.Core.FlightDefinition
{
    [MessagePackObject]
    public class FlightDataPointCameraSettings
    {
        [Key(0)]
        public float Aperture { get; set; }

        [Key(1)]
        public float ShutterSpeed { get; set; }

        [Key(2)]
        public int ISO { get; set; }

        [Key(3)]
        public float Exposure { get; set; }

        [Key(4)]
        public float DigitalZoom { get; set; }

        [Key(5)]
        public float FocalLength { get; internal set; }

        [Key(6)]
        public string ColorMode { get; internal set; }
    }
}
