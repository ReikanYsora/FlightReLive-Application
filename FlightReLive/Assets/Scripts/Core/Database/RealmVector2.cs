using Realms;
using UnityEngine;

namespace FlightReLive.Core.Database
{
    public class RealmVector2 : EmbeddedObject
    {
        #region PROPERTIES
        public float X { get; set; }

        public float Y { get; set; }
        #endregion

        #region CONSTRUCTOR
        public RealmVector2() { }

        public RealmVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public RealmVector2(Vector2 v)
        {
            X = v.x;
            Y = v.y;
        }
        #endregion

        #region METHODS

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        #endregion
    }
}
