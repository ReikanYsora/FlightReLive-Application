/******************************************************************************************/
/* Universal Renderer Feature */
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
/* Exported on: 28.05.2025 23:05:59 */

#if MK_URP
using UnityEngine;
using UnityEngine.Rendering.Universal;
/****************************************************/
/* Namespace Injection */
/****************************************************/
using MK.EdgeDetection.PostProcessing.Generic;

namespace MK.EdgeDetection.UniversalRendererFeatures
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
	/* Renderer Feature */
	/******************************************************************************************/
	[System.Serializable] [DisallowMultipleRendererFeature("MK Edge Detection")]
	public sealed class MKEdgeDetectionRendererFeature : UnityEngine.Rendering.Universal.ScriptableRendererFeature, MK.EdgeDetection.UniversalRendererFeatures.IParameters
	{
		#if UNITY_EDITOR
		private bool _rendererFeatureRunsInEditor => true;
		#else
		private bool _rendererFeatureRunsInEditor => false;
		#endif
		public enum WorkMode
		{
			[UnityEngine.InspectorName("Post-Process Volumes")]
			PostProcessVolumes = 0,
			[UnityEngine.InspectorName("Global")]
			Global = 1
		};
		public WorkMode workMode = WorkMode.PostProcessVolumes;
		
		
		private static UnityEngine.Material _copyMaterial = null;
		private static UnityEngine.Shader _copyShader = null;
		
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
		[UnityEngine.SerializeField] [UnityEngine.HideInInspector] public bool _propertiesInitialized = false;
		#pragma warning restore CS0414
		#endif
		
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
		/* Render Pass */
		/****************************************************/
		public sealed class MKEdgeDetectionRenderPass : UnityEngine.Rendering.Universal.ScriptableRenderPass, MK.EdgeDetection.PostProcessing.Generic.ICameraData
		{
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
			
			private MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection volumeComponent
			{
				get
				{
					return UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection>();
				}
			}
			
			public Material copyMaterial = null;
			public WorkMode activeWorkmode = WorkMode.PostProcessVolumes;
			public MK.EdgeDetection.UniversalRendererFeatures.IParameters rendererFeatureSettings = null;
			public UnityEngine.Rendering.Universal.ScriptableRenderer scriptableRenderer = null;
			public DepthTextureMode depthTextureMode = DepthTextureMode.None;
			
			/****************************************************/
			/* Pass Data */
			/****************************************************/
			public sealed class PassData : MK.EdgeDetection.PostProcessing.Generic.ICameraData
			{
				public MK.EdgeDetection.UniversalVolumeComponents.MKEdgeDetection volumeComponent = null;
				public MKEdgeDetectionRenderPass renderPass;
				#if UNITY_2023_3_OR_NEWER
				public UnityEngine.Rendering.RenderGraphModule.TextureHandle sourceTextureHandle;
				public UnityEngine.Rendering.RenderGraphModule.TextureHandle destinationTextureHandle;
				public UnityEngine.Rendering.Universal.UniversalCameraData universalCameraData = null;
				#endif
				public UnityEngine.Rendering.RenderTargetIdentifier sourceRenderTargetIdentifier;
				public UnityEngine.Rendering.RenderTargetIdentifier destinationRenderTargetIdentifier;
				public MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature.WorkMode workMode = MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature.WorkMode.PostProcessVolumes;
				public MK.EdgeDetection.UniversalVolumeComponents.IParameters universalVolumeSettings = null;
				public UnityEngine.Rendering.Universal.CameraData cameraData;
				public UnityEngine.RenderTextureDescriptor sourceDescriptor;
				public MK.EdgeDetection.UniversalRendererFeatures.IParameters rendererFeatureSettings = null;
				public readonly static int renderBufferID = UnityEngine.Shader.PropertyToID("_MKEdgeDetectionScriptableRendererBuffer");
				public readonly static string renderGraphRenderBufferName = "_MKEdgeDetectionRenderGraphScriptableRendererBuffer";
				public readonly static int mainTexID = UnityEngine.Shader.PropertyToID("_MKShaderMainTex");
				public Material copyMaterial = null;
				public DepthTextureMode depthTextureMode = DepthTextureMode.None;
				public bool xrEnvironment { get; set; }
				public int width { get; set; }
				public int height { get; set; }
				public bool stereoEnabled { get; set; }
				public float aspect { get; set; }
				public Matrix4x4 viewMatrix { get; set; }
				public Matrix4x4 projectionMatrix { get; set; }
				public bool hasTargetTexture { get; set; }
				public bool useCustomRenderTargetSetup { get; set; }
				public UnityEngine.Rendering.TextureDimension customRenderTargetDimension { get; set; }
				public int customRenderTargetVolumeDepth { get; set; }
				public bool isSceneView { get; set; }
				public bool allowDynamicResolution { get; set; }
			}
			private PassData _passData = new PassData();
			public PassData passData { get { return _passData; } set { _passData = value; } }
			private const string _profilerName = "MKEdgeDetection";
			
			/****************************************************/
			/* Core Functions */
			/****************************************************/
			/****************************************************/
			/* Render Pass */
			/****************************************************/
			public MKEdgeDetectionRenderPass()
			{
				this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingSkybox;
			}
			
			/****************************************************/
			/* Dispose */
			/****************************************************/
			public void Dispose()
			{
			}
			
			/****************************************************/
			/* Configure */
			/****************************************************/
			#if UNITY_2023_3_OR_NEWER
			[System.Obsolete]
			#endif
			public override void Configure(UnityEngine.Rendering.CommandBuffer cmd, UnityEngine.RenderTextureDescriptor cameraTextureDescriptor)
			{
				_passData.sourceDescriptor = cameraTextureDescriptor;
			}
			
			/****************************************************/
			/* On Camera Setup */
			/****************************************************/
			#if UNITY_2023_3_OR_NEWER
			[System.Obsolete]
			#endif
			public override void OnCameraSetup(UnityEngine.Rendering.CommandBuffer cmd, ref UnityEngine.Rendering.Universal.RenderingData renderingData) 
			{
				base.OnCameraSetup(cmd, ref renderingData);
				#if UNITY_2022_1_OR_NEWER
					_passData.sourceRenderTargetIdentifier = scriptableRenderer.cameraColorTargetHandle;
				#else
					_passData.sourceRenderTargetIdentifier = scriptableRenderer.cameraColorTarget;
				#endif
			}
			
			/****************************************************/
			/* Execute */
			/****************************************************/
			#if UNITY_2023_3_OR_NEWER
			[System.Obsolete]
			#endif
			public override void Execute(UnityEngine.Rendering.ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
			{
				_passData.volumeComponent = this.volumeComponent;
				_passData.workMode = this.activeWorkmode;
				_passData.cameraData = renderingData.cameraData;
				_passData.copyMaterial = copyMaterial;
				if(_passData.workMode == WorkMode.PostProcessVolumes)
				{
					if(_passData.volumeComponent == null || renderingData.cameraData.camera == null)
						return;
					if(!_passData.volumeComponent.IsActive() || !_passData.volumeComponent.active)
						return;
					_passData.universalVolumeSettings = _passData.volumeComponent;
					_passData.rendererFeatureSettings = rendererFeatureSettings;
					renderingData.cameraData.camera.depthTextureMode |= _passData.depthTextureMode;
				}
				else
				{
					if(renderingData.cameraData.camera == null)
						return;
					_passData.rendererFeatureSettings = rendererFeatureSettings;
					renderingData.cameraData.camera.depthTextureMode |= _passData.depthTextureMode;
				}
				UnityEngine.Rendering.CommandBuffer cmd = UnityEngine.Rendering.CommandBufferPool.Get(_profilerName);
				if(renderingData.cameraData.camera.allowDynamicResolution)
				{
					_passData.width = Mathf.RoundToInt(renderingData.cameraData.cameraTargetDescriptor.width * UnityEngine.ScalableBufferManager.widthScaleFactor);
					_passData.height = Mathf.RoundToInt(renderingData.cameraData.cameraTargetDescriptor.height * UnityEngine.ScalableBufferManager.heightScaleFactor);
				}
				else
				{
					_passData.width = renderingData.cameraData.cameraTargetDescriptor.width;
					_passData.height = renderingData.cameraData.cameraTargetDescriptor.height;
				}
				#if UNITY_2020_2_OR_NEWER
				_passData.stereoEnabled = renderingData.cameraData.xrRendering;
				#else
				_passData.stereoEnabled = renderingData.cameraData.isStereoEnabled;
				#endif
				_passData.isSceneView = renderingData.cameraData.camera.cameraType == CameraType.SceneView;
				_passData.aspect = renderingData.cameraData.camera.aspect;
				_passData.viewMatrix = renderingData.cameraData.camera.worldToCameraMatrix;
				_passData.projectionMatrix = renderingData.cameraData.camera.projectionMatrix;
				_passData.hasTargetTexture = renderingData.cameraData.camera.targetTexture != null ? true : false;
				_passData.useCustomRenderTargetSetup = false;
				_passData.customRenderTargetDimension = UnityEngine.Rendering.TextureDimension.Tex2D;
				_passData.customRenderTargetVolumeDepth = 1;
				_passData.allowDynamicResolution = renderingData.cameraData.camera.allowDynamicResolution;
				#if !UNITY_2021_3_OR_NEWER
					_passData.sourceRenderTargetIdentifier = scriptableRenderer.cameraColorTarget;
				#endif
				_passData.destinationRenderTargetIdentifier = new UnityEngine.Rendering.RenderTargetIdentifier(PassData.renderBufferID, 0, CubemapFace.Unknown, -1);
				
				cmd.GetTemporaryRT(PassData.renderBufferID, _passData.sourceDescriptor, UnityEngine.FilterMode.Bilinear);
				cmd.SetGlobalTexture(PassData.mainTexID, _passData.sourceRenderTargetIdentifier);
				cmd.SetRenderTarget(_passData.destinationRenderTargetIdentifier, 0, CubemapFace.Unknown, -1);
				if(UnityEngine.SystemInfo.graphicsShaderLevel >= 35)
					cmd.DrawProcedural(Matrix4x4.identity, _passData.copyMaterial, 0, UnityEngine.MeshTopology.Triangles, 3, 1);
				else
					cmd.DrawMesh(MK.EdgeDetection.PostProcessing.Generic.MKBlitter.screenMesh, Matrix4x4.identity, _passData.copyMaterial, 0, 0);
				if(_passData.workMode == WorkMode.PostProcessVolumes)
				{
					DrawEffect(_passData.destinationRenderTargetIdentifier, _passData.sourceRenderTargetIdentifier, _passData.universalVolumeSettings, _passData, cmd);
				}
				else
					DrawEffect(_passData.destinationRenderTargetIdentifier, _passData.sourceRenderTargetIdentifier, _passData.rendererFeatureSettings, _passData, cmd);
				cmd.ReleaseTemporaryRT(PassData.renderBufferID);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				UnityEngine.Rendering.CommandBufferPool.Release(cmd);
			}
			
			/****************************************************/
			/* DrawEffect */
			/****************************************************/
			private void DrawEffect(UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier destination, MK.EdgeDetection.UniversalRendererFeatures.IParameters properties, MK.EdgeDetection.PostProcessing.Generic.ICameraData cameraData, UnityEngine.Rendering.CommandBuffer cmd)
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
			/* DrawEffect */
			/****************************************************/
			private void DrawEffect(UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier destination, MK.EdgeDetection.UniversalVolumeComponents.IParameters properties, MK.EdgeDetection.PostProcessing.Generic.ICameraData cameraData, UnityEngine.Rendering.CommandBuffer cmd)
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
			
			
			#if UNITY_2023_3_OR_NEWER
			private static void ExecutePass(PassData data, UnityEngine.Rendering.RenderGraphModule.UnsafeGraphContext context)
			{
				if(data.workMode == WorkMode.PostProcessVolumes)
				{
					if(data.volumeComponent == null || data.universalCameraData.camera == null)
						return;
					if(!data.volumeComponent.IsActive() || !data.volumeComponent.active)
						return;
					data.universalVolumeSettings = data.volumeComponent;
					data.universalCameraData.camera.depthTextureMode |= data.depthTextureMode;
				}
				else
				{
					if(data.universalCameraData.camera == null)
						return;
					data.universalCameraData.camera.depthTextureMode |= data.depthTextureMode;
				}
				UnityEngine.Rendering.CommandBuffer unsafeCmd = UnityEngine.Rendering.CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
				unsafeCmd.SetGlobalTexture(PassData.mainTexID, data.sourceTextureHandle);
				unsafeCmd.SetRenderTarget(data.destinationTextureHandle, 0, CubemapFace.Unknown, -1);
				if(UnityEngine.SystemInfo.graphicsShaderLevel >= 35)
					unsafeCmd.DrawProcedural(Matrix4x4.identity, data.copyMaterial, 0, UnityEngine.MeshTopology.Triangles, 3, 1);
				else
					unsafeCmd.DrawMesh(MK.EdgeDetection.PostProcessing.Generic.MKBlitter.screenMesh, Matrix4x4.identity, data.copyMaterial, 0, 0);
				if(data.workMode == WorkMode.PostProcessVolumes)
					data.renderPass.DrawEffect(data.destinationTextureHandle, data.sourceTextureHandle, data.universalVolumeSettings, data, unsafeCmd);
				else
					data.renderPass.DrawEffect(data.destinationTextureHandle, data.sourceTextureHandle, data.rendererFeatureSettings, data, unsafeCmd);
			}
			
			/****************************************************/
			/* Execute Render Graph */
			/****************************************************/
			public override void RecordRenderGraph(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph, UnityEngine.Rendering.ContextContainer frameData)
			{
				using (var builder = renderGraph.AddUnsafePass<PassData>("MKEdgeDetection", out var passData))
				{
					UnityEngine.Rendering.Universal.UniversalResourceData resourceData = frameData.Get<UnityEngine.Rendering.Universal.UniversalResourceData>();
					UnityEngine.Rendering.Universal.UniversalCameraData universalCameraData = frameData.Get<UnityEngine.Rendering.Universal.UniversalCameraData>();
					passData.workMode = this.activeWorkmode;
					passData.volumeComponent = this.volumeComponent;
					passData.universalCameraData = universalCameraData;
					passData.copyMaterial = this.copyMaterial;
					passData.rendererFeatureSettings = this.rendererFeatureSettings;
					passData.depthTextureMode = this.depthTextureMode;
					if(universalCameraData.camera.allowDynamicResolution)
					{
						passData.width = Mathf.RoundToInt(universalCameraData.cameraTargetDescriptor.width * UnityEngine.ScalableBufferManager.widthScaleFactor);
						passData.height = Mathf.RoundToInt(universalCameraData.cameraTargetDescriptor.height * UnityEngine.ScalableBufferManager.heightScaleFactor);
					}
					else
					{
						passData.width = universalCameraData.cameraTargetDescriptor.width;
						passData.height = universalCameraData.cameraTargetDescriptor.height;
					}
					passData.stereoEnabled = universalCameraData.xrRendering;
					passData.isSceneView = universalCameraData.camera.cameraType == CameraType.SceneView;
					passData.aspect = universalCameraData.camera.aspect;
					passData.viewMatrix = universalCameraData.camera.worldToCameraMatrix;
					passData.projectionMatrix = universalCameraData.camera.projectionMatrix;
					passData.hasTargetTexture = universalCameraData.camera.targetTexture != null ? true : false;
					passData.useCustomRenderTargetSetup = false;
					passData.customRenderTargetDimension = UnityEngine.Rendering.TextureDimension.Tex2D;
					passData.customRenderTargetVolumeDepth = 1;
					passData.allowDynamicResolution = universalCameraData.camera.allowDynamicResolution;
					passData.sourceTextureHandle = resourceData.activeColorTexture;
					passData.renderPass = this;
					RenderTextureDescriptor desc = universalCameraData.cameraTargetDescriptor;
					desc.depthBufferBits = 0;
					UnityEngine.Rendering.RenderGraphModule.TextureHandle destination = UnityEngine.Rendering.Universal.UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, PassData.renderGraphRenderBufferName, false);
					passData.destinationTextureHandle = destination;
					_passData = passData;
					builder.UseTexture(passData.sourceTextureHandle);
					builder.UseTexture(passData.destinationTextureHandle);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc((PassData data, UnityEngine.Rendering.RenderGraphModule.UnsafeGraphContext context) => ExecutePass(data, context));
				}
			}
			#endif
			
			/****************************************************/
			/* Camera Data */
			/****************************************************/
			#if ENABLE_VR
			public bool xrEnvironment => true;
			#else
			public bool xrEnvironment => false;
			#endif
			public int width => _passData.width;
			public int height => _passData.height;
			public bool stereoEnabled => _passData.stereoEnabled;
			public bool isSceneView => _passData.isSceneView;
			public float aspect => _passData.aspect;
			public UnityEngine.Matrix4x4 viewMatrix => _passData.viewMatrix;
			public UnityEngine.Matrix4x4 projectionMatrix => _passData.projectionMatrix;
			public bool hasTargetTexture => _passData.hasTargetTexture;
			public bool useCustomRenderTargetSetup => false;
			public UnityEngine.Rendering.TextureDimension customRenderTargetDimension => UnityEngine.Rendering.TextureDimension.Tex2D;
			public int customRenderTargetVolumeDepth => 1;
			public bool allowDynamicResolution => _passData.allowDynamicResolution;
			
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
		/* Create */
		/****************************************************/
		private MKEdgeDetectionRenderPass _mKEdgeDetectionRenderPass;
		public override void Create()
		{
			if(_mKEdgeDetectionRenderPass == null)
				_mKEdgeDetectionRenderPass = new MKEdgeDetectionRenderPass();
			_mKEdgeDetectionRenderPass.rendererFeatureSettings = this;
			_mKEdgeDetectionRenderPass.OnEnable();
		}
		
		/****************************************************/
		/* Add Render Passes */
		/****************************************************/
		public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if(MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyShader == null)
				MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyShader = UnityEngine.Shader.Find("Hidden/MK/EdgeDetection/PostProcessing/MKEdgeDetectionUniversalCopy");
			if(MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyShader == null)
				return;
			if(MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyMaterial == null)
				MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyMaterial = new Material(MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyShader) { hideFlags = HideFlags.HideAndDontSave };
			if(MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyMaterial == null)
				return;
			if(_mKEdgeDetectionRenderPass == null)
				Create();
			_mKEdgeDetectionRenderPass.copyMaterial = MK.EdgeDetection.UniversalRendererFeatures.MKEdgeDetectionRendererFeature._copyMaterial;
			_mKEdgeDetectionRenderPass.ConfigureInput(MKGetScriptableRenderPassInput(this));
			MKOnInitialize();
			if(workMode == WorkMode.Global)
			{
				if(MKIsActiveGlobal(this) && isActive)
				{
					_mKEdgeDetectionRenderPass.depthTextureMode = MKGetDepthTextureMode(this);
					_mKEdgeDetectionRenderPass.scriptableRenderer = renderer;
					_mKEdgeDetectionRenderPass.activeWorkmode = workMode;
					renderer.EnqueuePass(_mKEdgeDetectionRenderPass);
				}
			}
			else
			{
				if(renderingData.cameraData.postProcessEnabled && isActive)
				{
					_mKEdgeDetectionRenderPass.depthTextureMode = MKGetDepthTextureMode(this);
					_mKEdgeDetectionRenderPass.scriptableRenderer = renderer;
					_mKEdgeDetectionRenderPass.activeWorkmode = workMode;
					renderer.EnqueuePass(_mKEdgeDetectionRenderPass);
				}
			}
		}
		
		/****************************************************/
		/* Dispose */
		/****************************************************/
		protected override void Dispose(bool disposing)
		{
			if(_mKEdgeDetectionRenderPass != null)
			{
				_mKEdgeDetectionRenderPass.OnDisable();
				_mKEdgeDetectionRenderPass.Dispose();
				_mKEdgeDetectionRenderPass = null;
			}
			MKOnTerminate();
		}
		
		/****************************************************/
		/* MKIsActiveGlobal */
		/****************************************************/
		private bool MKIsActiveGlobal(MK.EdgeDetection.UniversalRendererFeatures.IParameters properties)
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
		/* MKGetDepthTextureMode */
		/****************************************************/
		private DepthTextureMode MKGetDepthTextureMode(MK.EdgeDetection.UniversalRendererFeatures.IParameters properties)
		{
			DepthTextureMode depthTextureMode = DepthTextureMode.None;
			
			if(properties.getInputData.value.HasFlag(InputData.Depth))
				depthTextureMode |= DepthTextureMode.Depth;
			if(properties.getInputData.value.HasFlag(InputData.Normal))
				depthTextureMode |= DepthTextureMode.DepthNormals;
			
			return depthTextureMode;
		}
		
		/****************************************************/
		/* MKGetScriptableRenderPassInput */
		/****************************************************/
		private static ScriptableRenderPassInput MKGetScriptableRenderPassInput(MK.EdgeDetection.UniversalRendererFeatures.IParameters properties)
		{
			ScriptableRenderPassInput scriptableRenderPassInput = ScriptableRenderPassInput.None;
			
			if(properties.getInputData.value.HasFlag(InputData.Depth))
				scriptableRenderPassInput |= ScriptableRenderPassInput.Depth;
			if(properties.getInputData.value.HasFlag(InputData.Normal))
				scriptableRenderPassInput |= ScriptableRenderPassInput.Normal;
				
			return scriptableRenderPassInput;
		}
	}
}
#endif