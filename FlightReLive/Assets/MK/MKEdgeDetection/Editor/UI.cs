/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.EdgeDetection.Editor
{
    public static class UI
    {
        private const string fadeLimit = "Ranges between 0 and 1 to either show edges on the distance or hide edges on a close lookup. A minimum limit higher than 0 will show edges far away from the camera and a maximum limit lower value than 1 will hide edges near to the camera.";
        private const string fadeLeftHandle = "Left Handle: Controls how visible lines far away are (end point for fading). A value higher than 0 will show lines at large distances.";
        private const string fadeRightHandle = "Right Handle: Controls how visible lines close to the camera are (start point for fading). A value lower than 1 will hide lines at close distances.";
        private const string threshold = "Setups threshold for high pass and low pass values based on the input data to filter out unwanted edges/lines on detailed areas. Edges/lines are generated based on the filtered value range.";
        private const string thresholdLeftHandle = "Left Handle: Controls how much rough details are filtered out. A higher value will reduce the amount of detected edges on rough details.";
        private const string thresholdRightHandle = "Right Handle: Controls how much fine details are filtered out. A higher value will reduce the amount of detected edges on noisy and small details.";
        private const string nearFade = "Controls the start point where the fading should start (world units).";
        private const string farFade = "Controls the end point where the fading should end (world units).";

        public static readonly GUIContent workflow = new GUIContent
        (
            "Workflow", 
            "Determines how the edges are detected.\n\n" +
            "Generic: Detects everything rendered on the screen. It detects in general more precise than the selective workflow.\n\n" +
            "Selective: Detects only edges on specific layers. This way you are able to control which rendered objects receive the edge detection. Always requires the depth map. Not recommend for alpha clipped geometry."
        );
        public static readonly GUIContent precision = new GUIContent
        (
            "Precision", 
            "Determines the overall precision to detect edges. A higher precision detects edges more reliable. This property has a high impact on performance.\n\n" +
            "High: Utilizes a full set of box lookups. Best quality.\n\n" +
            "Medium: Utilizes a limited set of lookups. Balanced in a good trade-off in terms of quality and performance.\n\n" +
            "Low: Utilizes a minimum amount of lookups. Best performance."
        );
        public static readonly GUIContent largeKernel = new GUIContent
        (
            "Kernel", 
            "Determines the input kernel used for the detection based on the desired precision.\n\n" +
            "Sobel: Gives smooth results and emphasizes edges of all kinds.\n\n" +
            "Scharr: Provides a good rational symmetry between the edges.\n\n" +
            "Prewitt: Emphasizes horizontal and vertical edges, but also takes diagonal edges into account."
        );
        public static readonly GUIContent mediumKernel = new GUIContent
        (
            "Kernel", 
            "Determines the input kernel used for the detection based on the desired precision.\n\n" +
            "Roberts Cross (Diagonal): Focuses on the diagonal edges.\n\n" +
            "Roberts Cross (Axis): Focuses on horizontal and vertical edges."
        );
        public static readonly GUIContent smallKernel = new GUIContent
        (
            "Kernel", 
            "Determines the input kernel used for the detection based on the desired precision.\n\n" +
            "Horizontal: Approximates horizontal edges only.\n\n" +
            "Vertical: Approximates vertical edges only."
        );
        public static readonly GUIContent inputData = new GUIContent
        (
            "Input Data", 
            "Sets up the input data for the effect and combines the results in a final composition. Its generally recommend to enable depth and normal for best results. Scene Color is in most cases optional in most cases. If no input data is provided the effect will be disabled.\n\n" +
            "Depth: Focuses on the detection of the silhouette produced by the depth buffer. This option will force the depth texture from the render pipeline.\n\n" +
            "Normal: Highlights inner edges based on the camera normals buffer, but also enhances them by the silhouette. This option will force the depth normals texture from the render pipeline.\n\n" +
            "Scene Color: Shows small details based on the rendered image of the camera."
        );
        public static readonly GUIContent lineHardness = new GUIContent
        (
            "Hardness", 
            "Outputs more softer or harder visually appearing edges and interpolates in between them.\n\n" +
            "0: Edges are more of an artistic kind and projected softly.\n\n" +
            "1: Edges are very clear and strongly visibly projected."
        );
        public static readonly string matchWidth = "Width";
        public static readonly string matchHeight = "Height";
        public static readonly GUIContent lineSizeMatchFactor = new GUIContent
        (
            "Match Factor", 
            "Determines if the line size is using the width or height as reference, or a mix in between to adopt to different screen sizes. The line size is always automatically scaled for a consistent behaviour across different resolutions."
        );
        public static readonly GUIContent fade = new GUIContent
        (
            "Fade", 
            "Enables the fading feature. Edges will fade out based on a given camera distance in world units. Each input data has its own fading setup.\n\n" +
            $"Limits: {fadeLimit}\n\n" +
            $"Begin: {nearFade}\n\n" +
            $"End: {farFade}"
        );
        public static readonly GUIContent enhanceDetails = new GUIContent
        (
            "Enhance Details", 
            "Uses a combined amount of input data to calculate and detect edges (planar projected). This can greatly increase the quality of detected edges for difficult and complex geometry (Only available if Depth and Normal input data enabled)."
        );
        public static readonly GUIContent globalLineSize = new GUIContent
        (
            "Global Size", 
            "Multiplies onto the different line sizes for separate input data to influence them globally. (line size) * (global line size) = (final Line Size). Use this to globally increase the size of all edges based on the input data."
        );
        public static readonly GUIContent depthLineSize = new GUIContent
        (
            "Size", 
            "Determines the thickness of the edges/lines created by the depth input. Use this to fine-tune the ratio between the different edge sizes based on the input data."
        );
        public static readonly GUIContent normalLineSize = new GUIContent
        (
            "Size", 
            "Determines the thickness of the edges/lines created by the normal input. Use this to fine-tune the ratio between the different edge sizes based on the input data."
        );
        public static readonly GUIContent selectiveLayer = new GUIContent
        (
            "layer", 
            "Determines the layer of objects, which receives the edge detection."
        );
        public static readonly GUIContent depthFadeLimits = new GUIContent
        (
            "Fade Limit", 
            $"{fadeLimit}\n\n" +
            $"{fadeLeftHandle}\n\n" +
            $"{fadeRightHandle}"
        );
        public static readonly GUIContent normalFadeLimits = new GUIContent
        (
            "Fade Limit", 
            $"{fadeLimit}\n\n" +
            $"{fadeLeftHandle}\n\n" +
            $"{fadeRightHandle}"
        );
        public static readonly GUIContent depthThreshold = new GUIContent
        (
            "Threshold", 
            $"{threshold}\n\n" +
            $"{thresholdLeftHandle}\n\n" +
            $"{thresholdRightHandle}"
        );
        public static readonly GUIContent combinedInputThreshold = new GUIContent
        (
            "Threshold", 
            $"{threshold}\n\n" +
            $"{thresholdLeftHandle}\n\n" +
            $"{thresholdRightHandle}"
        );
        public static readonly GUIContent lineColor = new GUIContent
        (
            "Line Color", 
            "Sets the color (RGBA) for generated lines/edges that appear on the screen. Alpha controls opacity."
        );
        public static readonly GUIContent overlayTextureScale = new GUIContent
        (
            "Scale", 
            "Controls the scale of the overlay texture."
        );
        public static readonly GUIContent overlayColor = new GUIContent
        (
            "Overlay Color", 
            "Sets an overlay color (RGBA). (A) Controls the opacity of the overlay and interpolates between the scene image and the overlay."
        );
        public static readonly GUIContent depthNearFade = new GUIContent
        (
            "Fade Start", 
            $"{nearFade}"
        );
        public static readonly GUIContent depthFarFade = new GUIContent
        (
            "Fade End", 
            $"{farFade}"
        );
        public static readonly GUIContent normalNearFade = new GUIContent
        (
            "Fade Start", 
            $"{nearFade}"
        );
        public static readonly GUIContent normalsFarFade = new GUIContent
        (
            "Fade End", 
            $"{farFade}"
        );
        public static readonly GUIContent normalThreshold = new GUIContent
        (
            "Threshold", 
            $"{threshold}\n\n" +
            $"{thresholdLeftHandle}\n\n" +
            $"{thresholdRightHandle}"
        );
        public static readonly GUIContent sceneColorThreshold = new GUIContent
        (
            "Threshold", 
            $"{threshold}\n\n" +
            $"{thresholdLeftHandle}\n\n" +
            $"{thresholdRightHandle}"
        );
        public static readonly GUIContent sceneColorLineSize = new GUIContent
        (
            "Size", 
            "Determines the thickness of the edges/lines created by the scene color input. Use this to fine-tune the ratio between the different edge sizes based on the input data."
        );
        public static readonly GUIContent sceneColorNearFade = new GUIContent
        (
           "Fade Start", 
            $"{nearFade}"
        );
        public static readonly GUIContent sceneColorFarFade = new GUIContent
        (
            "Fade End", 
            $"{farFade}"
        );
        public static readonly GUIContent sceneColorFadeLimits = new GUIContent
        (
            "Fade Limit", 
            $"{fadeLimit}\n\n" +
            $"{fadeLeftHandle}\n\n" +
            $"{fadeRightHandle}"
        );
        public static readonly GUIContent visualizeEdges = new GUIContent
        (
            "Visualize Edges",
            "Visualizes edges as colors (RGB) based on the current view. Overlapping edges from different inputs will result in mixed colors.\n\n" +
            "(R) - Depth edges\n\n" +
            "(G) - Normal edges\n\n" +
            "(B) - Scene color edges\n\n" + 
            "If the Enhanced Details feature is enabled existing data will be adjusted accordingly."
        );

        //Sketch Lines
        public static readonly GUIContent sketch = new GUIContent
        (
            "Sketch", 
            "Enables the sketch feature, giving detected edges a stylized, noisy look.\n\nThis option may significantly impact performance."
        );

        public static readonly GUIContent sketchAdditionalNoiseMap = new GUIContent
        (
            "Additional Noise", 
            "Applies an extra noise texture to further enhance the sketch effect."
        );

        public static readonly GUIContent sketchIntensity = new GUIContent
        (
            "Intensity", 
            "Adjusts the strength of the sketch effect. Higher values result in more pronounced, noisy edges."
        );

        public static readonly GUIContent sketchFrequency = new GUIContent
        (
            "Frequency", 
            "Sets the noise pattern frequency for the sketch effect. Higher values yield a more tightly tiled noise appearance."
        );

        public static readonly GUIContent sketchNearFade = new GUIContent
        (
            "Fade Start", 
            $"{nearFade}"
        );
        public static readonly GUIContent sketchFarFade = new GUIContent
        (
            "Fade End", 
            $"{farFade}"
        );
        public static readonly GUIContent sketchFadeLimits = new GUIContent
        (
            "Fade Limit", 
            $"{fadeLimit}\n\n" +
            $"{fadeLeftHandle}\n\n" +
            $"{fadeRightHandle}"
        );
    }
}
#endif