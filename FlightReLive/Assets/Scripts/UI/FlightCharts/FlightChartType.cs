using MessagePack;

namespace FlightReLive.UI.FlightCharts
{
    public enum FlightChartType
    {
        Speed = 0,
        RelativeAltitude = 1,
        AbsoluteAltitude = 2,
        Aperture = 3,
        ShutterSpeed = 4,
        ISO = 5,
        Exposure = 6,
        DigitalZoom = 7
    }
}