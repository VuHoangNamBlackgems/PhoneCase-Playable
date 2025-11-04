Shader "DIY/CaseComposite_BR"
{
    Properties
    {
        _MainTex    ("Base (case texture)", 2D) = "white" {}
        _PaintTex   ("PaintRT (from RenderTexture)", 2D) = "black" {}
        _MaskTex    ("Optional Mask", 2D) = "white" {}
        _PaintColor ("Paint Color", Color) = (0.2, 0.8, 0.2, 1)
        _Metallic   ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags{ "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PaintTex;
        sampler2D _MaskTex;
        fixed4 _PaintColor;
        half _Metallic, _Smoothness;

        struct Input { float2 uv_MainTex; };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseCol  = tex2D(_MainTex,  IN.uv_MainTex);

            // alpha của sơn được bạn vẽ vào PaintRT
            fixed4 paintRT  = tex2D(_PaintTex, IN.uv_MainTex);
            // optional: giới hạn vùng được phép sơn
            fixed  mask     = tex2D(_MaskTex,  IN.uv_MainTex).a;

            fixed  a = saturate(paintRT.a * mask);  // lượng sơn 0..1
            fixed3 mixed = lerp(baseCol.rgb, _PaintColor.rgb, a);

            o.Albedo     = mixed;
            o.Metallic   = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha      = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
