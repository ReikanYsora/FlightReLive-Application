using FlightReLive.UI;
using Fu;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class FlightReLiveWindowsNames : FuSystemWindowsNames
{
    #region ATTRIBUTES
    private static FuWindowName _ReLiveView = new FuWindowName(11, FlightReLiveIcons.Drone + "  ReLive", true, -1);

    public static FuWindowName ReLiveView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _ReLiveView; }

    private static FuWindowName _Workspace = new FuWindowName(12, FlightReLiveIcons.Workspace + "  Workspace", true, -1);

    public static FuWindowName Workspace { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Workspace; }

    private static FuWindowName _VideoPlayer = new FuWindowName(13, FlightReLiveIcons.VideoPlayer + "  Video Player", true, -1);

    public static FuWindowName VideoPlayer { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _VideoPlayer; }

    private static FuWindowName _FlightCharts = new FuWindowName(14, FlightReLiveIcons.Charts + "  Flight Charts", true, -1);

    public static FuWindowName FlightCharts { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _FlightCharts; }
    #endregion

    #region METHODS
    public static List<FuWindowName> GetAllWindowsNames()
    {
        return new List<FuWindowName>()
        {
            _ReLiveView,
            _Workspace,
            _VideoPlayer,
            _FlightCharts
        };
    }
    #endregion
}
