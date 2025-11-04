Shader "DIY/S_Glue_Overlay_RimOnly_Add"
{
    Properties
    {
        // RT keo do bước Glue vẽ (R = mask)
        _MaskTex        ("Glue Mask (R)", 2D) = "black" {}
        _MaskTexelSize  ("Mask Texel Size (1/pixel)", Vector) = (0.001,0.001,0,0)

        // (tùy chọn) RT sơn: dùng A để ẩn keo nơi đã sơn
        _PaintTex       ("Paint RT (A=amount)", 2D) = "black" {}
        _OnlyOnUnpainted("Show Glue Only On Unpainted", Float) = 1

        // Look
        _CoatSmooth     ("Rim Smoothness", Range(0,1)) = 0.95
        _CoatTint       ("Rim Sheen (RGB)+Strength(A)", Color) = (1,1,1,0.30)
        _FresnelPow     ("Fresnel Power", Range(0.5,8)) = 2.5

        // Điều khiển ring (viền)
        _EdgeWidthPx    ("Ring Width (px)", Range(0.2,6)) = 1.5
        _EdgeBoost      ("Ring Boost", Range(0,3)) = 1.0
        _EdgeThresh     ("Ring Threshold", Range(0,0.5)) = 0.04

        // Giữ tương thích: _Percent = Intensity
        _Percent        ("Global Intensity", Range(0,2)) = 1
    }

    SubShader
    {
        // Vẽ sau mesh case, không ghi depth, ADDITIVE => không bao giờ làm tối nền
        Tags { "Queue"="Transparent+110" "RenderType"="Transparent" }
        ZWrite Off
        Cull Back
        Blend One One

        CGPROGRAM
        #pragma surface surf Standard keepalpha
        #pragma target 3.0

        sampler2D _MaskTex, _PaintTex;
        float4 _MaskTexelSize;                 // (1/w,1/h,w,h)
        half _OnlyOnUnpainted, _FresnelPow, _CoatSmooth;
        half _EdgeWidthPx, _EdgeBoost, _EdgeThresh, _Percent;
        fixed4 _CoatTint;

        struct Input {
            float2 uv_MaskTex;
            float3 worldPos;
            float3 worldNormal;
        };

        // Tạo "rim" từ chênh lệch mask theo X/Y, có độ dày (EdgeWidthPx)
        float EdgeMask(float2 uv)
        {
            float2 px = _MaskTexelSize.xy * _EdgeWidthPx;

            float c   = tex2D(_MaskTex, uv).r;
            float cx1 = tex2D(_MaskTex, uv + float2( px.x, 0)).r;
            float cx2 = tex2D(_MaskTex, uv + float2(-px.x, 0)).r;
            float cy1 = tex2D(_MaskTex, uv + float2(0,  px.y)).r;
            float cy2 = tex2D(_MaskTex, uv + float2(0, -px.y)).r;

            float dx = max(abs(cx1 - c), abs(cx2 - c));
            float dy = max(abs(cy1 - c), abs(cy2 - c));
            float g  = max(dx, dy);

            // Ngưỡng + boost để rim gọn, chỉ lóe ở mép
            g = saturate((g - _EdgeThresh) * (4.0 + _EdgeWidthPx)) * _EdgeBoost;
            return g;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MaskTex;

            // Mask keo gốc
            float mask = tex2D(_MaskTex, uv).r;

            // Ẩn keo ở nơi đã sơn (nếu bật)
            if (_OnlyOnUnpainted > 0.5)
                mask *= (1.0 - saturate(tex2D(_PaintTex, uv).a));

            // Rim ở mép (tâm = 0)
            float rim = EdgeMask(uv) * mask * _Percent;

            // Overlay add: không đổi albedo/alpha, chỉ cộng sáng + spec ở viền
            o.Albedo     = 0;
            o.Metallic   = 0;
            o.Smoothness = _CoatSmooth * rim;
            o.Alpha      = 0;

            // Fresnel để viền trắng bóng theo góc nhìn
            float3 V = normalize(_WorldSpaceCameraPos - IN.worldPos);
            float3 N = normalize(IN.worldNormal);
            float fres = pow(1.0 - saturate(dot(N, V)), _FresnelPow) * _CoatTint.a;

            // Chỉ viền sáng; tâm trong suốt
            o.Emission = _CoatTint.rgb * fres * rim;
        }
        ENDCG
    }
    FallBack Off
}
