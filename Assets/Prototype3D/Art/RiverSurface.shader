Shader "SpeedRovers/RiverSurface" {
 Properties { _Color("Deep water",Color)=(.035,.18,.24,1) }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 struct Input { float3 worldPos; };
 fixed4 _Color;
 void surf(Input IN,inout SurfaceOutputStandard o) {
 float2 p=IN.worldPos.xz;float t=_Time.y;
 float a=sin(p.x*.08+p.y*.05+t*.3),b=sin(p.x*.14-p.y*.09-t*.2);
 o.Normal=normalize(float3(a*.025,b*.025,1));
 o.Albedo=_Color.rgb*(.9+.1*sin(p.x*.05+p.y*.08+t*.4));o.Metallic=.2;o.Smoothness=.72;o.Alpha=1;
 }
 ENDCG
 } FallBack "Standard"
}
