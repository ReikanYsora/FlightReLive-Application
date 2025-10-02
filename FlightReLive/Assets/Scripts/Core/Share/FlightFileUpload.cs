using System;
using System.Collections.Generic;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightFileUpload
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public double Frequency { get; set; }

        public long DurationTicks { get; set; }

        public DateTime CreationDate { get; set; }

        public byte[] MapData { get; set; }

        public byte[] ThumbnailData { get; set; }

        public double? TakeOffLatitude { get; set; }

        public double? TakeOffLongitude { get; set; }

        public float? FlightGPSX { get; set; }

        public float? FlightGPSY { get; set; }

        public bool HasExtractionError { get; set; }

        public bool HasTakeOffPosition { get; set; }

        public bool IsValid { get; set; }

        public string ErrorMessagesJson { get; set; }

        public List<FlightDataPointUpload> DataPoints { get; set; } = new List<FlightDataPointUpload>();
    }

}
