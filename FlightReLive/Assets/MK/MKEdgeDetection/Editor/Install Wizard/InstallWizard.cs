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
using Configuration = MK.EdgeDetection.Editor.InstallWizard.Configuration;
namespace MK.EdgeDetection.Editor.InstallWizard
{
    public sealed class InstallWizard : EditorWindow
    {
        private static readonly GUIContent pixelPerfectionGUIContent = new GUIContent
        (
            "Pixel Perfection",
            "When enabled, rasterization is optimized for pixel precision. Edges increment exactly one pixel per size unit, achieving maximum granularity."
        );

        [InitializeOnLoadMethod]
        private static void Validate()
        {
            MK.EdgeDetection.Editor.InstallWizard.Utility.ValidateDictionary(_packageInformationLookup);
            MK.EdgeDetection.Editor.InstallWizard.Utility.ValidateDictionary(_examplesInformationLookup);
        }

        #pragma warning disable CS0414
        private const string _version = "1.3.1";
        #pragma warning restore CS0414

        private static readonly Vector2Int _referenceResolution = new Vector2Int(2560, 1440);
        private static float _sizeScale;
        private static int _scaledWidth;
        private static int _scaledHeight;
        private static Vector2 _windowScrollPos;

        private static readonly int _rawWidth = 360;
        private static readonly int _rawHeight = 640;
        private static readonly string _title = "MK Edge Detection Install Wizard";

        private static GUIStyle _flowTextStyle { get { return new GUIStyle(EditorStyles.label) { wordWrap = true }; } }
        private static readonly int _loadTimeInFrames = 72;
        private static int _waitFramesTillReload = _loadTimeInFrames;

        private static InstallWizard _window;
        private static RenderPipeline _targetRenderPipeline = RenderPipeline.Built_in_PostProcessingStack;
        private static bool _showInstallerOnReload = true;

        [MenuItem("Window/MK/Edge Detection/Install Wizard")]
        private static void ShowWindow()
        {
            if(Screen.currentResolution.height > Screen.currentResolution.width)
                _sizeScale = (float) Screen.currentResolution.width / (float)_referenceResolution.x;
            else
                _sizeScale = (float) Screen.currentResolution.height / (float)_referenceResolution.y;

            _scaledWidth = (int)((float)_rawWidth * _sizeScale);
            _scaledHeight = (int)((float)_rawHeight * _sizeScale);
            _window = (InstallWizard) EditorWindow.GetWindow<InstallWizard>(true, _title, true);

            _window.minSize = new Vector2(_scaledWidth, _scaledHeight);
            _window.maxSize = new Vector2(_scaledWidth * 2, _scaledHeight * 2);
            _window.Show();
        }

        [InitializeOnLoadMethod]
        private static void ShowInstallerOnReload()
        {
            QueryReload();
        }

        private static void QueryReload()
        {
            _waitFramesTillReload = _loadTimeInFrames;
            EditorApplication.update += Reload;
        }

        private static void Reload()
        {
            if (_waitFramesTillReload > 0)
            {
                --_waitFramesTillReload;
            }
            else
            {
                EditorApplication.update -= Reload;

                if(!Configuration.isReady)
                    return;

                if(Configuration.GetShowInstallerOnReload())
                    ShowWindow();
                
                Configuration.SetShowInstallerOnReload(false);
            }
        }

