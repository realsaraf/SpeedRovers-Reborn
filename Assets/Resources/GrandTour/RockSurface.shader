Shader "SpeedRovers/WorldRock" {
 Properties {
  _MainTex ("Rock detail", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex;
  fixed4 _Color;
  struct Input { float3 worldPos; float3 worldNormal; };
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float3 w=pow(abs(IN.worldNormal),4);w/=max(.0001,w.x+w.y+w.z);
   float3 p=IN.worldPos*.18;
   fixed3 tex=tex2D(_MainTex,p.zy).rgb*w.x+tex2D(_MainTex,p.xz).rgb*w.y+tex2D(_MainTex,p.xy).rgb*w.z;
   fixed3 macro=tex2D(_MainTex,p.xz*.13).rgb;
   o.Albedo=tex*_Color.rgb*lerp(.75,1.3,macro.r);
   o.Metallic=0;o.Smoothness=.12;o.Occlusion=.9;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
