using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
[CustomEditor(typeof(VolumetricClouds))]
#else
[VolumeComponentEditor(typeof(VolumetricClouds))]
#endif
class VolumetricCloudsEditor : VolumeComponentEditor
{
    // General
    SerializedDataParameter m_Enable;
    SerializedDataParameter m_LocalClouds;

    // Shape (simple mode + curves)
    SerializedDataParameter m_CloudPreset;
    SerializedDataParameter m_DensityCurve;
    SerializedDataParameter m_ErosionCurve;
    SerializedDataParameter m_AmbientOcclusionCurve;

    SerializedDataParameter m_BottomAltitude;
    SerializedDataParameter m_AltitudeRange;
    SerializedDataParameter m_FadeInMode;
    SerializedDataParameter m_FadeInStart;
    SerializedDataParameter m_FadeInDistance;

    // Shape params
    SerializedDataParameter m_DensityMultiplier;
    SerializedDataParameter m_ShapeFactor;
    SerializedDataParameter m_ShapeScale;
    SerializedDataParameter m_ShapeOffset;
    SerializedDataParameter m_EarthCurvature;

    // Erosion
    SerializedDataParameter m_ErosionFactor;
    SerializedDataParameter m_ErosionScale;

    // Micro-erosion
    SerializedDataParameter m_MicroErosion;
    SerializedDataParameter m_MicroErosionFactor;
    SerializedDataParameter m_MicroErosionScale;

    // Lighting
    SerializedDataParameter m_ScatteringTint;
    SerializedDataParameter m_PowderEffectIntensity;
    SerializedDataParameter m_MultiScattering;
    SerializedDataParameter m_AmbientLightProbeDimmer;
    SerializedDataParameter m_SunLightDimmer;
    SerializedDataParameter m_ErosionOcclusion;

    // Wind
    SerializedDataParameter m_GlobalWindSpeed;
    SerializedDataParameter m_Orientation;
    SerializedDataParameter m_ShapeSpeedMultiplier;
    SerializedDataParameter m_ErosionSpeedMultiplier;
    SerializedDataParameter m_VerticalShapeWindSpeed;
    SerializedDataParameter m_VerticalErosionWindSpeed;
    SerializedDataParameter m_AltitudeDistortion;

    // Quality
    SerializedDataParameter m_TemporalAccumulationFactor;
    SerializedDataParameter m_PerceptualBlending;
    SerializedDataParameter m_NumPrimarySteps;
    SerializedDataParameter m_NumLightSteps;

    // Shadows
    SerializedDataParameter m_Shadows;
    SerializedDataParameter m_ShadowResolution;
    SerializedDataParameter m_ShadowDistance;
    SerializedDataParameter m_ShadowOpacity;
    SerializedDataParameter m_ShadowOpacityFallback;

    const string k_RendererDataList = "m_RendererDataList";

    const string k_VolumetricCloudsRendererFeature = "VolumetricCloudsURP";
    const string k_NoRendererFeatureMessage = "Volumetric Clouds renderer feature is disabled in the active URP renderer.";
    const string k_RendererFeatureOffMessage = "\"Volumetric Clouds\" is disabled in the active URP renderer.";
    const string k_RenderingDebuggerMessage = "\"Volumetric Clouds\" is disabled to avoid affecting rendering debugging.";

    const string k_LocalCloudsMessage = "The \"Local Clouds\" property is not available, please adjust the \"Rendering Space\" in \"Visual Environment\" override instead.";
    const string k_EarthCurvatureMessage = "The \"Earth Curvature\" property is not available, please adjust the \"Planet Radius\" in \"Visual Environment\" override instead.";

    const string k_CustomSkyShaderGraphMessage = "It looks like the \"Sky Material\" is using a shader graph. Please ensure that the \"Render Face\" setting is set to \"Both\".";

    const string k_UniversalForward = "Universal Forward";
    const string k_VISUAL_ENVIRONMENT_DYNAMIC_SKY = "VISUAL_ENVIRONMENT_DYNAMIC_SKY";

    static public readonly GUIContent k_PerceptualBlending = EditorGUIUtility.TrTextContent("Perceptual Blending", "When enabled, the clouds will blend in a perceptual way with the environment. This may cause artifacts when the sky is over-exposed.");

