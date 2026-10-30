Shader "SpaceJanitors/WorkedMetal"
{
    Properties {
        _Color ("Paint",Color)=(.6,.65,.68,1)
        _Metallic ("Metallic",Range(0,1))=.45
        _Smoothness ("Smoothness",Range(0,1))=.4
        _Wear ("Surface variation",Range(0,.15))=.04
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;half _Metallic,_Smoothness,_Wear;
        struct Input {float3 worldPos;float3 worldNormal;};
        float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
        void surf(Input i,inout SurfaceOutputStandard o){
            float grain=hash(floor(i.worldPos*175));
            float slow=hash(floor(i.worldPos*3));
            float marks=step(.985,hash(floor(i.worldPos*float3(2,80,90))));
            o.Albedo=_Color.rgb*(1-_Wear*(grain*.7+slow*.6+marks));
            o.Metallic=_Metallic;o.Smoothness=saturate(_Smoothness+(grain-.5)*.08);o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
