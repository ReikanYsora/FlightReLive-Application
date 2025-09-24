using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.ProceduralTerrain;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

            double minLat = double.MaxValue;
            double maxLat = double.MinValue;
            double minLon = double.MaxValue;
            double maxLon = double.MinValue;

            foreach (var tile in _tileDefinitions)
            {
                GPSBoundingBox bbox = tile.BoundingBox;
                minLat = Math.Min(minLat, bbox.MinLatitude);
                maxLat = Math.Max(maxLat, bbox.MaxLatitude);
                minLon = Math.Min(minLon, bbox.MinLongitude);
                maxLon = Math.Max(maxLon, bbox.MaxLongitude);
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
