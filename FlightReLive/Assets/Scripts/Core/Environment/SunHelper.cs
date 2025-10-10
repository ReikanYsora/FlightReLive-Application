using System;
using UnityEngine;

namespace FlightReLive.Core.Environment
{
    internal static class SunHelper
    {

        /// <summary>
        /// Computes precise sun position (azimuth/elevation) from UTC date, latitude and longitude using NOAA algorithm.
        /// </summary>
        internal static SunPosition CalculateSunPosition(DateTime utcTime, double latitude, double longitude)
        {
            //Convert to Julian Day
            double julianDay = utcTime.ToOADate() + 2415018.5;
            double julianCentury = (julianDay - 2451545.0) / 36525.0;

            //Mean longitude, anomaly, eccentricity
            double geomMeanLongSun = (280.46646 + julianCentury * (36000.76983 + julianCentury * 0.0003032)) % 360.0;
            double geomMeanAnomSun = 357.52911 + julianCentury * (35999.05029 - 0.0001537 * julianCentury);
            double eccentEarthOrbit = 0.016708634 - julianCentury * (0.000042037 + 0.0000001267 * julianCentury);

            //Sun equation of center
            double sunEqOfCenter = Math.Sin(Mathf.Deg2Rad * (float)geomMeanAnomSun) * (1.914602 - julianCentury * (0.004817 + 0.000014 * julianCentury))
                                 + Math.Sin(Mathf.Deg2Rad * (float)(2 * geomMeanAnomSun)) * (0.019993 - 0.000101 * julianCentury)
                                 + Math.Sin(Mathf.Deg2Rad * (float)(3 * geomMeanAnomSun)) * 0.000289;

            //True longitude
            double sunTrueLong = geomMeanLongSun + sunEqOfCenter;

            //Apparent longitude (correction nutation + aberration)
            double omega = 125.04 - 1934.136 * julianCentury;
            double sunAppLong = sunTrueLong - 0.00569 - 0.00478 * Math.Sin(Mathf.Deg2Rad * (float)omega);

            //Mean obliquity of ecliptic
            double meanObliqEcliptic = 23.0 + (26.0 + ((21.448 - julianCentury * (46.815 + julianCentury * (0.00059 - julianCentury * 0.001813)))) / 60.0) / 60.0;
            double obliqCorr = meanObliqEcliptic + 0.00256 * Math.Cos(Mathf.Deg2Rad * (float)omega);

            //Declination
            double declination = Math.Asin(Math.Sin(Mathf.Deg2Rad * (float)obliqCorr) * Math.Sin(Mathf.Deg2Rad * (float)sunAppLong));

            //Equation of time (in minutes)
            double y = Math.Tan(Mathf.Deg2Rad * (float)(obliqCorr / 2.0)) * Math.Tan(Mathf.Deg2Rad * (float)(obliqCorr / 2.0));
            double eqTime = 4.0 * (y * Math.Sin(2.0 * Mathf.Deg2Rad * (float)geomMeanLongSun)
                - 2.0 * eccentEarthOrbit * Math.Sin(Mathf.Deg2Rad * (float)geomMeanAnomSun)
                + 4.0 * eccentEarthOrbit * y * Math.Sin(Mathf.Deg2Rad * (float)geomMeanAnomSun) * Math.Cos(2.0 * Mathf.Deg2Rad * (float)geomMeanLongSun)
                - 0.5 * y * y * Math.Sin(4.0 * Mathf.Deg2Rad * (float)geomMeanLongSun)
                - 1.25 * eccentEarthOrbit * eccentEarthOrbit * Math.Sin(2.0 * Mathf.Deg2Rad * (float)geomMeanAnomSun));

            //True solar time (degrees)
            double timeOffset = eqTime + 4.0 * longitude - 0.0; // UTC offset already zero
            double trueSolarTime = (utcTime.TimeOfDay.TotalMinutes + timeOffset) % 1440.0;

            //Hour angle
            double hourAngle = (trueSolarTime / 4.0 < 0) ? trueSolarTime / 4.0 + 180.0 : trueSolarTime / 4.0 - 180.0;

            //Elevation
            double haRad = Mathf.Deg2Rad * (float)hourAngle;
            double latRad = Mathf.Deg2Rad * (float)latitude;
            double declRad = declination;
            double elevationRad = Math.Asin(Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad));

