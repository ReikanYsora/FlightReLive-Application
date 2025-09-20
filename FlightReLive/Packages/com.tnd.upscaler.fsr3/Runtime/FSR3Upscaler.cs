using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using FidelityFX;
using FidelityFX.FSR3;
using TND.Upscaling.Framework;

namespace TND.Upscaling.FSR3
{
    public class FSR3Upscaler: UpscalerBase<FSR3UpscalerSettings>
    {
        public static readonly string DisplayName = "FSR 3.1";
        
        private readonly Fsr3UpscalerAssets _assets;
        private readonly Fsr3Upscaler.DispatchDescription _dispatchDescription = new();
        
        private Fsr3UpscalerContext _context;
        private UpscalerInitParams _initParams;

        private Texture _defaultReactive;

        public FSR3Upscaler(FSR3UpscalerSettings settings, Fsr3UpscalerAssets assets)
            : base(settings)
        {
            _assets = assets;
        }
        
        public override bool Initialize(CommandBuffer commandBuffer, in UpscalerInitParams initParams)
        {
            if (_assets?.shaders == null)
                return false;

            _initParams = initParams;
            
            Fsr3Upscaler.InitializationFlags flags = Fsr3Upscaler.InitializationFlags.EnableDynamicResolution;
            if (initParams.enableHDR) flags |= Fsr3Upscaler.InitializationFlags.EnableHighDynamicRange;
            if (initParams.invertedDepth) flags |= Fsr3Upscaler.InitializationFlags.EnableDepthInverted;
            if (initParams.highResMotionVectors) flags |= Fsr3Upscaler.InitializationFlags.EnableDisplayResolutionMotionVectors;
            if (initParams.jitteredMotionVectors) flags |= Fsr3Upscaler.InitializationFlags.EnableMotionVectorsJitterCancellation;

            Fsr3Upscaler.ContextDescription contextDescription = new() 
            {
                Flags = flags,
                MaxUpscaleSize = initParams.upscaleSize,
                MaxRenderSize = initParams.maxRenderSize,
                Shaders = _assets.shaders,
            };
            
            _context = new Fsr3UpscalerContext();
            _context.Create(contextDescription);

            _defaultReactive = _initParams.CreateMatchingLookupTexture("Default Reactive", GraphicsFormat.R8_UNorm, Color.clear);
            return true;
        }

        public override void Destroy(CommandBuffer commandBuffer)
        {
            UpscalerUtils.DestroyTexture(ref _defaultReactive);
            
            _context.Destroy();
            base.Destroy(commandBuffer);
        }

        public override void Dispatch(CommandBuffer commandBuffer, in UpscalerDispatchParams dispatchParams)
        {
            _dispatchDescription.Color = ToResourceView(dispatchParams.inputColor);
            _dispatchDescription.Depth = ToResourceView(dispatchParams.inputDepth);
            _dispatchDescription.MotionVectors = ToResourceView(dispatchParams.inputMotionVectors);
            _dispatchDescription.Exposure = ToResourceView(dispatchParams.inputExposure);
            _dispatchDescription.Reactive = dispatchParams.inputReactiveMask.IsValid ? ToResourceView(dispatchParams.inputReactiveMask) : ToResourceView(_defaultReactive);
            _dispatchDescription.TransparencyAndComposition = Settings.transparencyAndCompositionMask != null ? ToResourceView(Settings.transparencyAndCompositionMask) : ToResourceView(_defaultReactive);
            _dispatchDescription.Output = ToResourceView(dispatchParams.outputColor);
            _dispatchDescription.JitterOffset = dispatchParams.jitterOffset;
            _dispatchDescription.MotionVectorScale = dispatchParams.motionVectorScale;
            _dispatchDescription.RenderSize = dispatchParams.renderSize;
            _dispatchDescription.UpscaleSize = _initParams.upscaleSize;
            _dispatchDescription.EnableSharpening = dispatchParams.enableSharpening;
            _dispatchDescription.Sharpness = dispatchParams.sharpness;
            _dispatchDescription.FrameTimeDelta = Time.unscaledDeltaTime;
            _dispatchDescription.PreExposure = dispatchParams.preExposure;
            _dispatchDescription.Reset = dispatchParams.resetHistory;
            _dispatchDescription.ViewSpaceToMetersFactor = 1.0f;    // 1 unit == 1 meter in Unity
            _dispatchDescription.VelocityFactor = Settings.advancedParameters.velocityFactor;
            _dispatchDescription.ReactivenessScale = Settings.advancedParameters.reactivenessScale;
            _dispatchDescription.ShadingChangeScale = Settings.advancedParameters.shadingChangeScale;
            _dispatchDescription.AccumulationAddedPerFrame = Settings.advancedParameters.accumulationAddedPerFrame;
            _dispatchDescription.MinDisocclusionAccumulation = Settings.advancedParameters.minDisocclusionAccumulation;
            _dispatchDescription.Flags = Settings.enableDebugView ? Fsr3Upscaler.DispatchFlags.DrawDebugView : 0;
            _dispatchDescription.UseTextureArrays = _initParams.useTextureArrays;
            _dispatchDescription.IsSrgbInput = UpscalerUtils.IsSRGBFormat(dispatchParams.inputColor.GraphicsFormat);

            Camera camera = _initParams.camera;
            if (camera != null)
            {
                _dispatchDescription.CameraNear = camera.nearClipPlane;
                _dispatchDescription.CameraFar = camera.farClipPlane;
                _dispatchDescription.CameraFovAngleVertical = camera.fieldOfView * Mathf.Deg2Rad;
            }
            else
            {
                // If we're missing camera information, use the Unity default values instead
                _dispatchDescription.CameraNear = 0.3f;
                _dispatchDescription.CameraFar = 1000f;
                _dispatchDescription.CameraFovAngleVertical = 60f * Mathf.Deg2Rad;
            }

            if (_initParams.invertedDepth)
            {
                // Swap the near and far clip plane distances as FSR3 Upscaler expects this when using inverted depth
                (_dispatchDescription.CameraNear, _dispatchDescription.CameraFar) = (_dispatchDescription.CameraFar, _dispatchDescription.CameraNear);
            }

            commandBuffer.BeginSample(DisplayName);
            _context.Dispatch(_dispatchDescription, commandBuffer);
            commandBuffer.EndSample(DisplayName);
        }
        
        private static ResourceView ToResourceView(in TextureRef textureRef)
        {
            return textureRef.IsValid ? new ResourceView(textureRef.GetRenderTargetIdentifier(), textureRef.GetRenderTextureSubElement()) : ResourceView.Unassigned;
        }
        
        private static ResourceView ToResourceView(Texture texture)
        {
            return texture != null ? new ResourceView(texture) : ResourceView.Unassigned;
        }
    }
}
