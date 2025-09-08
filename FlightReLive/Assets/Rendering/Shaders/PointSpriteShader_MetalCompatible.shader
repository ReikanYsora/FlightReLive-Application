Shader "FlightReLive/PointSpriteShader_MetalCompatible"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _GradientTex("Gradient Texture", 2D) = "white" {}
        _PointSize("Point Size (in pixels)", Float) = 32.0
        _RelativeOrAbsolute("Gradient Sampling Mode (0=Abs, 1=Rel)", Float) = 0
        _MainTexOpacity("Main Texture Opacity", Range(0,1)) = 1.0
        _GradientIntensity("Gradient Intensity", Float) = 1.0
        _AltitudeMin("Absolute Altitude Min", Float) = 0.0
        _AltitudeMax("Absolute Altitude Max", Float) = 10000.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "PointSpritesGlobal"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_GradientTex);    SAMPLER(sampler_GradientTex);

            float _PointSize;
            float _RelativeOrAbsolute;
            float _MainTexOpacity;
            float _GradientIntensity;
            float _AltitudeMin;
            float _AltitudeMax;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 gradientCoord : TEXCOORD2;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD3;
                float altitude : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float size = _PointSize * 0.5;

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp    = UNITY_MATRIX_V[1].xyz;

                // Simulate quad corners using texcoord
                float2 offset = input.texcoord * 2.0 - 1.0;
                float3 offsetWS = camRight * offset.x * size + camUp * offset.y * size;
                float3 finalPosWS = worldPos + offsetWS;

                o.positionCS = TransformWorldToHClip(finalPosWS);
                o.texcoord = input.texcoord;
                o.gradientCoord = input.uv2;
                o.color = input.color;
                o.worldPos = worldPos;
                o.altitude = worldPos.y;

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
                texColor.a *= _MainTexOpacity;
                texColor.rgb *= texColor.a;

                float gradientUV;
                if (_RelativeOrAbsolute >= 1.0)
                {
                    gradientUV = saturate(i.gradientCoord.y);
                }
                else
                {
                    gradientUV = (i.gradientCoord.x - _AltitudeMin) / (_AltitudeMax - _AltitudeMin);
                }

                float gradientY = (0.5 / 8.0);
                float4 gradientColor = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, float2(gradientUV, gradientY));
                gradientColor.rgb *= gradientColor.a;
                gradientColor *= _GradientIntensity;

                float4 finalColor = texColor * gradientColor * i.color;
                return SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, float2(gradientUV, gradientY));
            }
            ENDHLSL
        }
    }
}
