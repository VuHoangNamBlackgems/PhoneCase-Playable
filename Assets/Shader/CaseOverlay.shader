Shader "DIY/ProjectRT_OnMesh"
{
    Properties
    {
        _BaseMap    ("Base Case Texture", 2D) = "white" {}
        _BaseColor  ("Base Tint", Color) = (1,1,1,1)
        _OverlayRT  ("Overlay RT", 2D) = "black" {}
        _Tint       ("Overlay Tint", Color) = (1,1,1,1)
        _Intensity  ("Intensity", Range(0,4)) = 1
        _EdgeSoft   ("Edge Softness", Range(0,0.2)) = 0.02
        _AngleSoft  ("Angle Softness", Range(0,1)) = 0.3   // giảm ở rìa góc xiên

        [KeywordEnum(Alpha, Additive, Opaque)] _BlendMode("Blend Mode", Float) = 0
    }

    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #pragma shader_feature_local _BLENDMODE_ALPHA _BLENDMODE_ADDITIVE _BLENDMODE_OPAQUE

            sampler2D _BaseMap; float4 _BaseMap_ST;
            sampler2D _OverlayRT;
            float4 _BaseColor, _Tint;
            float  _Intensity, _EdgeSoft, _AngleSoft;

            // set từ C#:
            float4x4 _ProjectorVP;      // proj * view của DrawCam
            float3   _ProjectorFwd;     // forward của DrawCam (world)
            float3   _ProjectorPos;     // position của DrawCam (world)

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos    : SV_Position;
                float3 wpos   : TEXCOORD0;
                float3 wnorm  : TEXCOORD1;
                float2 uv     : TEXCOORD2;
            };

            v2f vert(appdata v){
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.wpos  = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wnorm = UnityObjectToWorldNormal(v.normal);
                o.uv    = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            inline float3 ProjectorClip(float3 wpos, out float2 uv, out float clipZ)
            {
                float4 cp = mul(_ProjectorVP, float4(wpos,1));
                clipZ = cp.z / cp.w;                          // 0..1 trước camera
                float2 ndc = cp.xy / max(cp.w, 1e-6);         // -1..1
                uv = ndc * 0.5 + 0.5;                         // 0..1
                return cp.xyz;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_BaseMap, i.uv) * _BaseColor;

                // Project
                float2 puv; float clipZ;
                ProjectorClip(i.wpos, puv, clipZ);

                // nằm ngoài frustum hoặc phía sau camera → không chiếu
                float inFrustum = step(0.0, puv.x) * step(0.0, puv.y) *
                                  step(puv.x, 1.0) * step(puv.y, 1.0) *
                                  step(0.0, clipZ)  * step(clipZ, 1.0);

                // edge soften (mềm ở viền UV)
                float edge =
                    smoothstep(0.0, _EdgeSoft, puv.x) *
                    smoothstep(0.0, _EdgeSoft, puv.y) *
                    smoothstep(0.0, _EdgeSoft, 1.0 - puv.x) *
                    smoothstep(0.0, _EdgeSoft, 1.0 - puv.y);

                // angle soften: nếu mặt nghiêng nhiều so với hướng projector thì giảm
                float3 n = normalize(i.wnorm);
                float ang = saturate(dot(n, normalize(_ProjectorFwd)));
                ang = smoothstep(0.0, _AngleSoft, ang);

                float mask = inFrustum * edge * ang;

                fixed4 over = tex2D(_OverlayRT, puv) * _Tint;
                // nếu RT không có alpha, lấy alpha từ độ sáng
                float a = max(over.a, max(over.r, max(over.g, over.b)));
                a *= mask;
                over.rgb *= _Intensity;

                fixed4 outCol;
                #if defined(_BLENDMODE_ADDITIVE)
                    outCol.rgb = baseCol.rgb + over.rgb * a;
                    outCol.a   = saturate(baseCol.a + a*(1 - baseCol.a));
                #elif defined(_BLENDMODE_OPAQUE)
                    float m = step(0.01, a);
                    outCol  = lerp(baseCol, fixed4(over.rgb,1), m);
                #else // Alpha
                    outCol.rgb = lerp(baseCol.rgb, over.rgb, a);
                    outCol.a   = saturate( lerp(baseCol.a, 1.0, a) );
                #endif

                return outCol;
            }
            ENDCG
        }
    }
    FallBack Off
}
