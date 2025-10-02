using System;

namespace FlightReLive.Core.Share
{
    [Serializable]
    internal class FlightFileShareResponse
    {
        public string ShareHash { get; set; }

        public DateTime ExpirationDate { get; set; }
    }

}
