using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Workspace;
using FlightReLive.UI.Helpers;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlightReLive.UI.Workspace
{
    public class WorkspaceViewManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float TOP_BAR_HEIGHT = 26f;
        private const float BOTTOM_BAR_HEIGHT = 26f;
        #endregion

        #region ATTIBUTES
        [Header("Thumbnail Settings")]
        [SerializeField][Range(0f, 1f)] private float _thumbnailScale = 1f;

        private string _filterWord = "";
        private bool _workspaceIsLoading = false;
        private float _loadingProgress = 0f;
        #endregion

        #region UNITY METHODS
        public void Start()
        {
            WorkspaceManager.Instance.OnWorkspaceLoading += OnWorkspaceLoading;
            WorkspaceManager.Instance.OnWorkspaceStartLoading += OnWorkspaceStartLoading;
            WorkspaceManager.Instance.OnWorkspaceEndLoading += OnWorkspaceEndLoading;
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded += OnFLightUnloaded;
        }

        private void OnDestroy()
        {
            WorkspaceManager.Instance.OnWorkspaceLoading -= OnWorkspaceLoading;
            WorkspaceManager.Instance.OnWorkspaceStartLoading -= OnWorkspaceStartLoading;
            WorkspaceManager.Instance.OnWorkspaceEndLoading -= OnWorkspaceEndLoading;
            LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            LoadingManager.Instance.OnFlightUnloaded -= OnFLightUnloaded;
        }
        #endregion

        /// <summary>
        /// Whenever the window is created, set the camera to the MouseOrbitImproved component
        /// </summary>
        /// <param name="window"> FuWindow instance</param>
        public override void OnWindowCreated(FuWindow window)
        {
            window.HeaderHeight = TOP_BAR_HEIGHT;
            window.FooterHeight = TOP_BAR_HEIGHT;
            window.HeaderUI = DrawWorkspaceHeader;
            window.FooterUI = DrawWorkspaceFooter;
            window.UI = OnUI;
            _thumbnailScale = SettingsManager.CurrentSettings.WorkspaceZoom;
        }

        #region CALLBACKS
        private void ChangeWorkspace(string[] paths)
        {
            if (paths == null || paths.Length != 1 || !Directory.Exists(paths[0]))
            {
                return;
            }

            SettingsManager.SaveWorkspacePath(paths[0]);
        }

        private void OnWorkspaceStartLoading()
        {
            _workspaceIsLoading = true;
        }

        private void OnWorkspaceLoading(float progress)
        {
            _loadingProgress = progress;
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Workspace);
        }

        private void OnWorkspaceEndLoading()
        {
            _loadingProgress = 1f;
            _workspaceIsLoading = false;
        }

        private void OnFlightEndLoading()
        {
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Workspace);
        }

        private void OnFLightUnloaded()
        {
            Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Workspace);
        }
        #endregion

        #region UI
        private void DrawWorkspaceHeader(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            size.y = TOP_BAR_HEIGHT * scale;
            float unscaledHeight = size.y / scale;
            Fugui.PushFont(14, FontType.Regular);
            Vector2 buttonSize = Fugui.CalcTextSize("Select Workspace", FuTextWrapping.Wrap);
            Fugui.PopFont();
            FuLayout layout = new FuLayout();

            FuStyle customStyle = new FuStyle(
                FuTextStyle.Default,
                FuFrameStyle.Default,
                new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)),
                FuStyle.Unpadded.FramePadding,
                FuStyle.Unpadded.WindowPadding);

            using (FuPanel panel = new FuPanel("workspaceSettingsPanel", customStyle, false, window.HeaderHeight, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));
                Fugui.MoveY(3f);
                ImGui.BeginGroup();
                layout.Spacing();
                layout.SameLine();
                layout.SetNextElementToolTip("Select a new workspace");
                Fugui.PushFont(14, FontType.Regular);

                if (layout.Button(FlightReLiveIcons.Workspace + " Select Workspace", new FuElementSize(buttonSize.x + 24f, unscaledHeight - 6f), Vector2.zero, Vector2.zero, Fugui.Themes.CurrentTheme.ButtonsGradientStrenght, FuButtonStyle.Info, false))
                {
                    string safePath = Path.Combine(Application.persistentDataPath, "Captures");
                    FileBrowser.OpenFolderPanelAsync("Select a FlightReLive Workspace", safePath, false, ChangeWorkspace);
                }

                Fugui.PopFont();
                layout.SameLine();

                float panelWidth = window.WorkingAreaSize.x;
                float spacing = Fugui.Themes.CurrentTheme.ItemSpacing.x * scale;
                float offsetX = 2f;
                float searchWidth = 200f * scale;
                Vector2 iconSize = new Vector2(14f, 14f);
                float totalWidth = buttonSize.x + 24f + spacing + iconSize.x + spacing + searchWidth + offsetX + 5f;
                float searchX = panelWidth - totalWidth + buttonSize.x + 24f + spacing;

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
                    layout.TextInput("##workspaceSearchPanel0", "", ref _filterWord, 128, width: searchWidth);
                    ImGui.Dummy(new Vector2(1, 1));
                    Fugui.PopStyle(2);
                }

                ImGui.EndGroup();
                Fugui.PopColor();
            }

            layout.Dispose();
        }

        private void DrawWorkspaceFooter(FuWindow window, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            float footerHeight = BOTTOM_BAR_HEIGHT * scale;
            float unscaledHeight = footerHeight / scale;

            // Positionner le curseur tout en bas
            Vector2 screenSize = ImGui.GetIO().DisplaySize;
            Vector2 footerPos = new Vector2(
                window.LocalPosition.x,
                screenSize.y - footerHeight
            );

            ImGui.SetCursorScreenPos(footerPos);

            FuStyle footerStyle = new FuStyle(
                FuTextStyle.Default,
                FuFrameStyle.Default,
                new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)),
                FuStyle.Unpadded.FramePadding,
                FuStyle.Unpadded.WindowPadding
            );

            using (FuPanel panel = new FuPanel("workspaceFooterPanel", footerStyle, false, footerHeight, size.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.PopupBg, Fugui.Themes.GetColor(FuColors.Border));
                Fugui.MoveY(3f);
                ImGui.BeginGroup();
                FuLayout layout = new FuLayout();
                layout.Spacing();
                layout.SameLine();

                // Zoom slider thumbnails
                float spacing = Fugui.Themes.CurrentTheme.ItemSpacing.x * scale;
                float sliderWidth = 140f * scale;
                float offsetX = 2f;
                float sliderTotalWidth = spacing + sliderWidth + offsetX + 5f;
                float sliderX = size.x - sliderTotalWidth;

                // Workspace info
                string workspacePath = SettingsManager.CurrentSettings.WorkspacePath ?? "No workspace selected";
                int flights = WorkspaceManager.Instance.LoadedFlights.Count;
                string flightsCount = flights == 1 ? "1 flight" : $"{flights} flights";
                string workspaceText = string.IsNullOrEmpty(SettingsManager.CurrentSettings.WorkspacePath)
                    ? "No workspace defined"
                    : $"Current : {workspacePath} ({flightsCount})";

                // Calcul de la largeur du texte
                Fugui.PushFont(14, FontType.Regular);
                Vector2 workspaceTextSize = ImGui.CalcTextSize(workspaceText);
                Fugui.PopFont();

                float fullWidthNeeded = workspaceTextSize.x + spacing + sliderTotalWidth + 20f;
                float sliderOnlyWidth = sliderTotalWidth + 20f;

                float frameHeight = ImGui.GetFrameHeight();
                float iconOffsetY = (frameHeight - 14f + 2) / 2f;
                float baseY = ImGui.GetCursorPosY() + iconOffsetY;

                if (size.x > fullWidthNeeded)
                {
                    // Text + slider
                    layout.SetNextElementToolTip("Current workspace path");
                    Fugui.PushFont(14, FontType.Regular);
                    layout.Text(workspaceText, FuTextStyle.Default);
                    Fugui.PopFont();
                    layout.SameLine();

                    ImGui.SetCursorPos(new Vector2(sliderX, baseY));
                    layout.SetNextElementToolTip("Adjust thumbnail scale");
                    Fugui.PushFont(14, FontType.Regular);
                    layout.Text(FlightReLiveIcons.DigitalZoom);

                    layout.SameLine();
                    if (layout.Slider("##thumbnailScaleSlider", ref _thumbnailScale, 0f, 1f, 0.01f, format: "%.2f"))
                    {
                        SettingsManager.SaveWorkspaceZoom(_thumbnailScale);
                    }
                }
                else if (size.x > sliderOnlyWidth)
                {
                    // Slider
                    ImGui.SetCursorPos(new Vector2(sliderX, baseY));
                    layout.SetNextElementToolTip("Adjust thumbnail scale");
                    Fugui.PushFont(14, FontType.Regular);
                    if (layout.Slider("##thumbnailScaleSlider", ref _thumbnailScale, 0f, 1f, 0.01f, format: "%.2f"))
                    {
                        SettingsManager.SaveWorkspaceZoom(_thumbnailScale);
                    }
                }

                Fugui.PopFont();
                ImGui.EndGroup();
                Fugui.PopColor();
                layout.Dispose();
            }
        }

        /// <summary>
        /// Called each frame to draw the UI of this window
        /// </summary>
        /// <param name="window"> the window that is drawing this UI</param>
        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            using (FuPanel panel = new FuPanel("workspacePanel", flags: FuPanelFlags.Default))
            {
                float scale = Fugui.CurrentContext.Scale;
                float thumbnailScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(_thumbnailScale));

                if (_workspaceIsLoading)
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        Vector2 screenSize = ImGui.GetContentRegionAvail();
                        float barWidth = 100f * scale;
                        float barHeight = 16f * scale;
                        FuElementSize barSize = new FuElementSize(barWidth, barHeight);

                        Vector2 barPos = new Vector2(
                            screenSize.x / 2f - barWidth / 2f,
                            screenSize.y / 2f - barHeight
                        );

                        ImGui.SetCursorPos(barPos);
                        layout.ProgressBar("Loading Workspace", _loadingProgress, barSize, ProgressBarTextPosition.Inside);

                        string message = "Workspace is loading. Please wait...";
                        Vector2 textSize = ImGui.CalcTextSize(message);
                        Vector2 textPos = new Vector2(
                            screenSize.x / 2f - textSize.x / 2f,
                            barPos.y + barHeight + 12f * scale
                        );

                        ImGui.SetCursorPos(textPos);
                        Fugui.PushFont(14, FontType.Regular);
                        layout.Text(message, FuTextStyle.Default);
                        Fugui.PopFont();
                    }

                    return;
                }

                Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);

                Vector2 itemBaseSize = new Vector2(160, 95);
                Vector2 itemSize = itemBaseSize * scale * thumbnailScale;

                float paddingX = 16f * scale * thumbnailScale;
                float paddingY = 16f * scale * thumbnailScale;

                ImGui.Dummy(new Vector2(1, 10f));
                Vector2 contentRegion = new Vector2(windowLayout.GetAvailableWidth(), windowLayout.GetAvailableHeight() - BOTTOM_BAR_HEIGHT);
                Vector2 cursorPos = ImGui.GetCursorScreenPos();

                float x = cursorPos.x;
                float y = cursorPos.y;
                float maxY = y;

                foreach (FlightFile file in WorkspaceManager.Instance.LoadedFlights
                    .Where(f => string.IsNullOrEmpty(_filterWord) || f.Name.Contains(_filterWord, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.Name))
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        if (LoadingManager.Instance.IsLoading)
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
                        float workspaceTop = windowPos.y + TOP_BAR_HEIGHT;
                        float workspaceBottom = windowPos.y + windowSize.y - BOTTOM_BAR_HEIGHT;

                        bool isHovered = ImGui.IsMouseHoveringRect(itemPos, itemEnd) &&
                                         window.IsHovered &&
                                         mousePos.y > workspaceTop &&
                                         mousePos.y < workspaceBottom;

                        bool isSelected = LoadingManager.Instance.CurrentFlightData?.VideoPath == file.VideoPath;
                        uint bgColor = ImGui.GetColorU32(
                            isSelected
                                ? Fugui.Themes.GetColor(FuColors.Highlight)
                                : (!file.IsValid || file.HasExtractionError)
                                    ? Fugui.Themes.GetColor(FuColors.BackgroundDanger)
                                    : isHovered
                                        ? Fugui.Themes.GetColor(FuColors.HoveredWindowTab)
                                        : Fugui.Themes.GetColor(FuColors.Button)
                        );

                        float cornerRadius = 4f * scale * thumbnailScale;
                        FuguiDrawListHelper.DrawRoundedRect(drawListItem, itemPos, itemSize, bgColor, cornerRadius, 5);

                        if (isHovered)
                        {
                            string tooltipText = $"{file.Name}\n\n";
                            tooltipText += $"{SettingsManager.FormatDateTime(file.CreationDate)}\n\n";
                            if (!file.IsValid)
                            {
                                tooltipText += $"One or more GPS points in this file are missing or corrupted.\nThis file cannot be opened (possibly due to part of the flight being indoors).";
                            }
                            else if (file.HasExtractionError)
                            {
                                tooltipText = $"Error encountered while extracting video data.\nVideo file format is incompatible or not from a DJI drone.";
                            }
                            else
                            {
                                tooltipText += $"Double click to load this video file.\nThis file contains {file.DataPoints.Count} recorded flight points.\n\nClick on {FlightReLiveIcons.GoogleMaps} to display waypoints on Google Maps.";
                            }

                            Vector2 tooltipPadding = new Vector2(12f, 8f) * scale * thumbnailScale;
                            float tooltipMinWidth = 300f * scale * thumbnailScale;
                            float tooltipCornerRadius = 8f * scale * thumbnailScale;
                            Vector2 offset = new Vector2(16f, 16f) * scale * thumbnailScale;
                            float screenMargin = 4f * scale * thumbnailScale;

                            Vector2 textSize = ImGui.CalcTextSize(tooltipText);
                            Vector2 tooltipSize = new Vector2(Mathf.Max(textSize.x + tooltipPadding.x * 2, tooltipMinWidth), textSize.y + tooltipPadding.y * 2);
                            Vector2 screenSize = ImGui.GetIO().DisplaySize;
                            Vector2 tooltipPos = mousePos + offset;

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

                            Fugui.Push(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0.95f));
                            Fugui.Push(ImGuiStyleVar.WindowRounding, tooltipCornerRadius);

                            ImGui.SetNextWindowPos(tooltipPos);
                            ImGui.SetNextWindowSize(tooltipSize);
                            ImGui.BeginTooltip();
                            ImGui.SetCursorScreenPos(tooltipPos + tooltipPadding);

                            Fugui.PushFont(14, FontType.Regular);
                            layout.Text(tooltipText, file.IsValid && !file.HasExtractionError ? FuTextStyle.Default : FuTextStyle.Danger);
                            Fugui.PopFont();

                            ImGui.EndTooltip();
                            Fugui.PopStyle(2);
                        }

                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && isHovered && file.IsValid && !file.HasExtractionError)
                        {
                            WorkspaceManager.Instance.SelectFlight(file);
                        }

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

                            float borderThickness = 2f * scale * thumbnailScale;
                            drawListItem.AddRectFilled(thumbPosition, thumbPosition + new Vector2(thumbSize.x, borderThickness), bgColor);
                            drawListItem.AddRectFilled(thumbPosition + new Vector2(0f, thumbSize.y - borderThickness), thumbPosition + new Vector2(thumbSize.x, thumbSize.y), bgColor);
                            drawListItem.AddRectFilled(thumbPosition, thumbPosition + new Vector2(borderThickness, thumbSize.y), bgColor);
                            drawListItem.AddRectFilled(thumbPosition + new Vector2(thumbSize.x - borderThickness, 0f), thumbPosition + new Vector2(thumbSize.x, thumbSize.y), bgColor);
                        }

                        if (file.Duration != null)
                        {
                            Fugui.PushFont(12, FontType.Regular);
                            string duration = file.Duration.ToString(@"hh\:mm\:ss");
                            Vector2 textSize = ImGui.CalcTextSize(duration);
                            Vector2 padding = new Vector2(4f, 2f) * scale * thumbnailScale;
                            Vector2 bgSize = textSize + padding * 2;
                            Vector2 pos = thumbPosition + new Vector2(4f * scale * thumbnailScale, 4f * scale * thumbnailScale);
                            Vector2 finalPos = pos + new Vector2((bgSize.x - textSize.x) / 2f, padding.y);
                            drawListItem.AddRectFilled(pos, pos + bgSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.9f)));
                            ImGui.SetCursorScreenPos(finalPos);
                            layout.Text(duration, FuTextStyle.Default);
                            Fugui.PopFont();
                        }

                        //Right top icon
                        Fugui.PushFont(12, FontType.Regular);
                        Vector2 iconSize = file.IsValid ? ImGui.CalcTextSize(FlightReLiveIcons.GoogleMaps) : ImGui.CalcTextSize(FlightReLiveIcons.Warning);
                        Vector2 iconPadding = new Vector2(4f, 2f) * scale * thumbnailScale;
                        Vector2 iconBgSize = iconSize + iconPadding * 2;
                        Vector2 iconPos = thumbPosition + new Vector2(thumbSize.x - iconBgSize.x - 4f * scale * thumbnailScale, 4f * scale * thumbnailScale);
                        Vector2 iconFinalPosition = iconPos + new Vector2((iconBgSize.x - iconSize.x) / 2f, iconPadding.y);
                        drawListItem.AddRectFilled(iconPos, iconPos + iconBgSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.9f)));
                        ImGui.SetCursorScreenPos(iconFinalPosition);

                        if (file.IsValid && !file.HasExtractionError)
                        {
                            if (layout.ClickableText(FlightReLiveIcons.GoogleMaps, FuTextStyle.Default))
                            {
                                GoogleAPIHelper.OpenGoogleMapsBrowser(file.DataPoints.Select(p => new Vector2((float)p.Latitude, (float)p.Longitude)).ToList());
                            }
                        }
                        else
                        {
                            layout.Text(FlightReLiveIcons.Warning, FuTextStyle.Danger);
                        }

                        Fugui.PopFont();

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
