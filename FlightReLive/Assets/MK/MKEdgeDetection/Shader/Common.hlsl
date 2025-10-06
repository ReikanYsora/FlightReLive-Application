/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#ifndef MK_EDGE_DETECTION_POST_PROCESSING_COMMON
	#define MK_EDGE_DETECTION_POST_PROCESSING_COMMON

    /******************************************************************************************/
    /* Notes */
    /******************************************************************************************/
    // Notes:
    // Roberts Cross               - good performance, ok detection, also needs less fine tuning than sobel
    // Sobel 3x3                   - slowest, good detection
    // Sobel 1x3                   - good performance, ok dection
    // Scharr (both variations)    - similar to sobel 
    // Compass                     - worst performance, best detection
    // Prewitt                     - similar to sobel
    // Custom Roberts Cross 45     - same as roberts cross, results are different because of the rotation

    //Roberts: Performance > Precision
    //Sobel: Precision > Performance, |Gx| + |Gy| aproxximation could be used
    //Laplacian: not very user friendly, very situational, very noisy - skip
    //Gradient: very low quality. Works only in one direction well.
    //Half Cross: good compromise, reasonable samples

    //Selective workmode only uses medium precision to limit required texture samples (could be extended in the future)

    /******************************************************************************************/
    /* Helpers */
    /******************************************************************************************/
    #ifndef MK_HALF_MIN
        #define MK_HALF_MIN 6.10e-5
    #endif
    #ifndef MK_FLOAT_MIN
        #define MK_FLOAT_MIN 1.17549e-38
    #endif

    inline half ApplyAdjustments(half mask, half lowPass, half highPass)
    {
        return smoothstep(lowPass, highPass, mask);
        //return saturate(step(threshold, pow(mask, bias)) * intensity);
        //return saturate((pow(mask, bias) - threshold) * intensity);
        //return smoothstep(threshold - _ThresholdParams.w, bias + _ThresholdParams.w, mask) * intensity;
    }

    inline float2 MKPrepareUV(float2 uv, float2 texelOffset, float2 texelSize)
    {
        #ifdef MK_OPTIMIZE_UV
            float2 absTexelOffset = abs(texelOffset);
            return clamp(uv + texelOffset, float2(0, 0), float2(1, 1) - absTexelOffset);
            //return saturate(uv + texelOffset);
            //return texelOffset + clamp(uv + absTexelOffset, absTexelOffset, float2(1, 1) - absTexelOffset);
            //return lerp(saturate(uv + texelOffset), saturate(uv - texelOffset), uv);
        #else
            return uv + texelOffset;
        #endif
    }

    /*
    inline half MKViewLimit(half3 normal, float3 positionWorld)
    {
        return saturate(1 - (dot(normalize(positionWorld - _WorldSpaceCameraPos), normal) * 2 - 1) - 1);
    }
    */

    /******************************************************************************************/
    /* Sampling */
    /******************************************************************************************/
    struct MKEdgeDetectionDepthSampleData
    {
        float d0, d1, d2, d3, d4, d5, d6, d7, d8;
        float d0Linear01, d1Linear01, d2Linear01, d3Linear01, d4Linear01, d5Linear01, d6Linear01, d7Linear01, d8Linear01;
        float nearestLinear01;
    };
    struct MKEdgeDetectionSelectiveMaskData
    {
        float sm0, sm1, sm2, sm3, sm4, sm5, sm6, sm7, sm8;
        float sm0Linear, sm1Linear, sm2Linear, sm3Linear, sm4Linear, sm5Linear, sm6Linear, sm7Linear, sm8Linear;
        float nearestLinear;
    };
    struct MKEdgeDetectionSelectiveDepthData
    {
        float sd0, sd1, sd2, sd3, sd4, sd5, sd6, sd7, sd8;
        float sd0Linear, sd1Linear, sd2Linear, sd3Linear, sd4Linear, sd5Linear, sd6Linear, sd7Linear, sd8Linear;
        float nearestLinear;
    };
    struct MKEdgeDetectionNormalSampleData
    {
        half3 n0, n1, n2, n3, n4, n5, n6, n7, n8;
    };
    struct MKEdgeDetectionPositionSampleData
    {
        float3 w0, w1, w2, w3, w4, w5, w6, w7, w8;
    };

    inline void SampleSelectiveMaskAxis(const float2 uv, const float4 offset, out float left, out float right, out float bottom, out float top)
    {
        left = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(-offset.x, 0), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
        right = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(offset.z, 0), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
        bottom = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(0, -offset.y), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
        top = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(0, offset.w), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
    }

    inline void SampleSelectiveMaskDiagonal(const float2 uv, const float4 offset, out float topLeft, out float topRight, out float bottomLeft, out float bottomRight)
    {
        topLeft = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(-offset.x, offset.w), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
        topRight = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(offset.z, offset.w), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
        bottomLeft = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, -float2(offset.x, offset.y), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
        bottomRight= LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(offset.z, -offset.y), _MKEdgeDetectionSelectiveMask_TexelSize.xy), _MKShaderMainTexDimension).r;
    }

    inline void SampleDepthAxis(const float2 uv, const float4 offset, out float left, out float right, out float bottom, out float top)
    {
        left = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(-offset.x, 0), _CameraDepthTexture_TexelSize.xy));
        right = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(offset.z, 0), _CameraDepthTexture_TexelSize.xy));
        bottom = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(0, -offset.y), _CameraDepthTexture_TexelSize.xy));
        top = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(0, offset.w), _CameraDepthTexture_TexelSize.xy));
    }

    inline void SampleDepthDiagonal(const float2 uv, const float4 offset, out float topLeft, out float topRight, out float bottomLeft, out float bottomRight)
    {
        topLeft = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(-offset.x, offset.w), _CameraDepthNormalsTexture_TexelSize.xy));
        topRight = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(offset.z, offset.w), _CameraDepthNormalsTexture_TexelSize.xy));
        bottomLeft = ComputeSampleCameraDepth(MKPrepareUV(uv, -float2(offset.x, offset.y), _CameraDepthNormalsTexture_TexelSize.xy));
        bottomRight = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(offset.z, -offset.y), _CameraDepthNormalsTexture_TexelSize.xy));
    }

    inline void SampleNormalsAxis(const float2 uv, const float4 offset, out half3 left, out half3 right, out half3 bottom, out half3 top)
    {
        left = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(-offset.x, 0), _CameraDepthNormalsTexture_TexelSize.xy));
        right = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(offset.z, 0), _CameraDepthNormalsTexture_TexelSize.xy));
        bottom = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(0, -offset.y), _CameraDepthNormalsTexture_TexelSize.xy));
        top = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(0, offset.w), _CameraDepthNormalsTexture_TexelSize.xy));
    }

    inline void SampleNormalsDiagonal(float2 uv, const float4 offset, out half3 topLeft, out half3 topRight, out half3 bottomLeft, out half3 bottomRight)
    {
        topLeft = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(-offset.x, offset.w), _CameraDepthNormalsTexture_TexelSize.xy));
        topRight = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(offset.z, offset.w), _CameraDepthNormalsTexture_TexelSize.xy));
        bottomLeft = ComputeSampleCameraNormals(MKPrepareUV(uv, -float2(offset.x, offset.y), _CameraDepthNormalsTexture_TexelSize.xy));
        bottomRight = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(offset.z, -offset.y), _CameraDepthNormalsTexture_TexelSize.xy));
    }

    inline void SampleColorsAxis(const float2 uv, const float4 offset, out half3 left, out half3 right, out half3 bottom, out half3 top)
    {
        left = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(-offset.x, 0), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        right = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(offset.z, 0), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        bottom = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(0, -offset.y), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        top = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(0, offset.w), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
    }

    inline void SampleColorsDiagonal(const float2 uv, const float4 offset, out half3 topLeft, out half3 topRight, out half3 bottomLeft, out half3 bottomRight)
    {
        topLeft = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(-offset.x, offset.w), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        topRight = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(offset.z, offset.w), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        bottomLeft = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, -float2(offset.x, offset.y), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        bottomRight = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(offset.z, -offset.y), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
    }

    inline void ComputePositionWorldAxis(const float2 uv, const float4 offset, const float4x4 cameraInverseViewProjectionMatrix, in float leftInput, in float rightInput, in float bottomInput, in float topInput, out float3 left, out float3 right, out float3 bottom, out float3 top)
    {
        left = MKComputePositionWorld(MKPrepareUV(uv, float2(-offset.x, 0), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(leftInput), cameraInverseViewProjectionMatrix);
        right = MKComputePositionWorld(MKPrepareUV(uv, float2(offset.z, 0), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(rightInput), cameraInverseViewProjectionMatrix);
        bottom = MKComputePositionWorld(MKPrepareUV(uv, float2(0, -offset.y), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(bottomInput), cameraInverseViewProjectionMatrix);
        top = MKComputePositionWorld(MKPrepareUV(uv, float2(0, offset.w), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(topInput), cameraInverseViewProjectionMatrix);
    }

    inline void ComputePositionWorldDiagonal(const float2 uv, const float4 offset, const float4x4 cameraInverseViewProjectionMatrix, in float topLeftInput, in float topRightInput, in float bottomLeftInput, in float bottomRightInput, out float3 topLeft, out float3 topRight, out float3 bottomLeft, out float3 bottomRight)
    {
        topLeft = MKComputePositionWorld(MKPrepareUV(uv, float2(-offset.x, offset.w), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(topLeftInput), cameraInverseViewProjectionMatrix);
        topRight = MKComputePositionWorld(MKPrepareUV(uv, float2(offset.z, offset.w), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(topRightInput), cameraInverseViewProjectionMatrix);
        bottomLeft = MKComputePositionWorld(MKPrepareUV(uv, -float2(offset.x, offset.y), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(bottomLeftInput), cameraInverseViewProjectionMatrix);
        bottomRight = MKComputePositionWorld(MKPrepareUV(uv, float2(offset.z, -offset.y), _MKShaderMainTex_TexelSize.xy), AdjustRawDepthForPlatform(bottomRightInput), cameraInverseViewProjectionMatrix);
    }

    /******************************************************************************************/
    /* Medium */
    /******************************************************************************************/
    inline void SampleRobertsCrossDiagonalSelectiveMask(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionSelectiveMaskData selectiveMaskData)
    {
        float4 d0268;
        SampleSelectiveMaskDiagonal(uv, computedSampleSize, d0268.x, d0268.y, d0268.z, d0268.w);

        selectiveMaskData.sm0 = d0268.x;
        selectiveMaskData.sm2 = d0268.y;
        selectiveMaskData.sm6 = d0268.z;
        selectiveMaskData.sm8 = d0268.w;

        selectiveMaskData.sm0Linear = ComputeLinearDepth(d0268.x);
        selectiveMaskData.sm2Linear = ComputeLinearDepth(d0268.y);
        selectiveMaskData.sm6Linear = ComputeLinearDepth(d0268.z);
        selectiveMaskData.sm8Linear = ComputeLinearDepth(d0268.w);

        selectiveMaskData.nearestLinear = min(selectiveMaskData.sm0Linear, min(selectiveMaskData.sm2Linear, min(selectiveMaskData.sm6Linear, min(selectiveMaskData.sm8Linear, selectiveMaskData.sm4Linear))));
    }

    inline void SampleRobertsCrossAxisSelectiveMask(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionSelectiveMaskData selectiveMaskData)
    {
        float4 d3571;

        SampleSelectiveMaskAxis(uv, computedSampleSize, d3571.x, d3571.y, d3571.z, d3571.w);

        selectiveMaskData.sm3 = d3571.x;
        selectiveMaskData.sm5 = d3571.y;
        selectiveMaskData.sm7 = d3571.z;
        selectiveMaskData.sm1 = d3571.w;

        selectiveMaskData.sm3Linear = ComputeLinearDepth(d3571.x);
        selectiveMaskData.sm5Linear = ComputeLinearDepth(d3571.y);
        selectiveMaskData.sm7Linear = ComputeLinearDepth(d3571.z);
        selectiveMaskData.sm1Linear = ComputeLinearDepth(d3571.w);

        selectiveMaskData.nearestLinear = min(selectiveMaskData.sm3Linear, min(selectiveMaskData.sm5Linear, min(selectiveMaskData.sm7Linear, min(selectiveMaskData.sm1Linear, selectiveMaskData.sm4Linear))));
    }

    inline void SampleRobertsCrossDiagonalSelectiveDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionSelectiveDepthData selectiveDepthData)
    {
        float4 d0268;
        SampleDepthDiagonal(uv, computedSampleSize, d0268.x, d0268.y, d0268.z, d0268.w);

        selectiveDepthData.sd0 = d0268.x;
        selectiveDepthData.sd2 = d0268.y;
        selectiveDepthData.sd6 = d0268.z;
        selectiveDepthData.sd8 = d0268.w;

        selectiveDepthData.sd0Linear = ComputeLinearDepth(d0268.x);
        selectiveDepthData.sd2Linear = ComputeLinearDepth(d0268.y);
        selectiveDepthData.sd6Linear = ComputeLinearDepth(d0268.z);
        selectiveDepthData.sd8Linear = ComputeLinearDepth(d0268.w);

        selectiveDepthData.nearestLinear = min(selectiveDepthData.sd0Linear, min(selectiveDepthData.sd2Linear, min(selectiveDepthData.sd6Linear, min(selectiveDepthData.sd8Linear, selectiveDepthData.sd4Linear))));
    }

    inline void SampleRobertsCrossAxisSelectiveDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionSelectiveDepthData selectiveDepthData)
    {
        float4 d3571;

        SampleDepthAxis(uv, computedSampleSize, d3571.x, d3571.y, d3571.z, d3571.w);

        selectiveDepthData.sd3 = d3571.x;
        selectiveDepthData.sd5 = d3571.y;
        selectiveDepthData.sd7 = d3571.z;
        selectiveDepthData.sd1 = d3571.w;

        selectiveDepthData.sd3Linear = ComputeLinearDepth(d3571.x);
        selectiveDepthData.sd5Linear = ComputeLinearDepth(d3571.y);
        selectiveDepthData.sd7Linear = ComputeLinearDepth(d3571.z);
        selectiveDepthData.sd1Linear = ComputeLinearDepth(d3571.w);

        selectiveDepthData.nearestLinear = min(selectiveDepthData.sd3Linear, min(selectiveDepthData.sd5Linear, min(selectiveDepthData.sd7Linear, min(selectiveDepthData.sd1Linear, selectiveDepthData.sd4Linear))));
    }

    inline half SampleRobertsCrossDiagonalDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionDepthSampleData depthData)
    {
        float4 d0268;
        SampleDepthDiagonal(uv, computedSampleSize, d0268.x, d0268.y, d0268.z, d0268.w);

        depthData.d0 = d0268.x;
        depthData.d2 = d0268.y;
        depthData.d6 = d0268.z;
        depthData.d8 = d0268.w;

        depthData.d0Linear01 = ComputeLinear01Depth(d0268.x);
        depthData.d2Linear01 = ComputeLinear01Depth(d0268.y);
        depthData.d6Linear01 = ComputeLinear01Depth(d0268.z);
        depthData.d8Linear01 = ComputeLinear01Depth(d0268.w);

        d0268 = float4(depthData.d0Linear01, depthData.d2Linear01, depthData.d6Linear01, depthData.d8Linear01);

        depthData.nearestLinear01 = min(depthData.d0Linear01, min(depthData.d2Linear01, min(depthData.d6Linear01, min(depthData.d8Linear01, depthData.d4Linear01))));
        d0268 = lerp(d0268, depthData.nearestLinear01.xxxx, step(d0268, depthData.nearestLinear01.xxxx));

        const float depthLimiter = max(MK_HALF_MIN, depthData.nearestLinear01);
        d0268 /= depthLimiter;

        float sumX = dot(d0268.xw, _KernelX._m00_m22);
        float sumY = dot(d0268.yz, _KernelY._m02_m20);

        return sqrt(sumX * sumX + sumY * sumY);
    }

    inline half SampleRobertsCrossAxisDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionDepthSampleData depthData)
    {
        float4 d3571;

        SampleDepthAxis(uv, computedSampleSize, d3571.x, d3571.y, d3571.z, d3571.w);

        depthData.d3 = d3571.x;
        depthData.d5 = d3571.y;
        depthData.d7 = d3571.z;
        depthData.d1 = d3571.w;

        depthData.d3Linear01 = ComputeLinear01Depth(d3571.x);
        depthData.d5Linear01 = ComputeLinear01Depth(d3571.y);
        depthData.d7Linear01 = ComputeLinear01Depth(d3571.z);
        depthData.d1Linear01 = ComputeLinear01Depth(d3571.w);

        d3571 = float4(depthData.d3Linear01, depthData.d5Linear01, depthData.d7Linear01, depthData.d1Linear01);

        //improves steep angles
        depthData.nearestLinear01 = min(depthData.d3Linear01, min(depthData.d5Linear01, min(depthData.d7Linear01, min(depthData.d1Linear01, depthData.d4Linear01))));
        d3571 = lerp(d3571, depthData.nearestLinear01.xxxx, step(d3571, depthData.nearestLinear01.xxxx));

        //Comment out for only OUTER lines
        const float depthLimiter = max(MK_HALF_MIN, depthData.nearestLinear01);
        d3571 /= depthLimiter;

        float sumX = dot(d3571.xy, _KernelX._m10_m12);
        float sumY = dot(d3571.zw, _KernelY._m21_m01);

        return sqrt(sumX * sumX + sumY * sumY);
    }

    inline half SampleRobertsCrossDiagonalNormals(in float2 uv, in float sampleSize, inout MKEdgeDetectionNormalSampleData normalData)
    {
        half3 n0, n2, n6, n8;
        float4 computedSampleSize = ComputeSampleSize(sampleSize, _CameraDepthNormalsTexture_TexelSize.xy);

        SampleNormalsDiagonal(uv, computedSampleSize, n0, n2, n6, n8);

        normalData.n0 = n0;
        normalData.n2 = n2;
        normalData.n6 = n6;
        normalData.n8 = n8;

        half3 sumX = n0 * _KernelX._m00;
        sumX += n8 * _KernelX._m22;

        half3 sumY = n2 * _KernelY._m02;
        sumY += n6 * _KernelY._m20;

        return sqrt(dot(sumX, sumX) + dot(sumY, sumY));
    }

    inline half SampleRobertsCrossAxisNormals(in float2 uv, in float sampleSize, inout MKEdgeDetectionNormalSampleData normalData)
    {
        half3 n3, n5, n7, n1;
        float4 computedSampleSize = ComputeSampleSize(sampleSize, _CameraDepthNormalsTexture_TexelSize.xy);

        SampleNormalsAxis(uv, computedSampleSize, n3, n5, n7, n1);

        normalData.n3 = n3;
        normalData.n5 = n5;
        normalData.n7 = n7;
        normalData.n1 = n1;

        half3 sumX = n3 * _KernelX._m10;
        sumX += n5 * _KernelX._m12;

        half3 sumY = n7 * _KernelY._m21;
        sumY += n1 * _KernelY._m01;

        return sqrt(dot(sumX, sumX) + dot(sumY, sumY));
    }

    inline half SampleRobertsCrossDiagonalColors(in float2 uv, in float sampleSize)
    {
        float4 texelOffset = ComputeSampleSize(sampleSize, _MKShaderMainTex_TexelSize.xy);

        half3 c0, c2, c6, c8;
        SampleColorsDiagonal(uv, texelOffset, c0, c2, c6, c8);

        half3 sumX = c0 * _KernelX._m00;
        sumX += c8 * _KernelX._m22;

        half3 sumY = c2 * _KernelY._m02;
        sumY += c6 * _KernelY._m20;

        return sqrt(dot(sumX, sumX) + dot(sumY, sumY));
    }

    inline half SampleRobertsCrossAxisColors(in float2 uv, in float sampleSize)
    {
        float4 texelOffset = ComputeSampleSize(sampleSize, _MKShaderMainTex_TexelSize.xy);

        half3 c3, c5, c7, c1;
        SampleColorsAxis(uv, texelOffset, c3, c5, c7, c1);

        half3 sumX = c3 * _KernelX._m10;
        sumX += c5 * _KernelX._m12;

        half3 sumY = c7 * _KernelY._m21;
        sumY += c1 * _KernelY._m01;

        return sqrt(dot(sumX, sumX) + dot(sumY, sumY));
    }

    inline half ComputeRobertsCrossDiagonalCombinedInput(float2 uv, float4 computedSampleSize, const in MKEdgeDetectionDepthSampleData depthData, const in MKEdgeDetectionNormalSampleData normalData, inout MKEdgeDetectionPositionSampleData positionData, const float4x4 cameraInverseViewProjectionMatrix)
    {
        ComputePositionWorldDiagonal(uv, computedSampleSize, cameraInverseViewProjectionMatrix, depthData.d0, depthData.d2, depthData.d6, depthData.d8, positionData.w0, positionData.w2, positionData.w6, positionData.w8);

        float sum = dot(normalData.n4, positionData.w4 - positionData.w0);
        sum += dot(normalData.n4, positionData.w4 - positionData.w2);
        sum += dot(normalData.n4, positionData.w4 - positionData.w6);
        sum += dot(normalData.n4, positionData.w4 - positionData.w8);

        sum = saturate(sum);

        return sqrt(dot(sum, sum));
    }

    inline half ComputeRobertsCrossAxisCombinedInput(float2 uv, float4 computedSampleSize, const in MKEdgeDetectionDepthSampleData depthData, const in MKEdgeDetectionNormalSampleData normalData, inout MKEdgeDetectionPositionSampleData positionData, const float4x4 cameraInverseViewProjectionMatrix)
    {
        ComputePositionWorldAxis(uv, computedSampleSize, cameraInverseViewProjectionMatrix, depthData.d3, depthData.d5, depthData.d7, depthData.d1, positionData.w3, positionData.w5, positionData.w7, positionData.w1);

        float sum = dot(normalData.n4, positionData.w4 - positionData.w1);
        sum += dot(normalData.n4, positionData.w4 - positionData.w3);
        sum += dot(normalData.n4, positionData.w4 - positionData.w5);
        sum += dot(normalData.n4, positionData.w4 - positionData.w7);

        sum = saturate(sum);

        return sqrt(dot(sum, sum));
    }

    /******************************************************************************************/
    /* Low */
    /******************************************************************************************/
    inline half SampleHalfCrossHorizontalDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionDepthSampleData depthData)
    {
        float3 d468;
        
        d468.y = ComputeSampleCameraDepth(MKPrepareUV(uv, -computedSampleSize.xy, _CameraDepthTexture_TexelSize.xy));
        d468.z = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(computedSampleSize.z, -computedSampleSize.y), _CameraDepthTexture_TexelSize.xy));

        depthData.d6 = d468.y;
        depthData.d8 = d468.z;

        depthData.d6Linear01 = ComputeLinear01Depth(depthData.d6);
        depthData.d8Linear01 = ComputeLinear01Depth(depthData.d8);

        d468 = float3(depthData.d4Linear01, depthData.d6Linear01, depthData.d8Linear01);

        depthData.nearestLinear01 = min(depthData.d4Linear01, min(depthData.d6Linear01, depthData.d8Linear01));
        d468 = lerp(d468, depthData.nearestLinear01.xxx, step(d468, depthData.nearestLinear01.xxx));
        
        const float depthLimiter = max(MK_HALF_MIN, depthData.nearestLinear01);
        d468 /= depthLimiter;

        float sum = dot(d468, _KernelX._m11_m20_m22);
        
        return sum * sum;
    }

    inline half SampleHalfCrossVerticalDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionDepthSampleData depthData)
    {
        float3 d046;
        
        d046.x = ComputeSampleCameraDepth(MKPrepareUV(uv, float2(-computedSampleSize.x, computedSampleSize.w), _CameraDepthTexture_TexelSize.xy));
        d046.z = ComputeSampleCameraDepth(MKPrepareUV(uv, -computedSampleSize.xy, _CameraDepthTexture_TexelSize.xy));

        depthData.d0 = d046.x;
        depthData.d6 = d046.z;

        depthData.d0Linear01 = ComputeLinear01Depth(depthData.d0);
        depthData.d6Linear01 = ComputeLinear01Depth(depthData.d6);

        d046 = float3(depthData.d0Linear01, depthData.d4Linear01, depthData.d6Linear01);

        depthData.nearestLinear01 = min(depthData.d0Linear01, min(depthData.d4Linear01, depthData.d6Linear01));
        d046 = lerp(d046, depthData.nearestLinear01.xxx, step(d046, depthData.nearestLinear01.xxx));
        
        const float depthLimiter = max(MK_HALF_MIN, depthData.nearestLinear01);
        d046 /= depthLimiter;

        float sum = dot(d046, _KernelY._m00_m11_m20);

        return sum * sum;
    }

    inline half SampleHalfCrossHorizontalNormals(in float2 uv, in half3 cameraNormal, in float sampleSize, inout MKEdgeDetectionNormalSampleData normalData)
    {
        half3 n4, n6, n8;

        float4 offset = ComputeSampleSize(sampleSize, _CameraDepthNormalsTexture_TexelSize.xy);

        n4 = cameraNormal;
        n6 = ComputeSampleCameraNormals(MKPrepareUV(uv, -offset.x, _CameraDepthNormalsTexture_TexelSize.xy));
        n8 = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(offset.z, -offset.y), _CameraDepthNormalsTexture_TexelSize.xy));

        normalData.n6 = n6;
        normalData.n8 = n8;

        half3 sum = n4 * _KernelX._m11;
        sum += n6 * _KernelX._m20;
        sum += n8 * _KernelX._m22;

        return dot(sum, sum);
    }

    inline half SampleHalfCrossVerticalNormals(in float2 uv, in half3 cameraNormal, in float sampleSize, inout MKEdgeDetectionNormalSampleData normalData)
    {
        half3 n0, n4, n6;

        float4 offset = ComputeSampleSize(sampleSize, _CameraDepthNormalsTexture_TexelSize.xy);

        n4 = cameraNormal;
        n0 = ComputeSampleCameraNormals(MKPrepareUV(uv, float2(-offset.x, offset.w), _CameraDepthNormalsTexture_TexelSize.xy));
        n6 = ComputeSampleCameraNormals(MKPrepareUV(uv, -offset.xy, _CameraDepthNormalsTexture_TexelSize.xy));

        normalData.n0 = n0;
        normalData.n6 = n6;

        half3 sum = n0 * _KernelY._m00;
        sum += n4 * _KernelY._m11;
        sum += n6 * _KernelY._m20;

        return dot(sum, sum);
    }

    inline half SampleHalfCrossHorizontalColors(in float2 uv, in half3 mainSample, in float sampleSize)
    {
        half3 c4, c6, c8;

        float4 texelOffset = ComputeSampleSize(sampleSize, _MKShaderMainTex_TexelSize.xy);

        c4 = mainSample;
        c6 = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, -texelOffset.xy, _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        c8 = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(texelOffset.z, -texelOffset.y), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;

        half3 sum = c4 * _KernelX._m11;
        sum += c6 * _KernelX._m20;
        sum += c8 * _KernelX._m22;

        return dot(sum, sum);
    }

    inline half SampleHalfCrossVerticalColors(in float2 uv, in half3 mainSample, in float sampleSize)
    {
        half3 c0, c4, c6;

        float4 texelOffset = ComputeSampleSize(sampleSize, _MKShaderMainTex_TexelSize.xy);

        c4 = mainSample;
        c0 = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, float2(-texelOffset.x, texelOffset.w), _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;
        c6 = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), MKPrepareUV(uv, -texelOffset.xy, _MKShaderMainTex_TexelSize.xy), _MKShaderMainTexDimension).rgb;

        half3 sum = c0 * _KernelY._m00;
        sum += c4 * _KernelY._m11;
        sum += c6 * _KernelY._m20;

        return dot(sum, sum);
    }

    inline half ComputeHalfCrossHorizontalCombinedInput(float2 uv, float4 computedSampleSize, const in MKEdgeDetectionDepthSampleData depthData, const in MKEdgeDetectionNormalSampleData normalData, inout MKEdgeDetectionPositionSampleData positionData, const float4x4 cameraInverseViewProjectionMatrix)
    {
        positionData.w6 = MKComputePositionWorld(uv - computedSampleSize.xy, AdjustRawDepthForPlatform(depthData.d6), cameraInverseViewProjectionMatrix);
        positionData.w8 = MKComputePositionWorld(uv + float2(computedSampleSize.z, -computedSampleSize.y), AdjustRawDepthForPlatform(depthData.d8), cameraInverseViewProjectionMatrix);

        float sum = dot(normalData.n4, positionData.w4 - positionData.w6);
        sum += dot(normalData.n4, positionData.w4 - positionData.w8);

        sum = saturate(sum);

        return dot(sum, sum);
    }

    inline half ComputeHalfCrossVerticalCombinedInput(float2 uv, float4 computedSampleSize, const in MKEdgeDetectionDepthSampleData depthData, const in MKEdgeDetectionNormalSampleData normalData, inout MKEdgeDetectionPositionSampleData positionData, const float4x4 cameraInverseViewProjectionMatrix)
    {
        positionData.w0 = MKComputePositionWorld(uv + float2(-computedSampleSize.x, computedSampleSize.w), AdjustRawDepthForPlatform(depthData.d0), cameraInverseViewProjectionMatrix);
        positionData.w6 = MKComputePositionWorld(uv - computedSampleSize.xy, AdjustRawDepthForPlatform(depthData.d6), cameraInverseViewProjectionMatrix);

        float sum = dot(normalData.n4, positionData.w4 - positionData.w0);
        sum += dot(normalData.n4, positionData.w4 - positionData.w6);

        sum = saturate(sum);

        return dot(sum, sum);
    }

    /******************************************************************************************/
    /* High */
    /******************************************************************************************/
    inline half SampleSobelDepth(in float2 uv, in float4 computedSampleSize, inout MKEdgeDetectionDepthSampleData depthData)
    {
        float4 d0268, d1357;

        float2 centerUV = uv;

        SampleDepthDiagonal(uv, computedSampleSize, d0268.x, d0268.y, d0268.z, d0268.w);
        SampleDepthAxis(uv, computedSampleSize, d1357.y, d1357.z, d1357.w, d1357.x);

        depthData.d0 = d0268.x;
        depthData.d2 = d0268.y;
        depthData.d6 = d0268.z;
        depthData.d8 = d0268.w;

        depthData.d1 = d1357.x;
        depthData.d3 = d1357.y;
        depthData.d5 = d1357.z;
        depthData.d7 = d1357.w;

        depthData.d0Linear01 = ComputeLinear01Depth(d0268.x);
        depthData.d2Linear01 = ComputeLinear01Depth(d0268.y);
        depthData.d6Linear01 = ComputeLinear01Depth(d0268.z);
        depthData.d8Linear01 = ComputeLinear01Depth(d0268.w);

        depthData.d1Linear01 = ComputeLinear01Depth(d1357.x);
        depthData.d3Linear01 = ComputeLinear01Depth(d1357.y);
        depthData.d5Linear01 = ComputeLinear01Depth(d1357.z);
        depthData.d7Linear01 = ComputeLinear01Depth(d1357.w);

        depthData.nearestLinear01 = min(depthData.d0Linear01, min(depthData.d1Linear01, min(depthData.d2Linear01, min(depthData.d3Linear01, min(depthData.d4Linear01, min(depthData.d5Linear01, min(depthData.d6Linear01, min(depthData.d7Linear01, depthData.d8Linear01))))))));

        d1357 = float4(depthData.d1Linear01, depthData.d3Linear01, depthData.d5Linear01, depthData.d7Linear01);
        d0268 = float4(depthData.d0Linear01, depthData.d2Linear01, depthData.d6Linear01, depthData.d8Linear01);

        d0268 = lerp(d0268, depthData.nearestLinear01.xxxx, step(d0268, depthData.nearestLinear01.xxxx));
        d1357 = lerp(d1357, depthData.nearestLinear01.xxxx, step(d1357, depthData.nearestLinear01.xxxx));

        const float depthLimiter = max(MK_HALF_MIN, depthData.nearestLinear01);
        d0268 /= depthLimiter;
        d1357 /= depthLimiter;

        float sumX = dot(float4(d0268.x, d0268.y, d1357.y, d1357.z), float4(_KernelX._m00, _KernelX._m02, _KernelX._m10, _KernelX._m12)) + dot(float2(d0268.z, d0268.w), float2(_KernelX._m20, _KernelX._m22));
        float sumY = dot(float4(d0268.x, d1357.x, d0268.y, d0268.z), float4(_KernelY._m00, _KernelY._m01, _KernelY._m02, _KernelY._m20)) + dot(float2(d1357.w, d0268.w), float2(_KernelY._m21, _KernelY._m22));

        return sqrt(sumX * sumX + sumY * sumY);
    }

    inline half SampleSobelNormals(in float2 uv, in float sampleSize, inout MKEdgeDetectionNormalSampleData normalData)
    {
        half3 n0, n1, n2, n3, n5, n6, n7, n8;

        float4 texelOffset = ComputeSampleSize(sampleSize, _CameraDepthNormalsTexture_TexelSize.xy);

        SampleNormalsDiagonal(uv, texelOffset, n0, n2, n6, n8);
        SampleNormalsAxis(uv, texelOffset, n3, n5, n7, n1);

        normalData.n0 = n0;
        normalData.n2 = n2;
        normalData.n6 = n6;
        normalData.n8 = n8;

        normalData.n1 = n1;
        normalData.n3 = n3;
        normalData.n5 = n5;
        normalData.n7 = n7;

        half3 sumX = n0 * _KernelX._m00;
        sumX += n2 * _KernelX._m02;
        sumX += n3 * _KernelX._m10;
        sumX += n5 * _KernelX._m12;
        sumX += n6 * _KernelX._m20;
        sumX += n8 * _KernelX._m22;

        half3 sumY = n0 * _KernelY._m00;
        sumY += n1 * _KernelY._m01;
        sumY += n2 * _KernelY._m02;
        sumY += n6 * _KernelY._m20;
        sumY += n7 * _KernelY._m21;
        sumY += n8 * _KernelY._m22;

        return sqrt(dot(sumX, sumX) + dot(sumY, sumY));
    }
    inline half SampleSobelColors(in float2 uv, in float sampleSize)
    {
        float4 texelOffset = ComputeSampleSize(sampleSize, _MKShaderMainTex_TexelSize.xy);

        half3 c0, c1, c2, c3, c5, c6, c7, c8;

        SampleColorsDiagonal(uv, texelOffset, c0, c2, c6, c8);
        SampleColorsAxis(uv, texelOffset, c3, c5, c7, c1);

        half3 sumX = c0 * _KernelX._m00;
        sumX += c2 * _KernelX._m02;
        sumX += c3 * _KernelX._m10;
        sumX += c5 * _KernelX._m12;
        sumX += c6 * _KernelX._m20;
        sumX += c8 * _KernelX._m22;

        half3 sumY = c0 * _KernelY._m00;
        sumY += c1 * _KernelY._m01;
        sumY += c2 * _KernelY._m02;
        sumY += c6 * _KernelY._m20;
        sumY += c7 * _KernelY._m21;
        sumY += c8 * _KernelY._m22;

        return sqrt(dot(sumX, sumX) + dot(sumY, sumY));
    }

    inline half ComputeSobelCombinedInput(float2 uv, float4 computedSampleSize, const in MKEdgeDetectionDepthSampleData depthData, const in MKEdgeDetectionNormalSampleData normalData, inout MKEdgeDetectionPositionSampleData positionData, const float4x4 cameraInverseViewProjectionMatrix)
    {
        ComputePositionWorldDiagonal(uv, computedSampleSize, cameraInverseViewProjectionMatrix, depthData.d0, depthData.d2, depthData.d6, depthData.d8, positionData.w0, positionData.w2, positionData.w6, positionData.w8);
        ComputePositionWorldAxis(uv, computedSampleSize, cameraInverseViewProjectionMatrix, depthData.d3, depthData.d5, depthData.d7, depthData.d1, positionData.w3, positionData.w5, positionData.w7, positionData.w1);

        float sum = dot(normalData.n4, positionData.w4 - positionData.w0);
        sum += dot(normalData.n4, positionData.w4 - positionData.w1);
        sum += dot(normalData.n4, positionData.w4 - positionData.w2);
        sum += dot(normalData.n4, positionData.w4 - positionData.w3);

        sum += dot(normalData.n4, positionData.w4 - positionData.w5);
        sum += dot(normalData.n4, positionData.w4 - positionData.w6);
        sum += dot(normalData.n4, positionData.w4 - positionData.w7);
        sum += dot(normalData.n4, positionData.w4 - positionData.w8);

        sum = saturate(sum);

       return sqrt(dot(sum, sum));
    }

    /******************************************************************************************/
    /* Detection Wrapper */
    /******************************************************************************************/
    inline void ComputeSelectiveMask(float2 uv, float rawSelectiveMask, float linearSelectiveMask, float4 computedSampleSize, out MKEdgeDetectionSelectiveMaskData selectiveMaskData)
    {
        INITIALIZE_STRUCT(MKEdgeDetectionSelectiveMaskData, selectiveMaskData);
        selectiveMaskData.sm0 = selectiveMaskData.sm1 = selectiveMaskData.sm2 = selectiveMaskData.sm3 = selectiveMaskData.sm4 = selectiveMaskData.sm5 = selectiveMaskData.sm6 = selectiveMaskData.sm7 = selectiveMaskData.sm8 = 0;
        selectiveMaskData.sm0Linear = selectiveMaskData.sm1Linear = selectiveMaskData.sm2Linear = selectiveMaskData.sm3Linear = selectiveMaskData.sm4Linear = selectiveMaskData.sm5Linear = selectiveMaskData.sm6Linear = selectiveMaskData.sm7Linear = selectiveMaskData.sm8Linear = 0;
        
        selectiveMaskData.sm4 = rawSelectiveMask;
        selectiveMaskData.sm4Linear = linearSelectiveMask;

        #ifdef MK_UTILIZE_AXIS_ONLY
            SampleRobertsCrossAxisSelectiveMask(uv, computedSampleSize, selectiveMaskData);
        #else
            SampleRobertsCrossDiagonalSelectiveMask(uv, computedSampleSize, selectiveMaskData);
        #endif
    }

    inline MKEdgeDetectionSelectiveDepthData ComputeSelectiveDepth(float2 uv, float linearDepth, float4 computedSampleSize)
    {   
        MKEdgeDetectionSelectiveDepthData selectiveDepthData;
        INITIALIZE_STRUCT(MKEdgeDetectionSelectiveDepthData, selectiveDepthData);
        selectiveDepthData.sd0 = selectiveDepthData.sd1 = selectiveDepthData.sd2 = selectiveDepthData.sd3 = selectiveDepthData.sd4 = selectiveDepthData.sd5 = selectiveDepthData.sd6 = selectiveDepthData.sd7 = selectiveDepthData.sd8 = 0;
        selectiveDepthData.sd0Linear = selectiveDepthData.sd1Linear = selectiveDepthData.sd2Linear = selectiveDepthData.sd3Linear = selectiveDepthData.sd4Linear = selectiveDepthData.sd5Linear = selectiveDepthData.sd6Linear = selectiveDepthData.sd7Linear = selectiveDepthData.sd8Linear = 0;
        
        selectiveDepthData.sd4Linear = linearDepth;

        //Force Medium only precision
        #ifdef MK_UTILIZE_AXIS_ONLY
            SampleRobertsCrossAxisSelectiveDepth(uv, computedSampleSize, selectiveDepthData);
        #else
            SampleRobertsCrossDiagonalSelectiveDepth(uv, computedSampleSize, selectiveDepthData);
        #endif

        return selectiveDepthData;
    }

    inline float ComputeDepthEdges(float2 uv, float rawDepth, float linear01Depth, float4 computedSampleSize, out MKEdgeDetectionDepthSampleData depthData)
    {   
        INITIALIZE_STRUCT(MKEdgeDetectionDepthSampleData, depthData);
        depthData.d0 = depthData.d1 = depthData.d2 = depthData.d3 = depthData.d4 = depthData.d5 = depthData.d6 = depthData.d7 = depthData.d8 = 0;
        depthData.d0Linear01 = depthData.d1Linear01 = depthData.d2Linear01 = depthData.d3Linear01 = depthData.d4Linear01 = depthData.d5Linear01 = depthData.d6Linear01 = depthData.d7Linear01 = depthData.d8Linear01 = 0;
        
        depthData.d4 = rawDepth;
        depthData.d4Linear01 = linear01Depth;

        #if defined(MK_PRECISION_MEDIUM)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return SampleRobertsCrossAxisDepth(uv, computedSampleSize, depthData);
            #else
                return SampleRobertsCrossDiagonalDepth(uv, computedSampleSize, depthData);
            #endif
        #elif defined(MK_PRECISION_LOW)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return SampleHalfCrossHorizontalDepth(uv, computedSampleSize, depthData);
            #else
                return SampleHalfCrossVerticalDepth(uv, computedSampleSize, depthData);
            #endif
        #else //Sobel
            return SampleSobelDepth(uv, computedSampleSize, depthData);
        #endif
    }

    inline half ComputeNormalEdges(float2 uv, float sampleSize, out MKEdgeDetectionNormalSampleData normalData)
    {
        INITIALIZE_STRUCT(MKEdgeDetectionNormalSampleData, normalData);
        normalData.n0 = normalData.n1 = normalData.n2 = normalData.n3 = normalData.n4 = normalData.n5 = normalData.n6 = normalData.n7 = normalData.n8 = 0;
        
        #if defined(MK_EVALUATE_COMBINED_INPUT) || defined(MK_PRECISION_LOW)
            normalData.n4 = ComputeSampleCameraNormals(uv);
        #endif
        
        #if defined(MK_PRECISION_MEDIUM)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return SampleRobertsCrossAxisNormals(uv, sampleSize, normalData);
            #else
                return SampleRobertsCrossDiagonalNormals(uv, sampleSize, normalData);
            #endif
        #elif defined(MK_PRECISION_LOW)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return SampleHalfCrossHorizontalNormals(uv, normalData.n4, sampleSize, normalData);
            #else
                return SampleHalfCrossVerticalNormals(uv, normalData.n4, sampleSize, normalData);
            #endif
        #else
            return SampleSobelNormals(uv, sampleSize, normalData);
        #endif
    }

    inline half ComputeColorEdges(float2 uv, half3 mainSample, float sampleSize)
    {
        #if defined(MK_PRECISION_MEDIUM)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return SampleRobertsCrossAxisColors(uv, sampleSize);
            #else
                return SampleRobertsCrossDiagonalColors(uv, sampleSize);
            #endif
        #elif defined(MK_PRECISION_LOW)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return SampleHalfCrossHorizontalColors(uv, mainSample, sampleSize);
            #else
                return SampleHalfCrossVerticalColors(uv, mainSample, sampleSize);
            #endif
        #else
            return SampleSobelColors(uv, sampleSize);
        #endif
    }

    inline half ComputeCombinedEdges(float2 uv, float4 computedSampleSize, const in MKEdgeDetectionDepthSampleData depthData, const in MKEdgeDetectionNormalSampleData normalData, out MKEdgeDetectionPositionSampleData positionData)
    {   
        INITIALIZE_STRUCT(MKEdgeDetectionPositionSampleData, positionData);
        positionData.w0 = positionData.w1 = positionData.w2 = positionData.w3 = positionData.w4 = positionData.w5 = positionData.w6 = positionData.w7 = positionData.w8 = 0;

        #ifdef MK_RENDER_PIPELINE_BUILT_IN
            const float4x4 cameraInverseViewProjectionMatrix = _MKInverseViewProjectionMatrix;
        #else
            const float4x4 cameraInverseViewProjectionMatrix = _MKInverseViewProjectionMatrix;
            //const float4x4 cameraInverseViewProjectionMatrix = UNITY_MATRIX_I_VP;
        #endif

        positionData.w4 = MKComputePositionWorld(uv, AdjustRawDepthForPlatform(depthData.d4), cameraInverseViewProjectionMatrix);

        #if defined(MK_PRECISION_MEDIUM)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return ComputeRobertsCrossAxisCombinedInput(uv, computedSampleSize, depthData, normalData, positionData, cameraInverseViewProjectionMatrix);
            #else
                return ComputeRobertsCrossDiagonalCombinedInput(uv, computedSampleSize, depthData, normalData, positionData, cameraInverseViewProjectionMatrix);;
            #endif
        #elif defined(MK_PRECISION_LOW)
            #ifdef MK_UTILIZE_AXIS_ONLY
                return ComputeHalfCrossHorizontalCombinedInput(uv, computedSampleSize, depthData, normalData, positionData, cameraInverseViewProjectionMatrix);
            #else
                return ComputeHalfCrossVerticalCombinedInput(uv, computedSampleSize, depthData, normalData, positionData, cameraInverseViewProjectionMatrix);
            #endif
        #else
            return ComputeSobelCombinedInput(uv, computedSampleSize, depthData, normalData, positionData, cameraInverseViewProjectionMatrix);
        #endif
    }
#endif