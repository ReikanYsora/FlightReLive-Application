using FlightReLive.Core;
using FlightReLive.Core.Cache;
using FlightReLive.Core.Layouts;
using FlightReLive.Core.Library;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Platform;
using FlightReLive.Core.Settings;
using FlightReLive.UI.Share;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace FlightReLive.UI.Menu
{
    public class MenuManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Texture2D _tipeee;
        private bool _aboutOpened = false;
        #endregion

        #region UNITY METHODS
        private void Start()
        {
            RegisterMainMenuItems();
        }

        private void OnDestroy()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            MacOsMainMenuManager.ResetMenu();
#endif
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Register main menu items
        /// </summary>
        private void RegisterMainMenuItems()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
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

            //Import menu
            MacOsMainMenuManager.AddMenuEntry("Import", "Add flight into library", () =>
            {
                string safePath = Path.Combine(Application.persistentDataPath);
                FileBrowser.OpenFilePanelAsync("Select *.mp4 DJI drone video flight", Application.persistentDataPath, new ExtensionFilter[] { new ExtensionFilter("MPEG-4", "mp4") }, true,
                    async (paths) =>
                    {
                        if (paths.Length > 0)
                        {
                            await LibraryManager.Instance.ImportFlights(paths);
                        }
                    });

            }, "L");

            MacOsMainMenuManager.AddMenuEntry("Import", "Import from SharedHash", () =>
            {
                ShareViewManager.DisplaySharedHashModal();
            }, "H");

            //Settings menu
            MacOsMainMenuManager.AddMenuEntry("Settings", "Preferences", () =>
            {
                SettingsManager.ShowPreferencesModal();
            }, "P");

            MacOsMainMenuManager.AddSeparator("Settings");
            MacOsMainMenuManager.AddMenuEntry("Settings", "Restore default layout", () =>
            {
                LayoutManager.Instance.RestoreDefaultLayout();
            });

            MacOsMainMenuManager.AddMenuEntry("Settings", "Clear local cache", () =>
            {
                if (!LoadingManager.Instance.IsLoading)
                {
                    FlightReLiveUIHelper.ShowYesNoMessageBox("Clear local cache?", "This action will delete all downloaded tiles. This action cannot be undone. Continue?", CacheManager.ClearCache, null);
                }
                else
                {
                    Fugui.Notify("Action canceled", "Action not allowed during loading. Please try again.", StateType.Warning, 5f);
                }
            });

            MacOsMainMenuManager.AddMenuEntry("Settings", "Clear flights library", () =>
            {
                if (!LoadingManager.Instance.IsLoading)
                {
                    FlightReLiveUIHelper.ShowYesNoMessageBox("Clear flights library?", "You're about to clear your flights library. This action cannot be undone. Continue?", LibraryManager.Instance.ClearLibrary, null);
                }
                else
                {
                    Fugui.Notify("Action canceled", "Action not allowed during loading. Please try again.", StateType.Warning, 5f);
                }
            });

            MacOsMainMenuManager.AddMenuEntry("Settings", "Reset preferences", () =>
            {
                if (!LoadingManager.Instance.IsLoading)
                {
                    FlightReLiveUIHelper.ShowYesNoMessageBox("Restore default preferences?", "This action will restore all settings to their default values. Are you sure you want to continue?", SettingsManager.LoadDefaultSettings, null);
                }
                else
                {
                    Fugui.Notify("Action canceled", "Action not allowed during loading. Please try again.", StateType.Warning, 5f);
                }
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

                MacOsMainMenuManager.AddMenuEntry("Windows",
                    displayName,
                    () =>
                    {
                        // Use full FuWindowName (with icon) for window creation
                        Fugui.CreateWindowAsync(windowName, null);
                    });
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
            string flightReLiveImport = "Import";
            string flightReLiveSettings = "Settings";

            //"Flight ReLive" menu
            Fugui.RegisterMainMenuItem(flightReLiveTitle, null);
            Fugui.RegisterMainMenuItem(FlightReLiveIcons.Quit + "  Exit", () => { ApplicationManager.Instance.QuitApplication(); }, flightReLiveTitle);

            //Import menu
            Fugui.RegisterMainMenuItem(flightReLiveImport, null);
            Fugui.RegisterMainMenuItem(FlightReLiveIcons.Database + "  Add flight into library", () =>
            {
                string safePath = Path.Combine(Application.persistentDataPath);
                FileBrowser.OpenFilePanelAsync("Select *.mp4 DJI drone video flight", Application.persistentDataPath, new ExtensionFilter[] { new ExtensionFilter("MPEG-4", "mp4") }, true,
                    async (paths) =>
                    {
                        if (paths.Length > 0)
                            await LibraryManager.Instance.ImportFlights(paths);
                    });

            }, flightReLiveImport);

            Fugui.RegisterMainMenuItem(FlightReLiveIcons.Share + "  Import from SharedHash", () =>
            {
                ShareViewManager.DisplaySharedHashModal();
            }, flightReLiveImport);

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
            Fugui.RegisterMainMenuItem("Restore default layout", () =>
            {
                LayoutManager.Instance.RestoreDefaultLayout();
            }, flightReLiveSettings);
            Fugui.RegisterMainMenuItem("Clear local cache", () =>
            {
                if (!LoadingManager.Instance.IsLoading)
                {
                    FlightReLiveUIHelper.ShowYesNoMessageBox("Clear local cache?", "This action will delete all downloaded tiles. This action cannot be undone. Continue?", CacheManager.ClearCache, null);
                }
                else
                {
                    Fugui.Notify("Action canceled", "Action not allowed during loading. Please try again.", StateType.Warning, 5f);
                }
            }, flightReLiveSettings);
            Fugui.RegisterMainMenuItem("Clear flights library", () =>
            {
                if (!LoadingManager.Instance.IsLoading)
                {
                    FlightReLiveUIHelper.ShowYesNoMessageBox("Clear flights library?", "You're about to clear your flights library. This action cannot be undone. Continue?", LibraryManager.Instance.ClearLibrary, null);
                }
                else
                {
                    Fugui.Notify("Action canceled", "Action not allowed during loading. Please try again.", StateType.Warning, 5f);
                }
            }, flightReLiveSettings);
            Fugui.RegisterMainMenuItem("Restore default preferences", () =>
            {
                if (!LoadingManager.Instance.IsLoading)
                {
                    FlightReLiveUIHelper.ShowYesNoMessageBox("Restore default preferences?", "This action will restore all settings to their default values. Are you sure you want to continue?", SettingsManager.LoadDefaultSettings, null);
                }
                else
                {
                    Fugui.Notify("Action canceled", "Action not allowed during loading. Please try again.", StateType.Warning, 5f);
                }
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
            Fugui.ShowModal(FlightReLiveIcons.About + "  About Flight ReLive", (aboutLayout) =>
            {
                ImGui.Indent(10f);
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
                    Fugui.PopFont();
                }
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
                    creditGrid.TextURL("UnityPhysicallyBasedSkyURP", "https://github.com/jiaozi158/UnityPhysicallyBasedSkyURP");
                    creditGrid.TextURL("UnityVolumetricCloudsURP", "https://github.com/jiaozi158/UnityVolumetricCloudsURP");
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

                aboutLayout.CenterNextItemH(128);
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
