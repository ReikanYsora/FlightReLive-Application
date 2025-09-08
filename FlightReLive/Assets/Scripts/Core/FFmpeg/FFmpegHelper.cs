using FlightReLive.Core.Cache;
using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.FFmpeg
{
    /// <summary>
    /// FFmpegRunner handles extraction of SRT subtitle metadata from drone video files
    /// and parses flight data to populate runtime structures for visualization.
    /// </summary>

    public static class FFmpegHelper
    {
        #region ATTRIBUTES
        /// <summary>
        /// Regex to detect the creation timestamp from ffmpeg metadata.
        /// </summary>
        private static Regex creationTimeRegex = new Regex(@"creation_time\s*:\s*(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})", RegexOptions.Compiled);

        /// <summary>
        /// Regex to detect GPS coordinates from ffmpeg metadata.
        /// </summary>
        private static Regex gpsLocationRegex = new Regex(@"location\s*:\s*\+?([0-9]*\.?[0-9]+)\+?([0-9]*\.?[0-9]+)", RegexOptions.Compiled);

        /// <summary>
        /// Regex to detect video length
        /// </summary>
        private static Regex durationRegex = new Regex(@"Duration:\s(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2})\.(?<ms>\d{2})", RegexOptions.Compiled);
        #endregion

        #region METHODS
        public static async Task<FlightDataContainer> ExtractOrLoadFlightDataAsync(string videoPath)
        {
            if (await CacheManager.VideoBinaryDataExistsAsync(videoPath))
            {
                return await CacheManager.LoadVideoBinaryDataAsync(videoPath);
            }

            FlightDataContainer extracted = ExtractFlightData(videoPath);

            await CacheManager.SaveVideoBinaryDataAsync(videoPath, extracted);

            return extracted;
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
                container.DataPoints.Where(x => x.Latitude == 0 || x.Longitude == 0).Any() ||
                container.FlightGPSCoordinates == null ||
                container.FlightGPSCoordinates.x == 0 ||
                container.FlightGPSCoordinates.y == 0 ||
                container.EstimateTakeOffPosition.Latitude == 0 ||
                container.EstimateTakeOffPosition.Longitude == 0 ||
                container.EstimateTakeOffPosition == null)
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

            //if (File.Exists(srtPath))
            //{
            //    try
            //    {
            //        List<string> srtLines = File.ReadAllLines(srtPath).ToList();
            //        if (srtLines.Count > 0)
            //        {
            //            dataContainer.DataPoints = ParseSRTMini4Pro(srtLines, dataContainer);
            //            dataContainer.IsValid = IsFlightDataValid(dataContainer);
            //            dataContainer.EstimateTakeOffPosition = EstimateFlightStartFromGPS(dataContainer);
            //            dataContainer.ThumbnailImage = ExtractThumbnail(ffmpegPath, videoPath);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        dataContainer.HasExtractionError = true;
            //        dataContainer.ErrorMessages.Add($"Error reading SRT file: {ex.Message}");
            //    }

            //    return dataContainer;
            //}

            //if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            //{
            //    UnityEngine.Debug.LogError("FFmpeg path is not set or executable not found.");
            //    return null;
            //}

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
                    List<string> errorBuffer = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (errorBuffer.Any(IsCriticalFFmpegError))
                    {
                        dataContainer.HasExtractionError = true;
                        dataContainer.ErrorMessages = errorBuffer.Where(IsCriticalFFmpegError).ToList();
                        UnityEngine.Debug.LogWarning("FFmpeg reported critical errors. Continuing with partial data.");
                        return dataContainer;
                    }

                    if (srtLines.Count > 0)
                    {
                        // ✅ Enregistrement du fichier SRT dans le même dossier que la vidéo
                        File.WriteAllText(srtPath, output);
                        UnityEngine.Debug.Log($"SRT file extracted and saved to: {srtPath}");

                        dataContainer.DataPoints = ParseSRT(srtLines, dataContainer);
                    }


                    foreach (string line in errorBuffer)
                    {
                        Match matchTime = creationTimeRegex.Match(line);
                        if (matchTime.Success && DateTime.TryParse(matchTime.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedTime))
                        {
                            dataContainer.Date = parsedTime.ToLocalTime();
                        }

                        Match matchGPS = gpsLocationRegex.Match(line);
                        if (matchGPS.Success)
                        {
                            float lat = float.Parse(matchGPS.Groups[1].Value, CultureInfo.InvariantCulture);
                            float lon = float.Parse(matchGPS.Groups[2].Value, CultureInfo.InvariantCulture);
                            dataContainer.FlightGPSCoordinates = new SerializableVector2(new Vector2(lat, lon));
                        }

                        Match matchDuration = durationRegex.Match(line);
                        if (matchDuration.Success)
                        {
                            int h = int.Parse(matchDuration.Groups["h"].Value);
                            int m = int.Parse(matchDuration.Groups["m"].Value);
                            int s = int.Parse(matchDuration.Groups["s"].Value);
                            int ms = int.Parse(matchDuration.Groups["ms"].Value) * 10;
                            dataContainer.Lenght = new TimeSpan(0, h, m, s, ms);
                        }
                    }

                    dataContainer.EstimateTakeOffPosition = EstimateFlightStartFromGPS(dataContainer);
                    dataContainer.ThumbnailImage = ExtractThumbnail(ffmpegPath, videoPath);
                    dataContainer.IsValid = IsFlightDataValid(dataContainer);
                }
            }
            catch (Exception ex)
            {
                dataContainer.HasExtractionError = true;
                dataContainer.ErrorMessages.Add(ex.Message);
            }

            return dataContainer;
        }

        //Check if stderr contains reel fatar extraction errors
        private static bool IsCriticalFFmpegError(string line)
        {
            string lower = line.ToLowerInvariant();

            return lower.Contains("error") ||
                   lower.Contains("invalid data found") ||
                   lower.Contains("stream specifier") ||
                   lower.Contains("could not find") ||
                   lower.Contains("not supported") ||
                   lower.Contains("no subtitle") ||
                   lower.Contains("failed");
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
        private static List<FlightDataPoint> ParseSRT(List<string> srtBuffer, FlightDataContainer dataContainer)
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
                        point.GPSAltitude = double.Parse(gps.Groups[3].Value, CultureInfo.InvariantCulture);
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
                        point.Height = double.Parse(hMatch.Groups[1].Value, CultureInfo.InvariantCulture);
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

        private static List<FlightDataPoint> ParseSRTMini4Pro(List<string> srtBuffer, FlightDataContainer dataContainer)
        {
            var dataPoints = new List<FlightDataPoint>();

            for (int i = 0; i < srtBuffer.Count - 4; i++)
            {
                string timestampLine = srtBuffer[i + 3].Trim();
                string metadataLine = srtBuffer[i + 4].Trim();

                if (!DateTime.TryParse(timestampLine, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime absoluteTime))
                    continue;

                var point = new FlightDataPoint
                {
                    Time = absoluteTime.ToLocalTime(),
                    TimeSpan = absoluteTime.ToLocalTime() - dataContainer.Date,
                    CameraSettings = new FlightDataPointCameraSettings()
                };

                // ✅ Altitude parsing séparé
                try
                {
                    string relAltStr = ExtractValue(metadataLine, "rel_alt");
                    string absAltStr = ExtractValue(metadataLine, "abs_alt");

                    if (!string.IsNullOrEmpty(relAltStr))
                        point.Height = double.Parse(relAltStr, CultureInfo.InvariantCulture);

                    if (!string.IsNullOrEmpty(absAltStr))
                        point.GPSAltitude = double.Parse(absAltStr, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Altitude parsing failed: {ex.Message}");
                }

                // ✅ Parsing des autres clés
                string[] tokens = metadataLine.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string token in tokens)
                {
                    // On ignore le bloc combiné rel_alt + abs_alt déjà traité
                    if (token.Contains("rel_alt:") && token.Contains("abs_alt:"))
                        continue;

                    string[] parts = token.Split(new[] { ':' }, 2);
                    if (parts.Length != 2) continue;

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

                dataPoints.Add(point);
                i += 4; // on saute au bloc suivant
            }

            return dataPoints;
        }

        private static string ExtractValue(string line, string key)
        {
            int start = line.IndexOf(key + ":");
            if (start == -1) return null;

            start += key.Length + 1;
            int end = line.IndexOfAny(new[] { ' ', ']' }, start);
            if (end == -1) end = line.Length;

            return line.Substring(start, end - start).Trim();
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

            if (points.Count < 3)
            {
                return new FlightGPSData(0.0f, 0.0f);
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
