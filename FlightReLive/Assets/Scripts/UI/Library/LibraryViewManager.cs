using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Library;
using FlightReLive.UI.Helpers;
using FlightReLive.UI.Share;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FlightReLive.Core.Database;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace FlightReLive.UI.Library
{
    public class LibraryViewManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float HEADER_BAR_HEIGHT = 26f;
        private const float FOOTER_BAR_HEIGHT = 26f;
        private const float THUMBNAIL_BORDER_THICKNESS = 1f;
        private const float PROGRESSBAR_WIDTH = 100f;
        private const float PROGRESSBAR_HEIGHT = 6f;
        private const float SLIDER_WIDTH = 80f;
        private const float SLIDER_HEIGHT = 20f;
        private const float HORIZONTAL_PADDING = 13f;
        #endregion

        #region ATTIBUTES
        [Header("Thumbnail Settings")]
        private float _thumbnailScale;
        private string _filterWord = "";
        private bool _libraryIsLoading = false;
        private float _loadingProgress = 0f;
        #endregion

        #region UNITY METHODS
        public void Start()
        {
            LibraryManager.Instance.OnLibraryLoading += OnLibraryLoading;
            LibraryManager.Instance.OnLibraryStartLoading += OnLibraryStartLoading;
            SettingsManager.OnLibraryZoomChanged += OnLibraryZoomChanged;
            LibraryManager.Instance.OnLibraryEndLoading += OnLibraryEndLoading;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFlightUnloaded;
            _thumbnailScale = SettingsManager.CurrentSettings.LibraryZoom;

            LibraryManager.Instance.LoadFlightsFromDatabase();
        }

        private void OnDestroy()
        {
            LibraryManager.Instance.OnLibraryLoading -= OnLibraryLoading;
            LibraryManager.Instance.OnLibraryStartLoading -= OnLibraryStartLoading;
            SettingsManager.OnLibraryZoomChanged -= OnLibraryZoomChanged;
            LibraryManager.Instance.OnLibraryEndLoading -= OnLibraryEndLoading;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded -= OnFlightUnloaded;
        }
        #endregion

        /// <summary>
        /// Whenever the window is created, set the camera to the MouseOrbitImproved component
        /// </summary>
        /// <param name="window"> FuWindow instance</param>
        public override void OnWindowCreated(FuWindow window)
        {
            window.HeaderHeight = HEADER_BAR_HEIGHT;
            window.FooterHeight = HEADER_BAR_HEIGHT;
            window.HeaderUI = DrawLibraryHeader;
            window.FooterUI = DrawLibraryFooter;
            window.UI = OnUI;
            _thumbnailScale = SettingsManager.CurrentSettings.LibraryZoom;
        }

        #region CALLBACKS
        private void OnLibraryStartLoading()
        {
            _libraryIsLoading = true;
        }

        private void OnLibraryLoading(float progress)
        {
            _loadingProgress = progress;
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
        }

        private void OnLibraryEndLoading()
        {
            _loadingProgress = 1f;
            _libraryIsLoading = false;
        }

        private void OnFlightEndLoading()
        {
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
        }

        private void OnFlightUnloaded()
        {
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
        }

        private void OnLibraryZoomChanged(float zoom)
        {
            _thumbnailScale = SettingsManager.CurrentSettings.LibraryZoom;
        }
        #endregion

        #region UI
        private void DrawLibraryHeader(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            size.y = HEADER_BAR_HEIGHT * scale;
            float unscaledHeight = size.y / scale;
            FuStyle customStyle = new FuStyle(FuTextStyle.Default, FuFrameStyle.Default, new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)), FuStyle.Unpadded.FramePadding, FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("libraryHeaderPanel", customStyle, false, HEADER_BAR_HEIGHT, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                using (FuLayout layout = new FuLayout())
                {
                    Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));
                    Fugui.MoveY(3f);
                    ImGui.BeginGroup();
                    layout.Spacing();
                    layout.SameLine();

                    float panelWidth = window.WorkingAreaSize.x;
                    float spacing = Fugui.Themes.CurrentTheme.ItemSpacing.x * scale * 2;
                    float searchWidth = 200f * scale;
                    Vector2 iconSize = new Vector2(14f, 14f);
                    float totalWidth = layout.GetAvailableWidth();
                    float searchX = panelWidth - totalWidth;

                    if (panelWidth > totalWidth)
                    {
                        ImGui.SetCursorPosX(searchX);
                        float frameHeight = ImGui.GetFrameHeight();
                        float iconOffsetY = (frameHeight - iconSize.y + 2) / 2f;
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + iconOffsetY);
                        Fugui.PushFont(14, FontType.Regular);
                        layout.Text(FlightReLiveIcons.Filter);
                        Fugui.PopFont();
                        layout.SameLine();
                        Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(4f, 3));
                        Fugui.Push(ImGuiStyleVar.FrameRounding, 6f);
                        layout.TextInput("##librarySearchPanel0", "", ref _filterWord, 128, width: searchWidth);
                        ImGui.Dummy(new Vector2(1, 1));
                        Fugui.PopStyle(2);
                    }

                    ImGui.EndGroup();
                    Fugui.PopColor();
                }
            }
        }

        private void DrawLibraryFooter(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            FuStyle footerStyle = new FuStyle(FuTextStyle.Default, FuFrameStyle.Default, new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)), FuStyle.Unpadded.FramePadding, FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("libraryFooterPanel", footerStyle, false, FOOTER_BAR_HEIGHT, size.x, FuPanelFlags.NoScroll))
            {
                using (FuLayout layout = new FuLayout())
                {
                    Fugui.Push(ImGuiCol.PopupBg, Fugui.Themes.GetColor(FuColors.FrameBg));
                    float frameHeight = ImGui.GetFrameHeight();
                    float iconOffsetY = (frameHeight - 14f + 2) / 2f;
                    float baseY = ImGui.GetCursorPosY() + iconOffsetY;

                    if (_libraryIsLoading)
                    {
                        Vector2 barSize = new Vector2(PROGRESSBAR_WIDTH, PROGRESSBAR_HEIGHT);
                        FuElementSize fuBarSize = new FuElementSize(barSize);
                        Fugui.MoveX(HORIZONTAL_PADDING);
                        layout.CenterNextItemV(PROGRESSBAR_HEIGHT);
                        layout.ProgressBar("##libraryFooterSLoadingBar", _loadingProgress, fuBarSize, ProgressBarTextPosition.None);
                        layout.SameLine();
                    }

                    if (layout.GetAvailableWidth() > SLIDER_WIDTH + HORIZONTAL_PADDING * 2)
                    {
                        float sliderW = SLIDER_WIDTH * scale;
                        float sliderH = SLIDER_HEIGHT * scale;
                        float paddingRight = HORIZONTAL_PADDING * scale;
                        float xRight = size.x - sliderW - paddingRight;
                        float knobOffset = Fugui.Themes.NodeKnobRadius / 2 * scale;
                        float yCenter = (FOOTER_BAR_HEIGHT - SLIDER_HEIGHT) * 0.5f * scale + knobOffset;
                        ImGui.SetCursorPos(new Vector2(xRight, yCenter));
                        ImGui.BeginChild("libraryFooterSliderChild", new Vector2(sliderW, sliderH), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
                        float tempZoom = SettingsManager.CurrentSettings.LibraryZoom;

                        if (layout.Slider("sliderLibraryScale", ref tempZoom, 0f, 1f, 0.01f, FuSliderFlags.NoDrag))
                        {
                            SettingsManager.SaveLibraryZoom(tempZoom);
                        }

                        ImGui.EndChild();
                    }
                }
            }
        }

        private void DrawContextualMenu(FuWindow window, RealmFlightItem flight)
        {
            if (window.Mouse.IsDown(FuMouseButton.Right))
            {
                FuContextMenuBuilder contextMenuBuilder = FuContextMenuBuilder.Start();

                contextMenuBuilder.AddItem($"Load", () =>
                {
                    LibraryManager.Instance.SelectFlight(flight);
                });

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Maps}  Display in OpenStreetMap", () =>
                {
                    OpenStreetMapHelper.OpenOpenStreetMapBrowser(flight.DataPoints.Select(p => new Vector2((float)p.Latitude, (float)p.Longitude)).ToList());
                });

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Share}  Share", () =>
                {
                    ShareViewManager.DisplayShareModal(flight);
                });

                contextMenuBuilder.AddSeparator();

                contextMenuBuilder.AddItem("Remove from library", () =>
                {

                });

                contextMenuBuilder.AddSeparator();

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Inspector}  Properties", () =>
                {

                });

                List<FuContextMenuItem> contextMenuItems = contextMenuBuilder.Build();
                Fugui.PushContextMenuItems(contextMenuItems);
                Fugui.TryOpenContextMenu();
                Fugui.PopContextMenuItems();
            }
        }

        /// <summary>
        /// Called each frame to draw the UI of this window
        /// </summary>
        /// <param name="window"> the window that is drawing this UI</param>
        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            float scale = Fugui.CurrentContext.Scale;
            float thumbnailScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(_thumbnailScale));
            Vector2 contentRegion = new Vector2(windowLayout.GetAvailableWidth(), windowLayout.GetAvailableHeight() / scale - FOOTER_BAR_HEIGHT);

            using (FuPanel panel = new FuPanel("libraryUIPanel", false, contentRegion.y, contentRegion.x, flags: FuPanelFlags.Default))
            {
                Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);
                Vector2 itemBaseSize = new Vector2(160, 95);
                Vector2 itemSize = itemBaseSize * scale * thumbnailScale;
                float paddingX = 16f * scale * thumbnailScale;
                float paddingY = 16f * scale * thumbnailScale;
                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                float x = cursorPos.x;
                float y = cursorPos.y;
                float maxY = y;

                foreach (RealmFlightItem file in LibraryManager.Instance.LoadedFlights
                    .Where(f => string.IsNullOrEmpty(_filterWord) || f.Name.Contains(_filterWord, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.Name))
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        if (LoadingManager.Instance.IsLoading || _libraryIsLoading)
                        {
                            layout.DisableNextElements();
                        }

                        if (x + itemSize.x > cursorPos.x + contentRegion.x)
                        {
                            x = cursorPos.x;
                            y = maxY + paddingY;
                        }

                        Vector2 itemPos = new Vector2(x, y);
                        Vector2 itemEnd = itemPos + itemSize;
                        ImDrawListPtr drawListItem = ImGui.GetWindowDrawList();
                        Vector2 mousePos = ImGui.GetMousePos();
                        Vector2 windowPos = ImGui.GetWindowPos();
                        Vector2 windowSize = ImGui.GetWindowSize();
                        float workspaceTop = windowPos.y + HEADER_BAR_HEIGHT;
                        float workspaceBottom = windowPos.y + windowSize.y - FOOTER_BAR_HEIGHT;
                        bool isHovered = ImGui.IsMouseHoveringRect(itemPos, itemEnd) && window.IsHovered && mousePos.y > workspaceTop && mousePos.y < workspaceBottom;
                        bool isSelected = LoadingManager.Instance.CurrentFlightData?.Name == file.Name;
                        uint bgColor = ImGui.GetColorU32(isSelected ? Fugui.Themes.GetColor(FuColors.Highlight) : isHovered ? Fugui.Themes.GetColor(FuColors.HoveredWindowTab) : Fugui.Themes.GetColor(FuColors.Button));
                        float cornerRadius = 4f * scale * thumbnailScale;
                        FuguiDrawListHelper.DrawRoundedRect(drawListItem, itemPos, itemSize, bgColor, cornerRadius, 5);

                        if (isHovered)
                        {
                            DrawContextualMenu(window, file);
                        }

                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && isHovered)
                        {
                            LibraryManager.Instance.SelectFlight(file);
                        }

                        float thumbMaxWidth = 150f * scale * thumbnailScale;
                        float thumbMaxHeight = itemSize.y - 10f * scale * thumbnailScale;
                        int originalWidth = file.Thumbnail != null ? file.Thumbnail.width : 0;
                        int originalHeight = file.Thumbnail != null ? file.Thumbnail.height : 0;
                        float finalScale = Mathf.Min(thumbMaxWidth / originalWidth, thumbMaxHeight / originalHeight);
                        Vector2 thumbSize = new Vector2(originalWidth, originalHeight) * finalScale;
                        float thumbPadding = 5f * scale * thumbnailScale;
                        Vector2 thumbPosition = itemPos + new Vector2((itemSize.x - thumbSize.x) / 2f, thumbPadding);

                        //Thumbnail
                        if (file.Thumbnail != null)
                        {
                            IntPtr textureID = FuWindow.CurrentDrawingWindow.Container.GetTextureID(file.Thumbnail);
                            ImGui.SetCursorScreenPos(thumbPosition);
                            ImGui.Image(textureID, thumbSize);

                            float borderThickness = THUMBNAIL_BORDER_THICKNESS * scale * thumbnailScale;
                            drawListItem.AddRectFilled(thumbPosition, thumbPosition + new Vector2(thumbSize.x, borderThickness), bgColor);
                            drawListItem.AddRectFilled(thumbPosition + new Vector2(0f, thumbSize.y - borderThickness), thumbPosition + new Vector2(thumbSize.x, thumbSize.y), bgColor);
                            drawListItem.AddRectFilled(thumbPosition, thumbPosition + new Vector2(borderThickness, thumbSize.y), bgColor);
                            drawListItem.AddRectFilled(thumbPosition + new Vector2(thumbSize.x - borderThickness, 0f), thumbPosition + new Vector2(thumbSize.x, thumbSize.y), bgColor);
                        }

                        //Bottom-left text (duration)
                        if (file.Duration != null)
                        {
                            Fugui.PushFont(12, FontType.Regular);
                            string duration = file.Duration.ToString(@"hh\:mm\:ss");
                            Vector2 textSize = ImGui.CalcTextSize(duration);
                            Vector2 padding = new Vector2(4f, 2f) * scale * thumbnailScale;
                            Vector2 bgSize = textSize + padding * 2;
                            Vector2 pos = thumbPosition + new Vector2(4f * scale * thumbnailScale, thumbSize.y - bgSize.y - 4f * scale * thumbnailScale);
                            Vector2 finalPos = pos + new Vector2((bgSize.x - textSize.x) / 2f, padding.y);

                            drawListItem.AddRectFilled(pos, pos + bgSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.9f)));
                            ImGui.SetCursorScreenPos(finalPos);
                            layout.Text(duration, FuTextStyle.Default);
                            Fugui.PopFont();
                        }

                        x += itemSize.x + paddingX;
                        maxY = Mathf.Max(maxY, itemEnd.y);
                    }
                }

                ImGui.SetCursorScreenPos(new Vector2(cursorPos.x, maxY + paddingY));
                ImGui.Dummy(new Vector2(1, 2f));
                Fugui.PopStyle();
            }
        }
    }
    #endregion
}