            double elevation = elevationRad * Mathf.Rad2Deg;

            //Azimuth
            double azimuth = (Math.Atan2(Math.Sin(haRad), Math.Cos(haRad) * Math.Sin(latRad) - Math.Tan(declRad) * Math.Cos(latRad)) * Mathf.Rad2Deg + 180.0) % 360.0;
            float unityAzimuth = (float)((360.0 - azimuth) % 360.0);

            float factor = Mathf.Clamp01((float)((elevation + 6.0) / 96.0)); // -6�=twilight start, 90�=zenith

            return new SunPosition
            {
                Elevation = (float)elevation,
                Azimuth = unityAzimuth,
                AzimuthPhysical = (float)azimuth,
                DistanceFactor = factor,
                ElevationFactor = Mathf.Clamp01((float)elevation / 90f)
            };
        }

        /// <summary>
        /// Returns a perceptual sun intensity factor (0 to 1) based on elevation angle.
        /// </summary>
        /// <param name="elevation"></param>
        /// <returns></returns>
        internal static float GetPerceptualSunFactor(float elevation)
        {
            float factor = Mathf.InverseLerp(-6f, 60f, elevation);
            factor = Mathf.Pow(factor, 0.9f);

            return Mathf.Clamp01(factor);
        }

        /// <summary>
        /// Estimates sunrise and sunset times (UTC) for a given date and position.
        /// Uses elevation crossing 0° criterion, derived from existing SunHelper calculations.
        /// </summary>
        internal static SunTimes GetSunriseSunset(DateTime dateUtc, double latitude, double longitude, float stepMinutes = 2f)
        {
            SunTimes result = new SunTimes
            {
                HasSunrise = false,
                HasSunset = false
            };

            if (dateUtc.Kind != DateTimeKind.Utc)
                dateUtc = dateUtc.ToUniversalTime();

            DateTime dayStart = dateUtc.Date;
            DateTime dayEnd = dayStart.AddDays(1);

            float previousElevation = CalculateSunPosition(dayStart, latitude, longitude).Elevation;
            DateTime previousTime = dayStart;

            for (DateTime t = dayStart.AddMinutes(stepMinutes); t <= dayEnd; t = t.AddMinutes(stepMinutes))
            {
                float currentElevation = CalculateSunPosition(t, latitude, longitude).Elevation;

                // Detect sunrise (crossing from negative to positive)
                if (!result.HasSunrise && previousElevation < 0f && currentElevation >= 0f)
                {
                    result.HasSunrise = true;
                    result.SunriseUTC = InterpolateTime(previousTime, t, previousElevation, currentElevation, 0f);
                }

                // Detect sunset (crossing from positive to negative)
                if (!result.HasSunset && previousElevation > 0f && currentElevation <= 0f)
                {
                    result.HasSunset = true;
                    result.SunsetUTC = InterpolateTime(previousTime, t, previousElevation, currentElevation, 0f);
                }

                previousElevation = currentElevation;
                previousTime = t;
            }

            return result;
        }

        /// <summary>
        /// Simple linear interpolation of the moment when elevation crosses the target (usually 0°).
        /// </summary>
        private static DateTime InterpolateTime(DateTime t1, DateTime t2, float elev1, float elev2, float target)
        {
            float ratio = Mathf.InverseLerp(elev1, elev2, target);
            double seconds = (t2 - t1).TotalSeconds * ratio;
            return t1.AddSeconds(seconds);
        }
    }
}
