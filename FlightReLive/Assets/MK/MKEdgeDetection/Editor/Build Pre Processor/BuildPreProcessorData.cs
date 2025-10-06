/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MK.EdgeDetection.Editor.BuildPreProcessor
{
    //[CreateAssetMenu(fileName = "BuildPreProcessorData", menuName = "MK/Edge Detection/Build Pre Processor Data")]
    public sealed class BuildPreProcessorData : ScriptableObject
    {
        [Header("Toggle variants on/off that you want to include when building.\nDisabled variants will be stripped away.")]
        
        #if MK_URP && MK_SELECTIVE_MASK_ENABLED
        [Space]
        [Header("Workflow")]
        [DisplayNameAttribute("Generic Workflow")]
        public bool workflowGeneric = true;
        [DisplayNameAttribute("Selective Workflow (Only available on URP)")]
        public bool workflowSelective = true;
        #endif

        [Space]
        [Header("Input Data")]
        [DisplayNameAttribute("Depth")]
        public bool inputDataDepth = true;
        [DisplayNameAttribute("Normal")]
        public bool inputDataNormal = true;
        [DisplayNameAttribute("Scene Color")]
        public bool inputDataSceneColor = true;

        [Space]
        [Header("Generic")]
        [DisplayNameAttribute("Fade")]
        public bool fade = true;
        [DisplayNameAttribute("Enhance Details")]
        public bool enhanceDetails = true;
        [DisplayNameAttribute("Sketch Lines")]
        public bool sketchLines = true;

        [Space]
        [Header("Precision")]
        [DisplayNameAttribute("High")]
        public bool precisionHigh = true;
        [DisplayNameAttribute("Medium")]
        public bool precisionMedium = true;
        [DisplayNameAttribute("Low")]
        public bool precisionLow = true;

        [Space]
        [Header("Kernels")]
        [DisplayNameAttribute("Sobel / Scharr / Prewitt")]
        public bool kernelSobel_Scharr_Prewitt = true;
        [DisplayNameAttribute("Roberts Cross (Diagonal)")]
        public bool kernelRobertsCrossDiagonal = true;
        [DisplayNameAttribute("Roberts Cross (Axis)")]
        public bool kernelRobertsCrossAxis = true;
        [DisplayNameAttribute("Half Cross (Horizontal)")]
        public bool kernelHalfCrossHorizontal = true;
        [DisplayNameAttribute("Half Cross (Vertical)")]
        public bool kernelHalfCrossVertical = true;
        
        [Space]
        [Header("Debug")]
        [DisplayNameAttribute("Visualize Edges")]
        public bool visualizeEdges = true;
    }
}
#endif