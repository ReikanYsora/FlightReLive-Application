using System;
using UnityEngine;

namespace FlightReLive.Core.Rendering
{
    [Serializable]
    internal class SkyboxPreset
    {
        [SerializeField] internal Material Material;
        [SerializeField] internal float Offset;
    }
}