    const string k_FixButtonName = "Fix";
    const string k_EnableButtonName = "Enable";

    // --- Reflection cache
    private static FieldInfo RenderDataListFieldInfo;

    public override void OnEnable()
    {
        var o = new PropertyFetcher<VolumetricClouds>(serializedObject);

        RenderDataListFieldInfo = typeof(UniversalRenderPipelineAsset).GetField(k_RendererDataList, BindingFlags.Instance | BindingFlags.NonPublic);

        // General
        m_Enable = Unpack(o.Find(x => x.state));
        m_LocalClouds = Unpack(o.Find(x => x.localClouds));

        // Simple mode + curves
        m_CloudPreset = Unpack(o.Find(x => x.cloudPreset));
        m_DensityCurve = Unpack(o.Find(x => x.densityCurve));
        m_ErosionCurve = Unpack(o.Find(x => x.erosionCurve));
        m_AmbientOcclusionCurve = Unpack(o.Find(x => x.ambientOcclusionCurve));

        m_BottomAltitude = Unpack(o.Find(x => x.bottomAltitude));
        m_AltitudeRange = Unpack(o.Find(x => x.altitudeRange));

        m_FadeInMode = Unpack(o.Find(x => x.fadeInMode));
        m_FadeInStart = Unpack(o.Find(x => x.fadeInStart));
        m_FadeInDistance = Unpack(o.Find(x => x.fadeInDistance));

        // Shape
        m_DensityMultiplier = Unpack(o.Find(x => x.densityMultiplier));
        m_ShapeFactor = Unpack(o.Find(x => x.shapeFactor));
        m_ShapeScale = Unpack(o.Find(x => x.shapeScale));
        m_ShapeOffset = Unpack(o.Find(x => x.shapeOffset));
        m_EarthCurvature = Unpack(o.Find(x => x.earthCurvature));

        // Erosion
        m_ErosionFactor = Unpack(o.Find(x => x.erosionFactor));
        m_ErosionScale = Unpack(o.Find(x => x.erosionScale));

        // Micro-erosion
        m_MicroErosion = Unpack(o.Find(x => x.microErosion));
        m_MicroErosionFactor = Unpack(o.Find(x => x.microErosionFactor));
        m_MicroErosionScale = Unpack(o.Find(x => x.microErosionScale));

        // Lighting
        m_ScatteringTint = Unpack(o.Find(x => x.scatteringTint));
        m_PowderEffectIntensity = Unpack(o.Find(x => x.powderEffectIntensity));
        m_MultiScattering = Unpack(o.Find(x => x.multiScattering));
        m_AmbientLightProbeDimmer = Unpack(o.Find(x => x.ambientLightProbeDimmer));
        m_SunLightDimmer = Unpack(o.Find(x => x.sunLightDimmer));
        m_ErosionOcclusion = Unpack(o.Find(x => x.erosionOcclusion));

        // Wind
        m_Orientation = Unpack(o.Find(x => x.globalOrientation));
        m_GlobalWindSpeed = Unpack(o.Find(x => x.globalSpeed));
        m_ShapeSpeedMultiplier = Unpack(o.Find(x => x.shapeSpeedMultiplier));
        m_ErosionSpeedMultiplier = Unpack(o.Find(x => x.erosionSpeedMultiplier));
        m_AltitudeDistortion = Unpack(o.Find(x => x.altitudeDistortion));
        m_VerticalShapeWindSpeed = Unpack(o.Find(x => x.verticalShapeWindSpeed));
        m_VerticalErosionWindSpeed = Unpack(o.Find(x => x.verticalErosionWindSpeed));

        // Quality
        m_TemporalAccumulationFactor = Unpack(o.Find(x => x.temporalAccumulationFactor));
        m_PerceptualBlending = Unpack(o.Find(x => x.perceptualBlending));
        m_NumPrimarySteps = Unpack(o.Find(x => x.numPrimarySteps));
        m_NumLightSteps = Unpack(o.Find(x => x.numLightSteps));

        // Shadows
        m_Shadows = Unpack(o.Find(x => x.shadows));
        m_ShadowResolution = Unpack(o.Find(x => x.shadowResolution));
        m_ShadowDistance = Unpack(o.Find(x => x.shadowDistance));
        m_ShadowOpacity = Unpack(o.Find(x => x.shadowOpacity));
        m_ShadowOpacityFallback = Unpack(o.Find(x => x.shadowOpacityFallback));

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        // --- Secure feature lookup and cast
        var feature = GetRendererFeature(k_VolumetricCloudsRendererFeature);
        if (feature == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(k_NoRendererFeatureMessage, MessageType.Error, wide: true);
            return;
        }

        var clouds = feature as VolumetricCloudsURP;
        if (clouds == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("The VolumetricCloudsURP renderer feature exists but is not of the expected type.", MessageType.Warning, wide: true);
            return;
        }

        if (!clouds.isActive)
        {
            EditorGUILayout.Space();
            CoreEditorUtils.DrawFixMeBox(k_RendererFeatureOffMessage, MessageType.Warning, k_FixButtonName, () =>
            {
                clouds.SetActive(true);
                GUIUtility.ExitGUI();
            });
            EditorGUILayout.Space();
        }
        else
        {
            if (clouds.AmbientUpdateMode == VolumetricCloudsURP.CloudsAmbientMode.Dynamic && !Shader.IsKeywordEnabled(k_VISUAL_ENVIRONMENT_DYNAMIC_SKY))
            {
                var sb = RenderSettings.skybox;
                if (sb != null)
                {
                    // GetPassName(0) can throw on some shaders — guard it
                    string pass0 = string.Empty;
                    try { pass0 = sb.GetPassName(0); } catch { /* ignore */ }
                    if (pass0 == k_UniversalForward)
                    {
                        EditorGUILayout.HelpBox(k_CustomSkyShaderGraphMessage, MessageType.Info, wide: true);
                    }
                }
            }
        }

        bool anyDebugUI = false;
        try
        {
            anyDebugUI = DebugManager.instance != null && DebugManager.instance.isAnyDebugUIActive;
        }
        catch { /* safety */ }

        bool showDebuggerMessage = anyDebugUI && !clouds.RenderingDebugger;
        bool enableClouds = m_Enable.value.boolValue && m_Enable.overrideState.boolValue;

        if (clouds.isActive && enableClouds && showDebuggerMessage)
        {
            EditorGUILayout.Space();
            CoreEditorUtils.DrawFixMeBox(k_RenderingDebuggerMessage, MessageType.Warning, k_EnableButtonName, () =>
            {
                clouds.RenderingDebugger = true;
                GUIUtility.ExitGUI();
            });
            EditorGUILayout.Space();
        }

        PropertyField(m_Enable);

#if URP_PBSKY
        var stack = VolumeManager.instance.stack;
        VisualEnvironment visualEnvVolume = stack.GetComponent<VisualEnvironment>();
        bool hasVisualEnvVolume = visualEnvVolume != null && visualEnvVolume.IsActive() && visualEnvVolume.skyType.value != 0;

        if (hasVisualEnvVolume) { EditorGUILayout.HelpBox(k_LocalCloudsMessage, MessageType.Info, wide: true); }
        using (new EditorGUI.DisabledScope(hasVisualEnvVolume))
        {
            PropertyField(m_LocalClouds);
        }
#else
        bool hasVisualEnvVolume = false;
        PropertyField(m_LocalClouds);
#endif

        bool hasCloudMap = CloudsShapeUI(hasVisualEnvVolume);

        // --- Wind
        PropertyField(m_GlobalWindSpeed);
        if (showAdditionalProperties)
        {
            using (new IndentLevelScope())
            {
                PropertyField(m_ShapeSpeedMultiplier);
                PropertyField(m_ErosionSpeedMultiplier);
            }
        }
        PropertyField(m_Orientation);
        using (new IndentLevelScope())
        {
            PropertyField(m_AltitudeDistortion);
        }

        PropertyField(m_VerticalShapeWindSpeed);
        PropertyField(m_VerticalErosionWindSpeed);

        // --- Lighting
        PropertyField(m_AmbientLightProbeDimmer);
        PropertyField(m_SunLightDimmer);
        PropertyField(m_ErosionOcclusion);
        PropertyField(m_ScatteringTint);
        PropertyField(m_PowderEffectIntensity);
        PropertyField(m_MultiScattering);

        // --- Shadows
        PropertyField(m_Shadows);
        using (new IndentLevelScope())
        {
            PropertyField(m_ShadowResolution);
            PropertyField(m_ShadowOpacity);
            PropertyField(m_ShadowDistance);
            PropertyField(m_ShadowOpacityFallback);
        }

        // --- Quality
        PropertyField(m_TemporalAccumulationFactor);

        using (var scope = new OverridablePropertyScope(m_PerceptualBlending, k_PerceptualBlending, this))
            m_PerceptualBlending.value.floatValue = EditorGUILayout.Toggle(k_PerceptualBlending, m_PerceptualBlending.value.floatValue == 1.0f) ? 1.0f : 0.0f;

        PropertyField(m_NumPrimarySteps);
        PropertyField(m_NumLightSteps);
        PropertyField(m_FadeInMode);
        using (new IndentLevelScope())
        {
            if ((VolumetricClouds.CloudFadeInMode)m_FadeInMode.value.enumValueIndex == VolumetricClouds.CloudFadeInMode.Manual)
            {
                PropertyField(m_FadeInStart);
                PropertyField(m_FadeInDistance);
            }
        }

        // --- Défense anti-crash AnimationCurve
        EnsureCurvesAreDistinctInstances();
    }

