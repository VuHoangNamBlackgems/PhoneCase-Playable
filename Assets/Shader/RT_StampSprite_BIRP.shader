Shader "DIY/RT_StampSprite_BIRP"
{
    Properties{
        _MainTex ("Base RT", 2D) = "black" {}
        _Stamp   ("Sticker", 2D) = "white" {}
        _Center  ("CenterUV", Vector) = (0.5,0.5,0,0)
        _Scale   ("ScaleUV",  Vector) = (0.2,0.2,0,0)
        _Rot     ("RotationRad", Float) = 0
        _Tint    ("Tint", Color) = (1,1,1,1)
    }
    SubShader{
        Pass{
            ZWrite Off ZTest Always Cull Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _Stamp;
            float4 _Center, _Scale; float _Rot; fixed4 _Tint;

            fixed4 frag(v2f_img i):SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);

                // to local UV of sticker
                float2 d = i.uv - _Center.xy;
                float c = cos(_Rot), s = sin(_Rot);
                float2 local = mul(float2x2(c,-s,s,c), d / _Scale.xy) + 0.5;

                if (local.x < 0 || local.x > 1 || local.y < 0 || local.y > 1)
                    return baseCol;

                fixed4 sc = tex2D(_Stamp, local) * _Tint;

                // RGB: blend theo alpha của sticker
                fixed3 outRGB = lerp(baseCol.rgb, sc.rgb, sc.a);

                // ALPHA: alpha-over (giữ phần đã có + phần mới)
                fixed outA = saturate(baseCol.a + sc.a - baseCol.a * sc.a);
                return fixed4(outRGB, outA);
            }
            ENDCG
        }
    }
    Fallback Off
}
