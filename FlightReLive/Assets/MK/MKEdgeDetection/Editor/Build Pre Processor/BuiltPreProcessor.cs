/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;
using UnityEditor.Rendering;
    
namespace MK.EdgeDetection.Editor.BuildPreProcessor
{
    class BuildPreProcessor : IPreprocessShaders
    {
        [MenuItem("Window/MK/Edge Detection/Variant Stripping")]
        private static void SelectAsset()
        {
            Selection.activeObject = _buildPreProcessorData;
            EditorGUIUtility.PingObject(_buildPreProcessorData);
        }

        private static MK.EdgeDetection.Editor.BuildPreProcessor.BuildPreProcessorData _buildPreProcessorData = null;

        [InitializeOnLoadMethod]
        private static void LoadData()
        {
            if(_buildPreProcessorData == null)
            {
                string[] _guids = AssetDatabase.FindAssets("t:" + typeof(MK.EdgeDetection.Editor.BuildPreProcessor.BuildPreProcessorData).Namespace + ".BuildPreProcessorData", null);
                if(_guids.Length > 0)
                {
                    _buildPreProcessorData = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(_guids[0]), typeof(MK.EdgeDetection.Editor.BuildPreProcessor.BuildPreProcessorData)) as MK.EdgeDetection.Editor.BuildPreProcessor.BuildPreProcessorData;
                }
            }
        }

        private ShaderKeyword _keywordWorkflowSelective;
        private ShaderKeyword _keywordPrecisionMedium;
        private ShaderKeyword _keywordPrecisionLow;
        private ShaderKeyword _keywordInputDataDepth;
        private ShaderKeyword _keywordInputDataNormal;
        private ShaderKeyword _keywordInputDataSceneColor;
        private ShaderKeyword _keywordFade;
        private ShaderKeyword _keywordEnhanceDetails;
        private ShaderKeyword _keywordVisualizeEdges;
        private ShaderKeyword _keywordUtilizeAxisOnly;
        private ShaderKeyword _keywordSketch;

        private void InitializeShaderKeywords(Shader shader)
        {
            _keywordWorkflowSelective = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.selectiveWorkflow);
            _keywordPrecisionMedium = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.precisionMedium);
            _keywordPrecisionLow = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.precisionLow);
            _keywordInputDataDepth = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.inputDepth);
            _keywordInputDataNormal = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.inputNormal);
            _keywordInputDataSceneColor = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.inputSceneColor);
            _keywordFade = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.fade);
            _keywordEnhanceDetails = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.enhanceDetails);
            _keywordVisualizeEdges = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.visualizeEdges);
            _keywordUtilizeAxisOnly = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.utilizeAxisOnly);
            _keywordSketch = new ShaderKeyword(shader, MK.EdgeDetection.Keywords.sketch);
        }

        public int callbackOrder { get { return 0; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if(shader.name != "Hidden/MK/Edge Detection/Main")
                return;

            if(_buildPreProcessorData == null)
                return;

            InitializeShaderKeywords(shader);

            for (int i = 0; i < data.Count; ++i)
            {
                bool removeVariant = false;
                ShaderKeywordSet keywordSet = data[i].shaderKeywordSet;

                //Precision High
                //Default

                //Precision Medium
                if(keywordSet.IsEnabled(_keywordPrecisionMedium) && !_buildPreProcessorData.precisionMedium)
                {
                    removeVariant = true;
                }

                //Precision Low
                if(keywordSet.IsEnabled(_keywordPrecisionLow) && !_buildPreProcessorData.precisionLow)
                {
                    removeVariant = true;
                }

                //Input Data Depth
                if(keywordSet.IsEnabled(_keywordInputDataDepth) && !_buildPreProcessorData.inputDataDepth)
                {
                    removeVariant = true;
                }

                //Input Data Normal
                if(keywordSet.IsEnabled(_keywordInputDataNormal) && !_buildPreProcessorData.inputDataNormal)
                {
                    removeVariant = true;
                }

                //Input Data Normal
                if(keywordSet.IsEnabled(_keywordInputDataSceneColor) && !_buildPreProcessorData.inputDataSceneColor)
                {
                    removeVariant = true;
                }

                //Fade
                if(keywordSet.IsEnabled(_keywordFade) && !_buildPreProcessorData.fade)
                {
                    removeVariant = true;
                }

                //Enhance Details
                if(keywordSet.IsEnabled(_keywordEnhanceDetails) && !_buildPreProcessorData.enhanceDetails)
                {
                    removeVariant = true;
                }

                //Sketch Lines
                if(keywordSet.IsEnabled(_keywordSketch) && !_buildPreProcessorData.sketchLines)
                {
                    removeVariant = true;
                }

                //Visualize Edges
                if(keywordSet.IsEnabled(_keywordVisualizeEdges) && !_buildPreProcessorData.visualizeEdges)
                {
                    removeVariant = true;
                }

                //Kernels High
                //Default

                //Kernels Medium
                if(keywordSet.IsEnabled(_keywordPrecisionMedium) && keywordSet.IsEnabled(_keywordUtilizeAxisOnly) && !_buildPreProcessorData.kernelRobertsCrossAxis)
                {
                    removeVariant = true;
                }
                if(keywordSet.IsEnabled(_keywordPrecisionMedium) && !keywordSet.IsEnabled(_keywordUtilizeAxisOnly) && !_buildPreProcessorData.kernelRobertsCrossDiagonal)
                {
                    removeVariant = true;
                }

                //Kernels Low
                if(keywordSet.IsEnabled(_keywordPrecisionLow) && keywordSet.IsEnabled(_keywordUtilizeAxisOnly) && !_buildPreProcessorData.kernelHalfCrossHorizontal)
                {
                    removeVariant = true;
                }
                if(keywordSet.IsEnabled(_keywordPrecisionLow) && !keywordSet.IsEnabled(_keywordUtilizeAxisOnly) && !_buildPreProcessorData.kernelHalfCrossVertical)
                {
                    removeVariant = true;
                }

                if(removeVariant)
                {
                    data.RemoveAt(i);
                    --i;
                }
            }
        }
    }
}
#endif