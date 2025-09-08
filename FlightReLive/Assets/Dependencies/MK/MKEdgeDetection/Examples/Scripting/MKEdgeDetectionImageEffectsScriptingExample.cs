/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.EdgeDetection.Examples
{
    public class MKEdgeDetectionImageEffectsScriptingExample : MonoBehaviour
    {
        //1) Apply the component from the rendering camera in the inspector
        //MKEdgeDetectionComponents assembly reference required
        public MK.EdgeDetection.ImageEffectsComponents.MKEdgeDetection imageEffectsComponent;

        void Start()
        {
            if(imageEffectsComponent == null)
                return;

            //2) change the values
            imageEffectsComponent.globalLineSize = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty
            (
                1,
                imageEffectsComponent.globalLineSize.minValue,
                imageEffectsComponent.globalLineSize.maxValue
            );
        }
    }
}
