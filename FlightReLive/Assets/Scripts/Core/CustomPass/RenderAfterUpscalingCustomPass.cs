#if HAS_HDRP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace FlightReLive.Core
{
    /// <summary>
    /// Custom pass that draws only a specific Layer after upscaling (FSR).
    /// Performs its own culling pass, so the Layer can be excluded from the main camera.
    /// </summary>
    [Serializable]
    public class RenderAfterUpscalingCustomPass : CustomPass
    {
        #region ATTRIBUTES (INSPECTOR)
        [Header("Filtering")]
        [Tooltip("All renderers on these layers will be drawn by this pass.")]
        public LayerMask targetLayers = 0;
        public Camera targetCamera;

        [Tooltip("If enabled, draw opaque objects (RenderQueue 0..2500).")]
        public bool drawOpaque = true;

        [Tooltip("If enabled, draw transparent objects (RenderQueue 2501..5000).")]
        public bool drawTransparent = true;

        [Header("Overrides")]
        public Material overrideMaterial;
        public int overrideMaterialPassIndex = 0;
        public SortingCriteria transparentSorting = SortingCriteria.CommonTransparent;
        [Header("Debug")]
        public bool colorOnly = true;
        #endregion

        #region PROPERTIES
        private ShaderTagId[] _shaderTags;
        #endregion

        #region UNITY METHODS
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _shaderTags = new ShaderTagId[]
            {
                new ShaderTagId("Forward"),
                new ShaderTagId("ForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward")
            };

            name = string.IsNullOrEmpty(name) ? "Render After Upscaling (Custom Layer)" : name;
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (ctx.hdCamera.camera != targetCamera)
            {
                return;
            }

            if (!drawOpaque && !drawTransparent)
            {
                return;
            }

            CommandBuffer cmd = ctx.cmd;

            //Set render target
            RTHandle colorBuffer = ctx.cameraColorBuffer;
            RTHandle depthBuffer = ctx.cameraDepthBuffer;
            CoreUtils.SetRenderTarget(cmd, colorBuffer, colorOnly ? (RTHandle)null : depthBuffer, ClearFlag.None);

            //Custom Culling
            ScriptableCullingParameters cullParams;
            if (!ctx.hdCamera.camera.TryGetCullingParameters(out cullParams))
            {
                return;
            }

            //Only cull the target layers
            cullParams.cullingMask = (uint)targetLayers.value;
            CullingResults customResults = ctx.renderContext.Cull(ref cullParams);

            //Draw opaque
            if (drawOpaque)
            {
                DrawLayer(ctx, customResults, RenderQueueRange.opaque, SortingCriteria.CommonOpaque);
            }

            //Draw transparent
            if (drawTransparent)
            {
                DrawLayer(ctx, customResults, RenderQueueRange.transparent, transparentSorting);
            }
        }

        protected override void Cleanup()
        {
            _shaderTags = null;
        }
        #endregion

        #region METHODS
        private void DrawLayer(CustomPassContext ctx, CullingResults cullingResults, RenderQueueRange queue, SortingCriteria sorting)
        {
            RendererListDesc desc = new RendererListDesc(_shaderTags, cullingResults, ctx.hdCamera.camera)
            {
                renderQueueRange = queue,
                sortingCriteria = sorting,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = Mathf.Max(0, overrideMaterialPassIndex),
                excludeObjectMotionVectors = false,
                layerMask = targetLayers
            };

            RendererList list = ctx.renderContext.CreateRendererList(desc);
            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, list);
        }
        #endregion
    }
}
#endif
