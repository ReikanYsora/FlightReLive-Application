using FlightReLive.Core;
using FlightReLive.Core.Cache;
using FlightReLive.Core.Platform;
using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System.Diagnostics;
using UnityEngine;

namespace FlightReLive.UI.Layout
{
    public class LayoutManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Texture2D _tipeee;
        private bool _aboutOpened = false;
        #endregion

        #region UNITY METHODS
        private void Start()
        {
            Fugui.Layouts.SetLayout("Default");
            RegisterMainMenuItems();
        }
        #endregion

        #region METHODS

        /// <summary>
        /// Register main menu items
        /// </summary>
        private void RegisterMainMenuItems()
        {
#if UNITY_STANDALONE_OSX
            RegisterMainMenuItemsMacOs();
#else
            RegisterMainMenuItemsWindows();
#endif
        }

        private void RegisterMainMenuItemsMacOs()
        {
            Fugui.DisableMainMenu();

            MacOsMainMenuManager.AddQuitMenuEntry("Flight ReLive", () =>
            {
                ApplicationManager.Instance.QuitApplication();
            });

#if UNITY_EDITOR
            //Fugui Settings menu (only in editor)
            MacOsMainMenuManager.AddMenuEntry("Settings", "Fugui Settings", () =>
            {
                Fugui.CreateWindowAsync(FuSystemWindowsNames.FuguiSettings, null);
            });
#endif
            //Settings menu
            MacOsMainMenuManager.AddMenuEntry("Settings", "Preferences", () =>
            {
                SettingsManager.ShowPreferencesModal();
            }, "P");

            MacOsMainMenuManager.AddSeparator("Settings");

            MacOsMainMenuManager.AddMenuEntry("Settings", "Clear local cache", () =>
            {
                CacheManager.ClearCache();
            });

            MacOsMainMenuManager.AddMenuEntry("Settings", "Reset preferences", () =>
            {
                SettingsManager.LoadDefaultSettings();
            });

            //Windows menu
            foreach (FuWindowName windowName in FlightReLiveWindowsNames.GetAllWindowsNames())
            {
                string displayName = windowName.Name;

                // Remove leading private-use unicode icon if present
                if (!string.IsNullOrEmpty(displayName) && displayName[0] >= '\uE000' && displayName[0] <= '\uF8FF')
                {
                    // Remove icon + following space if present
                    displayName = displayName.Length > 1 && displayName[1] == ' '
                        ? displayName.Substring(2).Trim()
                        : displayName.Substring(1).Trim();
                }

                // Determine shortcut: first alphabetic character of display name
                char? shortcut = null;
                foreach (char c in displayName)
                {
                    if (char.IsLetter(c))
                    {
                        shortcut = char.ToUpper(c);
                        break;
                    }
                }

                MacOsMainMenuManager.AddMenuEntry(
                    "Windows",
                    displayName,
                    () =>
                    {
                        // Use full FuWindowName (with icon) for window creation
                        Fugui.CreateWindowAsync(windowName, null);
                    },
                    shortcut?.ToString() ?? "" // pass shortcut, or empty string if none
                );
            }

            //Help menu
            MacOsMainMenuManager.AddMenuEntry("Help", "About", () =>
            {
                ShowAboutModal();
            }, "H");
        }

        private void RegisterMainMenuItemsWindows()
        {
            string flightReLiveTitle = "Flight ReLive";
            string flightReLiveSettings = "Settings";

            //"Flight ReLive" menu
            Fugui.RegisterMainMenuItem(flightReLiveTitle, null);
            Fugui.RegisterMainMenuItem(FlightReLiveIcons.Quit + "  Exit", () => { ApplicationManager.Instance.QuitApplication(); }, flightReLiveTitle);

            //Settings menu
            Fugui.RegisterMainMenuItem(flightReLiveSettings, null);
#if UNITY_EDITOR
            Fugui.RegisterMainMenuItem(FlightReLiveIcons.Preferences + "  Fugui Settings", () => Fugui.CreateWindowAsync(FuSystemWindowsNames.FuguiSettings, null), flightReLiveSettings);
#endif
            Fugui.RegisterMainMenuItem(FlightReLiveIcons.Preferences + "  Preferences", () =>
            {
                SettingsManager.ShowPreferencesModal();
            }, flightReLiveSettings);
            Fugui.RegisterMainMenuSeparator(flightReLiveSettings);
            Fugui.RegisterMainMenuItem("Clear local cache", () =>
            {
                CacheManager.ClearCache();
            }, flightReLiveSettings);
            Fugui.RegisterMainMenuItem("Reset preferences", () =>
            {
                SettingsManager.LoadDefaultSettings();
            }, flightReLiveSettings);

            //"Windows" menu
            Fugui.RegisterMainMenuItem("Windows", null);

            foreach (FuWindowName windowName in FlightReLiveWindowsNames.GetAllWindowsNames())
            {
                Fugui.RegisterMainMenuItem(windowName.ToString(), () => Fugui.CreateWindowAsync(windowName, null), "Windows");
            }

            //"Help" menu
            Fugui.RegisterMainMenuItem("Help", null);
            Fugui.RegisterMainMenuItem(FlightReLiveIcons.About + "  About", ShowAboutModal, "Help");
        }

