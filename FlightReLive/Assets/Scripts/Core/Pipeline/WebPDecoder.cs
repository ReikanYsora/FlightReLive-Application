using System;
using unity.libwebp;
using unity.libwebp.Interop;
using UnityEngine;

namespace FlightReLive.Core.Pipeline.API
{
    public delegate void ScalingFunction(ref int width, ref int height);

    public enum Error
    {
        Success = 0,
        Failure = 1
    }

    public static class WebPDecoder
    {
        public static unsafe byte[] LoadRGBAFromWebP(byte[] lData, ref int lWidth, ref int lHeight, bool lMipmaps, out Error lError, ScalingFunction scalingFunction = null)
        {
            lError = Error.Failure;
            byte[] lRawData = null;
            int lLength = lData.Length;

            fixed (byte* lDataPtr = lData)
            {
                scalingFunction?.Invoke(ref lWidth, ref lHeight);

                int numBytesRequired = lWidth * lHeight * 4;
                if (lMipmaps)
                {
                    numBytesRequired = Mathf.CeilToInt((numBytesRequired * 4.0f) / 3.0f);
                }

                lRawData = new byte[numBytesRequired];
                fixed (byte* lRawDataPtr = lRawData)
                {
                    int lStride = 4 * lWidth;
                    byte* lTmpDataPtr = lRawDataPtr + (lHeight - 1) * lStride;

                    WebPDecoderConfig config = new WebPDecoderConfig();

                    if (NativeLibwebp.WebPInitDecoderConfig(&config) == 0)
                    {
                        throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
                    }

                    config.options.use_threads = 1;
                    if (scalingFunction != null)
                    {
                        config.options.use_scaling = 1;
                    }
                    config.options.scaled_width = lWidth;
                    config.options.scaled_height = lHeight;

                    VP8StatusCode result = NativeLibwebp.WebPGetFeatures(lDataPtr, (UIntPtr)lLength, &config.input);
                    if (result != VP8StatusCode.VP8_STATUS_OK)
                    {
                        throw new Exception($"Failed WebPGetFeatures with error {result}");
                    }

                    config.output.colorspace = WEBP_CSP_MODE.MODE_RGBA;
                    config.output.u.RGBA.rgba = lTmpDataPtr;
                    config.output.u.RGBA.stride = -lStride;
                    config.output.u.RGBA.size = (UIntPtr)(lHeight * lStride);
                    config.output.height = lHeight;
                    config.output.width = lWidth;
                    config.output.is_external_memory = 1;

                    result = NativeLibwebp.WebPDecode(lDataPtr, (UIntPtr)lLength, &config);
                    if (result != VP8StatusCode.VP8_STATUS_OK)
                    {
                        throw new Exception($"Failed WebPDecode with error {result}");
                    }
                }

                lError = Error.Success;
            }

            return lRawData;
        }
    }
}
