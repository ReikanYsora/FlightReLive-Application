using MessagePack;
using UnityEngine;

namespace FlightReLive.Core.FFmpeg
{
    [MessagePackObject]
    public class SerializableVector2
    {
        [Key(0)]
        public float x { get; set; }

        [Key(1)]
        public float y { get; set; }

        public SerializableVector2() { }

        public SerializableVector2(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }
}
