Shader "SpaceJanitors/MapRevision/Atmosphere"
{
    Properties { _Color ("Atmospheric scattering", Color) = (.2,.53,1,.54) _SunDirection ("Direction to sun", Vector) = (-.8,.3,.5,0) }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float3 _SunDirection;
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos-i.world);
                float fresnel=pow(1-saturate(dot(n,v)),4.5);
                float sunlight=smoothstep(-.2,.45,dot(n,normalize(_SunDirection)));
                return float4(_Color.rgb,_Color.a*fresnel*sunlight);
            }
            ENDCG
        }
    }
}
