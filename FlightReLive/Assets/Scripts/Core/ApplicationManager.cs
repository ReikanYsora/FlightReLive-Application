using FlightReLive.Core.Cache;
using FlightReLive.Core.Database;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Version;
using FlightReLive.UI;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace FlightReLive.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Welcome")]
        [SerializeField] private Texture2D _welcome;

        [Header("Cameras & upscalers settings")]
        [SerializeField] private Camera _reliveCamera;
        [SerializeField] private Camera _povCamera;

#if UNITY_STANDALONE_WIN
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const int SM_CYFULLSCREEN = 17;
#endif
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

            //Initialize database
            DatabaseManager.Initialize();
        }

        private void Start()
        {
            //Save current version
            SettingsManager.SaveCurrentVersion(Application.version);

            //Initialize cache
            CacheManager.Initialize();

            //Apply Fugui global scale
            ApplySavedGlobalScale();

            //Register events
            SettingsManager.OnGlobalScaleChanged += OnGlobalScaleChanged;
            SettingsManager.OnApplicationTargetFPSChanged += OnApplicationTargetFPSChanged;

            //Check if welcome panel need do be displayed
            bool displayWizard = SettingsManager.CurrentSettings.DisplayWizard;
            bool displayWelcomePanel = CheckIfDisplayWelcomePanelNeedToBeDisplayed();

            if (displayWizard)
            {
                //Display wizard (first start))
                DisplayUIScaleSettings();
            }
            else if (displayWelcomePanel)
            {
                //Display welcome panel
                DisplayWelcomePanel();
            }

            if (!Application.isEditor)
            {
                //Check latest version
                CheckLastVersion();
            }
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
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Check if the welcome panel need to be displayed
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Check the latest version available and notify the user if a newer version is available.
        /// </summary>
        private async void CheckLastVersion()
        {
            AppVersionDTO latestVersion = await VersionService.GetLatestVersionAsync();

            if (latestVersion == null)
            {
                Debug.LogWarning("Unable to retrieve the latest version.");
                return;
            }

            string localVersion = Application.version;
            string remoteVersion = latestVersion.GetFullVersion();

            if (IsRemoteVersionNewer(localVersion, remoteVersion))
            {
                Fugui.Notify("Update Available", $"A newer version of Flight ReLive is available for your system ({latestVersion.DisplayName}).\nWe recommend updating to enjoy the latest improvements and features.", StateType.Info, 5f);
            }
        }

        /// <summary>
        /// Check if the remote version is newer than the local version.
        /// </summary>
        /// <param name="localVersion"></param>
        /// <param name="remoteVersion"></param>
        /// <returns></returns>
        private bool IsRemoteVersionNewer(string localVersion, string remoteVersion)
        {
            System.Version local = new System.Version(localVersion);
            System.Version remote = new System.Version(remoteVersion);

            return remote > local;
        }

        /// <summary>
        /// Quit the application.
        /// </summary>
        internal void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Set the native screen resolution in windowed mode, excluding taskbar (Windows only).
        /// </summary>
        private static void SetNativeResolutionSafe()
        {
            int width = 0;
            int height = 0;

#if UNITY_STANDALONE_WIN
            // Use native Windows API to get usable screen area (excluding taskbar)
            width = GetSystemMetrics(SM_CXSCREEN);
            int fullHeight = GetSystemMetrics(SM_CYFULLSCREEN);
            int totalHeight = GetSystemMetrics(SM_CYSCREEN);
            int taskbarHeight = totalHeight - fullHeight;
            height = totalHeight - taskbarHeight;
#else
            // Fallback for macOS or other platforms
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
#endif

            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, false);
        }

        /// <summary>
        /// Apply the saved global scale to the Fugui context.
        /// </summary>
        private void ApplySavedGlobalScale()
        {
            float scale = SettingsManager.CurrentSettings.GlobalScale;
            Fugui.SetScale(scale, scale);
        }
        #endregion

        #region UI
        /// <summary>
        /// Display the UI scale settings wizard (first start).
        /// </summary>
        private void DisplayUIScaleSettings()
        {
            float paddingX = 10f;

            Fugui.ShowModal(" ", (layout) =>
            {
                layout.BeginGroup();
                layout.Image("Global settings", _welcome, new FuElementSize(496, 195), true, false);

                ImGui.Indent(10f);
                //Title
                Fugui.PushFont(16, FontType.Bold);
                string title = "Welcome to Flight ReLive. Your journey starts here.";
                layout.CenterNextItemH(title);
                layout.Text(title);
                Fugui.PopFont();
                layout.Spacing();

                //Introduction

                Fugui.PushFont(16, FontType.Regular);
                string message = "Before you take off, we recommend adjusting a few key settings to ensure the smoothest and most immersive experience. It only takes a moment, and it makes all the difference.";
                layout.CenterNextItemH(message);
                layout.Text(message, FuTextWrapping.Wrap);
                Fugui.PopFont();
                ImGui.Unindent(10);

                Fugui.PushFont(14, FontType.Regular);
                layout.Spacing();
                layout.Separator();
                layout.Spacing();
                using (FuGrid uiGrid = new FuGrid("wizardUiGrid", new FuGridDefinition(2, new float[] { 0.4f, 0.6f }), FuGridFlag.Default, 2, 2, paddingX))
                {
                    uiGrid.SetNextElementToolTipWithLabel("Global UI scale. You can always change this setting later via the ‘Preferences’ menu.");
                    uiGrid.Combobox("Global UI Scale##UIScaleCombobox", (int)(Fugui.DefaultContext.Scale * 100f) + "%", () =>
                    {
                        foreach (float scale in SettingsManager.AvailableUIScale)
                        {
                            if (ImGui.Selectable((scale == Fugui.DefaultContext.Scale ? FlightReLiveIcons.Check : " ") + "  " + scale * 100f + "%"))
                            {
                                SettingsManager.SaveGlobalScale(scale);
                            }
                        }
                    });
                }

                layout.Spacing();
                layout.Separator();
                layout.Spacing();

                using (FuGrid apiGrid = new FuGrid("wizardApiGrid", new FuGridDefinition(2, new float[] { 0.4f, 0.6f }), FuGridFlag.Default, 2, 2, paddingX))
                {

                    string mapTilerAPIKey = SettingsManager.CurrentSettings.MapTilerAPIKey;
                    apiGrid.SetNextElementToolTipWithLabel("MapTiler API key required for downloading satellite, topographic, buildings, hillshade images.\nA MapTiler account is required (free for less than 100,000 tile downloads per month).");

                    if (apiGrid.TextInput("MapTiler API key", ref mapTilerAPIKey, flags: FuInputTextFlags.Password))
                    {
                        SettingsManager.SaveMapTilerApiKey(mapTilerAPIKey);
                    }
                    apiGrid.NextColumn();
                    apiGrid.TextURL("Follow this link to create a free MapTiler API Account", "https://www.maptiler.com/", FuTextWrapping.Clip);
                }

                layout.Spacing();
                layout.Separator();
                layout.Spacing();

                using (FuGrid timeZoneGrid = new FuGrid("timeZoneGrid", new FuGridDefinition(2, new float[] { 0.4f, 0.6f }), FuGridFlag.Default, 2, 2, paddingX))
                {
                    timeZoneGrid.SetNextElementToolTipWithLabel("The time zone is used to accurately calculate the lighting and position of the sun in the scene.");

                    TimeZoneInfo currentTz = SettingsManager.CurrentSettings.UserTimeZone;
                    string currentTzId = currentTz.Id;
                    string comboLabel = currentTz.DisplayName.StartsWith("(UTC") ? currentTz.DisplayName : $"(UTC{SettingsManager.FormatUtcOffset(currentTz.BaseUtcOffset)}) {currentTz.DisplayName}";

                    timeZoneGrid.Combobox("TimeZone##TZCombobox", comboLabel, () =>
                    {
                        foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
                        {
                            bool isSelected = tz.Id == currentTzId;

                            string label = tz.DisplayName.StartsWith("(UTC")
                                ? $"{(isSelected ? FlightReLiveIcons.Check : " ")} {tz.DisplayName}"
                                : $"{(isSelected ? FlightReLiveIcons.Check : " ")} (UTC{SettingsManager.FormatUtcOffset(tz.BaseUtcOffset)}) {tz.DisplayName}";

                            if (ImGui.Selectable(label))
                            {
                                SettingsManager.SaveTimeZone(tz);
                            }
                        }
                    });

                    timeZoneGrid.SetNextElementToolTipWithLabel("Choose how dates are displayed throughout the application.");

                    DateFormatStyle currentFormat = SettingsManager.CurrentSettings.DateFormatStyle;
                    string formatLabel = SettingsManager.GetDateFormatLabel(currentFormat);

                    timeZoneGrid.Combobox("DateFormat##DateFormatCombobox", formatLabel, () =>
                    {
                        foreach (DateFormatStyle format in Enum.GetValues(typeof(DateFormatStyle)))
                        {
                            bool isSelected = format == currentFormat;
                            string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {SettingsManager.GetDateFormatLabel(format)}";

                            if (ImGui.Selectable(label))
                            {
                                SettingsManager.SaveDateFormatStyle(format);
                            }
                        }
                    });

                    timeZoneGrid.SetNextElementToolTipWithLabel("Choose between 12-hour or 24-hour time format.");

                    TimeFormatStyle currentTimeFormat = SettingsManager.CurrentSettings.TimeFormatStyle;
                    string timeFormatLabel = SettingsManager.GetTimeFormatLabel(currentTimeFormat);

                    timeZoneGrid.Combobox("TimeFormat##TimeFormatCombobox", timeFormatLabel, () =>
                    {
                        foreach (TimeFormatStyle format in Enum.GetValues(typeof(TimeFormatStyle)))
                        {
                            bool isSelected = format == currentTimeFormat;
                            string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {SettingsManager.GetTimeFormatLabel(format)}";

                            if (ImGui.Selectable(label))
                            {
                                SettingsManager.SaveTimeFormatStyle(format);
                            }
                        }
                    });

                    timeZoneGrid.SetNextElementToolTipWithLabel("Select your preferred unit system for altitude and speed display.");

                    UnitSystemType currentUnitSystem = SettingsManager.CurrentSettings.UnitSystemType;
                    string unitSystemLabel = SettingsManager.GetUnitSystemLabel(currentUnitSystem);

                    timeZoneGrid.Combobox("UnitSystem##UnitSystemCombobox", unitSystemLabel, () =>
                    {
                        foreach (UnitSystemType system in Enum.GetValues(typeof(UnitSystemType)))
                        {
                            bool isSelected = system == currentUnitSystem;
                            string label = $"{(isSelected ? FlightReLiveIcons.Check : " ")} {SettingsManager.GetUnitSystemLabel(system)}";

                            if (ImGui.Selectable(label))
                            {
                                SettingsManager.SaveUnitSystemType(system);
                            }
                        }
                    });
                }

                Fugui.PopFont();
                layout.EndGroup();

            }, FuModalSize.Medium, new FuModalButton("OK", () => { SettingsManager.SaveDisplayWizard(false); }, FuButtonStyle.Highlight));

        }

        /// <summary>
        /// Display the welcome panel.
        /// </summary>
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
        /// <summary>
        /// Apply the new global scale to the Fugui context.
        /// </summary>
        /// <param name="scale"></param>
        private void OnGlobalScaleChanged(float scale)
        {
            ApplySavedGlobalScale();
        }

        /// <summary>
        /// Apply the new target FPS to the application.
        /// </summary>
        /// <param name="value"></param>
        private void OnApplicationTargetFPSChanged(int value)
        {
            Application.targetFrameRate = SettingsManager.CurrentSettings.ApplicationTargetFPS;
        }
        #endregion
    }
}
