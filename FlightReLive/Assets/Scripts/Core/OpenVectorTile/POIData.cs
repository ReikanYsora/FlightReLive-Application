using UnityEngine;

namespace FlightReLive.Core.OpenVectorTile
{
    /// <summary>
    /// Lightweight baked POI data extracted from OpenMapTiles.
    /// Used by POIManager for dynamic visibility & instantiation.
    /// </summary>
    public struct POIData
    {
        public string Name;                     //Label / name of POI
        public OpenMapTileFeatureType Type;     //Type of feature (POI, Place, MountainPeak, etc.)
        public Vector3 WorldPosition;           //Precomputed world-space position
        public int Rank;                        //Importance rank (1 = most important)

        public POIData(string name, OpenMapTileFeatureType type, Vector3 worldPos, int rank)
        {
            Name = name;
            Type = type;
            WorldPosition = worldPos;
            Rank = rank;
        }
    }
}
