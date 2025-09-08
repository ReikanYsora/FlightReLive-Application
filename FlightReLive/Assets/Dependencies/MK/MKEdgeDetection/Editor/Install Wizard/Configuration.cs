/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace MK.EdgeDetection.Editor.InstallWizard
{
    //[CreateAssetMenu(fileName = "Configuration", menuName = "MK/Install Wizard/Create Configuration Asset")]
    public sealed class Configuration : ScriptableObject
    {
        [InitializeOnLoadMethod]
        private static void Validate()
        {
            MK.EdgeDetection.Editor.InstallWizard.Utility.ValidateDictionary(_examplesImportLookup);
        }

        #pragma warning disable CS0414

        internal static bool isReady 
        { 
            get
            { 
                if(_instance == null)
                    GetInstance();
                return _instance != null; 
            } 
        }

        [SerializeField]
        private RenderPipeline _renderPipeline = RenderPipeline.Built_in_PostProcessingStack;

        [SerializeField]
        internal bool showInstallerOnReload = true;

        [SerializeField][Space]
        private Texture2D _titleImage = null;

        [SerializeField][Space]
        private Object _readMe = null;

        [SerializeField][Space]
        private Object _examplesPackageShared = null;
        [SerializeField]
        private Object _examplesPackageBuiltin = null;
        [SerializeField]
        private Object _examplesPackagePPSv2 = null;
        [SerializeField]
        private Object _examplesPackageURP3D = null;
        [SerializeField]
        private Object _examplesPackageHDRP = null;

        [SerializeField][Space]
        private ExampleContainer[] _examples = null;

        [SerializeField][Space]
        private UnityEngine.Object _globalShaderFeatures = null;

        private static void LogAssetNotFoundError()
        {
            //Debug.LogError("Could not find Install Wizard Configuration Asset, please try to import the package again.");
        }

        private static MK.EdgeDetection.Editor.InstallWizard.Configuration _instance = null;
        
        [InitializeOnLoadMethod]
        internal static MK.EdgeDetection.Editor.InstallWizard.Configuration GetInstance()
        {
            if(_instance == null)
            {
                string[] _guids = AssetDatabase.FindAssets("t:" + typeof(MK.EdgeDetection.Editor.InstallWizard.Configuration).Namespace + ".Configuration", null);
                if(_guids.Length > 0)
                {
                    _instance = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(_guids[0]), typeof(MK.EdgeDetection.Editor.InstallWizard.Configuration)) as Configuration;
                    if(_instance != null)
                        return _instance;
                    else
                    {
                        LogAssetNotFoundError();
                        return null;
                    }
                }
                else
                {
                    LogAssetNotFoundError();
                    return null;
                }
            }
            else
                return _instance;
        }

        internal static string GetPath()
        {
            return AssetDatabase.GetAssetPath(_instance);
        }

        internal static Texture2D GetTitleImage()
        {
            return _instance._titleImage;
        }

        internal static ExampleContainer[] GetExamples()
        {
            return _instance._examples;
        }

        internal static bool GetShowInstallerOnReload()
        {
            return _instance.showInstallerOnReload;
        }
        internal static void SetShowInstallerOnReload(bool v)
        {
            if(_instance.showInstallerOnReload == v)
                return;
            _instance.showInstallerOnReload = v;
            SaveInstance();
        }
        internal static RenderPipeline GetRenderPipeline()
        {
            return _instance._renderPipeline;
        }

        internal static void SetRenderPipeline(RenderPipeline v)
        {
            if(_instance._renderPipeline == v)
                return;
            _instance._renderPipeline = v;
            SaveInstance();
        }

        internal static void SaveInstance()
        {
            EditorUtility.SetDirty(_instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static readonly Dictionary<RenderPipeline, System.Action> _examplesImportLookup = new Dictionary<RenderPipeline, System.Action>()
        {
            {
                RenderPipeline.Built_in_Legacy, () => 
                { 
                    AssetDatabase.ImportPackage(AssetDatabase.GetAssetPath(_instance._examplesPackageBuiltin), false); 
                }
            },
            {
                RenderPipeline.Built_in_PostProcessingStack, () => 
                { 
                    AssetDatabase.ImportPackage(AssetDatabase.GetAssetPath(_instance._examplesPackagePPSv2), false); 
                }
            },
            {
                RenderPipeline.Universal3D, () => 
                { 
                    AssetDatabase.ImportPackage(AssetDatabase.GetAssetPath(_instance._examplesPackageURP3D), false); 
                }
            },
            {
                RenderPipeline.Universal2D, () => 
                { 
                    //Disabled for now
                }
            },
            {
                RenderPipeline.High_Definition, () => 
                { 
                    AssetDatabase.ImportPackage(AssetDatabase.GetAssetPath(_instance._examplesPackageHDRP), false); 
                }
            }
        };

        internal static void ImportExamples(RenderPipeline renderPipeline)
        {
            AssetDatabase.ImportPackage(AssetDatabase.GetAssetPath(_instance._examplesPackageShared), false); 
            System.Action action;
            _examplesImportLookup.TryGetValue(_instance._renderPipeline, out action);
            action.Invoke();
            SetShowInstallerOnReload(false);
        }

        internal static void OpenReadMe()
        {
            AssetDatabase.OpenAsset(_instance._readMe);
        }

        public static string UnifyPath(string path)
        {
            #if UNITY_EDITOR_WIN
            return path.Replace("\\", "/");
            #else
            return path;
            #endif
        }

        private static StringBuilder _projectPath = new StringBuilder("");
        private static string projectPath
        {
            get
            {   
                if(_projectPath.ToString() == string.Empty)
                {
                    _projectPath = new StringBuilder(UnityEngine.Application.dataPath);
                    _projectPath.Remove(_projectPath.Length - 7, 7);

                    _projectPath.Append("/");
                    _projectPath = new StringBuilder(UnifyPath(_projectPath.ToString()));
                }
                return _projectPath.ToString();
            }
        }

        internal static void UpdateGlobalShaderFeatures(bool pixelPerfection)
        {
            string localAssetPath = AssetDatabase.GetAssetPath(_instance._globalShaderFeatures);
            string filePath = string.Concat(projectPath, localAssetPath);
            
            if(!System.IO.File.Exists(filePath))
                return;

            string fileContent = System.IO.File.ReadAllText(filePath);

            if(pixelPerfection)
            {
                fileContent = System.Text.RegularExpressions.Regex.Replace(fileContent, @"(?<=^|\s)\/\/#define MK_PIXEL_PERFECTION(?=\s|$)", @"#define MK_PIXEL_PERFECTION");
            }
            else
            {
                fileContent = System.Text.RegularExpressions.Regex.Replace(fileContent, @"(?<=^|\s)#define MK_PIXEL_PERFECTION(?=\s|$)", @"//#define MK_PIXEL_PERFECTION");
            }

            System.IO.File.WriteAllText(filePath, fileContent);
            AssetDatabase.ImportAsset(localAssetPath);
            AssetDatabase.Refresh();
        }

        #pragma warning restore CS0414
    }
}
#endif