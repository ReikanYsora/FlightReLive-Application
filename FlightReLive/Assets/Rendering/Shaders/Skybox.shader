Shader "Flight ReLive/Skybox"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _Exposure ("Exposure", Float) = 1.0
        _Rotation ("Rotation", Range(0,360)) = 0

        _FrontTex ("Front (+Z)", 2D) = "white" {}
        _BackTex ("Back (-Z)", 2D) = "white" {}
        _LeftTex ("Left (+X)", 2D) = "white" {}
        _RightTex ("Right (-X)", 2D) = "white" {}
        _UpTex ("Up (+Y)", 2D) = "white" {}
        _DownTex ("Down (-Y)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
            };

            float4 _Tint;
            float _Exposure;
            float _Rotation;

            sampler2D _FrontTex;
            sampler2D _BackTex;
            sampler2D _LeftTex;
            sampler2D _RightTex;
            sampler2D _UpTex;
            sampler2D _DownTex;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);

                // Convert to world direction
                float3 dir = normalize(mul((float3x3)UNITY_MATRIX_M, v.positionOS.xyz));

                // Apply horizontal rotation
                float rad = radians(_Rotation);
                float3x3 rotMatrix = float3x3(
                    cos(rad), 0, -sin(rad),
                    0,        1, 0,
                    sin(rad), 0, cos(rad)
                );
                o.directionWS = mul(rotMatrix, dir);

                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float3 dir = normalize(i.directionWS);
                float2 uv;
                float4 texColor;

                if (abs(dir.y) > abs(dir.x) && abs(dir.y) > abs(dir.z))
                {
                    // Up or Down
                    uv = dir.xz * 0.5 / abs(dir.y) + 0.5;
                    texColor = dir.y > 0 ? tex2D(_UpTex, uv) : tex2D(_DownTex, uv);
                }
                else if (abs(dir.x) > abs(dir.z))
                {
                    // Left or Right
                    uv = dir.zy * 0.5 / abs(dir.x) + 0.5;
                    texColor = dir.x > 0 ? tex2D(_LeftTex, uv) : tex2D(_RightTex, uv);
                }
                else
                {
                    // Front or Back
                    uv = dir.xy * 0.5 / abs(dir.z) + 0.5;
                    texColor = dir.z > 0 ? tex2D(_FrontTex, uv) : tex2D(_BackTex, uv);
                }

                return texColor * _Tint * _Exposure;
            }
            ENDHLSL
        }
    }
}
