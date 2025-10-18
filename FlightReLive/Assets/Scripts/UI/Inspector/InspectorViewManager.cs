using FlightReLive.Core.Database;
using FlightReLive.Core.Loading;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace FlightReLive.UI.Inspector
{
    /// <summary>
    /// Manager responsible for metadata display.
    /// </summary>
    internal class InspectorViewManager : FuWindowBehaviour
    {
        #region CONSTANTS
        private const float TOP_BAR_HEIGHT = 26f;
        #endregion

        #region PROPERTIES
        internal static InspectorViewManager Instance { get; private set; }
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
        }
        #endregion

        #region UI
        public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
            windowDefinition.SetHeaderUI(DrawHeader, TOP_BAR_HEIGHT);
            windowDefinition.SetUI(OnUI);
        }

        private void DrawHeader(FuWindow window, Vector2 size)
        {
            FlightData currentFlightData = LoadingManager.Instance.CurrentFlightData;
            float scale = Fugui.CurrentContext.Scale;
            size.y = TOP_BAR_HEIGHT * scale;
            FuLayout layout = new FuLayout();
            FuStyle customStyle = new FuStyle(FuTextStyle.Default, FuFrameStyle.Default, new FuPanelStyle(Fugui.Themes.GetColor(FuColors.MenuBarBg), Fugui.Themes.GetColor(FuColors.Border)), FuStyle.Unpadded.FramePadding, FuStyle.Unpadded.WindowPadding);

            using (FuPanel inspectorPanel = new FuPanel("inspectorPanel", customStyle, false, TOP_BAR_HEIGHT, window.WorkingAreaSize.x, FuPanelFlags.NoScroll))
            {
                Fugui.Push(ImGuiCol.MenuBarBg, Fugui.Themes.GetColor(FuColors.Border));
                layout.Spacing();
                layout.SameLine();

                if (LoadingManager.Instance.IsLoaded)
                {
                    Fugui.PushFont(12, FontType.Bold);
                    Vector2 textSize = ImGui.CalcTextSize(currentFlightData.Name);
                    float verticalOffset = (size.y - textSize.y) / 2f;
                    Fugui.MoveY(verticalOffset);
                    layout.CenterNextItemH(currentFlightData.Name);
                    layout.Text(currentFlightData.Name);
                    Fugui.PopFont();
                }

                Fugui.PopColor();
            }

            layout.Dispose();
        }

        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            if (LoadingManager.Instance.IsLoaded)
            {
                using (FuLayout layout = new FuLayout())
                {
                    MetadataViewManager.Instance.DrawMetadata(window, layout);
                }
            }
        }
        #endregion
    }
}
