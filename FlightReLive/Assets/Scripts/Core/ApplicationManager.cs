using FlightReLive.Core.Cache;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Version;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlightReLive.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Camera Settings")]
        [SerializeField] private Camera _camera;

        [Header("Welcome")]
        [SerializeField] private Texture2D _welcome;

        [Header("PostProcess Settings")]
        [SerializeField] private Volume _volume;

        [Header("Outline")]
        [SerializeField] private ScriptableRendererFeature _edgeFeature;
        private DepthOfField _depthOfField;
        private Vignette _vignette;
        #endregion

        #region PROPERTIES
        public static ApplicationManager Instance { get; private set; }
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

            //Set resolution, theme 
            SetNativeResolutionSafe();

            FuTheme flightReLiveTheme;
            if (Fugui.Themes.LoadTheme("Flight ReLive", out flightReLiveTheme))
            {
                Fugui.Themes.SetTheme(flightReLiveTheme);
            }

            //Initialize application settings
            SettingsManager.LoadAll();
        }

        private void Start()
        {
            //Initialize cache
            CacheManager.Initialize();

            //Apply Fugui global scale
            ApplySavedGlobalScale();

            //Apply hardware quality settings
            ApplyUnityQualityPreset(SettingsManager.CurrentSettings.HardwareQualityPreset);

            //Load post-processing values
            LoadPostProcessingValues();

            //Register events
            SettingsManager.OnHardwareQualityPresetChanged += OnHardwareQualityPresetChanged;
            SettingsManager.OnGlobalScaleChanged += OnGlobalScaleChanged;
            SettingsManager.OnVignettingIntensityChanged += OnVignettingIntensityChanged;
            SettingsManager.OnOutlineVisibilityChanged += OnOutlineVisibilityChanged;
            SettingsManager.OnDepthOfFieldEnabledChanged += OnDepthOfFieldEnabledChanged;
            SettingsManager.OnDepthOfFieldStartChanged += OnDepthOfFieldStartChanged;
            SettingsManager.OnDepthOfFieldEndChanged += OnDepthOfFieldEndChanged;
            SettingsManager.OnApplicationTargetFPSChanged += OnApplicationTargetFPSChanged;

            //Check if welcome panel need do be displayed
            bool displayWelcomePanel = CheckIfDisplayWelcomePanelNeedToBeDisplayed();

            if (displayWelcomePanel)
            {
                //Display welcome panel
                DisplayWelcomePanel();
            }

            //Check latest version
            CheckLastVersion();
        }

        private void Update()
        {
            //Main thread dispatcher
            UnityMainThreadDispatcher.ManageThreads();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Application.targetFrameRate = SettingsManager.CurrentSettings.ApplicationTargetFPS;
            }
            else
            {
                Application.targetFrameRate = SettingsManager.CurrentSettings.ApplicationIdleFPS;
            }
        }

        private void OnDestroy()
        {
            //Unregister events
            SettingsManager.OnHardwareQualityPresetChanged -= OnHardwareQualityPresetChanged;
            SettingsManager.OnGlobalScaleChanged -= OnGlobalScaleChanged;
            SettingsManager.OnVignettingIntensityChanged -= OnVignettingIntensityChanged;
            SettingsManager.OnOutlineVisibilityChanged -= OnOutlineVisibilityChanged;
            SettingsManager.OnDepthOfFieldEnabledChanged -= OnDepthOfFieldEnabledChanged;
            SettingsManager.OnDepthOfFieldStartChanged -= OnDepthOfFieldStartChanged;
            SettingsManager.OnDepthOfFieldEndChanged -= OnDepthOfFieldEndChanged;
            SettingsManager.OnApplicationTargetFPSChanged -= OnApplicationTargetFPSChanged;
        }
        #endregion

        #region METHODS
        private bool CheckIfDisplayWelcomePanelNeedToBeDisplayed()
        {
            bool displayWelcome = false;

            if (Application.version != SettingsManager.CurrentSettings.CurrentVersion)
            {
                displayWelcome = true;
                SettingsManager.SaveDontAskWelcomeVersion(false);
            }
            else if (!SettingsManager.CurrentSettings.DontAskWelcomeVersion)
            {
                displayWelcome = true;
            }

            return displayWelcome;
        }

        private async void CheckLastVersion()
        {
            AppVersionDTO latestVersion = await VersionService.GetLatestVersionAsync();

            if (latestVersion == null)
            {
                Debug.LogWarning("Impossible de récupérer la dernière version.");
                return;
            }

            string localVersion = Application.version;
            string remoteVersion = latestVersion.GetFullVersion();

            if (localVersion != remoteVersion)
            {
                Fugui.Notify("Update Available", $"A newer version of Flight ReLive is available for your system ({latestVersion.DisplayName}).\nWe recommend updating to enjoy the latest improvements and features.", StateType.Info);
            }
        }

        private void LoadPostProcessingValues()
        {
            //Apply saved post-processing values
            if (_volume != null && _volume.profile != null && _volume.profile.TryGet<Vignette>(out Vignette vignette))
            {
                _vignette = vignette;
            }

            if (_volume != null && _volume.profile != null && _volume.profile.TryGet<DepthOfField>(out DepthOfField depthOfField))
            {
                _depthOfField = depthOfField;
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = SettingsManager.CurrentSettings.VignettingIntensity;
                _vignette.active = true;
            }

            if (_depthOfField != null)
            {
                if (SettingsManager.CurrentSettings.DepthOfFieldEnabled)
                {
                    _depthOfField.mode.value = DepthOfFieldMode.Gaussian;
                }
                else
                {
                    _depthOfField.mode.value = DepthOfFieldMode.Off;
                }

                _depthOfField.gaussianStart.value = SettingsManager.CurrentSettings.DepthOfFieldStart;
                _depthOfField.gaussianEnd.value = SettingsManager.CurrentSettings.DepthOfFieldEnd;
            }

            _edgeFeature.SetActive(SettingsManager.CurrentSettings.OutlineVisibility);
        }

        internal void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SetNativeResolutionSafe()
        {
            int width = 0;
            int height = 0;
            Screen.fullScreenMode = FullScreenMode.Windowed;

            if (Display.main != null && Display.main.systemWidth > 0 && Display.main.systemHeight > 0)
            {
                width = Display.main.systemWidth;
                height = Display.main.systemHeight;
            }

            if ((width == 0 || height == 0) && Screen.currentResolution.width > 0 && Screen.currentResolution.height > 0)
            {
                width = Screen.currentResolution.width;
                height = Screen.currentResolution.height;
            }

            if ((width == 0 || height == 0) && Screen.width > 0 && Screen.height > 0)
            {
                width = Screen.width;
                height = Screen.height;
            }

            if (width == 0 || height == 0)
            {
                width = 1920;
                height = 1080;
            }

            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, false);
        }

        private void ApplySavedGlobalScale()
        {
            float scale = SettingsManager.CurrentSettings.GlobalScale;
            Fugui.SetScale(scale, scale);
        }

        private int GetUnityQualityIndex(QualityPreset preset)
        {
            switch (preset)
            {
                default:
                case QualityPreset.Quality:
                    return QualitySettings.names.ToList().FindIndex(q => q.Equals("Quality", StringComparison.OrdinalIgnoreCase));
                case QualityPreset.Balanced:
                    return QualitySettings.names.ToList().FindIndex(q => q.Equals("Balanced", StringComparison.OrdinalIgnoreCase));
                case QualityPreset.Performance:
                    return QualitySettings.names.ToList().FindIndex(q => q.Equals("Performance", StringComparison.OrdinalIgnoreCase));
            }
        }

        private void ApplyUnityQualityPreset(QualityPreset preset)
        {
            int index = GetUnityQualityIndex(preset);

            if (index >= 0 && index < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(index, true);
            }
        }

        internal void DisablePostProcessing()
        {
            _camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        internal void EnablePostProcessing()
        {
            _camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
        #endregion

        #region UI
        private void DisplayWelcomePanel()
        {
            Fugui.ShowModal(" ", (layout) =>
            {
                layout.BeginGroup();

                if (layout.Image("welcome", _welcome, new FuElementSize(496, 195), true, true))
                {

                }

                ImGui.Indent(10f);
                Fugui.PushFont(18, FontType.Bold);
                layout.CenterNextItem("Flight ReLive is 100% free.");
                layout.Text("Flight ReLive is 100% free.");
                Fugui.PopFont();

                Fugui.PushFont(16, FontType.Regular);
                layout.Spacing();
                layout.Text("This app was designed with passion to allow everyone to relive their flights, explore their GPS data, and visualize their trajectories like never before.\nNo ads.No subscription.Just a smooth, accurate, and immersive experience—accessible to all.", FuTextWrapping.Wrap);
                layout.Spacing();
                layout.Text("But behind this freedom lies a server, hardware, licenses, and hundreds of hours of development.", FuTextWrapping.Wrap);
                layout.Spacing();
                layout.Text("If Flight ReLive helps, inspires, or accompanies you in your aerial adventures, you can support its development by making a donation. Every contribution, no matter how small, helps keep the project alive and independent.", FuTextWrapping.Wrap);
                layout.Spacing();
                Fugui.PopFont();

                Fugui.PushFont(16, FontType.Italic);
                layout.CenterNextItem("Make a donation — so Flight ReLive can continue to fly freely.");
                layout.Text("Make a donation — so Flight ReLive can continue to fly freely.", FuTextWrapping.Wrap);
                Fugui.PopFont();
                layout.Spacing();
                Fugui.PushFont(16, FontType.Bold);
                layout.CenterNextItem("Support Flight ReLive on Tipee !");
                layout.TextURL("Support Flight ReLive on Tipee !", "https://fr.tipeee.com/flight-relive/", FuTextWrapping.Wrap);
                layout.Spacing();
                layout.CenterNextItem("Thank you for being here. And happy reliving.");
                layout.Text("Thank you for being here. And happy reliving.");
                Fugui.PopFont();
                ImGui.Unindent(10);
                layout.Separator();
                layout.Spacing();
                ImGui.Indent(10);
                Fugui.PushFont(14, FontType.Italic);
                bool dontAskForThisVersion = SettingsManager.CurrentSettings.DontAskWelcomeVersion;
                if (layout.CheckBox("askCheckbox", ref dontAskForThisVersion))
                {
                    SettingsManager.SaveDontAskWelcomeVersion(true);
                }
                layout.SameLine();
                layout.Text(" Don't ask me again for this version");
                Fugui.PopFont();
                ImGui.Unindent(10);
                layout.EndGroup();

            }, FuModalSize.Medium, new FuModalButton("I understand", null, FuButtonStyle.Highlight, FuKeysCode.Enter));
        }
        #endregion

        #region CALLBACKS
        private void OnHardwareQualityPresetChanged(QualityPreset qualityPreset)
        {
            ApplyUnityQualityPreset(qualityPreset);
        }

        private void OnGlobalScaleChanged(float scale)
        {
            ApplySavedGlobalScale();
        }

        internal void DrawPostProcessingSettings(FuLayout layout)
        {
            using (FuGrid grid = new FuGrid("gridPostProcessSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding: 10))
            {
                float vignettingIntensity = SettingsManager.CurrentSettings.VignettingIntensity;
                if (grid.Slider("Vignetting", ref vignettingIntensity, 0f, 1f, 0.01f))
                {
                    SettingsManager.SaveVignettingIntensity(vignettingIntensity);
                }

                bool depthOfFieldEnabled = SettingsManager.CurrentSettings.DepthOfFieldEnabled;

                if (grid.Toggle("Depth of Field", ref depthOfFieldEnabled))
                {
                    SettingsManager.SaveDepthOfFieldEnabled(depthOfFieldEnabled);
                }

                if (!depthOfFieldEnabled)
                {
                    grid.DisableNextElements();
                }

                float depthOfFieldStart = SettingsManager.CurrentSettings.DepthOfFieldStart;
                if (grid.Slider("DoF Start", ref depthOfFieldStart, 10f, 200f, 0.1f))
                {
                    SettingsManager.SaveDepthOfFieldStart(depthOfFieldStart);
                }

                float depthOfFieldEnd = SettingsManager.CurrentSettings.DepthOfFieldEnd;
                if (grid.Slider("DoF End", ref depthOfFieldEnd, 200f, 500f, 0.1f))
                {
                    SettingsManager.SaveDepthOfFieldEnd(depthOfFieldEnd);
                }

                grid.EnableNextElements();
                bool outlineEnabled = SettingsManager.CurrentSettings.OutlineVisibility;
                if (grid.Toggle("Display Outline", ref outlineEnabled))
                {
                    SettingsManager.SaveOutlineVisibility(outlineEnabled);
                }
            }
        }

        private void OnVignettingIntensityChanged(float intensity)
        {
            if (_vignette != null)
            {
                _vignette.intensity.value = intensity;
                _vignette.active = true;
            }
        }

        private void OnOutlineVisibilityChanged(bool status)
        {
            if (_edgeFeature != null)
            {
                _edgeFeature.SetActive(status);
            }
        }

        private void OnDepthOfFieldEnabledChanged(bool enabled)
        {
            if (SettingsManager.CurrentSettings.DepthOfFieldEnabled)
            {
                _depthOfField.mode.value = DepthOfFieldMode.Gaussian;
            }
            else
            {
                _depthOfField.mode.value = DepthOfFieldMode.Off;
            }
        }

        private void OnDepthOfFieldStartChanged(float start)
        {
            if (_depthOfField != null)
            {
                _depthOfField.mode.value = DepthOfFieldMode.Gaussian;
                _depthOfField.gaussianStart.value = SettingsManager.CurrentSettings.DepthOfFieldStart;
            }
        }

        private void OnDepthOfFieldEndChanged(float end)
        {
            if (_depthOfField != null)
            {
                _depthOfField.mode.value = DepthOfFieldMode.Gaussian;
                _depthOfField.gaussianEnd.value = SettingsManager.CurrentSettings.DepthOfFieldEnd;

            }
        }

        private void OnApplicationTargetFPSChanged(int value)
        {
            Application.targetFrameRate = SettingsManager.CurrentSettings.ApplicationTargetFPS;
        }
        #endregion
    }
}
