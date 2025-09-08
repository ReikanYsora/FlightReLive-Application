using FlightReLive.Core.FlightDefinition;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.FFmpeg
{
    [MessagePackObject]
    public class FlightDataContainer
    {
        [Key(0)]
        public Guid ID { get; set; }

        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public string VideoPath { get; set; }

        [Key(3)]
        public DateTime CreationDate { get; set; }

        [Key(4)]
        public FlightGPSData EstimateTakeOffPosition { get; set; }

        [Key(5)]
        public List<FlightDataPoint> DataPoints { get; set; }

        [Key(6)]
        public SerializableVector2 FlightGPSCoordinates { get; set; }

        [Key(7)]
        public byte[] Thumbnail { get; set; }

        [Key(8)]
        public TimeSpan Duration { get; set; }

        [Key(9)]
        public bool HasExtractionError { get; set; }

        [Key(10)]
        public bool IsValid { get; set; }

        [Key(11)]
        public List<string> ErrorMessages { get; set; } = new List<string>();

        [Key(12)]
        public bool TakeOffPositionAvailable { get; set; }

        #region CONSTRUCTOR
        public FlightDataContainer()
        {
            DataPoints = new List<FlightDataPoint>();
        }
        #endregion

        #region METHODS

        public SerializableVector2 GetFlightGPSCenter()
        {
            if (DataPoints == null || DataPoints.Count == 0)
            {
                return new SerializableVector2(new Vector2(0f, 0f));
            }

            double minLat = double.MaxValue;
            double maxLat = double.MinValue;
            double minLon = double.MaxValue;
            double maxLon = double.MinValue;

            foreach (var point in DataPoints)
            {
                if (point.Latitude < minLat)
                {
                    minLat = point.Latitude;
                }

                if (point.Latitude > maxLat)
                {
                    maxLat = point.Latitude;
                }

                if (point.Longitude < minLon)
                {
                    minLon = point.Longitude;
                }

                if (point.Longitude > maxLon)
                {
                    maxLon = point.Longitude;
                }
            }

            double centerLat = (minLat + maxLat) / 2.0;
            double centerLon = (minLon + maxLon) / 2.0;

            return new SerializableVector2(new Vector2((float)centerLat, (float)centerLon));
        }
        #endregion
    }
}
