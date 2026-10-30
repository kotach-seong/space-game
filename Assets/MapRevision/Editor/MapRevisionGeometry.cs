using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Independent assets so the previous ship and scene backup remain intact.
public static class MapRevisionGeometry {
    public const string Root="Assets/MapRevision";
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    public static readonly Dictionary<string,Material> Mats=new Dictionary<string,Material>();
    public static void Init(){
        foreach(string d in new[]{"Meshes","Materials","Scenes","Prefabs"})Directory.CreateDirectory(Root+"/"+d);AssetDatabase.Refresh();meshes.Clear();Mats.Clear();
        foreach(string name in new[]{"Alloy","Ivory","Graphite","Steel","Floor","Rubber","Amber","Copper","Blue","WhiteLight","AmberLight","Cyan","Screen","Green","Red"}){
            var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Frigate/Materials/"+name+".mat");Mats[name]=Save(new Material(original){name="R3_"+name},"Materials/"+name+".mat");
        }
        Mats["Glass"]=Save(new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Frigate/Materials/BridgeGlass.mat")),"Materials/Glass.mat");
        var label=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Frigate/Materials/WorldLabel.mat"));label.shader=Shader.Find("SpaceJanitors/MapRevision/Label");Mats["Label"]=Save(label,"Materials/Label.mat");
    }
    public static T Save<T>(T item,string path)where T:Object {string full=Root+"/"+path;var old=AssetDatabase.LoadAssetAtPath<T>(full);if(old){EditorUtility.CopySerialized(item,old);Object.DestroyImmediate(item);return old;}AssetDatabase.CreateAsset(item,full);return item;}
    public static Transform Group(Transform parent,string name,Vector3 position=default){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g.transform;}
    public static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,string mat,bool solid=true,float bevel=.035f){
        string key=string.Format(System.Globalization.CultureInfo.InvariantCulture,"B_{0:F3}_{1:F3}_{2:F3}_{3:F3}",size.x,size.y,size.z,bevel);
        if(!meshes.TryGetValue(key,out var mesh)){
            var v=new List<Vector3>();var n=new List<Vector3>();var tr=new List<int>();Vector3 half=size*.5f;float r=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
                Vector3 u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up).normalized,w=Vector3.Cross(normal,u);
                float hu=Vector3.Dot(Abs(u),half),hv=Vector3.Dot(Abs(w),half),hn=Vector3.Dot(Abs(normal),half);float[] us={-hu,-hu+r,hu-r,hu},vs={-hv,-hv+r,hv-r,hv};int off=v.Count;
                for(int j=0;j<4;j++)for(int i=0;i<4;i++){Vector3 q=normal*hn+u*us[i]+w*vs[j];Vector3 core=new Vector3(Mathf.Clamp(q.x,-half.x+r,half.x-r),Mathf.Clamp(q.y,-half.y+r,half.y-r),Mathf.Clamp(q.z,-half.z+r,half.z-r));var d=(q-core).normalized;v.Add(core+d*r);n.Add(d);}
                for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=off+j*4+i;tr.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
            }
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(tr,0);mesh.RecalculateBounds();mesh=Save(mesh,"Meshes/"+key+".asset");meshes[key]=mesh;
        }
        var g=Group(p,name,pos).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Mats[mat];if(solid)g.AddComponent<BoxCollider>().size=size;return g;
    }
    static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
    public static void Beam(Transform p,string name,Vector3 a,Vector3 b,float width,string material,bool solid=false){var g=Box(p,name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),material,solid);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    public static void Cylinder(Transform p,string name,Vector3 a,Vector3 b,float radius,string material){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=(a+b)*.5f;g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);g.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<MeshRenderer>().sharedMaterial=Mats[material];}
    public static void Ring(Transform p,string name,Vector3 centre,float outer,float inner,float depth,string material){
        var r=Group(p,name,centre);for(int i=0;i<48;i++){float angle=(i+.5f)*360/48;var a=angle*Mathf.Deg2Rad;var b=Box(r,"RingSegment",new Vector3(Mathf.Cos(a)*(outer+inner)*.5f,Mathf.Sin(a)*(outer+inner)*.5f,0),new Vector3(outer-inner,2*Mathf.PI*(outer+inner)*.5f/48*1.04f,depth),material,false,.025f);b.transform.localRotation=Quaternion.Euler(0,0,angle);}
    }
    public static void Fairing(Transform p,string name,float z0,float z1,float width0,float width1,float bottom0,float bottom1,float top0,float top1){
        // Four exterior shoulder faces; interiors are independently enclosed by thick slabs.
        Vector3[] a={new Vector3(-width0,bottom0,z0),new Vector3(width0,bottom0,z0),new Vector3(width0,top0,z0),new Vector3(-width0,top0,z0)},b={new Vector3(-width1,bottom1,z1),new Vector3(width1,bottom1,z1),new Vector3(width1,top1,z1),new Vector3(-width1,top1,z1)};
        var vs=new List<Vector3>();var ts=new List<int>();for(int i=0;i<4;i++){int n=vs.Count;vs.AddRange(new[]{a[i],a[(i+1)%4],b[(i+1)%4],b[i]});ts.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        var m=new Mesh{name=name};m.SetVertices(vs);m.SetTriangles(ts,0);m.RecalculateNormals();m.RecalculateBounds();m=Save(m,"Meshes/"+name+".asset");var g=Group(p,name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=m;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mats["Alloy"];g.gameObject.AddComponent<MeshCollider>().sharedMesh=m;
    }
    public static void Label(Transform p,string text,Vector3 pos,Quaternion rot,float size=.11f,Color? color=null){var g=Group(p,"Sign_"+text.Replace('\n','_'),pos);g.localRotation=rot;var t=g.gameObject.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.characterSize=size;t.fontSize=60;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color??new Color(.6f,.87f,.94f);g.GetComponent<MeshRenderer>().sharedMaterial=Mats["Label"];}
    public static void Lamp(Transform p,Vector3 pos,float range=11,float power=1){var l=Group(p,"AreaLight",pos).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.7f,.84f,.95f);l.intensity=power;l.range=range;l.shadows=LightShadows.None;}
    public static void Deck(Transform p,float x0,float x1,float z0,float z1,float y){
        Box(p,"ContinuousFloor",new Vector3((x0+x1)*.5f,y-.16f,(z0+z1)*.5f),new Vector3(x1-x0,.32f,z1-z0),"Graphite");
        for(float x=x0;x<x1-.1f;x+=3)for(float z=z0;z<z1-.1f;z+=3){float w=Mathf.Min(3,x1-x),h=Mathf.Min(3,z1-z);Box(p,"DeckTile",new Vector3(x+w*.5f,y+.012f,z+h*.5f),new Vector3(w-.025f,.028f,h-.025f),"Floor",false,.006f);}
    }
    public static void Panel(Transform p,Vector3 pos,Vector2 size,Quaternion rot,string mat="Alloy"){
        var r=Group(p,"ServicePanel",pos);r.localRotation=rot;Box(r,"Gasket",Vector3.zero,new Vector3(size.x+.055f,size.y+.055f,.075f),"Rubber",false,.01f);
        Box(r,"Plate",new Vector3(0,0,.05f),new Vector3(size.x,size.y,.065f),mat,false,.018f);
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})Box(r,"Fastener",new Vector3(x*(size.x*.5f-.09f),y*(size.y*.5f-.09f),.093f),new Vector3(.045f,.045f,.024f),"Steel",false,.007f);
    }
    public static void Combine(Transform p){
        var groups=new Dictionary<Material,List<MeshFilter>>();foreach(var f in p.GetComponentsInChildren<MeshFilter>()){
            if(f.GetComponentInParent<SJInteractable>())continue;var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||r.sharedMaterial==Mats["Glass"])continue;
            if(!groups.ContainsKey(r.sharedMaterial))groups[r.sharedMaterial]=new List<MeshFilter>();groups[r.sharedMaterial].Add(f);
        }
        int count=0;foreach(var pair in groups){if(pair.Value.Count<2)continue;var ci=new List<CombineInstance>();foreach(var f in pair.Value)ci.Add(new CombineInstance{mesh=f.sharedMesh,transform=p.worldToLocalMatrix*f.transform.localToWorldMatrix});
            var m=new Mesh{name=p.name+"_"+pair.Key.name,indexFormat=IndexFormat.UInt32};m.CombineMeshes(ci.ToArray());m.RecalculateBounds();m=Save(m,"Meshes/Batch_"+p.name+"_"+(count++)+".asset");var g=Group(p,"Static_"+pair.Key.name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=m;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
            foreach(var f in pair.Value){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
        }
    }
}
