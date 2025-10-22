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
        private const float RIGHT_PADDING = 10f;
        private const float TOOLTIP_MIN_WIDTH = 300f;
        private const float ICON_PADDING = 10f;
        private const float ICON_SIZE = 15f;
        private const float BORDER_RADIUS = 8f;
        private const float ITEM_LINE_HEIGHT = 26f;
        private const float ITEM_TOP_PADDING = 3f;
        private const float ITEM_LEFT_PADDING = 10f;
        private const float NOTIFICATION_CIRCLE_RADIUS = 4f;
        private const float NOTIFICATION_CIRCLE_SPACING = 4f;
        private const float THUMBNAIL_BORDER_THICKNESS = 1f;
        private const float ITEM_INLINE_TEXT_WIDTH = 200f;
        #endregion

        #region ATTIBUTES
        private string _filterWord = "";
        private bool _filterOnlyNew = false;
        private FlightDataOrigin _filteredOrigin = FlightDataOrigin.All;
        private ShareFilter _filteredShareState = ShareFilter.All;
        private List<string> _filterIcons = new List<string>() { FlightReLiveIcons.All, FlightReLiveIcons.Globe, FlightReLiveIcons.Database, FlightReLiveIcons.Share, FlightReLiveIcons.Circle };
        private List<string> tooltips = new List<string> { "Show all flights.", "Show imported shared flights.", "Show local flights only.", "Show local flights that are shared.", "Show only new flights." };
        private List<string> _dispositionIcons = new List<string>() { FlightReLiveIcons.List, FlightReLiveIcons.Thumbnail };
        private ItemDisposition _currentDisposition = ItemDisposition.Inline;
        private readonly Dictionary<string, float> _hoverBlendFactors = new Dictionary<string, float>();
        #endregion

        #region ENUM
        private enum ShareFilter
        {
            All,
            Shared,
            NotShared
        }

        private enum ItemDisposition
        {
            Inline,
            Thumbnail
        }
        #endregion

        #region UNITY METHODS
        public void Start()
        {
            SettingsManager.OnDateFormatStyleChanged += OnDateFormatStyleChanged;
            SettingsManager.OnTimeFormatStyleChanged += OnTimeFormatStyleChanged;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFlightUnloaded;
            LibraryManager.Instance.OnLibraryLoading += OnLibraryLoading;
        }

        private void OnDestroy()
        {
            SettingsManager.OnDateFormatStyleChanged -= OnDateFormatStyleChanged;
            SettingsManager.OnTimeFormatStyleChanged -= OnTimeFormatStyleChanged;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded -= OnFlightUnloaded;
            LibraryManager.Instance.OnLibraryLoading -= OnLibraryLoading;
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
        private void OnLibraryLoading(float progress)
        {
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
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
        #endregion

        #region UI

        /// <summary>
        /// Called each frame to draw the UI of this window
        /// </summary>
        /// <param name="window"> the window that is drawing this UI</param>
        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            switch (_currentDisposition)
            {
                case ItemDisposition.Inline:
                    DrawInlineView(window, windowLayout);
                    break;
                case ItemDisposition.Thumbnail:
                    DrawThumbnailView(window, windowLayout);
                    break;
            }
        }

        /// <summary>
        /// Draw library window header with unified filter and responsive search bar
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

                    float availableWidth = window.WorkingAreaSize.x - (paddingLeft + paddingRight);
                    float groupHeight = frameHeight * 1.1f;
                    float groupY = (HEADER_BAR_HEIGHT * scale - groupHeight) * 0.5f;
                    float groupWidth = (_filterIcons.Count * (frameHeight * 1.5f)) + (HORIZONTAL_PADDING * scale);
                    float searchFieldWidth = iconSize.x + spacing + searchWidth;

                    bool canDisplayGroup = availableWidth >= groupWidth;
                    bool canDisplaySearch = availableWidth >= groupWidth + searchFieldWidth + (HORIZONTAL_PADDING * scale);

                    //Filters group
                    if (canDisplayGroup)
                    {
                        ImGui.SetCursorPos(new Vector2(paddingLeft, groupY));

                        FuButtonsGroupStyle buttonStyle = FuButtonsGroupStyle.Default;
                        Vector2 iconPadding = new Vector2(6f * scale, 2f * scale);
                        Vector2 groupStart = ImGui.GetCursorScreenPos();

                        layout.ButtonsGroup<string>(
                            "##libraryUnifiedFilterButtons",
                            _filterIcons,
                            (int index) =>
                            {
                                switch (index)
                                {
                                    case 0: // All
                                        _filteredOrigin = FlightDataOrigin.All;
                                        _filteredShareState = ShareFilter.All;
                                        _filterOnlyNew = false;
                                        break;

                                    case 1: // Globe
                                        _filteredOrigin = FlightDataOrigin.SharedHash;
                                        _filteredShareState = ShareFilter.All;
                                        _filterOnlyNew = false;
                                        break;

                                    case 2: // Database
                                        _filteredOrigin = FlightDataOrigin.Local;
                                        _filteredShareState = ShareFilter.All;
                                        _filterOnlyNew = false;
                                        break;

                                    case 3: // Share
                                        _filteredOrigin = FlightDataOrigin.Local;
                                        _filteredShareState = ShareFilter.Shared;
                                        _filterOnlyNew = false;
                                        break;

                                    case 4: // Circle (new only)
                                        _filteredOrigin = FlightDataOrigin.All;
                                        _filteredShareState = ShareFilter.All;
                                        _filterOnlyNew = true;
                                        break;

                                    default:
                                        _filteredOrigin = FlightDataOrigin.All;
                                        _filteredShareState = ShareFilter.All;
                                        _filterOnlyNew = false;
                                        break;
                                }

                                Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
                            },
                            () =>
                            {
                                if (_filterOnlyNew)
                                {
                                    return FlightReLiveIcons.Circle;
                                }

                                if (_filteredOrigin == FlightDataOrigin.SharedHash)
                                {
                                    return FlightReLiveIcons.Globe;
                                }

                                if (_filteredOrigin == FlightDataOrigin.Local && _filteredShareState == ShareFilter.Shared)
                                {
                                    return FlightReLiveIcons.Share;
                                }

                                if (_filteredOrigin == FlightDataOrigin.Local)
                                {
                                    return FlightReLiveIcons.Database;
                                }

                                return FlightReLiveIcons.All;
                            },
                            width: groupWidth,
                            padding: iconPadding,
                            flags: FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons,
                            style: buttonStyle
                        );

                        //Button group tooltip display
                        ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
                        float buttonWidth = groupWidth / _filterIcons.Count;
                        Vector2 buttonSize = new Vector2(buttonWidth, frameHeight * 1.2f);

                        for (int i = 0; i < _filterIcons.Count; i++)
                        {
                            Vector2 btnMin = groupStart + new Vector2(i * buttonWidth, 0f);
                            Vector2 btnMax = btnMin + buttonSize;

                            if (ImGui.IsMouseHoveringRect(btnMin, btnMax))
                            {
                                float pad = 4f * scale;
                                float round = Fugui.Themes.WindowRounding * scale;
                                float borderThickness = Fugui.Themes.WindowBorderSize * scale;

                                Vector2 textSize = ImGui.CalcTextSize(tooltips[i]);
                                Vector2 tooltipSize = textSize + new Vector2(pad * 2f, pad * 2f);
                                Vector2 mousePos = ImGui.GetMousePos();
                                Vector2 tipPos = new Vector2(mousePos.x + 16f * scale, mousePos.y + 16f * scale);

                                uint bgColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.WindowBg));
                                uint borderColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border));
                                uint textColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Text));

                                //Background & border
                                drawList.AddRectFilled(tipPos, tipPos + tooltipSize, bgColor, round);
                                drawList.AddRect(tipPos, tipPos + tooltipSize, borderColor, round, ImDrawFlags.None, borderThickness);

                                //Horizontal text aligned
                                float textCenteredX = tipPos.x + (tooltipSize.x - textSize.x) * 0.5f;
                                Vector2 textPos = new Vector2(textCenteredX + pad, tipPos.y + pad);

                                Fugui.PushFont(13, FontType.Regular);
                                drawList.AddText(textPos, textColor, tooltips[i]);
                                Fugui.PopFont();

                                break;
                            }
                        }

                    }

                    //Input filter / search
                    if (canDisplaySearch)
                    {
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
                    }

                    Fugui.PopColor();
                }
            }
        }

        /// <summary>
        /// Draw library window footer with responsive disposition buttons and item count
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
                    float paddingLeft = HORIZONTAL_PADDING * scale;
                    float paddingRight = HORIZONTAL_PADDING * scale;
                    float availableWidth = size.x - (paddingLeft + paddingRight);
                    float groupWidth = (_dispositionIcons.Count * (frameHeight * 1.5f)) + (HORIZONTAL_PADDING * scale);

                    // Count text width (approximation via ImGui)
                    int totalCount = LibraryManager.Instance.LoadedFlights.Count;
                    int visibleCount = GetFilteredFlights().Count();
                    int hiddenCount = totalCount - visibleCount;

                    string countText = $"{visibleCount} visible - {hiddenCount} hidden";
                    Vector2 countTextSize = ImGui.CalcTextSize(countText);
                    float countTextWidth = countTextSize.x + (HORIZONTAL_PADDING * scale);

                    bool canShowGroup = availableWidth >= groupWidth;
                    bool canShowCounter = availableWidth >= groupWidth + countTextWidth;

                    //If toggle button group can be displayed
                    if (canShowGroup)
                    {
                        float groupHeight = frameHeight * 1.1f;
                        float groupY = (FOOTER_BAR_HEIGHT * scale - groupHeight) * 0.5f;
                        ImGui.SetCursorPos(new Vector2(paddingLeft, groupY));

                        FuButtonsGroupStyle buttonStyle = FuButtonsGroupStyle.Default;
                        Vector2 iconPadding = new Vector2(6f * scale, 2f * scale);

                        layout.ButtonsGroup<string>(
                            "##libraryFooterDispositionButtons",
                            _dispositionIcons,
                            (int index) =>
                            {
                                switch (index)
                                {
                                    case 0:
                                        _currentDisposition = ItemDisposition.Inline;
                                        break;

                                    case 1:
                                        _currentDisposition = ItemDisposition.Thumbnail;
                                        break;

                                    default:
                                        _currentDisposition = ItemDisposition.Inline;
                                        break;
                                }

                                Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
                            },
                            () =>
                            {
                                switch (_currentDisposition)
                                {
                                    default:
                                    case ItemDisposition.Inline:
                                        return FlightReLiveIcons.List;

                                    case ItemDisposition.Thumbnail:
                                        return FlightReLiveIcons.Thumbnail;
                                }
                            },
                            width: groupWidth * 0.9f,
                            padding: iconPadding,
                            flags: FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons,
                            style: buttonStyle
                        );
                    }

                    //Count filtered text
                    if (canShowCounter)
                    {
                        Fugui.PushFont(14, FontType.Regular);

                        float textY = (FOOTER_BAR_HEIGHT * scale - countTextSize.y) * 0.5f;
                        float textX = size.x - countTextWidth - paddingRight;

                        ImGui.SetCursorPos(new Vector2(textX, textY));
                        layout.Text(countText, FuTextStyle.Default, Fugui.Themes.GetColor(FuColors.Text));

                        Fugui.PopFont();
                    }

                    Fugui.PopColor();
                }
            }
        }

        /// <summary>
        /// Get filtered flights list results with current filter applies
        /// </summary>
        /// <returns></returns>
        private IOrderedEnumerable<SerializedFlightData> GetFilteredFlights()
        {
            return LibraryManager.Instance.LoadedFlights
                .Where(f =>
                    (string.IsNullOrEmpty(_filterWord) || f.Name.Contains(_filterWord, StringComparison.OrdinalIgnoreCase)) &&
                    // Origin filter
                    (_filteredOrigin == FlightDataOrigin.All || f.Origin == _filteredOrigin) &&
                    // Shared / NotShared filter
                    (_filteredShareState == ShareFilter.All ||
                    (_filteredShareState == ShareFilter.Shared && !string.IsNullOrEmpty(f.ShareHash)) ||
                    (_filteredShareState == ShareFilter.NotShared && string.IsNullOrEmpty(f.ShareHash))) &&
                    // "New only" filter (Circle icon)
                    (!_filterOnlyNew || f.IsNew)
                )
                .OrderBy(f => f.Name);
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
            float paddingY = ITEM_TOP_PADDING * scale;
            float paddingX = ITEM_LEFT_PADDING * scale;
            float circleRadius = NOTIFICATION_CIRCLE_RADIUS * scale;
            float circleSpacing = NOTIFICATION_CIRCLE_SPACING * scale;

            using FuPanel panel = new FuPanel("libraryInlinePanel", false, contentRegion.y, contentRegion.x, FuPanelFlags.Default);
            Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);

            int totalCount = LibraryManager.Instance.LoadedFlights.Count;
            int visibleCount = GetFilteredFlights().Count();
            int hiddenCount = totalCount - visibleCount;

            if (visibleCount == 0 && hiddenCount != 0)
            {
                using (FuLayout layout = new FuLayout())
                {
                    string filterText = "No flights found with active filters.";
                    Fugui.PushFont(14, FontType.Bold);
                    layout.CenterNextItemHV(filterText);
                    layout.Text(filterText);
                    Fugui.PopFont();
                }
                Fugui.PopStyle();
                return;
            }
            else if (visibleCount == 0 && hiddenCount == 0)
            {
                using (FuLayout layout = new FuLayout())
                {
                    string introText = "Import a flight from the 'Import' menu to start to ReLive!";
                    Fugui.PushFont(14, FontType.Bold);
                    layout.CenterNextItemHV(introText);
                    layout.Text(introText);
                    Fugui.PopFont();
                }
                Fugui.PopStyle();
                return;
            }

            Vector2 cursor = ImGui.GetCursorScreenPos();
            float y = cursor.y + paddingY;
            float availableWidth = contentRegion.x;
            float minWidthForIcons = ITEM_INLINE_TEXT_WIDTH * scale;
            bool showIcons = availableWidth >= minWidthForIcons;

            foreach (SerializedFlightData file in GetFilteredFlights())
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

                    //Colors
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
                        Vector2 notificationCircle = new(textStartX, itemPos.y + itemSize.y * 0.5f);
                        uint innerColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
                        uint borderColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HoveredWindowTab));
                        float borderThickness = 1f * scale;

                        drawList.AddCircleFilled(notificationCircle, circleRadius, innerColor);
                        drawList.AddCircle(notificationCircle, circleRadius + (borderThickness * 0.5f), borderColor, 0, borderThickness);
                    }

                    //Flight name
                    float nameStartX = textStartX + circleRadius * 2f + circleSpacing;
                    ImGui.SetCursorScreenPos(new Vector2(nameStartX, itemPos.y + (lineHeight - ImGui.GetTextLineHeight()) * 0.5f));
                    Fugui.PushFont(13, FontType.Bold);
                    layout.Text(file.Name, new FuElementSize(ITEM_INLINE_TEXT_WIDTH, ImGui.GetTextLineHeight()), FuTextWrapping.Clip);
                    Fugui.PopFont();

                    //Right icons
                    if (showIcons)
                    {
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
                    }

                    //Contextual menu
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

            //Cleanup blend table
            _hoverBlendFactors.Keys
                .Where(k => !LibraryManager.Instance.LoadedFlights.Any(f => f.UniqueKey == k))
                .ToList()
                .ForEach(k => _hoverBlendFactors.Remove(k));

            ImGui.SetCursorScreenPos(new Vector2(cursor.x, y));
            ImGui.Dummy(new Vector2(contentRegion.x, 0f));

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
            Vector2 contentRegion = new(windowLayout.GetAvailableWidth(), windowLayout.GetAvailableHeight() / scale);

            using (FuPanel panel = new FuPanel("libraryUIPanel", false, contentRegion.y, contentRegion.x, flags: FuPanelFlags.Default))
            {
                Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);
                int totalCount = LibraryManager.Instance.LoadedFlights.Count;
                int visibleCount = GetFilteredFlights().Count();
                int hiddenCount = totalCount - visibleCount;

                if (visibleCount == 0 && hiddenCount != 0)
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        string filterText = "No flights found with active filters.";
                        Fugui.PushFont(14, FontType.Bold);
                        layout.CenterNextItemHV(filterText);
                        layout.Text(filterText);
                        Fugui.PopFont();
                    }
                    Fugui.PopStyle();
                    return;
                }
                else if (visibleCount == 0 && hiddenCount == 0)
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        string introText = "Import a flight from the 'Import' menu to start to ReLive!";
                        Fugui.PushFont(14, FontType.Bold);
                        layout.CenterNextItemHV(introText);
                        layout.Text(introText);
                        Fugui.PopFont();
                    }
                    Fugui.PopStyle();
                    return;
                }

                Vector2 itemBaseSize = new Vector2(160, 95);
                Vector2 itemSize = itemBaseSize * scale;
                float circleRadius = NOTIFICATION_CIRCLE_RADIUS * scale;
                float circleSpacing = NOTIFICATION_CIRCLE_SPACING * scale;
                float paddingX = 16f * scale;
                float paddingY = 16f * scale;

                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                float x = cursorPos.x;
                float y = cursorPos.y + paddingY;
                float maxY = y;

                float scrollbarWidth = Fugui.Themes.ScrollbarSize;
                float visibleContentWidth = contentRegion.x - scrollbarWidth - 2f * scale;

                foreach (SerializedFlightData file in GetFilteredFlights())
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
                        float cornerRadius = 4f * scale;
                        FuguiDrawListHelper.DrawRoundedRect(drawListItem, itemPos, itemSize, bgColor, cornerRadius, 5);

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

                        //Thumbnail
                        float thumbMaxWidth = 150f * scale;
                        float thumbMaxHeight = itemSize.y - 10f * scale;

                        int originalWidth = file.Thumbnail != null ? file.Thumbnail.width : 0;
                        int originalHeight = file.Thumbnail != null ? file.Thumbnail.height : 0;

                        float finalScale = Mathf.Min(thumbMaxWidth / originalWidth, thumbMaxHeight / originalHeight);
                        Vector2 thumbSize = new Vector2(originalWidth, originalHeight) * finalScale;
                        float thumbPadding = 5f * scale;

                        Vector2 thumbPosition = itemPos + new Vector2((itemSize.x - thumbSize.x) / 2f, thumbPadding);

                        if (file.Thumbnail != null)
                        {
                            IntPtr textureID = FuWindow.CurrentDrawingWindow.Container.GetTextureID(file.Thumbnail);
                            ImGui.SetCursorScreenPos(thumbPosition);
                            ImGui.Image(textureID, thumbSize);

                            float borderThickness = THUMBNAIL_BORDER_THICKNESS * scale;

                            drawListItem.AddRectFilled(thumbPosition, thumbPosition + new Vector2(thumbSize.x, borderThickness), bgColor);
                            drawListItem.AddRectFilled(thumbPosition + new Vector2(0f, thumbSize.y - borderThickness), thumbPosition + new Vector2(thumbSize.x, thumbSize.y), bgColor);
                            drawListItem.AddRectFilled(thumbPosition, thumbPosition + new Vector2(borderThickness, thumbSize.y), bgColor);
                            drawListItem.AddRectFilled(thumbPosition + new Vector2(thumbSize.x - borderThickness, 0f), thumbPosition + new Vector2(thumbSize.x, thumbSize.y), bgColor);

                            // Inner border
                            float innerBorderThickness = 1f * scale;
                            uint innerBorderColor = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));

                            drawListItem.AddRect(thumbPosition + new Vector2(innerBorderThickness * 0.5f, innerBorderThickness * 0.5f), thumbPosition + thumbSize - new Vector2(innerBorderThickness * 0.5f, innerBorderThickness * 0.5f), innerBorderColor, 0f, ImDrawFlags.None, innerBorderThickness);
                        }

                        //Icons
                        float iconSize = 16f * scale;
                        float iconPadding = 6f * scale;

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

                        //Notification circle
                        if (file.IsNew)
                        {
                            uint innerColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PlotLinesHovered));
                            uint borderColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HoveredWindowTab));
                            float borderThickness = 1f * scale;

                            Vector2 notificationCircle = thumbPosition + new Vector2(thumbSize.x - circleRadius - circleSpacing, thumbSize.y - circleRadius - circleSpacing);
                            drawListItem.AddCircleFilled(notificationCircle, circleRadius, innerColor);
                            drawListItem.AddCircle(notificationCircle, circleRadius + (borderThickness * 0.5f), borderColor, 0, borderThickness);
                        }

                        // Bottom-left text (duration)
                        if (file.Duration != null)
                        {
                            Fugui.PushFont(12, FontType.Regular);

                            string duration = file.Duration.ToString(@"hh\:mm\:ss");
                            Vector2 textSize = ImGui.CalcTextSize(duration);
                            Vector2 padding = new Vector2(4f, 2f) * scale;
                            Vector2 bgSize = textSize + padding * 2;
                            Vector2 pos = thumbPosition + new Vector2(4f * scale, thumbSize.y - bgSize.y - 4f * scale);
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

                //Hover effect fading clear
                _hoverBlendFactors.Keys.Where(k => !LibraryManager.Instance.LoadedFlights.Any(f => f.UniqueKey == k)).ToList().ForEach(k => _hoverBlendFactors.Remove(k));

                ImGui.SetCursorScreenPos(new Vector2(cursorPos.x, maxY + paddingY));
                ImGui.Dummy(new Vector2(1, 2f));

                Fugui.PopStyle();
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

            //Thumbnail info
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
