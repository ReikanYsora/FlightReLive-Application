Shader "FlightReLive/PathProgressShader"
{
    Properties
    {
        _Progress ("Progress", Range(0,1)) = 0.0
        _HoverProgress ("Hover Progress", Range(0,1)) = 0.0
        _ColorA ("Played Color", Color) = (0,2,0,1)
        _ColorB ("Hover Color", Color) = (1,1,0,1)
        _ColorC ("Remaining Color", Color) = (0.2,0.2,0.2,1)
        _ColorD ("Remaining Alt Color", Color) = (0.1,0.1,0.1,1)
        _BaseThickness ("Base Thickness", Float) = 0.09
        _CameraDistanceFactor ("Camera Distance Factor", Float) = 1.0
        _GlowProgress ("Glow Progress", Range(0,1)) = 0.0
        [HDR]_GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowBandWidth ("Glow Band Width", Range(0.001,0.1)) = 0.02
        _StripeFrequency ("Stripe Frequency", Range(1,500)) = 20.0
        _DeformationAmplitude ("Glow Deformation Amplitude", Range(0,0.2)) = 0.05
        _DeformationBandWidth ("Deformation Band Width", Range(0.001,0.2)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _Progress;
            float _HoverProgress;
            float4 _ColorA;
            float4 _ColorB;
            float4 _ColorC;
            float4 _ColorD;
            float _BaseThickness;
            float _CameraDistanceFactor;
            float _GlowProgress;
            float4 _GlowColor;
            float _GlowBandWidth;
            float _StripeFrequency;
            float _DeformationAmplitude;
            float _DeformationBandWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                // Déformation fluide dans une bande élargie
                float glowOffset = _GlowBandWidth * 0.5;
                float glowCenter = saturate(_Progress - glowOffset);
                float deformationFalloff = 1.0 - saturate(abs(v.uv.y - glowCenter) / _DeformationBandWidth);
                float deformationStrength = _GlowProgress * deformationFalloff;

                float3 displacedPos = worldPos + worldNormal * (_BaseThickness * _CameraDistanceFactor);
                displacedPos += worldNormal * _DeformationAmplitude * deformationStrength;

                o.pos = UnityWorldToClipPos(displacedPos);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 baseColor;

                if (_HoverProgress < 0)
                {
                    if (i.uv.y <= _Progress)
                    {
                        baseColor = _ColorA;
                    }
                    else
                    {
                        float stripe = fmod(i.uv.y * _StripeFrequency, 1.0) < 0.5 ? 0.0 : 1.0;
                        baseColor = lerp(_ColorC, _ColorD, stripe);
                    }
                }
                else
                {
                    float minProgress = min(_Progress, _HoverProgress);
                    float maxProgress = max(_Progress, _HoverProgress);

                    if (i.uv.y <= minProgress)
                        baseColor = _ColorA;
                    else if (i.uv.y <= maxProgress)
                        baseColor = _ColorB;
                    else
                    {
                        float stripe = fmod(i.uv.y * _StripeFrequency, 1.0) < 0.5 ? 0.0 : 1.0;
                        baseColor = lerp(_ColorC, _ColorD, stripe);
                    }
                }

                // Glow visuel net
                float glowOffset = _GlowBandWidth * 0.5;
                float glowCenter = saturate(_Progress - glowOffset);
                float glowMask = step(abs(i.uv.y - glowCenter), _GlowBandWidth);
                float glowStrength = _GlowProgress * glowMask;

                // Tranches permanentes
                float progressMask = step(abs(i.uv.y - _Progress), _GlowBandWidth);
                float hoverMask = step(abs(i.uv.y - _HoverProgress), _GlowBandWidth);

                float totalGlow = glowStrength + progressMask + hoverMask;
                baseColor.rgb += _GlowColor.rgb * totalGlow;

                return baseColor;
            }
            ENDCG
        }
    }
}
