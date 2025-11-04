Shader "Hidden/DIY/ClearChannel_BIRP"
{
 SubShader{ Tags{"RenderType"="Opaque"} ZWrite Off ZTest Always Cull Off
  Pass{ CGPROGRAM
   #pragma vertex vert_img  #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; int _Channel; float _Value;
   fixed4 frag(v2f_img i):SV_Target{
     fixed4 c = tex2D(_MainTex, i.uv);
     if (_Channel==0) c.r = _Value;
     else if (_Channel==1) c.g = _Value;
     else if (_Channel==2) c.b = _Value;
     else c.a = _Value;
     return c;
   }
  ENDCG}
 }
}
