/******************************************************************************************/
/* Post Processing Volume Component Editor */
/******************************************************************************************/

/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de

ASSET STORE TERMS OF SERVICE AND EULA:
https://unity.com/de/legal/as-terms
*****************************************************/

/* File created using: */
/* MK Shader - Cross Compiling Shaders */
/* Version: 1.1.63  */
/* Exported on: 28.05.2025 23:06:01 */

#if MK_POST_PROCESSING_STACK_V2 && UNITY_EDITOR
using UnityEngine;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine.Rendering.PostProcessing;
/****************************************************/
/* Editor Namespace Injection */
/****************************************************/
using MKEdgeDetectionUI = MK.EdgeDetection.Editor.UI;
using MKEdgeDetectionUIDrawers = MK.EdgeDetection.Editor.UILayoutDrawers;

namespace MK.EdgeDetection.PostProcessingStackV2VolumeComponents.Editor
{
	/******************************************************************************************/
	/* Post Processing Stack Volume Component Editor */
	/******************************************************************************************/
	[UnityEditor.Rendering.PostProcessing.PostProcessEditorAttribute(typeof(MK.EdgeDetection.PostProcessingStackV2VolumeComponents.MKEdgeDetection))]
	public sealed class MKEdgeDetectionEditor : UnityEditor.Rendering.PostProcessing.PostProcessEffectEditor<MK.EdgeDetection.PostProcessingStackV2VolumeComponents.MKEdgeDetection>
	{
		/****************************************************/
		/* Parameter Drawer Definitions */
		/****************************************************/
		#if UNITY_EDITOR
		[UnityEditor.CustomPropertyDrawer(typeof(MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision>))]
		public sealed class PrecisionEnumPropertyDrawer : MK.EdgeDetection.PostProcessing.Generic.Editor.EnumPropertyDrawer<Precision> {}
		#endif
		
		#if UNITY_EDITOR
		[UnityEditor.CustomPropertyDrawer(typeof(MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel>))]
		public sealed class LargeKernelEnumPropertyDrawer : MK.EdgeDetection.PostProcessing.Generic.Editor.EnumPropertyDrawer<LargeKernel> {}
		#endif
		
		#if UNITY_EDITOR
		[UnityEditor.CustomPropertyDrawer(typeof(MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel>))]
		public sealed class MediumKernelEnumPropertyDrawer : MK.EdgeDetection.PostProcessing.Generic.Editor.EnumPropertyDrawer<MediumKernel> {}
		#endif
		
		#if UNITY_EDITOR
		[UnityEditor.CustomPropertyDrawer(typeof(MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel>))]
		public sealed class SmallKernelEnumPropertyDrawer : MK.EdgeDetection.PostProcessing.Generic.Editor.EnumPropertyDrawer<SmallKernel> {}
		#endif
		
		#if UNITY_EDITOR
		[UnityEditor.CustomPropertyDrawer(typeof(MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow>))]
		public sealed class WorkflowEnumPropertyDrawer : MK.EdgeDetection.PostProcessing.Generic.Editor.EnumPropertyDrawer<Workflow> {}
		#endif
		
		#if UNITY_EDITOR
		[UnityEditor.CustomPropertyDrawer(typeof(MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData>))]
		public sealed class InputDataBitmaskPropertyDrawer : MK.EdgeDetection.PostProcessing.Generic.Editor.BitmaskPropertyDrawer<InputData> {}
		#endif
		
		/****************************************************/
		/* API Editor Injection */
		/****************************************************/
		
		/****************************************************/
		/* Parameter Editor Declarations */
		/****************************************************/
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  precision;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  largeKernel;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  mediumKernel;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  smallKernel;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  inputData;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  lineHardness;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  fade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  lineSizeMatchFactor;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  globalLineSize;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  depthLineSize;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  normalLineSize;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  depthFadeLimits;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  normalFadeLimits;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  depthThreshold;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  lineColor;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  overlayColor;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  depthNearFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  depthFarFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  normalNearFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  normalsFarFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  normalThreshold;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sceneColorThreshold;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sceneColorLineSize;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sceneColorNearFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sceneColorFarFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sceneColorFadeLimits;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  visualizeEdges;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  enhanceDetails;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketch;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketchIntensity;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketchFrequency;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketchAdditionalNoiseMap;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketchFadeLimits;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketchNearFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  sketchFarFade;
		private UnityEditor.Rendering.PostProcessing.SerializedParameterOverride  workflow;
		private UnityEditor.SerializedProperty _propertiesInitialized;
		
