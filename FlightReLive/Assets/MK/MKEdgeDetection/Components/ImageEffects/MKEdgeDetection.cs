/******************************************************************************************/
/* Image Effects Component */
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
/* Exported on: 28.05.2025 23:06:01 */

using UnityEngine;
/****************************************************/
/* Namespace Injection */
/****************************************************/
using MK.EdgeDetection.PostProcessing.Generic;

namespace MK.EdgeDetection.ImageEffectsComponents
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
	/* Image Effects Component */
	/******************************************************************************************/
	[System.Serializable] [UnityEngine.ExecuteAlways] [UnityEngine.ImageEffectAllowedInSceneView] [UnityEngine.DisallowMultipleComponent] [UnityEngine.RequireComponent(typeof(UnityEngine.Camera))]
	public sealed class MKEdgeDetection : UnityEngine.MonoBehaviour, MK.EdgeDetection.ImageEffectsComponents.IParameters, MK.EdgeDetection.PostProcessing.Generic.ICameraData
	{
		/****************************************************/
		/* Parameter Declarations */
		/****************************************************/
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision> precision = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision>(Precision.Medium);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel> largeKernel = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel>(LargeKernel.Sobel);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel> mediumKernel = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel>(MediumKernel.RobertsCrossDiagonal);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel> smallKernel = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel>(SmallKernel.HalfCrossHorizontal);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData> inputData = new MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData>(InputData.Depth|InputData.Normal);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty lineHardness = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0.5f, 0, 1);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.BoolProperty fade = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty lineSizeMatchFactor = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0.5f, 0, 1);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty globalLineSize = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0f, 0, 3);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty depthLineSize = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(1f, 0, 2);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty normalLineSize = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(1f, 0, 2);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty depthFadeLimits = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty normalFadeLimits = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty depthThreshold = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.1f, 1f, 0, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.ColorProperty lineColor = new MK.EdgeDetection.PostProcessing.Generic.ColorProperty(0, 0, 0, 1, true, false);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.ColorProperty overlayColor = new MK.EdgeDetection.PostProcessing.Generic.ColorProperty(1, 1, 1, 0, true, false);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty depthNearFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(10);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty depthFarFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(30);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty normalNearFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(5);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty normalsFarFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(20);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty normalThreshold = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.1f, 1f, 0, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty sceneColorThreshold = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.1f, 1f, 0, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty sceneColorLineSize = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(1f, 0, 2);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty sceneColorNearFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(0);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty sceneColorFarFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(10);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty sceneColorFadeLimits = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.BoolProperty visualizeEdges = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.BoolProperty enhanceDetails = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.BoolProperty sketch = new MK.EdgeDetection.PostProcessing.Generic.BoolProperty(false);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.RangeProperty sketchIntensity = new MK.EdgeDetection.PostProcessing.Generic.RangeProperty(0.333f, 0, 1);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.FloatProperty sketchFrequency = new MK.EdgeDetection.PostProcessing.Generic.FloatProperty(4);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty sketchAdditionalNoiseMap = new MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty(null);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty sketchFadeLimits = new MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty(0.0f, 1.0f, 0.0f, 1.0f);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty sketchNearFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(10);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty sketchFarFade = new MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty(30);
		 [field: SerializeField] public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow> workflow = new MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow>(Workflow.Generic);
		#if UNITY_EDITOR
		#pragma warning disable CS0414
		[UnityEngine.SerializeField] [UnityEngine.HideInInspector] private bool _propertiesInitialized = false;
		#pragma warning restore CS0414
		#endif
		private const string _profilerName = "MKEdgeDetection";
		private UnityEngine.Camera _renderingCamera = null;
		private UnityEngine.Rendering.CommandBuffer _commandBuffer = null;
		
		/****************************************************/
		/* Volume Component Interface Implementation */
		/****************************************************/
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Precision> getPrecision => precision;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<LargeKernel> getLargeKernel => largeKernel;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<MediumKernel> getMediumKernel => mediumKernel;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<SmallKernel> getSmallKernel => smallKernel;
		public MK.EdgeDetection.PostProcessing.Generic.BitmaskProperty<InputData> getInputData => inputData;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getLineHardness => lineHardness;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getFade => fade;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getLineSizeMatchFactor => lineSizeMatchFactor;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getGlobalLineSize => globalLineSize;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getDepthLineSize => depthLineSize;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getNormalLineSize => normalLineSize;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getDepthFadeLimits => depthFadeLimits;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getNormalFadeLimits => normalFadeLimits;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getDepthThreshold => depthThreshold;
		public MK.EdgeDetection.PostProcessing.Generic.ColorProperty getLineColor => lineColor;
		public MK.EdgeDetection.PostProcessing.Generic.ColorProperty getOverlayColor => overlayColor;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getDepthNearFade => depthNearFade;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getDepthFarFade => depthFarFade;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getNormalNearFade => normalNearFade;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getNormalsFarFade => normalsFarFade;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getNormalThreshold => normalThreshold;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSceneColorThreshold => sceneColorThreshold;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getSceneColorLineSize => sceneColorLineSize;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSceneColorNearFade => sceneColorNearFade;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSceneColorFarFade => sceneColorFarFade;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSceneColorFadeLimits => sceneColorFadeLimits;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getVisualizeEdges => visualizeEdges;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getEnhanceDetails => enhanceDetails;
		public MK.EdgeDetection.PostProcessing.Generic.BoolProperty getSketch => sketch;
		public MK.EdgeDetection.PostProcessing.Generic.RangeProperty getSketchIntensity => sketchIntensity;
		public MK.EdgeDetection.PostProcessing.Generic.FloatProperty getSketchFrequency => sketchFrequency;
		public MK.EdgeDetection.PostProcessing.Generic.Texture2DProperty getSketchAdditionalNoiseMap => sketchAdditionalNoiseMap;
		public MK.EdgeDetection.PostProcessing.Generic.MinMaxRangeProperty getSketchFadeLimits => sketchFadeLimits;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSketchNearFade => sketchNearFade;
		public MK.EdgeDetection.PostProcessing.Generic.AbsFloatProperty getSketchFarFade => sketchFarFade;
		public MK.EdgeDetection.PostProcessing.Generic.EnumProperty<Workflow> getWorkflow => workflow;
		
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
		/* MKIsTileCompatible */
		/****************************************************/
		private bool MKIsTileCompatible()
		{
			return true;
		}
		
		/****************************************************/
		/* MKIsActiveGlobal */
		/****************************************************/
		private bool MKIsActiveGlobal(MK.EdgeDetection.ImageEffectsComponents.IParameters properties)
		{
			return properties.getInputData != InputData.None && properties.getGlobalLineSize > 0 || properties.getVisualizeEdges;
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
		/* On Enable */
		/****************************************************/
		public void OnEnable()
		{_materialPropertyBlock = new MaterialPropertyBlock();
			_renderingCamera = GetComponent<UnityEngine.Camera>();
			if(this._commandBuffer != null)
			{
				this._commandBuffer.Clear();
				this._commandBuffer.Dispose();
				this._commandBuffer = null;
			}
			_commandBuffer = new UnityEngine.Rendering.CommandBuffer() { name = "MKEdgeDetection Command Buffer" };
		}
		
		/****************************************************/
		/* On Disable */
		/****************************************************/
		public void OnDisable()
		{
			_materialPropertyBlock = null;
			if(this._commandBuffer != null)
			{
				this._commandBuffer.Clear();
				this._commandBuffer.Dispose();
				this._commandBuffer = null;
				}
			_renderingCamera = null;
			MKOnTerminate();
		}
		
		/****************************************************/
		/* MKGetDepthTextureMode */
		/****************************************************/
		private DepthTextureMode MKGetDepthTextureMode(MK.EdgeDetection.ImageEffectsComponents.IParameters properties)
		{
			DepthTextureMode depthTextureMode = DepthTextureMode.None;
			
			if(properties.getInputData.value.HasFlag(InputData.Depth))
				depthTextureMode |= DepthTextureMode.Depth;
			if(properties.getInputData.value.HasFlag(InputData.Normal))
				depthTextureMode |= DepthTextureMode.DepthNormals;
			
			return depthTextureMode;
		}
		
		private bool resolvedMsaaRequired { get { return UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass && _renderingCamera.stereoEnabled; } }
		private RenderTexture copyBuffer;
		
		/****************************************************/
		/* DrawEffect */
		/****************************************************/
		private void DrawEffect(UnityEngine.Rendering.CommandBuffer cmd, UnityEngine.RenderTexture source, UnityEngine.RenderTexture destination, MK.EdgeDetection.ImageEffectsComponents.IParameters properties, MK.EdgeDetection.PostProcessing.Generic.ICameraData cameraData)
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
		/* On Render Image */
		/****************************************************/
		[UnityEngine.ImageEffectUsesCommandBuffer]
		private void OnRenderImage(UnityEngine.RenderTexture source, UnityEngine.RenderTexture destination)
		{
			//Compatibility Mode Only
			
			UnityEngine.Profiling.Profiler.BeginSample(_profilerName);
			if(_renderingCamera == null || !MKIsActiveGlobal(this))
			{
				UnityEngine.Graphics.Blit(source, destination);
				return;
			}
			
			_renderingCamera.depthTextureMode = MKGetDepthTextureMode(this);
			
			RenderTextureDescriptor desc = source.descriptor;
			desc.msaaSamples = 1;
			if(resolvedMsaaRequired)
			{
				copyBuffer = RenderTexture.GetTemporary(desc);
				UnityEngine.Graphics.Blit(source, copyBuffer);
			}
			else
			{
				copyBuffer = source;
			}
			MKOnInitialize();
			RenderTexture destinationBuffer = RenderTexture.GetTemporary(desc);
			DrawEffect(_commandBuffer, copyBuffer, destinationBuffer, this, this);
			UnityEngine.Graphics.ExecuteCommandBuffer(_commandBuffer);
			UnityEngine.Graphics.Blit(destinationBuffer, destination);
			_commandBuffer.Clear();
			if(resolvedMsaaRequired)
				RenderTexture.ReleaseTemporary(copyBuffer);
			RenderTexture.ReleaseTemporary(destinationBuffer);
			UnityEngine.Profiling.Profiler.EndSample();
		}
		
		/****************************************************/
		/* Camera Data */
		/****************************************************/
		#if ENABLE_VR
		public bool xrEnvironment => true;
		#else
		public bool xrEnvironment => false;
		#endif
		public int width => _renderingCamera.allowDynamicResolution ? Mathf.RoundToInt(_renderingCamera.pixelWidth * ScalableBufferManager.widthScaleFactor) : _renderingCamera.pixelWidth;
		public int height => _renderingCamera.allowDynamicResolution ? Mathf.RoundToInt(_renderingCamera.pixelHeight * ScalableBufferManager.heightScaleFactor) : _renderingCamera.pixelHeight;
		public bool stereoEnabled => _renderingCamera.stereoEnabled;
		public bool isSceneView => _renderingCamera.cameraType == CameraType.SceneView;
		public float aspect => _renderingCamera.aspect;
		public UnityEngine.Matrix4x4 viewMatrix => _renderingCamera.worldToCameraMatrix;
		public UnityEngine.Matrix4x4 projectionMatrix => _renderingCamera.projectionMatrix;
		public bool hasTargetTexture => _renderingCamera.targetTexture != null ? true : false;
		public bool useCustomRenderTargetSetup => false;
		public UnityEngine.Rendering.TextureDimension customRenderTargetDimension => UnityEngine.Rendering.TextureDimension.Tex2D;
		public int customRenderTargetVolumeDepth => 1;
		public bool allowDynamicResolution => _renderingCamera.allowDynamicResolution;
	}
}