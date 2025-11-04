Shader "DIY/SeparableBlur_BIRP"
{
    Properties{
        _MainTex ("RT", 2D) = "black" {}
        _TexelStep ("Texel Step (uv)", Vector) = (1,0,0,0) // (x,y)=hướng & độ lớn
        _RadiusPx ("Radius (px)", Float) = 16
    }
    SubShader{
        Tags{ "Queue"="Transparent" } Cull Off ZWrite Off ZTest Always
        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float2 _TexelStep; float _RadiusPx;

            struct v2f{ float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }

            // Gaussian 1D, 9 taps (nhẹ) — có thể tăng 13-15 taps nếu cần mịn hơn
            fixed4 frag(v2f i):SV_Target{
                float2 stepUV = _TexelStep * _MainTex_TexelSize.xy * _RadiusPx;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.227027f; // w0
                c += tex2D(_MainTex, i.uv + stepUV*1.0) * 0.1945946f;
                c += tex2D(_MainTex, i.uv - stepUV*1.0) * 0.1945946f;
                c += tex2D(_MainTex, i.uv + stepUV*2.0) * 0.1216216f;
                c += tex2D(_MainTex, i.uv - stepUV*2.0) * 0.1216216f;
                c += tex2D(_MainTex, i.uv + stepUV*3.0) * 0.054054f;
                c += tex2D(_MainTex, i.uv - stepUV*3.0) * 0.054054f;
                c += tex2D(_MainTex, i.uv + stepUV*4.0) * 0.016216f;
                c += tex2D(_MainTex, i.uv - stepUV*4.0) * 0.016216f;
                return c;
            }
            ENDCG
        }
    }
}
