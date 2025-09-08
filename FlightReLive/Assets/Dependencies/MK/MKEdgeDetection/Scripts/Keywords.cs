/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MK.EdgeDetection
{
    public static class Keywords
    {
        //Data
        public static readonly string selectiveWorkflow = "_MK_SELECTIVE_WORKFLOW";
        public static readonly string inputDepth = "_MK_INPUT_DEPTH";
        public static readonly string inputNormal = "_MK_INPUT_NORMAL";
        public static readonly string inputSceneColor = "_MK_INPUT_SCENE_COLOR";
        public static readonly string fade = "_MK_FADE";
        public static readonly string sketch = "_MK_SKETCH";
        public static readonly string enhanceDetails = "_MK_ENHANCE_DETAILS";
        public static readonly string sketchAdditionalNoiseMap = "_MK_SKETCH_ADDITIONAL_NOISE_MAP";

        //Method
        public static readonly string precisionMedium = "_MK_PRECISION_MEDIUM";
        public static readonly string precisionLow = "_MK_PRECISION_LOW";

        //Kernel
        public static readonly string utilizeAxisOnly = "_MK_UTILIZE_AXIS_ONLY";

        //Debug
        public static readonly string visualizeEdges = "_MK_DEBUG_VISUALIZE_EDGES";
        
        public static void SetKeyword(Material material, string keyword, bool state) => MK.EdgeDetection.PostProcessing.Generic.MKBlitter.SetKeyword(material, keyword, state);
    }
}
