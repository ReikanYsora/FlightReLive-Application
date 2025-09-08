/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

using UnityEngine;

namespace MK.EdgeDetection
{
    [System.Serializable]
    public class Resources : ScriptableObject
    {
        [SerializeField]
        private UnityEngine.Shader _shaderMain;
        internal UnityEngine.Shader shaderMain { get { return _shaderMain; } }
        [SerializeField]
        private UnityEngine.Shader _shaderURPCopy;
        internal UnityEngine.Shader shaderURPCopy { get { return _shaderURPCopy; } }
        //[SerializeField]
        //private UnityEngine.Shader _selectiveMaskShader;
        //internal UnityEngine.Shader selectiveMaskShader { get { return _selectiveMaskShader; } }
        [SerializeField]
        private UnityEngine.Shader _shaderSketch;
        internal UnityEngine.Shader shaderSketch { get { return _shaderSketch; } }
        [SerializeField]
        private bool _pixelPerfection;
        internal bool pixelPerfection { get { return _pixelPerfection; } set { _pixelPerfection = value; } }

        /*
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Window/MK/Edge Detection/Create Resources Asset")]
        static void CreateAsset()
        {
            Resources asset = ScriptableObject.CreateInstance<Resources>();

            UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/_MK/MKEdgeDetection/Resources.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.EditorUtility.FocusProjectWindow();

            UnityEditor.Selection.activeObject = asset;
        }
        #endif
        */
    }
}
