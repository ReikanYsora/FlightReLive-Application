using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Video
{
    /// <summary>
    /// External VLC process manager (clean minimal version).
    /// - Allows custom aspect ratio and window size.
    /// - Disables subtitles, media bar, looping.
    /// - No unsupported or platform-specific arguments.
    /// </summary>
    public class VLCPlaybackManager
    {
        #region EVENTS
        public event Action OnVlcStarted;
        public event Action OnVlcExited;
        public event Action<string> OnError;
        #endregion

        #region ATTRIBUTES
        private Process _vlcProcess;
        private string _vlcPath;
        private bool _isRunning;
        #endregion

        #region PROPERTIES
        public bool IsRunning
        {
            get
            { 
                return _isRunning;
            }
        }

        public string VideoPath { get; private set; }
        #endregion

        #region METHODS
        /// <summary>
        /// Validates the VLC executable path.
        /// </summary>
        public bool Initialize(string vlcPath)
        {
            _vlcPath = vlcPath;

            if (string.IsNullOrEmpty(_vlcPath) || !File.Exists(_vlcPath))
            {
                if (OnError != null)
                {
                    OnError.Invoke("VLC executable not found at: " + _vlcPath);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Launches VLC with specific aspect ratio, window size, and clean interface.
        /// </summary>
        /// <summary>
        /// Launches VLC with: custom aspect ratio, window size, no subtitles, no media bar, no loop.
        /// The video file path is appended at the end of the arguments (required by VLC).
        /// </summary>
        public async Task<bool> LaunchAsync(
            string vlcPath,
            string videoPath,
            int windowWidth,
            int windowHeight,
            int aspectWidth,
            int aspectHeight,
            int x = 200,
            int y = 150)
        {
            if (!Initialize(vlcPath))
            {
                return false;
            }

            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                if (OnError != null)
                {
                    OnError.Invoke("Video file not found: " + videoPath);
                }

                return false;
            }

            try
            {
                KillExistingVLC();

                // Build a strict ratio like "3840:2160" (VLC accepts W:H).
                string ratio = aspectWidth.ToString(CultureInfo.InvariantCulture) + ":" +
                               aspectHeight.ToString(CultureInfo.InvariantCulture);

                // We use RC interface so there is no Qt UI / mediabar.
                // Only safe, cross-platform flags are used.
                string args =
                    "--intf dummy " +                        // interface minimale
                    "--no-video-title-show " +
                    "--no-sub-autodetect-file " +
                    "--no-loop " +
                    "--no-repeat " +
                    "--no-playlist-autostart " +
                    "--no-osd " +
                    "--aspect-ratio=" + ratio + " " +
                    "--width=" + windowWidth + " " +
                    "--height=" + windowHeight + " " +
                    "--video-x=" + x + " " +
                    "--video-y=" + y + " " +
                    "--play-and-exit " +                    // ferme VLC à la fin
                    "\"" + videoPath + "\"";

                _vlcProcess = new Process();
                _vlcProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = _vlcPath,
                    Arguments = args,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                _vlcProcess.EnableRaisingEvents = true;
                _vlcProcess.Exited += HandleVlcExited;

                bool started = _vlcProcess.Start();

                if (started)
                {
                    _isRunning = true;
                    VideoPath = videoPath;

                    if (OnVlcStarted != null)
                    {
                        OnVlcStarted.Invoke();
                    }

                    await Task.Delay(150);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (OnError != null)
                {
                    OnError.Invoke("Failed to start VLC: " + ex.Message);
                }

                return false;
            }
        }

        /// <summary>
        /// Closes the VLC process if running.
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                if (_vlcProcess != null && !_vlcProcess.HasExited)
                {
                    _vlcProcess.Kill();
                    await Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
                if (OnError != null)
                {
                    OnError.Invoke("Error closing VLC: " + ex.Message);
                }
            }
            finally
            {
                _isRunning = false;
                _vlcProcess = null;
                VideoPath = null;
            }
        }

        private void HandleVlcExited(object sender, EventArgs e)
        {
            _isRunning = false;

            if (OnVlcExited != null)
            {
                OnVlcExited.Invoke();
            }
        }

        private void KillExistingVLC()
        {
#if UNITY_STANDALONE_WIN
            Process[] procs = Process.GetProcessesByName("vlc");

            for (int i = 0; i < procs.Length; i++)
            {
                try
                {
                    procs[i].Kill();
                }
                catch { }
            }
#elif UNITY_STANDALONE_OSX
            try
            {
                Process.Start("killall", "VLC");
            }
            catch { }
#endif
        }
        #endregion
    }
}
