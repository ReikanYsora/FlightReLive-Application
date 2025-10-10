using System;

namespace FlightReLive.Core.Environment
{
    /// <summary>
    /// Contains sunrise/sunset UTC times for a given date and location.
    /// </summary>
    internal struct SunTimes
    {
        public DateTime SunriseUTC;
        public DateTime SunsetUTC;
        public bool HasSunrise;
        public bool HasSunset;
    }
}