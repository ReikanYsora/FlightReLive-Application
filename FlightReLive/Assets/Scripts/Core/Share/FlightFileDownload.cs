using System;
using System.Collections.Generic;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightFileDownload
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public float Frequency { get; set; }

        public long DurationTicks { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public byte[] ThumbnailData { get; set; }

        public float? TakeOffLatitude { get; set; }

        public float? TakeOffLongitude { get; set; }

        public float TakeOffAltitude { get; set; }

        public float? FlightLatitude { get; set; }

        public float? FlightLongitude { get; set; }

        public bool HasTakeOffPosition { get; set; }

        public string ShareHash { get; set; }

        public List<FlightDataPointDownload> DataPoints { get; set; } = new List<FlightDataPointDownload>();
    }
}
