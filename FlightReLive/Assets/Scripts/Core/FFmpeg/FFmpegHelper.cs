using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
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
        internal static string GetFFmpegPath()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            //macOS (Editor or Standalone)
            string basePath = Path.Combine(Application.streamingAssetsPath, "ffmpeg");

            //Detect actual runtime architecture
            Architecture arch = RuntimeInformation.ProcessArchitecture;

            switch (arch)
            {
                case Architecture.Arm64:
                    return Path.Combine(basePath, "AppleSilicon", "ffmpeg");
                case Architecture.X64:
                    return Path.Combine(basePath, "Intel", "ffmpeg");
                default:
                    UnityEngine.Debug.LogError($"Unsupported macOS architecture: {arch}");
                    return string.Empty;
            }

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Windows (Editor or Standalone)
            return Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg.exe");
#endif
        }

        /// <summary>
        /// Extracts video metadata (resolution, fps) from FFmpeg stderr.
        /// </summary>
        public static void ExtractVideoMetadata(string videoPath, FlightDataContainer container)
        {
            string ffmpegPath = GetFFmpegPath();
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                throw new Exception("FFmpeg path invalid.");
            }

            if (!File.Exists(videoPath))
            {
                throw new Exception($"Video {videoPath} not found.");
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

                    Match match = Regex.Match(errorOutput, @"Video:.*?(\d{2,5})x(\d{2,5}).*?([\d\.]+)\s*fps", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int width))
                        {
                            container.Width = width;
                        }

                        if (int.TryParse(match.Groups[2].Value, out int height))
                        {
                            container.Height = height;
                        }

                        if (float.TryParse(match.Groups[3].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float fps))
                        {
                            container.Frequency = fps;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception($"FFmpeg metadata extraction failed.");
            }
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
        /// Runs FFmpeg as a child process to extract subtitles and capture metadata from stderr.
        /// </summary>
        private static FlightDataContainer ExtractSubtitles(string ffmpegPath, string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                throw new Exception("Video file not found.");
            }

            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                throw new Exception("FFmpeg path is not set or executable not found.");
            }

            //Define template with automatic detection (if SRT is embedded in the video file or not)
            if (IsEmbeddedSubtitles(ffmpegPath, videoPath))
            {
                return new EmbeddedSRT(ffmpegPath, videoPath).DataContainer;
            }
            else
            {
                return new ExternalSRT(ffmpegPath, videoPath).DataContainer;
            }
        }

        /// <summary>
        /// Check if a video file has integrated subtitiles
        /// </summary>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">video path</param>
        /// <returns></returns>
        private static bool IsEmbeddedSubtitles(string ffmpegPath, string videoPath)
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
            catch (Exception)
            {
                Console.WriteLine($"An error occurred while checking subtitles.");
                return false;
            }
        }
        #endregion
    }
}
