Shader "SpeedRovers/TireMotion"
{
    Properties { _MainTex ("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float4 _Region, _Tire;
            float _Phase, _Velocity, _BodyPhase;
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(Output i):SV_Target
            {
                float2 local=(i.uv-_Region.xy)/_Region.zw;
                // Small rider articulation and suspension travel. The bottom
                // tire contact remains fixed; no rotation or strip shearing.
                float upper=smoothstep(.52,.92,local.y);
                float2 animatedUV=i.uv;
                float riding=saturate(_Velocity/80);
                animatedUV.x+=(sin(_BodyPhase)*.004+sin(_BodyPhase*.43)*.003)*upper*_Region.z*riding;
                animatedUV.y+=sin(_BodyPhase*1.3)*.004*upper*_Region.w*riding;
                fixed4 original=tex2D(_MainTex,animatedUV);
                float nx=(local.x-(_Tire.x+local.y*_Tire.w))/_Tire.y;
                float ny=local.y/_Tire.z;
                // Affect rubber inside its painted silhouette, never draw a second tire.
                float mask=smoothstep(1,.72,abs(nx))*smoothstep(0,.10,ny)*smoothstep(1,.86,ny);
                fixed3 rubber=0;
                for(int tap=-4;tap<=4;tap++)
                {
                    float sampleY=clamp(local.y+tap*.012,.035,_Tire.z-.035);
                    rubber+=tex2D(_MainTex,float2(i.uv.x+(sampleY-local.y)*_Tire.w*_Region.z,_Region.y+sampleY*_Region.w)).rgb/9;
                }
                float blur=smoothstep(70,180,_Velocity);
                float groove=pow(saturate(cos(ny*55+abs(nx)*4-_Phase*6.283)),18);
                rubber*=1-groove*lerp(.32,.025,blur);
                rubber+=sin(nx*22+_Phase)*.006*blur;
                original.rgb=lerp(original.rgb,rubber,mask*.97);
                return original*i.color;
            }
            ENDCG
        }
    }
}