        private void OnGUI()
        {
            if(Configuration.isReady)
            {
                Texture2D titleImage = Configuration.GetTitleImage();
                if(titleImage)
                {
                    float titleScaledWidth = EditorGUIUtility.currentViewWidth - EditorGUIUtility.standardVerticalSpacing * 4;
                    float titleScaledHeight = titleScaledWidth * ((float)titleImage.height / (float)titleImage.width);
                    Rect titleRect = EditorGUILayout.GetControlRect();
                    titleRect.width = titleScaledWidth;
                    titleRect.height = titleScaledHeight;
                    GUI.DrawTexture(titleRect, titleImage, ScaleMode.ScaleToFit);
                    GUILayout.Label("", GUILayout.Height(titleScaledHeight - 20));
                    Divider();
                }

                _windowScrollPos = EditorGUILayout.BeginScrollView(_windowScrollPos);
                EditorGUILayout.LabelField("1. Select your Render Pipeline", UnityEditor.EditorStyles.boldLabel);
                _targetRenderPipeline = Configuration.GetRenderPipeline();
                EditorGUI.BeginChangeCheck();
                _targetRenderPipeline = (RenderPipeline) EditorGUILayout.EnumPopup("Render Pipeline", _targetRenderPipeline);
                if(EditorGUI.EndChangeCheck())
                    Configuration.SetRenderPipeline(_targetRenderPipeline);
                VerticalSpace();
                Divider();
                VerticalSpace();
                EditorGUILayout.LabelField("2. Setup", UnityEditor.EditorStyles.boldLabel);
                System.Action packageInformationAction;
                _packageInformationLookup.TryGetValue(Configuration.GetRenderPipeline(), out packageInformationAction);
                packageInformationAction.Invoke();
                VerticalSpace();
                Divider();
                VerticalSpace();
                EditorGUILayout.LabelField("3. Global Shader Features", UnityEditor.EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                MK.EdgeDetection.Config.pixelPerfection = EditorGUILayout.Toggle(pixelPerfectionGUIContent, MK.EdgeDetection.Config.pixelPerfection);
                if(EditorGUI.EndChangeCheck())
                {
                    Configuration.UpdateGlobalShaderFeatures(MK.EdgeDetection.Config.pixelPerfection);
                    MK.EdgeDetection.Config.SaveInstance();
                }
                VerticalSpace();
                Divider();
                VerticalSpace();
                EditorGUILayout.LabelField("4. Import Examples (optional)", UnityEditor.EditorStyles.boldLabel);
                System.Action examplesInformationAction;
                _examplesInformationLookup.TryGetValue(Configuration.GetRenderPipeline(), out examplesInformationAction);
                examplesInformationAction.Invoke();
                if(_targetRenderPipeline != RenderPipeline.Universal2D)
                {
                    if(GUILayout.Button("Import Examples"))
                    {
                        EditorUtility.DisplayProgressBar("MK Edge Detection Install Wizard", "Importing Examples", 0.5f);
                        Configuration.ImportExamples(_targetRenderPipeline);
                        EditorUtility.ClearProgressBar();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Examples are only available for 3D renderers...");
                }
                VerticalSpace();
                Divider();
                ExampleContainer[] examples = Configuration.GetExamples();
                if(examples.Length > 0 && examples[0].scene != null)
                {
                    VerticalSpace();
                    EditorGUILayout.LabelField("Example Scenes:");
                    EditorGUILayout.BeginHorizontal();
                    for(int i = 0; i < examples.Length; i++)
                    {
                        if(examples[i].scene != null)
                            examples[i].DrawEditorButton();
                    }
                    EditorGUILayout.EndHorizontal();
                    VerticalSpace();
                    Divider();
                }
                VerticalSpace();
                EditorGUILayout.LabelField("5. Read Me (Recommended)", UnityEditor.EditorStyles.boldLabel);
                if(GUILayout.Button("Open Read Me"))
                {
                    Configuration.OpenReadMe();
                }
                VerticalSpace();
                Divider();
                VerticalSpace();

                _showInstallerOnReload = Configuration.GetShowInstallerOnReload();
                EditorGUI.BeginChangeCheck();
                _showInstallerOnReload = EditorGUILayout.Toggle("Show Installer On Reload", _showInstallerOnReload);
                if(EditorGUI.EndChangeCheck())
                    Configuration.SetShowInstallerOnReload(_showInstallerOnReload);

                EditorGUILayout.EndScrollView();
                GUI.FocusControl(null);
            }
            else
            {
                Repaint();
            }
        }

        private static void VerticalSpace()
        {
            GUILayoutUtility.GetRect(1f, EditorGUIUtility.standardVerticalSpacing);
        }

        private static void Divider()
        {
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2) });
        }

