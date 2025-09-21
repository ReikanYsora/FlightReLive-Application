using System;
using UnityEngine;
using TND.Upscaling.Framework;

namespace TND.Upscaling.FSR3
{
    [Serializable]
    public class FSR3UpscalerSettings: UpscalerSettingsBase
    {
        public Texture transparencyAndCompositionMask;

        public bool enableDebugView;

        public AdvancedParameters advancedParameters = new()
        {
            velocityFactor = 1.0f,
            reactivenessScale = 1.0f,
            shadingChangeScale = 1.0f,
            accumulationAddedPerFrame = 1.0f / 3.0f,
            minDisocclusionAccumulation = -1.0f / 3.0f,
        };

        [Serializable]
        public struct AdvancedParameters
        {
            [Range(0, 1)] public float velocityFactor;
            [Range(0, 1)] public float reactivenessScale;
            [Range(0, 1)] public float shadingChangeScale;
            [Range(0, 1)] public float accumulationAddedPerFrame;
            [Range(-1, 1)] public float minDisocclusionAccumulation;
        }
    }
}
