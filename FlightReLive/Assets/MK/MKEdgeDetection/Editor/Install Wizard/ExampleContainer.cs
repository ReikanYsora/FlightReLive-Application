/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

#if UNITY_EDITOR
using UnityEngine;

namespace MK.EdgeDetection.Editor.InstallWizard
{
    [System.Serializable]
    public class ExampleContainer
    {
        public string name = "";
        public UnityEngine.Object scene = null;
        public UnityEngine.Texture2D icon = null;

        public void DrawEditorButton()
        {
            if(UnityEngine.GUILayout.Button(icon, UnityEngine.GUILayout.Width(64), UnityEngine.GUILayout.Height(64)))
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UnityEditor.AssetDatabase.GetAssetOrScenePath(scene));
        }
    }
}
#endif