    // -----------------------------
    // Curves helpers & simple mode
    // -----------------------------

    static AnimationCurve CloneCurve(AnimationCurve src)
    {
        if (src == null) return new AnimationCurve();
        // deep copy keys – guarantees a distinct instance
        return new AnimationCurve(src.keys);
    }

    void EnsureCurvesAreDistinctInstances()
    {
        // Sur certains blends de volumes, Unity essaie de copier une courbe "dans elle-même"
        // (dest et src référencent la même instance) => crash. On force ici une instance unique.
        TryCloneCurve(m_DensityCurve);
        TryCloneCurve(m_ErosionCurve);
        TryCloneCurve(m_AmbientOcclusionCurve);
    }

    void TryCloneCurve(SerializedDataParameter curveParam)
    {
        try
        {
            var c = curveParam?.value?.animationCurveValue;
            if (c == null) return;

            // Crée systématiquement une nouvelle instance — évite tout aliasing de référence
            var cloned = CloneCurve(c);
            if (!ReferenceEquals(c, cloned))
                curveParam.value.animationCurveValue = cloned;
        }
        catch { /* safe guard */ }
    }

    void LoadPresetValues(VolumetricClouds.CloudPresets preset, bool microDetails)
    {
        switch (preset)
        {
            case VolumetricClouds.CloudPresets.Sparse:
                {
                    m_DensityMultiplier.value.floatValue = 0.4f;
                    if (microDetails)
                    {
                        m_ShapeFactor.value.floatValue = 0.925f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.85f;
                        m_ErosionScale.value.floatValue = 75.0f;
                        m_MicroErosionFactor.value.floatValue = 0.65f;
                        m_MicroErosionScale.value.floatValue = 300.0f;
                    }
                    else
                    {
                        m_ShapeFactor.value.floatValue = 0.95f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.8f;
                        m_ErosionScale.value.floatValue = 107.0f;
                    }

                    m_DensityCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.05f, 1.0f), new Keyframe(0.75f, 1.0f), new Keyframe(1.0f, 0.0f)));
                    m_ErosionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f)));
                    m_AmbientOcclusionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.5f), new Keyframe(1.0f, 0.0f)));

                    m_BottomAltitude.value.floatValue = 3000.0f;
                    m_AltitudeRange.value.floatValue = 1000.0f;
                }
                break;

            case VolumetricClouds.CloudPresets.Cloudy:
                {
                    m_DensityMultiplier.value.floatValue = 0.4f;

                    if (microDetails)
                    {
                        m_ShapeFactor.value.floatValue = 0.875f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.9f;
                        m_ErosionScale.value.floatValue = 75.0f;
                        m_MicroErosionFactor.value.floatValue = 0.65f;
                        m_MicroErosionScale.value.floatValue = 300.0f;
                    }
                    else
                    {
                        m_ShapeFactor.value.floatValue = 0.9f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.8f;
                        m_ErosionScale.value.floatValue = 107.0f;
                    }

                    m_DensityCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f)));
                    m_ErosionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f)));
                    m_AmbientOcclusionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f)));

                    m_BottomAltitude.value.floatValue = 1200.0f;
                    m_AltitudeRange.value.floatValue = 2000.0f;
                }
                break;

            case VolumetricClouds.CloudPresets.Overcast:
                {
                    m_DensityMultiplier.value.floatValue = 0.3f;

                    if (microDetails)
                    {
                        m_ShapeFactor.value.floatValue = 0.45f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.7f;
                        m_ErosionScale.value.floatValue = 75.0f;
                        m_MicroErosionFactor.value.floatValue = 0.5f;
                        m_MicroErosionScale.value.floatValue = 300.0f;
                    }
                    else
                    {
                        m_ShapeFactor.value.floatValue = 0.5f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.5f;
                        m_ErosionScale.value.floatValue = 107.0f;
                    }

                    m_DensityCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.05f, 1.0f), new Keyframe(0.9f, 0.0f), new Keyframe(1.0f, 0.0f)));
                    m_ErosionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f)));
                    m_AmbientOcclusionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1.0f, 0.0f)));

                    m_BottomAltitude.value.floatValue = 1500.0f;
                    m_AltitudeRange.value.floatValue = 2500.0f;
                }
                break;

            case VolumetricClouds.CloudPresets.Stormy:
                {
                    m_DensityMultiplier.value.floatValue = 0.35f;

                    if (microDetails)
                    {
                        m_ShapeFactor.value.floatValue = 0.825f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.9f;
                        m_ErosionScale.value.floatValue = 75.0f;
                        m_MicroErosionFactor.value.floatValue = 0.6f;
                        m_MicroErosionScale.value.floatValue = 300.0f;
                    }
                    else
                    {
                        m_ShapeFactor.value.floatValue = 0.85f;
                        m_ShapeScale.value.floatValue = 5.0f;
                        m_ErosionFactor.value.floatValue = 0.75f;
                        m_ErosionScale.value.floatValue = 107.0f;
                    }

                    m_DensityCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.037f, 1.0f), new Keyframe(0.6f, 1.0f), new Keyframe(1.0f, 0.0f)));
                    m_ErosionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(
                                                                                    new Keyframe(0f, 1f),
                                                                                    new Keyframe(0.05f, 0.8f),
                                                                                    new Keyframe(0.2438f, 0.9498f),
                                                                                    new Keyframe(0.5f, 1.0f),
                                                                                    new Keyframe(0.93f, 0.9268f),
                                                                                    new Keyframe(1.0f, 1.0f)));
                    m_AmbientOcclusionCurve.value.animationCurveValue = CloneCurve(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 0.4f), new Keyframe(1.0f, 0.0f)));

                    m_BottomAltitude.value.floatValue = 1000.0f;
                    m_AltitudeRange.value.floatValue = 5000.0f;
                }
                break;

            default:
                break;
        }

        // Par sécurité, on s’assure que chaque curve est une instance unique
        EnsureCurvesAreDistinctInstances();
    }

    void SimpleControlMode(bool controlChanged)
    {
        VolumetricClouds.CloudPresets previousControlPreset = (VolumetricClouds.CloudPresets)m_CloudPreset.value.enumValueIndex;

        EditorGUI.BeginChangeCheck();
        PropertyField(m_CloudPreset);
        VolumetricClouds.CloudPresets controlPreset = (VolumetricClouds.CloudPresets)m_CloudPreset.value.enumValueIndex;

        if (EditorGUI.EndChangeCheck() || previousControlPreset != controlPreset)
        {
            if (controlPreset != VolumetricClouds.CloudPresets.Custom)
            {
                LoadPresetValues(controlPreset, m_MicroErosion.value.boolValue);
            }
        }

        if (controlPreset != VolumetricClouds.CloudPresets.Custom)
        {
            bool ovr = m_CloudPreset.overrideState.boolValue;
            m_DensityMultiplier.overrideState.boolValue = ovr;
            m_DensityCurve.overrideState.boolValue = ovr;
            m_ShapeFactor.overrideState.boolValue = ovr;
            m_ShapeScale.overrideState.boolValue = ovr;
            m_ErosionFactor.overrideState.boolValue = ovr;
            m_ErosionScale.overrideState.boolValue = ovr;
            m_ErosionCurve.overrideState.boolValue = ovr;
            m_MicroErosionFactor.overrideState.boolValue = ovr;
            m_MicroErosionScale.overrideState.boolValue = ovr;
            m_AmbientOcclusionCurve.overrideState.boolValue = ovr;
            m_BottomAltitude.overrideState.boolValue = ovr;
            m_AltitudeRange.overrideState.boolValue = ovr;
        }

        // Tweaks group 1
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(!(m_CloudPreset.overrideState.boolValue)))
        {
            using (new IndentLevelScope())
            {
                PropertyField(m_DensityMultiplier);
                PropertyField(m_DensityCurve);
                PropertyField(m_ShapeFactor);
                PropertyField(m_ShapeScale);
                PropertyField(m_ErosionFactor);
                PropertyField(m_ErosionScale);
                PropertyField(m_ErosionCurve);
            }
        }
        if (EditorGUI.EndChangeCheck() && controlPreset != VolumetricClouds.CloudPresets.Custom)
        {
            m_CloudPreset.value.enumValueIndex = (int)VolumetricClouds.CloudPresets.Custom;
        }

        using (new IndentLevelScope())
        {
            PropertyField(m_MicroErosion);
            if (m_MicroErosion.value.boolValue)
            {
                PropertyField(m_MicroErosionFactor);
                PropertyField(m_MicroErosionScale);
            }
        }

        // Tweaks group 2
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(!(m_CloudPreset.overrideState.boolValue)))
        {
            using (new IndentLevelScope())
            {
                PropertyField(m_AmbientOcclusionCurve);
                PropertyField(m_BottomAltitude);
                PropertyField(m_AltitudeRange);
            }
        }
        if (EditorGUI.EndChangeCheck() && controlPreset != VolumetricClouds.CloudPresets.Custom)
        {
            m_CloudPreset.value.enumValueIndex = (int)VolumetricClouds.CloudPresets.Custom;
        }

        // Clone defensif
        EnsureCurvesAreDistinctInstances();
    }

    bool CloudsShapeUI(bool hasVisualEnvVolume)
    {
        SimpleControlMode(controlChanged: false);

        // Additional properties
        PropertyField(m_ShapeOffset);

#if URP_PBSKY
        if (hasVisualEnvVolume) { EditorGUILayout.HelpBox(k_EarthCurvatureMessage, MessageType.Info, wide: true); }
        using (new EditorGUI.DisabledScope(hasVisualEnvVolume))
        {
            PropertyField(m_EarthCurvature);
        }
#else
        PropertyField(m_EarthCurvature);
#endif
        return false;
    }

    // -----------------------------
    // RendererFeature helpers
    // -----------------------------

    private static ScriptableRendererData[] GetRendererDataList(UniversalRenderPipelineAsset asset = null)
    {
        try
        {
            if (asset == null)
                asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

            if (asset == null || RenderDataListFieldInfo == null)
                return null;

            var renderDataList = (ScriptableRendererData[])RenderDataListFieldInfo.GetValue(asset);
            return renderDataList;
        }
        catch
        {
            return null;
        }
    }

    private static ScriptableRendererFeature GetRendererFeature(string typeName)
    {
        var renderDataList = GetRendererDataList();
        if (renderDataList == null || renderDataList.Length == 0)
            return null;

        foreach (var renderData in renderDataList)
        {
            if (renderData == null || renderData.rendererFeatures == null) continue;

            foreach (var rendererFeature in renderData.rendererFeatures)
            {
                if (rendererFeature == null) continue;

                var t = rendererFeature.GetType();
                if (t != null && t.Name != null && t.Name.Contains(typeName))
                    return rendererFeature;
            }
        }
        return null;
    }
}
