Shader "SpeedRovers/ProjectedLine"
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
            float4 _Endpoints,_Size;
            Output vert(Input v) { Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(Output i):SV_Target
            {
                float2 p=float2(i.uv.x,1-i.uv.y)*_Size.xy;
                float2 delta=_Endpoints.zw-_Endpoints.xy;
                float t=saturate(dot(p-_Endpoints.xy,delta)/max(dot(delta,delta),.0001));
                float d=length(p-(_Endpoints.xy+t*delta));
                fixed4 result=i.color;
                result.a*=1-smoothstep(max(0,_Size.z*.5-.6),_Size.z*.5+.6,d);
                return result;
            }
            ENDCG
        }
    }
}
