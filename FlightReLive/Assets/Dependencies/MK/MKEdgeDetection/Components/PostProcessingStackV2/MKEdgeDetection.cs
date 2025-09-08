/******************************************************************************************/
/* Post Processing Volume Component */
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
/* Exported on: 28.05.2025 23:06:00 */

#if MK_POST_PROCESSING_STACK_V2
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
/****************************************************/
/* Namespace Injection */
/****************************************************/
using MK.EdgeDetection.PostProcessing.Generic;

namespace MK.EdgeDetection.PostProcessingStackV2VolumeComponents
{
	/******************************************************************************************/
	/* Volume Component Interface */
	/******************************************************************************************/
	public interface IParameters
	{
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision> getPrecision { get; }
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel> getLargeKernel { get; }
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel> getMediumKernel { get; }
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel> getSmallKernel { get; }
		public MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData> getInputData { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getLineHardness { get; }
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getLineSizeMatchFactor { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getGlobalLineSize { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getDepthLineSize { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getNormalLineSize { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getDepthFadeLimits { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getNormalFadeLimits { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getDepthThreshold { get; }
		public MK.EdgeDetection.PostProcessing.Generic.ColorProperty getLineColor { get; }
		public MK.EdgeDetection.PostProcessing.Generic.ColorProperty getOverlayColor { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getDepthNearFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getDepthFarFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getNormalNearFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getNormalsFarFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getNormalThreshold { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSceneColorThreshold { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getSceneColorLineSize { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSceneColorNearFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSceneColorFarFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSceneColorFadeLimits { get; }
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getVisualizeEdges { get; }
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getEnhanceDetails { get; }
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getSketch { get; }
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getSketchIntensity { get; }
		public MK.EdgeDetection.PostProcessing.Generic.FloatProperty getSketchFrequency { get; }
		public MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty getSketchAdditionalNoiseMap { get; }
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSketchFadeLimits { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSketchNearFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSketchFarFade { get; }
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow> getWorkflow { get; }
	}
	
	/******************************************************************************************/
	/* Post Processing Stack Volume Component */
	/******************************************************************************************/
	[System.Serializable]
	[UnityEngine.Rendering.PostProcessing.PostProcessAttribute(typeof(MK.EdgeDetection.PostProcessingStackV2VolumeComponents.MKEdgeDetection.MKEdgeDetectionRenderer), UnityEngine.Rendering.PostProcessing.PostProcessEvent.BeforeTransparent, "MK/MK Edge Detection")]
	public sealed class MKEdgeDetection : UnityEngine.Rendering.PostProcessing.PostProcessEffectSettings, MK.EdgeDetection.PostProcessingStackV2VolumeComponents.IParameters
	{
		/****************************************************/
		/* Parameter Definitions */
		/****************************************************/
		[System.Serializable]
		public sealed class EnumParameter<T> : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.EnumProperty<T>>where T : System.Enum
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.EnumProperty<T> from, MK.EdgeDetection.PostProcessing.Generic.EnumProperty<T> to, float t)
			{
				value = t > 0.5f ? to : from;
			}
		}
		[System.Serializable]
		public sealed class BitmaskParameter<T> : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<T>>where T : System.Enum
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<T> from, MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<T> to, float t)
			{
				value = t > 0.5f ? to : from;
			}
		}
		[System.Serializable]
		public sealed class RangeParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.RangeProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.RangeProperty from, MK.EdgeDetection.PostProcessing.Generic.RangeProperty to, float t)
			{
				value = RangeProperty.Lerp(from, to, t);
			}
		}
		[System.Serializable]
		public sealed class BoolParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.BoolProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.BoolProperty from, MK.EdgeDetection.PostProcessing.Generic.BoolProperty to, float t)
			{
				value = t > 0.5f ? to : from;
			}
		}
		[System.Serializable]
		public sealed class MinMaxRangeParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty from, MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty to, float t)
			{
				value = MinMaxRangeProperty.Lerp(from, to, t);
			}
		}
		[System.Serializable]
		public sealed class ColorParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.ColorProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.ColorProperty from, MK.EdgeDetection.PostProcessing.Generic.ColorProperty to, float t)
			{
				value = ColorProperty.Lerp(from, to, t);
			}
		}
		[System.Serializable]
		public sealed class AbsFloatParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty from, MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty to, float t)
			{
				value = Mathf.Lerp(from, to, t);
			}
		}
		[System.Serializable]
		public sealed class FloatParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.FloatProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.FloatProperty from, MK.EdgeDetection.PostProcessing.Generic.FloatProperty to, float t)
			{
				value = Mathf.Lerp(from, to, t);
			}
		}
		[System.Serializable]
		public sealed class Texture2DParameter : UnityEngine.Rendering.PostProcessing.ParameterOverride<MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty>
		{
			public override void Interp(MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty from, MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty to, float t)
			{
				value = t > 0.5f ? to : from;
			}
		}
		#if UNITY_EDITOR
		#pragma warning disable CS0414
		[UnityEngine.SerializeField] [UnityEngine.HideInInspector] public bool _propertiesInitialized = false;
		#pragma warning restore CS0414
		#endif
		private const string _profilerName = "MKEdgeDetection";
		
		/****************************************************/
		/* Parameter Declarations */
		/****************************************************/
		 [field: SerializeField] public EnumParameter<Precision> precision = new EnumParameter<Precision>() { value = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision>(Precision.Medium) };
		 [field: SerializeField] public EnumParameter<LargeKernel> largeKernel = new EnumParameter<LargeKernel>() { value = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel>(LargeKernel.Sobel) };
		 [field: SerializeField] public EnumParameter<MediumKernel> mediumKernel = new EnumParameter<MediumKernel>() { value = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel>(MediumKernel.RobertsCrossDiagonal) };
		 [field: SerializeField] public EnumParameter<SmallKernel> smallKernel = new EnumParameter<SmallKernel>() { value = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel>(SmallKernel.HalfCrossHorizontal) };
		 [field: SerializeField] public BitmaskParameter<InputData> inputData = new BitmaskParameter<InputData>() { value = new MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData>(InputData.Depth|InputData.Normal) };
		 [field: SerializeField] public RangeParameter lineHardness = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0.5f, 0, 1) };
		 [field: SerializeField] public BoolParameter fade = new BoolParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false) };
		 [field: SerializeField] public RangeParameter lineSizeMatchFactor = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0.5f, 0, 1) };
		 [field: SerializeField] public RangeParameter globalLineSize = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0f, 0, 3) };
		 [field: SerializeField] public RangeParameter depthLineSize = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(1f, 0, 2) };
		 [field: SerializeField] public RangeParameter normalLineSize = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(1f, 0, 2) };
		 [field: SerializeField] public MinMaxRangeParameter depthFadeLimits = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f) };
		 [field: SerializeField] public MinMaxRangeParameter normalFadeLimits = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f) };
		 [field: SerializeField] public MinMaxRangeParameter depthThreshold = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.1f, 1f, 0, 1.0f) };
		 [field: SerializeField] public ColorParameter lineColor = new ColorParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.ColorProperty(0, 0, 0, 1, true, false) };
		 [field: SerializeField] public ColorParameter overlayColor = new ColorParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.ColorProperty(1, 1, 1, 0, true, false) };
		 [field: SerializeField] public AbsFloatParameter depthNearFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(10) };
		 [field: SerializeField] public AbsFloatParameter depthFarFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(30) };
		 [field: SerializeField] public AbsFloatParameter normalNearFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(5) };
		 [field: SerializeField] public AbsFloatParameter normalsFarFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(20) };
		 [field: SerializeField] public MinMaxRangeParameter normalThreshold = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.1f, 1f, 0, 1.0f) };
		 [field: SerializeField] public MinMaxRangeParameter sceneColorThreshold = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.1f, 1f, 0, 1.0f) };
		 [field: SerializeField] public RangeParameter sceneColorLineSize = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(1f, 0, 2) };
		 [field: SerializeField] public AbsFloatParameter sceneColorNearFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(0) };
		 [field: SerializeField] public AbsFloatParameter sceneColorFarFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(10) };
		 [field: SerializeField] public MinMaxRangeParameter sceneColorFadeLimits = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f) };
		 [field: SerializeField] public BoolParameter visualizeEdges = new BoolParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false) };
		 [field: SerializeField] public BoolParameter enhanceDetails = new BoolParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false) };
		 [field: SerializeField] public BoolParameter sketch = new BoolParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false) };
		 [field: SerializeField] public RangeParameter sketchIntensity = new RangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0.333f, 0, 1) };
		 [field: SerializeField] public FloatParameter sketchFrequency = new FloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.FloatProperty(4) };
		 [field: SerializeField] public Texture2DParameter sketchAdditionalNoiseMap = new Texture2DParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty(null) };
		 [field: SerializeField] public MinMaxRangeParameter sketchFadeLimits = new MinMaxRangeParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f) };
		 [field: SerializeField] public AbsFloatParameter sketchNearFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(10) };
		 [field: SerializeField] public AbsFloatParameter sketchFarFade = new AbsFloatParameter() { value = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(30) };
		 [field: SerializeField] public EnumParameter<Workflow> workflow = new EnumParameter<Workflow>() { value = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow>(Workflow.Generic) };
		
		/****************************************************/
		/* Volume Component Interface Implementation */
		/****************************************************/
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision> getPrecision => precision.value;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel> getLargeKernel => largeKernel.value;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel> getMediumKernel => mediumKernel.value;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel> getSmallKernel => smallKernel.value;
		public MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData> getInputData => inputData.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getLineHardness => lineHardness.value;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getFade => fade.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getLineSizeMatchFactor => lineSizeMatchFactor.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getGlobalLineSize => globalLineSize.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getDepthLineSize => depthLineSize.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getNormalLineSize => normalLineSize.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getDepthFadeLimits => depthFadeLimits.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getNormalFadeLimits => normalFadeLimits.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getDepthThreshold => depthThreshold.value;
		public MK.EdgeDetection.PostProcessing.Generic.ColorProperty getLineColor => lineColor.value;
		public MK.EdgeDetection.PostProcessing.Generic.ColorProperty getOverlayColor => overlayColor.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getDepthNearFade => depthNearFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getDepthFarFade => depthFarFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getNormalNearFade => normalNearFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getNormalsFarFade => normalsFarFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getNormalThreshold => normalThreshold.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSceneColorThreshold => sceneColorThreshold.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getSceneColorLineSize => sceneColorLineSize.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSceneColorNearFade => sceneColorNearFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSceneColorFarFade => sceneColorFarFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSceneColorFadeLimits => sceneColorFadeLimits.value;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getVisualizeEdges => visualizeEdges.value;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getEnhanceDetails => enhanceDetails.value;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getSketch => sketch.value;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getSketchIntensity => sketchIntensity.value;
		public MK.EdgeDetection.PostProcessing.Generic.FloatProperty getSketchFrequency => sketchFrequency.value;
		public MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty getSketchAdditionalNoiseMap => sketchAdditionalNoiseMap.value;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSketchFadeLimits => sketchFadeLimits.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSketchNearFade => sketchNearFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSketchFarFade => sketchFarFade.value;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow> getWorkflow => workflow.value;
		
		/****************************************************/
		/* MKIsActiveLocal */
		/****************************************************/
		private bool MKIsActiveLocal(MK.EdgeDetection.PostProcessingStackV2VolumeComponents.IParameters properties)
		{
			return properties.getInputData != InputData.None && properties.getGlobalLineSize > 0 || properties.getVisualizeEdges;
		}
		
		/****************************************************/
		/* MKIsTileCompatible */
		/****************************************************/
		private bool MKIsTileCompatible()
		{
			return true;
		}
		
		/****************************************************/
		/* Is Enabled And Supported */
		/****************************************************/
		public override bool IsEnabledAndSupported(UnityEngine.Rendering.PostProcessing.PostProcessRenderContext context)
		{
			return enabled.value && MKIsActiveLocal(this);
		}
		
		/****************************************************/
		/* MKOnInitialize */
		/****************************************************/
		public void MKOnInitialize()
		{
			
		}
		
		/****************************************************/
		/* MKOnTerminate */
		/****************************************************/
		public void MKOnTerminate()
		{
			
		}
		
		/****************************************************/
		/* Renderer */
		/****************************************************/
		public sealed class MKEdgeDetectionRenderer : UnityEngine.Rendering.PostProcessing.PostProcessEffectRenderer<MKEdgeDetection>, MK.EdgeDetection.PostProcessing.Generic.ICameraData
		{
			private const string _ppsv2Keyword = "_MK_SHADER_PPSV2";
			private UnityEngine.Rendering.PostProcessing.PostProcessRenderContext _renderingContext = null;
			/****************************************************/
			/* API Injection */
			/****************************************************/
			#if UNITY_EDITOR
			private bool _runsInEditor => true;
			#else
			private bool _runsInEditor => false;
			#endif
			private MaterialPropertyBlock _materialPropertyBlock;
			private static float AdjustSize(float rawSize, float width, float height, float scaleFactor, bool allowDynamicResolution)
			{
				float logScaledWidth = Mathf.Log(width / (allowDynamicResolution ? Config.referenceResolution.x * UnityEngine.ScalableBufferManager.widthScaleFactor : Config.referenceResolution.x), 2);
				float logScaledHeight = Mathf.Log(height / (allowDynamicResolution ? Config.referenceResolution.y * UnityEngine.ScalableBufferManager.heightScaleFactor : Config.referenceResolution.y), 2);
				return rawSize * Mathf.Pow(2, Mathf.Lerp(logScaledWidth, logScaledHeight, scaleFactor));
			}
			
			/****************************************************/
			/* Camera Data */
			/****************************************************/
			#if ENABLE_VR
			public bool xrEnvironment => true;
			#else
			public bool xrEnvironment => false;
			#endif
			public int width => _renderingContext.camera.allowDynamicResolution ? Mathf.RoundToInt(_renderingContext.camera.pixelWidth * ScalableBufferManager.widthScaleFactor) : _renderingContext.camera.pixelWidth;
			public int height => _renderingContext.camera.allowDynamicResolution ? Mathf.RoundToInt(_renderingContext.camera.pixelHeight * ScalableBufferManager.heightScaleFactor) : _renderingContext.camera.pixelHeight;
			public bool stereoEnabled => _renderingContext.camera.stereoEnabled;
			public bool isSceneView => _renderingContext.isSceneView;
			public float aspect => _renderingContext.camera.aspect;
			public UnityEngine.Matrix4x4 viewMatrix => _renderingContext.camera.worldToCameraMatrix;
			public UnityEngine.Matrix4x4 projectionMatrix => _renderingContext.camera.projectionMatrix;
			public bool hasTargetTexture => _renderingContext.camera.targetTexture != null ? true : false;
			public bool useCustomRenderTargetSetup => true;
			public UnityEngine.Rendering.TextureDimension customRenderTargetDimension => UnityEngine.Rendering.TextureDimension.Tex2D;
			public int customRenderTargetVolumeDepth => 1;
			public bool allowDynamicResolution => _renderingContext.camera.allowDynamicResolution;
			
			/****************************************************/
			/* OnEnable */
			/****************************************************/
			public void OnEnable()
			{
				_materialPropertyBlock = new MaterialPropertyBlock();
			}
			
			/****************************************************/
			/* OnDisable */
			/****************************************************/
			public void OnDisable()
			{
				_materialPropertyBlock = null;
			}
			
			/****************************************************/
			/* Init */
			/****************************************************/
			public override void Init()
			{
				OnEnable();
			}
			
			/****************************************************/
			/* Release */
			/****************************************************/
			public override void Release()
			{
				OnDisable();
				settings.MKOnTerminate();
			}
			
			/****************************************************/
			/* DrawEffect */
			/****************************************************/
			private void DrawEffect(UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier destination, MK.EdgeDetection.PostProcessingStackV2VolumeComponents.IParameters properties, MK.EdgeDetection.PostProcessing.Generic.ICameraData cameraData, UnityEngine.Rendering.CommandBuffer cmd)
			{
				Material renderMaterial = Config.ReceiveRenderMaterial();
				Material sketchMaterial = Config.ReceiveSketchRenderMaterial();
				
				cmd.SetGlobalTexture(PropertyIDs.mkShaderMainTex, source);
				_materialPropertyBlock.SetMatrix(PropertyIDs.inverseViewProjectionMatrix, (GL.GetGPUProjectionMatrix(cameraData.projectionMatrix, true) * cameraData.viewMatrix).inverse);
				_materialPropertyBlock.SetVector(PropertyIDs.mkShaderMainTexDimension, new Vector2(cameraData.width, cameraData.height));
				float thresholdMultiplier = Config.ReceiveAdaptedThresholdMultiplier(properties.getEnhanceDetails);
				_materialPropertyBlock.SetVector(PropertyIDs.thresholdLowParams, new Vector4(Mathf.Max(Config.kHalfEpsilon, properties.getDepthThreshold.minValue * thresholdMultiplier), Mathf.Max(Config.kHalfEpsilon, properties.getNormalThreshold.minValue * Config.kThresholdMultiplier), Mathf.Max(Config.kHalfEpsilon, properties.getSceneColorThreshold.minValue * Config.kThresholdMultiplier), Mathf.Max(Config.kHalfEpsilon, properties.getDepthThreshold.minValue * Config.kCombinedInputThresholdMultiplier)));
				_materialPropertyBlock.SetVector(PropertyIDs.thresholdHighParams, new Vector4(properties.getDepthThreshold.maxValue * thresholdMultiplier, properties.getNormalThreshold.maxValue * Config.kThresholdMultiplier, properties.getSceneColorThreshold.maxValue * Config.kThresholdMultiplier, properties.getDepthThreshold.maxValue * Config.kCombinedInputThresholdMultiplier));
				_materialPropertyBlock.SetColor(PropertyIDs.lineColor, properties.getLineColor);
				
				float pixelPerfectionScale = Config.pixelPerfection ? 2.0f : 1.0f;
				//float sketchLinesScale = properties.getSketch.value ? 0.5f : 1.0f;
				float sketchLinesScale = 1.0f;
				float logDepthSize = AdjustSize(properties.getDepthLineSize * properties.getGlobalLineSize * pixelPerfectionScale * sketchLinesScale, cameraData.width, cameraData.height, properties.getLineSizeMatchFactor, cameraData.allowDynamicResolution);
				float logNormalLineSize = AdjustSize(properties.getNormalLineSize * properties.getGlobalLineSize * pixelPerfectionScale * sketchLinesScale, cameraData.width, cameraData.height, properties.getLineSizeMatchFactor, cameraData.allowDynamicResolution);
				float logSceneColorLineSize = AdjustSize(properties.getSceneColorLineSize * properties.getGlobalLineSize * pixelPerfectionScale * sketchLinesScale, cameraData.width, cameraData.height, properties.getLineSizeMatchFactor, cameraData.allowDynamicResolution);
				_materialPropertyBlock.SetVector(PropertyIDs.lineSizeParams, new Vector4(logDepthSize, logNormalLineSize, logSceneColorLineSize));
				
				_materialPropertyBlock.SetVector(PropertyIDs.depthFadeParams, new UnityEngine.Vector4(properties.getDepthFadeLimits.minValue, properties.getDepthFadeLimits.maxValue, properties.getDepthNearFade, properties.getDepthFarFade));
				_materialPropertyBlock.SetVector(PropertyIDs.normalsFadeParams, new UnityEngine.Vector4(properties.getNormalFadeLimits.minValue, properties.getNormalFadeLimits.maxValue, properties.getNormalNearFade, properties.getNormalsFarFade));
				_materialPropertyBlock.SetVector(PropertyIDs.colorsFadeParams, new UnityEngine.Vector4(properties.getSceneColorFadeLimits.minValue, properties.getSceneColorFadeLimits.maxValue, properties.getSceneColorNearFade, properties.getSceneColorFarFade));
				_materialPropertyBlock.SetFloat(PropertyIDs.lineHardness, properties.getLineHardness * Config.kLineHardnessDamping);
				_materialPropertyBlock.SetColor(PropertyIDs.overlayColor, properties.getOverlayColor);
				_materialPropertyBlock.SetVector(PropertyIDs.fogParams, new Vector4(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));
				KernelCollection.SetKernels(_materialPropertyBlock, renderMaterial, properties.getWorkflow, properties.getPrecision, properties.getLargeKernel, properties.getMediumKernel, properties.getSmallKernel);
				
				Keywords.SetKeyword(renderMaterial, Keywords.enhanceDetails, properties.getEnhanceDetails && (properties.getInputData & (InputData.Depth | InputData.Normal)) != 0);
				Keywords.SetKeyword(renderMaterial, Keywords.inputDepth, (properties.getInputData & InputData.Depth) != 0);
				Keywords.SetKeyword(renderMaterial, Keywords.inputNormal, (properties.getInputData & InputData.Normal) != 0);
				Keywords.SetKeyword(renderMaterial, Keywords.inputSceneColor, (properties.getInputData & InputData.SceneColor) != 0);
				Keywords.SetKeyword(renderMaterial, Keywords.fade, properties.getFade);
				Keywords.SetKeyword(renderMaterial, Keywords.visualizeEdges, properties.getVisualizeEdges);
				Keywords.SetKeyword(renderMaterial, Keywords.sketch, properties.getSketch);
				
				#if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
				Keywords.SetKeyword(renderMaterial, Keywords.selectiveWorkflow, false);
				#endif
				
				if(properties.getSketch.value)
				{
					MKBlitter.GetTemporaryTexture(cmd, PropertyIDs.edgeMask, MKBlitter.GetRenderTextureDescriptor(cameraData, cameraData.width, cameraData.height, RenderTextureFormat.Default, false));
					
					MKBlitter.Draw(cmd, PropertyIDs.edgeMask, renderMaterial, _materialPropertyBlock, 0, new Rect(0, 0, cameraData.width, cameraData.height));
					_materialPropertyBlock.SetVector(PropertyIDs.sketchFadeParams, new UnityEngine.Vector4(properties.getSketchFadeLimits.minValue, properties.getSketchFadeLimits.maxValue, properties.getSketchNearFade, properties.getSketchFarFade));
					_materialPropertyBlock.SetVector(PropertyIDs.sketchParams, new Vector4(properties.getSketchIntensity * 0.005f, properties.getSketchFrequency));
					cmd.SetGlobalTexture(PropertyIDs.sketchNoiseMap, properties.getSketchAdditionalNoiseMap.value);
					Keywords.SetKeyword(sketchMaterial, Keywords.visualizeEdges, properties.getVisualizeEdges);
					Keywords.SetKeyword(sketchMaterial, Keywords.sketchAdditionalNoiseMap, properties.getSketchAdditionalNoiseMap.value != null ? true : false);
					Keywords.SetKeyword(sketchMaterial, Keywords.fade, properties.getFade);
					MKBlitter.Draw(cmd, destination, sketchMaterial, _materialPropertyBlock, 0, new Rect(0, 0, cameraData.width, cameraData.height));
					
					MKBlitter.ReleaseTemporaryTexture(cmd, PropertyIDs.edgeMask);
				}
				else
				{
					MKBlitter.Draw(cmd, destination, renderMaterial, _materialPropertyBlock, 0, new Rect(0, 0, cameraData.width, cameraData.height));
				}
			}
			
			/****************************************************/
			/* Render */
			/****************************************************/
			public override void Render(UnityEngine.Rendering.PostProcessing.PostProcessRenderContext context)
			{
				context.command.BeginSample(_profilerName);
				context.command.EnableShaderKeyword(_ppsv2Keyword);
				_renderingContext = context;
				settings.MKOnInitialize();
				DrawEffect(context.source, context.destination, this.settings, this, context.command);
				context.command.DisableShaderKeyword(_ppsv2Keyword);
				context.command.EndSample(_profilerName);
			}
			
			/****************************************************/
			/* MKGetDepthTextureMode */
			/****************************************************/
			private DepthTextureMode MKGetDepthTextureMode(MK.EdgeDetection.PostProcessingStackV2VolumeComponents.IParameters properties)
			{
				DepthTextureMode depthTextureMode = DepthTextureMode.None;
				
				if(properties.getInputData.value.HasFlag(InputData.Depth))
					depthTextureMode |= DepthTextureMode.Depth;
				if(properties.getInputData.value.HasFlag(InputData.Normal))
					depthTextureMode |= DepthTextureMode.DepthNormals;
				
				return depthTextureMode;
			}
			
			/****************************************************/
			/* GetCameraFlags */
			/****************************************************/
			public override DepthTextureMode GetCameraFlags()
			{
				return MKGetDepthTextureMode(settings);
			}
		}
	}
}
#endif