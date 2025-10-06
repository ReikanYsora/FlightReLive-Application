/******************************************************************************************/
/* Image Effects Component Editor */
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
/* Exported on: 28.05.2025 23:06:02 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
/****************************************************/
/* Editor Namespace Injection */
/****************************************************/
using MKEdgeDetectionUI = MK.EdgeDetection.Editor.UI;
using MKEdgeDetectionUIDrawers = MK.EdgeDetection.Editor.UILayoutDrawers;

namespace MK.EdgeDetection.ImageEffectsComponents.Editor
{
	/******************************************************************************************/
	/* Image Effects Component Editor */
	/******************************************************************************************/
	[System.Serializable] [UnityEditor.CustomEditor(typeof(MK.EdgeDetection.ImageEffectsComponents.MKEdgeDetection))]
	public sealed class MKEdgeDetectionEditor : UnityEditor.Editor
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
		private UnityEditor.SerializedProperty precision;
		private UnityEditor.SerializedProperty largeKernel;
		private UnityEditor.SerializedProperty mediumKernel;
		private UnityEditor.SerializedProperty smallKernel;
		private UnityEditor.SerializedProperty inputData;
		private UnityEditor.SerializedProperty lineHardness;
		private UnityEditor.SerializedProperty fade;
		private UnityEditor.SerializedProperty lineSizeMatchFactor;
		private UnityEditor.SerializedProperty globalLineSize;
		private UnityEditor.SerializedProperty depthLineSize;
		private UnityEditor.SerializedProperty normalLineSize;
		private UnityEditor.SerializedProperty depthFadeLimits;
		private UnityEditor.SerializedProperty normalFadeLimits;
		private UnityEditor.SerializedProperty depthThreshold;
		private UnityEditor.SerializedProperty lineColor;
		private UnityEditor.SerializedProperty overlayColor;
		private UnityEditor.SerializedProperty depthNearFade;
		private UnityEditor.SerializedProperty depthFarFade;
		private UnityEditor.SerializedProperty normalNearFade;
		private UnityEditor.SerializedProperty normalsFarFade;
		private UnityEditor.SerializedProperty normalThreshold;
		private UnityEditor.SerializedProperty sceneColorThreshold;
		private UnityEditor.SerializedProperty sceneColorLineSize;
		private UnityEditor.SerializedProperty sceneColorNearFade;
		private UnityEditor.SerializedProperty sceneColorFarFade;
		private UnityEditor.SerializedProperty sceneColorFadeLimits;
		private UnityEditor.SerializedProperty visualizeEdges;
		private UnityEditor.SerializedProperty enhanceDetails;
		private UnityEditor.SerializedProperty sketch;
		private UnityEditor.SerializedProperty sketchIntensity;
		private UnityEditor.SerializedProperty sketchFrequency;
		private UnityEditor.SerializedProperty sketchAdditionalNoiseMap;
		private UnityEditor.SerializedProperty sketchFadeLimits;
		private UnityEditor.SerializedProperty sketchNearFade;
		private UnityEditor.SerializedProperty sketchFarFade;
		private UnityEditor.SerializedProperty workflow;
		private UnityEditor.SerializedProperty _propertiesInitialized;
		
