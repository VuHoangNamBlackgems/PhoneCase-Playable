// DIY/GlitterPiece_BIRP
Shader "DIY/GlitterPiece_BIRP" {
    Properties{
        _MainTex("Sprite", 2D) = "white" {}
        _Tint("Tint", Color) = (1,0.85,0.3,1)
        _Noise("Flicker Noise", 2D) = "gray" {}
        _Intensity("Emission", Range(0,6)) = 2
        _Flicker("Flicker Speed", Range(0,10)) = 2
    }
    SubShader{
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off Cull Back
        Blend One OneMinusSrcAlpha

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _Noise; float4 _MainTex_ST;
            fixed4 _Tint; float _Intensity, _Flicker;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 wNormal:TEXCOORD1; float3 wView:TEXCOORD2; };

            v2f vert(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.wNormal = UnityObjectToWorldNormal(v.normal);
                o.wView = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i):SV_Target{
                fixed4 s = tex2D(_MainTex, i.uv);      // sprite RGBA
                if (s.a < 0.01) discard;

                // Fresnel để hạt lóe theo góc nhìn
                half ndv = saturate(dot(normalize(i.wNormal), normalize(i.wView)));
                half fres = pow(1 - ndv, 3);

                // Flicker: noise lệch pha từng hạt + _Time
                float n = tex2D(_Noise, i.uv * 3).r;
                half tw = 0.6 + 0.4 * sin(_Time.y * _Flicker + n * 20);

                fixed3 col = s.rgb * _Tint.rgb;
                fixed3 emis = col * _Intensity * tw * (0.5 + 0.5 * fres);

                fixed4 outC;
                outC.rgb = col * s.a + emis; // nền + phát sáng
                outC.a   = s.a * _Tint.a;
                return outC;
            }
            ENDCG
        }
    }
}
