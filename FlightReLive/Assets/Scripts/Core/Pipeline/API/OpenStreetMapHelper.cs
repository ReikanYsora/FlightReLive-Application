using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace FlightReLive.Core.Pipeline.API
{
    public static class OpenStreetMapHelper
    {
        #region METHODS
        /// <summary>
        /// Open OpenStreetMap in the browser centered on a single GPS coordinate.
        /// </summary>
        internal static void OpenOpenStreetMapBrowser(Vector2 gpsCoord, int zoomLevel = 14)
        {
            string latitude = gpsCoord.x.ToString(CultureInfo.InvariantCulture);
            string longitude = gpsCoord.y.ToString(CultureInfo.InvariantCulture);
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
        internal static void OpenOpenStreetMapBrowser(List<Vector2> gpsPoints, int zoomLevel = 10)
        {
            if (gpsPoints == null || gpsPoints.Count == 0)
            {
                return;
            }

            float minLat = float.MaxValue;
            float maxLat = float.MinValue;
            float minLon = float.MaxValue;
            float maxLon = float.MinValue;

            foreach (Vector2 point in gpsPoints)
            {
                if (point.x < minLat)
                {
                    minLat = point.x;
                }

                if (point.x > maxLat)
                {
                    maxLat = point.x;
                }

                if (point.y < minLon)
                {
                    minLon = point.y;
                }

                if (point.y > maxLon)
                {
                    maxLon = point.y;
                }
            }

            //Bounding box central point
            float centerLat = (minLat + maxLat) / 2f;
            float centerLon = (minLon + maxLon) / 2f;

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
