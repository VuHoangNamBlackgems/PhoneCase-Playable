Shader "DIY/CaseCompositeWithGlitterRT_BIRP"
{
    Properties{
        _BaseMap   ("Base (case)", 2D) = "white" {}
        _PaintTex  ("Paint RT (RGB=color A=amount)", 2D) = "black" {}
        _MaskPack  ("MaskPack RT (R=Glue G=Heat B=FX A=Res)", 2D) = "black" {}
        _GlitterRT ("Glitter RT (A=amount)", 2D) = "black" {}
        _FXNoise   ("Noise", 2D) = "gray" {}

        _PaintTint ("Paint Tint", Color) = (1,1,1,1)

        _GlueTint  ("Glue Tint (RGB) & Strength(A)", Color) = (1,1,1,0.06)
        _BaseSmooth("Base Smoothness", Range(0,1)) = 0.25
        _GlueSmooth("Glue Smoothness", Range(0,1)) = 0.85
        _Metallic  ("Metallic", Range(0,1)) = 0

        _GlitColor ("Glitter Color", Color) = (1,0.9,0.5,1)
        _GlitIntensity ("Glitter Intensity", Range(0,2)) = 0.8
        _GlitTiling ("Glitter Noise Tiling", Range(0.5,10)) = 3
        _GlitSpeed  ("Glitter Flicker Speed", Range(0,5)) = 1.2
    }
    SubShader{
        Tags{ "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _BaseMap,_PaintTex,_MaskPack,_GlitterRT,_FXNoise;
        fixed4 _PaintTint,_GlueTint,_GlitColor;
        half _BaseSmooth,_GlueSmooth,_Metallic,_GlitIntensity,_GlitTiling,_GlitSpeed;

        struct Input{
            float2 uv_BaseMap;
            float3 viewDir;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_BaseMap;

            // Base & Paint
            fixed4 baseC = tex2D(_BaseMap, uv);
            fixed4 p     = tex2D(_PaintTex, uv);
            p.rgb *= _PaintTint.rgb;
            fixed3 col = lerp(baseC.rgb, p.rgb, p.a);

            // Pack masks
            fixed4 m = tex2D(_MaskPack, uv);

            // Glue coat
            half smooth = _BaseSmooth;
            {
                fixed g = m.r;
                fixed3 coat = col*(1 - _GlueTint.a) + _GlueTint.rgb * _GlueTint.a;
                col    = lerp(col, coat, g);
                smooth = lerp(_BaseSmooth, _GlueSmooth, g);
            }

            // Glitter from RT (no spawn objects)
            fixed gMask = tex2D(_GlitterRT, uv).a;               // 0..1 vùng có hạt
            if (gMask > 0.001)
            {
                // nhiễu + nhấp nháy + Fresnel
                fixed3 n = tex2D(_FXNoise, uv * _GlitTiling + _Time.y*_GlitSpeed).rgb;
                half fres = pow(1 - saturate(dot(normalize(IN.viewDir),
                                                normalize(IN.worldNormal))), 4);
                half star = saturate(n.r*1.8 - 0.8);             // ngưỡng tạo “lấp lánh”
                fixed3 glint = _GlitColor.rgb * star * fres * _GlitIntensity * gMask;

                // dùng Emission để “sáng” mà không bệt màu base
                o.Emission += glint;
                // (tuỳ chọn) thêm chút màu vào albedo:
                // col += glint*0.2;
            }

            o.Albedo     = col;
            o.Smoothness = smooth;
            o.Metallic   = _Metallic;
            o.Alpha      = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
