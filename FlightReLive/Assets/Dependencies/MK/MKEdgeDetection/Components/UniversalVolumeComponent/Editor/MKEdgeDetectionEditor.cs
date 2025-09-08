/******************************************************************************************/
/* Universal Volume Component Editor */
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
/* Exported on: 28.05.2025 23:05:58 */

#if MK_URP && UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEditor;
/****************************************************/
/* Editor Namespace Injection */
/****************************************************/
using MKEdgeDetectionUI = MK.EdgeDetection.Editor.UI;
using MKEdgeDetectionUIDrawers = MK.EdgeDetection.Editor.UILayoutDrawers;

namespace MK.EdgeDetection.UniversalVolumeComponents.Editor
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
	
	#if UNITY_2022_2_OR_NEWER
	[CustomEditor(typeof(MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection))]
	#else
	[UnityEditor.Rendering.VolumeComponentEditor(typeof(MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection))]
	#endif
	public sealed class MKEdgeDetectionEditor : UnityEditor.Rendering.VolumeComponentEditor
	{
		/****************************************************/
		/* API Editor Injection */
		/****************************************************/
		
		/****************************************************/
		/* Parameter Editor Declarations */
		/****************************************************/
		private UnityEditor.Rendering.SerializedDataParameter precision;
		private UnityEditor.Rendering.SerializedDataParameter largeKernel;
		private UnityEditor.Rendering.SerializedDataParameter mediumKernel;
		private UnityEditor.Rendering.SerializedDataParameter smallKernel;
		private UnityEditor.Rendering.SerializedDataParameter inputData;
		private UnityEditor.Rendering.SerializedDataParameter lineHardness;
		private UnityEditor.Rendering.SerializedDataParameter fade;
		private UnityEditor.Rendering.SerializedDataParameter lineSizeMatchFactor;
		private UnityEditor.Rendering.SerializedDataParameter globalLineSize;
		private UnityEditor.Rendering.SerializedDataParameter depthLineSize;
		private UnityEditor.Rendering.SerializedDataParameter normalLineSize;
		private UnityEditor.Rendering.SerializedDataParameter depthFadeLimits;
		private UnityEditor.Rendering.SerializedDataParameter normalFadeLimits;
		private UnityEditor.Rendering.SerializedDataParameter depthThreshold;
		private UnityEditor.Rendering.SerializedDataParameter lineColor;
		private UnityEditor.Rendering.SerializedDataParameter overlayColor;
		private UnityEditor.Rendering.SerializedDataParameter depthNearFade;
		private UnityEditor.Rendering.SerializedDataParameter depthFarFade;
		private UnityEditor.Rendering.SerializedDataParameter normalNearFade;
		private UnityEditor.Rendering.SerializedDataParameter normalsFarFade;
		private UnityEditor.Rendering.SerializedDataParameter normalThreshold;
		private UnityEditor.Rendering.SerializedDataParameter sceneColorThreshold;
		private UnityEditor.Rendering.SerializedDataParameter sceneColorLineSize;
		private UnityEditor.Rendering.SerializedDataParameter sceneColorNearFade;
		private UnityEditor.Rendering.SerializedDataParameter sceneColorFarFade;
		private UnityEditor.Rendering.SerializedDataParameter sceneColorFadeLimits;
		private UnityEditor.Rendering.SerializedDataParameter visualizeEdges;
		private UnityEditor.Rendering.SerializedDataParameter enhanceDetails;
		private UnityEditor.Rendering.SerializedDataParameter sketch;
		private UnityEditor.Rendering.SerializedDataParameter sketchIntensity;
		private UnityEditor.Rendering.SerializedDataParameter sketchFrequency;
		private UnityEditor.Rendering.SerializedDataParameter sketchAdditionalNoiseMap;
		private UnityEditor.Rendering.SerializedDataParameter sketchFadeLimits;
		private UnityEditor.Rendering.SerializedDataParameter sketchNearFade;
		private UnityEditor.Rendering.SerializedDataParameter sketchFarFade;
		private UnityEditor.Rendering.SerializedDataParameter workflow;
		private UnityEditor.SerializedProperty _propertiesInitialized;
		
		/****************************************************/
		/* On Enable */
		/****************************************************/
		public override void OnEnable()
		{
			UnityEditor.Rendering.PropertyFetcher<MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection> propertyFetcher = new UnityEditor.Rendering.PropertyFetcher<MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection>(serializedObject);
			
			/* <-----| Find Editor Parameters |-----> */
			precision = Unpack(propertyFetcher.Find(x => x.precision));
			largeKernel = Unpack(propertyFetcher.Find(x => x.largeKernel));
			mediumKernel = Unpack(propertyFetcher.Find(x => x.mediumKernel));
			smallKernel = Unpack(propertyFetcher.Find(x => x.smallKernel));
			inputData = Unpack(propertyFetcher.Find(x => x.inputData));
			lineHardness = Unpack(propertyFetcher.Find(x => x.lineHardness));
			fade = Unpack(propertyFetcher.Find(x => x.fade));
			lineSizeMatchFactor = Unpack(propertyFetcher.Find(x => x.lineSizeMatchFactor));
			globalLineSize = Unpack(propertyFetcher.Find(x => x.globalLineSize));
			depthLineSize = Unpack(propertyFetcher.Find(x => x.depthLineSize));
			normalLineSize = Unpack(propertyFetcher.Find(x => x.normalLineSize));
			depthFadeLimits = Unpack(propertyFetcher.Find(x => x.depthFadeLimits));
			normalFadeLimits = Unpack(propertyFetcher.Find(x => x.normalFadeLimits));
			depthThreshold = Unpack(propertyFetcher.Find(x => x.depthThreshold));
			lineColor = Unpack(propertyFetcher.Find(x => x.lineColor));
			overlayColor = Unpack(propertyFetcher.Find(x => x.overlayColor));
			depthNearFade = Unpack(propertyFetcher.Find(x => x.depthNearFade));
			depthFarFade = Unpack(propertyFetcher.Find(x => x.depthFarFade));
			normalNearFade = Unpack(propertyFetcher.Find(x => x.normalNearFade));
			normalsFarFade = Unpack(propertyFetcher.Find(x => x.normalsFarFade));
			normalThreshold = Unpack(propertyFetcher.Find(x => x.normalThreshold));
			sceneColorThreshold = Unpack(propertyFetcher.Find(x => x.sceneColorThreshold));
			sceneColorLineSize = Unpack(propertyFetcher.Find(x => x.sceneColorLineSize));
			sceneColorNearFade = Unpack(propertyFetcher.Find(x => x.sceneColorNearFade));
			sceneColorFarFade = Unpack(propertyFetcher.Find(x => x.sceneColorFarFade));
			sceneColorFadeLimits = Unpack(propertyFetcher.Find(x => x.sceneColorFadeLimits));
			visualizeEdges = Unpack(propertyFetcher.Find(x => x.visualizeEdges));
			enhanceDetails = Unpack(propertyFetcher.Find(x => x.enhanceDetails));
			sketch = Unpack(propertyFetcher.Find(x => x.sketch));
			sketchIntensity = Unpack(propertyFetcher.Find(x => x.sketchIntensity));
			sketchFrequency = Unpack(propertyFetcher.Find(x => x.sketchFrequency));
			sketchAdditionalNoiseMap = Unpack(propertyFetcher.Find(x => x.sketchAdditionalNoiseMap));
			sketchFadeLimits = Unpack(propertyFetcher.Find(x => x.sketchFadeLimits));
			sketchNearFade = Unpack(propertyFetcher.Find(x => x.sketchNearFade));
			sketchFarFade = Unpack(propertyFetcher.Find(x => x.sketchFarFade));
			workflow = Unpack(propertyFetcher.Find(x => x.workflow));
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
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif