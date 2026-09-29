Shader "Pupverse/FoilPack"
{
 Properties { _Color("Collection colour", Color)=(.2,.8,1,1) }
 SubShader
 {
  Tags { "RenderType"="Opaque" }
  Pass
  {
   Cull Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 world:TEXCOORD2; };
   v2f vert(appdata_base v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o; }
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos-i.world);
    float rim=pow(1-abs(dot(n,v)),2);
    float band=pow(saturate(.5+.5*sin(i.uv.x*5+i.uv.y*2+v.x*9)),12);
    float crease=.5+.5*sin(i.uv.x*170+i.uv.y*12);
    float3 rainbow=.5+.5*cos(6.283*(i.uv.x+i.uv.y*.25+v.x*.7+float3(0,.33,.67)));
    float edge=step(i.uv.y,.09)+step(.93,i.uv.y);
    float3 silver=lerp(float3(.08,.10,.15),float3(.65,.73,.82),abs(dot(n,normalize(float3(-.45,.7,-1)))));
    float3 c=silver*.48+_Color.rgb*.16+band*(.65+rainbow*.3)+rim*_Color.rgb;
    c*=lerp(1,.48+crease*.52,saturate(edge));
    c+=pow(saturate(1-abs(i.uv.x-.06)*90),3)*_Color.rgb*.8;
    return fixed4(c,1);
   }
   ENDCG
  }
 }
}
