using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;

namespace FlightReLive.Core.FFmpeg
{
    internal class SpeedCalculator
    {
        #region CONSTANTS
        private const double EARTH_RADIUS = 6371000;
        #endregion

        private static double CalculateHorizontalDistance(FlightDataPoint point1, FlightDataPoint point2)
        {
            double lat1 = ToRadians(point1.Latitude);
            double lon1 = ToRadians(point1.Longitude);
            double lat2 = ToRadians(point2.Latitude);
            double lon2 = ToRadians(point2.Longitude);
            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;
            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EARTH_RADIUS * c;
        }

        private static double CalculateHorizontalSpeed(FlightDataPoint point1, FlightDataPoint point2)
        {
            double distance = CalculateHorizontalDistance(point1, point2);
            double timeDifference = (point2.Time - point1.Time).TotalSeconds;

            return timeDifference > 0 ? distance / timeDifference : 0;
        }


        private static double CalculateVerticalSpeed(FlightDataPoint point1, FlightDataPoint point2)
        {
            double deltaAltitude = point2.AbsoluteAltitude - point1.AbsoluteAltitude;
            double timeDifference = (point2.Time - point1.Time).TotalSeconds;

            return timeDifference > 0 ? deltaAltitude / timeDifference : 0;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        internal static void CalculateSpeeds(List<FlightDataPoint> points)
        {
            for (int i = 1; i < points.Count; i++)
            {
                double horizontalSpeed = CalculateHorizontalSpeed(points[i - 1], points[i]);
                double verticalSpeed = CalculateVerticalSpeed(points[i - 1], points[i]);

                points[i].HorizontalSpeed = horizontalSpeed;
                points[i].VerticalSpeed = verticalSpeed;
            }
        }
    }
}
