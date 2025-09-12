using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Pipeline
{
    public class TileDefinition
    {
        #region PROPERTIES
        public GPSBoundingBox BoundingBox { set; get; }

        public TilePriority TilePriority { set; get; }

        public int ZoomLevel { set; get; }

        public int X { set; get; }

        public int Y { set; get; }

        public Texture2D SatelliteTexture { set; get; }

        public float[,] HeightMap { set; get; }

        public MeshData MeshData { set; get; }

        public List<BuildingData> Buildings { get; internal set; }

        internal FeatureCollection GeoData { get; set; }
        #endregion
    }
}
