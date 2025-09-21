using FidelityFX.FSR3;
using TND.Upscaling.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace TND.Upscaling.FSR3
{
    public class FSR3UpscalerPlugin: UpscalerPlugin<FSR3Upscaler, FSR3UpscalerSettings>
    {
        public override UpscalerName Name => UpscalerName.FSR3;
        public override string DisplayName => FSR3Upscaler.DisplayName;
        public override int Priority => (int)UpscalerName.FSR3 + 31;
        public override bool IsSupported => SystemInfo.supportsComputeShaders;
        public override bool IsTemporalUpscaler => true;
        public override bool SupportsDynamicResolution => true;
        public override bool AcceptsReactiveMask => true;

        private Fsr3UpscalerAssets _assets;
        
        protected override bool TryCreateUpscaler(CommandBuffer commandBuffer, FSR3UpscalerSettings settings, in UpscalerInitParams initParams, out FSR3Upscaler upscaler)
        {
            if (_assets == null)
            {
                _assets = Resources.Load<Fsr3UpscalerAssets>("FSR3 Upscaler Assets");
                if (_assets == null)
                {
                    upscaler = null;
                    return false;
                }
            }

            upscaler = new FSR3Upscaler(settings, _assets);
            return upscaler.Initialize(commandBuffer, initParams);
        }

        public override void Cleanup()
        {
            if (_assets != null)
            {
                Resources.UnloadAsset(_assets);
                _assets = null;
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void RegisterUpscalerPlugin()
        {
            RegisterUpscalerPlugin(new FSR3UpscalerPlugin());
        }
    }
}
