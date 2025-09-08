using FlightReLive.Core.Pipeline;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.FlightDefinition
{
    [Serializable]
    internal class FlightData : IDisposable
    {
        #region PROPERTIES
        internal Guid ID { set; get; }

        internal string Name { set; get; }

        internal string VideoPath { set; get; }

        internal DateTime Date { set; get; }

        internal FlightGPSData _gps { set; get; }

        internal List<FlightDataPoint> Points { set; get; }

        internal MapTilesDefinition MapDefinition { set; get; }

        internal Texture2D Thumbnail { set; get; }

        internal Vector2 SceneCenterGPS { get; set; }
        #endregion

        #region PROPERTIES
        internal bool HasExtractionError { get; set; }

        internal bool IsValid { get; set; }

        internal FlightGPSData EstimateTakeOffPosition { set; get; }

        internal float TakeOffAltitude { get; set; }

        internal FlightGPSData GPSOrigin
        {
            get
            {
                return _gps;
            }
            set
            {
                _gps = value;
                MapDefinition = new MapTilesDefinition(_gps.Latitude, _gps.Longitude);
            }
        }

        internal TimeSpan Length { get; set; }
        #endregion

        #region METHODS
        public void Dispose()
        {
            foreach (TileDefinition tileDefinition in MapDefinition.TileDefinitions)
            {
                tileDefinition.SatelliteTexture = null;
                tileDefinition.HeightMap = null;
                tileDefinition.MeshData = null;
            }
        }
        #endregion
    }
}
