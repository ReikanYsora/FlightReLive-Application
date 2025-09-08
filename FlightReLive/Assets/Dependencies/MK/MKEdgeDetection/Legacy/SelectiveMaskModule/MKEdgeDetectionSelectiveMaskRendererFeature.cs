#if MK_URP && MK_SELECTIVE_MASK_ENABLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MK.EdgeDetection.UniversalRendererFeatures
{
    [System.Serializable] [DisallowMultipleRendererFeature("MK Edge Detection Selective Mask")]
	public sealed class MKEdgeDetectionSelectiveMaskRendererFeature : UnityEngine.Rendering.Universal.ScriptableRendererFeature
	{
        public LayerMask layer = -1;

        public class MKEdgeDetectionSelectiveMaskPass : ScriptableRenderPass
        {
            private const string selectiveMaskName = "_MKEdgeDetectionSelectiveMask";
            private static readonly int selectiveMaskID = UnityEngine.Shader.PropertyToID(selectiveMaskName);

            private Material _renderMaterial;
            private readonly RTHandle _selectiveMaskRenderTarget;

            private List<ShaderTagId> _shaderTagList;
            private FilteringSettings _filteringSettings;

            public LayerMask layerMask = -1;

            private RenderTextureDescriptor _sourceDescriptor;

            #if UNITY_2023_3_OR_NEWER
            public sealed class PassData
			{
				#if UNITY_2023_3_OR_NEWER
				public UnityEngine.Rendering.RenderGraphModule.TextureHandle selectiveMaskRenderTarget;
				#endif

				public Material renderMaterial;
                public List<ShaderTagId> shaderTagList;
                public FilteringSettings filteringSettings;
                public LayerMask layerMask = -1;
                public RenderTextureDescriptor sourceDescriptor;
                public UnityEngine.Rendering.RenderGraphModule.RendererListHandle rendererListHandle;
			}
			private PassData _passData = new PassData();
			public PassData passData { get { return _passData; } set { _passData = value; } }
            #endif

            private void SetupSettings()
            {
                this.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
                _filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

                _shaderTagList = new List<ShaderTagId>()
                {
                    new ShaderTagId("DepthOnly")
                };
            }

            private void SetupRenderMaterial()
            {
                if(_renderMaterial == null)
                    _renderMaterial = MK.EdgeDetection.Config.ReceiveSelectiveRenderMaterial();
            }

            public MKEdgeDetectionSelectiveMaskPass()
            {
                SetupRenderMaterial();
                SetupSettings();

                _selectiveMaskRenderTarget = RTHandles.Alloc(selectiveMaskName, name: selectiveMaskName);
            }

            #if UNITY_2023_3_OR_NEWER
			[System.Obsolete]
			#endif
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                SetupRenderMaterial();

                _sourceDescriptor = cameraTextureDescriptor;
                _sourceDescriptor.msaaSamples = 1;
                _sourceDescriptor.colorFormat = RenderTextureFormat.RFloat;

                cmd.GetTemporaryRT(selectiveMaskID, _sourceDescriptor, FilterMode.Point);

                ConfigureTarget(_selectiveMaskRenderTarget);
                ConfigureClear(ClearFlag.All, Color.black);
            }

            #if UNITY_2023_3_OR_NEWER
			[System.Obsolete]
			#endif
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if(!_renderMaterial)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, new ProfilingSampler("MK Edge Detection Selective Mask")))
                {
                    DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                    drawingSettings.enableDynamicBatching = true;
                    drawingSettings.overrideMaterial = _renderMaterial;

                    _filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(selectiveMaskID);
            }

            #if UNITY_2023_3_OR_NEWER
			private static void ExecutePass(PassData data, UnityEngine.Rendering.RenderGraphModule.UnsafeGraphContext context)
			{
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                RenderTextureDescriptor desc = data.sourceDescriptor;
                desc.msaaSamples = 1;
                desc.colorFormat = RenderTextureFormat.RFloat;

                cmd.GetTemporaryRT(selectiveMaskID, desc, FilterMode.Point);

                RenderTargetIdentifier selectiveMaskRTI = new RenderTargetIdentifier(selectiveMaskID, 0, CubemapFace.Unknown, -1);
                cmd.SetRenderTarget(selectiveMaskRTI, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(true, true, Color.black);

                cmd.DrawRendererList(data.rendererListHandle);
			}
			
			/****************************************************/
			/* Execute Render Graph */
			/****************************************************/
			public override void RecordRenderGraph(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph, UnityEngine.Rendering.ContextContainer frameData)
			{
				using (var builder = renderGraph.AddUnsafePass<PassData>("MK Edge Detection Selective Mask", out var passData))
				{
                    SetupRenderMaterial();

                    UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
					UnityEngine.Rendering.Universal.UniversalResourceData resourceData = frameData.Get<UnityEngine.Rendering.Universal.UniversalResourceData>();
					UnityEngine.Rendering.Universal.UniversalCameraData universalCameraData = frameData.Get<UnityEngine.Rendering.Universal.UniversalCameraData>();
					UniversalLightData lightData = frameData.Get<UniversalLightData>();

                    passData.renderMaterial = this._renderMaterial;
                    passData.shaderTagList = this._shaderTagList;
                    passData.layerMask = this.layerMask;
                    passData.sourceDescriptor = universalCameraData.cameraTargetDescriptor;

                    DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagList, renderingData, universalCameraData, lightData, universalCameraData.defaultOpaqueSortFlags);
                    drawingSettings.enableDynamicBatching = true;
                    drawingSettings.overrideMaterial = _renderMaterial;

                    passData.filteringSettings = new FilteringSettings(RenderQueueRange.opaque) { layerMask = passData.layerMask };
                    RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, passData.filteringSettings);
                    passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                    builder.UseRendererList(passData.rendererListHandle);

					_passData = passData;

                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData data, UnityEngine.Rendering.RenderGraphModule.UnsafeGraphContext context) => ExecutePass(data, context));
                }
			}
			#endif
        }

        private MKEdgeDetectionSelectiveMaskPass _mkEdgeDetectionSelectiveMaskPass;

        public override void Create()
        {
            _mkEdgeDetectionSelectiveMaskPass = new MKEdgeDetectionSelectiveMaskPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_mkEdgeDetectionSelectiveMaskPass == null)
                _mkEdgeDetectionSelectiveMaskPass = new MKEdgeDetectionSelectiveMaskPass();

            _mkEdgeDetectionSelectiveMaskPass.layerMask = layer;
            renderer.EnqueuePass(_mkEdgeDetectionSelectiveMaskPass);
        }
    }
}
#endif