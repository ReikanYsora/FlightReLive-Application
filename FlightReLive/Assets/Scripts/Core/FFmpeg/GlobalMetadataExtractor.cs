using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FlightReLive.Core.FFmpeg
{
    internal class GlobalMetadata
    {
        #region PROPERTIES
        internal DateTime CreationDate { get; set; }

        internal TimeSpan Duration { get; set; }

        internal byte[] Thumbnail { get; set; }
        #endregion
    }

    internal static class GlobalMetadataExtractor
    {
        /// <summary>
        /// Extract date and lengh from the original video file
        /// </summary>
        /// <param name="ffmpegPath">ffmpeg path</param>
        /// <param name="videoPath">video path</param>
        /// <returns></returns>
        internal static GlobalMetadata ExtractMetadata(string ffmpegPath, string videoPath)
        {
            // Vérification de la validité des chemins
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                Console.WriteLine("FFmpeg path is not valid.");
                return null;
            }

            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                Console.WriteLine("Video file not found.");
                return null;
            }

            // Commande FFmpeg pour obtenir les métadonnées du fichier vidéo
            string arguments = $"-i \"{videoPath}\"";  // On n'a besoin que des métadonnées de base

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
                GlobalMetadata result = new GlobalMetadata();

                using (Process process = Process.Start(psi))
                {
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    //Extract date
                    string creationTimePattern = @"creation_time\s*[:=]\s*(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z)";
                    var creationMatch = Regex.Match(errorOutput, creationTimePattern);

                    if (creationMatch.Success)
                    {
                        string creationTimeStr = creationMatch.Groups[1].Value;

                        if (DateTime.TryParseExact(creationTimeStr, "yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
                        {
                            result.CreationDate = parsedDate.ToLocalTime();
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"Failed to parse creation time: {creationTimeStr}");
                        }
                    }

                    //Extract length
                    string durationPattern = @"Duration:\s*(\d{2}):(\d{2}):(\d{2})\.(\d{2})";
                    var durationMatch = Regex.Match(errorOutput, durationPattern);

                    if (durationMatch.Success)
                    {
                        int hours = int.Parse(durationMatch.Groups[1].Value);
                        int minutes = int.Parse(durationMatch.Groups[2].Value);
                        int seconds = int.Parse(durationMatch.Groups[3].Value);
                        int milliseconds = int.Parse(durationMatch.Groups[4].Value) * 10;

                        result.Duration = new TimeSpan(hours, minutes, seconds, 0, milliseconds);
                    }

                    result.Thumbnail = ExtractThumbnail(ffmpegPath, videoPath);

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while extracting video metadata: {ex.Message}");
            }
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
    }
}
