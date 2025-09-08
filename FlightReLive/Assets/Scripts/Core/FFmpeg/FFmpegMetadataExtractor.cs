using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public class MetadataStream
{
    public string StreamId { get; set; }       // ex: "0:0"
    public string Type { get; set; }           // ex: "Video"
    public string Codec { get; set; }          // ex: "hevc (Main)"
    public string Details { get; set; }        // ex: "yuv420p(tv, bt709), 3840x2160, 90196 kb/s, 29.97 fps"
    public string Handler { get; set; }        // ex: "VideoHandler" ou "DJI meta"
}

public class FFmpegMetadataExtractor
{
    public static List<MetadataStream> ExtractAllStreams(string ffmpegPath, string videoPath)
    {
        var result = new List<MetadataStream>();

        if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            throw new FileNotFoundException("FFmpeg path is not valid.");

        if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            throw new FileNotFoundException("Video file not found.");

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

        using (Process process = Process.Start(psi))
        {
            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Regex générique pour tous les flux (Video, Audio, Data, Subtitle…)
            var streamRegex = new Regex(
                @"Stream #(?<id>\d+:\d+).*?: (?<type>Video|Audio|Subtitle|Data).*?: (?<codec>[^,]+)(?:, (?<details>.*))?",
                RegexOptions.IgnoreCase);

            var handlerRegex = new Regex(@"handler_name\s*:\s*(?<handler>.+)", RegexOptions.IgnoreCase);

            string[] lines = errorOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            MetadataStream currentStream = null;

            foreach (string line in lines)
            {
                var match = streamRegex.Match(line);
                if (match.Success)
                {
                    currentStream = new MetadataStream
                    {
                        StreamId = match.Groups["id"].Value,
                        Type = match.Groups["type"].Value,
                        Codec = match.Groups["codec"].Value.Trim(),
                        Details = match.Groups["details"].Value.Trim()
                    };
                    result.Add(currentStream);
                }
                else if (currentStream != null)
                {
                    var handlerMatch = handlerRegex.Match(line);
                    if (handlerMatch.Success)
                    {
                        currentStream.Handler = handlerMatch.Groups["handler"].Value.Trim();
                        currentStream = null; // reset
                    }
                }
            }
        }

        return result;
    }
}
