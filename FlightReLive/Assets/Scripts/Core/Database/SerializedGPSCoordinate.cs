using MessagePack;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Serializable double-precision 2D object for geospatial coordinates.
    /// </summary>
    [MessagePackObject]
    public class SerializedGPSCoordinate
    {
        #region PROPERTIES
        [Key(0)] public float Latitude { get; set; }

        [Key(1)] public float Longitude { get; set; }
        #endregion

        #region CONSTRUCTOR
        public SerializedGPSCoordinate() { }

        public SerializedGPSCoordinate(float latitude, float longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        #endregion
    }
}