        private static readonly Dictionary<RenderPipeline, System.Action> _packageInformationLookup = new Dictionary<RenderPipeline, System.Action>()
        {
            {
                RenderPipeline.Built_in_Legacy, () => 
                { 
                    EditorGUILayout.LabelField("On the game object of your rendering camera click the “Add Component” Button and apply the “MK Edge Detection” script.", _flowTextStyle);
                }
            },
            {
                RenderPipeline.Built_in_PostProcessingStack, () => 
                { 
                    EditorGUILayout.LabelField("1. Make sure the Post Processing Stack V2 is installed. It is available via the Package Manager (Window/Package Manager).", _flowTextStyle);
                    EditorGUILayout.LabelField("2. After Post Processing Stack V2 is ready you can click the “Add effect” button and add MK Edge Detection via \"MK/MK Edge Detection\".", _flowTextStyle);
                }
            },
            {
                RenderPipeline.Universal3D, () =>
                { 
                    EditorGUILayout.LabelField("1. Select your Universal Render Pipeline Renderer Asset and add the custom Renderer Feature: MK Edge Detection.", _flowTextStyle);
                    EditorGUILayout.LabelField("2. Apply MK Edge Detection on your Volume Component via \"Post-processing/MK/MK Edge Detection (URP)\" (Only required if Post-Process Volumes used).", _flowTextStyle);
                }
            },
            {
                RenderPipeline.Universal2D, () => 
                { 
                    EditorGUILayout.LabelField("1. Select your Universal Render Pipeline Renderer Asset and add the custom Renderer Feature: MK Edge Detection.", _flowTextStyle);
                    EditorGUILayout.LabelField("2. Apply MK Edge Detection on your Volume Component via \"Post-processing/MK/MK Edge Detection (URP)\" (Only required if Post-Process Volumes used).", _flowTextStyle);
                }
            },
            {
                RenderPipeline.High_Definition, () => 
                { 
                    EditorGUILayout.LabelField("1. Add MK Edge Detection to your custom post processing under HDRP Default/Global Settings on the “After Opaque And Sky” List. Navigate to it via: Edit > Project Settings > Graphics > HDRP Default/Global Settings.", _flowTextStyle);
                    EditorGUILayout.LabelField("2. Apply MK Edge Detection on your Volume Component via \"Post-processing/MK/MK Edge Detection (HDRP)\"", _flowTextStyle);
                }
            }
        };

        private static readonly Dictionary<RenderPipeline, System.Action> _examplesInformationLookup = new Dictionary<RenderPipeline, System.Action>()
        {
            {
                RenderPipeline.Built_in_Legacy, () => 
                {
                    EditorGUILayout.LabelField("The example scenes are based on Linear Color Space and HDR. Make sure to change from Gamma to Linear Color Space and enable HDR.", _flowTextStyle);
                }
            },
            {
                RenderPipeline.Built_in_PostProcessingStack, () => 
                { 
                    EditorGUILayout.LabelField("The example scenes are based on Linear Color Space and HDR. Make sure to change from Gamma to Linear Color Space and enable HDR.", _flowTextStyle);
                }
            },
            {
                RenderPipeline.Universal3D, () =>
                { 
                    EditorGUILayout.LabelField("The example scenes are based on Linear Color Space and HDR. Make sure to change from Gamma to Linear Color Space and enable HDR.", _flowTextStyle);
                }
            },
            {
                RenderPipeline.Universal2D, () => 
                { 
                    EditorGUILayout.LabelField("The example scenes are based on Linear Color Space and HDR. Make sure to change from Gamma to Linear Color Space and enable HDR.", _flowTextStyle);
                }
            },
            {
                RenderPipeline.High_Definition, () => 
                { 
                }
            }
        };
    }
}
#endif