		/****************************************************/
		/* On Enable */
		/****************************************************/
		public void OnEnable()
		{
			/* <-----| Find Editor Parameters |-----> */
			precision = serializedObject.FindProperty("precision");
			largeKernel = serializedObject.FindProperty("largeKernel");
			mediumKernel = serializedObject.FindProperty("mediumKernel");
			smallKernel = serializedObject.FindProperty("smallKernel");
			inputData = serializedObject.FindProperty("inputData");
			lineHardness = serializedObject.FindProperty("lineHardness");
			fade = serializedObject.FindProperty("fade");
			lineSizeMatchFactor = serializedObject.FindProperty("lineSizeMatchFactor");
			globalLineSize = serializedObject.FindProperty("globalLineSize");
			depthLineSize = serializedObject.FindProperty("depthLineSize");
			normalLineSize = serializedObject.FindProperty("normalLineSize");
			depthFadeLimits = serializedObject.FindProperty("depthFadeLimits");
			normalFadeLimits = serializedObject.FindProperty("normalFadeLimits");
			depthThreshold = serializedObject.FindProperty("depthThreshold");
			lineColor = serializedObject.FindProperty("lineColor");
			overlayColor = serializedObject.FindProperty("overlayColor");
			depthNearFade = serializedObject.FindProperty("depthNearFade");
			depthFarFade = serializedObject.FindProperty("depthFarFade");
			normalNearFade = serializedObject.FindProperty("normalNearFade");
			normalsFarFade = serializedObject.FindProperty("normalsFarFade");
			normalThreshold = serializedObject.FindProperty("normalThreshold");
			sceneColorThreshold = serializedObject.FindProperty("sceneColorThreshold");
			sceneColorLineSize = serializedObject.FindProperty("sceneColorLineSize");
			sceneColorNearFade = serializedObject.FindProperty("sceneColorNearFade");
			sceneColorFarFade = serializedObject.FindProperty("sceneColorFarFade");
			sceneColorFadeLimits = serializedObject.FindProperty("sceneColorFadeLimits");
			visualizeEdges = serializedObject.FindProperty("visualizeEdges");
			enhanceDetails = serializedObject.FindProperty("enhanceDetails");
			sketch = serializedObject.FindProperty("sketch");
			sketchIntensity = serializedObject.FindProperty("sketchIntensity");
			sketchFrequency = serializedObject.FindProperty("sketchFrequency");
			sketchAdditionalNoiseMap = serializedObject.FindProperty("sketchAdditionalNoiseMap");
			sketchFadeLimits = serializedObject.FindProperty("sketchFadeLimits");
			sketchNearFade = serializedObject.FindProperty("sketchNearFade");
			sketchFarFade = serializedObject.FindProperty("sketchFarFade");
			workflow = serializedObject.FindProperty("workflow");
			_propertiesInitialized = serializedObject.FindProperty("_propertiesInitialized");
		}
		
