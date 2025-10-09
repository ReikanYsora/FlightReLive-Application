using FlightReLive.Core.FlightDefinition;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Video
{
    /// <summary>
    /// Manages the lifecycle of the external VLC process (launch/unload only).
    /// </summary>
    public class VideoSyncManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("VLC Integration")]
        [SerializeField] private string _vlcExecutablePath;

        private VLCPlaybackManager _vlc;
        private bool _hasVideo;

        internal static VideoSyncManager Instance { get; private set; }
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _vlc = new VLCPlaybackManager();
            _vlc.OnError += (msg) => Debug.LogWarning("[VLCPlaybackManager] " + msg);
            _vlc.OnVlcStarted += () => Debug.Log("[VLCPlaybackManager] VLC started.");
            _vlc.OnVlcExited += () => Debug.LogWarning("[VLCPlaybackManager] VLC closed.");
        }

        private async void OnDestroy()
        {
            if (_vlc != null)
            {
                await _vlc.CloseAsync();
                _vlc = null;
            }
        }
        #endregion

        #region PUBLIC METHODS
        internal async void Load(FlightData flightData)
        {
            if (flightData == null || string.IsNullOrEmpty(flightData.VideoPath) || !File.Exists(flightData.VideoPath))
            {
                _hasVideo = false;
                return;
            }

            _hasVideo = true;

            // Use the source video dimensions as both the window size and the exact aspect ratio.
            bool ok = await _vlc.LaunchAsync(
                _vlcExecutablePath,
                flightData.VideoPath,
                flightData.Width,
                flightData.Height,
                flightData.Width,
                flightData.Height,
                200,
                150
            );

            if (!ok)
            {
                Debug.LogError("[VideoSyncManager] VLC failed to start.");
                _hasVideo = false;
            }
        }

        internal async Task Unload()
        {
            if (_vlc != null && _vlc.IsRunning)
            {
                Debug.Log("[VideoSyncManager] Closing VLC...");
                await _vlc.CloseAsync();
            }

            _hasVideo = false;
        }
        #endregion
    }
}
