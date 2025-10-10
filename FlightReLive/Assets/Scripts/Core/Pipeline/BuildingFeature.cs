using System.Collections.Generic;
using FlightReLive.Core.Pipeline;
using MessagePack;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core
{
    [MessagePackObject]
    public class BuildingFeature
    {
        [Key(0)]
        public List<List<SerializablePoint2D>> Geometry { get; set; }

        [Key(1)] public float RenderHeight { get; set; }

        [Key(2)] public float RenderMinHeight { get; set; }

        [IgnoreMember]
        internal TileDefinition TileDefinition { get; set; }
    }

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
