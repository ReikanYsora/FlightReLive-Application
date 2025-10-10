using System;
using MessagePack;
using MessagePack.Formatters;

namespace FlightReLive.Core.Cache
{
    /// <summary>
    /// Custom formatter for OpenMapTileFeature hierarchy.
    /// Handles all derived types with a compact enum-based identifier.
    /// Format: [ typeId, object ]
    /// </summary>
    public class OpenMapTileFeatureFormatter : IMessagePackFormatter<OpenMapTileFeature>
    {
        #region SERIALIZE
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
                    writer.Write((int)OpenMapTileFeatureType.Building);
                    MessagePackSerializer.Serialize(ref writer, b, options);
                    break;
                case LanduseFeature l:
                    writer.Write((int)OpenMapTileFeatureType.Landuse);
                    MessagePackSerializer.Serialize(ref writer, l, options);
                    break;
                case LandcoverFeature lc:
                    writer.Write((int)OpenMapTileFeatureType.Landcover);
                    MessagePackSerializer.Serialize(ref writer, lc, options);
                    break;
                case WaterFeature w:
                    writer.Write((int)OpenMapTileFeatureType.Water);
                    MessagePackSerializer.Serialize(ref writer, w, options);
                    break;
                case ParkFeature p:
                    writer.Write((int)OpenMapTileFeatureType.Park);
                    MessagePackSerializer.Serialize(ref writer, p, options);
                    break;
                case AerowayFeature a:
                    writer.Write((int)OpenMapTileFeatureType.Aeroway);
                    MessagePackSerializer.Serialize(ref writer, a, options);
                    break;
                case POIFeature poi:
                    writer.Write((int)OpenMapTileFeatureType.POI);
                    MessagePackSerializer.Serialize(ref writer, poi, options);
                    break;
                case PlaceFeature place:
                    writer.Write((int)OpenMapTileFeatureType.Place);
                    MessagePackSerializer.Serialize(ref writer, place, options);
                    break;
                case MountainPeakFeature mp:
                    writer.Write((int)OpenMapTileFeatureType.MountainPeak);
                    MessagePackSerializer.Serialize(ref writer, mp, options);
                    break;
                case HouseNumberFeature hn:
                    writer.Write((int)OpenMapTileFeatureType.HouseNumber);
                    MessagePackSerializer.Serialize(ref writer, hn, options);
                    break;
                case AerodromeLabelFeature ad:
                    writer.Write((int)OpenMapTileFeatureType.AerodromeLabel);
                    MessagePackSerializer.Serialize(ref writer, ad, options);
                    break;
                case TransportationFeature tr:
                    writer.Write((int)OpenMapTileFeatureType.Transportation);
                    MessagePackSerializer.Serialize(ref writer, tr, options);
                    break;
                case TransportationNameFeature tn:
                    writer.Write((int)OpenMapTileFeatureType.TransportationName);
                    MessagePackSerializer.Serialize(ref writer, tn, options);
                    break;
                case BoundaryFeature bo:
                    writer.Write((int)OpenMapTileFeatureType.Boundary);
                    MessagePackSerializer.Serialize(ref writer, bo, options);
                    break;
                case WaterwayFeature ww:
                    writer.Write((int)OpenMapTileFeatureType.Waterway);
                    MessagePackSerializer.Serialize(ref writer, ww, options);
                    break;
                case WaterNameFeature wn:
                    writer.Write((int)OpenMapTileFeatureType.WaterName);
                    MessagePackSerializer.Serialize(ref writer, wn, options);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported OpenMapTileFeature type: {value.GetType().FullName}");
            }
        }
        #endregion

        #region DESERIALIZE
        public OpenMapTileFeature Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            int count = reader.ReadArrayHeader();
            if (count < 2)
            {
                throw new InvalidOperationException($"Invalid format for OpenMapTileFeature: expected at least 2 elements but got {count}");
            }

            int typeId = reader.ReadInt32();
            OpenMapTileFeature feature;

            switch ((OpenMapTileFeatureType)typeId)
            {
                case OpenMapTileFeatureType.Building:
                    feature = MessagePackSerializer.Deserialize<BuildingFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Landuse:
                    feature = MessagePackSerializer.Deserialize<LanduseFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Landcover:
                    feature = MessagePackSerializer.Deserialize<LandcoverFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Water:
                    feature = MessagePackSerializer.Deserialize<WaterFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Park:
                    feature = MessagePackSerializer.Deserialize<ParkFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Aeroway:
                    feature = MessagePackSerializer.Deserialize<AerowayFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.POI:
                    feature = MessagePackSerializer.Deserialize<POIFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Place:
                    feature = MessagePackSerializer.Deserialize<PlaceFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.MountainPeak:
                    feature = MessagePackSerializer.Deserialize<MountainPeakFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.HouseNumber:
                    feature = MessagePackSerializer.Deserialize<HouseNumberFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.AerodromeLabel:
                    feature = MessagePackSerializer.Deserialize<AerodromeLabelFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Transportation:
                    feature = MessagePackSerializer.Deserialize<TransportationFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.TransportationName:
                    feature = MessagePackSerializer.Deserialize<TransportationNameFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Boundary:
                    feature = MessagePackSerializer.Deserialize<BoundaryFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.Waterway:
                    feature = MessagePackSerializer.Deserialize<WaterwayFeature>(ref reader, options);
                    break;
                case OpenMapTileFeatureType.WaterName:
                    feature = MessagePackSerializer.Deserialize<WaterNameFeature>(ref reader, options);
                    break;
                default:
                    throw new NotSupportedException($"Unknown OpenMapTileFeature typeId: {typeId}");
            }

            feature.FeatureType = (OpenMapTileFeatureType)typeId;

            return feature;
        }
        #endregion
    }
}
