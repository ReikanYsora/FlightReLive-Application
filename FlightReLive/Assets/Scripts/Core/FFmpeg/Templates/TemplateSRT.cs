using FlightReLive.Core.FlightDefinition;
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
        /// Check if a flight has correct GPS data values
        /// </summary>
        /// <returns>TRUE or FALSE either</returns>
        protected bool IsFlightDataValid()
        {
            if (DataContainer == null ||
                DataContainer.DataPoints == null ||
                DataContainer.DataPoints.Count == 0 ||
                DataContainer.DataPoints.Where(x => x.Latitude == 0 || x.Longitude == 0).Any())
            {
                UnityEngine.Debug.LogWarning($"{DataContainer.Name} : Flight data invalid: Missing or zero GPS coordinates.");
                return false;
            }

            SerializableVector2 gps = DataContainer.GetFlightGPSCenter();

            if (gps == null || (gps.x == 0.0f && gps.y == 0.0f))
            {
                UnityEngine.Debug.LogWarning($"{DataContainer.Name} : Flight data invalid: Center GPS coordinates calculation failed.");
                return false;
            }

            bool hasValidPoint = DataContainer.DataPoints.Any(dp =>
                (dp.Latitude != 0.0 || dp.Longitude != 0.0) &&
                dp.Latitude >= -90 && dp.Latitude <= 90 &&
                dp.Longitude >= -180 && dp.Longitude <= 180
            );

            return hasValidPoint;
        }

        /// <summary>
        /// Indicate if data for take off triangulation are present in the caonteinr
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        protected bool TakeOffPositionAvailable()
        {
            if (DataContainer.FlightGPSCoordinates == null ||
                DataContainer.FlightGPSCoordinates.x == 0 ||
                DataContainer.FlightGPSCoordinates.y == 0 ||
                DataContainer.EstimateTakeOffPosition.Latitude == 0 ||
                DataContainer.EstimateTakeOffPosition.Longitude == 0 ||
                DataContainer.EstimateTakeOffPosition == null)
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

        protected FlightGPSData EstimateFlightStartFromGPS()
        {
            List<FlightDataPoint> points = DataContainer.DataPoints.Where(p => p.Latitude != 0 && p.Longitude != 0 && p.Distance > 0).ToList();

            if (DataContainer.DataPoints.Count > 0 && points.Count < 3)
            {
                return new FlightGPSData(DataContainer.DataPoints[0].Latitude, DataContainer.DataPoints[0].Longitude);
            }

            double originLat = points[0].Latitude;
            double originLon = points[0].Longitude;

            List<FlightGPSData> gpsPoints = new List<FlightGPSData>();
            List<double> distances = new List<double>();

            foreach (FlightDataPoint p in points)
            {
                gpsPoints.Add(new FlightGPSData(p.Latitude, p.Longitude));
                distances.Add(p.Distance);
            }

            FlightGPSData estimatedGPS = EstimateGPSAdaptive(gpsPoints, distances);

            return estimatedGPS;
        }

        private FlightGPSData EstimateGPSAdaptive(List<FlightGPSData> gpsPoints, List<double> distances)
        {
            double latCenter = gpsPoints[0].Latitude;
            double lonCenter = gpsPoints[0].Longitude;

            double step = 0.0001; // ~11 m
            int range = 50;       // ±0.005 = ±550 m
            int zoomLevels = 4;

            FlightGPSData bestPoint = null;
            double bestError = double.MaxValue;

            for (int zoom = 0; zoom < zoomLevels; zoom++)
            {
                for (int i = -range; i <= range; i++)
                {
                    for (int j = -range; j <= range; j++)
                    {
                        double lat = latCenter + i * step;
                        double lon = lonCenter + j * step;

                        double totalError = 0;
                        for (int k = 0; k < gpsPoints.Count; k++)
                        {
                            double d = Haversine(lat, lon, gpsPoints[k].Latitude, gpsPoints[k].Longitude);
                            totalError += Math.Abs(d - distances[k]);
                        }

                        if (totalError < bestError)
                        {
                            bestError = totalError;
                            bestPoint = new FlightGPSData(lat, lon);
                        }
                    }
                }

                // Zoom in
                latCenter = bestPoint.Latitude;
                lonCenter = bestPoint.Longitude;
                step /= 2;
                range = 20; // Réduit pour accélérer
            }

            return bestPoint;
        }

        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371000; // Rayon terrestre en mètres
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double DegreesToRadians(double deg)
        {
            return deg * Math.PI / 180;
        }
        #endregion
    }
}
