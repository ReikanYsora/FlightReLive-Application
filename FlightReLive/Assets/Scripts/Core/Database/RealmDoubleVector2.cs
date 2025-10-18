
using Realms;

namespace FlightReLive.Core.Database
{
    public class RealmDoubleVector2 : EmbeddedObject
    {
        public double X { get; set; }

        public double Y { get; set; }

        public RealmDoubleVector2() { }

        public RealmDoubleVector2(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
