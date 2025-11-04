Shader "DIY/BlurBrushStamp_BIRP"
{
    Properties{
        _MainTex        ("Paint RT", 2D) = "black" {}

        _CenterUV       ("Center UV", Vector) = (0.5,0.5,0,0)
        _RadiusUV       ("Radius UV", Float)  = 0.1
        _Strength       ("Blur Strength", Range(0,1)) = 0.85

        // Loang mở rộng
        _BleedStepPx    ("Bleed Step (px)", Float) = 6.0
        _BleedExtentPx  ("Bleed Extent (px)", Float) = 48.0
        _BleedStrength  ("Bleed Strength", Range(0,1)) = 0.6

        _EdgeSoft       ("Edge Softness (UV)", Range(0.0005,0.05)) = 0.01

        // RGB wide bleed
        _BleedColorWide ("Bleed Color Wide (0/1)", Float) = 1
        _ColorBleedScale("Color Bleed Scale", Range(0,2)) = 1
    }

    SubShader{
        Tags{ "Queue"="Transparent" "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // (1/w,1/h,w,h)

            float2 _CenterUV;
            float  _RadiusUV;
            float  _Strength;

            float  _BleedStepPx;
            float  _BleedExtentPx;
            float  _BleedStrength;

            float  _EdgeSoft;

            float  _BleedColorWide;
            float  _ColorBleedScale;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f      { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }

            float3 SmallBlurRGB(sampler2D tex, float2 uv, float2 texel)
            {
                float3 acc = 0;
                acc += tex2D(tex, uv + float2(-texel.x, -texel.y)).rgb;
                acc += tex2D(tex, uv + float2( 0,        -texel.y)).rgb;
                acc += tex2D(tex, uv + float2( texel.x,  -texel.y)).rgb;

                acc += tex2D(tex, uv + float2(-texel.x, 0)).rgb;
                acc += tex2D(tex, uv).rgb;
                acc += tex2D(tex, uv + float2( texel.x, 0)).rgb;

                acc += tex2D(tex, uv + float2(-texel.x,  texel.y)).rgb;
                acc += tex2D(tex, uv + float2( 0,         texel.y)).rgb;
                acc += tex2D(tex, uv + float2( texel.x,   texel.y)).rgb;

                return acc / 9.0;
            }

            float DilateA_Rings(sampler2D tex, float2 uv, float2 texel, float stepPx, float extentPx)
            {
                float2 stepUV = texel * stepPx;
                int maxRings = 12;
                int rings = (int)ceil(saturate(extentPx / max(1e-5, stepPx)));
                rings = clamp(rings, 1, maxRings);

                float aMax = tex2D(tex, uv).a;

                [loop]
                for (int r = 1; r <= rings; r++)
                {
                    float2 ofs = stepUV * r;
                    // 8 hướng
                    aMax = max(aMax, tex2D(tex, uv + float2( ofs.x, 0)).a);
                    aMax = max(aMax, tex2D(tex, uv + float2(-ofs.x, 0)).a);
                    aMax = max(aMax, tex2D(tex, uv + float2(0,  ofs.y)).a);
                    aMax = max(aMax, tex2D(tex, uv + float2(0, -ofs.y)).a);
                    aMax = max(aMax, tex2D(tex, uv + ofs).a);
                    aMax = max(aMax, tex2D(tex, uv - ofs).a);
                    aMax = max(aMax, tex2D(tex, uv + float2( ofs.x,-ofs.y)).a);
                    aMax = max(aMax, tex2D(tex, uv + float2(-ofs.x, ofs.y)).a);
                }
                return aMax;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // mask brush gốc (cho trộn màu)
                float2 duv   = uv - _CenterUV;
                float  distN = length(duv) / max(_RadiusUV, 1e-6);
                float inner  = saturate(1.0 - smoothstep(1.0 - _EdgeSoft, 1.0, distN));

                fixed4 src = tex2D(_MainTex, uv);

                // blur màu (nhẹ)
                float2 one = _MainTex_TexelSize.xy;
                float3 blurRGB = SmallBlurRGB(_MainTex, uv, one);

                // vùng loang mở rộng ra ngoài brush
                float pxToUV = max(one.x, one.y);
                float bleedGrowUV = _BleedExtentPx * pxToUV;
                float outerEdge   = 1.0 + (bleedGrowUV / max(_RadiusUV, 1e-6));
                float innerBleed  = saturate(1.0 - smoothstep(1.0 - _EdgeSoft, outerEdge, distN));

                // dilate alpha nhiều vòng
                float aDilate = DilateA_Rings(_MainTex, uv, one, _BleedStepPx, _BleedExtentPx);
                float bleedK  = saturate(_BleedStrength * _Strength);

                // alpha: dùng mask mở rộng
                float newA = lerp(src.a, aDilate, bleedK * innerBleed);

                // màu: tuỳ chọn dùng mask mở rộng hay không
                float rgbMask   = lerp(inner, innerBleed, saturate(_BleedColorWide));
                float rgbAmount = saturate(_Strength * rgbMask * max(1.0, _ColorBleedScale * 0.5));
                float3 newRGB   = lerp(src.rgb, blurRGB, rgbAmount);

                return float4(newRGB, newA);
            }
            ENDCG
        }
    }
}
