Shader "SpeedRovers/FoliageWind"
{
 Properties { _MainTex("Foliage",2D)="white" {} }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  Cull Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   sampler2D _MainTex;
   float4 _Region;
   float _Phase;
   Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
   fixed4 frag(Output i):SV_Target {
    float2 p=(i.uv-_Region.xy)/_Region.zw;
    // A single silhouette: trunk stays rooted while branches bend independently.
    float crown=smoothstep(.18,.75,p.y);
    float gust=sin(_Phase*.8)*.009+sin(_Phase*1.7+p.y*6)*.003;
    p.x+=gust*crown*crown;
    p.y+=sin(_Phase*2.3+p.x*16)*.002*crown;
    float inside=step(0,p.x)*step(p.x,1)*step(0,p.y)*step(p.y,1);
    fixed4 c=tex2D(_MainTex,_Region.xy+clamp(p,.001,.999)*_Region.zw);
    c.a*=inside;
    c.rgb*=1+sin(_Phase*1.3+p.x*15+p.y*9)*.018*crown;
    return c*i.color;
   }
   ENDCG
  }
 }
}
