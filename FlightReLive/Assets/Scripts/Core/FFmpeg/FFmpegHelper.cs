using System;
using System.Diagnostics;
using System.IO;
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
        /// Runs FFmpeg as a child process to extract subtitles and capture metadata from stderr.
        /// </summary>
        private static FlightDataContainer ExtractSubtitles(string ffmpegPath, string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                UnityEngine.Debug.LogError("Video file not found.");
                return null;
            }

            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                UnityEngine.Debug.LogError("FFmpeg path is not set or executable not found.");
                return null;
            }

            try
            {
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
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return null;
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
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking subtitles: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}
