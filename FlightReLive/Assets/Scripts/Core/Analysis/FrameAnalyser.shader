Shader "Hidden/FrameAnalyser"
{
    Properties
    {
        _MainTexA ("Previous Frame", 2D) = "white" {}
        _MainTexB ("Current Frame", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "DiffPass"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTexA;
            sampler2D _MainTexB;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 a = tex2D(_MainTexA, i.uv).rgb;
                float3 b = tex2D(_MainTexB, i.uv).rgb;
                float3 diff = abs(b - a);
                return float4(diff, 1.0);
            }
            ENDHLSL
        }
    }
}
