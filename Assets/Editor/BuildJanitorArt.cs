using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

// Original procedural artwork. Metres, Y up, character faces +Z.
public static class BuildJanitorArt
{
    const string Root = "Assets/SpaceJanitors";
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static readonly List<string> checks = new List<string>();
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Deliverables"));

    [MenuItem("Space Janitors/Rebuild Art Pack")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        foreach (string d in new [] {"Materials", "Meshes", "Prefabs/Characters", "Prefabs/Ship", "Prefabs/Props", "Scenes"})
            Directory.CreateDirectory(Root + "/" + d);
        AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Palette();
        var characters = new List<GameObject>();
        string[] colors = {"Orange", "Teal", "Yellow", "Rose"};
        foreach (string c in colors) {
            var g = Character(c);
            Save(g, "Characters/Janitor_" + c);
            characters.Add(g);
        }
        var ship = Ship();
        Save(ship, "Ship/SJ_Workship_Complete");
        var props = new List<GameObject>();
        props.Add(Crate(null, Vector3.zero));
        props.Add(Console(null, Vector3.zero));
        props.Add(Repair(null, Vector3.zero));
        foreach (var p in props) { Save(p, "Props/"+p.name); UnityEngine.Object.DestroyImmediate(p); }
        Validate(ship, characters);
        SetLighting();
        foreach (var c in characters) c.SetActive(false);
        var ground = Box(null, "StudioGround", new Vector3(0,-.65f,0), new Vector3(500,.5f,500), "Backdrop", .1f, false);
        Render("01_Ship_Exterior.png", new Vector3(43,36,51), new Vector3(0,1,0), 29);
        Render("06_Ship_Engines.png", new Vector3(-43,29,-51), new Vector3(0,1,-2), 29);
        ship.transform.Find("Exterior/Roof").gameObject.SetActive(false);
        Render("02_Ship_Cutaway.png", new Vector3(39,52,48), new Vector3(0,0,0), 29);
        Render("03_Interior_Closeup.png", new Vector3(10,15,17), new Vector3(3,0,2), 12);
        ship.SetActive(false);
        for(int i=0;i<characters.Count;i++) {characters[i].SetActive(true); characters[i].transform.position=new Vector3((i-1.5f)*1.65f,0,0);}
        ground.transform.position=new Vector3(0,-.25f,0);
        Render("04_Crew_Lineup.png", new Vector3(4,2.8f,11), new Vector3(0,1.05f,0), 4.1f);
        for(int i=1;i<characters.Count;i++) characters[i].SetActive(false);
        characters[0].transform.position=Vector3.zero;
        Render("05_Character_Detail.png", new Vector3(3,2.2f,5), new Vector3(0,1.1f,0), 1.65f);
        foreach(var c in characters) UnityEngine.Object.DestroyImmediate(c);
        ship.SetActive(true);
        ship.transform.Find("Exterior/Roof").gameObject.SetActive(true);
        UnityEngine.Object.DestroyImmediate(ground);
        var demoCrew = Character("Orange"); demoCrew.transform.position=new Vector3(0,0,5);
        var sceneCamera=Group(null,"Main Camera").AddComponent<Camera>();sceneCamera.tag="MainCamera";
        sceneCamera.transform.position=new Vector3(43,36,51);sceneCamera.transform.LookAt(new Vector3(0,1,0));
        sceneCamera.orthographic=true;sceneCamera.orthographicSize=29;sceneCamera.farClipPlane=300;
        sceneCamera.clearFlags=CameraClearFlags.SolidColor;sceneCamera.backgroundColor=new Color(.25f,.34f,.38f);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Root+"/Scenes/ArtShowcase.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ExportPackage(Root, Output+"/SpaceJanitors_StarterArt.unitypackage", ExportPackageOptions.Recurse);
        File.WriteAllLines(Output+"/Validation.txt", checks);
        Debug.Log("SJ_ART_BUILD_SUCCESS: " + Output);
    }

