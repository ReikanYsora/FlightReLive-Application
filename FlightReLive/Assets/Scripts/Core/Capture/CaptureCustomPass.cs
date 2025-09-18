using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace FlightReLive.Core.Capture
{
    /// <summary>
    /// Custom pass HDRP : copy current color buffer of the active camera to an external RenderTexture
    /// </summary>
    class CaptureCustomPass : CustomPass
    {
        public RenderTexture TargetTexture;

        protected override void Execute(CustomPassContext ctx)
        {
            if (TargetTexture == null)
                return;

            // Le buffer HDRP actif (après post-process si injection AfterPostProcess)
            RTHandle source = ctx.cameraColorBuffer;

            // Copie le buffer HDRP dans ton RenderTexture classique
            CoreUtils.SetRenderTarget(ctx.cmd, TargetTexture);
            ctx.cmd.Blit(source, new RenderTargetIdentifier(TargetTexture));
        }
    }
}
