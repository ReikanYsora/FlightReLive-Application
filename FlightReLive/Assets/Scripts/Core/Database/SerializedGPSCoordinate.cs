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
        [Key(0)] public double Latitude { get; set; }

        [Key(1)] public double Longitude { get; set; }
        #endregion

        #region CONSTRUCTOR
        public SerializedGPSCoordinate() { }

        public SerializedGPSCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        #endregion
    }
}