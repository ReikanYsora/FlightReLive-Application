/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
namespace MK.EdgeDetection.Editor
{
    [CustomPropertyDrawer(typeof(MK.EdgeDetection.Editor.DisplayNameAttribute), true)]
    public class DisplayNameAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MK.EdgeDetection.Editor.DisplayNameAttribute displayNameAttribute = attribute as MK.EdgeDetection.Editor.DisplayNameAttribute;
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, new GUIContent(displayNameAttribute.displayName));
            EditorGUI.EndProperty();
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif