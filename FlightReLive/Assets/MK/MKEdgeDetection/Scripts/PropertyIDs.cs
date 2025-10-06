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
    public static class PropertyIDs
    {
        public static readonly int sketchNoiseMap = UnityEngine.Shader.PropertyToID("_MKEdgeDetectionSketchNoiseMap");
        public static readonly int edgeMask = UnityEngine.Shader.PropertyToID("_MKEdgeDetectionEdgeMask");
        public static readonly int mkShaderMainTex = UnityEngine.Shader.PropertyToID("_MKShaderMainTex");
        public static readonly int mkShaderMainTexDimension = UnityEngine.Shader.PropertyToID("_MKShaderMainTexDimension");
        public static readonly int lineSizeParams = UnityEngine.Shader.PropertyToID("_LineSizeParams");
        public static readonly int depthFadeParams = UnityEngine.Shader.PropertyToID("_DepthFadeParams");
        public static readonly int normalsFadeParams = UnityEngine.Shader.PropertyToID("_NormalFadeParams");
        public static readonly int thresholdLowParams = UnityEngine.Shader.PropertyToID("_ThresholdLowParams");
        public static readonly int thresholdHighParams = UnityEngine.Shader.PropertyToID("_ThresholdHighParams");
        public static readonly int lineColor = UnityEngine.Shader.PropertyToID("_LineColor");
        public static readonly int colorsFadeParams = UnityEngine.Shader.PropertyToID("_SceneColorFadeParams");
        public static readonly int lineHardness = UnityEngine.Shader.PropertyToID("_LineHardness");
        public static readonly int kernelX = UnityEngine.Shader.PropertyToID("_KernelX");
        public static readonly int kernelY = UnityEngine.Shader.PropertyToID("_KernelY");
        public static readonly int fogParams = UnityEngine.Shader.PropertyToID("_FogParams");
        public static readonly int overlayColor = UnityEngine.Shader.PropertyToID("_OverlayColor");
        public static readonly int inverseViewProjectionMatrix = UnityEngine.Shader.PropertyToID("_MKInverseViewProjectionMatrix");
        public static readonly int sketchParams = UnityEngine.Shader.PropertyToID("_SketchParams");
        public static readonly int sketchFadeParams = UnityEngine.Shader.PropertyToID("_SketchFadeParams");
    }
}
