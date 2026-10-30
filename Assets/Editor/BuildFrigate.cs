using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using static FrigateGeometry;

public static class BuildFrigate
{
    static string Out=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/FrigateV2"));
    static Color ice=new Color(.51f,.78f,.9f),warm=new Color(1,.69f,.34f);
    static GameObject ship;
    [MenuItem("Space Janitors/Build Kestrel Frigate")]
    public static void Build()
    {
        Directory.CreateDirectory(Out);Init();
        EditorSceneManager.OpenScene("Assets/SpaceJanitors/Scenes/FirstPersonTest.unity");
        var old=GameObject.Find("SJ_Workship_Complete");if(old)UnityEngine.Object.DestroyImmediate(old);
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if(root.name.StartsWith("Sign_")||root.name=="WorkLight")UnityEngine.Object.DestroyImmediate(root);
        ship=Group(null,"KESTREL_07_MaintenanceFrigate");
        Exterior();MainDeck();LowerDeck();Bridge();
        ConfigurePlayer();UpgradeLooseProps();
        foreach(Transform sector in ship.transform)if(sector.name!="Interactive")Combine(sector);
        PrefabUtility.SaveAsPrefabAsset(ship,Root+"/Prefabs/Kestrel07_Complete.prefab");
        Lighting();AssetDatabase.SaveAssets();
        SavePreviews();
        string scene=Root+"/Scenes/Kestrel_FirstPerson.unity";
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene,true)};
        PlayerSettings.productName="Space Janitors - Kestrel Frigate";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Validate();
        AssetDatabase.ExportPackage(new[]{Root,"Assets/SpaceJanitors/Runtime"},Out+"/Kestrel07_Unity.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
        string exe=Out+"/Windows/SpaceJanitors_Kestrel.exe";Directory.CreateDirectory(Path.GetDirectoryName(exe));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Kestrel build failed: "+report.summary.result);
        Debug.Log("KESTREL_BUILD_SUCCESS");
    }
    static Transform Sector(string name)=>Group(ship.transform,name).transform;
    public static void RenderSaved()
    {
        EditorSceneManager.OpenScene(Root+"/Scenes/Kestrel_FirstPerson.unity");
        ship=GameObject.Find("KESTREL_07_MaintenanceFrigate");
        SavePreviews();Validate();
        Debug.Log("KESTREL_RELOADED_RENDER_SUCCESS");
    }
    static void Exterior()
    {
        var p=Sector("Exterior_Hull");
        // Chamfered, tapering hull. The open starboard segment is the actual EVA airlock.
        Loft(p,"MainHull",new[]{-32f,-27,-19,-3,3,19,29,39,43},new[]{7.5f,12.25f,12.25f,12.25f,12.25f,12.25f,8,6.8f,2.2f},
            new[]{-4.9f,-5.1f,-5.1f,-5.1f,-5.1f,-5.1f,-2.5f,2.9f,2.9f},new[]{3.9f,4.25f,4.25f,4.25f,4.25f,4.25f,4.4f,4.4f,3.8f},"Steel",true);
        Box(p,"AftBulkhead",new Vector3(0,-.6f,-31.8f),new Vector3(14.7f,8.6f,.35f),"Graphite",.1f,true);
        Box(p,"NoseCap",new Vector3(0,3.28f,43),new Vector3(3.5f,.6f,.35f),"Alloy",.1f,true);
        RoofWing(p,-1);RoofWing(p,1);
        // Large overlapping armor plates with dark separation gaps, fasteners and service hatches.
        for(int side=-1;side<=1;side+=2){
            for(int j=0;j<8;j++){
                float z=-24+j*6;
                if(Mathf.Abs(z)<3.5f)continue;
                Panel(p,new Vector3(side*12.3f,.6f,z),new Vector2(5.55f,3.7f),Quaternion.Euler(0,side*90,0),j%3==0?"Alloy":"Ivory");
                Box(p,"ServiceBelt",new Vector3(side*12.49f,-1.57f,z),new Vector3(.1f,.18f,5.2f),"Amber",.02f);
                Vent(p,new Vector3(side*12.49f,2.33f,z),new Vector2(2.5f,.57f),Quaternion.Euler(0,side*90,0));
                Panel(p,new Vector3(side*11.2f,-3.85f,z),new Vector2(5.3f,1.4f),Quaternion.Euler(0,side*90,side*16),"Graphite",false);
            }
            // Roof armor is laid on both sides of a raised service spine.
            for(int z=-24;z<=16;z+=5)for(int k=0;k<2;k++){
                Panel(p,new Vector3(side*(3.1f+k*4.1f),4.28f,z),new Vector2(3.83f,4.7f),Quaternion.Euler(-90,0,0),"Alloy");
                if(k==1)Box(p,"RoofAccent",new Vector3(side*7.8f,4.45f,z),new Vector3(.21f,.05f,4.1f),"Amber",.01f);
            }
            for(int i=0;i<4;i++)Vent(p,new Vector3(side*6.1f,4.48f,-19+i*10),new Vector2(3.3f,2.25f),Quaternion.Euler(-90,0,0));
            Box(p,"ArmorShoulder",new Vector3(side*10.65f,3.8f,-16),new Vector3(3.4f,.75f,10.5f),"Ivory",.24f).transform.localRotation=Quaternion.Euler(0,side*8,side*-14);
            for(int j=0;j<3;j++){
                Box(p,"ManeuverThrusterHousing",new Vector3(side*13,1,-18+j*1.7f),new Vector3(1.5f,1.4f,1.35f),"Graphite",.18f);
                Cylinder(p,"ManeuverNozzle",new Vector3(side*13.5f,1,-18+j*1.7f),new Vector3(side*14,1,-18+j*1.7f),.4f,"Rubber");
            }
            // Exposed external pipe runs bridge panels without crossing the EVA opening.
            for(int run=0;run<2;run++){
                Cylinder(p,"CoolantSupply",new Vector3(side*12.5f,-2.25f-run*.26f,4),new Vector3(side*12.5f,-2.25f-run*.26f,18),.075f,run==0?"Copper":"Steel");
                for(int z=4;z<=18;z+=2)Box(p,"PipeClamp",new Vector3(side*12.53f,-2.4f,z),new Vector3(.12f,.7f,.12f),"Alloy",.015f);
            }
            Text(p,"KESTREL / 07",new Vector3(side*12.5f,.45f,13),Quaternion.Euler(0,side<0?-90:90,0),.12f,new Color(.18f,.24f,.28f));
            // Engine mounting beams and four staged nozzle rings.
            var engine=Group(p,"Engine_"+side,new Vector3(side*8.6f,-.35f,-30)).transform;
            Loft(engine,"EngineCowl_"+side,new[]{-7.5f,-5,-1,6},new[]{2.5f,3.2f,3.2f,2.3f},new[]{-2.5f,-2.8f,-2.8f,-1.9f},new[]{2.5f,2.8f,2.8f,1.9f},"Graphite");
            for(int z=-5;z<5;z+=3)Box(engine,"CowlBrace",new Vector3(0,2.9f,z),new Vector3(4.8f,.25f,.4f),"Alloy",.07f);
            Ring(engine,"OuterExhaust",new Vector3(0,0,-7.6f),2.5f,2.15f,.75f,"Alloy");
            Ring(engine,"ThermalCollar",new Vector3(0,0,-7.95f),2.13f,1.87f,.3f,"Copper");
            Ring(engine,"ExhaustGlow",new Vector3(0,0,-7.7f),1.85f,1.68f,.1f,"Cyan");
            Cylinder(engine,"DeepNozzle",new Vector3(0,0,-6.8f),new Vector3(0,0,-7.0f),1.85f,"Rubber",24);
            for(int j=0;j<12;j++){float a=j*30*Mathf.Deg2Rad;Beam(engine,"TurbineFin",new Vector3(Mathf.Cos(a)*.45f,Mathf.Sin(a)*.45f,-7.12f),new Vector3(Mathf.Cos(a+.15f)*1.6f,Mathf.Sin(a+.15f)*1.6f,-7.12f),.14f,.22f,"Steel");}
            Cylinder(engine,"TurbineHub",new Vector3(0,0,-7),new Vector3(0,0,-7.2f),.4f,"Cyan");
            for(int i=0;i<5;i++)Box(engine,"CoolingFin",new Vector3(side*3.4f,0,-4+i*1.6f),new Vector3(.85f,3.5f,.2f),"Alloy",.04f);
        }
        Box(p,"DorsalServiceSpine",new Vector3(0,4.6f,-6),new Vector3(2.1f,.75f,32),"Graphite",.15f);
        for(int z=-20;z<=8;z+=4)Panel(p,new Vector3(0,5.03f,z),new Vector2(1.65f,3.4f),Quaternion.Euler(-90,0,0),"Steel");
        for(int i=0;i<2;i++){
            float x=i==0?-3:3;
            Cylinder(p,"AntennaBase",new Vector3(x,4.4f,8),new Vector3(x,5.2f,8),.5f,"Graphite");
            Cylinder(p,"AntennaMast",new Vector3(x,5.2f,8),new Vector3(x,8.5f+i,8),.07f,"Alloy");
            Box(p,"AntennaSignal",new Vector3(x,8.6f+i,8),Vector3.one*.13f,"AmberLight",.02f);
        }
        Airlock(p);
    }
    static void Airlock(Transform p)
    {
        var a=Group(p,"EVA_Airlock",new Vector3(14,0,0)).transform;
        Box(a,"Deck",new Vector3(0,-.13f,0),new Vector3(4.4f,.26f,5.8f),"Floor",.04f,true);
        foreach(int side in new[]{-1,1}){
            Box(a,"SideWall",new Vector3(0,1.7f,side*2.9f),new Vector3(4.4f,3.4f,.22f),"Graphite",.07f,true);
            for(int i=0;i<3;i++)Panel(a,new Vector3(-1.4f+i*1.4f,1.8f,side*2.73f),new Vector2(1.15f,2.65f),Quaternion.Euler(0,side>0?180:0,0),"Alloy");
            Box(a,"PortalPost",new Vector3(2.25f,1.65f,side*1.75f),new Vector3(.4f,3.3f,.4f),"Amber",.06f,true);
            Box(a,"PortalLamp",new Vector3(2.48f,1.8f,side*1.75f),new Vector3(.05f,1.4f,.09f),"WhiteLight",.01f);
        }
        Box(a,"Roof",new Vector3(0,3.5f,0),new Vector3(4.6f,.24f,6),"Steel",.07f,true);
        Box(a,"PortalHeader",new Vector3(2.25f,3.27f,0),new Vector3(.4f,.25f,3.85f),"Amber",.05f);
        Text(a,"AIRLOCK  /  EVA-01",new Vector3(-1.75f,2.95f,0),Quaternion.Euler(0,90,0),.065f,ice);
        for(int i=0;i<8;i++)Box(a,"HazardThreshold",new Vector3(1.5f,-.005f,-1.5f+i*.42f),new Vector3(.8f,.03f,.12f),i%2==0?"Amber":"Rubber",.01f);
        for(int side=-1;side<=1;side+=2)for(int section=0;section<2;section++){
            float z=section==0?-10:10;
            Box(p,"ExternalCatwalk",new Vector3(side*13.75f,-.12f,z),new Vector3(2.8f,.24f,14),"Graphite",.04f,true);
            for(int j=0;j<27;j++)Box(p,"CatwalkTread",new Vector3(side*13.75f,.018f,z-6.5f+j*.5f),new Vector3(2.55f,.045f,.055f),"Steel",.012f);
            Cylinder(p,"EVA_Handrail",new Vector3(side*15.1f,1.1f,z-7),new Vector3(side*15.1f,1.1f,z+7),.048f,"Amber");
            for(int j=-1;j<=1;j++)Cylinder(p,"RailStanchion",new Vector3(side*15.1f,0,z+j*6.8f),new Vector3(side*15.1f,1.1f,z+j*6.8f),.048f,"Alloy");
        }
        RepairPanel(new Vector3(12.62f,1.8f,8),Quaternion.Euler(0,90,0),"외부 냉각 배선");
    }
    static void FloorTiles(Transform p,float xmin,float xmax,float zmin,float zmax,float y)
    {
        for(float x=xmin;x<xmax-.1f;x+=2)for(float z=zmin;z<zmax-.1f;z+=2){
            float sx=Mathf.Min(2,xmax-x)-.025f,sz=Mathf.Min(2,zmax-z)-.025f;
            Box(p,"DeckPlate",new Vector3(x+(sx+.025f)*.5f,y-.065f,z+(sz+.025f)*.5f),new Vector3(sx,.13f,sz),"Floor",.018f,true);
            if(Mathf.Abs(x)<2)foreach(float dx in new[]{-.65f,.65f})Box(p,"DeckInset",new Vector3(x+1+dx,y+.008f,z+1),new Vector3(.025f,.014f,1.5f),"Steel",.004f);
        }
    }
    static void MainDeck()
    {
        var corridor=Sector("Main_Concourse");FloorTiles(corridor,-2,2,-19,19,0);
        Box(corridor,"Ceiling",new Vector3(0,3.9f,0),new Vector3(4.1f,.24f,38),"Graphite",.03f,true);
        for(int z=-18;z<=18;z+=3){
            foreach(int side in new[]{-1,1}){
                Box(corridor,"DeckGuide",new Vector3(side*1.86f,.018f,z),new Vector3(.06f,.025f,2.6f),"Cyan",.007f);
                Box(corridor,"CeilingLightTrack",new Vector3(side*1.35f,3.74f,z),new Vector3(.18f,.06f,2.25f),"WhiteLight",.012f);
                Box(corridor,"CeilingCable",new Vector3(side*.7f,3.68f,z),new Vector3(.13f,.16f,2.95f),"Steel",.025f);
            }
            if(z%6==0){Arch(corridor,new Vector3(0,0,z),4,3.7f);Lamp(corridor,new Vector3(0,3.2f,z),ice,.72f,7);}
        }
        for(int side=-1;side<=1;side+=2)for(int zone=-1;zone<=1;zone++){
            float z=zone*12;string[] left={"01 / CARGO","03 / RECYCLING","05 / MEDICAL"},right={"02 / ENGINEERING","04 / WORKSHOP","06 / CREW QUARTERS"};
            var room=Sector((side<0?"Port_":"Starboard_")+zone);room.localPosition=new Vector3(side*7,0,z);
            FloorTiles(room,-4.85f,4.85f,-5.85f,5.85f,0);
            Box(room,"Ceiling",new Vector3(0,3.67f,0),new Vector3(9.8f,.2f,11.9f),"Graphite",.02f,true);
            // Corridor-facing wall segments leave an actual 3 m clear doorway.
            foreach(float offset in new[]{-3.75f,3.75f}){
                Box(room,"WallStructure",new Vector3(-side*4.93f,1.8f,offset),new Vector3(.18f,3.6f,4.5f),"Graphite",.02f,true);
                for(int j=0;j<2;j++){
                    Vector3 position=new Vector3(-side*5.04f,1.65f,offset-.98f+j*1.96f);
                    Panel(room,position,new Vector2(1.8f,2.6f),Quaternion.Euler(0,-side*90,0),"Ivory");
                    Vent(room,new Vector3(-side*5.18f,.4f,position.z),new Vector2(1.25f,.35f),Quaternion.Euler(0,-side*90,0));
                }
            }
            Box(room,"DoorHeader",new Vector3(-side*4.93f,3.37f,0),new Vector3(.48f,.48f,3.3f),"Steel",.07f,true);
            foreach(float dz in new[]{-1.56f,1.56f}){
                Box(room,"DoorJamb",new Vector3(-side*4.93f,1.6f,dz),new Vector3(.5f,3.2f,.16f),"Alloy",.03f,true);
                Box(room,"DoorStatus",new Vector3(-side*5.21f,1.8f,dz),new Vector3(.055f,1.6f,.06f),"Cyan",.008f);
            }
            Text(room,side<0?left[zone+1]:right[zone+1],new Vector3(-side*5.2f,3.37f,0),Quaternion.Euler(0,side<0?-90:90,0),.047f,ice);
            // Solid fore/aft bulkheads and layered wall surfaces.
            foreach(int end in new[]{-1,1}){
                Box(room,"Bulkhead",new Vector3(0,1.8f,end*5.95f),new Vector3(9.8f,3.6f,.18f),"Graphite",.02f,true);
                for(int k=0;k<4;k++)Panel(room,new Vector3(-3.5f+k*2.3f,1.85f,end*5.8f),new Vector2(2.05f,2.75f),Quaternion.Euler(0,end>0?180:0,0),"Alloy");
            }
            for(int k=-1;k<=1;k++){
                Box(room,"CeilingFixture",new Vector3(k*2.8f,3.5f,0),new Vector3(.18f,.08f,4),"WhiteLight",.02f);
                Lamp(room,new Vector3(k*2.8f,3.05f,0),ice,.6f,7);
            }
            // Pipes high on the service wall, clear of player movement.
            for(int k=0;k<3;k++)Cylinder(room,"SupplyPipe",new Vector3(-3.8f,3.05f+k*.17f,-5.5f),new Vector3(3.8f,3.05f+k*.17f,-5.5f),.055f,k==0?"Copper":"Steel");
            if(zone==-1&&side<0){for(int i=0;i<4;i++)Crate(room,new Vector3((i%2)*2.7f-1.4f,0,-2+(i/2)*3),true);}
            if(zone==-1&&side>0)Reactor(room);
            if(zone==0&&side<0){Processor(room);}
            if(zone==0&&side>0){Workbench(room);RepairPanel(new Vector3(7,1.9f,-2.7f),Quaternion.identity,"내부 배선 패널");}
            if(zone==1&&side<0)Medical(room);
            if(zone==1&&side>0)Quarters(room);
        }
        // End walls retain a passage into each deck's stairwell.
        foreach(int end in new[]{-1,1})foreach(int side in new[]{-1,1})Box(corridor,"EndBulkhead",new Vector3(side*7,1.8f,end*18.9f),new Vector3(9.8f,3.6f,.2f),"Graphite",.03f,true);
        Text(corridor,"LOWER HOLD\nSTAIRS DOWN",new Vector3(0,3.25f,-18.7f),Quaternion.Euler(0,180,0),.05f,warm);
        Text(corridor,"COMMAND BRIDGE\nSTAIRS UP",new Vector3(0,3.25f,18.7f),Quaternion.identity,.05f,ice);
    }
    static void Arch(Transform p,Vector3 pos,float width,float height)
    {
        var a=Group(p,"StructuralArch",pos).transform;
        foreach(int side in new[]{-1,1}){
            Box(a,"Upright",new Vector3(side*(width*.5f-.04f),height*.42f,0),new Vector3(.18f,height*.84f,.27f),"Steel",.03f);
            Beam(a,"Haunch",new Vector3(side*(width*.5f-.04f),height*.84f,0),new Vector3(side*(width*.5f-.5f),height,0),.22f,.27f,"Alloy");
        }
        Box(a,"Crown",new Vector3(0,height,0),new Vector3(width-1,.18f,.27f),"Alloy",.025f);
    }
    static void Crate(Transform p,Vector3 pos,bool movable)
    {
        var c=Group(p,"CargoCase",pos).transform;
        Box(c,"Shell",new Vector3(0,.68f,0),new Vector3(1.65f,1.36f,1.4f),"Blue",.09f,true);
        foreach(int side in new[]{-1,1}){
            Box(c,"CornerRail",new Vector3(side*.68f,.7f,0),new Vector3(.16f,1.48f,1.48f),"Steel",.025f);
            Box(c,"Latch",new Vector3(side*.58f,.85f,.745f),new Vector3(.21f,.26f,.09f),"Amber",.025f);
        }
        Panel(c,new Vector3(0,.73f,.72f),new Vector2(.85f,.72f),Quaternion.identity,"Graphite");
        Text(c,"SJ-07\nSERVICE CARGO",new Vector3(0,.73f,.875f),Quaternion.Euler(0,180,0),.023f,ice);
        if(movable){var rb=c.gameObject.AddComponent<Rigidbody>();rb.mass=12;rb.linearDamping=1;rb.angularDamping=.5f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var item=c.gameObject.AddComponent<SJInteractable>();item.kind=SJInteractable.Kind.Cargo;item.displayName="정비용 화물 상자";}
    }
    static void Reactor(Transform p)
    {
        for(int i=0;i<3;i++){
            float x=-2.7f+i*2.7f;Box(p,"ReactorBase",new Vector3(x,.28f,-2.7f),new Vector3(2.1f,.56f,2.2f),"Graphite",.12f,true);
            Cylinder(p,"PressureVessel",new Vector3(x,.55f,-2.7f),new Vector3(x,2.85f,-2.7f),.7f,"Steel",24);
            for(int j=0;j<6;j++){float a=j*Mathf.PI/3;Cylinder(p,"CoolingRod",new Vector3(x+Mathf.Cos(a)*.74f,.8f,-2.7f+Mathf.Sin(a)*.74f),new Vector3(x+Mathf.Cos(a)*.74f,2.5f,-2.7f+Mathf.Sin(a)*.74f),.048f,j%2==0?"Cyan":"Copper");}
            Cylinder(p,"TopCap",new Vector3(x,2.8f,-2.7f),new Vector3(x,3.0f,-2.7f),.9f,"Graphite",24);
            Cylinder(p,"Conduit",new Vector3(x,3.1f,-2.7f),new Vector3(x,3.1f,-5.5f),.14f,"Copper");
        }
        Console(p,new Vector3(0,0,1.8f),"COOLANT / PRESSURE",0);
        Text(p,"02  /  ENGINEERING",new Vector3(0,2.6f,-5.5f),Quaternion.Euler(0,180,0),.06f,ice);
    }
    static void Processor(Transform p)
    {
        Box(p,"WasteProcessor",new Vector3(0,1.25f,-3.7f),new Vector3(4.8f,2.5f,2.6f),"Steel",.14f,true);
        Panel(p,new Vector3(0,1.5f,-2.35f),new Vector2(3.8f,1.55f),Quaternion.identity,"Graphite");
        for(int i=0;i<8;i++)Box(p,"CrusherSlat",new Vector3(-1.5f+i*.42f,1.5f,-2.16f),new Vector3(.17f,1.05f,.14f),"Alloy",.025f);
        Box(p,"WarningStripe",new Vector3(0,2.34f,-2.32f),new Vector3(4.1f,.14f,.04f),"Amber",.01f);
        Text(p,"MATERIAL REPROCESSOR",new Vector3(0,2.65f,-2.3f),Quaternion.Euler(0,180,0),.055f,warm);
        Vent(p,new Vector3(-2.45f,1.3f,-3.6f),new Vector2(1.5f,1.2f),Quaternion.Euler(0,-90,0));
    }
    static void Workbench(Transform p)
    {
        Box(p,"Workbench",new Vector3(0,.55f,-4.1f),new Vector3(5.6f,1.1f,1.5f),"Graphite",.04f,true);
        Box(p,"Worktop",new Vector3(0,1.15f,-4.1f),new Vector3(5.85f,.17f,1.7f),"Alloy",.05f);
        for(int i=0;i<4;i++){
            Panel(p,new Vector3(-2.1f+i*1.4f,.6f,-3.31f),new Vector2(1.2f,.77f),Quaternion.identity,"Steel");
            Box(p,"DrawerHandle",new Vector3(-2.1f+i*1.4f,.75f,-3.14f),new Vector3(.6f,.045f,.07f),"Alloy",.012f);
        }
        for(int i=0;i<5;i++)Cylinder(p,"Tool",new Vector3(-2+i*.6f,1.3f,-4.1f),new Vector3(-2+i*.6f,1.3f,-3.65f),.025f,"Copper",8);
        Vent(p,new Vector3(3.8f,1.6f,-5.63f),new Vector2(1.4f,1.9f),Quaternion.identity);
        Text(p,"EVA ACCESS  >",new Vector3(4.5f,2.7f,0),Quaternion.Euler(0,90,0),.05f,warm);
    }
    static void Medical(Transform p)
    {
        for(int i=0;i<2;i++){
            float z=-2+i*4;Box(p,"BedBase",new Vector3(-1,.38f,z),new Vector3(3.3f,.75f,1.5f),"Steel",.12f,true);
            Box(p,"MedicalCushion",new Vector3(-1,.85f,z),new Vector3(3.1f,.2f,1.35f),"Ivory",.09f);
            Box(p,"Pillow",new Vector3(-2,.99f,z),new Vector3(.7f,.18f,1.16f),"Green",.07f);
            Cylinder(p,"MonitorStand",new Vector3(1,0,z),new Vector3(1,1.8f,z),.06f,"Alloy");
            Display(p,new Vector3(1,1.65f,z),Quaternion.Euler(0,30,0),"VITALS",.85f);
        }
        Text(p,"+  MEDICAL  +",new Vector3(0,2.8f,-5.6f),Quaternion.Euler(0,180,0),.07f,new Color(.45f,.85f,.7f));
    }
    static void Quarters(Transform p)
    {
        for(int side=-1;side<=1;side+=2)for(int bunk=0;bunk<2;bunk++){
            float y=.5f+bunk*1.5f;
            Box(p,"BunkFrame",new Vector3(side*2.85f,y,-2),new Vector3(1.8f,.16f,3),"Steel",.04f,true);
            Box(p,"Mattress",new Vector3(side*2.85f,y+.15f,-2),new Vector3(1.62f,.16f,2.8f),"Blue",.06f);
            Box(p,"Pillow",new Vector3(side*2.85f,y+.29f,-3),new Vector3(1.4f,.12f,.55f),"Ivory",.04f);
            foreach(float z in new[]{-3.5f,-.5f})Box(p,"BunkPost",new Vector3(side*3.7f,1.5f,z),new Vector3(.09f,3,.09f),"Alloy",.02f);
        }
        for(int i=0;i<3;i++){
            Box(p,"Locker",new Vector3(-2+i*2,1.3f,4.7f),new Vector3(1.6f,2.6f,1.05f),"Steel",.06f,true);
            Panel(p,new Vector3(-2+i*2,1.35f,4.14f),new Vector2(1.4f,2.3f),Quaternion.Euler(0,180,0),"Ivory");
            Vent(p,new Vector3(-2+i*2,2.12f,3.95f),new Vector2(1,.3f),Quaternion.Euler(0,180,0));
        }
    }
    static void RepairPanel(Vector3 pos,Quaternion rot,string label)
    {
        var interactive=ship.transform.Find("Interactive");if(!interactive)interactive=Sector("Interactive");
        var p=Group(interactive,"RepairPanel",pos).transform;p.localRotation=rot;
        Box(p,"Casing",Vector3.zero,new Vector3(1.65f,1.2f,.22f),"Amber",.05f,true);
        Box(p,"Inset",new Vector3(0,0,.13f),new Vector3(1.4f,1,.06f),"Rubber",.035f);
        for(int i=0;i<3;i++){
            float x=-.43f+i*.43f;Cylinder(p,"Socket",new Vector3(x,.35f,.16f),new Vector3(x,.35f,.27f),.085f,"Alloy",12);
            Cylinder(p,"Cable",new Vector3(x,-.35f,.26f),new Vector3(x,.3f,.26f),.042f,i==0?"Copper":i==1?"Cyan":"Alloy");
        }
        var item=p.gameObject.AddComponent<SJInteractable>();item.kind=SJInteractable.Kind.Repair;item.displayName=label;
    }
    static void Display(Transform p,Vector3 pos,Quaternion rot,string caption,float scale=1)
    {
        var d=Group(p,"InstrumentDisplay",pos).transform;d.localRotation=rot;d.localScale=Vector3.one*scale;
        Box(d,"Bezel",Vector3.zero,new Vector3(1.65f,1,.15f),"Graphite",.05f);
        Box(d,"DisplayGlass",new Vector3(0,0,.086f),new Vector3(1.45f,.8f,.025f),"Screen",.02f);
        Text(d,caption,new Vector3(0,.26f,.11f),Quaternion.Euler(0,180,0),.025f,ice);
        for(int i=0;i<7;i++)Box(d,"TelemetryBar",new Vector3(-.56f+i*.185f,-.17f,.112f),new Vector3(.07f,.1f+(i%3)*.08f,.007f),i==5?"AmberLight":"Cyan",.002f);
        for(int i=0;i<3;i++)Box(d,"TelemetryLine",new Vector3(0,.1f-i*.09f,.115f),new Vector3(1.05f,.012f,.007f),"WhiteLight",.002f);
    }
    static void Console(Transform p,Vector3 pos,string caption,float angle)
    {
        var c=Group(p,"CommandConsole",pos).transform;c.localRotation=Quaternion.Euler(0,angle,0);
        Box(c,"ConsoleBase",new Vector3(0,.55f,0),new Vector3(2.2f,1.1f,.85f),"Graphite",.08f,true);
        Box(c,"ControlDesk",new Vector3(0,1.12f,.2f),new Vector3(2.4f,.15f,1.2f),"Alloy",.045f);
        Display(c,new Vector3(0,1.85f,-.22f),Quaternion.Euler(-10,0,0),caption,1.1f);
        for(int i=0;i<8;i++)Box(c,"ControlKey",new Vector3(-.87f+i*.24f,1.23f,.43f),new Vector3(.13f,.055f,.13f),i==7?"AmberLight":"Steel",.015f);
        Cylinder(c,"ControlDial",new Vector3(.83f,1.22f,.13f),new Vector3(.83f,1.32f,.13f),.1f,"Graphite",12);
    }
    static void Stairs(Transform p,string name,float z0,float z1,float y0,float y1)
    {
        var s=Group(p,name).transform;int n=28;float dz=(z1-z0)/n,dy=(y1-y0)/n;
        for(int i=0;i<n;i++){
            float y=y0+(i+.5f)*dy,z=z0+(i+.5f)*dz;
            Box(s,"Tread",new Vector3(0,y-.075f,z),new Vector3(3.3f,.15f,Mathf.Abs(dz)+.015f),"Steel",.018f);
            Box(s,"StepEdge",new Vector3(0,y+.007f,z),new Vector3(3.12f,.023f,.035f),"Amber",.005f);
        }
        var ramp=Group(s,"SmoothStairCollision",new Vector3(0,(y0+y1)*.5f-.08f,(z0+z1)*.5f));
        ramp.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(y1-y0,z1-z0)*Mathf.Rad2Deg,0,0);
        // Keep local Z pointing towards the ascending direction even when the path runs aft.
        if(z1<z0)ramp.transform.localRotation=Quaternion.Euler(Mathf.Atan2(y1-y0,z0-z1)*Mathf.Rad2Deg,0,0);
        ramp.AddComponent<BoxCollider>().size=new Vector3(3.3f,.14f,Mathf.Sqrt((z1-z0)*(z1-z0)+(y1-y0)*(y1-y0))+.25f);
        foreach(int side in new[]{-1,1}){
            Beam(s,"Rail",new Vector3(side*1.7f,y0+1,z0),new Vector3(side*1.7f,y1+1,z1),.065f,.065f,"Alloy");
            for(int i=0;i<5;i++){float t=i/4f;Box(s,"RailPost",new Vector3(side*1.7f,Mathf.Lerp(y0,y1,t)+.5f,Mathf.Lerp(z0,z1,t)),new Vector3(.06f,1,.06f),"Steel",.01f);}
        }
    }
    static void LowerDeck()
    {
        var p=Sector("Lower_Cargo_Deck");FloorTiles(p,-9.3f,9.3f,-31,14,-4.5f);
        Stairs(p,"AftAccessStairs",-19,-29,0,-4.5f);
        // Main deck's plate is also the cargo bay ceiling; aft staircase remains open.
        Box(p,"CargoCeiling",new Vector3(0,-.35f,-2),new Vector3(19,.3f,34),"Graphite",.025f,true);
        foreach(int side in new[]{-1,1}){
            Box(p,"LowerSideWall",new Vector3(side*9.5f,-2.35f,-2),new Vector3(.24f,4.3f,34),"Steel",.04f,true);
            for(int z=-14;z<=10;z+=6){
                Panel(p,new Vector3(side*9.33f,-2.35f,z),new Vector2(5.3f,3.3f),Quaternion.Euler(0,-side*90,0),"Alloy");
                Box(p,"LowerLight",new Vector3(side*8.9f,-.61f,z),new Vector3(.15f,.09f,3.7f),"WhiteLight",.01f);
                for(int row=0;row<2;row++)Crate(p,new Vector3(side*(5.3f+row*2.2f),-4.5f,z),false);
            }
        }
        for(int z=-26;z<=12;z+=6){Lamp(p,new Vector3(0,-.9f,z),new Color(.67f,.78f,1),.85f,10);Arch(p,new Vector3(0,-4.5f,z),18.4f,3.8f);}
        Box(p,"LowerForwardWall",new Vector3(0,-2.35f,14),new Vector3(19,4.3f,.24f),"Graphite",.03f,true);
        Text(p,"LOWER DECK / 01\nFREIGHT & SALVAGE",new Vector3(0,-1.7f,13.8f),Quaternion.identity,.11f,warm);
        for(int z=-16;z<=12;z+=4)foreach(int side in new[]{-1,1})Box(p,"CargoLane",new Vector3(side*2,-4.48f,z),new Vector3(.1f,.018f,3.2f),"Amber",.006f);
    }
    static void Bridge()
    {
        var p=Sector("Upper_Command_Deck");
        Stairs(p,"BridgeAccessStairs",19,29,0,4.5f);
        Loft(p,"SlopedBridgeFairing",new[]{19f,29f},new[]{2.35f,2.35f},new[]{-.3f,4.2f},new[]{3.4f,7.9f},"Steel");
        FloorTiles(p,-6.3f,6.3f,29,39,4.5f);
        foreach(int side in new[]{-1,1}){
            for(int i=0;i<5;i++){
                float z=20+i*2,y=(z-19)*.45f;
                Panel(p,new Vector3(side*1.88f,y+1.6f,z),new Vector2(1.8f,2.65f),Quaternion.Euler(0,-side*90,0),"Alloy");
                Box(p,"StairGuide",new Vector3(side*1.78f,y+.5f,z),new Vector3(.06f,.06f,1.4f),"Cyan",.01f);
                Box(p,"StairRoofTile",new Vector3(0,y+3.08f,z),new Vector3(4.2f,.14f,2.1f),"Graphite",.025f,true);
            }
            // Raised bridge glazing, protected by chunky angular mullions.
            Box(p,"BridgeSill",new Vector3(side*6.45f,5.15f,34),new Vector3(.32f,1.3f,10.3f),"Alloy",.1f,true);
            Box(p,"BridgeGlass",new Vector3(side*6.45f,6.65f,34),new Vector3(.1f,1.75f,10.1f),"Glass",.015f,true);
            for(int z=29;z<=39;z+=2)Box(p,"Mullion",new Vector3(side*6.48f,6.7f,z),new Vector3(.26f,1.95f,.15f),"Steel",.025f);
            Console(p,new Vector3(side*3.6f,4.5f,35.6f),"NAV / 07",180+side*24);
        }
        Box(p,"BridgeForeSill",new Vector3(0,5.15f,39.1f),new Vector3(13,1.3f,.3f),"Alloy",.08f,true);
        Box(p,"ForwardGlass",new Vector3(0,6.65f,39.12f),new Vector3(12.8f,1.75f,.1f),"Glass",.02f,true);
        for(int x=-6;x<=6;x+=2)Box(p,"ForwardMullion",new Vector3(x,6.7f,39.12f),new Vector3(.16f,1.95f,.3f),"Steel",.025f);
        Box(p,"BridgeRoof",new Vector3(0,7.7f,34),new Vector3(13.8f,.42f,10.8f),"Ivory",.16f,true);
        Box(p,"RoofBrow",new Vector3(0,7.46f,39.5f),new Vector3(13.4f,.13f,.4f),"Amber",.04f);
        for(int i=-2;i<=2;i++)Panel(p,new Vector3(i*2.5f,7.95f,34),new Vector2(2.2f,8.6f),Quaternion.Euler(-90,0,0),"Alloy");
        // Bulkhead separates the bridge from outside, but leaves the stair portal open.
        foreach(int side in new[]{-1,1})Box(p,"AftBridgeWall",new Vector3(side*4.4f,6.1f,28.9f),new Vector3(4.8f,3.2f,.25f),"Steel",.06f,true);
        Console(p,new Vector3(0,4.5f,37.2f),"FLIGHT / SYSTEMS",180);
        foreach(int side in new[]{-1,1}){
            Box(p,"CommandChairBase",new Vector3(side*3.6f,4.9f,33.8f),new Vector3(.85f,.8f,.85f),"Steel",.1f,true);
            Box(p,"CommandSeat",new Vector3(side*3.6f,5.4f,33.8f),new Vector3(.95f,.2f,.95f),"Rubber",.12f);
            Box(p,"SeatBack",new Vector3(side*3.6f,5.95f,33.35f),new Vector3(.95f,1.25f,.2f),"Rubber",.13f);
            Box(p,"CeilingStrip",new Vector3(side*4.8f,7.45f,34),new Vector3(.13f,.05f,7.5f),"WhiteLight",.01f);
        }
        Lamp(p,new Vector3(0,7.2f,34),ice,1.05f,12);
        for(int i=0;i<3;i++)Lamp(p,new Vector3(0,2+i*1.3f,20+i*3),ice,.6f,6);
        Text(p,"KESTREL 07  /  COMMAND",new Vector3(0,7.15f,39),Quaternion.identity,.08f,ice);
    }
    static void ConfigurePlayer()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<SJPlayer>();
        p.useInteriorVolumes=true;
        p.interiorVolumes=new[]{new Bounds(new Vector3(0,1.9f,0),new Vector3(24.6f,4.6f,38.5f)),
            new Bounds(new Vector3(0,-2.2f,-8.5f),new Vector3(19.2f,4.9f,46)),
            new Bounds(new Vector3(0,-.1f,-24),new Vector3(4.2f,8.5f,11)),
            new Bounds(new Vector3(0,3.2f,24),new Vector3(4.2f,8.5f,11)),
            new Bounds(new Vector3(0,6.1f,34),new Vector3(13.2f,3.9f,10.5f))};
        p.view.fieldOfView=74;p.view.GetComponentInChildren<Light>().intensity=.42f;
        p.leftHand.localScale=p.rightHand.localScale=Vector3.one*.8f;
    }
    static void UpgradeLooseProps()
    {
        var stars=GameObject.Find("Starfield").GetComponent<MeshFilter>();var starMesh=UnityEngine.Object.Instantiate(stars.sharedMesh);var vertices=starMesh.vertices;
        for(int i=0;i<vertices.Length;i+=3){Vector3 c=(vertices[i]+vertices[i+1]+vertices[i+2])/3;for(int j=0;j<3;j++)vertices[i+j]=c+(vertices[i+j]-c)*.22f;}
        starMesh.vertices=vertices;starMesh.RecalculateBounds();stars.sharedMesh=Save(starMesh,"Meshes/DistantStars.asset");
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()){
            if(root.name=="RecyclingBin")foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial=materials["Amber"];
            if(root.name.StartsWith("Waste_"))root.GetComponent<Renderer>().sharedMaterial=materials[root.name.EndsWith("0")||root.name.EndsWith("2")?"Blue":"Amber"];
        }
        Text(ship.transform,"RECYCLE / DROP HERE",new Vector3(-5,1.9f,1.6f),Quaternion.Euler(0,180,0),.055f,warm);
        Text(ship.transform,"TETHER [F]",new Vector3(10.99f,2.02f,2.25f),Quaternion.Euler(0,90,0),.055f,warm);
        var bin=GameObject.Find("RecyclingBin");foreach(int side in new[]{-1,1})Cylinder(bin.transform,"Handle",new Vector3(side*.98f,.7f,-.5f),new Vector3(side*.98f,.7f,.5f),.05f,"Steel");
    }
    static void Lighting()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.28f,.35f,.43f);RenderSettings.ambientEquatorColor=new Color(.19f,.23f,.28f);RenderSettings.ambientGroundColor=new Color(.11f,.13f,.16f);
        var sun=GameObject.Find("DistantSun").GetComponent<Light>();sun.intensity=.8f;sun.color=new Color(.88f,.93f,1);sun.transform.rotation=Quaternion.Euler(37,-35,0);sun.shadowBias=.06f;sun.shadowNormalBias=.2f;
        QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadowDistance=120;QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=6;
    }
    static void SavePreviews()
    {
        var player=UnityEngine.Object.FindFirstObjectByType<SJPlayer>();player.gameObject.SetActive(false);
        var cam=Group(null,"ArtCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.024f,.038f,.06f);cam.farClipPlane=800;cam.nearClipPlane=.08f;
        Capture(cam,new Vector3(67,45,73),new Vector3(0,0,1),"01_Kestrel_Exterior",true,34);
        Capture(cam,new Vector3(-53,27,-62),new Vector3(0,0,-10),"02_Kestrel_Engines",true,40);
        Capture(cam,new Vector3(0,1.68f,10),new Vector3(0,1.65f,-12),"03_Main_Concourse",false,0);
        Capture(cam,new Vector3(2.2f,1.68f,-10),new Vector3(7,1.5f,-15),"04_Engineering",false,0);
        Capture(cam,new Vector3(0,-2.82f,-18),new Vector3(0,-2.8f,10),"05_Lower_Cargo",false,0);
        Capture(cam,new Vector3(-1,6.18f,30),new Vector3(0,6.1f,39),"06_Bridge",false,0);
        UnityEngine.Object.DestroyImmediate(cam.gameObject);player.gameObject.SetActive(true);
    }
    static void Capture(Camera cam,Vector3 eye,Vector3 target,string filename,bool ortho,float size)
    {
        cam.transform.position=eye;cam.transform.LookAt(target);cam.orthographic=ortho;cam.orthographicSize=size>0?size:5;cam.fieldOfView=70;
        var rt=new RenderTexture(1600,1000,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
        File.WriteAllBytes(Out+"/"+filename+".png",image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    }
    static void Validate()
    {
        long triangles=0;int renderers=0;
        foreach(var r in ship.GetComponentsInChildren<MeshRenderer>()){
            if(!r.sharedMaterial||!r.sharedMaterial.shader.isSupported)throw new Exception("Invalid material on "+r.name);
            var mf=r.GetComponent<MeshFilter>();if(mf){if(!mf.sharedMesh)throw new Exception("Missing mesh");triangles+=mf.sharedMesh.triangles.Length/3;renderers++;}
        }
        Physics.SyncTransforms();
        if(Physics.Raycast(new Vector3(9,1,0),Vector3.right,out var hit,8,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Airlock blocked: "+hit.collider.name);
        File.WriteAllLines(Out+"/BuildValidation.txt",new[]{"Original Kestrel-07 geometry; no third-party model files.","PASS: meshes and shaders present.","PASS: airlock centre-line clear.","Rendered ship triangles: "+triangles,"Ship mesh renderers after sector batching: "+renderers,"3 connected deck elevations: -4.5 / 0 / 4.5 metres.","Requires standalone playtest for stair traversal and interactions."});
    }
}
