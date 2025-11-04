Shader "DIY/PaintColorBrush_BIRP"{
 SubShader{
  Tags{ "RenderType"="Opaque" } ZWrite Off ZTest Always Cull Off
  Pass{
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"

   sampler2D _MainTex;     // current PaintRT
   sampler2D _BrushTex;    // PNG tròn mềm
   float4 _Params;         // (u, v, sizeUV, strength)
   float4 _Color;          // màu muốn tô

   fixed4 frag(v2f_img i):SV_Target{
     fixed4 cur = tex2D(_MainTex, i.uv);

     // brush mask mềm theo UV
     float2 d   = (i.uv - _Params.xy) / max(_Params.z, 1e-4);
     float  m   = 1 - smoothstep(0.5, 1.0, length(d));     // viền mượt
     float  a   = saturate(m * _Params.w);                 // alpha cọ

     // ghi màu vào RGB, tích lũy độ phủ vào A
     cur.rgb = saturate( lerp(cur.rgb, _Color.rgb, a) );
     cur.a   = saturate( cur.a + a );

     return cur;
   }
   ENDCG
  }
 }
}
