Shader "SpeedRovers/FuelBeaconGlow"
{
    Properties { _Tint ("Glow", Color) = (0.1,1,0.64,0.26) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct v2f { float4 vertex : SV_POSITION; UNITY_FOG_COORDS(0) };
            fixed4 _Tint;
            v2f vert(appdata_base v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); UNITY_TRANSFER_FOG(o,o.vertex); return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = _Tint; UNITY_APPLY_FOG_COLOR(i.fogCoord,color,fixed4(0,0,0,0)); return color;
            }
            ENDCG
        }
    }
}
