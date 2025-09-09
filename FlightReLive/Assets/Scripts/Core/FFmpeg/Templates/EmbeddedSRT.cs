using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlightReLive.Core.FFmpeg
{
    public class EmbeddedSRT : TemplateSRT
    {
        #region CONSTRUCTOR
        public EmbeddedSRT(string ffmpegPath, string videoPath) : base(ffmpegPath, videoPath) { }
        #endregion

        #region METHODS
        public override FlightDataContainer ExtractSubtitles()
        {
            string arguments = $"-i \"{VideoPath}\" -map 0:s:0 -f srt -";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = FFmpegPath,
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

                    DataContainer.DataPoints = ParseSRT(srtLines);
                    DataContainer.EstimateTakeOffPosition = EstimateFlightStartFromGPS();
                    DataContainer.IsValid = IsFlightDataValid();
                    DataContainer.TakeOffPositionAvailable = TakeOffPositionAvailable();
                }
            }
            catch (Exception ex)
            {
                DataContainer.HasExtractionError = true;
                DataContainer.ErrorMessages.Add(ex.Message);
            }

            return DataContainer;
        }

        /// <summary>
        /// Parses the .srt file to extract individual flight data points.
        /// </summary>
        private List<FlightDataPoint> ParseSRT(List<string> srtBuffer)
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
                    absoluteTime = DataContainer.CreationDate.Add(offset);
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
        #endregion
    }
}