    static void Palette()
    {
        string[] names={"Cream","Hull","Dark","Visor","White","Orange","Teal","Yellow","Rose","Rubber","Steel","Floor","Red","Mint","Backdrop"};
        string[] hex={"E6DDC6","92ABA9","34484D","16343E","FFF6DB","E98B4D","51B5AA","EDC65B","D8838D","3F5054","758D90","71868A","D65547","9BDCC3","B8C9CA"};
        for(int i=0;i<names.Length;i++) {
            ColorUtility.TryParseHtmlString("#"+hex[i],out var color);
            var mat=new Material(Shader.Find("Standard")); mat.name="SJ_"+names[i]; mat.color=color;
            mat.SetFloat("_Glossiness", names[i]=="Visor" ? .35f : .12f);
            mat.SetFloat("_Metallic", 0);
            if(names[i]=="Mint") {mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.35f);}
            string path=Root+"/Materials/"+mat.name+".mat";
            var old=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(old!=null){EditorUtility.CopySerialized(mat,old);UnityEngine.Object.DestroyImmediate(mat);mat=old;} else AssetDatabase.CreateAsset(mat,path);
            mats[names[i]]=mat;
        }
    }
    static GameObject Group(Transform parent,string name,Vector3 p=default(Vector3)) {
        var g=new GameObject(name); g.transform.SetParent(parent,false);g.transform.localPosition=p;return g;
    }
    static GameObject Shape(Transform parent,string name,Vector3 p,Vector3 scale,string material,PrimitiveType type,bool collider=false) {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mats[material];
        if(!collider) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,string mat,float bevel=.08f,bool collider=true) {
        var g=Group(parent,name,p);var mf=g.AddComponent<MeshFilter>();mf.sharedMesh=RoundedBox(size,bevel);
        g.AddComponent<MeshRenderer>().sharedMaterial=mats[mat];
        if(collider)g.AddComponent<BoxCollider>().size=size;
        return g;
    }
    static Mesh RoundedBox(Vector3 size,float radius) {
        string key=$"Box_{size.x:F3}_{size.y:F3}_{size.z:F3}_{radius:F3}";
        if(meshes.TryGetValue(key,out var cached))return cached;
        Vector3 h=size*.5f;float r=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.95f);
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        foreach(var normal in normals){
            Vector3 axisU=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up).normalized;
            Vector3 axisV=Vector3.Cross(normal,axisU).normalized;
            float hu=Vector3.Dot(new Vector3(Mathf.Abs(axisU.x),Mathf.Abs(axisU.y),Mathf.Abs(axisU.z)),h);
            float hv=Vector3.Dot(new Vector3(Mathf.Abs(axisV.x),Mathf.Abs(axisV.y),Mathf.Abs(axisV.z)),h);
            float hn=Vector3.Dot(new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z)),h);
            float[] us={-hu,-hu+r,hu-r,hu};float[] vs={-hv,-hv+r,hv-r,hv};int start=v.Count;
            for(int j=0;j<4;j++)for(int i=0;i<4;i++){
                Vector3 q=normal*hn+axisU*us[i]+axisV*vs[j];
                Vector3 core=new Vector3(Mathf.Clamp(q.x,-h.x+r,h.x-r),Mathf.Clamp(q.y,-h.y+r,h.y-r),Mathf.Clamp(q.z,-h.z+r,h.z-r));
                Vector3 direction=(q-core).normalized;v.Add(core+direction*r);n.Add(direction);uv.Add(new Vector2((float)i/3,(float)j/3));
            }
            for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=start+j*4+i;t.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
        }
        var mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
        string path=Root+"/Meshes/"+key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old!=null){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        meshes[key]=mesh;return mesh;
    }
    static GameObject Orb(Transform p,string name,Vector3 pos,Vector3 size,string m) => Shape(p,name,pos,size,m,PrimitiveType.Sphere);
    static void Pipe(Transform parent,string name,Vector3 a,Vector3 b,float radius,string mat) {
        var g=Shape(parent,name,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),mat,PrimitiveType.Cylinder);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static GameObject Character(string color) {
        var root=Group(null,"Janitor_"+color);var p=root.transform;
        Box(p,"Torso",new Vector3(0,.98f,0),new Vector3(.66f,.68f,.43f),color,.17f,false);
        Box(p,"ChestPatch",new Vector3(0,1.1f,.232f),new Vector3(.35f,.23f,.05f),"Cream",.04f,false);
        Box(p,"Badge",new Vector3(-.09f,1.11f,.27f),new Vector3(.065f,.1f,.02f),"Teal",.01f,false);
        for(int i=0;i<2;i++)Orb(p,"StatusLamp_"+i,new Vector3(.03f+i*.065f,1.12f,.27f),Vector3.one*.04f,i==0?"Mint":"Orange");
        var head=Group(p,"Head",new Vector3(0,1.64f,0)).transform;
        Orb(head,"HelmetShell",Vector3.zero,new Vector3(1.03f,.87f,.86f),"Cream");
        Orb(head,"FaceSeal",new Vector3(0,-.015f,.315f),new Vector3(.85f,.61f,.29f),"Dark");
        Orb(head,"Visor",new Vector3(0,-.012f,.377f),new Vector3(.76f,.51f,.23f),"Visor");
        for(int side=-1;side<=1;side+=2){
            Orb(head,"Eye_"+side,new Vector3(side*.135f,.006f,.491f),new Vector3(.073f,.115f,.035f),"White");
            var ear=Shape(head,"HelmetFastener_"+side,new Vector3(side*.49f,0,0),new Vector3(.24f,.05f,.24f),color,PrimitiveType.Cylinder);ear.transform.localRotation=Quaternion.Euler(0,0,90);
            var arm=Group(p,side<0?"Arm_L":"Arm_R",new Vector3(side*.36f,1.17f,0)).transform;
            var sleeve=Shape(arm,"Sleeve",new Vector3(side*.09f,-.21f,0),new Vector3(.23f,.22f,.24f),color,PrimitiveType.Capsule);
            sleeve.transform.localRotation=Quaternion.Euler(0,0,side*20);
            Orb(arm,"Glove",new Vector3(side*.16f,-.44f,.01f),new Vector3(.27f,.27f,.29f),"Rubber");
            var leg=Group(p,side<0?"Leg_L":"Leg_R",new Vector3(side*.19f,.68f,0)).transform;
            Shape(leg,"Trouser",new Vector3(0,-.22f,0),new Vector3(.25f,.23f,.27f),color,PrimitiveType.Capsule);
            Box(leg,"Boot",new Vector3(0,-.55f,.07f),new Vector3(.3f,.26f,.46f),"Rubber",.08f,false);
            Box(leg,"Sole",new Vector3(0,-.635f,.075f),new Vector3(.31f,.07f,.47f),"Dark",.02f,false);
        }
        Box(p,"Backpack",new Vector3(0,1.03f,-.34f),new Vector3(.53f,.6f,.3f),"Hull",.1f,false);
        for(int side=-1;side<=1;side+=2)Shape(p,"AirBottle_"+side,new Vector3(side*.18f,1.05f,-.52f),new Vector3(.15f,.22f,.15f),"Yellow",PrimitiveType.Capsule);
        Box(p,"Belt",new Vector3(0,.75f,.015f),new Vector3(.67f,.11f,.47f),"Rubber",.04f,false);
        Box(p,"Buckle",new Vector3(0,.75f,.27f),new Vector3(.14f,.1f,.04f),"Yellow",.02f,false);
        return root;
    }
    static GameObject Crate(Transform parent,Vector3 pos) {
        var g=Group(parent,"CargoCrate",pos);var p=g.transform;
        Box(p,"Container",new Vector3(0,.7f,0),new Vector3(1.5f,1.4f,1.5f),"Orange",.15f);
        foreach(float x in new[]{-.52f,.52f})Box(p,"Strap",new Vector3(x,.71f,0),new Vector3(.13f,1.46f,1.54f),"Dark",.035f);
        Box(p,"Label",new Vector3(0,.8f,.77f),new Vector3(.55f,.32f,.04f),"Cream",.03f,false);
        return g;
    }
    static GameObject Console(Transform parent,Vector3 pos) {
        var g=Group(parent,"MissionConsole",pos);var p=g.transform;
        Box(p,"Pedestal",new Vector3(0,.6f,0),new Vector3(1.9f,1.2f,.85f),"Hull",.15f);
        Box(p,"MonitorFrame",new Vector3(0,1.8f,-.05f),new Vector3(2.25f,1.55f,.55f),"Cream",.18f);
        Box(p,"Screen",new Vector3(0,1.85f,.25f),new Vector3(1.9f,1.16f,.05f),"Visor",.08f,false);
        for(int i=0;i<3;i++){
            Box(p,"TaskLine_"+i,new Vector3(.1f,2.18f-i*.31f,.292f),new Vector3(1.05f,.09f,.025f),"Mint",.015f,false);
            Box(p,"TaskCheckbox_"+i,new Vector3(-.64f,2.18f-i*.31f,.293f),new Vector3(.17f,.17f,.025f),i==2?"Yellow":"Mint",.02f,false);
        }
        Box(p,"Keyboard",new Vector3(0,1.19f,.52f),new Vector3(2,.2f,.7f),"Dark",.07f);
        for(int i=0;i<5;i++)Box(p,"Key_"+i,new Vector3(-.65f+i*.26f,1.31f,.58f),new Vector3(.17f,.05f,.16f),"Cream",.015f,false);
        return g;
    }
    static GameObject Repair(Transform parent,Vector3 pos) {
        var g=Group(parent,"RepairPanel",pos);var p=g.transform;
        Box(p,"Casing",Vector3.zero,new Vector3(1.8f,1.4f,.32f),"Yellow",.1f);
        Box(p,"Inset",new Vector3(0,0,.18f),new Vector3(1.5f,1.1f,.07f),"Dark",.05f,false);
        for(int i=0;i<3;i++) {
            string m=new[]{"Orange","Teal","Cream"}[i];float x=-.45f+i*.45f;
            Pipe(p,"Cable_"+i,new Vector3(x,-.32f,.28f),new Vector3(x,.3f,.28f),.065f,m);
            Orb(p,"Socket_"+i,new Vector3(x,.36f,.28f),Vector3.one*.17f,"Steel");
        }
        return g;
    }
    static GameObject Ship() {
        var root=Group(null,"SJ_Workship_Complete");var interior=Group(root.transform,"Interior").transform;
        var exterior=Group(root.transform,"Exterior").transform;var roof=Group(exterior,"Roof").transform;
        var markers=Group(root.transform,"GameplayAnchors").transform;
        Box(interior,"Deck",new Vector3(0,-.32f,0),new Vector3(24,.44f,38),"Dark",.15f);
        // Tiled modular decks and room dividers. All passage thresholds are level.
        string[] rooms={"Cargo","Cooling","Waste","Workshop","LifeSupport","Bridge"};
        for(int iz=0;iz<3;iz++)for(int side=-1;side<=1;side+=2){
            float z=(iz-1)*12;float x=side*7;int index=iz*2+(side+1)/2;
            var room=Group(interior,rooms[index],new Vector3(x,0,z)).transform;
            Box(room,"Floor",new Vector3(0,-.045f,0),new Vector3(9.7f,.09f,11.7f),"Floor",.025f);
            // A 3 m wide opening connects every room to the central corridor.
            foreach(float dz in new[]{-3.75f,3.75f})Box(room,"CorridorWall",new Vector3(-side*4.9f,2,dz),new Vector3(.24f,4,4.5f),"Cream",.07f);
            Box(room,"DoorLintel",new Vector3(-side*4.9f,3.7f,0),new Vector3(.36f,.6f,3),"Hull",.06f);
            foreach(float dz in new[]{-1.6f,1.6f})Box(room,"DoorTrim",new Vector3(-side*4.91f,1.7f,dz),new Vector3(.4f,3.4f,.18f),"Yellow",.035f);
            if(iz<2)Box(room,"Partition",new Vector3(0,2,6),new Vector3(9.7f,4,.24f),"Hull",.07f);
            Group(markers,"Mission_"+rooms[index],new Vector3(x,0,z));
            if(index==0){for(int i=0;i<4;i++)Crate(room,new Vector3((i%2)*2.5f-1.3f,0,(i/2)*3-1.5f));}
            if(index==1 || index==4){
                for(int i=0;i<3;i++){
                    Shape(room,"CoolantTank",new Vector3(-2+i*2,1.6f,-3.5f),new Vector3(1.5f,1.6f,1.5f),"Teal",PrimitiveType.Cylinder,true);
                    Box(room,"TankBand",new Vector3(-2+i*2,1.5f,-3.5f),new Vector3(1.53f,.24f,1.53f),"Cream",.12f);
                    Pipe(room,"CoolantPipe",new Vector3(-2+i*2,3.15f,-3.5f),new Vector3(-2+i*2,3.15f,-5.4f),.13f,"Orange");
                }
            }
            if(index==2){
                Box(room,"WasteProcessor",new Vector3(1,1.25f,-3),new Vector3(3.6f,2.5f,2.4f),"Cream",.22f);
                Box(room,"WasteIntake",new Vector3(1,1.4f,-1.77f),new Vector3(2.5f,1.1f,.08f),"Dark",.12f);
                Box(room,"WarningStripe",new Vector3(1,2.05f,-1.7f),new Vector3(2.6f,.17f,.07f),"Yellow",.025f);
            }
            if(index==3){
                Box(room,"Workbench",new Vector3(0,1,-3.5f),new Vector3(5,2,1.4f),"Hull",.1f);
                Repair(room,new Vector3(0,2.8f,-4));
            }
            if(index==5){Console(room,new Vector3(0,0,2));Console(room,new Vector3(-2.6f,0,-2));}
            Box(room,"CeilingLight",new Vector3(0,3.8f,-5.7f),new Vector3(2,.15f,.25f),"Mint",.04f,false);
        }
        for(int i=-8;i<=8;i++){
            Box(interior,"CorridorTile",new Vector3(0,-.04f,i*2.1f),new Vector3(3.85f,.08f,2.02f),"Hull",.02f);
            Box(interior,"WayfindingDash",new Vector3(0,.007f,i*2.1f),new Vector3(.12f,.012f,.65f),"Yellow",.005f,false);
        }
        foreach(int side in new[]{-1,1}) {
            // Starboard wall has a real opening into the airlock.
            if(side<0)Box(exterior,"PortHull",new Vector3(-12,2,0),new Vector3(.6f,4.4f,38),"Cream",.17f);
            else foreach(float z in new[]{-11f,11f})Box(exterior,"StarboardHull",new Vector3(12,2,z),new Vector3(.6f,4.4f,16),"Cream",.17f);
            for(int iz=-1;iz<=1;iz++){
                Box(roof,"RoofModule",new Vector3(side*6,4.35f,iz*12),new Vector3(11.8f,.7f,11.8f),"Cream",.24f);
                Box(roof,"RoofStripe",new Vector3(side*6,4.72f,iz*12),new Vector3(2.1f,.08f,11.5f),"Orange",.02f,false);
                if(side<0 || iz!=0)Box(exterior,"HullRib",new Vector3(side*12.36f,1.9f,iz*12),new Vector3(.35f,4.5f,.9f),"Hull",.1f);
                float patchZ=iz*12+(side>0&&iz==0?5:3);
                Box(exterior,"HullPatch",new Vector3(side*12.34f,1.4f,patchZ),new Vector3(.1f,1.6f,2.3f),"Hull",.05f,false);
                for(int k=0;k<2;k++)Orb(exterior,"HullBolt",new Vector3(side*12.42f,1.4f,patchZ-.7f+k*1.4f),Vector3.one*.17f,"Dark");
            }
            Box(exterior,"EnginePod",new Vector3(side*8.7f,1.5f,-21),new Vector3(5,5,7),"Hull",.85f);
            Box(exterior,"EngineBand",new Vector3(side*8.7f,1.5f,-20.5f),new Vector3(5.1f,5.1f,1.2f),"Orange",.85f);
            var nozzle=Shape(exterior,"EngineNozzle",new Vector3(side*8.7f,1.5f,-24.6f),new Vector3(3.6f,.5f,3.6f),"Dark",PrimitiveType.Cylinder,true);nozzle.transform.localRotation=Quaternion.Euler(90,0,0);
            var glow=Shape(exterior,"EngineCore",new Vector3(side*8.7f,1.5f,-25.12f),new Vector3(2.3f,.02f,2.3f),"Teal",PrimitiveType.Cylinder);glow.transform.localRotation=Quaternion.Euler(90,0,0);
            // Exterior service catwalks with gaps at the airlock.
            foreach(float z in new[]{-10f,10f}){
                Box(exterior,"ServiceWalkway",new Vector3(side*13.6f,-.15f,z),new Vector3(2.7f,.3f,14),"Steel",.08f);
                Pipe(exterior,"SafetyRail",new Vector3(side*14.9f,1.1f,z-6.8f),new Vector3(side*14.9f,1.1f,z+6.8f),.07f,"Yellow");
                for(int i=-1;i<=1;i++)Pipe(exterior,"RailPost",new Vector3(side*14.9f,0,z+i*6.5f),new Vector3(side*14.9f,1.1f,z+i*6.5f),.07f,"Yellow");
            }
            var repair=Repair(exterior,new Vector3(side*12.6f,1.9f,8));repair.transform.localRotation=Quaternion.Euler(0,side*90,0);
            Group(markers,"ExteriorRepair_"+side,new Vector3(side*13.8f,0,8));
        }
        Box(exterior,"RearBulkhead",new Vector3(0,2,-19),new Vector3(24,4.4f,.6f),"Cream",.17f);
        Box(exterior,"FrontBulkhead",new Vector3(0,2,19),new Vector3(24,4.4f,.6f),"Cream",.17f);
        Box(exterior,"NoseBumper",new Vector3(0,.7f,20),new Vector3(22,2.7f,2.4f),"Hull",.7f);
        Box(roof,"RaisedCockpit",new Vector3(0,5.25f,15.8f),new Vector3(9.4f,2.6f,5.8f),"Hull",.7f);
        Box(roof,"CockpitVisor",new Vector3(0,5.55f,18.64f),new Vector3(7.8f,1.3f,.25f),"Visor",.2f,false);
        Box(roof,"CockpitBrow",new Vector3(0,6.3f,18.65f),new Vector3(8.9f,.24f,.5f),"Orange",.1f);
        foreach(float x in new[]{-2.6f,0,2.6f})Box(roof,"CockpitFrame",new Vector3(x,5.55f,18.81f),new Vector3(.17f,1.4f,.1f),"Cream",.03f,false);
        for(int i=-2;i<=2;i++)Box(exterior,"ForwardWindow",new Vector3(i*3.9f,2.8f,19.35f),new Vector3(3.1f,1.35f,.2f),"Visor",.24f,false);
        var air=Group(exterior,"Airlock",new Vector3(14,0,0)).transform;
        Box(air,"Floor",new Vector3(0,-.15f,0),new Vector3(4.5f,.3f,6),"Steel",.07f);
        foreach(float z in new[]{-3f,3f})Box(air,"Wall",new Vector3(0,1.8f,z),new Vector3(4.5f,3.6f,.25f),"Yellow",.1f);
        Box(roof,"AirlockRoof",new Vector3(14,3.8f,0),new Vector3(4.5f,.3f,6.2f),"Hull",.1f);
        // Open door leaves: visually separate for future animations and interlocks.
        foreach(float z in new[]{-2.35f,2.35f})Box(air,"OuterDoor_Open",new Vector3(2.25f,1.65f,z),new Vector3(.24f,3.3f,1.2f),"Cream",.1f);
        Box(air,"OuterLintel",new Vector3(2.25f,3.5f,0),new Vector3(.3f,.35f,6),"Orange",.05f);
        Group(markers,"TetherAnchor",new Vector3(14,1.2f,2.6f));
        Group(markers,"CrewSpawn",new Vector3(0,0,10));
        Group(markers,"AirlockExit",new Vector3(16.5f,0,0));
        // Rooftop repair equipment: visibly chunky, readable from a distance.
        Box(roof,"VentHousing",new Vector3(-5,5.05f,0),new Vector3(4,.8f,6),"Hull",.2f);
        for(int i=0;i<6;i++)Box(roof,"VentFin",new Vector3(-5,5.52f,-2+i*.8f),new Vector3(3.5f,.1f,.25f),"Dark",.03f,false);
        Pipe(roof,"AntennaMast",new Vector3(5,4.7f,10),new Vector3(5,8,10),.15f,"Dark");
        Orb(roof,"AntennaCap",new Vector3(5,8,10),Vector3.one*.65f,"Orange");
        Pipe(roof,"AntennaCrossbar",new Vector3(3.5f,7,10),new Vector3(6.5f,7,10),.1f,"Cream");
        Group(markers,"RoofRepair",new Vector3(-5,5.6f,0));
        return root;
    }
    static void Save(GameObject g,string name){
        string path=Root+"/Prefabs/"+name+".prefab";
        var prefab=PrefabUtility.SaveAsPrefabAsset(g,path);
        if(prefab==null)throw new Exception("Failed prefab: "+path);
    }
    static void SetLighting(){
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.4f,.45f);
        RenderSettings.ambientIntensity=1;
        var key=Group(null,"StudioKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.85f;key.color=new Color(1,.92f,.8f);key.shadows=LightShadows.Soft;
        key.transform.rotation=Quaternion.Euler(48,-32,0);key.shadowBias=.025f;key.shadowNormalBias=.15f;
        var fill=Group(null,"StudioFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.color=new Color(.69f,.83f,1);fill.transform.rotation=Quaternion.Euler(25,140,0);
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadowDistance=150;QualitySettings.antiAliasing=4;
    }
    static void Render(string filename,Vector3 eye,Vector3 target,float size){
        var camera=Group(null,"PreviewCamera").AddComponent<Camera>();camera.transform.position=eye;camera.transform.LookAt(target);
        camera.orthographic=true;camera.orthographicSize=size;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.72f,.79f,.8f);camera.farClipPlane=1000;
        var rt=new RenderTexture(1800,1200,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1800,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1800,1200),0,0);image.Apply();
        File.WriteAllBytes(Output+"/"+filename,image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }
    static void Validate(GameObject ship,List<GameObject> characters){
        int triangles=0;
        foreach(var pair in meshes){var mesh=pair.Value;
            foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.x))throw new Exception("Invalid vertex");
            var verts=mesh.vertices;var idx=mesh.triangles;
            for(int i=0;i<idx.Length;i+=3)if(Vector3.Cross(verts[idx[i+1]]-verts[idx[i]],verts[idx[i+2]]-verts[idx[i]]).sqrMagnitude<1e-12f)throw new Exception("Degenerate triangle: "+pair.Key);
        }
        foreach(var mf in ship.GetComponentsInChildren<MeshFilter>())triangles+=mf.sharedMesh.triangles.Length/3;
        checks.Add("PASS: All generated meshes have non-degenerate triangles.");checks.Add("Ship rendered triangles: "+triangles);
        checks.Add("PASS: 4 character prefabs, 1 complete ship prefab, 3 prop prefabs saved.");
        foreach(var c in characters){int count=0;foreach(var m in c.GetComponentsInChildren<MeshFilter>())count+=m.sharedMesh.triangles.Length/3;checks.Add(c.name+" triangles: "+count);}
        foreach(string anchor in new[]{"CrewSpawn","AirlockExit","TetherAnchor","RoofRepair","ExteriorRepair_1","ExteriorRepair_-1"})
            if(ship.transform.Find("GameplayAnchors/"+anchor)==null)throw new Exception("Missing anchor: "+anchor);
        checks.Add("PASS: Gameplay anchors present; central corridor 3.85 m, room openings 3 m.");
        checks.Add("LIMIT: Character meshes are static segmented artwork, not skinned or animated.");
        checks.Add("LIMIT: Gameplay, multiplayer, gravity, tether, airlock interlocks and mission logic are not implemented.");
        checks.Add("LIMIT: 10-20 minute round length requires gameplay testing; not established by model size.");
    }

    public static void RecheckAndExport(){
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var report=new List<string>();int count=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"})){
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            foreach(var r in g.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
                if(m==null || m.shader==null || !m.shader.isSupported)throw new Exception("Missing/unsupported material: "+path);
            foreach(var f in g.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh==null)throw new Exception("Missing mesh: "+path);
            report.Add("PASS reloaded prefab: "+path);count++;UnityEngine.Object.DestroyImmediate(g);
        }
        if(count!=8)throw new Exception("Expected 8 prefabs, found "+count);
        var ship=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Ship/SJ_Workship_Complete.prefab"));
        Physics.SyncTransforms();
        AssertClear(new Vector3(0,1,17),Vector3.back,34,"Central corridor",report);
        foreach(float z in new[]{-12f,0,12f})foreach(int side in new[]{-1,1})
            AssertClear(new Vector3(0,1,z),Vector3.right*side,7,"Room entry "+side+" / "+z,report);
        AssertClear(new Vector3(7,1,0),Vector3.right,10,"Airlock passage",report);
        if(!Physics.Raycast(new Vector3(0,1,10),Vector3.down,2))throw new Exception("Missing floor collider");
        report.Add("PASS: Spawn floor has a collider.");
        report.Add("Passage checks use centre-line raycasts, not full character movement simulation.");
        report.Add("Unity version: "+Application.unityVersion);
        AssetDatabase.Refresh();AssetDatabase.ExportPackage(Root,Output+"/SpaceJanitors_StarterArt.unitypackage",ExportPackageOptions.Recurse);
        File.WriteAllLines(Output+"/Recheck.txt",report);
        Debug.Log("SJ_RECHECK_SUCCESS");
    }
    static void AssertClear(Vector3 origin,Vector3 direction,float length,string label,List<string> report){
        if(Physics.Raycast(origin,direction,out RaycastHit hit,length))throw new Exception(label+" blocked by "+hit.collider.name);
        report.Add("PASS: "+label+" centre-line unobstructed.");
    }
}
