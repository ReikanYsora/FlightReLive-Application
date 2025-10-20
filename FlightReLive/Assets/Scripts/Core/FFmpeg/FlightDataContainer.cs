using FlightReLive.Core.Database;
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

        public float Frequency { get; set; }

        public DateTime CreationDate { get; set; }

        public SerializedGPSCoordinate EstimateTakeOffPosition { get; set; }

        public List<SerializedFlightDataPoint> DataPoints { get; set; }

        public SerializedGPSCoordinate FlightGPSCoordinates { get; set; }

        public byte[] Thumbnail { get; set; }

        public TimeSpan Duration { get; set; }

        public bool TakeOffPositionAvailable { get; set; }

        #region CONSTRUCTOR
        public FlightDataContainer()
        {
            DataPoints = new List<SerializedFlightDataPoint>();
        }
        #endregion

        #region METHODS

        public SerializedGPSCoordinate GetFlightGPSCenter()
        {
            if (DataPoints == null || DataPoints.Count == 0)
            {
                return new SerializedGPSCoordinate(0f, 0f);
            }

            float minLat = float.MaxValue;
            float maxLat = float.MinValue;
            float minLon = float.MaxValue;
            float maxLon = float.MinValue;

            foreach (SerializedFlightDataPoint point in DataPoints)
            {
                if (point.Coordinate.Latitude < minLat)
                {
                    minLat = point.Coordinate.Latitude;
                }

                if (point.Coordinate.Latitude > maxLat)
                {
                    maxLat = point.Coordinate.Latitude;
                }

                if (point.Coordinate.Longitude < minLon)
                {
                    minLon = point.Coordinate.Longitude;
                }

                if (point.Coordinate.Longitude > maxLon)
                {
                    maxLon = point.Coordinate.Longitude;
                }
            }

            float centerLat = (minLat + maxLat) / 2f;
            float centerLon = (minLon + maxLon) / 2f;

            return new SerializedGPSCoordinate(centerLat, centerLon);
        }
        #endregion
    }
}
