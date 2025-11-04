Shader "DIY/MaskChannelBrush_BIRP (Props)"
{
 Properties{
   _BrushTex("Brush", 2D) = "white" {}
   [Enum(R,0,G,1,B,2,A,3)] _Channel("Channel", Float) = 0
 }
 SubShader{
  Tags{"RenderType"="Opaque"} ZWrite Off ZTest Always Cull Off
  Pass{
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex, _BrushTex;
   float4 _Params; int _Channel;         // _Params set bằng code
   fixed4 frag(v2f_img i):SV_Target{
     fixed4 c = tex2D(_MainTex, i.uv);
     float2 d=(i.uv-_Params.xy)/max(_Params.z,1e-4);
     float  a=saturate((1 - smoothstep(0.5,1.0,length(d))) * _Params.w);
     if (_Channel==0) c.r = saturate(max(c.r,a));
     else if (_Channel==1) c.g = saturate(max(c.g,a));
     else if (_Channel==2) c.b = saturate(max(c.b,a));
     else c.a = saturate(max(c.a,a));
     return c;
   }
   ENDCG
  }
 }
}
