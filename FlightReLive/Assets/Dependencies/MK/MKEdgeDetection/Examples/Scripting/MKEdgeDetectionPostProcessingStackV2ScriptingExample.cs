/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

#if MK_POST_PROCESSING_STACK_V2
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.EdgeDetection.Examples
{
    public class MKEdgeDetectionPostProcessingStackV2ScriptingExample : MonoBehaviour
    {
        //1) Apply the Post Process Volume in the inspector
        //Unity.Postprocessing.Runtime assembly reference required
        public UnityEngine.Rendering.PostProcessing.PostProcessVolume volume;

        void Start()
        {
            if(volume == null)
                return;

            //2) get the MK Edge Detection Component
            //MKEdgeDetectionComponents assembly reference required
            MK.EdgeDetection.PostProcessingStackV2VolumeComponents.MKEdgeDetection volumeComponent;
            volume.profile.TryGetSettings(out volumeComponent);

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