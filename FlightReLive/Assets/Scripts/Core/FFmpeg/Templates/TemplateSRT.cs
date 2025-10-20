using FlightReLive.Core.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FlightReLive.Core.FFmpeg
{
    public abstract class TemplateSRT
    {
        #region PROPERTIES
        internal FlightDataContainer DataContainer { get; private set; }

        protected string FFmpegPath { get; private set; }

        protected string VideoPath { get; private set; }
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ffmpegPath"></param>
        /// <param name="videoPath"></param>
        protected TemplateSRT(string ffmpegPath, string videoPath)
        {
            FFmpegPath = ffmpegPath;
            VideoPath = videoPath;

            GlobalMetadata metadata = GlobalMetadataExtractor.ExtractMetadata(FFmpegPath, VideoPath);

            DataContainer = new FlightDataContainer
            {
                Name = Path.GetFileNameWithoutExtension(videoPath),
                VideoPath = videoPath
            };

            if (metadata != null)
            {
                DataContainer.Duration = metadata.Duration;
                DataContainer.CreationDate = metadata.CreationDate;
                DataContainer.Thumbnail = metadata.Thumbnail;
            }

            DataContainer = ExtractSubtitles();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Specific method define by template for extract SRT datas
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">Video path</param>
        /// <returns></returns>
        public abstract FlightDataContainer ExtractSubtitles();

        /// <summary>
        /// Extract global metdata from video file
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">Video path</param>
        protected void SetGlobalMetadatas(string ffmpegPath, string videoPath)
        {
            GlobalMetadata metadata = GlobalMetadataExtractor.ExtractMetadata(ffmpegPath, videoPath);

            if (metadata == null)
            {
                throw new Exception("Video metadata exctration failed");
            }

            DataContainer.Duration = metadata.Duration;
            DataContainer.CreationDate = metadata.CreationDate;
            DataContainer.Thumbnail = metadata.Thumbnail;
        }

        /// <summary>
        /// Check if a flight has correct fields
        /// </summary>
        protected void CheckFlightIsValid()
        {
            if (DataContainer == null || DataContainer.DataPoints == null || DataContainer.DataPoints.Count == 0 || DataContainer.DataPoints.Where(x => x.Coordinate.Latitude == 0 || x.Coordinate.Longitude == 0).Any())
            {
                throw new Exception("Missing or zero GPS coordinates.");
            }

            SerializedGPSCoordinate gps = DataContainer.GetFlightGPSCenter();

            if (gps == null || (gps.Latitude == 0.0f && gps.Longitude == 0.0f))
            {
                throw new Exception("Center GPS coordinates calculation failed.");
            }

            bool hasValidPoint = DataContainer.DataPoints.Any(dp =>
                (dp.Coordinate.Latitude != 0.0 || dp.Coordinate.Longitude != 0.0) &&
                dp.Coordinate.Latitude >= -90 && dp.Coordinate.Latitude <= 90 &&
                dp.Coordinate.Longitude >= -180 && dp.Coordinate.Longitude <= 180
            );

            if (!hasValidPoint)
            {
                throw new Exception("No flight points.");
            }
        }

        /// <summary>
        /// Indicate if data for take off triangulation are present in the caonteinr
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        protected bool TakeOffPositionAvailable()
        {
            if (DataContainer.FlightGPSCoordinates == null || DataContainer.FlightGPSCoordinates.Latitude == 0 || DataContainer.FlightGPSCoordinates.Longitude == 0 || DataContainer.EstimateTakeOffPosition.Latitude == 0 || DataContainer.EstimateTakeOffPosition.Longitude == 0 || DataContainer.EstimateTakeOffPosition == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses an SRT timestamp ("hh:mm:ss,ms") into a TimeSpan object.
        /// </summary>
        protected TimeSpan ParseTimecode(string timecode)
        {
            string[] parts = timecode.Split(':', ',', '.');
            int h = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int m = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int s = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int ms = int.Parse(parts[3], CultureInfo.InvariantCulture);

            return new TimeSpan(0, h, m, s, ms);
        }

        /// <summary>
        /// Estimate flight start position from GPS data
        /// </summary>
        /// <returns></returns>
        protected SerializedGPSCoordinate EstimateFlightStartFromGPS()
        {
            List<SerializedFlightDataPoint> points = DataContainer.DataPoints.Where(p => p.Coordinate != null && p.Coordinate.Latitude != 0 && p.Coordinate.Longitude != 0 && p.Distance > 0).ToList();

            if (DataContainer.DataPoints.Count > 0 && points.Count < 3)
            {
                return new SerializedGPSCoordinate(DataContainer.DataPoints[0].Coordinate.Latitude, DataContainer.DataPoints[0].Coordinate.Longitude);
            }

            double originLat = points[0].Coordinate.Latitude;
            double originLon = points[0].Coordinate.Longitude;

            List<SerializedGPSCoordinate> gpsPoints = new List<SerializedGPSCoordinate>();
            List<double> distances = new List<double>();

            foreach (SerializedFlightDataPoint p in points)
            {
                gpsPoints.Add(new SerializedGPSCoordinate(p.Coordinate.Latitude, p.Coordinate.Longitude));
                distances.Add(p.Distance);
            }

            SerializedGPSCoordinate estimatedGPS = EstimateGPSAdaptive(gpsPoints, distances);

            return estimatedGPS;
        }

        /// <summary>
        /// Estimate GPS position using an adaptive grid search algorithm
        /// </summary>
        /// <param name="gpsPoints"></param>
        /// <param name="distances"></param>
        /// <returns></returns>
        private SerializedGPSCoordinate EstimateGPSAdaptive(List<SerializedGPSCoordinate> gpsPoints, List<double> distances)
        {
            float latCenter = gpsPoints[0].Latitude;
            float lonCenter = gpsPoints[0].Longitude;

            float step = 0.0001f;
            int range = 50;
            int zoomLevels = 4;

            SerializedGPSCoordinate bestPoint = null;
            float bestError = float.MaxValue;

            for (int zoom = 0; zoom < zoomLevels; zoom++)
            {
                for (int i = -range; i <= range; i++)
                {
                    for (int j = -range; j <= range; j++)
                    {
                        float lat = latCenter + i * step;
                        float lon = lonCenter + j * step;

                        float totalError = 0;
                        for (int k = 0; k < gpsPoints.Count; k++)
                        {
                            float d = Haversine(new SerializedGPSCoordinate(lat, lon), gpsPoints[k]);
                            totalError += (float)Math.Abs(d - distances[k]);
                        }

                        if (totalError < bestError)
                        {
                            bestError = totalError;
                            bestPoint = new SerializedGPSCoordinate(lat, lon);
                        }
                    }
                }

                //Zoom in
                latCenter = bestPoint.Latitude;
                lonCenter = bestPoint.Longitude;
                step /= 2;
                range = 20;
            }

            return bestPoint;
        }

        /// <summary>
        /// Calculates the Haversine distance between two GPS coordinates in meters.
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns></returns>
        private static float Haversine(SerializedGPSCoordinate coord1, SerializedGPSCoordinate coord2)
        {
            double R = 6371000;
            double dLat = DegreesToRadians(coord2.Latitude - coord1.Latitude);
            double dLon = DegreesToRadians(coord2.Longitude - coord1.Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(coord1.Latitude)) * Math.Cos(DegreesToRadians(coord2.Latitude)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (float)(R * c);
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        private static double DegreesToRadians(double deg)
        {
            return deg * Math.PI / 180;
        }
        #endregion
    }
}
