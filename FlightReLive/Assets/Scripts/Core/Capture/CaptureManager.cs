using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlightReLive.Core.Capture
{
    public class CaptureManager : MonoBehaviour
    {
        #region CONSTANTS
        private const int MAX_PENDING_FRAMES = 3;
        #endregion

        #region ATTRIBUTES
        [Header("Capture Settings")]
        [SerializeField] private Camera _cameraToDuplicate;
        [SerializeField] private Material _captureFlipVerticalMaterial;
        internal static Dictionary<int, string> _resolutions = new Dictionary<int, string>()
        {
            { 0, "720p (1280x720)" },
            { 1, "1080p (1920x1080)" },
            { 2, "1440p (2560x1440)" },
            { 3, "4K (3840x2160)" }
        };

        internal static Dictionary<int, string> _encoders;

        internal static Dictionary<int, string> _framerates = new Dictionary<int, string>()
        {
            { 0, "30 FPS" },
            { 1, "60 FPS" },
            { 2, "90 FPS" },
            { 3, "120 FPS" }
        };

        [Header("Output Settings")]
        [SerializeField] private string _filePrefix;

        [Header("Logo Overlay")]
        [SerializeField] private string _logoFileName = "logo.png";

        private RenderTexture _renderTexture;
        private RenderTexture _flippedTexture;
        private Process _ffmpegProcess;
        private string _outputPath;
        private GameObject _captureCameraObjectInstance;
        private Camera _captureCameraInstance;
        private Queue<byte[]> _frameQueue = new Queue<byte[]>();
        private Thread _writerThread;
        private bool _writerRunning = false;
        private int _width;
        private int _height;
        private int _encoder;
        private int _framerate;
        private DateTime _captureStartTime;
        private string _captureElapsedTime;
        private AutoResetEvent _frameAvailable = new AutoResetEvent(false);
        #endregion

        #region PROPERTIES
        internal static CaptureManager Instance { get; private set; }

        internal bool IsCapturing { get; private set; }

        public string ElapsedTime
        {
            get { return _captureElapsedTime; }
        }
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
            DetectAvailableEncoders();
        }

        private void Start()
        {
            _encoder = SettingsManager.CurrentSettings.CaptureEncoder;
            _framerate = SettingsManager.CurrentSettings.CaptureFramerate;
            SettingsManager.OnCaptureResolutionChanged += OnCaptureResolutionChanged;
            SettingsManager.OnCaptureEncoderChanged += OnCaptureEncoderChanged;
            SettingsManager.OnCaptureFramerateChanged += OnCaptureFramerateChanged;
        }

        private void Update()
        {
            if (!IsCapturing)
            {
                return;
            }

            CaptureFrame();

            if (_cameraToDuplicate != null && _captureCameraInstance != null)
            {
                _captureCameraInstance.transform.position = _cameraToDuplicate.transform.position;
                _captureCameraInstance.transform.rotation = _cameraToDuplicate.transform.rotation;
            }

            TimeSpan elapsed = DateTime.Now - _captureStartTime;
            _captureElapsedTime = string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
        }

        private void OnDestroy()
        {
            SettingsManager.OnCaptureResolutionChanged -= OnCaptureResolutionChanged;
            SettingsManager.OnCaptureEncoderChanged -= OnCaptureEncoderChanged;
            SettingsManager.OnCaptureFramerateChanged -= OnCaptureFramerateChanged;

            if (IsCapturing)
            {
                StopCapture();
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Detect available video encoders on this system by querying FFmpeg.
        /// Updates the internal _encoders dictionary accordingly.
        /// </summary>
        private void DetectAvailableEncoders()
        {
            string ffmpegPath = GetPlatformFFmpegPath();

            if (!File.Exists(ffmpegPath))
            {
                _encoders = new Dictionary<int, string> { { 0, "X264 (Default)" } };
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-hide_banner -encoders",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    bool hasNvenc = output.Contains("h264_nvenc", StringComparison.OrdinalIgnoreCase);
                    bool hasAv1Nvenc = output.Contains("av1_nvenc", StringComparison.OrdinalIgnoreCase);
                    bool hasAmf = output.Contains("h264_amf", StringComparison.OrdinalIgnoreCase);
                    bool hasQsv = output.Contains("h264_qsv", StringComparison.OrdinalIgnoreCase);
                    bool hasVtb = output.Contains("h264_videotoolbox", StringComparison.OrdinalIgnoreCase);

                    _encoders = new Dictionary<int, string> { { 0, "X264 (Default)" } };

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    if (hasNvenc)
                    { 
                        _encoders.Add(1, "NVENC (NVIDIA)");
                    }

                    if (hasAv1Nvenc)
                    {
                        _encoders.Add(2, "AV1 (NVIDIA)");
                    } 

                    if (hasAmf)
                    {
                        _encoders.Add(3, "AMF (AMD)");
                    }

                    if (hasQsv)
                    {
                        _encoders.Add(4, "QuickSync (Intel)");
                    }
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                    if (hasVtb)
                    {
                        _encoders.Add(1, "VideoToolbox (Apple)");
                    }
#endif
                }

                // alidate the user's saved encoder index
                int savedEncoder = SettingsManager.CurrentSettings.CaptureEncoder;
                if (!_encoders.ContainsKey(savedEncoder))
                {
                    SettingsManager.SaveCaptureEncoder(0);
                }
            }
            catch (Exception)
            {
                _encoders = new Dictionary<int, string> { { 0, "X264 (Default)" } };
            }
        }

        private void StartCapture()
        {
            //Duplicate current camera
            _captureCameraObjectInstance = new GameObject("CaptureCamera");
            _captureCameraInstance = _captureCameraObjectInstance.AddComponent<Camera>();

            if (_cameraToDuplicate != null)
            {
                // Synchronise position et rotation
                _captureCameraObjectInstance.transform.position = _cameraToDuplicate.transform.position;
                _captureCameraObjectInstance.transform.rotation = _cameraToDuplicate.transform.rotation;

                // Copie les paramètres essentiels
                _captureCameraInstance.fieldOfView = _cameraToDuplicate.fieldOfView;
                _captureCameraInstance.nearClipPlane = _cameraToDuplicate.nearClipPlane;
                _captureCameraInstance.farClipPlane = _cameraToDuplicate.farClipPlane;
                _captureCameraInstance.orthographic = _cameraToDuplicate.orthographic;
                _captureCameraInstance.orthographicSize = _cameraToDuplicate.orthographicSize;
                _captureCameraInstance.allowHDR = _cameraToDuplicate.allowHDR;
                _captureCameraInstance.allowMSAA = _cameraToDuplicate.allowMSAA;
                _captureCameraInstance.depth = _cameraToDuplicate.depth;
                _captureCameraInstance.cullingMask = _cameraToDuplicate.cullingMask;

                UniversalAdditionalCameraData additionalData = _captureCameraInstance.gameObject.GetComponent<UniversalAdditionalCameraData>();

                if (additionalData == null)
                {
                    additionalData = _captureCameraInstance.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }

                UniversalAdditionalCameraData sourceData = _cameraToDuplicate.GetComponent<UniversalAdditionalCameraData>();

                if (sourceData != null)
                {
                    additionalData.renderPostProcessing = sourceData.renderPostProcessing;
                    additionalData.volumeLayerMask = sourceData.volumeLayerMask;
                    additionalData.volumeTrigger = sourceData.volumeTrigger;
                    additionalData.antialiasing = sourceData.antialiasing;
                    additionalData.antialiasingQuality = sourceData.antialiasingQuality;
                    additionalData.volumeTrigger = _captureCameraInstance.transform;
                }

                _captureCameraInstance.clearFlags = CameraClearFlags.Skybox;

                if (additionalData != null)
                {
                    additionalData.renderPostProcessing = true;
                    additionalData.requiresColorOption = CameraOverrideOption.On;
                    additionalData.requiresDepthOption = CameraOverrideOption.On;

                    if (_cameraToDuplicate != null)
                    {
                        UniversalAdditionalCameraData srcData = _cameraToDuplicate.GetComponent<UniversalAdditionalCameraData>();
                        if (srcData != null)
                        {
                            additionalData.volumeLayerMask = srcData.volumeLayerMask;
                            additionalData.volumeTrigger = srcData.volumeTrigger != null
                                ? srcData.volumeTrigger
                                : _cameraToDuplicate.transform;
                        }
                    }

                    VolumeStack stack = VolumeManager.instance.stack;
                    VolumeManager.instance.Update(stack, _captureCameraInstance.transform, additionalData.volumeLayerMask);
                }
            }

            SetupRenderTexture();
            PrepareOutputPath();

            if (IsCapturing)
            {
                return;
            }

            string ffmpegPath = GetPlatformFFmpegPath();
            string logoPath = Path.Combine(Application.streamingAssetsPath, "Images", _logoFileName).Replace("\\", "/");

            if (!File.Exists(ffmpegPath))
            {
                Fugui.Notify("Critial capture error", "Unable to start capture recording.", StateType.Danger, 5f);
                return;
            }

            int framerateValue;

            switch (_framerate)
            {
                default:
                case 0:
                    framerateValue = 30;
                    break;
                case 1:
                    framerateValue = 60;
                    break;
                case 2:
                    framerateValue = 90;
                    break;
                case 3:
                    framerateValue = 120;
                    break;
            }

            string ffmpegInput =
                $"-y -fflags +genpts -use_wallclock_as_timestamps 1 " +
                $"-f rawvideo -pixel_format rgba -video_size {_width}x{_height} -framerate {framerateValue} -i -";


            string ffmpegFilter = "";
            string encoderArgs;


            switch (_encoder)
            {
                default:
                case 0:
                    encoderArgs = "-c:v libx264 -preset ultrafast -b:v 10M";
                    break;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                case 1:
                    encoderArgs = "-c:v h264_videotoolbox -b:v 10M -pix_fmt yuv420p -allow_sw 1";
                    break;
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                case 1:
                    encoderArgs = "-c:v h264_nvenc -preset p1 -b:v 10M";
                    break;
                case 2:
                    encoderArgs = "-c:v av1_nvenc -preset p5 -cq 30";
                    break;
#endif
            }

            string ffmpegOutput = $"-r {framerateValue} -an {encoderArgs} -pix_fmt yuv420p -movflags +faststart \"{_outputPath}\"";

            if (File.Exists(logoPath) && SettingsManager.CurrentSettings.CaptureEncodedLogo)
            {
                ffmpegInput += $" -i \"{logoPath}\"";
                ffmpegFilter = "-filter_complex \"[1:v]scale=256:256[logo];[0:v][logo]overlay=10:H-h-10\"";
            }

            string args = $"{ffmpegInput} {ffmpegFilter} {ffmpegOutput}";

            _ffmpegProcess = new Process();
            _ffmpegProcess.StartInfo.FileName = ffmpegPath;
            _ffmpegProcess.StartInfo.Arguments = args;
            _ffmpegProcess.StartInfo.UseShellExecute = false;
            _ffmpegProcess.StartInfo.RedirectStandardInput = true;
            _ffmpegProcess.StartInfo.CreateNoWindow = true;
            _ffmpegProcess.StartInfo.RedirectStandardError = true;
            _ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            _ffmpegProcess.Start();

            _writerRunning = true;
            _writerThread = new Thread(WriterLoop)
            {
                Name = "FFmpegWriterThread",
                IsBackground = true
            };
            _writerThread.Start();
            IsCapturing = true;
            _captureStartTime = DateTime.Now;

            Fugui.Notify("Capture started", $"Capture started ({_width}x{_height}).\nOutput path : {_outputPath}.", StateType.Info, 5f);
        }

        /// <summary>
        /// Thread loop that writes captured frames to FFmpeg input stream.
        /// Includes safety checks for FFmpeg process death and clean shutdown.
        /// </summary>
        private void WriterLoop()
        {
            try
            {
                while (_writerRunning)
                {
                    byte[] frame = null;

                    //Dequeue frame
                    lock (_frameQueue)
                    {
                        if (_frameQueue.Count > 0)
                        {
                            frame = _frameQueue.Dequeue();
                        }
                    }

                    //If FFmpeg has exited, stop immediately
                    if (_ffmpegProcess == null || _ffmpegProcess.HasExited)
                    {
                        _writerRunning = false;
                        break;
                    }

                    //If frame available, write to stdin
                    if (frame != null)
                    {
                        try
                        {
                            _ffmpegProcess.StandardInput.BaseStream.Write(frame, 0, frame.Length);
                            _ffmpegProcess.StandardInput.BaseStream.Flush();
                        }
                        catch (Exception e)
                        {
                            // top on write failure (pipe closed or FFmpeg dead)
                            UnityEngine.Debug.LogWarning($"[CaptureManager] FFmpeg write error: {e.Message}");
                            _writerRunning = false;
                            break;
                        }
                    }
                    else
                    {
                        _frameAvailable.WaitOne(5);
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CaptureManager] Writer thread crashed: {e.Message}");
            }
            finally
            {
                try
                {
                    _ffmpegProcess?.StandardInput?.Flush();
                    _ffmpegProcess?.StandardInput?.Close();
                }
                catch { }
            }
        }


        /// <summary>
        /// Configure and create render textures for frame capture.
        /// </summary>
        private void SetupRenderTexture()
        {
            int captureResolution = SettingsManager.CurrentSettings.CaptureResolution;

            switch (captureResolution)
            {
                case 0:
                    _width = 1280;
                    _height = 720;
                    break;
                default:
                case 1:
                    _width = 1920;
                    _height = 1080;
                    break;
                case 2:
                    _width = 2560;
                    _height = 1440;
                    break;
                case 3:
                    _width = 3840;
                    _height = 2160;
                    break;
            }

            RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;

            //Main render target
            _renderTexture = new RenderTexture(_width, _height, 24, rtFormat);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();

            _captureCameraInstance.allowHDR = true;
            _captureCameraInstance.forceIntoRenderTexture = true;
            _captureCameraInstance.targetTexture = _renderTexture;

            //Flipped texture for GPU readback
            _flippedTexture = new RenderTexture(_width, _height, 0, rtFormat);
            _flippedTexture.Create();
        }

        private void PrepareOutputPath()
        {
            string defaultPath = Path.Combine(Application.persistentDataPath, "Captures");

            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _outputPath = Path.Combine(defaultPath, $"{_filePrefix}{timestamp}.mp4");

        }
        private void CaptureFrame()
        {
            if (!IsCapturing || _ffmpegProcess == null || _ffmpegProcess.HasExited)
            {
                return;
            }

            if (_frameQueue.Count < MAX_PENDING_FRAMES)
            {
                Graphics.Blit(_renderTexture, _flippedTexture, _captureFlipVerticalMaterial);
                AsyncGPUReadback.Request(_flippedTexture, 0, TextureFormat.RGBA32, OnFrameReadback);
            }
        }

        private void OnFrameReadback(AsyncGPUReadbackRequest request)
        {
            if (request.hasError || !IsCapturing || _ffmpegProcess == null)
            {
                return;
            }

            byte[] frameBytes = request.GetData<byte>().ToArray();

            lock (_frameQueue)
            {
                _frameQueue.Enqueue(frameBytes);
                _frameAvailable.Set();
            }
        }

        private void StopCapture()
        {
            IsCapturing = false;
            _captureElapsedTime = "";
            _captureStartTime = DateTime.MinValue;

            try
            {
                _writerRunning = false;

                //Syop writer thread
                if (_writerThread != null && _writerThread.IsAlive)
                {
                    _writerThread.Join(500);
                    _writerThread = null;
                }

                //Close FFmpeg input
                _ffmpegProcess?.StandardInput?.Close();

                //Wait for FFmpeg exiting
                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    if (!_ffmpegProcess.WaitForExit(1000))
                    {
                        UnityEngine.Debug.LogWarning("[CaptureManager] FFmpeg did not exit in time, killing process...");
                        _ffmpegProcess.Kill();
                    }
                }

                _ffmpegProcess?.Dispose();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Erreur à la fermeture de FFmpeg : {e.Message}");
            }

            //Release GPU resources
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }

            if (_flippedTexture != null)
            {
                _flippedTexture.Release();
                _flippedTexture = null;
            }

            //Clean temp camera
            if (_captureCameraObjectInstance != null)
            {
                Destroy(_captureCameraObjectInstance);
                _captureCameraObjectInstance = null;
                _captureCameraInstance = null;
            }

            //Flush waiting queue
            lock (_frameQueue)
            {
                _frameQueue.Clear();
            }

            //Rest frame available signal
            _frameAvailable?.Dispose();
            _frameAvailable = new AutoResetEvent(false);

            Fugui.Notify("Capture stopped", $"Capture stopped ({_width}x{_height}).\nOutput path : {_outputPath}.", StateType.Info, 5f);
        }

        internal void ToggleCapture()
        {
            if (!IsCapturing)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        string GetPlatformFFmpegPath()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg.exe");
#else
        throw new PlatformNotSupportedException("Plateforme non supportée.");
#endif
        }
        #endregion

        #region UI
        internal void DrawCaptureModeSettings(FuLayout layout)
        {
            layout.FramedText("Capture");
            layout.Separator();

            using (FuGrid grid = new FuGrid("grdCaptureSettings", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                if (IsCapturing)
                {
                    grid.DisableNextElements();
                }

                SettingsManager.DisplaySettingsComboboxWithReset(
                    grid,
                    "Capture resolution",
                    "Capture output native resolution.",
                    "Reset to default resolution",
                    SettingsManager.CurrentSettings.CaptureResolution,
                    SettingsManager.CAPTURE_RESOLUTION_DEFAULT_VALUE,
                    (id) => _resolutions.ContainsKey(id) ? _resolutions[id] : "Unknown",
                    _resolutions.Keys,
                    (newId) => SettingsManager.SaveCaptureResolution(newId),
                    () => SettingsManager.ResetCaptureResolution()
                );

                SettingsManager.DisplaySettingsComboboxWithReset(
                    grid,
                    "Capture framerate",
                    "Capture output native framerate.",
                    "Reset to default framerate",
                    SettingsManager.CurrentSettings.CaptureFramerate,
                    SettingsManager.CAPTURE_FRAMERATE_DEFAULT_VALUDE,
                    (id) => _framerates.ContainsKey(id) ? _framerates[id] : "Unknown",
                    _framerates.Keys,
                    (newId) => SettingsManager.SaveCaptureFramerate(newId),
                    () => SettingsManager.ResetCaptureFramerate()
                );

                SettingsManager.DisplaySettingsComboboxWithReset(
                    grid,
                    "Capture encoder",
                    "Capture encoder.",
                    "Reset to default encoder",
                    SettingsManager.CurrentSettings.CaptureEncoder,
                    SettingsManager.CAPTURE_ENCODER_DEFAULT_VALUE,
                    (id) => _encoders.ContainsKey(id) ? _encoders[id] : "Unknown",
                    _encoders.Keys,
                    (newId) => SettingsManager.SaveCaptureEncoder(newId),
                    () => SettingsManager.ResetCaptureEncoder()
                );

                bool encodedLogo = SettingsManager.CurrentSettings.CaptureEncodedLogo;
                SettingsManager.DisplaySettingsToggleWithReset(
                    grid,
                    "App logo",
                    "If enabled, this option inserts the Flight ReLive logo at the bottom-left corner of the exported video.",
                    "Reset logo displayed state to default value",
                    SettingsManager.CurrentSettings.CaptureEncodedLogo,
                    SettingsManager.CAPTURE_ENCODED_LOGO_DEFAULT_VALUE,
                    (x) => SettingsManager.SaveCaptureEncodedLogo(x),
                    () => SettingsManager.ResetCaptureEncodedLogo()
                );
            }
        }

        #endregion

        #region CALLBACKS
        private void OnCaptureResolutionChanged(int resoluytion)
        {
            int captureResolution = SettingsManager.CurrentSettings.CaptureResolution;

            switch (captureResolution)
            {
                case 0:
                    _width = 1280;
                    _height = 720;
                    break;
                default:
                case 1:
                    _width = 1920;
                    _height = 1080;
                    break;
                case 2:
                    _width = 2560;
                    _height = 1440;
                    break;
                case 3:
                    _width = 3840;
                    _height = 2160;
                    break;
            }
        }

        private void OnCaptureEncoderChanged(int obj)
        {
            _encoder = SettingsManager.CurrentSettings.CaptureEncoder;
        }

        private void OnCaptureFramerateChanged(int obj)
        {
            _framerate = SettingsManager.CurrentSettings.CaptureFramerate;
        }
        #endregion
    }
}
