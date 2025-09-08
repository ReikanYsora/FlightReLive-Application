using System.Collections.Generic;
using MessagePack;
using VexTile.Mapbox.VectorTile.Geometry;

namespace FlightReLive.Core
{
    [MessagePackObject]
    public class BuildingData
    {
        #region PROPERTIES
        [Key(0)]
        public List<List<SerializablePoint2D>> Geometry { get; set; }

        [Key(1)]
        public float Height { get; set; }

        [Key(2)]
        public Dictionary<string, string> Properties { get; set; }
        #endregion

        #region METHODS
        public List<List<Point2d<int>>> GetGeometryAsPoint2D()
        {
            var result = new List<List<Point2d<int>>>();

            foreach (var ring in Geometry)
            {
                var convertedRing = new List<Point2d<int>>();
                foreach (var pt in ring)
                {
                    convertedRing.Add(pt.ToPoint2D());
                }
                result.Add(convertedRing);
            }

            return result;
        }
        #endregion
    }

    [MessagePackObject]
    public struct SerializablePoint2D
    {
        #region PROPERTIES
        [Key(0)]
        public int X { get; set; }

        [Key(1)]
        public int Y { get; set; }
        #endregion

        #region CONSTRUCTOR
        public SerializablePoint2D(int x, int y)
        {
            X = x;
            Y = y;
        }
        #endregion

        #region METHODS
        public Point2d<int> ToPoint2D()
        {
            return new Point2d<int>(X, Y);
        }

        public static SerializablePoint2D FromPoint2D(Point2d<int> point)
        {
            return new SerializablePoint2D(point.X, point.Y);
        }
        #endregion
    }
}
