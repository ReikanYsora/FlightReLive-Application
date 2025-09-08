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
    public class UILayoutDrawers : PropertyDrawer
    {
        private const int klabelHeightOffset = 12;
        private static readonly GUIStyle _leftAlignedLabel = new GUIStyle(EditorStyles.label);
        private static readonly GUIStyle _rightAlignedLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight};

        public static void DrawLabelBelowSlider(string leftLabel, string rightLabel)
        {
            Rect position = UnityEditor.EditorGUILayout.GetControlRect(true, GUILayout.Height(EditorGUIUtility.singleLineHeight * 0.5f));
            position.height = EditorGUIUtility.singleLineHeight;
            position.y -= klabelHeightOffset;
            position.xMin += EditorGUIUtility.labelWidth;
            position.xMax -= EditorGUIUtility.fieldWidth;

            GUI.Label(position, leftLabel, _leftAlignedLabel);
            GUI.Label(position, rightLabel, _rightAlignedLabel);
        }
    }
}
#endif