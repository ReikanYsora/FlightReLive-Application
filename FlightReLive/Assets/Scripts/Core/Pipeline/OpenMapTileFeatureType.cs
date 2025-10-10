
namespace FlightReLive.Core
{
    /// <summary>
    /// Enum defining all supported feature types.
    /// These IDs must remain stable for backward compatibility with cached data.
    /// </summary>
    public enum OpenMapTileFeatureType
    {
        Building = 0,
        Landuse = 1,
        Landcover = 2,
        Water = 3,
        Park = 4,
        Aeroway = 5,
        POI = 6,
        Place = 7,
        MountainPeak = 8,
        HouseNumber = 9,
        AerodromeLabel = 10,
        Transportation = 11,
        TransportationName = 12,
        Boundary = 13,
        Waterway = 14,
        WaterName = 15
    }
}
