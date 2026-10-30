using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

public static class BuildPlayable
{
    const string Root="Assets/SpaceJanitors";
    const string ScenePath=Root+"/Scenes/FirstPersonTest.unity";
    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SJ_"+name+".mat");
    static GameObject Obj(string name,Vector3 pos,Vector3 size,string color,PrimitiveType type=PrimitiveType.Cube,bool collision=true)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=Mat(color);
        if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static GameObject Prefab(string path)=> (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+path+".prefab"));
    static void Label(string value,Vector3 pos,Quaternion rotation,float size,Color color)
    {
        var g=new GameObject("Sign_"+value);g.transform.SetPositionAndRotation(pos,rotation);
        var t=g.AddComponent<TextMesh>();t.text=value;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");g.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;
    }
    static void LightAt(Vector3 position,Color color,float intensity,float range)
    {
        var g=new GameObject("WorkLight");g.transform.position=position;var light=g.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;
    }
    [MenuItem("Space Janitors/Build Playable Test")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.51f,.58f,.64f);
        RenderSettings.skybox=null;
        var sun=new GameObject("DistantSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.85f;sun.color=new Color(1,.88f,.7f);sun.transform.rotation=Quaternion.Euler(55,-45,0);sun.shadows=LightShadows.Soft;
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=80;QualitySettings.antiAliasing=4;
        var ship=Prefab("Ship/SJ_Workship_Complete");
        foreach(var tr in ship.GetComponentsInChildren<Transform>()){
            if(tr.name=="CargoCrate"){
                var rb=tr.gameObject.AddComponent<Rigidbody>();rb.mass=12;rb.linearDamping=1;rb.angularDamping=.5f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var item=tr.gameObject.AddComponent<SJInteractable>();item.kind=SJInteractable.Kind.Cargo;item.displayName="무거운 화물 / CARGO";
            }
            if(tr.name=="MissionConsole") {var item=tr.gameObject.AddComponent<SJInteractable>();item.kind=SJInteractable.Kind.Computer;item.displayName="미션 컴퓨터";}
        }
        // Only these two panels count as test missions; others remain set dressing.
        var inside=ship.transform.Find("Interior/Workshop/RepairPanel");inside.localPosition=new Vector3(0,1.9f,-2.7f);
        var task=inside.gameObject.AddComponent<SJInteractable>();task.kind=SJInteractable.Kind.Repair;task.displayName="내부 배선 패널";
        foreach(var tr in ship.transform.Find("Exterior").GetComponentsInChildren<Transform>())if(tr.name=="RepairPanel"&&tr.position.x>0){
            task=tr.gameObject.AddComponent<SJInteractable>();task.kind=SJInteractable.Kind.Repair;task.displayName="외부 냉각 패널";
        }
        // Distinct signs at player eye level help navigation in first person.
        for(int i=-1;i<=1;i++)foreach(int side in new[]{-1,1}){
            string[] left={"01  CARGO","03  RECYCLING","05  LIFE SUPPORT"};string[] right={"02  COOLING","04  WORKSHOP / AIRLOCK","06  BRIDGE"};
            Label(side<0?left[i+1]:right[i+1],new Vector3(side*1.9f,2.9f,i*12),Quaternion.Euler(0,side<0?90:-90,0),.055f,new Color(.12f,.25f,.28f));
            LightAt(new Vector3(side*6,3.3f,i*12),new Color(.7f,.91f,1),1.5f,12);
        }
        for(int i=-2;i<=2;i++)LightAt(new Vector3(0,3.4f,i*7),new Color(1,.92f,.76f),1.2f,10);
        Label("SPACE JANITORS\nMAINTENANCE DECK",new Vector3(0,2.4f,-18.65f),Quaternion.Euler(0,180,0),.075f,new Color(.1f,.22f,.24f));
        Label("EXIT  /  EVA",new Vector3(11.63f,2.7f,0),Quaternion.Euler(0,-90,0),.075f,new Color(.17f,.26f,.28f));
        Label("REPAIR  [F]",new Vector3(7,2.85f,-2.65f),Quaternion.identity,.045f,Color.white);

        var game=new GameObject("TestSession").AddComponent<SJGame>();
        var player=new GameObject("FirstPersonPlayer");player.layer=2;
        var cc=player.AddComponent<CharacterController>();cc.height=1.9f;cc.radius=.36f;cc.center=new Vector3(0,.95f,0);cc.stepOffset=.3f;cc.skinWidth=.025f;cc.minMoveDistance=0;
        var controller=player.AddComponent<SJPlayer>();game.player=controller;
        player.transform.SetPositionAndRotation(new Vector3(0,.1f,10),Quaternion.Euler(0,180,0));
        var camera=new GameObject("FirstPersonCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(player.transform,false);camera.transform.localPosition=new Vector3(0,1.68f,0);
        camera.fieldOfView=78;camera.nearClipPlane=.045f;camera.farClipPlane=900;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.016f,.033f);controller.view=camera;
        var lamp=new GameObject("HelmetLight").AddComponent<Light>();lamp.transform.SetParent(camera.transform,false);lamp.type=LightType.Spot;lamp.spotAngle=85;lamp.innerSpotAngle=45;lamp.range=22;lamp.intensity=2.8f;lamp.color=new Color(.85f,.95f,1);
        foreach(int side in new[]{-1,1}){
            var hand=new GameObject(side<0?"LeftHand":"RightHand");hand.transform.SetParent(camera.transform,false);hand.transform.localPosition=new Vector3(side*.31f,-.3f,.43f);hand.layer=2;
            var sleeve=Obj("Sleeve",Vector3.zero,new Vector3(.16f,.16f,.4f),"Orange",PrimitiveType.Capsule,false);sleeve.transform.SetParent(hand.transform,false);sleeve.transform.localRotation=Quaternion.Euler(90,0,side*-12);sleeve.transform.localPosition=new Vector3(0,-.12f,-.08f);sleeve.layer=2;
            var glove=Obj("Glove",Vector3.zero,new Vector3(.19f,.18f,.22f),"Rubber",PrimitiveType.Sphere,false);glove.transform.SetParent(hand.transform,false);glove.layer=2;
            if(side<0)controller.leftHand=hand.transform;else controller.rightHand=hand.transform;
        }
        var connector=Obj("TetherConnector",new Vector3(11.35f,1.3f,2.25f),new Vector3(.45f,.6f,.45f),"Yellow");
        var tether=connector.AddComponent<SJInteractable>();tether.kind=SJInteractable.Kind.Tether;tether.displayName="안전줄 연결기";
        Label("TETHER [F]",new Vector3(10.99f,2.02f,2.25f),Quaternion.Euler(0,-90,0),.055f,new Color(.13f,.2f,.24f));
        LightAt(new Vector3(14,3,0),new Color(1,.72f,.27f),1.5f,9);

        var bin=new GameObject("RecyclingBin");bin.transform.position=new Vector3(-5,0,1.6f);
        foreach(var part in new[]{new Vector3(0,.15f,0),new Vector3(-.85f,.65f,0),new Vector3(.85f,.65f,0),new Vector3(0,.65f,-.75f),new Vector3(0,.35f,.75f)}){
            Vector3 size=part.y<.2f?new Vector3(1.9f,.3f,1.7f):(Mathf.Abs(part.x)>.5f?new Vector3(.2f,1.2f,1.7f):new Vector3(1.7f,part.z>0?.6f:1.2f,.2f));
            var wall=Obj("BinWall",Vector3.zero,size,"Orange");wall.transform.SetParent(bin.transform,false);wall.transform.localPosition=part;
        }
        var binTrigger=new GameObject("CollectionVolume");binTrigger.transform.SetParent(bin.transform,false);binTrigger.transform.localPosition=new Vector3(0,.92f,0);
        var trigger=binTrigger.AddComponent<BoxCollider>();trigger.size=new Vector3(1.4f,1.1f,1.2f);trigger.isTrigger=true;binTrigger.AddComponent<SJBin>();
        Label("RECYCLE HERE",new Vector3(-5,1.9f,1.6f),Quaternion.identity,.06f,new Color(1,.77f,.26f));
        Vector3[] trash={new Vector3(.65f,.4f,6.5f),new Vector3(-3.3f,.4f,1),new Vector3(-7,.4f,-10),new Vector3(5,.4f,-10),new Vector3(5,.4f,10)};
        for(int i=0;i<trash.Length;i++){
            var g=Obj("Waste_"+i,trash[i],i%2==0?new Vector3(.42f,.42f,.55f):Vector3.one*.45f,i%2==0?"Teal":"Yellow",i%2==0?PrimitiveType.Cube:PrimitiveType.Sphere);
            g.transform.rotation=Quaternion.Euler(i*13,20+i*29,15);var rb=g.AddComponent<Rigidbody>();rb.mass=1;rb.linearDamping=.4f;rb.angularDamping=.5f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var item=g.AddComponent<SJInteractable>();item.kind=SJInteractable.Kind.Waste;item.displayName="폐기물 "+(i+1)+" / 수거함에 넣으세요";
        }
        var dummy=Prefab("Characters/Janitor_Teal");dummy.name="CrewScaleReference";dummy.transform.position=new Vector3(7,0,13);dummy.transform.rotation=Quaternion.Euler(0,210,0);
        Label("CREW SCALE REFERENCE",new Vector3(7,2.5f,13),Quaternion.Euler(0,210,0),.035f,Color.white);
        SpaceBackground();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.companyName="SpaceJanitorsPrototype";PlayerSettings.productName="Space Janitors - First Person Test";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/FirstPersonTest/SpaceJanitors.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("SJ_PLAYABLE_BUILD_SUCCESS: "+output);
    }
    static void SpaceBackground()
    {
        var stars=new GameObject("Starfield");var vertices=new List<Vector3>();var triangles=new List<int>();var random=new System.Random(72);
        for(int i=0;i<900;i++){
            Vector3 normal=new Vector3((float)random.NextDouble()*2-1,(float)random.NextDouble()*2-1,(float)random.NextDouble()*2-1).normalized;
            Vector3 centre=normal*380;Vector3 tangent=Vector3.Cross(normal,Vector3.up).normalized;Vector3 bitangent=Vector3.Cross(normal,tangent);float s=.13f+(float)random.NextDouble()*.55f;int j=vertices.Count;
            vertices.Add(centre-tangent*s-bitangent*s);vertices.Add(centre+tangent*s-bitangent*s);vertices.Add(centre+bitangent*s);triangles.AddRange(new[]{j,j+2,j+1,j,j+1,j+2});
        }
        var mesh=new Mesh{name="SJ_Stars"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        SaveAsset(mesh,Root+"/Meshes/Stars.asset");stars.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Stars.asset");
        var mat=new Material(Shader.Find("Unlit/Color")){color=new Color(.68f,.82f,.9f)};SaveAsset(mat,Root+"/Materials/Stars.mat");stars.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Stars.mat");
        var planet=Obj("DistantPlanet",new Vector3(210,-90,145),Vector3.one*120,"Teal",PrimitiveType.Sphere,false);
        var moon=Obj("SmallMoon",new Vector3(170,70,-160),Vector3.one*35,"Cream",PrimitiveType.Sphere,false);
    }
    static void SaveAsset(UnityEngine.Object asset,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);}else AssetDatabase.CreateAsset(asset,path);}
}
