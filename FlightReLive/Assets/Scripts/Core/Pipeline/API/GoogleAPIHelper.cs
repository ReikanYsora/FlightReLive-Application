using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace FlightReLive.Core.Pipeline.API
{
    public static class GoogleAPIHelper
    {
        #region METHODS
        internal static void OpenGoogleMapsBrowser(Vector2 gpsCoord)
        {
            string latitude = gpsCoord.x.ToString(CultureInfo.InvariantCulture);
            string longitude = gpsCoord.y.ToString(CultureInfo.InvariantCulture);
            string fullUrl = $"https://www.google.com/maps/search/?api=1&query={latitude},{longitude}";

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Process.Start(new ProcessStartInfo(fullUrl) { UseShellExecute = true });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Process.Start("open", fullUrl);
#else
            Application.OpenURL(fullUrl);
#endif
        }

        internal static void OpenGoogleMapsBrowser(List<Vector2> gpsPoints)
        {
            if (gpsPoints == null || gpsPoints.Count < 2)
            {
                return;
            }

            string baseUrl = "https://www.google.com/maps/dir";
            List<string> segments = new List<string>();

            foreach (Vector2 point in gpsPoints)
            {
                string latitude = point.x.ToString(CultureInfo.InvariantCulture);
                string longitude = point.y.ToString(CultureInfo.InvariantCulture);
                segments.Add($"{latitude},{longitude}");
            }

            string fullUrl = $"{baseUrl}/{string.Join("/", segments)}";

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
