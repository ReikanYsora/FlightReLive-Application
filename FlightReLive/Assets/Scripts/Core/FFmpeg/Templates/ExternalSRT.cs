using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlightReLive.Core.FFmpeg
{
    public class ExternalSRT : TemplateSRT
    {
        #region CONSTANTS
        private const double EARTH_RADIUS = 6371000;
        #endregion

        #region CONSTRUCTOR
        public ExternalSRT(string ffmpegPath, string videoPath) : base(ffmpegPath, videoPath) { }
        #endregion

        #region METHODS
        public override FlightDataContainer ExtractSubtitles()
        {
            string srtPath = Path.ChangeExtension(VideoPath, ".srt");

            if (!File.Exists(srtPath))
            {
                //No SRT founded
                DataContainer.HasExtractionError = true;
                DataContainer.ErrorMessages.Add($"No SRT file founded");
                DataContainer.IsValid = false;

                return DataContainer;
            }

            try
            {
                List<string> srtLines = File.ReadAllLines(srtPath).ToList();
                if (srtLines.Count > 0)
                {
                    DataContainer.DataPoints = ParseSRTFile(srtLines, DataContainer);
                    DataContainer.IsValid = IsFlightDataValid();
                    DataContainer.EstimateTakeOffPosition = EstimateFlightStartFromGPS();
                }
            }
            catch (Exception ex)
            {
                DataContainer.HasExtractionError = true;
                DataContainer.ErrorMessages.Add($"Error reading SRT file: {ex.Message}");
            }

            return DataContainer;
        }

        private List<FlightDataPoint> ParseSRTFile(List<string> srtBuffer, FlightDataContainer dataContainer)
        {
            List<FlightDataPoint> dataPoints = new List<FlightDataPoint>();

            for (int i = 0; i < srtBuffer.Count - 4; i++)
            {
                string indexLine = srtBuffer[i].Trim();
                string timeLine = srtBuffer[i + 1].Trim();
                string frameLine = srtBuffer[i + 2].Trim();
                string timestampLine = srtBuffer[i + 3].Trim();
                string metadataLine = srtBuffer[i + 4].Trim();

                if (!int.TryParse(indexLine, out _) || !timeLine.Contains("-->"))
                {
                    continue;
                }

                // Parse relative timecode
                DateTime absoluteTime;
                TimeSpan offset;

                try
                {
                    string startTime = timeLine.Split(new[] { " --> " }, StringSplitOptions.None)[0];
                    offset = ParseTimecode(startTime);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Invalid timecode at line {i}: {timeLine} ({ex.Message})");
                    continue;
                }

                // Parse absolute timestamp
                if (!DateTime.TryParse(timestampLine, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedAbsolute))
                {
                    absoluteTime = dataContainer.CreationDate.Add(offset);
                }
                else
                {
                    absoluteTime = parsedAbsolute.ToLocalTime();
                }

                FlightDataPoint point = new FlightDataPoint
                {
                    Time = absoluteTime,
                    TimeSpan = offset,
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

            dataPoints = ReduceToOnePointPerSecond(dataPoints);
            CalculateSpeeds(dataPoints);

            return dataPoints;
        }

        private float ParseShutterSpeed(string shutter)
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

        private (double relAlt, double absAlt) ExtractAltitudes(string input)
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

        private List<FlightDataPoint> ReduceToOnePointPerSecond(List<FlightDataPoint> rawPoints)
        {
            List<FlightDataPoint> reducedPoints = new List<FlightDataPoint>();
            HashSet<long> seenSeconds = new HashSet<long>();

            foreach (var point in rawPoints)
            {
                long second = new DateTimeOffset(point.Time).ToUnixTimeSeconds();

                if (!seenSeconds.Contains(second))
                {
                    reducedPoints.Add(point);
                    seenSeconds.Add(second);
                }
            }

            // Optionally ensure first and last points are included
            if (rawPoints.Count > 0)
            {
                if (!reducedPoints.Contains(rawPoints.First()))
                {
                    reducedPoints.Insert(0, rawPoints.First());
                }

                if (!reducedPoints.Contains(rawPoints.Last()))
                {
                    reducedPoints.Add(rawPoints.Last());
                }
            }

            return reducedPoints;
        }

        private double CalculateHorizontalDistance(FlightDataPoint point1, FlightDataPoint point2)
        {
            double lat1 = ToRadians(point1.Latitude);
            double lon1 = ToRadians(point1.Longitude);
            double lat2 = ToRadians(point2.Latitude);
            double lon2 = ToRadians(point2.Longitude);
            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;
            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EARTH_RADIUS * c;
        }

        private double CalculateHorizontalSpeed(FlightDataPoint point1, FlightDataPoint point2)
        {
            double distance = CalculateHorizontalDistance(point1, point2);
            double timeDifference = (point2.Time - point1.Time).TotalSeconds;

            return timeDifference > 0 ? distance / timeDifference : 0;
        }


        private double CalculateVerticalSpeed(FlightDataPoint point1, FlightDataPoint point2)
        {
            double deltaAltitude = point2.AbsoluteAltitude - point1.AbsoluteAltitude;
            double timeDifference = (point2.Time - point1.Time).TotalSeconds;

            return timeDifference > 0 ? deltaAltitude / timeDifference : 0;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        internal void CalculateSpeeds(List<FlightDataPoint> points)
        {
            for (int i = 1; i < points.Count; i++)
            {
                double horizontalSpeed = CalculateHorizontalSpeed(points[i - 1], points[i]);
                double verticalSpeed = CalculateVerticalSpeed(points[i - 1], points[i]);

                points[i].HorizontalSpeed = horizontalSpeed;
                points[i].VerticalSpeed = verticalSpeed;
            }
        }
        #endregion
    }
}
