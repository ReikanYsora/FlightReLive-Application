using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Library;
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
using FlightReLive.UI.Helpers;

namespace FlightReLive.UI.Library
{
    public class LibraryViewManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float HEADER_BAR_HEIGHT = 26f;
        private const float FOOTER_BAR_HEIGHT = 26f;
        private const float FILTER_WIDTH = 120f;
        private const float HORIZONTAL_PADDING = 10f;
        private const float PROGRESSBAR_WIDTH = 100f;
        private const float PROGRESSBAR_HEIGHT = 6f;
        private const float RIGHT_PADDING = 10f;
        private const float TOOLTIP_MIN_WIDTH = 300f;
        private const float ICON_PADDING = 10f;
        private const float ICON_SIZE = 15f;
        private const float BORDER_RADIUS = 8f;
        private const float ITEM_LINE_HEIGHT = 26f;
        private const float ITEM_LEFT_PADDING = 10f;
        private const float NOTIFICATION_CIRCLE_RADIUS = 4f;
        private const float NOTIFICATION_CIRCLE_SPACING = 4f;
        private const float SLIDER_WIDTH = 80f;
        private const float SLIDER_HEIGHT = 20f;
        private const float THUMBNAIL_BORDER_THICKNESS = 1f;
        #endregion

        #region ATTIBUTES
        private float _thumbnailScale;
        private string _filterWord = "";
        private FlightDataOrigin _filteredOrigin = FlightDataOrigin.All;
        private readonly Dictionary<string, float> _hoverBlendFactors = new Dictionary<string, float>();
        private bool _libraryIsLoading = false;
        private float _loadingProgress = 0f;
        private ShareFilter _filteredShareState = ShareFilter.All;
        #endregion

        #region ENUM
        private enum ShareFilter
        {
            All,
            Shared,
            NotShared
        }
        #endregion

