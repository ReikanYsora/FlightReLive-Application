using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.FlightDefinition;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Workspace
{
    [MessagePackObject]
    public class FlightFile
    {
        #region PROPERTIES
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public string VideoPath { get; set; }

        [Key(2)]
        public int Width { get; set; }

        [Key(3)]
        public int Height { get; set; }

        [Key(4)]
        public double Frequency { get; set; }

        [Key(5)]
        public TimeSpan Duration { get; set; }

        [Key(6)]
        public DateTime CreationDate { get; set; }

        [Key(7)]
        public byte[] MapData { get; set; }

        [Key(8)]
        public byte[] ThumbnailData { get; set; }

        [IgnoreMember]
        public Texture2D Map { get; set; }

        [IgnoreMember]
        public Texture2D Thumbnail { get; set; }

        [Key(9)]
        public FlightGPSData EstimateTakeOffPosition { get; set; }

        [Key(10)]
        public List<FlightDataPoint> DataPoints { get; set; }

        [Key(11)]
        public SerializableVector2 FlightGPSCoordinates { get; set; }

        [Key(12)]
        public bool HasExtractionError { get; set; }

        [Key(13)]
        public bool HasTakeOffPosition { get; set; }

        [Key(14)]
        public bool IsValid { get; set; }

        [Key(15)]
        public List<string> ErrorMessages { get; set; }
        #endregion

        #region CONSTRUCTOR
        public FlightFile()
        {
            DataPoints = new List<FlightDataPoint>();
            ErrorMessages = new List<string>();
        }
        #endregion

        #region METHODS
        public void EncodeTextures()
        {
            if (Map != null)
            {
                MapData = Map.EncodeToPNG();
            }

            if (Thumbnail != null)
            {
                ThumbnailData = Thumbnail.EncodeToPNG();
            }
        }

        public void DecodeTextures()
        {
            if (MapData != null && MapData.Length > 0)
            {
                Map = new Texture2D(2, 2);
                Map.LoadImage(MapData);
            }

            if (ThumbnailData != null && ThumbnailData.Length > 0)
            {
                Thumbnail = new Texture2D(2, 2);
                Thumbnail.LoadImage(ThumbnailData);
            }
        }
        #endregion
    }
}
