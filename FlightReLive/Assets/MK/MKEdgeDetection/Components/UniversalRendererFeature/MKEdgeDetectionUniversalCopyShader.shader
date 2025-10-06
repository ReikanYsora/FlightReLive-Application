/******************************************************************************************/
/* Universal Copy Shader */
/******************************************************************************************/

/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de

ASSET STORE TERMS OF SERVICE AND EULA:
https://unity.com/de/legal/as-terms
*****************************************************/

/* File created using: */
/* MK Shader - Cross Compiling Shaders */
/* Version: 1.1.23  */
/* Exported on: 28.05.2024 02:19:23 */

Shader "Hidden/MK/EdgeDetection/PostProcessing/MKEdgeDetectionUniversalCopy"
{
    SubShader
    {
        Tags {"LightMode" = "Always" "RenderType"="Opaque" "PerformanceChecks"="False"}
		Cull Off ZWrite Off ZTest Always

        Pass
        {
            PackageRequirements
			{
				"com.unity.render-pipelines.universal":"[1.0,99.99]"
			}

            Name "Copy"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            static const half4 vertexPositions[3] = { half4(-1, -1, 0, 1), half4(3, -1, 0, 1), half4(-1, 3, 0, 1) };
            #ifdef UNITY_UV_STARTS_AT_TOP
                static const float2 vertexUVs[3] = { float2(0, 1), float2(2, 1), float2(0, -1)  };
            #else
                static const float2 vertexUVs[3] = { float2(0, 0), float2(2, 0), float2(0, 2)  };
            #endif

            struct MKAttributes
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct MKVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            MKVaryings vert(MKAttributes input)
            {
                MKVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = vertexPositions[input.vertexID];
                output.uv = vertexUVs[input.vertexID].xy;

                return output;
            }

            TEXTURE2D_X(_MKShaderMainTex);
            SAMPLER(sampler_MKShaderMainTex);

            half4 frag (MKVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_MKShaderMainTex, sampler_MKShaderMainTex, input.uv);
            }
            ENDHLSL
        }

        Pass
        {
            PackageRequirements
			{
				"com.unity.render-pipelines.universal":"[1.0,99.99]"
			}

            Name "Copy"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct MKAttributes
            {
                float4 positionHCS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct MKVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float2 MKSetMeshUV(float2 vertex)
            {
                float2 uv = (vertex + 1.0) * 0.5;
                #ifdef UNITY_UV_STARTS_AT_TOP
                    uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #endif
                return uv;
            }

            MKVaryings vert(MKAttributes input)
            {
                MKVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = float4(input.positionHCS.xy, 0.0, 1.0);
                output.uv = MKSetMeshUV(input.positionHCS.xy);

                return output;
            }

            TEXTURE2D_X(_MKShaderMainTex);
            SAMPLER(sampler_MKShaderMainTex);

            half4 frag (MKVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_MKShaderMainTex, sampler_MKShaderMainTex, input.uv);
            }
            ENDHLSL
        }
    }

    Fallback Off
}