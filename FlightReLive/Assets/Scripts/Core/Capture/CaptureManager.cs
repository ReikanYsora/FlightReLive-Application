using FlightReLive.Core.Settings;
using FlightReLive.UI;
using Fu;
using Fu.Framework;
using ImGuiNET;
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

        internal static Dictionary<int, string> _encoders = new Dictionary<int, string>()
        {
            { 0, "X264 (Default)" },
            { 1, "NVENC (NVidia)" },
            { 2, "AV1 (NVidia)" }
        };

        internal static Dictionary<int, string> _framerates = new Dictionary<int, string>()
        {
            { 0, "30 FPS" },
            { 1, "60 FPS" }
        };

        [Header("Output Settings")]
        [SerializeField] private string _filePrefix = "video_";

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
        internal void StartCapture()
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

                if (SettingsManager.CurrentSettings.CaptureReplaceBackground)
                {
                    _captureCameraInstance.clearFlags = CameraClearFlags.SolidColor;
                    _captureCameraInstance.backgroundColor = SettingsManager.CurrentSettings.CameraCaptureBackgroundColor;
                }
                else
                {
                    _captureCameraInstance.clearFlags = CameraClearFlags.Skybox;
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
                Fugui.Notify("Critial capture error", "Unable to start capture recording.", StateType.Danger);
                return;
            }

            string ffmpegInput = $"-y -fflags +genpts -use_wallclock_as_timestamps 1 -f rawvideo -pixel_format rgb24 -video_size {_width}x{_height} -i -";
            string ffmpegFilter = "";
            string encoderArgs;

            switch (_encoder)
            {
                default:
                case 0:
                    encoderArgs = "-c:v libx264 -preset ultrafast -b:v 10M";
                    break;
                case 1:
                    encoderArgs = "-c:v h264_nvenc -preset p1 -b:v 10M";
                    break;
                case 2:
                    encoderArgs = "-c:v av1_nvenc -preset p5 -cq 30";
                    break;
            }

            int framerateValue = _framerate == 0 ? 30 : 60;
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
            _writerThread = new Thread(() =>
            {
                while (_writerRunning)
                {
                    byte[] frame = null;

                    lock (_frameQueue)
                    {
                        if (_frameQueue.Count > 0)
                        {
                            frame = _frameQueue.Dequeue();
                        }
                    }

                    if (frame != null)
                    {
                        try
                        {
                            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                            {
                                _ffmpegProcess.StandardInput.BaseStream.Write(frame, 0, frame.Length);
                                _ffmpegProcess.StandardInput.BaseStream.Flush();
                            }
                        }
                        catch (Exception e)
                        {
                            Fugui.Notify("Critical capture error", "Writing error during capture recording :\n" + e.GetBaseException().Message, StateType.Danger);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            });
            _writerThread.Start();
            IsCapturing = true;
            _captureStartTime = DateTime.Now;

            Fugui.Notify("Capture started", $"Capture started ({_width}x{_height}).\nOutput path : {_outputPath}.", StateType.Info);
        }

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

            _renderTexture = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();
            _captureCameraInstance.targetTexture = _renderTexture;
            _flippedTexture = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32);
            _flippedTexture.Create();
        }

        private void PrepareOutputPath()
        {
            string defaultPath = Path.Combine(Application.persistentDataPath, "Captures");
            string folderPath = Path.Combine(defaultPath, SettingsManager.CurrentSettings.CaptureOutputPath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _outputPath = Path.Combine(folderPath, $"{_filePrefix}{timestamp}.mp4");

        }
        private void CaptureFrame()
        {
            Graphics.Blit(_renderTexture, _flippedTexture, _captureFlipVerticalMaterial);
            AsyncGPUReadback.Request(_flippedTexture, 0, TextureFormat.RGB24, OnFrameReadback);
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
            }
        }

        internal void StopCapture()
        {
            IsCapturing = false;
            _captureElapsedTime = "";
            _captureStartTime = DateTime.MinValue;

            try
            {
                _writerRunning = false;

                if (_writerThread != null && _writerThread.IsAlive)
                {
                    _writerThread.Join(500);
                    _writerThread = null;
                }

                _ffmpegProcess?.StandardInput?.Close();
                _ffmpegProcess?.WaitForExit(1000);
                _ffmpegProcess?.Dispose();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Erreur à la fermeture de FFmpeg : {e.Message}");
            }

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

            if (_captureCameraObjectInstance != null)
            {
                Destroy(_captureCameraObjectInstance);
                _captureCameraObjectInstance = null;
                _captureCameraInstance = null;
            }

            lock (_frameQueue)
            {
                _frameQueue.Clear();
            }

            Fugui.Notify("Capture stopped", $"Capture stopped ({_width}x{_height}).\nOutput path : {_outputPath}.", StateType.Info);
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
            using (FuGrid grid = new FuGrid("gridCaptureSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                if (IsCapturing)
                {
                    grid.DisableNextElements();
                }

                grid.SetMinimumLineHeight(22f);
                grid.SetNextElementToolTipWithLabel("Capture output native resolution");

                int currentResolutionId = SettingsManager.CurrentSettings.CaptureResolution;
                string currentLabel = _resolutions.ContainsKey(currentResolutionId) ? _resolutions[currentResolutionId] : "Unknown";

                grid.Combobox("CaptureResolution##CaptureResolutionCombobox", currentLabel, () =>
                {
                    foreach (KeyValuePair<int, string> resolution in _resolutions)
                    {
                        int id = resolution.Key;
                        string label = resolution.Value;
                        bool isSelected = id == currentResolutionId;

                        string display = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {label}";

                        if (ImGui.Selectable(display))
                        {
                            SettingsManager.SaveCaptureResolution(id);
                        }
                    }
                });

                int currentFramerateId = SettingsManager.CurrentSettings.CaptureFramerate;
                string currentFramerateLabel = _framerates.ContainsKey(currentFramerateId) ? _framerates[currentFramerateId] : "Unknown";

                grid.Combobox("CaptureFramerate##CaptureEncoderCombobox", currentFramerateLabel, () =>
                {
                    foreach (KeyValuePair<int, string> framerate in _framerates)
                    {
                        int id = framerate.Key;
                        string label = framerate.Value;
                        bool isSelected = id == currentFramerateId;

                        string display = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {label}";

                        if (ImGui.Selectable(display))
                        {
                            SettingsManager.SaveCaptureFramerate(id);
                        }
                    }
                });

                int currentEncoderId = SettingsManager.CurrentSettings.CaptureEncoder;
                string currentEncoderLabel = _encoders.ContainsKey(currentEncoderId) ? _encoders[currentEncoderId] : "Unknown";

                grid.Combobox("CaptureEncoder##CaptureEncoderCombobox", currentEncoderLabel, () =>
                {
                    foreach (KeyValuePair<int, string> encoder in _encoders)
                    {
                        int id = encoder.Key;
                        string label = encoder.Value;
                        bool isSelected = id == currentEncoderId;

                        string display = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {label}";

                        if (ImGui.Selectable(display))
                        {
                            SettingsManager.SaveCaptureEncoder(id);
                        }
                    }
                });

                grid.SetNextElementToolTipWithLabel("Capture output path");
                string captureOutputPath = SettingsManager.CurrentSettings.CaptureOutputPath;

                grid.InputFolder("Capture output path", (path) =>
                {
                    SettingsManager.SaveCaptureOutputPath(path);
                }, captureOutputPath, new ExtensionFilter[0]);

                bool encodedLogo = SettingsManager.CurrentSettings.CaptureEncodedLogo;

                grid.SetNextElementToolTipWithLabel("If enabled, this option inserts the Flight ReLive logo at the bottom-left corner of the exported video.");
                if (grid.Toggle("App Logo", ref encodedLogo))
                {
                    SettingsManager.SaveCaptureEncodedLogo(encodedLogo);
                }

                bool captureReplaceBackground = SettingsManager.CurrentSettings.CaptureReplaceBackground;

                grid.SetNextElementToolTipWithLabel("Replace background (sky) by a specific background color");
                if (grid.Toggle("Replace background", ref captureReplaceBackground))
                {
                    SettingsManager.SaveCaptureReplaceBackground(captureReplaceBackground);
                }

                if (!captureReplaceBackground)
                {
                    grid.DisableNextElement();
                }

                grid.SetNextElementToolTipWithLabel("Custom background color for capture");
                Vector4 captureBackgroundColor = (Vector4)SettingsManager.CurrentSettings.CameraCaptureBackgroundColor;
                if (grid.ColorPicker("Capture background", ref captureBackgroundColor))
                {
                    SettingsManager.SaveCameraCaptureBackgroundColor(captureBackgroundColor);
                }
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
