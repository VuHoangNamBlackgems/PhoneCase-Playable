// DIY/DrierMix_BIRP
Shader "DIY/DrierMix_BIRP"{
    Properties{
        _PaintTex ("Paint (src)", 2D) = "black" {}
        _BlurTex  ("Blur (dst)",  2D) = "black" {}
        _MaskTex  ("HeatMask",    2D) = "black" {}
    }
    SubShader{
        Tags{ "RenderType"="Opaque" }
        ZWrite Off ZTest Always Cull Off
        Pass{
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _PaintTex, _BlurTex, _MaskTex;
            fixed4 frag(v2f_img i):SV_Target{
                fixed4 p = tex2D(_PaintTex, i.uv);   // RGB màu sơn, A = amount
                fixed3 b = tex2D(_BlurTex,  i.uv).rgb;
                fixed  m = tex2D(_MaskTex,  i.uv).r; // 0..1
                // Chỉ thay RGB theo mask, GIỮ NGUYÊN ALPHA của sơn
                fixed3 rgb = lerp(p.rgb, b, m);
                return fixed4(rgb, p.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
