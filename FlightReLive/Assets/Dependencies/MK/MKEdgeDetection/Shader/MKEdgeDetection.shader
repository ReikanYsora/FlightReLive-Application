/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de

ASSET STORE TERMS OF SERVICE AND EULA:
https://unity.com/de/legal/as-terms
*****************************************************/

/* File created using: */
/* MK Shader - Cross Compiling Shaders */
/* Version: 1.1.63  */
/* Exported on: 28.05.2025 23:05:45 */

Shader "Hidden/MK/Edge Detection/Main"
{
	/******************************************************************************************/
	/* Properties */
	/******************************************************************************************/
	Properties
	{
		/* <-----| User Properties |-----> */
		//Disabled
		
		/* <-----| System Properties |-----> */
		
	}
	
	/******************************************************************************************/
	/* HLSL Includes */
	/******************************************************************************************/
	HLSLINCLUDE
		/* <-----| System Defines |-----> */
		#ifndef MK_SHADER_TYPE_POST_PROCESSING
			#define MK_SHADER_TYPE_POST_PROCESSING
		#endif
		
		/* <-----| System HLSL Includes |-----> */
		#ifndef K_SPEC_DIELECTRIC_MIN
			#define K_SPEC_DIELECTRIC_MIN 0.04
		#endif
		#ifndef K_SPEC_DIELECTRIC_MAX
			#define K_SPEC_DIELECTRIC_MAX 0.96
		#endif
		#ifndef MK_HALF_MIN
			#define MK_HALF_MIN 6.10e-5
		#endif
		#ifndef ONE_MINUS_HALF_MIN
			#define ONE_MINUS_HALF_MIN 0.999939
		#endif
		#ifndef MK_PI
			#define MK_PI 3.141592
		#endif
		#ifndef TAU
			#define TAU 6.283185
		#endif
		#ifndef PI_H
			#define PI_H 1.570796
		#endif
		#ifndef MK_INV_PI
			#define MK_INV_PI 0.318309
		#endif
		#ifndef REL_LUMA
			#define REL_LUMA half3(0.2126,0.7152,0.0722)
		#endif
		#ifndef HALF2_ONE
			#define HALF2_ONE half2(1.0, 1.0)
		#endif
		#ifndef HALF3_ONE
			#define HALF3_ONE half3(1.0, 1.0, 1.0)
		#endif
		#ifndef HALF4_ONE
			#define HALF4_ONE half4(1.0, 1.0, 1.0, 1.0)
		#endif
		#ifndef HALF2_ZERO
			#define HALF2_ZERO half2(0.0, 0.0)
		#endif
		#ifndef HALF3_ZERO
			#define HALF3_ZERO half3(0.0, 0.0, 0.0)
		#endif
		#ifndef HALF4_ZERO
			#define HALF4_ZERO half4(0.0, 0.0, 0.0, 0.0)
		#endif
		#ifndef MK_REFERENCE_RESOLUTION
			#define MK_REFERENCE_RESOLUTION half2(3840, 2160)
		#endif
		#ifndef MK_REFERENCE_ASPECT
			#define MK_REFERENCE_ASPECT half2(1.777778, 0.5625)
		#endif
		inline half FastPow2(half v)
		{
			return v * v;
		}
		inline half FastPow3(half v)
		{
			return v * v * v;
		}
		inline half FastPow4(half v)
		{
			return v * v * v * v;
		}
		inline half FastPow5(half v)
		{
			return v * v * v * v * v;
		}
		inline float FastPow2HP(float v)
		{
			return v * v;
		}
		inline float FastPow3HP(float v)
		{
			return v * v * v;
		}
		inline float FastPow4HP(float v)
		{
			return v * v * v * v;
		}
		inline float FastPow5HP(float v)
		{
			return v * v * v * v * v;
		}
		inline half3 ComputeRGBToHSV(half3 c)
		{
			const half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
			half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
			half d = q.x - min(q.w, q.y);
			const half e = 1.0e-4;
			return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}
		inline half3 ComputeHSVToRGB(half3 c)
		{
			const half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
			return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
		}
		inline void Clip0(in half v)
		{
			clip(v + MK_HALF_MIN);
		}
		#define TRANSFER_SCALAR_TO_VECTOR(value) value.rgb = value.a;
		
		/* <-----| User Global HLSL |-----> */
		//Disabled
	ENDHLSL
	
	
	/******************************************************************************************/
	/* Render Pipeline: Universal */
	/******************************************************************************************/
	/******************************************************************************************/
	/* Sub Shader Target 4.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			PackageRequirements
			{
				"com.unity.render-pipelines.universal":"[12.0,99.99]"
			}
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 4.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma exclude_renderers gles d3d9 d3d11_9x psp2 n3ds wiiu
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_UNIVERSAL
				#define MK_RENDER_PIPELINE_UNIVERSAL
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			//Disabled
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			/* <-----| Includes |-----> */
			#ifdef MK_UNITY_2023_2_OR_NEWER
				#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			
			#ifdef UNITY_DOTS_INSTANCING_ENABLED
				UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
					/* <-----| Custom Code Dots Instancing Properties Float4 |-----> */
					
					//Disabled
				UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
				/* <-----| Custom Code Dots Instancing Declare Cached Properties |-----> */
				
				//Disabled
				void MKSetupDOTSMaterialPropertyCaches()
				{
					/* <-----| Custom Code Dots Instancing Assign Cached Properties |-----> */
					
					//Disabled
				}
				/* <-----| Custom Code Dots Instancing Re-Assign Cached Properties |-----> */
				
				//Disabled
			#endif
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 unpackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						unpackedUV = RemapFoveatedRenderingLinearToNonUniform(uv);
					}
					return unpackedUV;
				#else
					return uv;
				#endif
			}
			inline float2 MKRepackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 repackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						repackedUV = RemapFoveatedRenderingNonUniformToLinear(uv);
					}
					return repackedUV;
				#else
					return uv;
				#endif
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(_CameraDepthTexture)
			inline float ComputeSampleCameraDepth(float2 uv)
			{
				return SampleTex2DHPAuto(PASS_TEXTURE_2D(_CameraDepthTexture, MK_SAMPLER_LINEAR_CLAMP), uv).r;
			}
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(_CameraNormalsTexture)
			uniform float4 _CameraNormalsTexture_TexelSize;
			#ifndef _CameraDepthNormalsTexture_TexelSize
				#define _CameraDepthNormalsTexture_TexelSize _CameraNormalsTexture_TexelSize
			#endif
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				float3 normal = SampleTex2DAuto(PASS_TEXTURE_2D(_CameraNormalsTexture, MK_SAMPLER_LINEAR_CLAMP), uv).rgb;
				#if defined(_GBUFFER_NORMALS_OCT)
					float2 remappedOctNormalWS = Unpack888ToFloat2(normal);
					float2 octNormalWS = remappedOctNormalWS.xy * 2.0 - 1.0;
					normal = UnpackNormalOctQuadEncode(octNormalWS);
				#endif
				return normal;
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	/******************************************************************************************/
	/* Sub Shader Target 3.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			PackageRequirements
			{
				"com.unity.render-pipelines.universal":"[12.0,99.99]"
			}
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 3.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma only_renderers glcore gles3
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_UNIVERSAL
				#define MK_RENDER_PIPELINE_UNIVERSAL
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			//Disabled
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			/* <-----| Includes |-----> */
			#ifdef MK_UNITY_2023_2_OR_NEWER
				#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			
			#ifdef UNITY_DOTS_INSTANCING_ENABLED
				UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
					/* <-----| Custom Code Dots Instancing Properties Float4 |-----> */
					
					//Disabled
				UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
				/* <-----| Custom Code Dots Instancing Declare Cached Properties |-----> */
				
				//Disabled
				void MKSetupDOTSMaterialPropertyCaches()
				{
					/* <-----| Custom Code Dots Instancing Assign Cached Properties |-----> */
					
					//Disabled
				}
				/* <-----| Custom Code Dots Instancing Re-Assign Cached Properties |-----> */
				
				//Disabled
			#endif
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 unpackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						unpackedUV = RemapFoveatedRenderingLinearToNonUniform(uv);
					}
					return unpackedUV;
				#else
					return uv;
				#endif
			}
			inline float2 MKRepackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 repackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						repackedUV = RemapFoveatedRenderingNonUniformToLinear(uv);
					}
					return repackedUV;
				#else
					return uv;
				#endif
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(_CameraDepthTexture)
			inline float ComputeSampleCameraDepth(float2 uv)
			{
				return SampleTex2DHPAuto(PASS_TEXTURE_2D(_CameraDepthTexture, MK_SAMPLER_LINEAR_CLAMP), uv).r;
			}
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(_CameraNormalsTexture)
			uniform float4 _CameraNormalsTexture_TexelSize;
			#ifndef _CameraDepthNormalsTexture_TexelSize
				#define _CameraDepthNormalsTexture_TexelSize _CameraNormalsTexture_TexelSize
			#endif
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				float3 normal = SampleTex2DAuto(PASS_TEXTURE_2D(_CameraNormalsTexture, MK_SAMPLER_LINEAR_CLAMP), uv).rgb;
				#if defined(_GBUFFER_NORMALS_OCT)
					float2 remappedOctNormalWS = Unpack888ToFloat2(normal);
					float2 octNormalWS = remappedOctNormalWS.xy * 2.0 - 1.0;
					normal = UnpackNormalOctQuadEncode(octNormalWS);
				#endif
				return normal;
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	/******************************************************************************************/
	/* Sub Shader Target 2.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			PackageRequirements
			{
				"com.unity.render-pipelines.universal":"[12.0,99.99]"
			}
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 2.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma only_renderers gles d3d9 d3d11_9x psp2 n3ds wiiu
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_UNIVERSAL
				#define MK_RENDER_PIPELINE_UNIVERSAL
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			//Disabled
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			/* <-----| Includes |-----> */
			#ifdef MK_UNITY_2023_2_OR_NEWER
				#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			
			#ifdef UNITY_DOTS_INSTANCING_ENABLED
				UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
					/* <-----| Custom Code Dots Instancing Properties Float4 |-----> */
					
					//Disabled
				UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
				/* <-----| Custom Code Dots Instancing Declare Cached Properties |-----> */
				
				//Disabled
				void MKSetupDOTSMaterialPropertyCaches()
				{
					/* <-----| Custom Code Dots Instancing Assign Cached Properties |-----> */
					
					//Disabled
				}
				/* <-----| Custom Code Dots Instancing Re-Assign Cached Properties |-----> */
				
				//Disabled
			#endif
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 unpackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						unpackedUV = RemapFoveatedRenderingLinearToNonUniform(uv);
					}
					return unpackedUV;
				#else
					return uv;
				#endif
			}
			inline float2 MKRepackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 repackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						repackedUV = RemapFoveatedRenderingNonUniformToLinear(uv);
					}
					return repackedUV;
				#else
					return uv;
				#endif
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(_CameraDepthTexture)
			inline float ComputeSampleCameraDepth(float2 uv)
			{
				return SampleTex2DHPAuto(PASS_TEXTURE_2D(_CameraDepthTexture, MK_SAMPLER_LINEAR_CLAMP), uv).r;
			}
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(_CameraNormalsTexture)
			uniform float4 _CameraNormalsTexture_TexelSize;
			#ifndef _CameraDepthNormalsTexture_TexelSize
				#define _CameraDepthNormalsTexture_TexelSize _CameraNormalsTexture_TexelSize
			#endif
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				float3 normal = SampleTex2DAuto(PASS_TEXTURE_2D(_CameraNormalsTexture, MK_SAMPLER_LINEAR_CLAMP), uv).rgb;
				#if defined(_GBUFFER_NORMALS_OCT)
					float2 remappedOctNormalWS = Unpack888ToFloat2(normal);
					float2 octNormalWS = remappedOctNormalWS.xy * 2.0 - 1.0;
					normal = UnpackNormalOctQuadEncode(octNormalWS);
				#endif
				return normal;
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	/******************************************************************************************/
	/* Render Pipeline: High Definition */
	/******************************************************************************************/
	/******************************************************************************************/
	/* Sub Shader Target 4.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			"RenderPipeline" = "HDRenderPipeline"
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			PackageRequirements
			{
				"com.unity.render-pipelines.high-definition":"[12.0,99.99]"
			}
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 4.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma exclude_renderers gles d3d9 d3d11_9x psp2 n3ds wiiu
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_HIGH_DEFINITION
				#define MK_RENDER_PIPELINE_HIGH_DEFINITION
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			//Disabled
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
			
			/* <-----| Includes |-----> */
			#ifdef MK_UNITY_2023_2_OR_NEWER
				#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && SHADER_TARGET >= 35
				#ifndef MK_RENDER_PIPELINE_HIGH_DEFINITION
					#define MK_RENDER_PIPELINE_HIGH_DEFINITION
				#endif
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			
			#ifdef UNITY_DOTS_INSTANCING_ENABLED
				UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
					/* <-----| Custom Code Dots Instancing Properties Float4 |-----> */
					
					//Disabled
				UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
				/* <-----| Custom Code Dots Instancing Declare Cached Properties |-----> */
				
				//Disabled
				void MKSetupDOTSMaterialPropertyCaches()
				{
					/* <-----| Custom Code Dots Instancing Assign Cached Properties |-----> */
					
					//Disabled
				}
				/* <-----| Custom Code Dots Instancing Re-Assign Cached Properties |-----> */
				
				//Disabled
			#endif
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 unpackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						unpackedUV = RemapFoveatedRenderingLinearToNonUniform(uv);
					}
					return unpackedUV;
				#else
					return uv;
				#endif
			}
			inline float2 MKRepackUV(float2 uv)
			{
				#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					float2 repackedUV = uv;
					if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
					{
						repackedUV = RemapFoveatedRenderingNonUniformToLinear(uv);
					}
					return repackedUV;
				#else
					return uv;
				#endif
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			float ComputeSampleCameraDepth(float2 uv) 
			{ 
				return SampleCameraDepth(uv).r; 
			}
			uniform float4 _NormalBufferTexture_TexelSize;
			#ifndef _CameraDepthNormalsTexture_TexelSize
				#define _CameraDepthNormalsTexture_TexelSize _NormalBufferTexture_TexelSize
			#endif
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				NormalData normalData;
				DecodeFromNormalBuffer(uv * _PostProcessScreenSize.xy, normalData);
				return normalData.normalWS;
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	/******************************************************************************************/
	/* Sub Shader Target 3.5 */
	/******************************************************************************************/
	//Disabled
	/******************************************************************************************/
	/* Sub Shader Target 2.5 */
	/******************************************************************************************/
	//Disabled
	/******************************************************************************************/
	/* Render Pipeline: Built-in */
	/******************************************************************************************/
	/******************************************************************************************/
	/* Sub Shader Target 4.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			//Disabled RenderPipeline = Built_inRenderPipeline
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			//Disabled
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 4.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma exclude_renderers gles d3d9 d3d11_9x psp2 n3ds wiiu
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_BUILT_IN
				#define MK_RENDER_PIPELINE_BUILT_IN
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			#pragma multi_compile __ _MK_SHADER_PPSV2
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include "UnityCG.cginc"
			
			/* <-----| Includes |-----> */
			//Disabled
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef _MK_SHADER_PPSV2
				#define MK_SHADER_PPSV2
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if defined(MK_TEXTURE_2D_AS_ARRAY) && defined(MK_SHADER_PPSV2)
				#undef MK_TEXTURE_2D_AS_ARRAY
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				return uv;
			}
			inline float2 MKRePackUV(float2 uv)
			{
				return uv;
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(_CameraDepthTexture)
			inline float ComputeSampleCameraDepth(float2 uv)
			{
				return SampleTex2DHPAuto(PASS_TEXTURE_2D(_CameraDepthTexture, MK_SAMPLER_LINEAR_CLAMP), uv).r;
			}
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(_CameraDepthNormalsTexture)
			uniform float4 _CameraDepthNormalsTexture_TexelSize;
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				float4 depthNormal = SampleTex2DAuto(PASS_TEXTURE_2D(_CameraDepthNormalsTexture, MK_SAMPLER_LINEAR_CLAMP), uv);
				float4 decodedDepthNormal; 
				DecodeDepthNormal(depthNormal, decodedDepthNormal.w, decodedDepthNormal.xyz);
				return mul(UNITY_MATRIX_I_V, float4(decodedDepthNormal.xyz, 0)) * step(decodedDepthNormal.w, 1.0);
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	/******************************************************************************************/
	/* Sub Shader Target 3.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			//Disabled RenderPipeline = Built_inRenderPipeline
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			//Disabled
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 3.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma only_renderers glcore gles3
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_BUILT_IN
				#define MK_RENDER_PIPELINE_BUILT_IN
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			#pragma multi_compile __ _MK_SHADER_PPSV2
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include "UnityCG.cginc"
			
			/* <-----| Includes |-----> */
			//Disabled
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef _MK_SHADER_PPSV2
				#define MK_SHADER_PPSV2
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if defined(MK_TEXTURE_2D_AS_ARRAY) && defined(MK_SHADER_PPSV2)
				#undef MK_TEXTURE_2D_AS_ARRAY
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				return uv;
			}
			inline float2 MKRePackUV(float2 uv)
			{
				return uv;
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(_CameraDepthTexture)
			inline float ComputeSampleCameraDepth(float2 uv)
			{
				return SampleTex2DHPAuto(PASS_TEXTURE_2D(_CameraDepthTexture, MK_SAMPLER_LINEAR_CLAMP), uv).r;
			}
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(_CameraDepthNormalsTexture)
			uniform float4 _CameraDepthNormalsTexture_TexelSize;
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				float4 depthNormal = SampleTex2DAuto(PASS_TEXTURE_2D(_CameraDepthNormalsTexture, MK_SAMPLER_LINEAR_CLAMP), uv);
				float4 decodedDepthNormal; 
				DecodeDepthNormal(depthNormal, decodedDepthNormal.w, decodedDepthNormal.xyz);
				return mul(UNITY_MATRIX_I_V, float4(decodedDepthNormal.xyz, 0)) * step(decodedDepthNormal.w, 1.0);
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	/******************************************************************************************/
	/* Sub Shader Target 2.5 */
	/******************************************************************************************/
	SubShader
	{
		/* <-----| Sub Shader Tags |-----> */
		Tags
		{
			//Disabled RenderPipeline = Built_inRenderPipeline
			"LightMode"="Always"
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"DisableBatching"="False"
			"ForceNoShadowCasting"="True"
			"CanUseSpriteAtlas"="False"
			"PreviewType"="Sphere"
			"PerformanceChecks"="False"
		}
		
		/* <-----| Settings |-----> */
		Cull Off
		ZWrite Off
		ZTest Always
		
		/****************************************************/
		/* PostProcessingMain */
		/****************************************************/
		Pass
		{
			/* <-----| Package Requirements |-----> */
			//Disabled
			
			/* <-----| Pass Tags |-----> */
			Tags
			{
				
			}
			
			/* <-----| Stencil |-----> */
			//Disabled
			
			/* <-----| Settings |-----> */
			Name "PostProcessingMain"
			
			/* <-----| Program |-----> */
			HLSLPROGRAM
			
			/* <-----| System Directives |-----> */
			#pragma target 2.5
			#pragma vertex ProgramVertex
			#pragma fragment ProgramFragment
			
			/* <-----| Pass Specific Variants |-----> */
			
			/* <-----| Shader Target Filter |-----> */
			#pragma only_renderers gles d3d9 d3d11_9x psp2 n3ds wiiu
			
			/* <-----| User Variants |-----> */
			#pragma multi_compile_local_fragment __ _MK_INPUT_DEPTH
			#pragma multi_compile_local_fragment __ _MK_INPUT_NORMAL
			#pragma multi_compile_local_fragment __ _MK_INPUT_SCENE_COLOR
			#pragma multi_compile_local_fragment __ _MK_UTILIZE_AXIS_ONLY
			#pragma multi_compile_local_fragment __ _MK_PRECISION_MEDIUM _MK_PRECISION_LOW
			#pragma multi_compile_local_fragment __ _MK_FADE
			#pragma multi_compile_local_fragment __ _MK_DEBUG_VISUALIZE_EDGES
			#pragma multi_compile_local_fragment __ _MK_ENHANCE_DETAILS
			#pragma multi_compile_local_fragment __ _MK_SKETCH
			
			/* <-----| Render Pass Define |-----> */
			#define MK_RENDER_PASS_POST_PROCESSING_MAIN
			
			/* <-----| Render Pipeline Define |-----> */
			#ifndef MK_RENDER_PIPELINE_BUILT_IN
				#define MK_RENDER_PIPELINE_BUILT_IN
			#endif
			
			/* <-----| Constraints |-----> */
			#if defined(_MK_INPUT_DEPTH)
				#ifndef MK_INPUT_DEPTH
					#define MK_INPUT_DEPTH
				#endif
			#endif 
			#if defined(_MK_INPUT_NORMAL)
				#ifndef MK_INPUT_NORMAL
					#define MK_INPUT_NORMAL
				#endif
			#endif 
			#if defined(_MK_INPUT_SCENE_COLOR)
				#ifndef MK_INPUT_SCENE_COLOR
					#define MK_INPUT_SCENE_COLOR
				#endif
			#endif 
			#if defined(_MK_UTILIZE_AXIS_ONLY)
				#ifndef MK_UTILIZE_AXIS_ONLY
					#define MK_UTILIZE_AXIS_ONLY
				#endif
			#endif 
			#if defined(_MK_PRECISION_MEDIUM)
				#ifndef MK_PRECISION_MEDIUM
					#define MK_PRECISION_MEDIUM
				#endif
			#endif 
			#if defined(_MK_PRECISION_LOW)
				#ifndef MK_PRECISION_LOW
					#define MK_PRECISION_LOW
				#endif
			#endif 
			#if defined(_MK_FADE)
				#ifndef MK_FADE
					#define MK_FADE
				#endif
			#endif 
			#if defined(_MK_DEBUG_VISUALIZE_EDGES)
				#ifndef MK_DEBUG_VISUALIZE_EDGES
					#define MK_DEBUG_VISUALIZE_EDGES
				#endif
			#endif 
			#if defined(_MK_ENHANCE_DETAILS)
				#ifndef MK_ENHANCE_DETAILS
					#define MK_ENHANCE_DETAILS
				#endif
			#endif 
			#if defined(_MK_SKETCH)
				#ifndef MK_SKETCH
					#define MK_SKETCH
				#endif
			#endif 
			
			/* <-----| System Compile Directives Pre |-----> */
			
			/* <-----| System Preprocessor Directives |-----> */
			#pragma fragmentoption ARB_precision_hint_fastest
			
			/* <-----| Custom Code Local HLSL Pre |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Pre |-----> */
			//Disabled
			
			/* <-----| HLSL Includes Pre |-----> */
			//Disabled
			
			/* <-----| Pass Compile Directives |-----> */
			#pragma multi_compile __ _MK_SHADER_PPSV2
			
			/* <-----| Setup |-----> */
			//Disabled
			
			/* <-----| Unity Core Libraries |-----> */
			#include "UnityCG.cginc"
			
			/* <-----| Includes |-----> */
			//Disabled
			
			/* <-----| System Library Pre |-----> */
			#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
				#define INITIALIZE_STRUCT(type, name) name = (type) 0;
			#else
				#define INITIALIZE_STRUCT(type, name)
			#endif
			#ifdef _MK_SHADER_PPSV2
				#define MK_SHADER_PPSV2
			#endif
			#ifdef UNITY_SINGLE_PASS_STEREO
				static const half2 _StereoScale = half2(0.5, 1);
			#else
				static const half2 _StereoScale = half2(1, 1);
			#endif
			#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && ((defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)) || !defined(MK_RENDER_PIPELINE_HIGH_DEFINITION) && (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED))
				#ifndef MK_TEXTURE_2D_AS_ARRAY
					#define MK_TEXTURE_2D_AS_ARRAY
				#endif
			#endif
			#if defined(MK_TEXTURE_2D_AS_ARRAY) && defined(MK_SHADER_PPSV2)
				#undef MK_TEXTURE_2D_AS_ARRAY
			#endif
			#if SHADER_TARGET >= 35
				#if defined(MK_TEXTURE_2D_AS_ARRAY)
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2DArray<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2DArray<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2DArray<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2DArray<float4> textureName, SamplerState samplerName
				#else
					#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName;
					#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName;
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
					#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform Texture2D<float4> textureName; uniform SamplerState sampler##textureName;
					#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) Texture2D<float4> textureName, SamplerState samplerName
				#endif
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) Texture2D<half4> textureName, SamplerState samplerName
				#define UNIFORM_SAMPLER(samplerName) uniform SamplerState sampler##samplerName;
				#define UNIFORM_TEXTURE_2D(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) uniform Texture2D<half4> textureName; uniform SamplerState sampler_linear_clamp##textureName;
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName, samplerName
			#else
				#define UNIFORM_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(textureName) uniform sampler2D textureName;
				#define DECLARE_TEXTURE_2D_ARGS_AUTO_HP(textureName, samplerName) sampler2D textureName
				#define DECLARE_TEXTURE_2D_ARGS(textureName, samplerName) sampler2D textureName
				#define UNIFORM_SAMPLER(samplerName)
				#define UNIFORM_TEXTURE_2D(textureName) uniform sampler2D textureName;
				#define UNIFORM_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_TEXTURE_2D(textureName)
				#define UNIFORM_SAMPLER_AND_TEXTURE_2D_NO_SCALE(textureName) UNIFORM_SAMPLER_AND_TEXTURE_2D(textureName)
				#define PASS_TEXTURE_2D(textureName, samplerName) textureName
			#endif
			#define UNIFORM_SAMPLER_2D(sampler2DName) uniform sampler2D sampler2DName;
			#define PASS_SAMPLER_2D(sampler2DName) sampler2DName
			#define DECLARE_SAMPLER_2D_ARGS(sampler2DName) sampler2D sampler2DName
			UNIFORM_SAMPLER(_point_repeat_Main)
			UNIFORM_SAMPLER(_linear_repeat_Main)
			UNIFORM_SAMPLER(_point_clamp_Main)
			UNIFORM_SAMPLER(_linear_clamp_Main)
			#if SHADER_TARGET >= 35
				#ifdef MK_POINT_FILTERING
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_point_repeat_Main
					#endif
				#else
					#ifndef MK_SAMPLER_DEFAULT
						#define MK_SAMPLER_DEFAULT sampler_linear_repeat_Main
					#endif
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP sampler_point_clamp_Main
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP sampler_linear_clamp_Main
				#endif
			#else
				#ifndef MK_SAMPLER_DEFAULT
					#define MK_SAMPLER_DEFAULT
				#endif
				#ifndef MK_SAMPLER_POINT_CLAMP
					#define MK_SAMPLER_POINT_CLAMP
				#endif
				#ifndef MK_SAMPLER_LINEAR_CLAMP
					#define MK_SAMPLER_LINEAR_CLAMP
				#endif
			#endif
			inline half4 SampleTex2DNoScale(DECLARE_TEXTURE_2D_ARGS(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					return tex.Sample(samplerTex, uv);
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 SampleTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline float4 SampleTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv)
			{
				#if SHADER_TARGET >= 35
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Sample(samplerTex, float3((uv).xy, (float)unity_StereoEyeIndex));
					#else
						return tex.Sample(samplerTex, uv);
					#endif
				#else
					return tex2D(tex, uv);
				#endif
			}
			inline half4 LoadTex2DAuto(DECLARE_TEXTURE_2D_ARGS_AUTO(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline float4 LoadTex2DHPAuto(DECLARE_TEXTURE_2D_ARGS_AUTO_HP(tex, samplerTex), float2 uv, float2 textureSize)
			{
				#if defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
					#if defined(MK_TEXTURE_2D_AS_ARRAY)
						return tex.Load(int4(uv * textureSize, unity_StereoEyeIndex, 0));
					#else
						return tex.Load(int3(uv * textureSize, unity_StereoEyeIndex));
					#endif
				#else
					return SampleTex2DHPAuto(PASS_TEXTURE_2D(tex, samplerTex), uv);
				#endif
			}
			inline half4 SampleRamp1D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half value)
			{
				#if SHADER_TARGET >= 35
					return ramp.Sample(samplerTex, float2(saturate(value), 0.5));
				#else
					return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), float2(saturate(value), 0.5));
				#endif
			}
			inline half4 SampleRamp2D(DECLARE_TEXTURE_2D_ARGS(ramp, samplerTex), half2 value)
			{
				return SampleTex2DNoScale(PASS_TEXTURE_2D(ramp, samplerTex), saturate(value));
			}
			inline half Rcp(half v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0h / v;
				#endif
			}
			inline float RcpHP(float v)
			{
				#if SHADER_TARGET >= 50
					return rcp(v);
				#else
					return 1.0f / v;
				#endif
			}
			
			/* <-----| Constant Buffers |-----> */
			CBUFFER_START(UnityPerMaterial)
				/* <-----| Custom Code Constant Buffers |-----> */
				/* <-----| Custom Code Include Directives |-----> */
				//Disabled
				//Disabled
			CBUFFER_END
			/* <-----| Custom Code Textures |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			UNIFORM_TEXTURE_2D_AUTO(_MKShaderMainTex)
			/* <-----| Injected Buffers |-----> */
			
			/* <-----| Shared User Data |-----> */
			struct MKSharedUserData
			{
				
			};
			
			/* <-----| User Data |-----> */
			struct MKUserData
			{
				
			};
			
			/* <-----| System Library Core Data |-----> */
			
			/* <-----| HLSL Includes After Core Data |-----> */
			//Disabled
			
			/* <-----| System Functons Pre Library Core |-----> */
			
			/* <-----| System Library Core |-----> */
			
			inline float2 MKSetMeshUV(float2 positionObject)
			{
				float2 uv = (positionObject + 1.0) * 0.5;
				#ifdef UNITY_UV_STARTS_AT_TOP
					uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
				#endif
				return uv;
			}
			inline float2 MKUnpackUV(float2 uv)
			{
				return uv;
			}
			inline float2 MKRePackUV(float2 uv)
			{
				return uv;
			}
			inline float4 MKSetMeshPositionClip(float3 positionObject)
			{
				#ifdef MK_IMAGE_EFFECTS
					return UnityObjectToClipPos(positionObject);
				#else
					return float4(positionObject.xy, 0.0, 1.0);
				#endif
			}
			inline float ComputeLinear01Depth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.x * eyeDepth + _ZBufferParams.y);
			}
			inline float ComputeLinearDepth(float eyeDepth)
			{
				return RcpHP(_ZBufferParams.z * eyeDepth + _ZBufferParams.w);
			}
			inline float ComputeLinearDepthToEyeDepth(float eyeDepth)
			{
				#if UNITY_REVERSED_Z
					return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#else
					return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * eyeDepth;
				#endif
			}
			uniform float4 _CameraDepthTexture_TexelSize;
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO_HP(_CameraDepthTexture)
			inline float ComputeSampleCameraDepth(float2 uv)
			{
				return SampleTex2DHPAuto(PASS_TEXTURE_2D(_CameraDepthTexture, MK_SAMPLER_LINEAR_CLAMP), uv).r;
			}
			UNIFORM_SAMPLER_AND_TEXTURE_2D_AUTO(_CameraDepthNormalsTexture)
			uniform float4 _CameraDepthNormalsTexture_TexelSize;
			inline half3 ComputeSampleCameraNormals(float2 uv)
			{
				float4 depthNormal = SampleTex2DAuto(PASS_TEXTURE_2D(_CameraDepthNormalsTexture, MK_SAMPLER_LINEAR_CLAMP), uv);
				float4 decodedDepthNormal; 
				DecodeDepthNormal(depthNormal, decodedDepthNormal.w, decodedDepthNormal.xyz);
				return mul(UNITY_MATRIX_I_V, float4(decodedDepthNormal.xyz, 0)) * step(decodedDepthNormal.w, 1.0);
			}
			
			/* <-----| HLSL Includes Post |-----> */
			//Disabled
			
			/* <-----| User Local HLSL Post |-----> */
			uniform float4x4 _MKInverseViewProjectionMatrix;
			uniform half4x4 _KernelX;
			uniform half4x4 _KernelY;
			uniform float4 _MKShaderMainTex_TexelSize;
			uniform float4 _MKEdgeDetectionSelectiveMask_TexelSize;
			uniform half4 _DepthFadeParams;
			uniform half4 _NormalFadeParams;
			uniform half4 _LineColor;
			uniform half4 _OverlayColor;
			uniform half4 _ThresholdLowParams;
			uniform half4 _ThresholdHighParams;
			uniform half4 _SceneColorFadeParams;
			uniform half3 _LineSizeParams;
			uniform float3 _FogParams;
			uniform float2 _MKShaderMainTexDimension;
			uniform half _LineHardness;
			
			UNIFORM_TEXTURE_2D_AUTO_HP(_MKEdgeDetectionSelectiveMask)
			#pragma multi_compile_fog
			
			#include "GlobalShaderFeatures.hlsl"
			#include "Config.hlsl"
			#include "CommonShared.hlsl"
			#include "Common.hlsl"
			
			inline half MKComputeFogIntensity(float linear01Depth)
			{
				float fogDistance = linear01Depth * _ProjectionParams.z - _ProjectionParams.y;
				half fog = 0.0;
				#if defined(FOG_LINEAR)
					fog = (_FogParams.z - fogDistance) / (_FogParams.z - _FogParams.y);
				#elif defined(FOG_EXP)
					fog = exp2(-_FogParams.x * fogDistance);
				#else
					fog = _FogParams.x * fogDistance;
					fog = exp2(-fog * fog);
				#endif
				return saturate(fog);
			}
			
			/* <-----| User HLSL Includes Code Injection |-----> */
			//Disabled
			
			/* <-----| System Compile Directives Post |-----> */
			
			/* <-----| Custom Code Setup Vertex Data Object |-----> */
			/* <-----| Custom Code Include Directives |-----> */
			//Disabled
			
			/* <-----| System Library Vertex Data |-----> */
			
			/* <-----| Post Processing Initialize |-----> */
			inline void MKInitialize(in float2 uv, out MKUserData userData)
			{
				INITIALIZE_STRUCT(MKUserData, userData);
			}
			
			/* <-----| Post Processing |-----> */
			inline void MKPostProcessing(in float2 uv, in MKUserData userData, out half4 result)
			{
				half4 mainSample = LoadTex2DAuto(PASS_TEXTURE_2D(_MKShaderMainTex, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension);
				
				#ifdef MK_EDGE_DETECTION_OFF
					#ifdef MK_DEBUG_VISUALIZE_EDGES
						result = half4(0, 0, 0, 1);
					#else
						result = mainSample;
					#endif
				#else
					#if defined(MK_SELECTIVE_WORKFLOW) || defined(MK_FADE) || defined(MK_INPUT_DEPTH) || defined(MK_FOG) || defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
						const float rawDepth = ComputeSampleCameraDepth(uv);
						float linear01Depth = ComputeLinear01Depth(rawDepth);
						
						#if defined(MK_FADE) || defined(MK_SELECTIVE_WORKFLOW)
							float linearDepth = ComputeLinearDepth(rawDepth);
						#endif
					#else
						const float linear01Depth = 0;
						const float linearDepth = 1;
					#endif
					
					#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
						half isSkybox = step(1.0, linear01Depth);
					#endif
					
					#if defined(MK_INPUT_DEPTH) || defined(MK_SELECTIVE_WORKFLOW)
						MKEdgeDetectionDepthSampleData depthData;
						float depthMask;
						
						/*
						#if defined(MK_FADE)
							half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthLineSize = _LineSizeParams.x * depthFade;
							const half depthOpacity = depthFade;
						#else
							const half depthLineSize = _LineSizeParams.x;
							const half depthOpacity = 1;
						#endif
						*/
						
						float4 depthSampleSize = ComputeSampleSize(_LineSizeParams.x, _CameraDepthTexture_TexelSize.xy);
						depthMask = ComputeDepthEdges(uv, rawDepth, linear01Depth, depthSampleSize, depthData);
						
						#if defined(MK_FADE)
							linearDepth = ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01));
							const half depthFade = ComputeDistanceScale(linearDepth, _DepthFadeParams.z, _DepthFadeParams.w, _DepthFadeParams.x, _DepthFadeParams.y);
							const half depthOpacity = depthFade;
						#else
							const half depthFade = 1;
							const half depthOpacity = 1;
						#endif
						
						depthMask = depthOpacity * ApplyAdjustments(depthMask, _ThresholdLowParams.x, _ThresholdHighParams.x);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							isSkybox = step(1.0, depthData.nearestLinear01);
							depthMask = lerp(depthMask, 0, isSkybox);
						#endif
					#else
						float depthMask = 0;
					#endif
					
					#ifdef MK_INPUT_NORMAL
						MKEdgeDetectionNormalSampleData normalData;
						//additional steep angle fix could be implemented here
						#ifdef MK_FADE
							half normalOpacity = ComputeDistanceScale(linearDepth, _NormalFadeParams.z, _NormalFadeParams.w, _NormalFadeParams.x, _NormalFadeParams.y);
							const half normalLineSize = _LineSizeParams.y * normalOpacity;
						#else
							half normalOpacity = 1;
							const half normalLineSize = _LineSizeParams.y;
						#endif
						half normalMask = ComputeNormalEdges(uv, normalLineSize, normalData);
						//normalMask = step(_NormalsThreshold, normalMask);
						normalMask = normalOpacity * ApplyAdjustments(normalMask, _ThresholdLowParams.y, _ThresholdHighParams.y);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							normalMask = lerp(normalMask, 0, isSkybox);
						#endif
					#else
						half normalMask = 0;
					#endif
					
					#ifdef MK_INPUT_SCENE_COLOR
						//Color cannot be scaled, because it messes up with depth buffer on certain view angles
						#ifdef MK_FADE
							const half colorFade = ComputeDistanceScale(linearDepth, _SceneColorFadeParams.z, _SceneColorFadeParams.w, _SceneColorFadeParams.x, _SceneColorFadeParams.y);
						#else
							const half colorFade = 1;
						#endif
						half sceneColorMask = ComputeColorEdges(uv, mainSample.rgb, _LineSizeParams.z * colorFade);
						sceneColorMask = colorFade * ApplyAdjustments(sceneColorMask, _ThresholdLowParams.z, _ThresholdHighParams.z);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							//skybox buffer is always generated
							//sceneColorMask = lerp(sceneColorMask, 0, isSkybox);
						#endif
					#else
						half sceneColorMask = 0;
					#endif
					
					#ifdef MK_EVALUATE_COMBINED_INPUT
						MKEdgeDetectionPositionSampleData positionData;
						half combinedInputMask = ComputeCombinedEdges(uv, depthSampleSize, depthData, normalData, positionData);
						//combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, 0, _ThresholdHighParams.w);
						//combinedInputMask = step(MK_EVALUATE_COMBINED_INPUT_EPSILON, combinedInputMask);
						combinedInputMask = depthOpacity * ApplyAdjustments(combinedInputMask, _ThresholdLowParams.w, _ThresholdHighParams.w);
						#ifdef MK_RENDER_PIPELINE_HIGH_DEFINITION
							combinedInputMask = lerp(combinedInputMask, 0, isSkybox);
						#endif
						
						//Always force the combined details, disabled for now for smoother transition
						//combinedInputMask = combinedInputMask > 0;
					#else
						half combinedInputMask = 0;
					#endif
					
					#ifdef MK_SELECTIVE_WORKFLOW
						#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
							half isSelectiveSkybox = step(1.0, depthData.nearestLinear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#else
							half isSelectiveSkybox = step(1.0, depthData.d4Linear01);
							half selectiveSkyMask = lerp(1, 0, isSelectiveSkybox);
						#endif
						
						const float rawSelectiveMask = LoadTex2DHPAuto(PASS_TEXTURE_2D(_MKEdgeDetectionSelectiveMask, MK_SAMPLER_LINEAR_CLAMP), uv, _MKShaderMainTexDimension).r;
						const float linearSelectiveMask = ComputeLinearDepth(rawSelectiveMask);
						
						#if defined(MK_INPUT_DEPTH)
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 depthSelectiveMaskSampleSize = depthSampleSize;
								MKEdgeDetectionSelectiveMaskData depthSelectiveMaskData;
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, depthSelectiveMaskSampleSize, depthSelectiveMaskData);
								half selectiveDepthMask = selectiveSkyMask * step(distance(ComputeLinearDepth(ComputeEyeDepthFromLinear01Depth(depthData.nearestLinear01)), depthSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								depthMask *= selectiveDepthMask;
								combinedInputMask *= selectiveDepthMask;
							#endif
						#else
							depthMask = 0;
						#endif
						
						#ifdef MK_INPUT_NORMAL
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 normalSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * normalLineSize;
								MKEdgeDetectionSelectiveMaskData normalSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData normalDepthData = ComputeSelectiveDepth(uv, linearDepth, normalSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, normalSelectiveMaskSampleSize, normalSelectiveMaskData);
								half selectiveNormalMask = selectiveSkyMask * step(distance(normalDepthData.nearestLinear, normalSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								normalMask *= selectiveNormalMask;
							#endif
						#endif
						
						#ifdef MK_INPUT_SCENE_COLOR
							#ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
								float2 sceneColorSelectiveMaskSampleSize = _CameraDepthTexture_TexelSize.xy * _LineSizeParams.z * colorFade;
								MKEdgeDetectionSelectiveMaskData sceneColorSelectiveMaskData;
								MKEdgeDetectionSelectiveDepthData sceneColorDepthData = ComputeSelectiveDepth(uv, linearDepth, sceneColorSelectiveMaskSampleSize);
								ComputeSelectiveMask(uv, rawSelectiveMask, linearSelectiveMask, sceneColorSelectiveMaskSampleSize, sceneColorSelectiveMaskData);
								half selectiveSceneColorMask = selectiveSkyMask * step(distance(sceneColorDepthData.nearestLinear, sceneColorSelectiveMaskData.nearestLinear), MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON); 
								sceneColorMask *= selectiveSceneColorMask;
							#endif
						#endif
					#endif
					
					#if defined(MK_DEBUG_VISUALIZE_EDGES)
						#if defined(MK_FOG)
							half fogMult = MKComputeFogIntensity(linear01Depth);
						#else
							const half fogMult = 1;
						#endif
						
						half3 visualizeColor = half3(depthMask + combinedInputMask, normalMask, sceneColorMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							visualizeColor *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						result.rgb = lerp(half3(0, 0, 0), half3(1, 1, 1) - half3(_LineHardness, _LineHardness, _LineHardness), visualizeColor * fogMult);
						result.a = mainSample.a;
					#else
						half edgeMask = smoothstep(0, 1 - _LineHardness, depthMask + normalMask + sceneColorMask + combinedInputMask);
						
						#if defined(MK_SELECTIVE_WORKFLOW_FAST_MODE) && defined(MK_SELECTIVE_WORKFLOW)
							edgeMask *= selectiveSkyMask * step(distance(linearDepth, linearSelectiveMask), MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON);
						#endif
						
						#if defined(MK_FOG)
							edgeMask *= MKComputeFogIntensity(linear01Depth);
						#endif
						
						half3 composition;
						#ifdef MK_SKETCH
							composition.rgb = edgeMask;
						#else
							composition.rgb = lerp(lerp(mainSample.rgb, _OverlayColor.rgb, _OverlayColor.a), _LineColor.rgb, edgeMask * _LineColor.a);
						#endif
						result.rgb = composition;
						result.a = mainSample.a;
					#endif
				#endif
			}
			
			/* <-----| System Library Post |-----> */
			
			
			/* <-----| Attributes |-----> */
			struct MKAttributes
			{
				#if SHADER_TARGET >= 35
					uint vertexID : SV_VertexID;
				#else
					float4 positionObject : POSITION;
					float2 uv : TEXCOORD0;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			/* <-----| Varyings |-----> */
			struct MKVaryings
			{
				float4 svPositionClip : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			/* <-----| Program Vertex |-----> */
			#if SHADER_TARGET >= 35
				static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
				#ifdef UNITY_UV_STARTS_AT_TOP
					static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
				#else
					static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
				#endif
			#endif
			MKVaryings ProgramVertex(MKAttributes attributes)
			{
				MKVaryings varyings;
				UNITY_SETUP_INSTANCE_ID(attributes);
				INITIALIZE_STRUCT(MKVaryings, varyings);
				UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);
				#if SHADER_TARGET >= 35
					varyings.svPositionClip = vertexPositions[attributes.vertexID];
					varyings.uv = vertexUVs[attributes.vertexID].xy;
				#else
					varyings.svPositionClip = MKSetMeshPositionClip(attributes.positionObject.xyz);
					#ifdef MK_IMAGE_EFFECTS
						varyings.uv = attributes.uv.xy;
					#else
						varyings.uv = MKSetMeshUV(attributes.positionObject.xy);
					#endif
				#endif
				return varyings;
			}
			
			/* <-----| Program Fragment |-----> */
			half4 ProgramFragment(in MKVaryings varyings) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(varyings);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
				half4 result;
				MKUserData userData;
				MKInitialize(varyings.uv, userData);
				MKPostProcessing(varyings.uv, userData, result);
				return result;
			}
			ENDHLSL
		}
	}
	Fallback Off
}