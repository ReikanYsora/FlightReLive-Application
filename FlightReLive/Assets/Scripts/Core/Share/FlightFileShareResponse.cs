using System;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightFileShareResponse
    {
        public string ShareHash { get; set; }

        public string ShareHashDisplay { get; set; }

        public DateTime ExpirationDateUtc { get; set; }
    }
}
