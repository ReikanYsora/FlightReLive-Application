/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.EdgeDetection
{
    public static class Config
    {
        private static Material _renderMaterial = null;
        //private static Material _selectiveRenderMaterial = null;
        private static Material _renderSketchMaterial = null;
        public const float kLineHardnessDamping = 0.5f;
        public const float kThresholdMultiplier = 10.0f;
        public const float kCombinedInputThresholdMultiplier = 1f;
        public static readonly Vector2 referenceResolution = new Vector2(1280, 720);
        public const float kHalfEpsilon = 6.10e-5f;

        private static readonly MK.EdgeDetection.Resources resources = UnityEngine.Resources.Load<MK.EdgeDetection.Resources>("MKEdgeDetectionResources");
        
        #if UNITY_EDITOR
        public static void SaveInstance()
        {
            UnityEditor.EditorUtility.SetDirty(resources);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
        #endif

        public static bool pixelPerfection
        {
            get
            {
                return resources.pixelPerfection;
            }
            set
            {
                resources.pixelPerfection = value;
            }
        }

        public static float ReceiveAdaptedThresholdMultiplier(bool enhancedDetails)
        {
            return kThresholdMultiplier;
            //return enhancedDetails ? kCombinedInputThresholdMultiplier : kThresholdMultiplier;
        }

        public static Material ReceiveRenderMaterial()
        {
            if(_renderMaterial == null)
            {
                _renderMaterial = new Material(resources.shaderMain) { hideFlags = HideFlags.HideAndDontSave };
            }
            return _renderMaterial;
        }

        public static Material ReceiveSketchRenderMaterial()
        {
            if(_renderSketchMaterial == null)
            {
                _renderSketchMaterial = new Material(resources.shaderSketch) { hideFlags = HideFlags.HideAndDontSave };
            }
            return _renderSketchMaterial;
        }
        
        /*
        public static Material ReceiveSelectiveRenderMaterial()
        {
            if(_selectiveRenderMaterial == null)
            {
                _selectiveRenderMaterial = new Material(resources.selectiveMaskShader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return _selectiveRenderMaterial;
        }
        */
    }
}