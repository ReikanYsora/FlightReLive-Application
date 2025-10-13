using FlightReLive.Core;
using FlightReLive.Core.Cache;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Share;
using FlightReLive.Core.Workspace;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.IO;
using UnityEngine;

namespace FlightReLive.UI.Share
{
    internal static class ShareViewManager
    {
        #region CONSTANTS
        private const float PADDING = 10f;
        private const float SHARED_HASH_WIDTH = 200f;
        #endregion

        #region ATTRIBUTES
        private static string _shareHash = string.Empty;
        private static bool _isSharing = false;
        private static bool _isDownloading = false;
        private static bool _dowloadSuccess = false;
        private static string _downloadError = "";
        private static string _sharedHash = "";
        private static bool _focusGiven = false;
        #endregion

        #region METHODS
        internal static void DisplaySharedHashModal()
        {
            float uiScale = Fugui.DefaultContext.Scale;

            Fugui.ShowModal("   ", (layout) =>
            {
                if (_isDownloading && !_dowloadSuccess)
                {
                    string title = "Please wait...";
                    Fugui.PushFont(14, FontType.Regular);
                    layout.CenterNextItemH(title);
                    layout.Text(title);
                    Fugui.PopFont();
                }
                else
                {
                    string title = "Load a flight from a sharedhash";
                    Fugui.PushFont(14, FontType.Regular);
                    layout.CenterNextItemH(title);
                    layout.Text(title);
                    Fugui.PopFont();
                    layout.Separator();
                }

                layout.Spacing();

                if (!_isDownloading && !_dowloadSuccess)
                {
                    using (FuGrid sharedHashOpenGrid = new FuGrid("sharedHashOpenGrid", new FuGridDefinition(2, new float[] { 0.5f, 0.5f }), FuGridFlag.Default, 2, 2, 10))
                    {
                        float available = sharedHashOpenGrid.GetAvailableWidth();
                        float width = (available / uiScale / 2f) - (PADDING / 2f);

                        if (!_focusGiven)
                        {
                            ImGui.SetKeyboardFocusHere();
                            _focusGiven = true;
                        }

                        sharedHashOpenGrid.TextInput("Paste the sharedhash you want to load", ref _sharedHash, flags: FuInputTextFlags.EnterReturnsTrue);
                        sharedHashOpenGrid.NextColumn();

                        if (string.IsNullOrEmpty(_sharedHash.Trim()))
                        {
                            sharedHashOpenGrid.DisableNextElement();
                        }

                        if (Fugui.GetKeyPressed(FuKeysCode.Enter))
                        {
                            StartDownloadAsync(_sharedHash);
                        }

                        if (sharedHashOpenGrid.Button("Load flight from sharedhash", new FuElementSize(new Vector2(width, 20f)), FuButtonStyle.Info))
                        {
                            StartDownloadAsync(_sharedHash);
                        }
                    }
                }

                if (_isDownloading && !_dowloadSuccess)
                {
                    layout.CenterNextItemH(20f);
                    layout.Loader_CircleSpinner(20f, 24);
                }

                if (!string.IsNullOrEmpty(_downloadError))
                {
                    layout.Spacing();
                    Fugui.PushFont(14, FontType.Bold);
                    layout.CenterNextItemH(_downloadError);
                    ImGui.PushStyleColor(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDanger));
                    layout.Text(_downloadError);
                    ImGui.PopStyleColor();

                    Fugui.PopFont();
                }

            }, new FuModalSize(new Vector2(450, 450)), new FuModalButton("Close", () => { ResetDownload(); }, FuButtonStyle.Default));
        }

