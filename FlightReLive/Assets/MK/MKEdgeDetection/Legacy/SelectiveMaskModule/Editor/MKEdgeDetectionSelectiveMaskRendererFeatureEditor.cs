/******************************************************************************************/
/* Universal Renderer Feature Editor */
/******************************************************************************************/

/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de

ASSET STORE TERMS OF SERVICE AND EULA:
https://unity.com/de/legal/as-terms
*****************************************************/

#if MK_URP && UNITY_EDITOR && MK_SELECTIVE_MASK_ENABLED
using UnityEngine;
using UnityEditor;
using System.Configuration;

namespace MK.EdgeDetection.UniversalRendererFeatures.Editor
{
	[CustomEditor(typeof(MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionSelectiveMaskRendererFeature))]
	public sealed class MKEdgeDetectionSelectiveMaskRendererFeatureEditor : UnityEditor.Editor
	{		
		private static readonly GUIContent layerUI = new GUIContent("Layer", "Determines the layer mask on which objects receive the selective edge detection.");
		private UnityEditor.SerializedProperty _layer;

		private void OnEnable()
		{
			_layer = serializedObject.FindProperty("layer");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			EditorGUILayout.HelpBox("This component is only required if you are using the selective workflow.", MessageType.Info);
			
			EditorGUILayout.PropertyField(_layer, layerUI);
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif