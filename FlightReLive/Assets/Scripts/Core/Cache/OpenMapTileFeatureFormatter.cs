using System;
using MessagePack;
using MessagePack.Formatters;

namespace FlightReLive.Core.Cache
{
    public class OpenMapTileFeatureFormatter : IMessagePackFormatter<OpenMapTileFeature>
    {
        public void Serialize(ref MessagePackWriter writer, OpenMapTileFeature value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            //Format = [ typeId, object ]
            writer.WriteArrayHeader(2);

            switch (value)
            {
                case BuildingFeature b:
                    writer.Write(0);
                    MessagePackSerializer.Serialize(ref writer, b, options);
                    break;
                case LanduseFeature l:
                    writer.Write(1);
                    MessagePackSerializer.Serialize(ref writer, l, options);
                    break;
                case LandcoverFeature lc:
                    writer.Write(2);
                    MessagePackSerializer.Serialize(ref writer, lc, options);
                    break;
                case WaterFeature w:
                    writer.Write(3);
                    MessagePackSerializer.Serialize(ref writer, w, options);
                    break;
                case ParkFeature p:
                    writer.Write(4);
                    MessagePackSerializer.Serialize(ref writer, p, options);
                    break;
                case AerowayFeature a:
                    writer.Write(5);
                    MessagePackSerializer.Serialize(ref writer, a, options);
                    break;
                default:
                    throw new NotSupportedException(value.GetType().FullName);
            }
        }

        public OpenMapTileFeature Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            int count = reader.ReadArrayHeader();
            if (count < 2)
            {
                throw new InvalidOperationException($"Invalid format for OpenMapTileFeature, expected at least 2 but got {count}");
            }

            int typeId = reader.ReadInt32();

            switch (typeId)
            {
                case 0:
                    return MessagePackSerializer.Deserialize<BuildingFeature>(ref reader, options);
                case 1:
                    return MessagePackSerializer.Deserialize<LanduseFeature>(ref reader, options);
                case 2:
                    return MessagePackSerializer.Deserialize<LandcoverFeature>(ref reader, options);
                case 3:
                    return MessagePackSerializer.Deserialize<WaterFeature>(ref reader, options);
                case 4:
                    return MessagePackSerializer.Deserialize<ParkFeature>(ref reader, options);
                case 5:
                    return MessagePackSerializer.Deserialize<AerowayFeature>(ref reader, options);
                default:
                    throw new NotSupportedException($"Unknown typeId {typeId}");
            }
        }
    }
}
