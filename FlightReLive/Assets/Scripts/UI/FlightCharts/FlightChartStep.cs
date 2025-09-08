using FlightReLive.Core.FlightDefinition;
using System;
using UnityEngine;

namespace FlightReLive.UI.FlightCharts
{
    public class FlightChartStep
    {
        public int Index { get; set; }

        public string Label { get; set; }

        public DateTime? Date { get; set; }

        public double Value { get; set; }

        public uint ColorU32 { get; set; }

        public Vector2 TooltipSize { get; set; }

        public FlightDataPoint FlightDataPoint { get; set; }

        public FlightChartStep(int index, string label, DateTime date, double chartElement)
        {
            Index = index;
            Label = label;
            Date = date;
            Value = chartElement;
        }
    }

}
