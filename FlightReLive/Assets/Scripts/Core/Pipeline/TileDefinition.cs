using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.Pipeline
{
    public class TileDefinition
    {
        #region PROPERTIES
        internal GPSBoundingBox BoundingBox { set; get; }

        internal TilePriority TilePriority { set; get; }

        internal int ZoomLevel { set; get; }

        internal int X { set; get; }

        internal int Y { set; get; }

        internal Texture2D SatelliteTexture { set; get; }

        internal float[,] HeightMap { set; get; }

        internal MeshData MeshData { set; get; }

        internal List<BuildingData> Buildings { get; set; }

        internal FeatureCollection GeoData { get; set; }
        #endregion
    }
}
