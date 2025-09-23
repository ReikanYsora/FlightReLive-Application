using System;
using System.Collections.Generic;
using MessagePack;

namespace FlightReLive.Core.Pipeline
{
    [Serializable]
    [MessagePackObject]
    public class FeatureCollection
    {
        [Key(0)] public string type;
        [Key(1)] public List<Feature> features;
    }

    [Serializable]
    [MessagePackObject]
    public class Feature
    {
        [Key(0)] public string type;
        [Key(1)] public Properties properties;
        [Key(2)] public Geometry geometry;
        [Key(3)] public List<float> bbox;
        [Key(4)] public List<float> center;
        [Key(5)] public string place_name;
        [Key(6)] public List<string> place_type;
        [Key(7)] public string id;
        [Key(8)] public string text;
        [Key(9)] public List<string> place_type_name;
        [Key(10)] public List<Context> context;
    }

    [Serializable]
    [MessagePackObject]
    public class Properties
    {
        [Key(0)] public string refId;
        [Key(1)] public string country_code;
        [Key(2)] public string kind;
        [Key(3)] public List<string> place_type_name;
    }

    [Serializable]
    [MessagePackObject]
    public class Geometry
    {
        [Key(0)] public string type;
        [Key(1)] public List<float> coordinates;
    }

    [Serializable]
    [MessagePackObject]
    public class Context
    {
        [Key(0)] public string refId;
        [Key(1)] public string id;
        [Key(2)] public string text;
        [Key(3)] public string country_code;
        [Key(4)] public string kind;
        [Key(5)] public string wikidata;
        [Key(6)] public string text_fr;
        [Key(7)] public string text_en;
        [Key(8)] public string language;
        [Key(9)] public string language_fr;
        [Key(10)] public string language_en;
    }
}
