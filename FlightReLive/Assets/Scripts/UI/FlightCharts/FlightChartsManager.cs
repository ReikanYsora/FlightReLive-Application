using FlightReLive.Core;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightReLive.UI.FlightCharts
{
    public class FlightChartsManager : FuWindowBehaviour
    {
        #region ATTRIBUTES
        private Dictionary<FlightChartType, FlightChart> _flightChartsBar;
        [SerializeField] [Range(1f, 3f)] private float _chartLineWidth;
        private FlightChartType _displayedChart;
        private FlightChart _speedChart;
        private FlightChart _relativeAltitudeChart;
        private FlightChart _absoluteAltitudeChart;
        private FlightChart _apertureChart;
        private FlightChart _shutterSpeedChart;
        private FlightChart _focalChart;
        private FlightChart _isoChart;
        private FlightChart _exposureChart;
        private FlightChart _digitalZoomChart;
        #endregion

        #region PROPERTIES
        public static FlightChartsManager Instance { get; private set; }

        public FlightChartType DisplayedChart
        {
            set
            {
                _displayedChart = value;
            }
            get
            {
                return _displayedChart;
            }
        }
        #endregion

        #region UNITY METHODS
        public override void FuguiAwake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _flightChartsBar = new Dictionary<FlightChartType, FlightChart>();
            _displayedChart = FlightChartType.Speed;
            base.FuguiAwake();
        }

        private void Start()
        {
            TerrainManager.Instance.OnTerrainLoaded += OnTerrainLoaded;
            SettingsManager.OnUnitSystemTypeChanged += OnUnitSystemTypeChanged;
        }

        private void OnDestroy()
        {
            TerrainManager.Instance.OnTerrainLoaded -= OnTerrainLoaded;
            SettingsManager.OnUnitSystemTypeChanged -= OnUnitSystemTypeChanged;
        }
        #endregion

        #region METHODS
        internal void LoadFlightCharts(FlightData flight)
        {
            if (flight == null)
            {
                return;
            }

            //Charts initialization
            if (_speedChart == null)
            {
                _speedChart = AddChart(FlightChartType.Speed);
                _speedChart.IntervalDuration = 1;
                _speedChart.ChartColor = Color.white;
            }
            else
            {
                _speedChart.ClearSteps();
            }

            if (_relativeAltitudeChart == null)
            {
                _relativeAltitudeChart = AddChart(FlightChartType.RelativeAltitude);
                _relativeAltitudeChart.IntervalDuration = 1;
                _relativeAltitudeChart.ChartColor = Color.white;
            }
            else
            {
                _relativeAltitudeChart.ClearSteps();
            }

            if (_absoluteAltitudeChart == null)
            {
                _absoluteAltitudeChart = AddChart(FlightChartType.AbsoluteAltitude);
                _absoluteAltitudeChart.IntervalDuration = 1;
                _absoluteAltitudeChart.ChartColor = Color.white;
            }
            else
            {
                _absoluteAltitudeChart.ClearSteps();
            }

            if (_apertureChart == null)
            {
                _apertureChart = AddChart(FlightChartType.Aperture);
                _apertureChart.IntervalDuration = 1;
                _apertureChart.ChartColor = Color.white;
            }
            else
            {
                _apertureChart.ClearSteps();
            }

            if (_shutterSpeedChart == null)
            {
                _shutterSpeedChart = AddChart(FlightChartType.ShutterSpeed);
                _shutterSpeedChart.IntervalDuration = 1;
                _shutterSpeedChart.ChartColor = Color.white;
            }
            else
            {
                _shutterSpeedChart.ClearSteps();
            }

            if (_focalChart == null)
            {
                _focalChart = AddChart(FlightChartType.Focal);
                _focalChart.IntervalDuration = 1;
                _focalChart.ChartColor = Color.white;
            }
            else
            {
                _focalChart.ClearSteps();
            }

            if (_isoChart == null)
            {
                _isoChart = AddChart(FlightChartType.ISO);
                _isoChart.IntervalDuration = 1;
                _isoChart.ChartColor = Color.white;
            }
            else
            {
                _isoChart.ClearSteps();
            }

            if (_exposureChart == null)
            {
                _exposureChart = AddChart(FlightChartType.Exposure);
                _exposureChart.IntervalDuration = 1;
                _exposureChart.ChartColor = Color.white;
            }
            else
            {
                _exposureChart.ClearSteps();
            }

            if (_digitalZoomChart == null)
            {
                _digitalZoomChart = AddChart(FlightChartType.DigitalZoom);
                _digitalZoomChart.IntervalDuration = 1;
                _digitalZoomChart.ChartColor = Color.white;
            }
            else
            {
                _digitalZoomChart.ClearSteps();
            }

            List<FlightChartStep> speedChartSteps = new();
            List<FlightChartStep> relativeAltitudeChart = new();
            List<FlightChartStep> absoluteAltitudeChart = new();
            List<FlightChartStep> apertureChart = new();
            List<FlightChartStep> shutterSpeedChart = new();
            List<FlightChartStep> focalChart = new();
            List<FlightChartStep> isoChart = new();
            List<FlightChartStep> exposureChart = new();
            List<FlightChartStep> digitalZoomChart = new();

            List<FlightDataPoint> points = flight.Points;

            // Data preconversion
            List<float> convertedSpeeds = points.Select(p => SettingsManager.ConvertSpeed(CalculateSpeed((float)p.HorizontalSpeed, (float)p.VerticalSpeed))).ToList();
            List<float> convertedRelAlts = points.Select(p => SettingsManager.ConvertAltitude((float)p.RelativeAltitude)).ToList();
            List<float> convertedAbsAlts;

            if (points.Where(x => x.AbsoluteAltitude > 0).Any())
            {
                convertedAbsAlts = points.Select(p => SettingsManager.ConvertAltitude((float) p.AbsoluteAltitude)).ToList();
            }
            else
            {
               convertedAbsAlts = points.Select(p => SettingsManager.ConvertAltitude((float)(p.RelativeAltitude + flight.TakeOffAltitude))).ToList();
            }

            List<float> apertures = points.Select(p => p.CameraSettings.Aperture).ToList();
            List<float> shutterSpeeds = points.Select(p => p.CameraSettings.ShutterSpeed).ToList();
            List<float> focals = points.Select(p => p.CameraSettings.FocalLength).ToList();
            List<int> isos = points.Select(p => p.CameraSettings.ISO).ToList();
            List<float> exposures = points.Select(p => p.CameraSettings.Exposure).ToList();
            List<float> digitalZooms = points.Select(p => p.CameraSettings.DigitalZoom).ToList();

            // Min/max/range
            (var minSpeed, _, var rangeSpeed, var isFlatSpeed) = GetRange(convertedSpeeds, 10f);
            (var minRelAlt, _, var rangeRelAlt, var isFlatRelAlt) = GetRange(convertedRelAlts, 10f);
            (var minAbsAlt, _, var rangeAbsAlt, var isFlatAbsAlt) = GetRange(convertedAbsAlts, 10f);
            (var minAperture, _, var rangeAperture, var isFlatAperture) = GetRange(apertures, 5f);
            (var minShutterSpeed, _, var rangeShutterSpeed, var isFlatShutterSpeed) = GetRange(shutterSpeeds, 10f);
            (var minFocal, _, var rangeFocal, var isFlatFocal) = GetRange(focals, 100f);
            (var minIso, _, var rangeIso, var isFlatIso) = GetRange(isos, 100);
            (var minExposure, _, var rangeExposure, var isFlatExposure) = GetRange(exposures, 10f);
            (var minDigitalZoom, _, var rangeDigitalZoom, var isFlatDigitalZoom) = GetRange(digitalZooms, 5f);

            // Steps generation
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                string label = p.Time.ToString("HH:mm:ss");

                float speed = SettingsManager.ConvertSpeed(CalculateSpeed((float)p.HorizontalSpeed, (float)p.VerticalSpeed));
                float relAlt = SettingsManager.ConvertAltitude((float)p.RelativeAltitude);
                float absAlt = SettingsManager.ConvertAltitude((float)(p.RelativeAltitude + flight.TakeOffAltitude));
                float aperture = p.CameraSettings.Aperture;
                float shutterSpeed = p.CameraSettings.ShutterSpeed;
                float focal = p.CameraSettings.FocalLength;
                float iso = p.CameraSettings.ISO;
                float exposure = p.CameraSettings.Exposure;
                float digitalZoom = p.CameraSettings.DigitalZoom;

                speedChartSteps.Add(CreateStep(i, label, p.Time, speed, Normalize(speed, minSpeed, rangeSpeed, isFlatSpeed), p));
                relativeAltitudeChart.Add(CreateStep(i, label, p.Time, relAlt, Normalize(relAlt, minRelAlt, rangeRelAlt, isFlatRelAlt), p));
                absoluteAltitudeChart.Add(CreateStep(i, label, p.Time, absAlt, Normalize(absAlt, minAbsAlt, rangeAbsAlt, isFlatAbsAlt), p));
                apertureChart.Add(CreateStep(i, label, p.Time, aperture, Normalize(aperture, minAperture, rangeAperture, isFlatAperture), p));
                shutterSpeedChart.Add(CreateStep(i, label, p.Time, shutterSpeed, Normalize(shutterSpeed, minShutterSpeed, rangeShutterSpeed, isFlatShutterSpeed), p));
                focalChart.Add(CreateStep(i, label, p.Time, focal, Normalize(focal, minFocal, rangeFocal, isFlatFocal), p));
                isoChart.Add(CreateStep(i, label, p.Time, iso, Normalize(iso, minIso, rangeIso, isFlatIso), p));
                exposureChart.Add(CreateStep(i, label, p.Time, exposure, Normalize(exposure, minExposure, rangeExposure, isFlatExposure), p));
                digitalZoomChart.Add(CreateStep(i, label, p.Time, digitalZoom, Normalize(digitalZoom, minDigitalZoom, rangeDigitalZoom, isFlatDigitalZoom), p));
            }

            // Add charts
            _speedChart.AddStep(speedChartSteps);
            _relativeAltitudeChart.AddStep(relativeAltitudeChart);
            _absoluteAltitudeChart.AddStep(absoluteAltitudeChart);
            _apertureChart.AddStep(apertureChart);
            _shutterSpeedChart.AddStep(shutterSpeedChart);
            _focalChart.AddStep(focalChart);
            _isoChart.AddStep(isoChart);
            _exposureChart.AddStep(exposureChart);
            _digitalZoomChart.AddStep(digitalZoomChart);
        }

        private FlightChartStep CreateStep(int index, string label, DateTime time, float value, float normalized, FlightDataPoint dataPoint)
        {
            Color color = Fugui.Themes.GetColor(FuColors.PlotLinesHovered);

            return new FlightChartStep(index, label, time, value)
            {
                ColorU32 = ImGui.ColorConvertFloat4ToU32(color),
                TooltipSize = ImGui.CalcTextSize($"{value:F2}"),
                FlightDataPoint = dataPoint
            };
        }

        private FlightChart AddChart(FlightChartType chartType)
        {
            FlightChart flightChart = new FlightChart(chartType, Fugui.Themes.GetColor(FuColors.FrameBg), Fugui.Themes.GetColor(FuColors.Text), Fugui.Themes.GetColor(FuColors.DuotoneSecondaryColor), Fugui.Themes.GetColor(FuColors.DuotonePrimaryColor));
            _flightChartsBar.Add(chartType, flightChart);

            return flightChart;
        }

        private static (float min, float max, float range, bool isFlat) GetRange(List<float> values, float padding)
        {
            float min = values.Min();
            float max = values.Max();
            bool isFlat = Mathf.Approximately(min, max);

            if (isFlat)
            {
                min -= padding;
                max += padding;
            }

            float range = Mathf.Max(max - min, 1f);

            return (min, max, range, isFlat);
        }

        private static (int min, int max, int range, bool isFlat) GetRange(List<int> values, int padding)
        {
            int min = values.Min();
            int max = values.Max();
            bool isFlat = Mathf.Approximately(min, max);

            if (isFlat)
            {
                min -= padding;
                max += padding;
            }

            int range = Mathf.Max(max - min, 1);

            return (min, max, range, isFlat);
        }

        internal void UnloadFlightCharts()
        {
            foreach (KeyValuePair<FlightChartType, FlightChart> flightChart in _flightChartsBar)
            {
                flightChart.Value.ClearSteps();
            }

            _flightChartsBar.Clear();
            _speedChart = null;
            _relativeAltitudeChart = null;
            _absoluteAltitudeChart = null;
            _apertureChart = null;
            _shutterSpeedChart = null;
            _focalChart = null;
            _isoChart = null;
            _exposureChart = null;
            _digitalZoomChart = null;
        }

        private float Normalize(float value, float min, float range, bool isFlat)
        {
            return isFlat ? 0f : (value - min) / range;
        }

        private float CalculateSpeed(float horizontalSpeed, float verticalSpeed)
        {
            return Mathf.Sqrt(horizontalSpeed * horizontalSpeed + verticalSpeed * verticalSpeed);
        }
        #endregion

        #region CALLBACKS
        public override void OnWindowCreated(FuWindow window)
        {
            window.UI = OnUI;
        }

        private void OnUnitSystemTypeChanged(UnitSystemType obj)
        {
            if (LoadingManager.Instance.CurrentFlightData == null)
            {
                return;
            }

            FlightData flight = LoadingManager.Instance.CurrentFlightData;
            List<FlightChartStep> speedChartSteps = new();
            List<FlightChartStep> relativeAltitudeChart = new();
            List<FlightChartStep> absoluteAltitudeChart = new();
            List<FlightDataPoint> points = flight.Points;

            List<float> convertedSpeeds = points.Select(p =>
                SettingsManager.ConvertSpeed(CalculateSpeed((float)p.HorizontalSpeed, (float)p.VerticalSpeed))
            ).ToList();
            List<float> convertedRelAlts = points.Select(p =>
                SettingsManager.ConvertAltitude((float)p.RelativeAltitude)
            ).ToList();

            List<float> convertedAbsAlts;
            if (points.Where(x => x.AbsoluteAltitude > 0).Any())
            {
                convertedAbsAlts = points.Select(p => SettingsManager.ConvertAltitude((float)p.AbsoluteAltitude)).ToList();
            }
            else
            {
                convertedAbsAlts = points.Select(p => SettingsManager.ConvertAltitude((float)(p.RelativeAltitude + flight.TakeOffAltitude))).ToList();
            }

            List<float> apertures = points.Select(p => p.CameraSettings.Aperture).ToList();
            List<float> shutterSpeeds = points.Select(p => p.CameraSettings.ShutterSpeed).ToList();
            List<int> isos = points.Select(p => p.CameraSettings.ISO).ToList();
            List<float> exposures = points.Select(p => p.CameraSettings.Exposure).ToList();
            List<float> digitalZooms = points.Select(p => p.CameraSettings.DigitalZoom).ToList();

            (float minSpeed, float maxSpeed, float rangeSpeed, bool isFlatSpeed) = GetRange(convertedSpeeds, 10f);
            (float minRelAlt, float maxRelAlt, float rangeRelAlt, bool isFlatRelAlt) = GetRange(convertedRelAlts, 10f);
            (float minAbsAlt, float maxAbsAlt, float rangeAbsAlt, bool isFlatAbsAlt) = GetRange(convertedAbsAlts, 10f);

            // Charts initialization
            _speedChart ??= AddChart(FlightChartType.Speed);
            _speedChart.IntervalDuration = 1;
            _speedChart.ChartColor = Color.white;
            _speedChart.ClearSteps();

            _relativeAltitudeChart ??= AddChart(FlightChartType.RelativeAltitude);
            _relativeAltitudeChart.IntervalDuration = 1;
            _relativeAltitudeChart.ChartColor = Color.white;
            _relativeAltitudeChart.ClearSteps();

            _absoluteAltitudeChart ??= AddChart(FlightChartType.AbsoluteAltitude);
            _absoluteAltitudeChart.IntervalDuration = 1;
            _absoluteAltitudeChart.ChartColor = Color.white;
            _absoluteAltitudeChart.ClearSteps();
            Color color = Fugui.Themes.GetColor(FuColors.PlotLinesHovered);

            // Populate chart steps
            for (int i = 0; i < points.Count; i++)
            {
                FlightDataPoint p = points[i];
                string label = p.Time.ToString("HH:mm:ss");

                float speed = convertedSpeeds[i];
                float normSpeed = Normalize(speed, minSpeed, rangeSpeed, isFlatSpeed);
                speedChartSteps.Add(new FlightChartStep(i, label, p.Time, speed)
                {
                    ColorU32 = ImGui.ColorConvertFloat4ToU32(color),
                    TooltipSize = ImGui.CalcTextSize($"{speed:F2}"),
                    FlightDataPoint = p
                });

                float relAlt = convertedRelAlts[i];
                float normRelAlt = Normalize(relAlt, minRelAlt, rangeRelAlt, isFlatRelAlt);
                relativeAltitudeChart.Add(new FlightChartStep(i, label, p.Time, relAlt)
                {
                    ColorU32 = ImGui.ColorConvertFloat4ToU32(color),
                    TooltipSize = ImGui.CalcTextSize($"{relAlt:F2}"),
                    FlightDataPoint = p
                });

                float absAlt = convertedAbsAlts[i];
                float normAbsAlt = Normalize(absAlt, minAbsAlt, rangeAbsAlt, isFlatAbsAlt);
                absoluteAltitudeChart.Add(new FlightChartStep(i, label, p.Time, absAlt)
                {
                    ColorU32 = ImGui.ColorConvertFloat4ToU32(color),
                    TooltipSize = ImGui.CalcTextSize($"{absAlt:F2}"),
                    FlightDataPoint = p
                });
            }

            // Final chart update
            _speedChart.AddStep(speedChartSteps);
            _relativeAltitudeChart.AddStep(relativeAltitudeChart);
            _absoluteAltitudeChart.AddStep(absoluteAltitudeChart);
        }

        private void OnTerrainLoaded(FlightData flightData)
        {
            LoadFlightCharts(flightData);
        }
        #endregion

        #region UI
        public override void OnUI(FuWindow window, FuLayout windowLayout)
        {
            using (FuPanel panel = new FuPanel("flightChartsPanel", FuStyle.Unpadded))
            {
                using (FuLayout layout = new FuLayout())
                {
                    if (_flightChartsBar.Count > 0)
                    {
                        ImGui.Dummy(Vector2.zero);
                        layout.ButtonsGroup<FlightChartType>("Chart", (value) =>
                        {
                            DisplayedChart = (FlightChartType)value;
                        }, () => { return DisplayedChart; });

                        FlightChart toDisplay = null;

                        _flightChartsBar.TryGetValue(_displayedChart, out toDisplay);

                        if (toDisplay != null)
                        {
                            toDisplay.Draw(window, layout, _chartLineWidth);
                        }
                        ImGui.Dummy(Vector2.zero);
                    }
                }
            }
        }
        #endregion
    }
}
