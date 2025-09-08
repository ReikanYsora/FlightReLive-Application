using MessagePack;

namespace FlightReLive.Core.FlightDefinition
{
    [MessagePackObject]
    public class FlightGPSData
    {
        [Key(0)]
        public double Latitude { get; set; }

        [Key(1)]
        public double Longitude { get; set; }

        public FlightGPSData() { }

        public FlightGPSData(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
