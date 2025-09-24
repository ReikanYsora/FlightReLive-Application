using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace FlightReLive.Core.Cache
{
    public sealed class CustomResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new CustomResolver();

        private CustomResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(OpenMapTileFeature))
            {
                return (IMessagePackFormatter<T>)(object)new OpenMapTileFeatureFormatter();
            }

            return StandardResolverAllowPrivate.Instance.GetFormatter<T>();
        }
    }
}