        #region UNITY METHODS
        public void Start()
        {
            SettingsManager.OnDateFormatStyleChanged += OnDateFormatStyleChanged;
            SettingsManager.OnTimeFormatStyleChanged += OnTimeFormatStyleChanged;
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
            SettingsManager.OnDateFormatStyleChanged -= OnDateFormatStyleChanged;
            SettingsManager.OnTimeFormatStyleChanged -= OnTimeFormatStyleChanged;
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
            window.FooterHeight = FOOTER_BAR_HEIGHT;
            window.HeaderUI = DrawLibraryHeader;
            window.FooterUI = DrawLibraryFooter;
            window.UI = OnUI;
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

        private void OnTimeFormatStyleChanged(TimeFormatStyle style)
        {
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
        }

        private void OnDateFormatStyleChanged(DateFormatStyle style)
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
        /// Draw library window header with Fugui enum combobox + search bar
        /// </summary>
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
                    float spacing = 4f * scale;
                    float paddingLeft = HORIZONTAL_PADDING * scale;
                    float paddingRight = HORIZONTAL_PADDING * scale;
                    float searchWidth = FILTER_WIDTH * scale;
                    Vector2 iconSize = new Vector2(12f, 12f);

                    //Filter (global / local)
                    float groupHeight = frameHeight * 1.1f;
                    float groupY = (HEADER_BAR_HEIGHT * scale - groupHeight) * 0.5f;
                    ImGui.SetCursorPos(new Vector2(paddingLeft, groupY));

                    List<string> originIcons = new()
                    {
                        FlightReLiveIcons.Aperture,
                        FlightReLiveIcons.Globe,
                        FlightReLiveIcons.Database
                    };

                    FuButtonsGroupStyle buttonStyle = FuButtonsGroupStyle.Default;
                    Vector2 iconPadding = new Vector2(6f * scale, 2f * scale);
                    float buttonGroupWidth = (originIcons.Count * (frameHeight * 1.5f)) + (HORIZONTAL_PADDING * scale);

                    layout.ButtonsGroup<string>("##libraryOriginButtons", originIcons,
                        (int index) =>
                        {
                            _filteredOrigin = index switch
                            {
                                0 => FlightDataOrigin.All,
                                1 => FlightDataOrigin.SharedHash,
                                2 => FlightDataOrigin.Local,
                                _ => FlightDataOrigin.All
                            };
                            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
                        },
                        () => _filteredOrigin switch
                        {
                            FlightDataOrigin.SharedHash => FlightReLiveIcons.Globe,
                            FlightDataOrigin.Local => FlightReLiveIcons.Database,
                            _ => FlightReLiveIcons.Aperture
                        },
                        width: buttonGroupWidth,
                        padding: iconPadding,
                        flags: FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons,
                        style: buttonStyle
                    );

                    //Filter button (shared / no shared)
                    float shareGroupSpacing = 12f * scale;
                    ImGui.SameLine(0f, shareGroupSpacing);

                    List<string> shareIcons = new()
                    {
                        FlightReLiveIcons.Aperture,
                        FlightReLiveIcons.Share,
                        FlightReLiveIcons.ShutterSpeed
                    };

                    layout.ButtonsGroup<string>("##libraryShareButtons", shareIcons,
                        (int index) =>
                        {
                            _filteredShareState = index switch
                            {
                                0 => ShareFilter.All,
                                1 => ShareFilter.Shared,
                                2 => ShareFilter.NotShared,
                                _ => ShareFilter.All
                            };
                            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
                        },
                        () => _filteredShareState switch
                        {
                            ShareFilter.Shared => FlightReLiveIcons.Share,
                            ShareFilter.NotShared => FlightReLiveIcons.ShutterSpeed,
                            _ => FlightReLiveIcons.Aperture
                        },
                        width: buttonGroupWidth * 0.9f,
                        padding: iconPadding,
                        flags: FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons,
                        style: buttonStyle
                    );

                    //Search bar / Filter bar
                    float childW = iconSize.x + spacing + searchWidth;
                    float childH = frameHeight;
                    float xRight = window.WorkingAreaSize.x - childW - paddingRight;
                    float yCenter = (HEADER_BAR_HEIGHT * scale - childH) * 0.5f;

                    ImGui.SetCursorPos(new Vector2(xRight, yCenter));
                    ImGui.BeginChild("libraryHeaderSearchChild", new Vector2(childW, childH), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);

                    float startX = ImGui.GetCursorPosX();
                    float startY = ImGui.GetCursorPosY();
                    Fugui.PushFont(12, FontType.Regular);
                    layout.CenterNextItemV(FlightReLiveIcons.Search);
                    layout.Text(FlightReLiveIcons.Search);
                    Fugui.PopFont();

                    float inputY = (childH - frameHeight) * 0.5f;
                    ImGui.SetCursorPos(new Vector2(startX + iconSize.x + spacing, startY + inputY));
                    layout.TextInput("##librarySearchInput", "", ref _filterWord, 128, width: searchWidth);
                    ImGui.EndChild();

                    Fugui.PopColor();
                }
            }
        }

        /// <summary>
        /// Display inline mode (when thumbnail scale is low)
        /// </summary>
        /// <param name="window"></param>
        /// <param name="windowLayout"></param>
        private void DrawInlineView(FuWindow window, FuLayout windowLayout)
        {
            float scale = Fugui.CurrentContext.Scale;
            Vector2 contentRegion = new(windowLayout.GetAvailableWidth(), windowLayout.GetAvailableHeight() / scale);
            float lineHeight = ITEM_LINE_HEIGHT * scale;
            float paddingY = 4f * scale;
            float paddingX = ITEM_LEFT_PADDING * scale;
            float circleRadius = NOTIFICATION_CIRCLE_RADIUS * scale;
            float circleSpacing = NOTIFICATION_CIRCLE_SPACING * scale;

            using FuPanel panel = new FuPanel("libraryInlinePanel", false, contentRegion.y, contentRegion.x, FuPanelFlags.Default);
            Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);

            Vector2 cursor = ImGui.GetCursorScreenPos();
            float y = cursor.y + paddingY;

            foreach (SerializedFlightData file in LibraryManager.Instance.LoadedFlights
                .Where(f =>
                    (string.IsNullOrEmpty(_filterWord) || f.Name.Contains(_filterWord, StringComparison.OrdinalIgnoreCase)) &&
                    (_filteredOrigin == FlightDataOrigin.All || f.Origin == _filteredOrigin) &&
                    (_filteredShareState == ShareFilter.All ||
                     (_filteredShareState == ShareFilter.Shared && !string.IsNullOrEmpty(f.ShareHash)) ||
                     (_filteredShareState == ShareFilter.NotShared && string.IsNullOrEmpty(f.ShareHash)))
                )
                .OrderBy(f => f.Name))

            {
                using (FuLayout layout = new FuLayout())
                {
                    Vector2 mousePos = ImGui.GetMousePos();
                    float visibleWidth = ImGui.GetContentRegionAvail().x;
                    Vector2 itemPos = new Vector2(cursor.x, y);
                    Vector2 itemSize = new Vector2(visibleWidth, lineHeight);
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    bool isHovered = ImGui.IsMouseHoveringRect(itemPos, itemPos + itemSize);
                    bool isSelected = LoadingManager.Instance.CurrentFlightData?.UniqueKey == file.UniqueKey;

                    //Hover blend
                    float blend = _hoverBlendFactors.TryGetValue(file.UniqueKey, out float current)
                        ? Mathf.MoveTowards(current, isHovered ? 1f : 0f, 10f * Time.deltaTime)
                        : (isHovered ? 1f : 0f);
                    _hoverBlendFactors[file.UniqueKey] = blend;

                    // olors
                    Color baseColor = Fugui.Themes.GetColor(FuColors.Button);
                    Color hoverColor = Fugui.Themes.GetColor(FuColors.HoveredWindowTab);
                    Color selectedColor = Fugui.Themes.GetColor(FuColors.Highlight);
                    Color finalColor = isSelected ? selectedColor : Color.Lerp(baseColor, hoverColor, blend);
                    uint bgColor = ImGui.GetColorU32(finalColor);

                    //Background
                    float bgRightOffset = RIGHT_PADDING * scale;
                    drawList.AddRectFilled(itemPos, new Vector2(itemPos.x + itemSize.x - bgRightOffset, itemPos.y + itemSize.y), bgColor, 3f * scale);

                    //"New flight" notification circle
                    float textStartX = itemPos.x + paddingX;
                    if (file.IsNew)
                    {
                        Vector2 bulletCenter = new(textStartX, itemPos.y + itemSize.y * 0.5f);
                        uint innerColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
                        drawList.AddCircleFilled(bulletCenter, circleRadius, innerColor);
                    }

                    //Flight name
                    float nameStartX = textStartX + circleRadius * 2f + circleSpacing;
                    ImGui.SetCursorScreenPos(new Vector2(nameStartX, itemPos.y + (lineHeight - ImGui.GetTextLineHeight()) * 0.5f));
                    Fugui.PushFont(13, FontType.Bold);
                    layout.Text(file.Name);
                    Fugui.PopFont();

                    //Icons
                    float iconPadding = ICON_PADDING * scale;
                    float iconSize = ICON_SIZE * scale;
                    float rightOffset = RIGHT_PADDING * scale;
                    float rightX = itemPos.x + itemSize.x - rightOffset - iconPadding - iconSize;

                    Fugui.PushFont(16, FontType.Regular);

                    ImGui.SetCursorScreenPos(new Vector2(rightX, itemPos.y + (lineHeight - iconSize) * 0.5f));
                    if (file.Origin == FlightDataOrigin.SharedHash)
                    {
                        layout.Text(FlightReLiveIcons.Globe);
                    }
                    else if (file.Origin == FlightDataOrigin.Local)
                    {
                        layout.Text(FlightReLiveIcons.Database);
                    }
                    rightX -= iconSize + iconPadding;

                    if (!string.IsNullOrEmpty(file.ShareHash))
                    {
                        ImGui.SetCursorScreenPos(new Vector2(rightX, itemPos.y + (lineHeight - iconSize) * 0.5f));
                        layout.Text(FlightReLiveIcons.Share);
                    }
                    Fugui.PopFont();

                    //Context menu
                    if (isHovered && window.Mouse.IsDown(FuMouseButton.Right))
                    {
                        DrawContextualMenu(file);
                    }

                    //Double click = load
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && isHovered)
                    {
                        LibraryManager.Instance.SelectFlight(file);
                    }

                    //Tooltip
                    if (isHovered && !window.Mouse.IsDown(FuMouseButton.Right) && !Fugui.IsContextMenuOpen)
                    {
                        DrawTooltip(file, layout, mousePos, scale);
                    }

                    y += itemSize.y + paddingY;
                }
            }

            ImGui.SetCursorScreenPos(new Vector2(cursor.x, y));
            ImGui.Dummy(new Vector2(contentRegion.x, 0f));

            _hoverBlendFactors.Keys
                .Where(k => !LibraryManager.Instance.LoadedFlights.Any(f => f.UniqueKey == k))
                .ToList()
                .ForEach(k => _hoverBlendFactors.Remove(k));

            Fugui.PopStyle();
        }

        /// <summary>
        /// Display in thumbnail mode (with thumbnail scaling)
        /// </summary>
        /// <param name="window"></param>
        /// <param name="windowLayout"></param>
        private void DrawThumbnailView(FuWindow window, FuLayout windowLayout)
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
                    .Where(f =>
                        (string.IsNullOrEmpty(_filterWord) || f.Name.Contains(_filterWord, StringComparison.OrdinalIgnoreCase)) &&
                        (_filteredOrigin == FlightDataOrigin.All || f.Origin == _filteredOrigin) &&
                        (_filteredShareState == ShareFilter.All ||
                         (_filteredShareState == ShareFilter.Shared && !string.IsNullOrEmpty(f.ShareHash)) ||
                         (_filteredShareState == ShareFilter.NotShared && string.IsNullOrEmpty(f.ShareHash)))
                    )
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
                        bool isSelected = LoadingManager.Instance.CurrentFlightData?.UniqueKey == file.UniqueKey;

                        // Get or initialize blend factor for this file
                        float blend = 0f;
                        string flightKey = file.UniqueKey;

                        if (_hoverBlendFactors.TryGetValue(flightKey, out float current))
                        {
                            float target = isHovered ? 1f : 0f;
                            float fadeSpeed = 20f * Time.deltaTime;
                            blend = Mathf.MoveTowards(current, target, fadeSpeed);
                            _hoverBlendFactors[flightKey] = blend;
                        }
                        else
                        {
                            _hoverBlendFactors[flightKey] = isHovered ? 1f : 0f;
                            blend = _hoverBlendFactors[flightKey];
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

                        // Hovered
                        if (isHovered)
                        {
                            // Right click
                            if (window.Mouse.IsDown(FuMouseButton.Right))
                            {
                                DrawContextualMenu(file);
                            }
                            else if (!Fugui.IsContextMenuOpen)
                            {
                                string tooltipTitle = file.Name;
                                string tooltipBody = $"{SettingsManager.FormatDateTime(file.CreationDate)}\n\n" +
                                                     $"This file contains {file.DataPoints.Count} recorded flight points.\n\n" +
                                                     "Double click to load this flight, right click for more options.";

                                Vector2 tooltipPadding = new Vector2(12f, 8f) * scale * thumbnailScale;
                                float tooltipMinWidth = 300f * scale * thumbnailScale;
                                float tooltipCornerRadius = 8f * scale * thumbnailScale;
                                Vector2 offset = new Vector2(16f, 16f) * scale * thumbnailScale;
                                float screenMargin = 4f * scale * thumbnailScale;

                                Vector2 titleSize = ImGui.CalcTextSize(tooltipTitle);
                                Vector2 bodySize = ImGui.CalcTextSize(tooltipBody);
                                float spacingBetween = 8f * scale;

                                float totalHeight = titleSize.y + bodySize.y + tooltipPadding.y * 2 + spacingBetween;
                                float totalWidth = Mathf.Max(titleSize.x, bodySize.x) + tooltipPadding.x * 2;
                                Vector2 tooltipSize = new Vector2(Mathf.Max(totalWidth, tooltipMinWidth), totalHeight);

                                Vector2 screenSize = ImGui.GetIO().DisplaySize;
                                Vector2 tooltipPos = mousePos + offset;

                                // Manage screen limits
                                if (tooltipPos.x + tooltipSize.x > screenSize.x - screenMargin)
                                {
                                    tooltipPos.x = mousePos.x - tooltipSize.x - offset.x;
                                }
                                if (tooltipPos.y + tooltipSize.y > screenSize.y - screenMargin)
                                {
                                    tooltipPos.y = mousePos.y - tooltipSize.y - offset.y;
                                }

                                tooltipPos.x = Mathf.Clamp(tooltipPos.x, screenMargin, screenSize.x - tooltipSize.x - screenMargin);
                                tooltipPos.y = Mathf.Clamp(tooltipPos.y, screenMargin, screenSize.y - tooltipSize.y - screenMargin);

                                // Draw tooltip
                                Fugui.Push(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0.95f));
                                Fugui.Push(ImGuiStyleVar.WindowRounding, tooltipCornerRadius);
                                ImGui.SetNextWindowPos(tooltipPos);
                                ImGui.SetNextWindowSize(tooltipSize);
                                ImGui.BeginTooltip();

                                Vector2 cursorStart = ImGui.GetCursorScreenPos();
                                ImGui.SetCursorScreenPos(cursorStart + tooltipPadding);

                                // Title
                                Fugui.PushFont(14, FontType.Bold);
                                layout.Text(tooltipTitle);
                                Fugui.PopFont();

                                // Space
                                ImGui.SetCursorScreenPos(new Vector2(cursorStart.x + tooltipPadding.x, ImGui.GetCursorScreenPos().y + spacingBetween));

                                // Text
                                Fugui.PushFont(14, FontType.Regular);
                                layout.Text(tooltipBody);
                                Fugui.PopFont();

                                ImGui.EndTooltip();
                                Fugui.PopStyle(2);
                            }
                        }

                        // Double click to load
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && isHovered)
                        {
                            LibraryManager.Instance.SelectFlight(file);
                        }

                        // Thumbnail
                        float thumbMaxWidth = 150f * scale * thumbnailScale;
                        float thumbMaxHeight = itemSize.y - 10f * scale * thumbnailScale;

                        int originalWidth = file.Thumbnail != null ? file.Thumbnail.width : 0;
                        int originalHeight = file.Thumbnail != null ? file.Thumbnail.height : 0;

                        float finalScale = Mathf.Min(thumbMaxWidth / originalWidth, thumbMaxHeight / originalHeight);
                        Vector2 thumbSize = new Vector2(originalWidth, originalHeight) * finalScale;
                        float thumbPadding = 5f * scale * thumbnailScale;

                        Vector2 thumbPosition = itemPos + new Vector2((itemSize.x - thumbSize.x) / 2f, thumbPadding);

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

                            // Inner border
                            float innerBorderThickness = 1f * scale * thumbnailScale;
                            uint innerBorderColor = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));

                            drawListItem.AddRect(
                                thumbPosition + new Vector2(innerBorderThickness * 0.5f, innerBorderThickness * 0.5f),
                                thumbPosition + thumbSize - new Vector2(innerBorderThickness * 0.5f, innerBorderThickness * 0.5f),
                                innerBorderColor,
                                0f,
                                ImDrawFlags.None,
                                innerBorderThickness
                            );
                        }

                        // Icons
                        float iconSize = 16f * scale * thumbnailScale;
                        float iconPadding = 6f * scale * thumbnailScale;

                        Fugui.PushFont(16, FontType.Regular);

                        if (file.Origin == FlightDataOrigin.SharedHash)
                        {
                            Vector2 globePos = thumbPosition + new Vector2(iconPadding, iconPadding);
                            ImGui.SetCursorScreenPos(globePos);
                            layout.Text(FlightReLiveIcons.Globe, FuTextStyle.Default, Fugui.Themes.GetColor(FuColors.Text));
                        }

                        if (!string.IsNullOrEmpty(file.ShareHash))
                        {
                            Vector2 sharePos = thumbPosition + new Vector2(thumbSize.x - iconSize - iconPadding, iconPadding);
                            ImGui.SetCursorScreenPos(sharePos);
                            layout.Text(FlightReLiveIcons.Share, FuTextStyle.Default, Fugui.Themes.GetColor(FuColors.Text));
                        }

                        Fugui.PopFont();

                        // Notification circle
                        if (file.IsNew)
                        {
                            float badgeRadius = 5f * scale * thumbnailScale;
                            float badgeOffset = 6f * scale * thumbnailScale;
                            uint badgeColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
                            Vector2 badgeCenter = thumbPosition + new Vector2(thumbSize.x - badgeRadius - badgeOffset, thumbSize.y - badgeRadius - badgeOffset);
                            drawListItem.AddCircleFilled(badgeCenter, badgeRadius, badgeColor);
                        }

                        // Bottom-left text (duration)
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
                    .Where(k => !LibraryManager.Instance.LoadedFlights.Any(f => f.UniqueKey == k))
                    .ToList()
                    .ForEach(k => _hoverBlendFactors.Remove(k));

                ImGui.SetCursorScreenPos(new Vector2(cursorPos.x, maxY + paddingY));
                ImGui.Dummy(new Vector2(1, 2f));

                Fugui.PopStyle();
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
        /// Called each frame to draw the UI of this window
        /// </summary>
        /// <param name="window"> the window that is drawing this UI</param>
        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            if (_thumbnailScale > 0.5f)
            {
                DrawThumbnailView(window, windowLayout);
            }
            else
            {
                DrawInlineView(window, windowLayout);
            }
        }

        private void DrawTooltip(SerializedFlightData file, FuLayout layout, Vector2 mousePos, float scale)
        {
            string tipTitle = file.Name;
            string tipDate = SettingsManager.FormatDateTime(file.CreationDate);
            string tipBodyLine1 = $"This file contains {file.DataPoints.Count} recorded flight points.";
            string tipBodyLine2 = "Double click to load this flight, right click for more options.";

            Vector2 pad = new Vector2(12f, 8f) * scale;
            float minW = TOOLTIP_MIN_WIDTH * scale;
            float round = BORDER_RADIUS * scale;
            Vector2 offset = new Vector2(16f, 16f) * scale;
            float margin = 4f * scale;

            // Thumbnail info
            float thumbHeight = file.Thumbnail != null ? file.Thumbnail.height * 0.8f * scale : 0f;
            float thumbWidth = file.Thumbnail != null ? file.Thumbnail.width * 0.8f * scale : 0f;

            Vector2 t1 = ImGui.CalcTextSize(tipTitle);
            Vector2 tBody1 = ImGui.CalcTextSize(tipBodyLine1);
            Vector2 tBody2 = ImGui.CalcTextSize(tipBodyLine2);
            Vector2 tDate = ImGui.CalcTextSize(tipDate);
            float totalH = t1.y + tDate.y + tBody1.y + tBody2.y + pad.y * 2f + thumbHeight + (thumbHeight > 0 ? 20f * scale : 0f) + 34f * scale;
            float totalW = Mathf.Max(t1.x, tBody1.x, tBody2.x, tDate.x, thumbWidth) + pad.x * 2f;
            Vector2 size = new(Mathf.Max(totalW, minW), totalH);

            // Manage tooltip screen limits
            Vector2 screen = ImGui.GetIO().DisplaySize;
            Vector2 tipPos = mousePos + offset;

            if (tipPos.x + size.x > screen.x - margin)
            {
                tipPos.x = mousePos.x - size.x - offset.x;
            }
            if (tipPos.y + size.y > screen.y - margin)
            {
                tipPos.y = mousePos.y - size.y - offset.y;
            }

            tipPos.x = Mathf.Clamp(tipPos.x, margin, screen.x - size.x - margin);
            tipPos.y = Mathf.Clamp(tipPos.y, margin, screen.y - size.y - margin);

            Fugui.Push(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0.95f));
            Fugui.Push(ImGuiStyleVar.WindowRounding, round);
            ImGui.SetNextWindowPos(tipPos);
            ImGui.SetNextWindowSize(size);
            ImGui.BeginTooltip();

            Vector2 start = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(start + pad);

            //Title
            Fugui.PushFont(14, FontType.Bold);
            float titleWidth = ImGui.CalcTextSize(tipTitle).x;
            float titleStartX = tipPos.x + (size.x - titleWidth) * 0.5f;
            ImGui.SetCursorScreenPos(new Vector2(titleStartX, ImGui.GetCursorScreenPos().y));
            layout.Text(tipTitle);
            Fugui.PopFont();

            //Separator under title (full width)
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            uint borderColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border));

            //Thumbnail (centered with subtle orange background + thin border)
            if (file.Thumbnail != null)
            {
                IntPtr tex = FuWindow.CurrentDrawingWindow.Container.GetTextureID(file.Thumbnail);
                Vector2 thumbSize = new(thumbWidth, thumbHeight);
                float centerX = tipPos.x + (size.x - thumbSize.x) * 0.5f;
                Vector2 thumbPos = new(centerX, ImGui.GetCursorScreenPos().y + 10f * scale);

                // Background orange (thin, small radius)
                Vector4 orange = Fugui.Themes.GetColor(FuColors.PlotLinesHovered);
                uint orangeBg = ImGui.GetColorU32(orange);
                float bgThickness = 3f * scale;
                drawList.AddRectFilled(thumbPos - new Vector2(bgThickness, bgThickness), thumbPos + thumbSize + new Vector2(bgThickness, bgThickness), orangeBg, 3f * scale);

                //Image
                ImGui.SetCursorScreenPos(thumbPos);
                ImGui.Image(tex, thumbSize);

                //Thin black border around thumbnail
                uint black = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));
                drawList.AddRect(thumbPos - new Vector2(bgThickness, bgThickness), thumbPos + thumbSize + new Vector2(bgThickness, bgThickness), black, 3f * scale, ImDrawFlags.None, 1f * scale);

                //Date centered under the thumbnail
                float dateWidth = ImGui.CalcTextSize(tipDate).x;
                float dateX = tipPos.x + (size.x - dateWidth) * 0.5f;
                float dateY = thumbPos.y + thumbSize.y + 10f * scale;
                ImGui.SetCursorScreenPos(new Vector2(dateX, dateY));
                Fugui.PushFont(13, FontType.Regular);
                layout.Text(tipDate);
                Fugui.PopFont();

                //Separator under date (full width)
                Vector2 sep2Start = new Vector2(tipPos.x, dateY + tDate.y + 4f * scale);
                Vector2 sep2End = new Vector2(tipPos.x + size.x, sep2Start.y);
                drawList.AddLine(sep2Start, sep2End, borderColor, 1f * scale);

                //Move cursor for description text below
                ImGui.SetCursorScreenPos(new Vector2(tipPos.x + pad.x, sep2Start.y + 10f * scale));
            }
            else
            {
                //No thumbnail - date directly below title
                float dateWidth = ImGui.CalcTextSize(tipDate).x;
                float dateX = tipPos.x + (size.x - dateWidth) * 0.5f;
                ImGui.SetCursorScreenPos(new Vector2(dateX, ImGui.GetCursorScreenPos().y + 10f * scale));
                Fugui.PushFont(13, FontType.Regular);
                layout.Text(tipDate);
                Fugui.PopFont();

                ImGui.SetCursorScreenPos(new Vector2(tipPos.x + pad.x, ImGui.GetCursorScreenPos().y + 8f * scale));
            }

            //Description text lines (centered + proper bottom padding)
            Fugui.PushFont(14, FontType.Regular);

            float line1Width = ImGui.CalcTextSize(tipBodyLine1).x;
            float line2Width = ImGui.CalcTextSize(tipBodyLine2).x;
            float centerLine1 = tipPos.x + (size.x - line1Width) * 0.5f;
            float centerLine2 = tipPos.x + (size.x - line2Width) * 0.5f;

            ImGui.SetCursorScreenPos(new Vector2(centerLine1, ImGui.GetCursorScreenPos().y));
            layout.Text(tipBodyLine1);
            ImGui.SetCursorScreenPos(new Vector2(centerLine2, ImGui.GetCursorScreenPos().y + tBody1.y + 2f * scale));
            layout.Text(tipBodyLine2);

            Fugui.PopFont();

            //Add bottom spacing for breathing room
            ImGui.Dummy(new Vector2(0, 6f * scale));

            ImGui.EndTooltip();
            Fugui.PopStyle(2);
        }

        /// <summary>
        /// Draw flight item contextual menu
        /// </summary>
        /// <param name="window"></param>
        /// <param name="flight"></param>
        private void DrawContextualMenu(SerializedFlightData flight)
        {
            FuContextMenuBuilder contextMenuBuilder = FuContextMenuBuilder.Start();
            contextMenuBuilder.AddTitle(flight.Name);
            contextMenuBuilder.AddItem($"{FlightReLiveIcons.Play}   Load", () =>
            {
                LibraryManager.Instance.SelectFlight(flight);
            });
            contextMenuBuilder.AddItem($"{FlightReLiveIcons.MapPin}   Display in OpenStreetMap", () =>
            {
                OpenStreetMapHelper.OpenOpenStreetMapBrowser(flight.DataPoints.Select(p => p.Coordinate).ToList());
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
            contextMenuBuilder.AddSeparator();
            if (!string.IsNullOrEmpty(flight.ShareHash))
            {
                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Share}  Copy SharedHash", () =>
                {
                    ImGui.SetClipboardText(flight.ShareHash);
                    Fugui.Notify("SharedHash added to clipboard", $"{flight.Name} SharedHash automaticaly added to clipboard.", StateType.Info, 5f);
                });
            }
            else
            {
                contextMenuBuilder.AddItem($"{FlightReLiveIcons.Share}   Share", () =>
                {
                    ShareViewManager.DisplayShareModal(flight);
                });
            }
            List<FuContextMenuItem> contextMenuItems = contextMenuBuilder.Build();
            Fugui.PushContextMenuItems(contextMenuItems);
            Fugui.TryOpenContextMenu();
            Fugui.PopContextMenuItems();
        }
        #endregion
    }
}
