/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

#if MK_HDRP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.EdgeDetection.Examples
{
    public class MKEdgeDetectionHighDefinitionVolumeComponentScriptingExample : MonoBehaviour
    {
        //1) Apply the Volume in the inspector
        //Unity.RenderPipelines.Core.Runtime assembly reference required
        public UnityEngine.Rendering.Volume volume;

        void Start()
        {
            if(volume == null)
                return;

            //2) get the MK Edge Detection Component
            //MKEdgeDetectionComponents assembly reference required
            //Unity.RenderPipelines.HighDefinition.Runtime assembly reference required
            MK.EdgeDetection.HighDefinitionVolumeComponents.MKEdgeDetection volumeComponent;
            volume.TryGetComponent(out volumeComponent);

            if(volumeComponent == null)
                return;
            
            //3) Change the values of the post processing parameters
            volumeComponent.globalLineSize.value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty
            (
                1,
                volumeComponent.globalLineSize.value.minValue,
                volumeComponent.globalLineSize.value.maxValue
            );
        }
    }
}
#endif