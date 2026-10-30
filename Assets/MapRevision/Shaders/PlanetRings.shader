Shader "SpaceJanitors/MapRevision/PlanetRings"
{
    Properties { _Color ("Ice and dust", Color) = (.62,.56,.42,.54) }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float x=i.uv.x;
                float stripes=.60+.17*sin(x*371)+.12*sin(x*1491)+.10*sin(x*71);
                float gap=1-smoothstep(.45,.46,x)*(1-smoothstep(.50,.51,x));
                float edge=smoothstep(0,.04,x)*(1-smoothstep(.93,1,x));
                return float4(_Color.rgb*(.6+stripes*.4),_Color.a*stripes*gap*edge);
            }
            ENDCG
        }
    }
}
