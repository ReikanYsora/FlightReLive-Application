using FlightReLive.Core;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.UI.VideoPlayer;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightReLive.UI.FlightCharts
{
    public class FlightChart : IDisposable
    {
        #region CONSTANTS
        private const float CHARTS_GRADUATION_HORIZONTAL_INTERVAL = 30f;
        private const float CHARTS_GRADUATION_VERTICAL_INTERVAL = 60f;
        private const float CHARTS_CIRCLE_RADIUS = 2f;
        private const float CHARTS_CIRCLE_INNER_RADIUS = 3f;
        private const float CHARTS_CIRCLE_OUTER_RADIUS = 5f;
        #endregion

        #region ATTRIBUTES
        public FlightChartType ChartType;
        public float IntervalDuration;
        private uint _chartColor;
        private double _cachedMinValue;
        private double _cachedMaxValue;
        private double _cachedRange;

        private int _currentStepIndex;
        private float _currentRatio;
        private bool _isChartSet;
        private bool _isMouseClicked;
        private float _cursorRatioOnClick;
        private Color _chartsCircleOuterColor;
        private Color _chartsCursorColor;
        private Color _chartsHoverBarColor;
        private Color _darkBackgroundColor;
        private Color _blueBackgroundColor;
        private uint _darkBackgroundColorU32;
        private uint _blueBackgroundColorU32;
        #endregion

        #region EVENTS
        public event Action<FlightChartStep> OnStepChanged;
        public event Action<DateTime, float> OnDateChanged;
        #endregion

        #region PROPERTIES
        public List<FlightChartStep> Steps { private set; get; }

        public int Index
        {
            get
            {
                if (Steps != null)
                {
                    return Steps[_currentStepIndex].Index;
                }
                else
                {
                    Debug.LogError("FlightCharts does not contain Step...");
                    return 0;
                }
            }
            set
            {
                for (int i = 0; i < Steps.Count; i++)
                {
                    if (Steps[_currentStepIndex].Index == value)
                    {
                        _currentStepIndex = i;
                        break;
                    }
                }
            }
        }

        public Color ChartColor
        {
            get
            {
                return ImGui.ColorConvertU32ToFloat4(_chartColor);
            }
            set
            {
                _chartColor = ImGui.ColorConvertFloat4ToU32(value);
            }
        }
        #endregion

        #region CONSTRUCTOR
        public FlightChart(FlightChartType type, Color backgroundColor, Color circleColor, Color cursorColor, Color hoveredColor)
        {
            ChartType = type;
            IntervalDuration = 1;
            Steps = new List<FlightChartStep>();

            //Bake all color for performances
            _chartColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.CurrentTheme.Colors[(int)FuColors.Highlight]);

            _chartsCircleOuterColor = circleColor;
            _chartsCursorColor = cursorColor;
            _chartsHoverBarColor = hoveredColor;
            _darkBackgroundColor = backgroundColor;
            _darkBackgroundColorU32 = ImGui.ColorConvertFloat4ToU32(_darkBackgroundColor);
            _blueBackgroundColor = new Color(_chartsCursorColor.r, _chartsCursorColor.g, _chartsCursorColor.b, 0.1f);
            _blueBackgroundColorU32 = ImGui.ColorConvertFloat4ToU32(_blueBackgroundColor);

            _currentRatio = 0;
            _currentStepIndex = 0;
            _isChartSet = false;
            _isMouseClicked = false;

            VideoPlayerManager.Instance.OnProgressChanged += OnProgressChanged;
        }

        private void OnProgressChanged(float ratio, int index, FlightDataPoint point)
        {
            if (Steps == null || Steps.Count == 0 || point == null)
            {
                return;
            }

            _currentRatio = Math.Clamp(ratio, 0f, 1f);

            int stepIndex = Steps.FindIndex(s => s.FlightDataPoint == point);

            if (stepIndex >= 0)
            {
                _currentStepIndex = stepIndex;
            }
            else
            {
                _currentStepIndex = Math.Clamp(index, 0, Steps.Count - 1);
            }
        }
        #endregion

        #region METHODS
        public void AddStep(List<FlightChartStep> mediaBarSteplist)
        {
            Steps.AddRange(mediaBarSteplist);
            _isChartSet = true;
            CacheValueRange();
        }

        public void ClearSteps()
        {
            Steps.Clear();
            _isChartSet = false;
        }

        public void Dispose()
        {
            VideoPlayerManager.Instance.OnProgressChanged -= OnProgressChanged;
            Steps = null;
        }

        private int GetStepIndexFromRatio(float ratio, int stepCount)
        {
            int index = (int)Math.Round(ratio * (stepCount - 1));

            return Math.Clamp(index, 0, stepCount - 1);
        }

        private float GetRatioFromDataPoint(FlightDataPoint point)
        {
            double videoDuration = VideoPlayerManager.Instance.Length;
            double timeSeconds = point.TimeSpan.TotalSeconds;
            float ratio = (float)(timeSeconds / videoDuration);

            return Math.Clamp(ratio, 0f, 1f);
        }

        private long GetFrameFromDataPoint(FlightDataPoint point)
        {
            float ratio = GetRatioFromDataPoint(point);
            long totalFrames = VideoPlayerManager.Instance.TotalFrameCount;

            return Math.Clamp(Mathf.RoundToInt(ratio * (totalFrames - 1)), 0, totalFrames - 1);
        }

        private float GetYFromValue(double value, Vector2 position, Vector2 size, float scale)
        {
            float paddingRatio = Math.Clamp(10f * scale / size.y, 0.02f, 0.1f);
            float paddedHeight = size.y * (1f - 2f * paddingRatio);
            float topOffset = size.y * paddingRatio;

            double normalized = (value - _cachedMinValue) / _cachedRange;
            return position.y + topOffset + (float)((1.0 - normalized) * paddedHeight);
        }

        private void CacheValueRange()
        {
            _cachedMinValue = Steps.Min(s => s.Value);
            _cachedMaxValue = Steps.Max(s => s.Value);
            _cachedRange = _cachedMaxValue - _cachedMinValue;

            if (_cachedRange <= 0)
            {
                _cachedRange = 1;
            }
        }

        private Color U32ToColor(uint colorU32)
        {
            float a = ((colorU32 >> 24) & 0xFF) / 255f;
            float b = ((colorU32 >> 16) & 0xFF) / 255f;
            float g = ((colorU32 >> 8) & 0xFF) / 255f;
            float r = (colorU32 & 0xFF) / 255f;

            return new Color(r, g, b, a);
        }
        #endregion

        #region UI
        internal void Draw(FuWindow window, FuLayout layout, float lineWidth)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float availableWidth = layout.GetAvailableWidth();
            float scale = Fugui.CurrentContext.Scale;
            Vector2 graduationPosition = ImGui.GetCursorScreenPos();
            graduationPosition.x += 30f * scale;

            float rightPadding = 60f * scale;
            float verticalPaddingTop = 20f * scale;
            float verticalPaddingBottom = 20f * scale;
            float availableHeight = layout.GetAvailableHeight();
            graduationPosition.y += verticalPaddingTop;
            Vector2 size = new Vector2(availableWidth - rightPadding, availableHeight - verticalPaddingTop - verticalPaddingBottom);

            int stepCount = Steps.Count;

            DrawGraduation(stepCount, graduationPosition, size, drawList, scale);
            DrawCharts(stepCount, graduationPosition, size, drawList, lineWidth, scale);
            DrawCursor(stepCount, graduationPosition, size, drawList, scale);
        }

        private void DrawGraduation(int stepCount, Vector2 position, Vector2 size, ImDrawListPtr drawList, float scale)
        {
            uint color = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.CurrentTheme.Colors[(int)FuColors.Text]);
            uint colorMinor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.CurrentTheme.Colors[(int)FuColors.TextDisabled]);
            int textHeight = 10;

            float ratio = _isMouseClicked ? _cursorRatioOnClick : _currentRatio;
            ratio = Math.Clamp(ratio, 0f, 1f);
            float cursorX = position.x + size.x * ratio;

            drawList.AddRectFilled(position, new Vector2(cursorX, position.y + size.y), _blueBackgroundColorU32);
            drawList.AddRectFilled(new Vector2(cursorX, position.y), position + size, _darkBackgroundColorU32);

            //Horizontal graduations
            int verticalGraduationCount = Math.Max(2, (int)(size.y / (CHARTS_GRADUATION_HORIZONTAL_INTERVAL * scale)));

            for (int i = 0; i < verticalGraduationCount; i++)
            {
                double value = _cachedMinValue + (_cachedRange * i / verticalGraduationCount);
                float y = GetYFromValue(value, position, size, scale);

                drawList.AddLine(new Vector2(position.x, y), new Vector2(position.x + size.x, y), colorMinor, 1f);

                string label = value.ToString("F0");
                Fugui.PushFont(textHeight, FontType.Regular);
                Vector2 textSize = ImGui.CalcTextSize(label);
                Vector2 textPos = new Vector2(position.x - textSize.x - 6f, y - textSize.y / 2f);
                drawList.AddText(textPos, color, label);
                Fugui.PopFont();
            }

            for (int i = 0; i < verticalGraduationCount; i++)
            {
                double value = _cachedMinValue + (_cachedRange * i / verticalGraduationCount);
                float y = GetYFromValue(value, position, size, scale);

                drawList.AddLine(new Vector2(position.x, y), new Vector2(position.x + size.x, y), colorMinor, 1f);

                string label = value.ToString("F0");
                Fugui.PushFont(textHeight, FontType.Regular);
                Vector2 textSize = ImGui.CalcTextSize(label);
                Vector2 textPos = new Vector2(position.x + size.x + 6f, y - textSize.y / 2f);
                drawList.AddText(textPos, color, label);
                Fugui.PopFont();
            }

            //Vertical graduations
            int horizontalGraduationCount = Math.Min(stepCount, Math.Max(2, (int)(size.x / (CHARTS_GRADUATION_VERTICAL_INTERVAL * scale))));
            for (int i = 0; i <= horizontalGraduationCount; i++)
            {
                float x = position.x + size.x * i / horizontalGraduationCount;
                drawList.AddLine(new Vector2(x, position.y), new Vector2(x, position.y + size.y), colorMinor, 1f);

                int stepIndex = Math.Min(i * (stepCount - 1) / horizontalGraduationCount, stepCount - 1);
                string label = Steps[stepIndex].Label;
                Fugui.PushFont(textHeight, FontType.Regular);
                Vector2 textSize = ImGui.CalcTextSize(label);
                Vector2 textPos = new Vector2(x - textSize.x / 2f, position.y - textSize.y - 4f);
                drawList.AddText(textPos, color, label);
                Fugui.PopFont();
            }


            drawList.AddRect(position, position + size, colorMinor, 0f, ImDrawFlags.None, 1f);

            if (ImGui.IsMouseHoveringRect(position, position + size))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _isMouseClicked = true;
                }

                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    float clickPositionX = ImGui.GetMousePos().x;
                    float exactRatio = (clickPositionX - position.x) / size.x;
                    int theoricStep = (int)Math.Floor(exactRatio * stepCount);
                    theoricStep = Math.Max(0, Math.Min(theoricStep, stepCount - 1));
                    _cursorRatioOnClick = exactRatio;
                }
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _isMouseClicked)
            {
                _isMouseClicked = false;

                int stepIndex = GetStepIndexFromRatio(_cursorRatioOnClick, Steps.Count);
                _currentStepIndex = stepIndex;

                FlightChartStep selectedStep = Steps[stepIndex];
                FlightDataPoint point = selectedStep.FlightDataPoint;

                _currentRatio = GetRatioFromDataPoint(point);
                OnStepChanged?.Invoke(selectedStep);
                OnDateChanged?.Invoke(point.Time, _currentRatio);

                long targetFrame = GetFrameFromDataPoint(point);
                VideoPlayerManager.Instance.SetFrame(targetFrame, false);
            }
        }

        private void DrawCursor(int stepCount, Vector2 startPosition, Vector2 size, ImDrawListPtr drawList, float scale)
        {
            uint color = ImGui.ColorConvertFloat4ToU32(_chartsCursorColor);
            float ratio = _isMouseClicked ? _cursorRatioOnClick : _currentRatio;
            ratio = Math.Clamp(ratio, 0f, 1f);

            //Draw progression line
            float lineThickness = 2.5f;
            Vector2 lineStart = new Vector2(startPosition.x + size.x * ratio, startPosition.y);
            Vector2 lineEnd = new Vector2(lineStart.x, startPosition.y + size.y);
            drawList.AddLine(lineStart, lineEnd, color, lineThickness);
        }

        private void DrawCharts(int stepCount, Vector2 position, Vector2 size, ImDrawListPtr drawList, float lineWidth, float scale)
        {
            if (!_isChartSet || stepCount < 2)
            {
                return;
            }

            float chartTopY = position.y;
            float chartBottomY = position.y + size.y;
            float chartHeight = (size.y - 10f) * 2f / 3f;
            float intervalWidth = size.x / (float)(stepCount - 1);
            bool isHoveringPoint = false;
            float circleRadius = CHARTS_CIRCLE_RADIUS * Fugui.CurrentContext.Scale;
            float innerCircleRadius = CHARTS_CIRCLE_INNER_RADIUS * Fugui.CurrentContext.Scale;
            float outerCircleRadius = CHARTS_CIRCLE_OUTER_RADIUS * Fugui.CurrentContext.Scale;
            int previousChartIndex = -1;
            double previousChartValue = double.NaN;
            bool firstDrawingStep = true;

            for (int i = 0; i < stepCount; i++)
            {
                FlightChartStep step = Steps[i];

                if (double.IsNaN(previousChartValue) && !double.IsNaN(step.Value))
                {
                    previousChartValue = step.Value;
                    previousChartIndex = i;
                    continue;
                }

                if (!double.IsNaN(previousChartValue))
                {
                    FlightChartStep prevStep = Steps[previousChartIndex];
                    float xStart = position.x + previousChartIndex * intervalWidth;
                    float xEnd = position.x + i * intervalWidth;
                    float yStart = GetYFromValue(prevStep.Value, position, size, scale);
                    float yEnd = GetYFromValue(step.Value, position, size, scale);

                    Vector2 minStart = new Vector2(xStart, yStart);
                    Vector2 minEnd = new Vector2(xEnd, yEnd);

                    Vector2 bottomStart = new Vector2(xStart, chartBottomY);
                    Vector2 bottomEnd = new Vector2(xEnd, chartBottomY);
                    Color fillColor = U32ToColor(step.ColorU32);
                    fillColor.a = 0.1f;

                    uint fillColorU32 = ImGui.ColorConvertFloat4ToU32(fillColor);

                    Vector2[] fillVertices = new Vector2[]
                    {
                        minStart,
                        minEnd,
                        bottomEnd,
                        bottomStart
                    };

                    unsafe
                    {
                        drawList.AddConvexPolyFilled(ref fillVertices[0], fillVertices.Length, fillColorU32);
                    }

                    drawList.AddLine(minStart, minEnd, step.ColorU32, lineWidth);

                    DrawChartPoint(drawList, minEnd, step.TooltipSize, step.Value, ref isHoveringPoint, circleRadius, innerCircleRadius, outerCircleRadius, step.ColorU32, chartTopY, chartBottomY);

                    if (firstDrawingStep)
                    {
                        DrawChartPoint(drawList, minStart, prevStep.TooltipSize, prevStep.Value, ref isHoveringPoint, circleRadius, innerCircleRadius, outerCircleRadius, step.ColorU32, chartTopY, chartBottomY);
                        firstDrawingStep = false;
                    }

                    previousChartValue = step.Value;
                    previousChartIndex = i;
                }
            }
        }


        private void DrawChartPoint(ImDrawListPtr drawList, Vector2 position, Vector2 tooltipSize, double value, ref bool isHovering,
            float radius, float innerRadius, float outerRadius, uint pointColorU32,
            float chartTopY, float chartBottomY)
        {
            Vector2 outerRadiusVec = new Vector2(outerRadius, outerRadius);
            Vector2 hoverMin = position - outerRadiusVec;
            Vector2 hoverMax = position + outerRadiusVec;

            if (ImGui.IsMouseHoveringRect(hoverMin, hoverMax) && !isHovering)
            {
                isHovering = true;

                float pulse = Mathf.Sin(Time.time * 6f) * 0.3f + 1f;
                float animatedRadius = outerRadius * pulse;

                Color glowColor = ImGui.ColorConvertU32ToFloat4(pointColorU32);
                glowColor.a = 0.25f;
                uint glowColorU32 = ImGui.ColorConvertFloat4ToU32(glowColor);

                uint chartCircleOuterColor = ImGui.ColorConvertFloat4ToU32(_chartsCircleOuterColor);
                uint chartHoverBarColor = ImGui.ColorConvertFloat4ToU32(_chartsHoverBarColor);
                uint frameBgColor = ImGui.ColorConvertFloat4ToU32(Fugui.Themes.CurrentTheme.Colors[(int)FuColors.FrameBg]);

                drawList.AddCircleFilled(position, animatedRadius * 1.6f, glowColorU32);
                drawList.AddCircleFilled(position, animatedRadius, chartCircleOuterColor);
                drawList.AddCircleFilled(position, innerRadius * pulse, pointColorU32);

                drawList.AddLine(new Vector2(position.x, chartTopY), new Vector2(position.x, chartBottomY), chartHoverBarColor, 1.5f);

                string text = $"{value:F2}";
                Vector2 textSize = ImGui.CalcTextSize(text);
                Vector2 padding = Fugui.Themes.CurrentTheme.FramePadding;

                float verticalOffsetY = 20f;
                Vector2 tooltipPosition = position - new Vector2(0f, verticalOffsetY);

                Color frameBgColor90 = ImGui.ColorConvertU32ToFloat4(frameBgColor);
                frameBgColor90.a = 0.9f;
                uint frameBgColorU32_90 = ImGui.ColorConvertFloat4ToU32(frameBgColor90);

                Vector2 rectMin = tooltipPosition - (textSize / 2f) - padding;
                Vector2 rectMax = tooltipPosition + (textSize / 2f) + padding;

                drawList.AddRectFilled(rectMin, rectMax, frameBgColorU32_90);
                Fugui.PushFont(12, FontType.Regular);
                drawList.AddText(tooltipPosition - textSize / 2f, chartCircleOuterColor, text);
                Fugui.PopFont();
            }
        }
        #endregion
    }
}
