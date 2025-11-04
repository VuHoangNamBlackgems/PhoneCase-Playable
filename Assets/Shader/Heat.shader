Shader "DIY/CaseCompositeHeat_BIRP"
{
    Properties{
        _BaseMap   ("Base (case texture)", 2D) = "white" {}
        _PaintTex  ("PaintRT", 2D)             = "black" {}
        _BlurTex   ("BlurRT", 2D)              = "black" {}
        _HeatMask  ("HeatMaskRT", 2D)          = "black" {}
        _Metallic  ("Metallic", Range(0,1))    = 0
        _Smoothness("Smoothness", Range(0,1))  = 0.4
    }
    SubShader{
        Tags{ "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _BaseMap, _PaintTex, _BlurTex, _HeatMask;
        half _Metallic, _Smoothness;

        struct Input { float2 uv_BaseMap; };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv   = IN.uv_BaseMap;

            fixed4 baseC = tex2D(_BaseMap,  uv);
            fixed4 pC    = tex2D(_PaintTex, uv);   // sơn gốc (RGB màu, A độ phủ)
            fixed4 bC    = tex2D(_BlurTex,  uv);   // sơn đã blur
            fixed  heat  = tex2D(_HeatMask, uv).a; // nếu HeatMask dùng R8 thì đọc .r

            fixed3 painted = lerp(baseC.rgb, pC.rgb, pC.a);  // base -> paint
            fixed3 blurred = lerp(baseC.rgb, bC.rgb, bC.a);  // base -> blur
            fixed3 final   = lerp(painted,  blurred, heat);  // reveal blur theo mask

            o.Albedo     = final;
            o.Metallic   = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha      = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
