Shader "SpaceJanitors/ShipGlass"
{
 Properties { _Color("Tint",Color)=(.1,.25,.32,.18) }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);return o;}
 fixed4 frag(v2f i):SV_Target{float f=pow(1-abs(dot(normalize(i.normal),normalize(i.view))),4);return fixed4(_Color.rgb+f*.16,_Color.a+f*.32);}
 ENDCG
 }
 }
}
