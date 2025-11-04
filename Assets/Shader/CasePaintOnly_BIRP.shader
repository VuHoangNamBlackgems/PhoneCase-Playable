Shader "DIY/CasePaintOnly_NoMask_BIRP"
{
    Properties
    {
        // Base + Paint + Sticker như cũ
        _BaseMap   ("Base (case)", 2D) = "white" {}
        _PaintTex  ("Paint RT (RGB=color A=amount)", 2D) = "black" {}
        _StickerRT ("Sticker RT (RGB=color A=amount)", 2D) = "black" {}

        _PaintTint ("Paint Tint", Color) = (1,1,1,1)

        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _Metallic   ("Metallic",   Range(0,1)) = 0.0

        _StickerKeepUI   ("Sticker Keep UI Color", Range(0,1)) = 1
        _StickerOpacity  ("Sticker Opacity", Range(0,1)) = 1
        _StickerSmooth   ("Sticker Smoothness", Range(0,1)) = 1
        _StickerMetallic ("Sticker Metallic",   Range(0,1)) = 0

        // ===== Reveal Image (NEW) =====
        _RevealTex     ("Reveal Image (RGB)", 2D) = "black" {}
        _RevealMask    ("Reveal Mask (A, optional)", 2D) = "black" {}
        _RevealTint    ("Reveal Tint", Color) = (1,1,1,1)
        _RevealOpacity ("Reveal Opacity", Range(0,1)) = 1
        _RevealKeepUI  ("Reveal Keep UI Color", Range(0,1)) = 1    // 1 = unlit qua Emission, 0 = lit
        _UsePaintAForReveal ("Use _PaintTex.A as Reveal Mask", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _BaseMap, _PaintTex, _StickerRT;
        sampler2D _RevealTex, _RevealMask;

        fixed4 _PaintTint;
        fixed4 _RevealTint;

        half _Smoothness, _Metallic;
        half _StickerKeepUI, _StickerOpacity, _StickerSmooth, _StickerMetallic;

        half _RevealOpacity, _RevealKeepUI, _UsePaintAForReveal;

        struct Input { float2 uv_BaseMap; };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_BaseMap;

            // ===== 1) Base
            fixed3 baseCol = tex2D(_BaseMap, uv).rgb;

            // ===== 2) Paint (lit)
            fixed4 p = tex2D(_PaintTex, uv); // RGB = color, A = amount
            p.rgb *= _PaintTint.rgb;
            fixed3 colPaint = lerp(baseCol, p.rgb, p.a);

            // ===== 3) Reveal Image (mask = PaintA hoặc RevealMask.A)
            half  maskPaint = p.a;
            half  maskTex   = tex2D(_RevealMask, uv).a;
            half  rMask     = lerp(maskTex, maskPaint, _UsePaintAForReveal);  // chọn nguồn mask
            rMask = saturate(rMask * _RevealOpacity);

            fixed3 revealRGB = tex2D(_RevealTex, uv).rgb * _RevealTint.rgb;

            // 3a) Lit reveal: trộn trực tiếp vào albedo khi KeepUI=0
            fixed3 albedoAfterRevealLit = lerp(colPaint, revealRGB, rMask * (1 - _RevealKeepUI));

            // 3b) Unlit reveal: hạ albedo vùng có reveal rồi bù màu qua Emission khi KeepUI=1
            fixed3 albedoAfterReveal = lerp(
                albedoAfterRevealLit,
                colPaint * (1 - rMask),
                _RevealKeepUI
            );

            // ===== 4) Sticker (giữ logic cũ, chạy sau Reveal)
            fixed4 s = tex2D(_StickerRT, uv);    // RGB=color, A=amount
            half a = saturate(s.a * _StickerOpacity);
            fixed3 colLitSticker = s.rgb;

            fixed3 albedoWithLitSticker = lerp(albedoAfterReveal, colLitSticker, a * (1 - _StickerKeepUI));
            fixed3 albedoFinal = lerp(albedoWithLitSticker, albedoAfterReveal * (1 - a), _StickerKeepUI);

            o.Albedo     = albedoFinal;
            o.Emission   = 0;
            // Unlit phần Reveal:
            o.Emission  += revealRGB * (rMask * _RevealKeepUI);
            // Unlit phần Sticker:
            o.Emission  += s.rgb * (a * _StickerKeepUI);

            // Vật liệu (có thể giữ nguyên hoặc pha theo sticker như cũ)
            o.Smoothness = lerp(_Smoothness, _StickerSmooth, a);
            o.Metallic   = lerp(_Metallic,   _StickerMetallic, a);
            o.Alpha      = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
