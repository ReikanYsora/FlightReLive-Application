using ImGuiNET;
using UnityEngine;

namespace FlightReLive.UI.Helpers
{
    public class FuguiDrawListHelper : MonoBehaviour
    {
        #region METHODS
        internal static void DrawRoundedRect(
            ImDrawListPtr drawList,
            Vector2 pos,
            Vector2 size,
            uint color,
            float radius,
            int segments = 6)
        {
            Vector2 end = pos + size;

            // Coins
            drawList.AddCircleFilled(pos + new Vector2(radius, radius), radius, color, segments); // Top-left
            drawList.AddCircleFilled(end - new Vector2(radius, size.y - radius), radius, color, segments); // Top-right
            drawList.AddCircleFilled(pos + new Vector2(radius, size.y - radius), radius, color, segments); // Bottom-left
            drawList.AddCircleFilled(end - new Vector2(radius, radius), radius, color, segments); // Bottom-right

            // Bords
            drawList.AddRectFilled(
                pos + new Vector2(radius, 0),
                end - new Vector2(radius, size.y - radius * 2),
                color); // Top

            drawList.AddRectFilled(
                pos + new Vector2(radius, size.y - radius * 2),
                end - new Vector2(radius, 0),
                color); // Bottom

            drawList.AddRectFilled(
                pos + new Vector2(0, radius),
                end - new Vector2(size.x - radius * 2, radius),
                color); // Left

            drawList.AddRectFilled(
                pos + new Vector2(size.x - radius * 2, radius),
                end - new Vector2(0, radius),
                color); // Right

            // Centre
            drawList.AddRectFilled(
                pos + new Vector2(radius, radius),
                end - new Vector2(radius, radius),
                color); // Center
        }
        #endregion
    }
}
