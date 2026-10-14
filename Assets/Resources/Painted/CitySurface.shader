Shader "SpeedRovers/CitySurface"
{
 Properties { _MainTex ("Asphalt", 2D) = "white" {} }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  Cull Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
   sampler2D _MainTex;
   float _Distance, _Lane, _Horizon, _Speed;
   float4 _Haze, _Road, _Marking;
   Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
   float band(float value,float center,float width,float aa) { return 1-smoothstep(width-aa,width+aa,abs(value-center)); }
   fixed4 frag(Output i):SV_Target {
    float2 pixel=float2(i.uv.x*540,_Horizon+(1-i.uv.y)*(960-_Horizon));
    float d=max(.0001,(pixel.y-_Horizon)/(885-_Horizon));
    float farScale=27.0/497.0;
    float z=27/lerp(farScale,1,d)-27;
    float world=_Distance+z;
    float halfWidth=7+403*d;
    float bend=(sin((_Distance+z*.55)/240)*60+sin((_Distance+z)/340)*35)*(1-d)*(1-d);
    float center=270+bend-_Lane*halfWidth*.63;
    float x=(pixel.x-center)/halfWidth;
    float ax=abs(x), aa=max(fwidth(x),.001);
    float dz=max(fwidth(world),.01);
    float junctionOffset=abs(frac((world+70)/180)*180-90);
    float junction=1-smoothstep(12-dz,12+dz,junctionOffset);
    float roadMask=max(1-smoothstep(1-aa,1+aa,ax),junction);
    float2 uv=float2(x*1.5,world*.12);
    float3 grain=tex2D(_MainTex,uv).rgb;
    // Velocity stretches the grain only along travel, preserving road geometry.
    float motion=saturate((_Speed-100)/330)*.045;
    grain=(grain+tex2D(_MainTex,uv+float2(0,motion)).rgb+tex2D(_MainTex,uv-float2(0,motion)).rgb)/3;
    float tracks=exp(-pow((frac((x+1)*1.5)-.32)*9,2))*.035;
    float3 road=grain*_Road.rgb*(.88-tracks);
    // Broad grazing light and subtle worn wheel tracks give asphalt depth.
    road+=float3(.035,.027,.018)*pow(saturate(1-abs(x*.75)),4);
    float slab=frac(world/2.5);
    float joint=1-smoothstep(.025,.025+dz/2.5,min(slab,1-slab));
    float lateralJoint=band(frac(ax*8),.5,.012,aa*8);
    float3 pavement=float3(.40,.39,.36)*(1-joint*.18-lateralJoint*.07);
    pavement*=lerp(.68,1,smoothstep(1,1.08,ax));
    float curb=band(ax,1.014,.014,aa);
    pavement=lerp(pavement,float3(.63,.60,.53),curb);
    float3 ground=grain*float3(.35,.37,.36);
    float sidewalk=1-smoothstep(1.38-aa,1.38+aa,ax);
    float3 col=lerp(ground,pavement,sidewalk);
    col=lerp(col,road,roadMask);
    float dash=1-smoothstep(3-dz,3+dz,frac(world/7)*7);
    float lines=(band(x,-.333,.007,aa)+band(x,.333,.007,aa))*dash;
    float edges=band(ax,.975,.006,aa);
    col=lerp(col,_Marking.rgb,saturate(lines)*(1-junction)*.9);
    col=lerp(col,float3(.82,.80,.72),edges*(1-junction)*.85);
    float crossing=band(junctionOffset,10.25,1.25,dz);
    float zebra=1-smoothstep(.055-aa,.055+aa,abs(frac((x+.0875)/.175)*.175-.0875));
    col=lerp(col,float3(.78,.77,.71),crossing*zebra*(1-smoothstep(.94,1,ax))*.9);
    float workIndex=floor((world-420)/850);
    float workPos=world-(420+workIndex*850);
    float workLane=fmod(workIndex,2)<.5?1:-1;
    float work=step(0,workIndex)*step(0,workPos)*(1-step(42,workPos))*band(x,workLane*.63,.27,aa);
    col=lerp(col,grain*float3(.36,.32,.27),work*.9);
    float fog=pow(saturate(1-d),4);
    col=lerp(col,_Haze.rgb,fog*.65);
    // Fade over world distance into the existing waterfront approach.
    float alpha=1-smoothstep(4800,5160,world);
    return fixed4(col,alpha);
   }
   ENDCG
  }
 }
}
