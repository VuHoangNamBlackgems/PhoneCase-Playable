Shader "DIY/MaskParticle_R_BIRP"
{
    Properties{ _MainTex("Soft Circle",2D)="white"{} _Strength("Strength",Range(0,2))=1 }
    SubShader{
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off ZTest Always Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask R
        Pass{
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float _Strength;
            fixed4 frag(v2f_img i):SV_Target{
                fixed a = tex2D(_MainTex, TRANSFORM_TEX(i.uv,_MainTex)).a * _Strength;
                return fixed4(a,a,a,a); // ghi vào kênh R
            }
            ENDCG
        }
    }
}
