/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

#if UNITY_EDITOR
using UnityEngine;

namespace MK.EdgeDetection.Editor.InstallWizard
{
    public enum RenderPipeline
    {
        [UnityEngine.InspectorName("Built-in Image Effects (Legacy)")]
        Built_in_Legacy = 0,
        [UnityEngine.InspectorName("Built-in + Post Processing Stack V2")]
        Built_in_PostProcessingStack = 1,
        //Lightweight = 2,
        [UnityEngine.InspectorName("Universal 3D")]
        Universal3D = 2,
        #if UNITY_2021_2_OR_NEWER
        [UnityEngine.InspectorName("Universal 2D")]
        Universal2D = 3,
        #endif
        [UnityEngine.InspectorName("High Definition")]
        High_Definition = 4
    }
}
#endif