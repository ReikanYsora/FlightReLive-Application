using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Workspace
{
    public class FlightFile
    {
        #region PROPERTIES
        public string Name { get; set; }

        public string VideoPath { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTime CreationDate { get; set; }

        public Texture2D Thumbnail { get; set; }

        public FlightGPSData EstimateTakeOffPosition { get; set; }

        public List<FlightDataPoint> DataPoints { get; set; }

        public SerializableVector2 FlightGPSCoordinates { get; set; }

        public bool HasExtractionError { get; set; }

        public bool HasTakeOffPosition { get; set; }

        public bool IsValid { get; set; }

        public List<string> ErrorMessages { get; set; }
        #endregion

        #region CONSTRUCTOR
        public FlightFile()
        {
            DataPoints = new List<FlightDataPoint>();
        }
        #endregion
    }
}