		/****************************************************/
		/* On Enable */
		/****************************************************/
		public override void OnEnable()
		{
			/* <-----| Find Editor Parameters |-----> */
			precision = FindParameterOverride(x => x.precision);
			largeKernel = FindParameterOverride(x => x.largeKernel);
			mediumKernel = FindParameterOverride(x => x.mediumKernel);
			smallKernel = FindParameterOverride(x => x.smallKernel);
			inputData = FindParameterOverride(x => x.inputData);
			lineHardness = FindParameterOverride(x => x.lineHardness);
			fade = FindParameterOverride(x => x.fade);
			lineSizeMatchFactor = FindParameterOverride(x => x.lineSizeMatchFactor);
			globalLineSize = FindParameterOverride(x => x.globalLineSize);
			depthLineSize = FindParameterOverride(x => x.depthLineSize);
			normalLineSize = FindParameterOverride(x => x.normalLineSize);
			depthFadeLimits = FindParameterOverride(x => x.depthFadeLimits);
			normalFadeLimits = FindParameterOverride(x => x.normalFadeLimits);
			depthThreshold = FindParameterOverride(x => x.depthThreshold);
			lineColor = FindParameterOverride(x => x.lineColor);
			overlayColor = FindParameterOverride(x => x.overlayColor);
			depthNearFade = FindParameterOverride(x => x.depthNearFade);
			depthFarFade = FindParameterOverride(x => x.depthFarFade);
			normalNearFade = FindParameterOverride(x => x.normalNearFade);
			normalsFarFade = FindParameterOverride(x => x.normalsFarFade);
			normalThreshold = FindParameterOverride(x => x.normalThreshold);
			sceneColorThreshold = FindParameterOverride(x => x.sceneColorThreshold);
			sceneColorLineSize = FindParameterOverride(x => x.sceneColorLineSize);
			sceneColorNearFade = FindParameterOverride(x => x.sceneColorNearFade);
			sceneColorFarFade = FindParameterOverride(x => x.sceneColorFarFade);
			sceneColorFadeLimits = FindParameterOverride(x => x.sceneColorFadeLimits);
			visualizeEdges = FindParameterOverride(x => x.visualizeEdges);
			enhanceDetails = FindParameterOverride(x => x.enhanceDetails);
			sketch = FindParameterOverride(x => x.sketch);
			sketchIntensity = FindParameterOverride(x => x.sketchIntensity);
			sketchFrequency = FindParameterOverride(x => x.sketchFrequency);
			sketchAdditionalNoiseMap = FindParameterOverride(x => x.sketchAdditionalNoiseMap);
			sketchFadeLimits = FindParameterOverride(x => x.sketchFadeLimits);
			sketchNearFade = FindParameterOverride(x => x.sketchNearFade);
			sketchFarFade = FindParameterOverride(x => x.sketchFarFade);
			workflow = FindParameterOverride(x => x.workflow);
			_propertiesInitialized = FindProperty(x => x._propertiesInitialized);
		}
		
