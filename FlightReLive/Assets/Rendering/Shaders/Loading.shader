Shader "FlightReLive/Loading"
{
    Properties
    {
        _Progress ("Progress", Range(0,1)) = 0
        _MaxHeight ("Max Height", Float) = 20
        _DisplacementRadius ("Displacement Radius", Range(0,1)) = 0.8
        _NoiseScale ("Noise Scale", Float) = 5
        _EdgeFalloffRadius ("Edge Falloff Radius", Range(0,1)) = 0.7
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        _BlendSharpness ("Blend Sharpness", Range(0.001, 0.5)) = 0.05

        _CustomTime ("Custom Time", Float) = 0
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        _WaveFrequency ("Wave Frequency", Float) = 0.2
        _WaveWidth ("Wave Width", Range(0.001, 0.5)) = 0.05
        _WaveAmplitude ("Wave Amplitude", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Progress;
            float _MaxHeight;
            float _DisplacementRadius;
            float _NoiseScale;
            float _EdgeFalloffRadius;
            float _BlendSharpness;
            fixed4 _EdgeColor;

            float _CustomTime;
            float _RotationSpeed;
            float _WaveFrequency;
            float _WaveWidth;
            float _WaveAmplitude;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float height : TEXCOORD1;
                float mask : TEXCOORD2;
                float blendFactor : TEXCOORD3;
                float radialFactor : TEXCOORD4;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;

                // Rotation du mesh autour de Y
                float angle = _CustomTime * _RotationSpeed;
                float cosA = cos(angle);
                float sinA = sin(angle);

                float3 rotatedPos = v.vertex.xyz;
                rotatedPos.xz = float2(
                    v.vertex.x * cosA - v.vertex.z * sinA,
                    v.vertex.x * sinA + v.vertex.z * cosA
                );

                float2 uv = v.uv;
                float2 center = float2(0.5, 0.5);
                float dist = distance(uv, center) * 2.0;

                float mask = 1.0 - smoothstep(_DisplacementRadius, _EdgeFalloffRadius, dist);

                float n1 = noise(uv * _NoiseScale * 2.0);
                float n2 = noise(uv * _NoiseScale * 4.0);
                float combined = (n1 + n2 * 0.5);
                float sharpNoise = pow(combined, 0.6);

                float displacement = sharpNoise * _Progress * _MaxHeight * mask;

                // Onde de choc cyclique
                float waveRadius = frac(_CustomTime * _WaveFrequency);
                float waveMask = exp(-pow((dist - waveRadius) / _WaveWidth, 2.0));
                float waveDisplacement = waveMask * _WaveAmplitude;

                rotatedPos.y += displacement + waveDisplacement;

                o.pos = UnityObjectToClipPos(float4(rotatedPos, 1.0));
                o.uv = uv;
                o.color = v.color;
                o.height = displacement;
                o.mask = mask;
                o.blendFactor = dist;
                o.radialFactor = dist;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alphaFactor = saturate(i.height / _MaxHeight);

                float edge = saturate(1.0 - _Progress);
                float sharpness = max(_BlendSharpness, 0.001);
                float colorBlend = saturate((i.blendFactor - edge + sharpness) / (2.0 * sharpness));
                fixed4 blendedColor = lerp(i.color, _EdgeColor, colorBlend);

                blendedColor.a *= alphaFactor * i.mask;
                return blendedColor;
            }

            ENDCG
        }
    }
}
