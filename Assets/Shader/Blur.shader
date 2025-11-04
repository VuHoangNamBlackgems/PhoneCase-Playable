Shader "DIY/RT_MaskedSeparableBlur_BIRP"
{
    Properties{
        _MainTex("Src", 2D) = "black" {}
        _BrushTex("Soft Brush (A)", 2D) = "white" {}
        _BrushUV("Brush UV", Vector) = (0.5,0.5,0,0)
        _BrushRadiusUV("Brush Radius (UV)", Range(0.002,0.25)) = 0.06
        _Strength("Blur Strength", Range(0,1)) = 1
        _Radius("Blur Radius(px scale)", Range(0.5,3)) = 1.5
        _Direction("Dir (1,0)=H (0,1)=V", Vector) = (1,0,0,0)
    }
    SubShader{
        Tags{ "RenderType"="Opaque" } ZWrite Off ZTest Always Cull Off
        Pass{
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _BrushTex;
            float4 _MainTex_TexelSize;
            float2 _BrushUV;
            float  _BrushRadiusUV, _Strength, _Radius;
            float2 _Direction;

            static const float w0=0.227027, w1=0.194594, w2=0.121621, w3=0.054054, w4=0.016216;

            fixed4 frag(v2f_img i):SV_Target{
                float2 uv = i.uv;
                fixed4 cur = tex2D(_MainTex, uv);

                // mask chỉ trong vùng cọ
                float2 d = (uv - _BrushUV) / max(_BrushRadiusUV, 1e-5);
                float  mRadial = 1.0 - smoothstep(0.5, 1.0, length(d));
                float  mBrush  = tex2D(_BrushTex, d*0.5+0.5).a;
                float  m = saturate(mRadial * mBrush) * _Strength;

                float2 stepUV = _MainTex_TexelSize.xy * _Radius * _Direction;

                fixed4 blur = tex2D(_MainTex, uv) * w0;
                blur += tex2D(_MainTex, uv + stepUV*1) * w1;
                blur += tex2D(_MainTex, uv - stepUV*1) * w1;
                blur += tex2D(_MainTex, uv + stepUV*2) * w2;
                blur += tex2D(_MainTex, uv - stepUV*2) * w2;
                blur += tex2D(_MainTex, uv + stepUV*3) * w3;
                blur += tex2D(_MainTex, uv - stepUV*3) * w3;
                blur += tex2D(_MainTex, uv + stepUV*4) * w4;
                blur += tex2D(_MainTex, uv - stepUV*4) * w4;

                // lerp cả RGB và A để mép sơn mềm thật
                return lerp(cur, blur, m);
            }
            ENDCG
        }
    }
}
