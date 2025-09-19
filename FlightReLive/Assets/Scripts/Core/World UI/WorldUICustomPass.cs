using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace FlightReLive.Core.WorldUI
{
    [ExecuteAlways]
    public class WorldUICustomPass : CustomPass
    {
        #region ATTRIBUTES
        public LayerMask uiLayer = 1 << 8;
        #endregion

        #region METHODS
        protected override void Execute(CustomPassContext ctx)
        {
            Camera camera = ctx.hdCamera.camera;

            // Crée la description du renderer list
            RendererListDesc rendererListDesc = new RendererListDesc(new ShaderTagId[]
            {
                new ShaderTagId("Forward"),
                new ShaderTagId("SRPDefaultUnlit")
            }, ctx.cullingResults, camera);

            rendererListDesc.sortingCriteria = SortingCriteria.CommonTransparent;
            rendererListDesc.layerMask = uiLayer;
            rendererListDesc.renderQueueRange = RenderQueueRange.transparent;

            RendererList rendererList = ctx.renderContext.CreateRendererList(rendererListDesc);
            ctx.cmd.DrawRendererList(rendererList);
        }
        #endregion
    }
}
