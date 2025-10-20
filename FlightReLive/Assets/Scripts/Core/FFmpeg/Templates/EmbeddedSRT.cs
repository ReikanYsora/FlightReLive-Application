using FlightReLive.Core.Database;
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
                    DataContainer.TakeOffPositionAvailable = TakeOffPositionAvailable();

                    //Check flight validity, throw exception in case of error was founded
                    CheckFlightIsValid();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return DataContainer;
        }

        /// <summary>
        /// Parses the .srt file to extract individual flight data points.
        /// </summary>
        private List<SerializedFlightDataPoint> ParseSRT(List<string> srtBuffer)
        {
            List<SerializedFlightDataPoint> dataPoints = new List<SerializedFlightDataPoint>();

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
                    throw ex;
                }

                SerializedFlightDataPoint point = new SerializedFlightDataPoint { Time = absoluteTime, TimeSpan = offset };

                try
                {
                    Match gps = gpsRegex.Match(dataLine);
                    if (gps.Success && gps.Groups.Count >= 4)
                    {
                        point.Coordinate = new SerializedGPSCoordinate(double.Parse(gps.Groups[2].Value, CultureInfo.InvariantCulture), double.Parse(gps.Groups[1].Value, CultureInfo.InvariantCulture));
                    }

                    Match camera = cameraRegex.Match(dataLine);

                    if (camera.Success && camera.Groups.Count >= 6)
                    {
                        point.Aperture = float.Parse(camera.Groups[1].Value, CultureInfo.InvariantCulture);
                        point.ShutterSpeed = float.Parse(camera.Groups[2].Value, CultureInfo.InvariantCulture);
                        point.ISO = int.Parse(camera.Groups[3].Value, CultureInfo.InvariantCulture);
                        point.Exposure = float.Parse(camera.Groups[4].Value, CultureInfo.InvariantCulture);
                        point.DigitalZoom = float.Parse(camera.Groups[5].Value, CultureInfo.InvariantCulture);
                        point.ColorMode = "";
                    }

                    Match dMatch = dRegex.Match(dataLine);
                    if (dMatch.Success && dMatch.Groups.Count >= 2)
                    {
                        point.Distance = float.Parse(dMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }

                    Match hMatch = hRegex.Match(dataLine);
                    if (hMatch.Success && hMatch.Groups.Count >= 2)
                    {
                        point.RelativeAltitude = float.Parse(hMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }

                    Match hsMatch = hsRegex.Match(dataLine);
                    if (hsMatch.Success && hsMatch.Groups.Count >= 2)
                    {
                        point.HorizontalSpeed = float.Parse(hsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }

                    Match vsMatch = vsRegex.Match(dataLine);
                    if (vsMatch.Success && vsMatch.Groups.Count >= 2)
                    {
                        point.VerticalSpeed = float.Parse(vsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                dataPoints.Add(point);
            }

            return dataPoints;
        }
        #endregion
    }
}

