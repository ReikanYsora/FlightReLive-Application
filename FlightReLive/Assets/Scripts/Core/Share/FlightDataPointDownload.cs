using System;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightDataPointDownload
    {
        public DateTime TimeUtc { get; set; }

        public long TimeSpanTicks { get; set; }

        public float? Aperture { get; set; }

        public float? ShutterSpeed { get; set; }

        public int? ISO { get; set; }

        public float? Exposure { get; set; }

        public float? DigitalZoom { get; set; }

        public float? FocalLength { get; set; }

        public string ColorMode { get; set; }

        public float Longitude { get; set; }

        public float Latitude { get; set; }

        public float Distance { get; set; }

        public float RelativeAltitude { get; set; }

        public float AbsoluteAltitude { get; set; }

        public float HorizontalSpeed { get; set; }

        public float VerticalSpeed { get; set; }
    }
}
