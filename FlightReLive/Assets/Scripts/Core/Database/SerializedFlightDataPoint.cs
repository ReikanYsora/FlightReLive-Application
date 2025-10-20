
using MessagePack;
using System;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Embedded object representing a single recorded telemetry point.
    /// </summary>
    /// <summary>
    /// Serializable telemetry point (MessagePack).
    /// </summary>
    [MessagePackObject]
    public class SerializedFlightDataPoint
    {
        #region PROPERTIES
        [Key(0)] public DateTime Time { get; set; }

        [Key(1)] public TimeSpan TimeSpan { get; set; }

        [Key(2)] public SerializedGPSCoordinate Coordinate { get; set; }

        [Key(3)] public float Distance { get; set; }

        [Key(4)] public float RelativeAltitude { get; set; }

        [Key(5)] public float AbsoluteAltitude { get; set; }

        [Key(6)] public float HorizontalSpeed { get; set; }

        [Key(7)] public float VerticalSpeed { get; set; }

        [Key(8)] public float Aperture { get; set; }

        [Key(9)] public float ShutterSpeed { get; set; }

        [Key(10)] public int ISO { get; set; }

        [Key(11)] public float Exposure { get; set; }

        [Key(12)] public float DigitalZoom { get; set; }

        [Key(13)] public float FocalLength { get; set; }

        [Key(14)] public string ColorMode { get; set; }
        #endregion
    }
}
