Shader "SpaceJanitors/MapRevision/Planet"
{
    Properties
    {
        _OceanColor ("Deep ocean", Color) = (.02,.07,.15,1)
        _LandColor ("Vegetation", Color) = (.18,.23,.14,1)
        _HighlandsColor ("Highlands", Color) = (.47,.39,.23,1)
        _CloudColor ("Clouds", Color) = (.89,.92,.94,1)
        _GasWorld ("Gas world", Float) = 0
        _SunDirection ("Direction to sun", Vector) = (-.8,.3,.5,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _OceanColor, _LandColor, _HighlandsColor, _CloudColor;
            float3 _SunDirection;
            float _GasWorld;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 local : TEXCOORD0; float3 world : TEXCOORD1; float3 normal : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.local=v.vertex.xyz;
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o;
            }
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float noise(float3 p)
            {
                float3 a=floor(p), b=frac(p); b=b*b*(3-2*b);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),b.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),b.x),b.y),
                            lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),b.x),lerp(hash(a+float3(0,1,1)),hash(a+1),b.x),b.y),b.z);
            }
            float fbm(float3 p)
            {
                float value=0; float weight=.5;
                [unroll] for(int k=0;k<5;k++){value+=noise(p)*weight;p=p*2.04+float3(2.7,9.2,5.3);weight*=.5;}
                return value;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 p=normalize(i.local), n=normalize(i.normal), view=normalize(_WorldSpaceCameraPos-i.world), sun=normalize(_SunDirection);
                float terrain=fbm(p*3.8+float3(7.1,2.3,14.5));
                float land=smoothstep(.465,.485,terrain);
                float high=smoothstep(.53,.7,terrain);
                float3 albedo=lerp(_OceanColor.rgb,lerp(_LandColor.rgb,_HighlandsColor.rgb,high),land);
                // Thin continental shelves, pale deserts and imperfect polar ice caps.
                float shelf=smoothstep(.435,.46,terrain)*(1-land);
                albedo+=float3(.025,.095,.075)*shelf;
                float dryness=smoothstep(.46,.67,fbm(p*8.3+25))*land*(1-high);
                albedo=lerp(albedo,_HighlandsColor.rgb*.92,dryness*.7);
                float ice=smoothstep(.79,.93,abs(p.y)+noise(p*26)*.085);
                albedo=lerp(albedo,float3(.72,.79,.82),ice);
                // Warped, broken cloud belts; no visible texture seams at the poles.
                float3 cloudPoint=p*10 + float3(p.z,p.x,p.y)*noise(p*4)*1.3;
                float clouds=smoothstep(.49,.64,fbm(cloudPoint+36.2));
                clouds*=lerp(.65,1,noise(p*52));
                albedo=lerp(albedo,_CloudColor.rgb,clouds*.94);
                if (_GasWorld>.5)
                {
                    float bands=sin(p.y*89+noise(p*8)*4+fbm(p*29)*3)*.5+.5;
                    float streak=fbm(float3(p.x*15,p.y*83,p.z*15)+7);
                    albedo=lerp(_OceanColor.rgb,_HighlandsColor.rgb,saturate(bands*.45+streak*.7));
                    albedo=lerp(albedo,_LandColor.rgb,noise(p*19)*.3);
                    land=1; clouds=0;
                }
                float nl=dot(n,sun);
                float daylight=smoothstep(-.055,.12,nl);
                float lambert=max(0,nl)*.91+.09;
                float3 result=albedo*(.013+daylight*lambert);
                float highlight=pow(saturate(dot(n,normalize(sun+view))),110)*(1-land)*(1-clouds)*(1-ice);
                result+=float3(.90,.87,.79)*highlight*.40*daylight;
                float rim=pow(1-saturate(dot(n,view)),4);
                result+=(_GasWorld>.5?float3(.14,.12,.075):float3(.035,.105,.24))*rim*daylight;
                return float4(result,1);
            }
            ENDCG
        }
    }
}
