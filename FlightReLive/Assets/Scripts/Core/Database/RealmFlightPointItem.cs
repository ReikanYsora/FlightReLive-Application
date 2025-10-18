
using Realms;
using System;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Embedded object representing a single recorded telemetry point.
    /// </summary>
    public class RealmFlightPointItem : EmbeddedObject
    {
        #region PROPERTIES
        public DateTimeOffset TimeOffset { get; set; }

        [Ignored]
        public DateTime Time
        {
            get
            {
                return TimeOffset.UtcDateTime;
            }
            set
            {
                DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
                TimeOffset = new DateTimeOffset(utc, TimeSpan.Zero);
            }
        }

        public double TimeSeconds { get; set; }

        [Ignored]
        public TimeSpan TimeSpan
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSeconds);
            }
            set
            {
                TimeSeconds = value.TotalSeconds;
            }
        }

        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public double Distance { get; set; }

        public double RelativeAltitude { get; set; }

        public double AbsoluteAltitude { get; set; }

        public double HorizontalSpeed { get; set; }

        public double VerticalSpeed { get; set; }

        public float Aperture { get; set; }

        public float ShutterSpeed { get; set; }

        public int ISO { get; set; }

        public float Exposure { get; set; }

        public float DigitalZoom { get; set; }

        public float FocalLength { get; set; }

        public string ColorMode { get; set; }
        #endregion
    }
}
