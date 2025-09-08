using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Workspace
{
    public class FlightFile
    {
        public string Name { get; set; }

        public string VideoPath { get; set; }

        public DateTime Date { get; set; }

        public FlightGPSData EstimateTakeOffPosition { get; set; }

        public List<FlightDataPoint> DataPoints { get; set; }

        public SerializableVector2 FlightGPSCoordinates { get; set; }

        public Texture2D Thumbnail { get; set; }

        public TimeSpan Length { get; set; }

        public bool HasExtractionError { get; set; }

        public bool IsValid { get; set; }

        public List<string> ErrorMessages { get; set; }

        public FlightFile()
        {
            DataPoints = new List<FlightDataPoint>();
        }
    }
}
