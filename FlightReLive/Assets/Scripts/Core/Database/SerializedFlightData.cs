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

        [Key(1)] public FlightDataOrigin Origin { get; set; }

        [Key(2)] public string Name { get; set; }

        [Key(3)] public int Width { get; set; }

        [Key(4)] public int Height { get; set; }

        [Key(5)] public float Frequency { get; set; }

        [Key(6)] public TimeSpan Duration { get; set; }

        [Key(7)] public DateTime CreationDate { get; set; }

        [Key(8)] public byte[] ThumbnailData { get; set; }

        [IgnoreMember] public Texture2D Thumbnail { get; set; }

        [Key(9)] public SerializedGPSCoordinate EstimateTakeOffPosition { get; set; }

        [Key(10)] public float TakeOffAltitude { get; set; }

        [Key(11)] public SerializedGPSCoordinate FlightGPSCoordinates { get; set; }

        [Key(12)] public bool HasTakeOffPosition { get; set; }

        [Key(13)] public List<SerializedFlightDataPoint> DataPoints { get; set; } = new List<SerializedFlightDataPoint>();

        [Key(14)] public bool IsNew { get; set; } = true;

        [Key(15)] public string ShareHash { get; set; }
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
            string originPart = Origin.ToString().ToLowerInvariant();
            string namePart = Name?.Trim().ToLowerInvariant() ?? "unknown";
            string datePart = CreationDate.ToUniversalTime().ToString("yyyyMMddHHmmss");
            string durationPart = Duration.TotalSeconds.ToString("F2");

            UniqueKey = $"{originPart}_{namePart}_{datePart}_{durationPart}";
        }

        #endregion
    }
}