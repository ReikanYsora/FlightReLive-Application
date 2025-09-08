using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FlightReLive.Core.FFmpeg
{
    /// <summary>
    /// FFmpegRunner handles extraction of SRT subtitle metadata from drone video files
    /// and parses flight data to populate runtime structures for visualization.
    /// </summary>

    public static class FFmpegHelper
    {
        #region METHODS
        public static FlightDataContainer ExtractOrLoadFlightData(string videoPath)
        {
            return ExtractFlightData(videoPath);
        }

        /// <summary>
        /// Get the path to the FFmpeg executable based on the platform.
        /// </summary>
        /// <returns></returns>
        private static string GetFFmpegPath()
        {
#if UNITY_EDITOR_OSX
            // macOs editor : use StreamingAssets/ffmpeg
            return Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg");
#elif UNITY_STANDALONE_OSX
            // macOs build : use StreamingAssets/ffmpeg
            return Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Windows editor or build : use StreamingAssets/ffmpeg
            return Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg.exe");
#else
            // Unsupported platform
            UnityEngine.Debug.LogError("FFmpeg is only supported on Windows and macOS platforms.");
            return string.Enpty;
#endif
        }

        /// <summary>
        /// Extract flight data from SRT encrypted in video file
        /// </summary>
        /// <param name="videoPath">Video path file</param>
        /// <returns></returns>
        public static FlightDataContainer ExtractFlightData(string videoPath)
        {
            return ExtractSubtitles(GetFFmpegPath(), videoPath);
        }

        /// <summary>
        /// Check if a flight has correct GPS data values
        /// </summary>
        /// <param name="container">FlightDataContainer</param>
        /// <returns>TRUE or FALSE either</returns>
        private static bool IsFlightDataValid(FlightDataContainer container)
        {
            if (container == null ||
                container.DataPoints == null ||
                container.DataPoints.Count == 0 ||
                container.DataPoints.Where(x => x.Latitude == 0 || x.Longitude == 0).Any())
            {
                UnityEngine.Debug.LogWarning($"{container.Name} : Flight data invalid: Missing or zero GPS coordinates.");
                return false;
            }

            SerializableVector2 gps = container.GetFlightGPSCenter();

            if (gps == null || (gps.x == 0.0f && gps.y == 0.0f))
            {
                UnityEngine.Debug.LogWarning($"{container.Name} : Flight data invalid: Center GPS coordinates calculation failed.");
                return false;
            }

            bool hasValidPoint = container.DataPoints.Any(dp =>
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
        private static bool TakeOffPositionAvailable(FlightDataContainer container)
        {
            if (container.FlightGPSCoordinates == null ||
                container.FlightGPSCoordinates.x == 0 ||
                container.FlightGPSCoordinates.y == 0 || 
                container.EstimateTakeOffPosition.Latitude == 0 ||
                container.EstimateTakeOffPosition.Longitude == 0 ||
                container.EstimateTakeOffPosition == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a video file has integrated subtitiles
        /// </summary>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">video path</param>
        /// <returns></returns>
        private static bool HasSubtitles(string ffmpegPath, string videoPath)
        {
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                Console.WriteLine("FFmpeg path is not valid.");
                return false;
            }

            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                Console.WriteLine("Video file not found.");
                return false;
            }

            string arguments = $"-i \"{videoPath}\"";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = false
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    var subtitleRegex = new Regex(@"Stream #\d+:\d+.*(?:Subtitle|mov_text|text)", RegexOptions.IgnoreCase);

                    //Check if one flux contains subtitles
                    return subtitleRegex.IsMatch(errorOutput);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking subtitles: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extract date and lengh from the original video file
        /// </summary>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">video path</param>
        /// <returns></returns>
        private static (DateTime? creationDate, TimeSpan? videoDuration) ExtractVideoMetadata(string ffmpegPath, string videoPath)
        {
            // Vérification de la validité des chemins
            if (string.IsNullOrEmpty(ffmpegPath) || !System.IO.File.Exists(ffmpegPath))
            {
                Console.WriteLine("FFmpeg path is not valid.");
                return (null, null);
            }

            if (string.IsNullOrEmpty(videoPath) || !System.IO.File.Exists(videoPath))
            {
                Console.WriteLine("Video file not found.");
                return (null, null);
            }

            // Commande FFmpeg pour obtenir les métadonnées du fichier vidéo
            string arguments = $"-i \"{videoPath}\"";  // On n'a besoin que des métadonnées de base

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true, // Les erreurs contiennent les informations des flux
                RedirectStandardOutput = false
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    //Extract date
                    string creationTimePattern = @"creation_time\s*[:=]\s*(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z)";
                    var creationMatch = Regex.Match(errorOutput, creationTimePattern);
                    DateTime? creationDate = null;

                    if (creationMatch.Success)
                    {
                        string creationTimeStr = creationMatch.Groups[1].Value;

                        if (DateTime.TryParseExact(creationTimeStr, "yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
                        {
                            creationDate = parsedDate.ToLocalTime();
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"Failed to parse creation time: {creationTimeStr}");
                        }
                    }

                    //Extract length
                    string durationPattern = @"Duration:\s*(\d{2}):(\d{2}):(\d{2})\.(\d{2})";
                    var durationMatch = Regex.Match(errorOutput, durationPattern);
                    TimeSpan? videoDuration = null;
                    if (durationMatch.Success)
                    {
                        int hours = int.Parse(durationMatch.Groups[1].Value);
                        int minutes = int.Parse(durationMatch.Groups[2].Value);
                        int seconds = int.Parse(durationMatch.Groups[3].Value);
                        int milliseconds = int.Parse(durationMatch.Groups[4].Value) * 10;

                        videoDuration = new TimeSpan(hours, minutes, seconds, 0, milliseconds);
                    }

                    return (creationDate, videoDuration);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while extracting video metadata: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Runs FFmpeg as a child process to extract subtitles and capture metadata from stderr.
        /// </summary>
        private static FlightDataContainer ExtractSubtitles(string ffmpegPath, string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                UnityEngine.Debug.LogError("Video file not found.");
                return null;
            }

            FlightDataContainer dataContainer = new FlightDataContainer
            {
                Name = Path.GetFileNameWithoutExtension(videoPath),
                VideoPath = videoPath
            };

            string srtPath = Path.ChangeExtension(videoPath, ".srt");

            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                UnityEngine.Debug.LogError("FFmpeg path is not set or executable not found.");
                return null;
            }

            var t = FFmpegMetadataExtractor.ExtractAllStreams(ffmpegPath, videoPath);


            (DateTime? date, TimeSpan? length) metadata = ExtractVideoMetadata(ffmpegPath, videoPath);
            dataContainer.Lenght = metadata.length.HasValue ? metadata.length.Value : new TimeSpan(0);
            dataContainer.Date = metadata.date.HasValue ? metadata.date.Value : DateTime.MinValue;
            dataContainer.ThumbnailImage = ExtractThumbnail(ffmpegPath, videoPath);

            if (HasSubtitles(ffmpegPath, videoPath))
            {
                string arguments = $"-i \"{videoPath}\" -map 0:s:0 -f srt -";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                try
                {
                    using (Process process = Process.Start(psi))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        List<string> srtLines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        dataContainer.DataPoints = ParseSRTVideo(srtLines, dataContainer);
                        dataContainer.EstimateTakeOffPosition = EstimateFlightStartFromGPS(dataContainer);
                        dataContainer.IsValid = IsFlightDataValid(dataContainer);
                        dataContainer.TakeOffPositionAvailable = TakeOffPositionAvailable(dataContainer);
                    }

                    return dataContainer;
                }
                catch (Exception ex)
                {
                    dataContainer.HasExtractionError = true;
                    dataContainer.ErrorMessages.Add(ex.Message);
                }
            }
            else if (File.Exists(srtPath))
            {
                try
                {
                    List<string> srtLines = File.ReadAllLines(srtPath).ToList();
                    if (srtLines.Count > 0)
                    {
                        dataContainer.DataPoints = ParseSRTFile(srtLines, dataContainer);
                        dataContainer.IsValid = IsFlightDataValid(dataContainer);
                        dataContainer.EstimateTakeOffPosition = EstimateFlightStartFromGPS(dataContainer);
                        dataContainer.ThumbnailImage = ExtractThumbnail(ffmpegPath, videoPath);
                    }
                }
                catch (Exception ex)
                {
                    dataContainer.HasExtractionError = true;
                    dataContainer.ErrorMessages.Add($"Error reading SRT file: {ex.Message}");
                }

                return dataContainer;
            }

            //No SRT founded
            dataContainer.HasExtractionError = true;
            dataContainer.ErrorMessages.Add($"No SRT file founded");
            dataContainer.IsValid = false;
            dataContainer.ThumbnailImage = ExtractThumbnail(ffmpegPath, videoPath);

            return dataContainer;
        }

        /// <summary>
        /// Extract thumbnail
        /// </summary>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">video path</param>
        /// <returns>thumbnail as byte array</returns>
        private static byte[] ExtractThumbnail(string ffmpegPath, string videoPath)
        {
            string arguments = $"-y -i \"{videoPath}\" -frames:v 1 -vf \"scale=320:-1\" -f image2pipe -vcodec png -";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (Process process = Process.Start(psi))
                using (MemoryStream ms = new MemoryStream())
                {
                    process.StandardOutput.BaseStream.CopyTo(ms);
                    process.WaitForExit(); // ← important : attendre avant de lire stderr
                    string errorOutput = process.StandardError.ReadToEnd();

                    if (ms.Length == 0)
                    {
                        UnityEngine.Debug.LogWarning("Thumbnail extraction returned empty stream.");
                        return null;
                    }

                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("FFmpeg thumbnail error: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Parses the .srt file to extract individual flight data points.
        /// </summary>
        private static List<FlightDataPoint> ParseSRTVideo(List<string> srtBuffer, FlightDataContainer dataContainer)
        {
            List<FlightDataPoint> dataPoints = new List<FlightDataPoint>();

            Regex cameraRegex = new Regex(@"F\/([0-9.]+), SS ([0-9.]+), ISO (\d+), EV ([\-0-9.]+), DZOOM ([0-9.]+)", RegexOptions.Compiled);
            Regex gpsRegex = new Regex(@"GPS\s+\(([-+]?[0-9]*\.?[0-9]+),\s*([-+]?[0-9]*\.?[0-9]+),\s*([-+]?[0-9]*\.?[0-9]+)\)");
            Regex dRegex = new Regex(@"D\s+([-+]?[0-9]*\.?[0-9]+)m");
            Regex hRegex = new Regex(@"H\s+([-+]?[0-9]*\.?[0-9]+)m");
            Regex hsRegex = new Regex(@"H\.S\s+([-+]?[0-9]*\.?[0-9]+)m/s");
            Regex vsRegex = new Regex(@"V\.S\s+([-+]?[0-9]*\.?[0-9]+)m/s");

            for (int i = 0; i < srtBuffer.Count - 2; i++)
            {
                string indexLine = srtBuffer[i].Trim();
                string timeLine = srtBuffer[i + 1].Trim();
                string dataLine = srtBuffer[i + 2].Trim();

                if (!int.TryParse(indexLine, out _) || !timeLine.Contains("-->") || string.IsNullOrWhiteSpace(dataLine))
                {
                    continue;
                }

                DateTime absoluteTime;
                TimeSpan offset;

                try
                {
                    string startTime = timeLine.Split(new[] { " --> " }, StringSplitOptions.None)[0];
                    offset = ParseTimecode(startTime);
                    absoluteTime = dataContainer.Date.Add(offset);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Invalid timecode at line {i}: {timeLine} ({ex.Message})");
                    continue;
                }

                FlightDataPoint point = new FlightDataPoint { Time = absoluteTime, TimeSpan = offset };

                try
                {
                    Match gps = gpsRegex.Match(dataLine);
                    if (gps.Success && gps.Groups.Count >= 4)
                    {
                        point.Longitude = double.Parse(gps.Groups[1].Value, CultureInfo.InvariantCulture);
                        point.Latitude = double.Parse(gps.Groups[2].Value, CultureInfo.InvariantCulture);
                        point.Satellites = double.Parse(gps.Groups[3].Value, CultureInfo.InvariantCulture);
                    }

                    Match camera = cameraRegex.Match(dataLine);
                    if (camera.Success && camera.Groups.Count >= 6)
                    {
                        point.CameraSettings = new FlightDataPointCameraSettings
                        {
                            Aperture = float.Parse(camera.Groups[1].Value, CultureInfo.InvariantCulture),
                            ShutterSpeed = float.Parse(camera.Groups[2].Value, CultureInfo.InvariantCulture),
                            ISO = int.Parse(camera.Groups[3].Value, CultureInfo.InvariantCulture),
                            Exposure = float.Parse(camera.Groups[4].Value, CultureInfo.InvariantCulture),
                            DigitalZoom = float.Parse(camera.Groups[5].Value, CultureInfo.InvariantCulture)
                        };
                    }

                    Match dMatch = dRegex.Match(dataLine);
                    if (dMatch.Success && dMatch.Groups.Count >= 2)
                    {
                        point.Distance = double.Parse(dMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }

                    Match hMatch = hRegex.Match(dataLine);
                    if (hMatch.Success && hMatch.Groups.Count >= 2)
                    {
                        point.RelativeAltitude = double.Parse(hMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }

                    Match hsMatch = hsRegex.Match(dataLine);
                    if (hsMatch.Success && hsMatch.Groups.Count >= 2)
                    {
                        point.HorizontalSpeed = double.Parse(hsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }

                    Match vsMatch = vsRegex.Match(dataLine);
                    if (vsMatch.Success && vsMatch.Groups.Count >= 2)
                    {
                        point.VerticalSpeed = double.Parse(vsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Data parsing error at line {i}: {ex.Message}");
                }

                dataPoints.Add(point);
            }

            return dataPoints;
        }

        private static List<FlightDataPoint> ParseSRTFile(List<string> srtBuffer, FlightDataContainer dataContainer)
        {
            var dataPoints = new List<FlightDataPoint>();

            for (int i = 0; i < srtBuffer.Count - 4; i++)
            {
                string timestampLine = srtBuffer[i + 3].Trim();
                string metadataLine = srtBuffer[i + 4].Trim();

                if (!DateTime.TryParse(timestampLine, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime absoluteTime))
                {
                    continue;
                }

                FlightDataPoint point = new FlightDataPoint
                {
                    Time = absoluteTime.ToLocalTime(),
                    TimeSpan = absoluteTime.ToLocalTime() - dataContainer.Date,
                    CameraSettings = new FlightDataPointCameraSettings()
                };

                try
                {
                    (double relative, double absolute) altitudes = ExtractAltitudes(metadataLine);
                    point.RelativeAltitude = altitudes.relative;
                    point.AbsoluteAltitude = altitudes.absolute;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Altitude parsing failed: {ex.Message}");
                }

                string[] tokens = metadataLine.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string token in tokens)
                {
                    if (token.Contains("rel_alt:") && token.Contains("abs_alt:"))
                    {
                        continue;
                    }

                    string[] parts = token.Split(new[] { ':' }, 2);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    string key = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();

                    try
                    {
                        switch (key)
                        {
                            case "iso":
                                point.CameraSettings.ISO = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "shutter":
                                point.CameraSettings.ShutterSpeed = ParseShutterSpeed(value);
                                break;
                            case "fnum":
                                point.CameraSettings.Aperture = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "ev":
                                point.CameraSettings.Exposure = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "color_md":
                                point.CameraSettings.ColorMode = value;
                                break;
                            case "focal_len":
                                point.CameraSettings.FocalLength = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "latitude":
                                point.Latitude = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "longitude":
                                point.Longitude = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "ct":
                                point.FrameCounter = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Parsing error for key '{key}': {ex.Message}");
                    }
                }

                if (point.Time != default && point.Latitude != 0 && point.Longitude != 0)
                {
                    dataPoints.Add(point);
                }

                i += 4;
            }

            //Calculate Horizontal speed and Vertical speed for each points
            SpeedCalculator.CalculateSpeeds(dataPoints);

            return dataPoints;
        }

        private static (double relAlt, double absAlt) ExtractAltitudes(string input)
        {
            Regex regex = new Regex(@"\[rel_alt:\s*([\d\.]+)\s+abs_alt:\s*([\d\.]+)\]");
            Match match = regex.Match(input);

            if (match.Success &&
                double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double relAlt) &&
                double.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double absAlt))
            {
                return (relAlt, absAlt);
            }

            return (0, 0);
        }


        private static float ParseShutterSpeed(string shutter)
        {
            if (shutter.Contains("/"))
            {
                string[] parts = shutter.Split('/');

                if (parts.Length == 2 &&
                    float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float num) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float denom))
                {
                    return num / denom;
                }
            }
            return float.Parse(shutter, CultureInfo.InvariantCulture);
        }

        private static FlightGPSData EstimateFlightStartFromGPS(FlightDataContainer container)
        {
            List<FlightDataPoint> points = container.DataPoints.Where(p => p.Latitude != 0 && p.Longitude != 0 && p.Distance > 0).ToList();

            if (container.DataPoints.Count > 0 && points.Count < 3)
            {
                return new FlightGPSData(container.DataPoints[0].Latitude, container.DataPoints[0].Longitude);
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

        public static FlightGPSData EstimateGPSAdaptive(List<FlightGPSData> gpsPoints, List<double> distances)
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

        public static double Haversine(double lat1, double lon1, double lat2, double lon2)
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

        public static double DegreesToRadians(double deg)
        {
            return deg * Math.PI / 180;
        }

        /// <summary>
        /// Parses an SRT timestamp ("hh:mm:ss,ms") into a TimeSpan object.
        /// </summary>
        private static TimeSpan ParseTimecode(string timecode)
        {
            string[] parts = timecode.Split(':', ',', '.');
            int h = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int m = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int s = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int ms = int.Parse(parts[3], CultureInfo.InvariantCulture);
            return new TimeSpan(0, h, m, s, ms);
        }
        #endregion
    }
}
