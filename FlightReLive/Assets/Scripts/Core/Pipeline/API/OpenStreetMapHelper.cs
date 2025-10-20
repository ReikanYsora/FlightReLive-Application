using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using FlightReLive.Core.Database;

namespace FlightReLive.Core.Pipeline.API
{
    public static class OpenStreetMapHelper
    {
        #region METHODS
        /// <summary>
        /// Open OpenStreetMap in the browser centered on a single GPS coordinate.
        /// </summary>
        internal static void OpenOpenStreetMapBrowser(SerializedGPSCoordinate gpsCoord, int zoomLevel = 14)
        {
            string latitude = gpsCoord.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitude = gpsCoord.Longitude.ToString(CultureInfo.InvariantCulture);
            string fullUrl = $"https://www.openstreetmap.org/?mlat={latitude}&mlon={longitude}#map={zoomLevel}/{latitude}/{longitude}";

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Process.Start(new ProcessStartInfo(fullUrl) { UseShellExecute = true });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Process.Start("open", fullUrl);
#else
            Application.OpenURL(fullUrl);
#endif
        }

        /// <summary>
        /// Open OpenStreetMap centered on the bounding box of all given GPS coordinates.
        /// Each Vector2 is (X = Latitude, Y = Longitude).
        /// </summary>
        internal static void OpenOpenStreetMapBrowser(List<SerializedGPSCoordinate> gpsPoints, int zoomLevel = 10)
        {
            if (gpsPoints == null || gpsPoints.Count == 0)
            {
                return;
            }

            double minLat = double.MaxValue;
            double maxLat = double.MinValue;
            double minLon = double.MaxValue;
            double maxLon = double.MinValue;

            foreach (SerializedGPSCoordinate point in gpsPoints)
            {
                if (point.Latitude < minLat)
                {
                    minLat = point.Latitude;
                }

                if (point.Latitude > maxLat)
                {
                    maxLat = point.Latitude;
                }

                if (point.Longitude < minLon)
                {
                    minLon = point.Longitude;
                }

                if (point.Longitude > maxLon)
                {
                    maxLon = point.Longitude;
                }
            }

            //Bounding box central point
            double centerLat = (minLat + maxLat) / 2f;
            double centerLon = (minLon + maxLon) / 2f;

            string latitude = centerLat.ToString(CultureInfo.InvariantCulture);
            string longitude = centerLon.ToString(CultureInfo.InvariantCulture);

            string fullUrl = $"https://www.openstreetmap.org/?mlat={latitude}&mlon={longitude}#map={zoomLevel}/{latitude}/{longitude}";

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Process.Start(new ProcessStartInfo(fullUrl) { UseShellExecute = true });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Process.Start("open", fullUrl);
#else
            Application.OpenURL(fullUrl);
#endif
        }
        #endregion
    }
}
