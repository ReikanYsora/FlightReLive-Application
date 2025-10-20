using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightReLive.Core.Pipeline
{
    internal class MapTilesDefinition
    {
        #region ATTRIBUTES
        private List<TileDefinition> _tileDefinitions = new List<TileDefinition>();
        #endregion

        #region PROPERTIES
        internal double OriginLatitude { get; private set; }

        internal double OriginLongitude { get; private set; }

        internal GPSBoundingBox MapBoundingBox { get; private set; }

        internal List<TileDefinition> TileDefinitions
        {
            get
            {
                return _tileDefinitions;
            }
        }

        #endregion

        #region CONSTRUCTOR
        internal MapTilesDefinition(double originLat, double originLon)
        {
            OriginLatitude = originLat;
            OriginLongitude = originLon;
            MapBoundingBox = new GPSBoundingBox();
        }
        #endregion

        #region METHODS
        internal void UpdateBoundingBoxFromTiles()
        {
            if (_tileDefinitions == null || _tileDefinitions.Count == 0)
            {
                return;
            }

            float minLat = float.MaxValue;
            float maxLat = float.MinValue;
            float minLon = float.MaxValue;
            float maxLon = float.MinValue;

            foreach (var tile in _tileDefinitions)
            {
                GPSBoundingBox bbox = tile.BoundingBox;
                minLat = (float)Math.Min(minLat, bbox.MinLatitude);
                maxLat = (float)Math.Max(maxLat, bbox.MaxLatitude);
                minLon = (float)Math.Min(minLon, bbox.MinLongitude);
                maxLon = (float)Math.Max(maxLon, bbox.MaxLongitude);
            }

            MapBoundingBox = new GPSBoundingBox
            {
                MinLatitude = minLat,
                MaxLatitude = maxLat,
                MinLongitude = minLon,
                MaxLongitude = maxLon
            };
        }

        internal void AddTile(TileDefinition addDefinition)
        {
            if (addDefinition != null)
            {
                _tileDefinitions.Add(addDefinition);
                UpdateBoundingBoxFromTiles();
            }
        }

        internal List<TileDefinition> GetSortedTiles()
        {
            return _tileDefinitions
                .OrderBy(t => t.X)
                .ThenByDescending(t => t.Y)
                .ToList();
        }
        #endregion
    }
}
