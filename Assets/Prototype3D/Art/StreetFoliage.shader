Shader "CockpitLab/StreetFoliage" {
Properties { _MainTex("Foliage",2D)="white"{} _Cutoff("Leaf alpha cutoff",Range(0,1))=.55 }
SubShader {
 Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
 Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow alphatest:_Cutoff
 #pragma target 3.0
 sampler2D _MainTex;
 struct Input {float2 uv_MainTex;};
 void surf(Input IN,inout SurfaceOutputStandard o) {fixed4 c=tex2D(_MainTex,IN.uv_MainTex);o.Albedo=c.rgb*.8;o.Alpha=c.a;o.Smoothness=.08;}
 ENDCG
}
Fallback "Transparent/Cutout/Diffuse"
}
