Shader "DIY/RT_ApplyBrush_BIRP"
{
    Properties{
        _MainTex ("Prev RT", 2D) = "black" {}
        _BrushTex("Soft Brush (A)", 2D) = "white" {}
        _BrushColor("Brush Color RGBA", Color) = (0,1,0,1)
        _BrushUV("Brush UV", Vector) = (0.5,0.5,0,0)
        _BrushRadiusUV("Brush Radius (UV)", Range(0.002,0.25)) = 0.06
        _PaintOpacity("Paint Opacity", Range(0,1)) = 1
        _DoPaint("Do Paint", Float) = 0
    }
    SubShader{
        Tags{ "RenderType"="Opaque" } ZWrite Off ZTest Always Cull Off
        Pass{
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _BrushTex;
            float4 _BrushColor;
            float2 _BrushUV;
            float  _BrushRadiusUV, _PaintOpacity, _DoPaint;

            fixed4 frag(v2f_img i):SV_Target{
                float2 uv = i.uv;
                fixed4 cur = tex2D(_MainTex, uv);

                // mask tròn mềm * brush alpha
                float2 d = (uv - _BrushUV) / max(_BrushRadiusUV, 1e-5);
                float  mRadial = 1.0 - smoothstep(0.5, 1.0, length(d));
                float  mBrush  = tex2D(_BrushTex, d*0.5+0.5).a;
                float  m = saturate(mRadial * mBrush) * step(0.5, _DoPaint);

                float  aAdd = m * _PaintOpacity;
                float3 col  = lerp(cur.rgb, _BrushColor.rgb, aAdd);
                float  a    = saturate(cur.a + aAdd);

                return float4(col, a);   // (RGB = màu sơn, A = lượng sơn)
            }
            ENDCG
        }
    }
}
