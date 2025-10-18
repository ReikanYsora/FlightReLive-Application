using FlightReLive.Core.Database;
using MessagePack;
using System;
using System.Collections.Generic;

namespace FlightReLive.Core.FFmpeg
{
    public class FlightDataContainer
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public string VideoPath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public double Frequency { get; set; }

        public DateTime CreationDate { get; set; }

        public RealmDoubleVector2 EstimateTakeOffPosition { get; set; }

        public List<RealmFlightPointItem> DataPoints { get; set; }

        public RealmDoubleVector2 FlightGPSCoordinates { get; set; }

        public byte[] Thumbnail { get; set; }

        public TimeSpan Duration { get; set; }

        public bool TakeOffPositionAvailable { get; set; }

        #region CONSTRUCTOR
        public FlightDataContainer()
        {
            DataPoints = new List<RealmFlightPointItem>();
        }
        #endregion

        #region METHODS

        public RealmDoubleVector2 GetFlightGPSCenter()
        {
            if (DataPoints == null || DataPoints.Count == 0)
            {
                return new RealmDoubleVector2(0f, 0f);
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

            return new RealmDoubleVector2(centerLat, centerLon);
        }
        #endregion
    }
}
