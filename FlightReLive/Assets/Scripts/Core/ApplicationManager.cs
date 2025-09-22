using FlightReLive.Core.Cache;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Version;
using Fu;
using Fu.Framework;
using ImGuiNET;
using TND.Upscaling.Framework;
using UnityEngine;

namespace FlightReLive.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Welcome")]
        [SerializeField] private Texture2D _welcome;


        [Header("System settings")]
        [SerializeField] private TNDUpscaler _reliveUpscaler;
        [SerializeField] private TNDUpscaler _povCameraUpscaler;
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

            //Apply upscaler settings
            ApplyUpscalersSettingsValues();

            //Register events
            SettingsManager.OnGlobalScaleChanged += OnGlobalScaleChanged;
            SettingsManager.OnApplicationTargetFPSChanged += OnApplicationTargetFPSChanged;
            SettingsManager.OnUpscalerNameChanged += OnUpscalerNameChanged;
            SettingsManager.OnUpscalerQualityChanged += OnUpscalerQualityChanged;
            SettingsManager.OnUpscalerSharpeningEnabledChanged += OnUpscalerSharpeningEnabledChanged;
            SettingsManager.OnUpscalerSharpenessChanged += OnUpscalerSharpenessChanged;

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
            SettingsManager.OnGlobalScaleChanged -= OnGlobalScaleChanged;
            SettingsManager.OnApplicationTargetFPSChanged -= OnApplicationTargetFPSChanged;
            SettingsManager.OnUpscalerNameChanged -= OnUpscalerNameChanged;
            SettingsManager.OnUpscalerQualityChanged -= OnUpscalerQualityChanged;
            SettingsManager.OnUpscalerSharpeningEnabledChanged -= OnUpscalerSharpeningEnabledChanged;
            SettingsManager.OnUpscalerSharpenessChanged -= OnUpscalerSharpenessChanged;
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

        private void ApplyUpscalersSettingsValues()
        {
#if UNITY_EDITOR
            _reliveUpscaler.runInEditMode = true;
            _povCameraUpscaler.runInEditMode = true;
#endif
            if (SettingsManager.CurrentSettings.UpscalerName == UpscalerName.None)
            {
                UpscalerName mainCameraEnabledUpscaler = _reliveUpscaler.GetActiveUpscaler();

                _reliveUpscaler.SetQuality(UpscalerQuality.Off);
                _povCameraUpscaler.SetQuality(UpscalerQuality.Off);
                _reliveUpscaler.ResetCamera();
                _povCameraUpscaler.ResetCamera();
            }
            else
            {
                _reliveUpscaler.SetUpscaler(SettingsManager.CurrentSettings.UpscalerName);
                _reliveUpscaler.SetQuality(SettingsManager.CurrentSettings.UpscalerQuality);
                _reliveUpscaler.SetSharpening(SettingsManager.CurrentSettings.UpscalerSharpeningEnabled);
                _reliveUpscaler.SetSharpness(SettingsManager.CurrentSettings.UpscalerSharpeness);
                _reliveUpscaler.SetAutoReactive(true);
                _povCameraUpscaler.SetUpscaler(SettingsManager.CurrentSettings.UpscalerName);
                _povCameraUpscaler.SetQuality(SettingsManager.CurrentSettings.UpscalerQuality);
                _povCameraUpscaler.SetSharpening(SettingsManager.CurrentSettings.UpscalerSharpeningEnabled);
                _povCameraUpscaler.SetSharpness(SettingsManager.CurrentSettings.UpscalerSharpeness);
                _povCameraUpscaler.SetAutoReactive(true);
            }
        }
        #endregion

        #region UI
        private void DisplayWelcomePanel()
        {
            Fugui.ShowModal(" ", (layout) =>
            {
                layout.BeginGroup();
                layout.Image("welcome", _welcome, new FuElementSize(496, 195), true, false);

                ImGui.Indent(10f);
                Fugui.PushFont(18, FontType.Bold);
                layout.CenterNextItemH("Flight ReLive is 100% free.");
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
                layout.CenterNextItemH("Make a donation — so Flight ReLive can continue to fly freely.");
                layout.Text("Make a donation — so Flight ReLive can continue to fly freely.", FuTextWrapping.Wrap);
                Fugui.PopFont();
                layout.Spacing();
                Fugui.PushFont(16, FontType.Bold);
                layout.CenterNextItemH("Support Flight ReLive on Tipee !");
                layout.TextURL("Support Flight ReLive on Tipee !", "https://fr.tipeee.com/flight-relive/", FuTextWrapping.Wrap);
                layout.Spacing();
                layout.CenterNextItemH("Thank you for being here. And happy reliving.");
                layout.Text("Thank you for being here. And happy reliving.");
                Fugui.PopFont();
                ImGui.Unindent(10);
                layout.Separator();
                layout.Spacing();
                ImGui.Indent(10);
                Fugui.PushFont(14, FontType.Italic);
                bool dontAskForThisVersion = SettingsManager.CurrentSettings.DontAskWelcomeVersion;
                if (layout.CheckBox("##askForDisplay", ref dontAskForThisVersion))
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
        private void OnGlobalScaleChanged(float scale)
        {
            ApplySavedGlobalScale();
        }

        private void OnApplicationTargetFPSChanged(int value)
        {
            Application.targetFrameRate = SettingsManager.CurrentSettings.ApplicationTargetFPS;
        }

        private void OnUpscalerNameChanged(UpscalerName upscalerName)
        {
            ApplyUpscalersSettingsValues();
        }

        private void OnUpscalerQualityChanged(UpscalerQuality upscalerQuality)
        {
            ApplyUpscalersSettingsValues();
        }

        private void OnUpscalerSharpenessChanged(float enabled)
        {
            ApplyUpscalersSettingsValues();
        }

        private void OnUpscalerSharpeningEnabledChanged(bool value)
        {
            ApplyUpscalersSettingsValues();
        }
        #endregion
    }
}