		/****************************************************/
		/* On Inspector GUI */
		/****************************************************/
		public override void OnInspectorGUI()
		{
			if(_propertiesInitialized.boolValue == false)
			{
				/* <-----| Initial Value Editor Parameters |-----> */
				globalLineSize.overrideState.boolValue = true;
				globalLineSize.value.FindPropertyRelative("_value").floatValue = 1.0f;
				_propertiesInitialized.boolValue = true;
			}
			
			UnityEditor.EditorGUILayout.LabelField("Input", UnityEditor.EditorStyles.boldLabel);
			#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
			PropertyField(workflow, MKEdgeDetectionUI.workflow);
			if((Workflow) (int) workflow.value.FindPropertyRelative("_value").intValue == Workflow.Selective)
			{
				UnityEditor.EditorGUILayout.HelpBox("Reminder: Don't forget to add the Selective Mask Renderer Feature to your universal renderer asset and specify your selective mask layer...", UnityEditor.MessageType.Info);
			}
			#endif
			PropertyField(inputData, MKEdgeDetectionUI.inputData);
			if(inputData.value.FindPropertyRelative("_value").enumValueFlag == 0 && !visualizeEdges.value.FindPropertyRelative("_value").boolValue)
			{
				UnityEditor.EditorGUILayout.HelpBox("No input data available... Effect will be disabled...", UnityEditor.MessageType.Info);
				return;
			}
			#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
			if((Workflow) (int) workflow.value.FindPropertyRelative("_value").intValue == Workflow.Selective)
			{
				UnityEditor.EditorGUILayout.LabelField("Using selective workflow only medium precision is available.");
				PropertyField(mediumKernel, MKEdgeDetectionUI.mediumKernel);
			}
			else
			{
			#endif
				PropertyField(precision, MKEdgeDetectionUI.precision);
				switch((int) precision.value.FindPropertyRelative("_value").intValue)
				{
					case (int) Precision.High:
						PropertyField(largeKernel, MKEdgeDetectionUI.largeKernel);
					break;
					case (int) Precision.Medium:
						PropertyField(mediumKernel, MKEdgeDetectionUI.mediumKernel);
					break;
					case (int) Precision.Low:
						PropertyField(smallKernel, MKEdgeDetectionUI.smallKernel);
					break;
				}
			#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
			}
			#endif
			PropertyField(fade, MKEdgeDetectionUI.fade);
			PropertyField(enhanceDetails, MKEdgeDetectionUI.enhanceDetails);
			//bool enhanceDetailsEnabled = enhanceDetails.value.FindPropertyRelative("_value").boolValue && ((MK.EdgeDetection.InputData) inputData.value.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Normal) && ((MK.EdgeDetection.InputData) inputData.value.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Depth);
			
			MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
			UnityEditor.EditorGUILayout.LabelField("Appearance", UnityEditor.EditorStyles.boldLabel);
			PropertyField(globalLineSize, MKEdgeDetectionUI.globalLineSize);
			PropertyField(lineSizeMatchFactor, MKEdgeDetectionUI.lineSizeMatchFactor);
			MKEdgeDetectionUIDrawers.DrawLabelBelowSlider(MKEdgeDetectionUI.matchWidth, MKEdgeDetectionUI.matchHeight);
			PropertyField(lineHardness, MKEdgeDetectionUI.lineHardness);
			PropertyField(lineColor, MKEdgeDetectionUI.lineColor);
			PropertyField(overlayColor, MKEdgeDetectionUI.overlayColor);
			UnityEditor.EditorGUILayout.Space();
			PropertyField(sketch, MKEdgeDetectionUI.sketch);
			
			if(sketch.value.FindPropertyRelative("_value").boolValue == true)
			{
				 MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Sketch", UnityEditor.EditorStyles.boldLabel);
				PropertyField(sketchIntensity, MKEdgeDetectionUI.sketchIntensity);
				PropertyField(sketchFrequency, MKEdgeDetectionUI.sketchFrequency);
				PropertyField(sketchAdditionalNoiseMap, MKEdgeDetectionUI.sketchAdditionalNoiseMap);
				if(((MK.EdgeDetection.InputData) inputData.value.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Depth))
				{
					PropertyField(sketchFadeLimits, MKEdgeDetectionUI.sketchFadeLimits);
					PropertyField(sketchNearFade, MKEdgeDetectionUI.sketchNearFade);
					PropertyField(sketchFarFade, MKEdgeDetectionUI.sketchFarFade);
				}
			}
			
			if(((MK.EdgeDetection.InputData) inputData.value.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Depth))
			{
				MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Depth", UnityEditor.EditorStyles.boldLabel);
				PropertyField(depthLineSize, MKEdgeDetectionUI.depthLineSize);
				PropertyField(depthThreshold, MKEdgeDetectionUI.depthThreshold);
				if(fade.value.FindPropertyRelative("_value").boolValue)
				{
					PropertyField(depthFadeLimits, MKEdgeDetectionUI.depthFadeLimits);
					PropertyField(depthNearFade, MKEdgeDetectionUI.depthNearFade);
					PropertyField(depthFarFade, MKEdgeDetectionUI.depthFarFade);
				}
			}
			
			if(((MK.EdgeDetection.InputData) inputData.value.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Normal))
			{
				MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Normals", UnityEditor.EditorStyles.boldLabel);
				PropertyField(normalLineSize, MKEdgeDetectionUI.normalLineSize);
				PropertyField(normalThreshold, MKEdgeDetectionUI.normalThreshold);
				if(fade.value.FindPropertyRelative("_value").boolValue)
				{
					PropertyField(normalFadeLimits, MKEdgeDetectionUI.normalFadeLimits);
					PropertyField(normalNearFade, MKEdgeDetectionUI.normalNearFade);
					PropertyField(normalsFarFade, MKEdgeDetectionUI.normalsFarFade);
				}
			}
			
			if(((MK.EdgeDetection.InputData) inputData.value.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.SceneColor))
			{
				MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Scene Color", UnityEditor.EditorStyles.boldLabel);
				PropertyField(sceneColorLineSize, MKEdgeDetectionUI.sceneColorLineSize);
				PropertyField(sceneColorThreshold, MKEdgeDetectionUI.sceneColorThreshold);
				if(fade.value.FindPropertyRelative("_value").boolValue)
				{
					PropertyField(sceneColorFadeLimits, MKEdgeDetectionUI.sceneColorFadeLimits);
					PropertyField(sceneColorNearFade, MKEdgeDetectionUI.sceneColorNearFade);
					PropertyField(sceneColorFarFade, MKEdgeDetectionUI.sceneColorFarFade);
				}
			}
			
			MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
			UnityEditor.EditorGUILayout.LabelField("Debug", UnityEditor.EditorStyles.boldLabel);
			PropertyField(visualizeEdges, MKEdgeDetectionUI.visualizeEdges);
		}
	}
}
#endif