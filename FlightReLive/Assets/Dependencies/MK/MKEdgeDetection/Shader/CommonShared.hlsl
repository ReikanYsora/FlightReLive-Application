/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#ifndef MK_EDGE_DETECTION_POST_PROCESSING_COMMON_SHARED
	#define MK_EDGE_DETECTION_POST_PROCESSING_COMMON_SHARED

    inline float4 ComputeSampleSize(half size, float2 texelSize)
    {
        #ifdef MK_PIXEL_PERFECTION
            half halfSize = size * 0.5h;
            return float4(floor(halfSize) * texelSize.xy, ceil(halfSize) * texelSize.xy);
        #else
            return size * texelSize.xyxy;
        #endif
    }

    inline half ComputeDistanceScale(float linearDepth, float nearFade, float farFade, half maxNear, half minFar)
    {
        return clamp(saturate(1.0 - lerp(0.0, 1.0, ((linearDepth - nearFade) / (farFade - nearFade)))), maxNear, minFar);
    }
    
    inline float ComputeEyeDepthFromLinear01Depth(float linear01Depth)
    {
        return (RcpHP(linear01Depth) - _ZBufferParams.y) / _ZBufferParams.x;
    }

    inline float AdjustRawDepthForPlatform(float rawDepth)
    {
        #if UNITY_REVERSED_Z
            return rawDepth;
        #else
            return lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
        #endif
    }

    inline float4 MKComputePositionClip(float2 ndc, float rawDepth)
    {
        float4 positionClip = float4(ndc * 2.0 - 1.0, rawDepth, 1.0);

        #if UNITY_UV_STARTS_AT_TOP
            positionClip.y = -positionClip.y;
        #endif

        return positionClip;
    }

    inline float3 MKComputePositionWorld(float2 ndc, float rawDepth, float4x4 invViewProjMatrix)
    {
        float4 positionClip = MKComputePositionClip(ndc, rawDepth);
        float4 positionWorld = mul(invViewProjMatrix, positionClip);
        return positionWorld.xyz / positionWorld.w;
    }

#endif