using System;
using System.Collections.Generic;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightFileDownloadResponse
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public float Frequency { get; set; }

        public long DurationTicks { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public byte[] MapData { get; set; }

        public byte[] ThumbnailData { get; set; }

        public float? TakeOffLatitude { get; set; }

        public float? TakeOffLongitude { get; set; }

        public float? FlightGPSX { get; set; }

        public float? FlightGPSY { get; set; }

        public bool HasExtractionError { get; set; }

        public bool HasTakeOffPosition { get; set; }

        public bool IsValid { get; set; }

        public string ErrorMessagesJson { get; set; }

        public List<FlightDataPointDownload> DataPoints { get; set; }

        public string ShareHash { get; set; }

        public string ShareHashDisplay { get; set; }

        public DateTime ExpirationDateUtc { get; set; }
    }
}