        internal static void DisplayShareModal(FlightFile fileToShare)
        {
            float uiScale = Fugui.DefaultContext.Scale;

            Fugui.ShowModal("   ", (layout) =>
            {
                string title = "Select a method to share your flight";
                Fugui.PushFont(14, FontType.Bold);
                layout.CenterNextItemH(title);
                layout.Text(title);
                Fugui.PopFont();

                layout.Spacing();
                layout.Collapsable("Online - SharedHash", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);
                    layout.Text("Share your flight with a simple hashcode.", FuTextWrapping.Wrap);
                    layout.Spacing();
                    layout.Text("Send it to users, and anyone with this code will be able to view your flight (the video will not be included, only the flight data needed to reconstruct the scene in Flight ReLive).", FuTextWrapping.Wrap);
                    Fugui.PopFont();
                    layout.Spacing();
                    Fugui.PushFont(14, FontType.Bold);
                    layout.Text("Anonymous submission, no data other than that necessary for viewing the scene is sent.", FuTextWrapping.Wrap);
                    Fugui.PopFont();
                    layout.Spacing();

                    if (!_isSharing && string.IsNullOrEmpty(_shareHash))
                    {
                        using (FuGrid onlineGrid = new FuGrid("shareOnlineGrid", new FuGridDefinition(2, new float[] { 0.5f, 0.5f }), FuGridFlag.Default, 2, 2, 10))
                        {
                            if (_isSharing)
                            {
                                onlineGrid.DisableNextElements();
                            }

                            onlineGrid.NextColumn();

                            float available = onlineGrid.GetAvailableWidth();
                            float width = (available / uiScale) - (PADDING / 2f);

                            if (onlineGrid.Button("Get a Flight ReLive SharedHash", new FuElementSize(new Vector2(width, 20f)), FuButtonStyle.Info))
                            {
                                StartShareAsync(fileToShare);
                            }
                        }
                    }

                    if (_isSharing && string.IsNullOrEmpty(_shareHash))
                    {
                        layout.CenterNextItemH(20f);
                        layout.Loader_CircleSpinner(20f, 24);
                    }

                    if (!_isSharing && !string.IsNullOrEmpty(_shareHash))
                    {
                        layout.Separator();
                        layout.Spacing();
                        Fugui.PushFont(14, FontType.Bold);
                        string helpSharedHashText1 = "This is your Flight ReLive SharedHash.";
                        string helpSharedHashText2 = "It will no longer be visible after this operation. Use the button to copy it to your clipboard.";
                        layout.CenterNextItemH(helpSharedHashText1);
                        layout.Text(helpSharedHashText1);
                        Fugui.PopFont();
                        layout.Spacing();
                        Fugui.PushFont(14, FontType.Regular);
                        layout.CenterNextItemH(helpSharedHashText2);
                        layout.Text(helpSharedHashText2);
                        Fugui.PopFont();
                        layout.Spacing();
                        Fugui.MoveX((layout.GetAvailableWidth() - SHARED_HASH_WIDTH - PADDING - 24f) / 2f);
                        Fugui.PushFont(14, FontType.Italic);
                        layout.FramedText(_shareHash, new FuElementSize(SHARED_HASH_WIDTH, 24f));
                        Fugui.PopFont();
                        layout.SameLine();
                        Fugui.PushFont(12, FontType.Regular);
                        if (layout.Button(FlightReLiveIcons.Duplicate, new FuElementSize(new Vector2(24f, 24f)), FuButtonStyle.Default))
                        {
                            GUIUtility.systemCopyBuffer = _shareHash;
                        }
                        Fugui.PopFont();
                    }
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Spacing();

                layout.Collapsable("Offline - Local file", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);
                    layout.Text("Generates a file allowing other users to relive your flight from their Flight ReLive application.", FuTextWrapping.Wrap);
                    layout.Spacing();
                    layout.Text("Anyone with this file will be able to view your flight, but no data will be sent to the Flight ReLive API.", FuTextWrapping.Wrap);
                    Fugui.PopFont();
                    layout.Spacing();

                    using (FuGrid offlineGrid = new FuGrid("shareOfflineGrid", new FuGridDefinition(2, new float[] { 0.5f, 0.5f }), FuGridFlag.Default, 2, 2, 10))
                    {
                        offlineGrid.NextColumn();

                        float available = offlineGrid.GetAvailableWidth();
                        float width = (available / uiScale) - (PADDING / 2f);

                        if (offlineGrid.Button("Export flight to file", new FuElementSize(new Vector2(width, 20f)), FuButtonStyle.Info))
                        {
                            string safePath = Path.Combine(Application.persistentDataPath);
                            FileBrowser.SaveFilePanelAsync("Export a Flight Relive Shared file (.frs)", safePath, fileToShare.Name, "frs",
                            async (x) =>
                            {
                                if (!string.IsNullOrEmpty(x))
                                {
                                    await CacheManager.ExportFlightFileAsync(fileToShare, x);
                                }
                            });
                        }
                    }
                }, FuButtonStyle.Collapsable, defaultOpen: true);
            }, new FuModalSize(new Vector2(600, 600)), new FuModalButton("Close", () => { ResetUpload(); }, FuButtonStyle.Default));
        }

        private static void ResetUpload()
        {
            _shareHash = string.Empty;
            _isSharing = false;
        }

        private static void ResetDownload()
        {
            _isDownloading = false;
            _dowloadSuccess = false;
            _downloadError = "";
            _sharedHash = "";
            _focusGiven = false;
        }

        private static async void StartShareAsync(FlightFile fileToShare)
        {
            if (_isSharing || fileToShare == null)
            {
                return;
            }

            _isSharing = true;
            _shareHash = string.Empty;

            try
            {
                FlightFileShareResponse response = await FlightShareService.ShareFlightFileExAsync(fileToShare).ConfigureAwait(false);

                if (response == null)
                {
                    throw new Exception("FlightFileShareResponse null value");
                }

                OnShareHashReceived(response);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                OnShareHashReceived();
            }
        }

        private static void OnShareHashReceived()
        {
            _shareHash = "";
            _isSharing = false;
        }

        private static void OnShareHashReceived(FlightFileShareResponse response)
        {
            _shareHash = response.ShareHash;
            _isSharing = false;
        }

        internal static async void StartDownloadAsync(string sharedHash)
        {
            if (_isDownloading || _dowloadSuccess)
            {
                return;
            }

            _isDownloading = true;
            _dowloadSuccess = false;

            try
            {
                FlightFile flightFile = await FlightShareService.GetFlightFileAsync(sharedHash).ConfigureAwait(false);

                if (flightFile == null)
                {
                    OnFlightFileError();
                }
                else
                {
                    OnFlightFileReceived(flightFile);
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void OnFlightFileError()
        {
            _isDownloading = false;
            _dowloadSuccess = false;
            _downloadError = "SharedHash not found. Please paste a valid Flight ReLive sharedhash.";
        }

        private static void OnFlightFileReceived(FlightFile flightFile)
        {
            _isDownloading = false;
            _dowloadSuccess = true;
            _downloadError = "";

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                LoadingManager.Instance.StartLoadingScene(flightFile);
            });

            ResetDownload();
            Fugui.CloseModal();
        }
        #endregion
    }
}
