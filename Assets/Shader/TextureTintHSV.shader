Shader "Hippie/Unlit/TextureTintHSV"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color  ("Tint Color (rgba)", Color) = (1,1,1,1)

        [KeywordEnum(Multiply,Screen,Add)] _BLEND("Blend Mode", Float) = 0

        _Hue("Hue Shift (-1..1)", Range(-1,1)) = 0
        _Sat("Saturation (-1..1)", Range(-1,1)) = 0
        _Val("Value (-1..1)", Range(-1,1)) = 0
        _Contrast("Contrast (0..2)", Range(0,2)) = 1
        _Brightness("Brightness (0..2)", Range(0,2)) = 1

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Toggle] _ZWrite("ZWrite", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        LOD 100

        Cull [_Cull]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        // Dùng premultiplied alpha để trộn đẹp hơn
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma shader_feature_local _BLEND_MULTIPLY _BLEND_SCREEN _BLEND_ADD

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Hue, _Sat, _Val, _Contrast, _Brightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // RGB <-> HSV helpers (nhẹ, đủ xài mobile)
            half3 rgb2hsv(half3 c) {
                half4 K = half4(0., -1./3., 2./3., -1.);
                half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
                half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
                half d = q.x - min(q.w, q.y);
                half e = 1e-10;
                half3 hsv;
                hsv.x = abs(q.z + (q.w - q.y) / (6.*d + e));
                hsv.y = d / (q.x + e);
                hsv.z = q.x;
                return hsv;
            }

            half3 hsv2rgb(half3 c) {
                half3 rgb = clamp(abs(frac(c.x + half3(0., 2./6., 4./6.)) * 6. - 3.) - 1., 0., 1.);
                return c.z * lerp(half3(1.,1.,1.), rgb, c.y);
            }

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // HSV shift trên màu ảnh gốc
                half3 hsv = rgb2hsv(tex.rgb);
                hsv.x = frac(hsv.x + _Hue);               // xoay hue
                hsv.y = saturate(hsv.y * (1 + _Sat));     // tăng/giảm saturation
                hsv.z = saturate(hsv.z * (1 + _Val));     // tăng/giảm value
                half3 baseRGB = hsv2rgb(hsv);

                // Áp tint theo mode
                half3 tint = _Color.rgb;

                #if defined(_BLEND_SCREEN)
                    half3 outRGB = 1 - (1 - baseRGB) * (1 - tint);
                #elif defined(_BLEND_ADD)
                    half3 outRGB = saturate(baseRGB + tint);
                #else // _BLEND_MULTIPLY (mặc định)
                    half3 outRGB = baseRGB * tint;
                #endif

                // Contrast & brightness (đơn giản, nhanh)
                outRGB = (outRGB - 0.5) * _Contrast + 0.5;
                outRGB *= _Brightness;

                // Alpha theo ảnh * alpha tint, premultiply để Blend One/OneMinusSrcAlpha
                half a = tex.a * _Color.a;
                return fixed4(outRGB * a, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
