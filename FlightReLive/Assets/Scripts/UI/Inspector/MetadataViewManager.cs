using FlightReLive.Core.Cameras;
using FlightReLive.Core.Database;
using FlightReLive.Core.Loading;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.POI;
using FlightReLive.Core.Settings;
using FlightReLive.Core.TimeBar;
using FlightReLive.UI.FlightCharts;
using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FlightReLive.UI.Inspector
{
    public class MetadataViewManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private FlightDataPoint _currentDataPoint;
        #endregion

        #region PROPERTIES
        public static MetadataViewManager Instance { get; private set; }
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

        private void Start()
        {
            TimeBarManager.Instance.OnProgressChanged += OnProgressChanged;
        }

        private void OnDestroy()
        {
            TimeBarManager.Instance.OnProgressChanged -= OnProgressChanged;
        }
        #endregion

        #region CALLBACKS
        private void OnProgressChanged(float arg1, int arg2, FlightDataPoint point)
        {
            _currentDataPoint = point;
        }
        #endregion

        #region UI
        internal void DrawMetadata(FuWindow window, FuLayout layout)
        {
            float scale = Fugui.CurrentContext.Scale;
            float scrollPanelHeight = ImGui.GetContentRegionAvail().y - 20f * scale;
            Vector2 scrollPanelSize = new Vector2(ImGui.GetContentRegionAvail().x, scrollPanelHeight);
            FlightData currentFlightData = LoadingManager.Instance.CurrentFlightData;
            ImGui.BeginChild("DataScrollbalePanel", scrollPanelSize, ImGuiChildFlags.AutoResizeY);

            if (_currentDataPoint != null && currentFlightData != null)
            {
                layout.Collapsable(FlightReLiveIcons.VideoFile + "  Video##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid grid = new FuGrid("positionDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        string formattedResolution = $"{currentFlightData.Width}x{currentFlightData.Height}";
                        string formattedFramerate = $"{currentFlightData.Frequency:F2} FPS";
                        string formattedDuration = currentFlightData.Length.ToString(@"hh\:mm\:ss");

                        Draw(window, "11", grid, layout, FlightReLiveIcons.Resolution, formattedResolution, "Native resolution");
                        Draw(window, "12", grid, layout, FlightReLiveIcons.Framerate, formattedFramerate, "Framerate");
                        Draw(window, "13", grid, layout, FlightReLiveIcons.Duration, formattedDuration, "Duration");
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                if (!string.IsNullOrEmpty(currentFlightData.SharedHash))
                {
                    layout.Collapsable(FlightReLiveIcons.Share + "  Share##collapsable", () =>
                    {
                        Fugui.PushFont(14, FontType.Regular);

                        using (FuGrid grid = new FuGrid("positionDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                        {
                            Draw(window, "14", grid, layout, FlightReLiveIcons.Share, currentFlightData.SharedHash, "SharedHash");
                        }

                        Fugui.PopFont();
                    }, FuButtonStyle.Collapsable, defaultOpen: true);
                }

                layout.Collapsable(FlightReLiveIcons.Drone + "  Drone##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid grid = new FuGrid("positionDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        string formattedPosition = $"{_currentDataPoint.Coordinate.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {_currentDataPoint.Coordinate.Longitude.ToString("F5", CultureInfo.InvariantCulture)}";
                        Draw(window, "1", grid, layout, FlightReLiveIcons.GPSMarker, formattedPosition, "Current drone position", FlightReLiveIcons.OpenStreetMap, () =>
                        {
                            OpenStreetMapHelper.OpenOpenStreetMapBrowser(_currentDataPoint.Coordinate);
                        }, "Display on OpenStreetMap");

                        string formattedAbsoluteAltitude = SettingsManager.FormatAltitude(currentFlightData.TakeOffAltitude + _currentDataPoint.RelativeAltitude);
                        string formattedRelativeAltitude = SettingsManager.FormatAltitude(_currentDataPoint.RelativeAltitude);

                        double speed = CalculateSpeed((float)_currentDataPoint.HorizontalSpeed, (float)_currentDataPoint.VerticalSpeed);
                        string formattedSpeed = SettingsManager.FormatSpeed(speed);

                        Draw(window, "2", grid, layout, FlightReLiveIcons.Speed, formattedSpeed, "Current speed", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.Speed;
                        }, "Display speed chart");

                        Draw(window, "3", grid, layout, FlightReLiveIcons.AltitudeRelative, formattedRelativeAltitude, "Relative altitude", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.RelativeAltitude;
                        }, "Display relative altitude chart");

                        Draw(window, "4", grid, layout, FlightReLiveIcons.AltitudeAbsolute, formattedAbsoluteAltitude, "Absolute altitude", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.AbsoluteAltitude;
                        }, "Display absolute altitude chart");
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable(FlightReLiveIcons.Camera + "  Camera##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid grid = new FuGrid("cameraDataGrid", new FuGridDefinition(3, new int[] { 30, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        Draw(window, "5", grid, layout, FlightReLiveIcons.Aperture, _currentDataPoint.Aperture.ToString(), "Aperture", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.Aperture;
                        }, "Display Aperture chart");

                        Draw(window, "6", grid, layout, FlightReLiveIcons.ShutterSpeed, _currentDataPoint.ShutterSpeed.ToString(), "Shutter Speed", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.ShutterSpeed;
                        }, "Display Shutter speed chart");

                        Draw(window, "7", grid, layout, FlightReLiveIcons.PostProcess, _currentDataPoint.FocalLength.ToString(), "Focal Length", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.Focal;
                        }, "Display Focal length chart");

                        Draw(window, "8", grid, layout, FlightReLiveIcons.ISO, _currentDataPoint.ISO.ToString(), "ISO", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.ISO;
                        }, "Display ISO chart");

                        Draw(window, "9", grid, layout, FlightReLiveIcons.Exposure, _currentDataPoint.Exposure.ToString(), "Exposure", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.Exposure;
                        }, "Display exposure chart");

                        string formattedZoom = $"X{_currentDataPoint.DigitalZoom:F1}";
                        Draw(window, "10", grid, layout, FlightReLiveIcons.DigitalZoom, formattedZoom, "Digital Zoom", FlightReLiveIcons.Charts, () =>
                        {
                            FlightChartsManager.Instance.DisplayedChart = FlightChartType.DigitalZoom;
                        }, "Display digital zoom");
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

                layout.Collapsable(FlightReLiveIcons.GPSMarker + "  Points of Interest##collapsable", () =>
                {
                    Fugui.PushFont(14, FontType.Regular);

                    using (FuGrid grid = new FuGrid("poiDataGrid", new FuGridDefinition(3, new int[] { 24, -28 }), FuGridFlag.Default, 2, 2, 2))
                    {
                        // On récupère la liste des POI autour de la caméra
                        List<POIEntity> nearbyPOIs = POIManager.Instance.GetPOIsWithinDistance(100f);

                        if (nearbyPOIs.Count > 0)
                        {
                            int index = 0;
                            foreach (POIEntity poi in nearbyPOIs)
                            {
                                if (poi == null)
                                {
                                    continue;
                                }

                                // Calcul de la distance caméra -> POI
                                Camera cam = CameraManager.Instance.ReLiveCamera;

                                if (cam == null)
                                {
                                    break;
                                }

                                float distance = cam != null ? Vector3.Distance(cam.transform.position, poi.transform.position) : 0f;

                                //Convert distance to user unit system
                                string formattedDistance = FormatDistance(distance);

                                //Formatted GPS position
                                SerializedGPSCoordinate gpsData = LoadingManager.Instance.CurrentFlightData.ConvertWorldToGPSPosition(poi.transform.position);
                                string formattedPosition = $"{gpsData.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {gpsData.Longitude.ToString("F5", CultureInfo.InvariantCulture)}";

                                //Name with distance
                                string label = $"{poi.Text} ({formattedDistance})";

                                //Unique ID for ImGui
                                string uniqueId = $"POI_{index++}";

                                Draw(window, uniqueId, grid, layout, FlightReLiveIcons.MapPin,
                                    label,
                                    "Point of interest position",
                                    FlightReLiveIcons.OpenStreetMap,
                                    () =>
                                    {
                                        OpenStreetMapHelper.OpenOpenStreetMapBrowser(gpsData);
                                    },
                                    "Display on OpenStreetMap");
                            }
                        }
                    }

                    Fugui.PopFont();
                }, FuButtonStyle.Collapsable, defaultOpen: true);

            }

            ImGui.EndChild();
        }

        /// <summary>
        /// Format a distance value according to the current unit system.
        /// </summary>
        private static string FormatDistance(float distanceMeters)
        {
            switch (SettingsManager.CurrentSettings.UnitSystemType)
            {
                case UnitSystemType.Imperial:
                case UnitSystemType.Nautical:
                    float feet = distanceMeters * 3.28084f;
                    return $"{feet:F0} ft";
                case UnitSystemType.Custom:
                    return $"{distanceMeters:F1} m";
                default: // Metric
                    return $"{distanceMeters:F1} m";
            }
        }
        #endregion

        #region UI HELPERS
        internal static void Draw(FuWindow window, string actionId, FuGrid grid, FuLayout layout, string icon, string value, string tooltip, string actionText = null, Action actionButton = null, string actionTooltip = null)
        {
            grid.SetNextElementToolTipWithLabel(tooltip);
            Fugui.PushFont(14, FontType.Regular);
            grid.Text(icon);
            Fugui.PopFont();
            grid.NextColumn();

            layout.FramedText(value, 0.5f, FuTextWrapping.Clip);

            if (layout.LastItemHovered)
            {
                DrawContextualMenu(window, value);
            }

            grid.NextColumn();

            if (!string.IsNullOrEmpty(actionText) && actionButton != null)
            {
                if (!string.IsNullOrEmpty(actionTooltip))
                {
                    layout.SetNextElementToolTip(actionTooltip);
                }

                Fugui.PushFont(12, FontType.Regular);

                string uniqueButtonLabel = $"{actionText}##{actionId}";

                if (layout.Button(uniqueButtonLabel, FuElementSize.AutoSize, new Vector2(10f, 4f), Vector2.zero, FuButtonStyle.Default))
                {
                    actionButton?.Invoke();
                }

                Fugui.PopFont();
            }
        }

        internal static void DrawContextualMenu(FuWindow window, string value)
        {
            if (window.Mouse.IsDown(FuMouseButton.Right))
            {
                FuContextMenuBuilder contextMenuBuilder = FuContextMenuBuilder.Start();

                contextMenuBuilder.AddItem(FlightReLiveIcons.Duplicate + " Copy", () =>
                {
                    ImGui.SetClipboardText(value ?? string.Empty);
                    Fugui.Notify("Value copied to clipboard", "Current field value copied to clipboard.", StateType.Info, 5f);
                });

                List<FuContextMenuItem> contextMenuItems = contextMenuBuilder.Build();
                Fugui.PushContextMenuItems(contextMenuItems);
                Fugui.TryOpenContextMenu();
                Fugui.PopContextMenuItems();
            }
        }

        private float CalculateSpeed(float horizontalSpeed, float verticalSpeed)
        {
            return Mathf.Sqrt(horizontalSpeed * horizontalSpeed + verticalSpeed * verticalSpeed);
        }
        #endregion
    }
}
