using System.Collections.Generic;
using FlightReLive.Core.Pipeline;
using MessagePack;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core
{
    [MessagePackObject]
    public abstract class OpenMapTileFeature
    {
        [Key(0)]
        public List<List<SerializablePoint2D>> Geometry { get; set; }

        [Key(1)]
        public OpenMapTileFeatureType FeatureType { get; set; }

        [IgnoreMember]
        internal TileDefinition TileDefinition { get; set; }
    }

    #region EXISTING FEATURES
    /// <summary>
    /// Buildings
    /// </summary>
    [MessagePackObject]
    public class BuildingFeature : OpenMapTileFeature
    {
        [Key(2)] public float RenderHeight { get; set; }
        [Key(3)] public float RenderMinHeight { get; set; }
    }

    /// <summary>
    /// Land feature
    /// </summary>
    [MessagePackObject]
    public class LanduseFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
    }

    /// <summary>
    /// Land cover
    /// </summary>
    [MessagePackObject]
    public class LandcoverFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
        [Key(3)] public string Subclass { get; set; }
    }

    /// <summary>
    /// Water
    /// </summary>
    [MessagePackObject]
    public class WaterFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
        [Key(3)] public bool IsIntermittent { get; set; }
    }

    /// <summary>
    /// Park
    /// </summary>
    [MessagePackObject]
    public class ParkFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
        [Key(3)] public string Rank { get; set; }
    }

    /// <summary>
    /// Aeroway
    /// </summary>
    [MessagePackObject]
    public class AerowayFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
    }

    /// <summary>
    /// Points of Interest (shops, restaurants, etc.)
    /// </summary>
    [MessagePackObject]
    public class POIFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
        [Key(3)] public string Subclass { get; set; }
        [Key(4)] public string Name { get; set; }
        [Key(5)] public string Rank { get; set; }
    }

    /// <summary>
    /// Place labels (cities, towns, villages, etc.)
    /// </summary>
    [MessagePackObject]
    public class PlaceFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }    // city / town / village / locality
        [Key(3)] public string Name { get; set; }
        [Key(4)] public int Rank { get; set; }
        [Key(5)] public int Population { get; set; }
    }

    /// <summary>
    /// Mountain peaks (summits)
    /// </summary>
    [MessagePackObject]
    public class MountainPeakFeature : OpenMapTileFeature
    {
        [Key(2)] public string Name { get; set; }
        [Key(3)] public float Elevation { get; set; }
        [Key(4)] public string Rank { get; set; }
    }

    /// <summary>
    /// House numbers and addresses.
    /// </summary>
    [MessagePackObject]
    public class HouseNumberFeature : OpenMapTileFeature
    {
        [Key(2)] public string Housenumber { get; set; }
        [Key(3)] public string Street { get; set; }
    }

    /// <summary>
    /// Aerodrome / Airport label (usually point label, not polygon).
    /// </summary>
    [MessagePackObject]
    public class AerodromeLabelFeature : OpenMapTileFeature
    {
        [Key(2)] public string Name { get; set; }
        [Key(3)] public string Icao { get; set; }
        [Key(4)] public string Iata { get; set; }
        [Key(5)] public string Class { get; set; } // international / regional / small_airport
    }

    /// <summary>
    /// Transportation lines (roads, rails, etc.)
    /// </summary>
    [MessagePackObject]
    public class TransportationFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; } // motorway, primary, secondary, rail, etc.
        [Key(3)] public string Subclass { get; set; }
        [Key(4)] public bool IsBridge { get; set; }
        [Key(5)] public bool IsTunnel { get; set; }
        [Key(6)] public int Layer { get; set; }
    }

    /// <summary>
    /// Transportation name labels (usually text points over roads).
    /// </summary>
    [MessagePackObject]
    public class TransportationNameFeature : OpenMapTileFeature
    {
        [Key(2)] public string Name { get; set; }
        [Key(3)] public string Ref { get; set; }
        [Key(4)] public string Class { get; set; }
    }

    /// <summary>
    /// Boundaries (country, region, admin)
    /// </summary>
    [MessagePackObject]
    public class BoundaryFeature : OpenMapTileFeature
    {
        [Key(2)] public string AdminLevel { get; set; } // 2 = country, 4 = region, etc.
        [Key(3)] public string Maritime { get; set; }
    }

    /// <summary>
    /// Waterways (rivers, canals, streams)
    /// </summary>
    [MessagePackObject]
    public class WaterwayFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; } // river, stream, canal
        [Key(3)] public bool IsIntermittent { get; set; }
    }

    /// <summary>
    /// Water labels (names of rivers/lakes)
    /// </summary>
    [MessagePackObject]
    public class WaterNameFeature : OpenMapTileFeature
    {
        [Key(2)] public string Name { get; set; }
        [Key(3)] public string Class { get; set; }
    }
    #endregion

    #region POINT STRUCT
    [MessagePackObject]
    public struct SerializablePoint2D
    {
        [Key(0)] public int X { get; set; }
        [Key(1)] public int Y { get; set; }

        public SerializablePoint2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point2d<int> ToPoint2D()
        {
            return new Point2d<int>(X, Y);
        }

        public static SerializablePoint2D FromPoint2D(Point2d<int> point)
        {
            return new SerializablePoint2D(point.X, point.Y);
        }
    }
    #endregion
}
