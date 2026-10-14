Shader "SpeedRovers/VehicleMotion"
{
    Properties { _MainTex ("Sprite",2D)="white" {} }
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
            struct Input { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            sampler2D _MainTex;float4 _Region,_Wheels;float _Phase,_Velocity,_BodyPhase;
            Output vert(Input v) { Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(Output i):SV_Target
            {
                float2 local=(i.uv-_Region.xy)/_Region.zw;
                float2 bodyUV=i.uv;
                bodyUV.y+=sin(_BodyPhase)*.002*_Region.w*smoothstep(.27,.65,local.y)*saturate(_Velocity/60);
                fixed4 source=tex2D(_MainTex,bodyUV);
                float wheelX=abs(local.x-_Wheels.x)<abs(local.x-_Wheels.y)?_Wheels.x:_Wheels.y;
                float2 wheel=(local-float2(wheelX,.12))/float2(_Wheels.z,_Wheels.w);
                float mask=saturate((1-dot(wheel,wheel))*8);
                float dark=1-smoothstep(.20,.45,dot(source.rgb,float3(.299,.587,.114)));
                float blur=smoothstep(30,110,_Velocity);
                fixed3 rubber=0;
                for(int tap=-3;tap<=3;tap++)
                    rubber+=tex2D(_MainTex,i.uv+float2(0,tap*.006*_Region.w)).rgb/7;
                float motion=sin(local.y*155-_Phase*6.283)*lerp(.025,.004,blur);
                float angle=_Phase*6.283;
                float2 rotated=float2(wheel.x*cos(angle)-wheel.y*sin(angle),wheel.x*sin(angle)+wheel.y*cos(angle));
                float2 rimUV=_Region.xy+(float2(wheelX,.12)+rotated*float2(_Wheels.z,_Wheels.w)) * _Region.zw;
                fixed3 rim=tex2D(_MainTex,rimUV).rgb;
                // Spin only interior wheel pixels. Original alpha/contact stays intact.
                float hub=(1-smoothstep(.2,.65,dot(wheel,wheel)))*(1-dark);
                source.rgb=lerp(source.rgb,lerp(rim,rubber,blur),hub*.7);
                source.rgb=lerp(source.rgb,rubber+motion,mask*dark*.8);
                // Alpha and tire silhouette always come from the painted car.
                return source*i.color;
            }
            ENDCG
        }
    }
}


