using Realms;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Realm-persistent version of FlightFile.
    /// All complex types are flattened or serialized to primitives for Realm compatibility.
    /// </summary>
    public class RealmFlightItem : RealmObject
    {
        #region PROPERTIES
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }

        public string VideoPath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public double Frequency { get; set; }

        public double DurationSeconds { get; set; }

        [Ignored]
        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromSeconds(DurationSeconds);
            }
            set
            {
                DurationSeconds = value.TotalSeconds;
            }
        }

        public DateTimeOffset CreationDateOffset { get; set; }

        [Ignored]
        public DateTime CreationDate
        {
            get
            {
                return CreationDateOffset.UtcDateTime;
            }
            set
            {
                DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
                CreationDateOffset = new DateTimeOffset(utc, TimeSpan.Zero);
            }
        }

        public byte[] ThumbnailData { get; set; }

        [Ignored] public Texture2D Thumbnail { get; set; }

        public RealmDoubleVector2 EstimateTakeOffPosition { get; set; }

        public double TakeOffAltitude { get; set; }

        public RealmDoubleVector2 FlightGPSCoordinates { get; set; }

        public bool HasTakeOffPosition { get; set; }

        public IList<RealmFlightPointItem> DataPoints { get; }

        [Ignored]
        public bool HasThumbnail
        {
            get
            {
                return ThumbnailData != null && ThumbnailData.Length > 0;
            }
        }
        #endregion

        public void DecodeTextures()
        {
            if (HasThumbnail)
            {
                Thumbnail = new Texture2D(2, 2);
                Thumbnail.LoadImage(ThumbnailData);
            }
        }

        #region METHODS
        public void EncodeTextures()
        {
            if (Thumbnail != null)
            {
                ThumbnailData = Thumbnail.EncodeToPNG();
            }
        }
        #endregion
    }
}
