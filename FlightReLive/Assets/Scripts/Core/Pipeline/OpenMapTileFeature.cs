using System.Collections.Generic;
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
        public string LayerName { get; set; }

        [IgnoreMember] public Dictionary<string, string> Properties { get; set; }
    }

    [MessagePackObject]
    public class BuildingFeature : OpenMapTileFeature
    {
        [Key(2)] public float RenderHeight { get; set; }

        [Key(3)] public float RenderMinHeight { get; set; }

        [IgnoreMember]
        public float ExtrudeHeight => RenderHeight - RenderMinHeight;
    }

    [MessagePackObject]
    public class LanduseFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
    }

    [MessagePackObject]
    public class LandcoverFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }

        [Key(3)] public string Subclass { get; set; }
    }

    [MessagePackObject]
    public class WaterFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }

        [Key(3)] public bool IsIntermittent { get; set; }
    }

    [MessagePackObject]
    public class ParkFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }

        [Key(3)] public string Rank { get; set; }
    }

    [MessagePackObject]
    public class AerowayFeature : OpenMapTileFeature
    {
        [Key(2)] public string Class { get; set; }
    }


    [MessagePackObject]
    public struct SerializablePoint2D
    {
        [Key(0)]
        public int X { get; set; }

        [Key(1)]
        public int Y { get; set; }

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
}
