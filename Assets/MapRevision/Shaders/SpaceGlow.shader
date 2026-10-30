Shader "SpaceJanitors/MapRevision/SpaceGlow"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) _Sun ("Sun disc", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float _Sun;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float r=length(i.uv*2-1);
                float light=pow(saturate(1-r),2.0);
                if(_Sun>.5)
                {
                    float disc=1-smoothstep(.260,.276,r);
                    float corona=exp(-r*8)*.31*(1-smoothstep(.74,1,r));
                    return float4(lerp(_Color.rgb,float3(1,.96,.81),disc)* (disc+corona),1);
                }
                return float4(i.color.rgb*_Color.rgb*light,1);
            }
            ENDCG
        }
    }
}
