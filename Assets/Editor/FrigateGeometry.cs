using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Original, editable hard-surface construction kit. No downloaded geometry or textures.
public static class FrigateGeometry
{
    public const string Root="Assets/Frigate";
    static Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
    public static Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static int meshId;
    public static void Init()
    {
        cache.Clear();materials.Clear();meshId=0;
        foreach(string d in new[]{"Meshes","Materials","Prefabs","Scenes"})System.IO.Directory.CreateDirectory(Root+"/"+d);
        AssetDatabase.Refresh();
        Paint("Alloy","9BA5A9",.58f,.43f);Paint("Ivory","B8B9AD",.32f,.33f);Paint("Graphite","252F35",.68f,.4f);
        Paint("Steel","52616B",.7f,.47f);Paint("Floor","3D4B53",.5f,.3f);Paint("Rubber","151D23",.03f,.19f);
        Paint("Amber","C28A3C",.35f,.34f);Paint("Copper","895244",.7f,.45f);Paint("Blue","315D74",.48f,.35f);
        Paint("WhiteLight","C0EBEF",0,.2f,1.4f);Paint("AmberLight","FFB860",0,.2f,1.4f);
        Paint("Cyan","3DB4C9",.15f,.4f,1.8f);Paint("Screen","0B1B27",.25f,.55f,.12f);
        Paint("Green","569D87",.3f,.35f);Paint("Red","984945",.35f,.4f);
        var label=new Material(Shader.Find("SpaceJanitors/WorldLabel"));label.name="WorldLabel";
        label.mainTexture=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf").material.mainTexture;materials["Label"]=Save(label,"Materials/WorldLabel.mat");
        var glass=new Material(Shader.Find("SpaceJanitors/ShipGlass"));glass.name="BridgeGlass";glass.color=new Color(.12f,.25f,.32f,.18f);
        materials["Glass"]=Save(glass,"Materials/BridgeGlass.mat");
    }
    static void Paint(string key,string hex,float metal,float smooth,float emission=0)
    {
        ColorUtility.TryParseHtmlString("#"+hex,out Color c);
        var mat=new Material(Shader.Find(emission>0?"SpaceJanitors/InstrumentLight":"SpaceJanitors/WorkedMetal"));mat.name=key;mat.color=c;
        if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metal);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smooth);
        if(emission>0)mat.SetFloat("_Power",emission);
        materials[key]=Save(mat,"Materials/"+key+".mat");
    }
    public static T Save<T>(T asset,string relative)where T:UnityEngine.Object
    {
        string path=Root+"/"+relative;var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);return old;}
        AssetDatabase.CreateAsset(asset,path);return asset;
    }
    public static GameObject Group(Transform parent,string name,Vector3 position=default(Vector3))
    {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
    public static GameObject MeshObject(Transform parent,string name,Mesh mesh,string material,bool collision=false)
    {
        var g=Group(parent,name);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=materials[material];
        if(collision)g.AddComponent<MeshCollider>().sharedMesh=mesh;return g;
    }
    public static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,string mat,float bevel=.04f,bool collision=false)
    {
        var g=MeshObject(parent,name,BevelBox(size,bevel),mat);g.transform.localPosition=pos;
        if(collision)g.AddComponent<BoxCollider>().size=size;return g;
    }
    public static Mesh BevelBox(Vector3 size,float radius)
    {
        string key=string.Format(System.Globalization.CultureInfo.InvariantCulture,"B_{0:F3}_{1:F3}_{2:F3}_{3:F3}",size.x,size.y,size.z,radius);
        if(cache.TryGetValue(key,out var existing))return existing;
        Vector3 h=size*.5f;float r=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.95f);
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
            Vector3 u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up).normalized,w=Vector3.Cross(normal,u);
            float hu=Vector3.Dot(Abs(u),h),hv=Vector3.Dot(Abs(w),h),hn=Vector3.Dot(Abs(normal),h);
            float[] us={-hu,-hu+r,hu-r,hu},vs={-hv,-hv+r,hv-r,hv};int offset=v.Count;
            for(int j=0;j<4;j++)for(int i=0;i<4;i++){
                Vector3 q=normal*hn+u*us[i]+w*vs[j];var core=new Vector3(Mathf.Clamp(q.x,-h.x+r,h.x-r),Mathf.Clamp(q.y,-h.y+r,h.y-r),Mathf.Clamp(q.z,-h.z+r,h.z-r));
                Vector3 d=(q-core).normalized;v.Add(core+d*r);n.Add(d);uv.Add(new Vector2(us[i],vs[j]));
            }
            for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=offset+j*4+i;t.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
        }
        var mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
        cache[key]=Save(mesh,"Meshes/"+key+".asset");return cache[key];
    }
    static Vector3 Abs(Vector3 a)=>new Vector3(Mathf.Abs(a.x),Mathf.Abs(a.y),Mathf.Abs(a.z));
    public static GameObject Cylinder(Transform parent,string name,Vector3 a,Vector3 b,float radius,string mat,int sides=16)
    {
        string key=$"C_{sides}";if(!cache.TryGetValue(key,out var mesh)){
            var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<sides;i++){
                float t0=i*Mathf.PI*2/sides,t1=(i+1)*Mathf.PI*2/sides;
                Vector3 a0=new Vector3(Mathf.Cos(t0),0,Mathf.Sin(t0)),a1=new Vector3(Mathf.Cos(t1),0,Mathf.Sin(t1));int j=v.Count;
                v.AddRange(new[]{a0,a0+Vector3.up,a1+Vector3.up,a1});n.AddRange(new[]{a0,a0,a1,a1});t.AddRange(new[]{j,j+1,j+2,j,j+2,j+3});
                j=v.Count;v.AddRange(new[]{Vector3.zero,a0,a1});n.AddRange(new[]{Vector3.down,Vector3.down,Vector3.down});t.AddRange(new[]{j,j+1,j+2});
                j=v.Count;v.AddRange(new[]{Vector3.up,a1+Vector3.up,a0+Vector3.up});n.AddRange(new[]{Vector3.up,Vector3.up,Vector3.up});t.AddRange(new[]{j,j+1,j+2});
            }
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(t,0);mesh.RecalculateBounds();cache[key]=Save(mesh,"Meshes/"+key+".asset");mesh=cache[key];
        }
        var g=MeshObject(parent,name,mesh,mat);g.transform.localPosition=a;g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);g.transform.localScale=new Vector3(radius,Vector3.Distance(a,b),radius);return g;
    }
    public static GameObject Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,float depth,string mat)
    {var g=Box(parent,name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),depth),mat,.025f);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return g;}
    public static void Ring(Transform p,string name,Vector3 centre,float outer,float inner,float depth,string mat)
    {
        var g=Group(p,name,centre).transform;
        for(int i=0;i<24;i++){float a=(i+.5f)*360/24;var b=Box(g,"Rim",new Vector3(Mathf.Cos(a*Mathf.Deg2Rad)*(outer+inner)*.5f,Mathf.Sin(a*Mathf.Deg2Rad)*(outer+inner)*.5f,0),new Vector3((outer-inner),2*Mathf.PI*(outer+inner)*.5f/24*1.04f,depth),mat,.025f);b.transform.localRotation=Quaternion.Euler(0,0,a);}
    }
    public static void Panel(Transform parent,Vector3 pos,Vector2 size,Quaternion rotation,string mat="Ivory",bool bolts=true)
    {
        var p=Group(parent,"LayeredPanel",pos).transform;p.localRotation=rotation;
        Box(p,"Gasket",Vector3.zero,new Vector3(size.x+.07f,size.y+.07f,.09f),"Graphite",.025f);
        Box(p,"Plate",new Vector3(0,0,.065f),new Vector3(size.x,size.y,.12f),mat,.045f);
        if(bolts)foreach(float x in new[]{-.5f,.5f})foreach(float y in new[]{-.5f,.5f})
            Cylinder(p,"RecessedFastener",new Vector3(x*(size.x-.22f),y*(size.y-.22f),.13f),new Vector3(x*(size.x-.22f),y*(size.y-.22f),.15f),.026f,"Steel",6);
    }
    public static void Vent(Transform parent,Vector3 pos,Vector2 size,Quaternion rotation)
    {
        var p=Group(parent,"Vent",pos).transform;p.localRotation=rotation;
        Box(p,"Frame",Vector3.zero,new Vector3(size.x,size.y,.15f),"Steel",.045f);
        Box(p,"Recess",new Vector3(0,0,.09f),new Vector3(size.x-.18f,size.y-.16f,.03f),"Rubber",.015f);
        int count=Mathf.CeilToInt(size.y/.13f);for(int i=0;i<count;i++)Box(p,"Louvre",new Vector3(0,-size.y*.5f+.15f+i*(size.y-.3f)/Mathf.Max(1,count-1),.12f),new Vector3(size.x-.23f,.027f,.05f),"Alloy",.008f);
    }
    public static void Text(Transform parent,string value,Vector3 pos,Quaternion rotation,float size,Color color)
    {
        var g=Group(parent,"Label_"+value,pos);g.transform.localRotation=rotation;
        var tm=g.AddComponent<TextMesh>();tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.text=value;tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=color;
        g.GetComponent<MeshRenderer>().sharedMaterial=materials["Label"];
    }
    public static void Lamp(Transform p,Vector3 pos,Color color,float intensity=1,float range=7)
    {var g=Group(p,"PracticalLight",pos);var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
    public static void Loft(Transform p,string name,float[] z,float[] width,float[] bottom,float[] top,string mat,bool airlock=false)
    {
        var verts=new List<Vector3>();var tri=new List<int>();
        for(int j=0;j<z.Length-1;j++)for(int i=0;i<8;i++){
            if(airlock&&i==2&&z[j]>=-3.01f&&z[j+1]<=3.01f)continue;
            if(airlock&&i==4&&z[j]>=19&&z[j]<29)continue; // stairwell rises through this roof section
            var ringA=CrossSection(width[j],bottom[j],top[j],z[j]);var ringB=CrossSection(width[j+1],bottom[j+1],top[j+1],z[j+1]);
            int n=verts.Count;verts.AddRange(new[]{ringA[i],ringA[(i+1)%8],ringB[(i+1)%8],ringB[i]});tri.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        var m=new Mesh{name=name};m.SetVertices(verts);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();m=Save(m,"Meshes/"+name+".asset");MeshObject(p,name,m,mat,true);
    }
    static Vector3[] CrossSection(float w,float b,float t,float z)=>new[]{new Vector3(-w*.77f,b,z),new Vector3(w*.77f,b,z),new Vector3(w,b+1.0f,z),new Vector3(w,t-.85f,z),new Vector3(w*.83f,t,z),new Vector3(-w*.83f,t,z),new Vector3(-w,t-.85f,z),new Vector3(-w,b+1,z)};
    public static void RoofWing(Transform p,int side)
    {
        var v=new[]{new Vector3(side*2.35f,4.32f,19),new Vector3(side*10.1f,4.32f,19),new Vector3(side*6.64f,4.48f,29),new Vector3(side*2.35f,4.48f,29)};
        var mesh=new Mesh{name="ForwardShoulder_"+side};mesh.vertices=v;
        mesh.triangles=side>0?new[]{0,2,1,0,3,2}:new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
        MeshObject(p,mesh.name,Save(mesh,"Meshes/"+mesh.name+".asset"),"Alloy",true);
    }
    public static void Combine(Transform sector)
    {
        var groups=new Dictionary<Material,List<MeshFilter>>();
        foreach(var mf in sector.GetComponentsInChildren<MeshFilter>()){
            var r=mf.GetComponent<MeshRenderer>();if(!r||!r.enabled||mf.GetComponentInParent<SJInteractable>()||mf.GetComponent<TextMesh>())continue;
            if(!groups.ContainsKey(r.sharedMaterial))groups[r.sharedMaterial]=new List<MeshFilter>();groups[r.sharedMaterial].Add(mf);
        }
        foreach(var pair in groups){if(pair.Value.Count<2)continue;var ci=new List<CombineInstance>();
            foreach(var f in pair.Value)ci.Add(new CombineInstance{mesh=f.sharedMesh,transform=sector.worldToLocalMatrix*f.transform.localToWorldMatrix});
            var mesh=new Mesh{name="Batch_"+sector.name+"_"+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(ci.ToArray(),true,true);mesh.RecalculateBounds();
            mesh=Save(mesh,"Meshes/Batch_"+(meshId++)+".asset");var g=Group(sector,mesh.name);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
            foreach(var f in pair.Value){UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(f);}
        }
    }
}
