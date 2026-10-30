Shader "SpaceJanitors/InstrumentLight"
{
 Properties { _Color("Light color",Color)=(.4,.8,1,1) _Power("Power",Range(0,4))=1 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 150
 CGPROGRAM
 #pragma surface surf Standard
 #pragma target 3.0
 fixed4 _Color; half _Power;
 struct Input {float3 worldPos;};
 void surf(Input IN,inout SurfaceOutputStandard o){o.Albedo=_Color.rgb*.1;o.Emission=_Color.rgb*_Power;o.Smoothness=.35;o.Alpha=1;}
 ENDCG
 }
 FallBack "Unlit/Color"
}
