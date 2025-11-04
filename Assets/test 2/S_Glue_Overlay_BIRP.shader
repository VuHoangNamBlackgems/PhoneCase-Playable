Shader "DIY/S_Glue_Overlay_BIRP"
{
    Properties
    {
        // RT do GlueStep vẽ (R = mask độ dày keo)
        _MaskTex        ("Glue Mask (R)", 2D) = "black" {}
        _MaskTexelSize  ("Mask Texel Size (1/pixel)", Vector) = (0.001,0.001,0,0)

        // Tuỳ chọn: đưa PaintTex vào để ẩn keo nơi đã sơn (A = lượng sơn)
        _PaintTex       ("Paint RT (RGB, A=amount)", 2D) = "black" {}
        _OnlyOnUnpainted("Show Glue Only On Unpainted", Float) = 1

        // Tham số look
        _CoatSmooth     ("Glue Smoothness", Range(0,1)) = 0.95
        _CoatTint       ("Sheen (RGB) + Strength(A)", Color) = (1,1,1,0.35)
        _FresnelPow     ("Fresnel Power", Range(0.5,8)) = 2.5

        // Viền highlight nhẹ theo cạnh mask (rẻ, đẹp)
        _EdgeWidthPx    ("Edge Width (pixels)", Range(0,5)) = 1.2
        _EdgeStrength   ("Edge Strength", Range(0,3)) = 1.0
        _EdgeColor      ("Edge Color", Color) = (1,1,1,1)

        _Intensity      ("Global Intensity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        // là lớp phủ -> không ghi depth để không che base
        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        // surface dùng Standard để có specular từ light, alpha:fade cho trong suốt
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MaskTex, _PaintTex;
        float4 _MaskTexelSize;                   // (1/w, 1/h, w, h)
        half   _OnlyOnUnpainted, _FresnelPow;
        half   _CoatSmooth, _EdgeWidthPx, _EdgeStrength, _Intensity;
        fixed4 _CoatTint, _EdgeColor;

        struct Input {
            float2 uv_MaskTex;
            float3 worldPos;
            float3 worldNormal;
        };

        // edge từ mask bằng gradient đơn giản
        float EdgeFromMask(float2 uv)
        {
            float2 px = _MaskTexelSize.xy * _EdgeWidthPx;
            float c  = tex2D(_MaskTex, uv).r;
            float cx = tex2D(_MaskTex, uv + float2(px.x,0)).r;
            float cy = tex2D(_MaskTex, uv + float2(0,px.y)).r;
            float dx = abs(cx - c);
            float dy = abs(cy - c);
            return saturate(max(dx, dy) * (1.5 + _EdgeWidthPx)); // scale nhẹ
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MaskTex;

            // mask keo
            float glue = tex2D(_MaskTex, uv).r;

            // ẩn keo chỗ đã sơn (nếu muốn)
            if (_OnlyOnUnpainted > 0.5)
            {
                float paintA = tex2D(_PaintTex, uv).a;
                glue *= (1.0 - saturate(paintA));
            }

            glue *= _Intensity;

            // viền
            float edge = 0.0;
            if (_EdgeStrength > 0.001 && _EdgeWidthPx > 0.01)
                edge = EdgeFromMask(uv) * _EdgeStrength;

            // lớp phủ: không đổi albedo để thấy nền, chỉ thêm spec + emission
            o.Albedo     = 0;                // giữ nguyên màu nền (overlay trong suốt)
            o.Metallic   = 0;                // keo không metallic
            o.Smoothness = _CoatSmooth * glue;

            // alpha điều khiển bởi mask (đừng quá cao để không "bệt")
            o.Alpha = saturate(glue * 0.9 + edge * 0.4);

            // Fresnel sheen trắng như keo ướt
            float3 V = normalize(_WorldSpaceCameraPos - IN.worldPos);
            float3 N = normalize(IN.worldNormal);
            float fres = pow(1.0 - saturate(dot(N, V)), _FresnelPow) * _CoatTint.a;

            // emission giúp highlight rõ ràng ngay cả khi light yếu
            o.Emission = (_CoatTint.rgb * fres * glue) + (_EdgeColor.rgb * edge);
        }
        ENDCG
    }

    FallBack Off
}
