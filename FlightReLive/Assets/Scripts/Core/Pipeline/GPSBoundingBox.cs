namespace FlightReLive.Core.Pipeline
{
    public struct GPSBoundingBox
    {
        #region PROPERTIES
        public double MinLatitude { set; get; }
        public double MaxLatitude { set; get; }
        public double MinLongitude { set; get; }
        public double MaxLongitude { set; get; }
        #endregion
    }
}
