Shader "SpeedRovers/GoldCoin" {
 Properties { _MainTex("Texture",2D)="white"{} _Phase("Spin",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest Always
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 float _Phase;
 fixed4 frag(v2f_img i):SV_Target {
   float2 p=(i.uv-.5)*2;
   float c=cos(_Phase), face=max(.075,abs(c));
   p.x/=face;
   float r=length(p),aa=max(fwidth(r),.008);
   float alpha=1-smoothstep(.94-aa,.94+aa,r);
   float bevel=smoothstep(.72,.84,r)*(1-smoothstep(.91,.95,r));
   float light=saturate(.55+dot(normalize(float3(p*.65,.7)),normalize(float3(-.5,.7,1)))*.5);
   float3 gold=lerp(float3(.42,.17,.015),float3(1,.78,.21),light);
   gold+=bevel*.25;
   float ring=1-smoothstep(.012,.025,abs(r-.73)); gold-=ring*.22;
   float angle=atan2(p.y,p.x)+1.5708;
   float starRadius=lerp(.23,.49,pow(.5+.5*cos(angle*5),3));
   float star=1-smoothstep(starRadius-aa,starRadius+aa,r);
   gold=lerp(gold,float3(1,.87,.37)*(.8+.2*light),star);
   float glint=pow(saturate(1-abs(p.x+p.y*.35-sin(_Phase)*.7)),24)*.5;
   gold+=glint;
   gold*=lerp(.65,1,smoothstep(.08,.35,face));
   return float4(gold,alpha);
 }
 ENDCG }
 }
}
