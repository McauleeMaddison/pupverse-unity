Shader "Pupverse/CollectibleCardFoil"
{
 Properties
 {
  _MainTex("Original card artwork", 2D)="white" {}
  _FoilAmount("Foil strength", Range(0,1))=0
  _FoilTint("Foil metal", Color)=(.65,.85,1,1)
  _Prismatic("Prismatic finish", Range(0,1))=0
  _FoilTime("Shimmer time", Float)=0
  _FoilSeed("Card pattern", Float)=0
 }
 SubShader
 {
  Tags { "RenderType"="Opaque" }
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   float4 _MainTex_ST;
   half4 _FoilTint;
   float _FoilAmount, _Prismatic, _FoilTime, _FoilSeed;
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 view:TEXCOORD1; };
   v2f vert(appdata_base v)
   {
    v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);
    o.view=ObjSpaceViewDir(v.vertex);return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 art=tex2D(_MainTex,i.uv);
    float3 view=normalize(i.view);
    float phase=i.uv.x*5+i.uv.y*3+view.x*8+view.y*4+_FoilTime*.65+_FoilSeed;
    float sheen=pow(saturate(.5+.5*sin(phase)),14);
    float3 rainbow=.5+.5*cos(6.283*(i.uv.x*.7+i.uv.y*.4+view.x*.6+_FoilTime*.025+float3(0,.33,.67)));
    float edge=1-smoothstep(.015,.07,min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y)));
    // Keep the lower stat rows and title legible, with stronger foil over portrait and border.
    float mask=max(edge,lerp(.18,.75,smoothstep(.42,.64,i.uv.y)));
    float linePhase=(i.uv.x+i.uv.y*.35)*180;
    float etched=(.5+.5*sin(linePhase))*saturate(1-fwidth(linePhase));
    float3 metal=lerp(_FoilTint.rgb,rainbow,_Prismatic*.72);
    float intensity=_FoilAmount*mask;
    float3 colour=art.rgb+intensity*(sheen*(metal*.48+.3)+edge*metal*.08+etched*sheen*metal*.08);
    return fixed4(colour,art.a);
   }
   ENDCG
  }
 }
 Fallback "Unlit/Texture"
}