        private void ShowAboutModal()
        {
            if (_aboutOpened)
            {
                return;
            }

            _aboutOpened = true;
            Fugui.ShowModal("About Flight ReLive", (aboutLayout) =>
            {
                ImGui.Indent(10f);
                Fugui.PushFont(20, FontType.Bold);
                using (FuGrid appGrid = new FuGrid("appGrid", new FuGridDefinition(2, new float[] { 0.5f, 0.5f }), FuGridFlag.Default))
                {
                    Fugui.PushFont(14, FontType.Regular);
                    appGrid.Text("Application");
                    appGrid.Text(Application.companyName + " - 2025");
                    appGrid.Text("Version");
                    appGrid.Text(Application.version);
                    appGrid.Text("Author");
                    appGrid.Text("Jérôme CREMOUX");
                    appGrid.Text("Website");
                    appGrid.TextURL("https://www.flight-relive.org", "https://www.flight-relive.org");
                }
                Fugui.PopFont();
                ImGui.Unindent(10f);
                aboutLayout.Separator();

                //Credits
                ImGui.Indent(10f);
                Fugui.PushFont(20, FontType.Bold);
                ImGui.Text("Thanks to");
                Fugui.PopFont();
                aboutLayout.Spacing();

                using (FuGrid creditGrid = new FuGrid("creditGrid", new FuGridDefinition(1, new float[] { 1f }), FuGridFlag.Default))
                {
                    Fugui.PushFont(14, FontType.Regular);
                    creditGrid.TextURL("Unity Engine 6.2", "https://unity.com");
                    creditGrid.TextURL("Fugui", "https://github.com/Keksls/fugui");
                    creditGrid.TextURL("MapTiler", "https://www.maptiler.com/");
                    creditGrid.TextURL("FFmpeg", "https://ffmpeg.org/");
                    creditGrid.TextURL("Clipper2Lib", "https://github.com/AngusJohnson/Clipper2");
                    creditGrid.TextURL("LibTessDotNet", "https://github.com/speps/LibTessDotNet");
                    creditGrid.TextURL("Vector-tile-cs", "https://github.com/mapbox/vector-tile-cs");
                    creditGrid.TextURL("Unity.webp", "https://github.com/netpyoung/unity.webp");
                    Fugui.PopFont();
                }

                ImGui.Unindent(10f);
                aboutLayout.Separator();

                //Special thanks
                ImGui.Indent(10f);
                Fugui.PushFont(20, FontType.Bold);
                ImGui.Text("Special thanks to");
                Fugui.PopFont();
                aboutLayout.Spacing();

                using (FuGrid specialThanks = new FuGrid("specialThanksGrid", new FuGridDefinition(1, new float[] { 1f }), FuGridFlag.Default))
                {
                    Fugui.PushFont(14, FontType.Regular);
                    specialThanks.Text("Website design by Sylvie DECHORAIN");
                    specialThanks.Text("Fugui framework created by Kevin BOUETARD");
                    specialThanks.Text("");
                    Fugui.PopFont();
                    Fugui.PushFont(14, FontType.Italic);
                    specialThanks.Text("In memory of 'Mélusine'");
                    Fugui.PopFont();
                }

                ImGui.Unindent(10f);
                aboutLayout.Separator();

                aboutLayout.CenterNextItem(128);
                if (aboutLayout.Image("tipeee", _tipeee, new FuElementSize(128, 64), false, true))
                {
                    Process.Start("https://fr.tipeee.com/flight-relive/");
                }

            }, FuModalSize.Medium, new FuModalButton("OK", () => { _aboutOpened = false; }, FuButtonStyle.Highlight, FuKeysCode.Enter));
        }
        #endregion
    }

    public enum LayoutTypes
    {
        System = 0,
        Working = 1,
        Custom = 2
    }
}
