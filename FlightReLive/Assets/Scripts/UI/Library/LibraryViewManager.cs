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
using System.Collections.Generic;

namespace FlightReLive.UI.Library
{
    public class LibraryViewManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float HEADER_BAR_HEIGHT = 26f;
        private const float FOOTER_BAR_HEIGHT = 26f;
        private const float THUMBNAIL_BORDER_THICKNESS = 1f;
        private const float FILTER_WIDTH = 120f;
        private const float SLIDER_WIDTH = 80f;
        private const float SLIDER_HEIGHT = 20f;
        private const float HORIZONTAL_PADDING = 10f;
        private const float PROGRESSBAR_WIDTH = 100f;
        private const float PROGRESSBAR_HEIGHT = 6f;
        #endregion

        #region ATTIBUTES
        [Header("Thumbnail Settings")]
        private float _thumbnailScale;
        private string _filterWord = "";
        private readonly Dictionary<string, float> _hoverBlendFactors = new Dictionary<string, float>();
        private bool _libraryIsLoading = false;
        private float _loadingProgress = 0f;
        #endregion

        #region UNITY METHODS
        public void Start()
        {
            SettingsManager.OnLibraryZoomChanged += OnLibraryZoomChanged;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFlightUnloaded;
            LibraryManager.Instance.OnLibraryLoading += OnLibraryLoading;
            LibraryManager.Instance.OnLibraryStartLoading += OnLibraryStartLoading;
            LibraryManager.Instance.OnLibraryEndLoading += OnLibraryEndLoading;
            _thumbnailScale = SettingsManager.CurrentSettings.LibraryZoom;
        }

        private void OnDestroy()
        {
            SettingsManager.OnLibraryZoomChanged -= OnLibraryZoomChanged;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded -= OnFlightUnloaded;
            LibraryManager.Instance.OnLibraryLoading -= OnLibraryLoading;
            LibraryManager.Instance.OnLibraryStartLoading -= OnLibraryStartLoading;
            LibraryManager.Instance.OnLibraryEndLoading -= OnLibraryEndLoading;
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

        private void OnFlightEndLoading(SerializedFlightData flight)
        {
            flight.IsNew = false;
            DatabaseManager.SaveFlight(flight);
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
        /// <summary>
        /// Draw library window header
        /// </summary>
        /// <param name="window"></param>
        /// <param name="size"></param>
        private void DrawLibraryHeader(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            size.y = HEADER_BAR_HEIGHT * scale;

            FuStyle headerStyle = new FuStyle(FuTextStyle.Default, FuFrameStyle.Default, new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)), FuStyle.Unpadded.FramePadding, FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("libraryHeaderPanel", headerStyle, false, HEADER_BAR_HEIGHT, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                using (FuLayout layout = new FuLayout())
                {
                    Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));

                    float frameHeight = ImGui.GetFrameHeight();
                    Vector2 iconSize = new Vector2(12f, 12f);
                    float searchWidth = FILTER_WIDTH * scale;
                    float spacing = 3f * scale;
                    float paddingRight = HORIZONTAL_PADDING;
                    float childW = iconSize.x + spacing + searchWidth;
                    float childH = frameHeight;
                    float availableWidth = layout.GetAvailableWidth();

                    if (availableWidth > childW + HORIZONTAL_PADDING * 2f)
                    {
                        float xRight = window.WorkingAreaSize.x - childW - paddingRight;
                        float yCenter = (HEADER_BAR_HEIGHT * scale - childH) * 0.5f;
                        ImGui.SetCursorPos(new Vector2(xRight, yCenter));
                        ImGui.BeginChild("libraryHeaderSearchChild", new Vector2(childW, childH), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
                        float startX = ImGui.GetCursorPosX();
                        float startY = ImGui.GetCursorPosY();
                        Fugui.PushFont(12, FontType.Regular);
                        layout.CenterNextItemV(FlightReLiveIcons.Property);
                        layout.Text(FlightReLiveIcons.Property);
                        Fugui.PopColor();
                        Fugui.PopFont();
                        float inputY = (childH - frameHeight) * 0.5f;
                        ImGui.SetCursorPos(new Vector2(startX + iconSize.x + spacing, startY + inputY));
                        layout.TextInput("##librarySearchInput", "", ref _filterWord, 128, width: searchWidth);
                        ImGui.EndChild();
                    }

                    Fugui.PopColor();
                }
            }
        }

