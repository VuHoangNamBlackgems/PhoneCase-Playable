Shader "DIY/MaskTrail_R_BIRP"
{
    Properties{
        _MainTex("Width Gradient", 2D) = "white" {} // 1 dải trắng giữa, viền mềm
        _Alpha  ("Opacity", Range(0,2)) = 1
    }
    SubShader{
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off ZTest Always Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask R                    // CHỈ ghi kênh R của RT
        Pass{
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float _Alpha;
            fixed4 frag(v2f_img i):SV_Target{
                fixed a = tex2D(_MainTex, TRANSFORM_TEX(i.uv,_MainTex)).a * _Alpha;
                return fixed4(a, a, a, a); // R=alpha (các kênh khác bị ColorMask bỏ qua)
            }
            ENDCG
        }
    }
}
