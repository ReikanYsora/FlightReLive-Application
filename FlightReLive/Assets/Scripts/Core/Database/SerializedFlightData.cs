using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Serializable version of FlightData
    /// </summary>
    [MessagePackObject]
    public class SerializedFlightData
    {
        #region PROPERTIES
        [Key(0)] public string UniqueKey { get; set; }

        [Key(1)] public string Name { get; set; }

        [Key(2)] public int Width { get; set; }

        [Key(3)] public int Height { get; set; }

        [Key(4)] public float Frequency { get; set; }

        [Key(5)] public TimeSpan Duration { get; set; }

        [Key(6)] public DateTime CreationDate { get; set; }

        [Key(7)] public byte[] ThumbnailData { get; set; }

        [IgnoreMember] public Texture2D Thumbnail { get; set; }

        [Key(8)] public SerializedGPSCoordinate EstimateTakeOffPosition { get; set; }

        [Key(9)] public float TakeOffAltitude { get; set; }

        [Key(10)] public SerializedGPSCoordinate FlightGPSCoordinates { get; set; }

        [Key(11)] public bool HasTakeOffPosition { get; set; }

        [Key(12)] public List<SerializedFlightDataPoint> DataPoints { get; set; } = new List<SerializedFlightDataPoint>();

        [Key(13)] public bool IsNew { get; set; } = true;
        #endregion

        #region METHODS
        /// <summary>
        /// Decode thumbnail data into a Texture2D (PNG).
        /// </summary>
        public void DecodeTextures()
        {
            if (ThumbnailData == null || ThumbnailData.Length == 0)
            {
                return;
            }

            Thumbnail = new Texture2D(2, 2);
            Thumbnail.LoadImage(ThumbnailData);
        }

        /// <summary>
        /// Encode current Texture2D into PNG binary data.
        /// </summary>
        public void EncodeTextures()
        {
            if (Thumbnail != null)
            {
                ThumbnailData = Thumbnail.EncodeToPNG();
            }
        }

        /// <summary>
        /// Generates a deterministic key used to detect duplicate flights.
        /// </summary>
        public void ComputeUniqueKey()
        {
            string namePart = Name?.Trim().ToLowerInvariant() ?? "unknown";
            string datePart = CreationDate.ToUniversalTime().ToString("yyyyMMddHHmmss");
            string durationPart = Duration.TotalSeconds.ToString("F2");
            UniqueKey = $"{namePart}_{datePart}_{durationPart}";
        }
        #endregion
    }
}