        /// <summary>
        /// Draw library window footer
        /// </summary>
        /// <param name="window"></param>
        /// <param name="size"></param>
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

        /// <summary>
        /// Draw flight item contextual menu
        /// </summary>
        /// <param name="window"></param>
        /// <param name="flight"></param>
        private void DrawContextualMenu(FuWindow window, SerializedFlightData flight)
        {
            if (window.Mouse.IsDown(FuMouseButton.Right))
            {
                FuContextMenuBuilder contextMenuBuilder = FuContextMenuBuilder.Start();

                contextMenuBuilder.AddTitle(flight.Name);

                if (flight.Thumbnail != null)
                {
                    contextMenuBuilder.AddImage(flight.Thumbnail, new FuElementSize(flight.Thumbnail.width, flight.Thumbnail.height), 1, null);

                    contextMenuBuilder.AddSeparator();
                }

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Globe}   Load", () =>
                {
                    LibraryManager.Instance.SelectFlight(flight);
                });

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Maps}   Display in OpenStreetMap", () =>
                {
                    OpenStreetMapHelper.OpenOpenStreetMapBrowser(flight.DataPoints.Select(p => p.Coordinate).ToList());
                });

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Share}   Share", () =>
                {
                    ShareViewManager.DisplayShareModal(flight);
                });

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Check}   Mark as loaded", () =>
                {
                    flight.IsNew = false;
                    DatabaseManager.SaveFlight(flight);
                });

                contextMenuBuilder.AddSeparator();

                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Delete}   Remove from library", () =>
                {
                    LibraryManager.Instance.DeleteFlightItem(flight);
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
                float y = cursorPos.y + paddingY;
                float maxY = y;

                float scrollbarWidth = Fugui.Themes.ScrollbarSize;
                float visibleContentWidth = contentRegion.x - scrollbarWidth - 2f * scale;

                foreach (SerializedFlightData file in LibraryManager.Instance.LoadedFlights
                    .Where(f => string.IsNullOrEmpty(_filterWord) || f.Name.Contains(_filterWord, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.Name))
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        if (LoadingManager.Instance.IsLoading)
                        {
                            layout.DisableNextElements();
                        }

                        if (x + itemSize.x > cursorPos.x + visibleContentWidth)
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

                        // Get or initialize blend factor for this file
                        float blend = 0f;
                        if (_hoverBlendFactors.TryGetValue(file.Name, out float current))
                        {
                            float target = isHovered ? 1f : 0f;
                            float fadeSpeed = 10f * Time.deltaTime; // 10 = très rapide (0.1s environ)
                            blend = Mathf.MoveTowards(current, target, fadeSpeed);
                            _hoverBlendFactors[file.Name] = blend;
                        }
                        else
                        {
                            _hoverBlendFactors[file.Name] = isHovered ? 1f : 0f;
                            blend = _hoverBlendFactors[file.Name];
                        }

                        // Compute colors
                        Color baseColor = Fugui.Themes.GetColor(FuColors.Button);
                        Color hoverColor = Fugui.Themes.GetColor(FuColors.HoveredWindowTab);
                        Color selectedColor = Fugui.Themes.GetColor(FuColors.Highlight);

                        // Interpolate colors
                        Color finalColor = isSelected ? selectedColor : Color.Lerp(baseColor, hoverColor, blend);
                        uint bgColor = ImGui.GetColorU32(finalColor);


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

                        if (file.IsNew)
                        {
                            float badgeRadius = 5f * scale * thumbnailScale;
                            Vector2 badgeCenter = itemEnd - new Vector2(badgeRadius * 2.5f, itemSize.y - badgeRadius * 2.5f);
                            uint badgeColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
                            drawListItem.AddCircleFilled(badgeCenter, badgeRadius, badgeColor);
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

                _hoverBlendFactors.Keys
                    .Where(k => !LibraryManager.Instance.LoadedFlights.Any(f => f.Name == k))
                    .ToList()
                    .ForEach(k => _hoverBlendFactors.Remove(k));

                ImGui.SetCursorScreenPos(new Vector2(cursorPos.x, maxY + paddingY));
                ImGui.Dummy(new Vector2(1, 2f));
                Fugui.PopStyle();
            }
        }
    }
    #endregion
}
