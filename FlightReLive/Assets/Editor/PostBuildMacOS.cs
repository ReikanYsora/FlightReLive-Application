using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;
#if UNITY_STANDALONE_OSX
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

public class PostBuildMacOS
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
#if UNITY_STANDALONE_OSX
        if (target != BuildTarget.StandaloneOSX)
        {
            return;
        }

        string contentsPath = Path.Combine(pathToBuiltProject, "Contents");
        string plistPath = Path.Combine(contentsPath, "Info.plist");
        string resourcesPath = Path.Combine(contentsPath, "Resources");

        // Copy custom macOS icon into the Resources folder, overwriting Unity's default
        string sourceIconPath = "Assets/Rendering/Textures/Logo/app.icns";
        string targetIconPath = Path.Combine(resourcesPath, "PlayerIcon.icns"); // Unity expects this name

        if (File.Exists(sourceIconPath))
        {
            Directory.CreateDirectory(resourcesPath);
            File.Copy(sourceIconPath, targetIconPath, true);
            UnityEngine.Debug.Log("Custom icon copied as PlayerIcon.icns");
        }
        else
        {
            UnityEngine.Debug.LogWarning("Missing .icns file: " + sourceIconPath);
        }

        // Modify Info.plist using Unity's PlistDocument API
        if (!File.Exists(plistPath))
        {
            UnityEngine.Debug.LogWarning("Info.plist not found!");
            return;
        }

        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict rootDict = plist.root;

        rootDict.SetString("CFBundleIconFile", "PlayerIcon"); // No extension
        rootDict.SetString("NSApplicationSupportDirectoryUsageDescription", "Access to Application Support folder.");
        rootDict.SetString("NSDocumentsFolderUsageDescription", "Access to Documents folder.");
        rootDict.SetString("NSDownloadsFolderUsageDescription", "Access to Downloads folder.");
        rootDict.SetString("NSDesktopFolderUsageDescription", "Access to Desktop folder.");

        plist.WriteToFile(plistPath);
        UnityEngine.Debug.Log("Info.plist updated successfully.");

        // Validate Info.plist structure using plutil
        string plistValidation = RunShellCommand("plutil", $"-lint \"{plistPath}\"");
        UnityEngine.Debug.Log($"Info.plist validation result: {plistValidation}");

        // Remove Burst debug folder if it exists
        string burstDebugFolder = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), "Flight ReLive_BurstDebugInformation_DoNotShip");
        if (Directory.Exists(burstDebugFolder))
        {
            Directory.Delete(burstDebugFolder, true);
            UnityEngine.Debug.Log("Burst debug folder deleted.");
        }

        // Sign FFmpeg binary directly in StreamingAssets
        string ffmpegPath = "Assets/StreamingAssets/ffmpeg";
        if (File.Exists(ffmpegPath))
        {
            RunShellCommand("chmod", $"+x \"{ffmpegPath}\"");

            string signArgs = $"--force --options runtime --sign \"Developer ID Application: JEROME PASCAL JACKY CREMOUX (UJQW9XHQ37)\" \"{ffmpegPath}\"";
            string signOutput = RunShellCommand("codesign", signArgs);
            UnityEngine.Debug.Log($"FFmpeg signing result (StreamingAssets): {signOutput}");
        }
        else
        {
            UnityEngine.Debug.LogWarning("FFmpeg binary not found in StreamingAssets: " + ffmpegPath);
        }        
#endif
    }

    private static string RunShellCommand(string command, string arguments)
    {
        var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return string.IsNullOrEmpty(error) ? output.Trim() : error.Trim();
    }
}
