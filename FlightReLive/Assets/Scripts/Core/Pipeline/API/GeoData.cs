using System;
using System.Collections.Generic;

namespace FlightReLive.Core.Pipeline
{
    [Serializable]
    public class FeatureCollection
    {
        public string type;
        public List<Feature> features;
    }

    [Serializable]
    public class Feature
    {
        public string type;
        public Properties properties;
        public Geometry geometry;
        public List<float> bbox;
        public List<float> center;
        public string place_name;
        public List<string> place_type;
        public string id;
        public string text;
        public List<string> place_type_name;
        public List<Context> context;
    }

    [Serializable]
    public class Properties
    {
        public string refId;
        public string country_code;
        public string kind;
        public List<string> place_type_name;
    }

    [Serializable]
    public class Geometry
    {
        public string type;
        public List<float> coordinates;
    }

    [Serializable]
    public class Context
    {
        public string refId;
        public string id;
        public string text;
        public string country_code;
        public string kind;
        public string wikidata;
        public string text_fr;
        public string text_en;
        public string language;
        public string language_fr;
        public string language_en;
    }
}
