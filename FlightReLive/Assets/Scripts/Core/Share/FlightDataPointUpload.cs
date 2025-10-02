using System;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightDataPointUpload
    {
        public DateTime Time { get; set; }

        public long TimeSpanTicks { get; set; }

        public float? Aperture { get; set; }

        public float? ShutterSpeed { get; set; }

        public int? ISO { get; set; }

        public float? Exposure { get; set; }

        public float? DigitalZoom { get; set; }

        public float? FocalLength { get; set; }

        public string ColorMode { get; set; }

        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public double Distance { get; set; }

        public double RelativeAltitude { get; set; }

        public double AbsoluteAltitude { get; set; }

        public double HorizontalSpeed { get; set; }

        public double VerticalSpeed { get; set; }
    }
}
