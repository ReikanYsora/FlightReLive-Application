namespace FlightReLive.Core.Loading
{
    internal class ResourceResult<T>
    {
        public T Data { get; set; }
        public TileResourceSource Source { get; set; }

        public ResourceResult(T data, TileResourceSource source)
        {
            Data = data;
            Source = source;
        }
    }
}
