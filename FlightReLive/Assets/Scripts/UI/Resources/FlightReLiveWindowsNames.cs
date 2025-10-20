using FlightReLive.UI;
using Fu;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class FlightReLiveWindowsNames : FuSystemWindowsNames
{
    #region ATTRIBUTES
    private static FuWindowName _ReLiveView = new FuWindowName(11, FlightReLiveIcons.Drone + "  ReLive", true, -1);

    public static FuWindowName ReLiveView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _ReLiveView; }

    private static FuWindowName _Library = new FuWindowName(12, FlightReLiveIcons.Library + "  Library", true, -1);

    public static FuWindowName Library { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Library; }

    private static FuWindowName _Inspector = new FuWindowName(13, FlightReLiveIcons.Inspector + "  Inspector", true, -1);

    public static FuWindowName Inspector { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Inspector; }

    private static FuWindowName _FlightCharts = new FuWindowName(14, FlightReLiveIcons.Charts + "  Flight Charts", true, -1);

    public static FuWindowName FlightCharts { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _FlightCharts; }

    private static FuWindowName _POVView = new FuWindowName(15, FlightReLiveIcons.POV + "  POV View", true, -1);

    public static FuWindowName POVView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _POVView; }

    private static FuWindowName _MapView = new FuWindowName(16, FlightReLiveIcons.Maps + "  Map View", true, -1);

    public static FuWindowName MapView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _MapView; }
    #endregion

    #region METHODS
    public static List<FuWindowName> GetAllWindowsNames()
    {
        return new List<FuWindowName>()
        {
            _ReLiveView,
            _Library,
            _Inspector,
            _FlightCharts,
            _POVView,
            _MapView
        };
    }
    #endregion
}