		/****************************************************/
		/* On Inspector GUI */
		/****************************************************/
		public override void OnInspectorGUI()
		{
			
			serializedObject.Update();
			if(_propertiesInitialized.boolValue == false)
			{
				/* <-----| Initial Value Editor Parameters |-----> */
				globalLineSize.FindPropertyRelative("_value").floatValue = 1.0f;
				_propertiesInitialized.boolValue = true;
			}
			
			UnityEditor.EditorGUILayout.LabelField("Input", UnityEditor.EditorStyles.boldLabel);
			#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
			EditorGUILayout.PropertyField(workflow, MKEdgeDetectionUI.workflow);
			if((Workflow) (int) workflow.FindPropertyRelative("_value").intValue == Workflow.Selective)
			{
				UnityEditor.EditorGUILayout.HelpBox("Reminder: Don't forget to add the Selective Mask Renderer Feature to your universal renderer asset and specify your selective mask layer...", UnityEditor.MessageType.Info);
			}
			#endif
			EditorGUILayout.PropertyField(inputData, MKEdgeDetectionUI.inputData);
			if(inputData.FindPropertyRelative("_value").enumValueFlag == 0 && !visualizeEdges.FindPropertyRelative("_value").boolValue)
			{
				UnityEditor.EditorGUILayout.HelpBox("No input data available... Effect will be disabled...", UnityEditor.MessageType.Info);
				return;
			}
			#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
			if((Workflow) (int) workflow.FindPropertyRelative("_value").intValue == Workflow.Selective)
			{
				UnityEditor.EditorGUILayout.LabelField("Using selective workflow only medium precision is available.");
				EditorGUILayout.PropertyField(mediumKernel, MKEdgeDetectionUI.mediumKernel);
			}
			else
			{
			#endif
				EditorGUILayout.PropertyField(precision, MKEdgeDetectionUI.precision);
				switch((int) precision.FindPropertyRelative("_value").intValue)
				{
					case (int) Precision.High:
						EditorGUILayout.PropertyField(largeKernel, MKEdgeDetectionUI.largeKernel);
					break;
					case (int) Precision.Medium:
						EditorGUILayout.PropertyField(mediumKernel, MKEdgeDetectionUI.mediumKernel);
					break;
					case (int) Precision.Low:
						EditorGUILayout.PropertyField(smallKernel, MKEdgeDetectionUI.smallKernel);
					break;
				}
			#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
			}
			#endif
			EditorGUILayout.PropertyField(fade, MKEdgeDetectionUI.fade);
			EditorGUILayout.PropertyField(enhanceDetails, MKEdgeDetectionUI.enhanceDetails);
			//bool enhanceDetailsEnabled = enhanceDetails.FindPropertyRelative("_value").boolValue && ((MK.EdgeDetection.InputData) inputData.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Normal) && ((MK.EdgeDetection.InputData) inputData.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Depth);
			
			MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
			UnityEditor.EditorGUILayout.LabelField("Appearance", UnityEditor.EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(globalLineSize, MKEdgeDetectionUI.globalLineSize);
			EditorGUILayout.PropertyField(lineSizeMatchFactor, MKEdgeDetectionUI.lineSizeMatchFactor);
			MKEdgeDetectionUIDrawers.DrawLabelBelowSlider(MKEdgeDetectionUI.matchWidth, MKEdgeDetectionUI.matchHeight);
			EditorGUILayout.PropertyField(lineHardness, MKEdgeDetectionUI.lineHardness);
			EditorGUILayout.PropertyField(lineColor, MKEdgeDetectionUI.lineColor);
			EditorGUILayout.PropertyField(overlayColor, MKEdgeDetectionUI.overlayColor);
			UnityEditor.EditorGUILayout.Space();
			EditorGUILayout.PropertyField(sketch, MKEdgeDetectionUI.sketch);
			
			if(sketch.FindPropertyRelative("_value").boolValue == true)
			{
				 MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Sketch", UnityEditor.EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(sketchIntensity, MKEdgeDetectionUI.sketchIntensity);
				EditorGUILayout.PropertyField(sketchFrequency, MKEdgeDetectionUI.sketchFrequency);
				EditorGUILayout.PropertyField(sketchAdditionalNoiseMap, MKEdgeDetectionUI.sketchAdditionalNoiseMap);
				if(((MK.EdgeDetection.InputData) inputData.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Depth))
				{
					EditorGUILayout.PropertyField(sketchFadeLimits, MKEdgeDetectionUI.sketchFadeLimits);
					EditorGUILayout.PropertyField(sketchNearFade, MKEdgeDetectionUI.sketchNearFade);
					EditorGUILayout.PropertyField(sketchFarFade, MKEdgeDetectionUI.sketchFarFade);
				}
			}
			
			if(((MK.EdgeDetection.InputData) inputData.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Depth))
			{
				MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Depth", UnityEditor.EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(depthLineSize, MKEdgeDetectionUI.depthLineSize);
				EditorGUILayout.PropertyField(depthThreshold, MKEdgeDetectionUI.depthThreshold);
				if(fade.FindPropertyRelative("_value").boolValue)
				{
					EditorGUILayout.PropertyField(depthFadeLimits, MKEdgeDetectionUI.depthFadeLimits);
					EditorGUILayout.PropertyField(depthNearFade, MKEdgeDetectionUI.depthNearFade);
					EditorGUILayout.PropertyField(depthFarFade, MKEdgeDetectionUI.depthFarFade);
				}
			}
			
			if(((MK.EdgeDetection.InputData) inputData.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.Normal))
			{
				MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Normals", UnityEditor.EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(normalLineSize, MKEdgeDetectionUI.normalLineSize);
				EditorGUILayout.PropertyField(normalThreshold, MKEdgeDetectionUI.normalThreshold);
				if(fade.FindPropertyRelative("_value").boolValue)
				{
					EditorGUILayout.PropertyField(normalFadeLimits, MKEdgeDetectionUI.normalFadeLimits);
					EditorGUILayout.PropertyField(normalNearFade, MKEdgeDetectionUI.normalNearFade);
					EditorGUILayout.PropertyField(normalsFarFade, MKEdgeDetectionUI.normalsFarFade);
				}
			}
			
			if(((MK.EdgeDetection.InputData) inputData.FindPropertyRelative("_value").enumValueFlag).HasFlag(InputData.SceneColor))
			{
				MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
				UnityEditor.EditorGUILayout.LabelField("Scene Color", UnityEditor.EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(sceneColorLineSize, MKEdgeDetectionUI.sceneColorLineSize);
				EditorGUILayout.PropertyField(sceneColorThreshold, MKEdgeDetectionUI.sceneColorThreshold);
				if(fade.FindPropertyRelative("_value").boolValue)
				{
					EditorGUILayout.PropertyField(sceneColorFadeLimits, MKEdgeDetectionUI.sceneColorFadeLimits);
					EditorGUILayout.PropertyField(sceneColorNearFade, MKEdgeDetectionUI.sceneColorNearFade);
					EditorGUILayout.PropertyField(sceneColorFarFade, MKEdgeDetectionUI.sceneColorFarFade);
				}
			}
			
			MK.EdgeDetection.Generic.Editor.UIDrawers.Divider();
			UnityEditor.EditorGUILayout.LabelField("Debug", UnityEditor.EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(visualizeEdges, MKEdgeDetectionUI.visualizeEdges);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif