using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace FlightReLive.Core.Capture
{
    /// <summary>
    /// Custom pass HDRP : copy current color buffer of the active camera to an external RenderTexture
    /// </summary>
    public class CaptureCustomPass : CustomPass
    {
        #region ATTRIBUTES
        public RenderTexture TargetTexture;
        public RTHandle TargetHandle;
        #endregion

        #region METHODS
        protected override void Execute(CustomPassContext ctx)
        {
            if (TargetHandle == null || TargetHandle.rt == null || ctx.cameraColorBuffer == null)
            {
                return;
            }

            HDUtils.BlitCameraTexture(ctx.cmd, ctx.cameraColorBuffer, TargetHandle);
        }
        #endregion
    }
}
