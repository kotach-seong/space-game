using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static MapRevisionGeometry;

public static class BuildMapRevision {
    const string Scene="Assets/Frigate/Scenes/Kestrel_FirstPerson.unity";
    static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/MapRevision"));
    static Transform ship;
    static Transform Sector(string name)=>Group(ship,name);
    public static void Build(){
        Directory.CreateDirectory(Output);Init();EditorSceneManager.OpenScene(Scene);
        if(!File.Exists(Root+"/Scenes/Kestrel_BeforeMapRevision.unity"))AssetDatabase.CopyAsset(Scene,Root+"/Scenes/Kestrel_BeforeMapRevision.unity");
        var legacy=GameObject.Find("KESTREL_07_MaintenanceFrigate");var previous=GameObject.Find("KESTREL_07_MapRevision");
        var kept=GameObject.Find("Map_Interactions");if(!kept)kept=new GameObject("Map_Interactions");
        if(legacy){
            // Pull out existing mission components; replacing the map must not reset or duplicate them.
            foreach(var item in legacy.GetComponentsInChildren<SJInteractable>(true))item.transform.SetParent(kept.transform,true);
            Object.DestroyImmediate(legacy);
        }
        if(previous)Object.DestroyImmediate(previous);
        foreach(string name in new[]{"CrewScaleReference","Starfield","DistantPlanet","SmallMoon","Map_SpaceEnvironment"}){var o=GameObject.Find(name);if(o)Object.DestroyImmediate(o);}
        ship=Group(null,"KESTREL_07_MapRevision");
        MainDeck();LowerDeck();Bridge();Exterior();
        foreach(Transform sector in ship)Combine(sector);
        var env=Group(null,"Map_SpaceEnvironment");BuildSpaceEnvironment.Build(env);
        var player=Object.FindFirstObjectByType<SJPlayer>();player.useInteriorVolumes=true;
        player.interiorVolumes=new[]{new Bounds(new Vector3(0,2.05f,0),new Vector3(24.1f,5.1f,90.2f)),new Bounds(new Vector3(0,-2.65f,-16),new Vector3(20.2f,5.2f,92.2f)),new Bounds(new Vector3(0,.5f,-51),new Vector3(5.2f,10.3f,12.5f)),new Bounds(new Vector3(0,4.6f,51),new Vector3(5.2f,10.3f,12.5f)),new Bounds(new Vector3(0,7.25f,63.5f),new Vector3(24.3f,5.3f,13.3f)),new Bounds(new Vector3(13.45f,2,0),new Vector3(2.7f,4.6f,6))};
        player.view.farClipPlane=2500;player.view.backgroundColor=new Color(.004f,.008f,.018f);player.view.clearFlags=CameraClearFlags.SolidColor;
        foreach(var item in kept.GetComponentsInChildren<SJInteractable>())if(item.kind==SJInteractable.Kind.Repair && item.transform.position.x>12)item.transform.position=new Vector3(12.65f,1.8f,8);
        // Original garbage bin, mission computer, tether, five wastes and hand models stay untouched.
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.26f,.32f,.39f);RenderSettings.ambientEquatorColor=new Color(.16f,.20f,.25f);RenderSettings.ambientGroundColor=new Color(.08f,.11f,.14f);
        var sun=GameObject.Find("DistantSun");if(sun){var light=sun.GetComponent<Light>();light.intensity=.85f;light.color=new Color(1,.93f,.8f);sun.transform.rotation=Quaternion.LookRotation(-new Vector3(-800,300,500));light.shadows=LightShadows.Soft;}
        Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);
        PrefabUtility.SaveAsPrefabAsset(ship.gameObject,Root+"/Prefabs/Kestrel07_ExpandedMap.prefab");
        Validate();BuildPlayer();Debug.Log("MAP_REVISION_BUILD_SUCCESS");
    }
    static void MainDeck(){
        var c=Sector("CentralConcourse");Deck(c,-3,3,-45,45,0);Box(c,"ContinuousRoof",new Vector3(0,4.5f,0),new Vector3(6.2f,.32f,90.4f),"Graphite");
        for(int z=-42;z<=42;z+=6){foreach(int side in new[]{-1,1}){Box(c,"FloorGuidance",new Vector3(side*2.8f,.05f,z),new Vector3(.08f,.02f,5.1f),"Cyan",false);Box(c,"CeilingStrip",new Vector3(side*2.15f,4.31f,z),new Vector3(.18f,.06f,4.6f),"WhiteLight",false);Box(c,"CableTray",new Vector3(side*.95f,4.28f,z),new Vector3(.2f,.16f,5.98f),"Steel",false);}Lamp(c,new Vector3(0,3.8f,z),11,.85f);}
        foreach(int side in new[]{-1,1})for(int z=-42;z<=42;z+=6){if(z==-30||z==0||z==30)continue;
            Panel(c,new Vector3(side*2.965f,2.2f,z),new Vector2(5.2f,2.65f),Quaternion.Euler(0,side<0?90:-90,0),z%12==0?"Ivory":"Alloy");
            Box(c,"WallKickplate",new Vector3(side*2.92f,.45f,z),new Vector3(.07f,.45f,5.6f),"Graphite",false);
            for(int slat=0;slat<6;slat++)Box(c,"VentSlat",new Vector3(side*2.865f,.46f,z-.9f+slat*.35f),new Vector3(.045f,.24f,.065f),"Steel",false);
        }
        for(int side=-1;side<=1;side+=2)for(int zone=-1;zone<=1;zone++)Room(side,zone*30);
        // Broad, unobstructed corridor portals: no mid-door ribs or freestanding arches.
        Label(c,"LOWER HOLD / STAIRS DOWN",new Vector3(0,3.75f,-44.6f),Quaternion.Euler(0,180,0),.09f);
        Label(c,"COMMAND / OBSERVATION",new Vector3(0,3.75f,44.6f),Quaternion.identity,.09f);
        foreach(int z in new[]{-45,45})foreach(int side in new[]{-1,1})Box(c,"EndBulkhead",new Vector3(side*7.475f,2.25f,z),new Vector3(9.55f,4.5f,.3f),"Steel");
    }
    static void Room(int side,int z){
        string[] port={"01 / CARGO HANDLING","03 / RECYCLING","05 / MEDICAL & LAB"},starboard={"02 / ENGINEERING","04 / WORKSHOP","06 / CREW QUARTERS"};int index=z/30+1;
        var p=Sector((side<0?"Port":"Starboard")+"Room_"+index);float x0=side<0?-12:3,x1=side<0?-3:12;float cx=side*7.5f;
        Deck(p,x0,x1,z-15,z+15,0);Box(p,"SolidCeiling",new Vector3(cx,4.5f,z),new Vector3(9.2f,.32f,30.2f),"Graphite");
        // All exterior walls have volume on BOTH sides and real solid colliders.
        Box(p,"SolidOuterWall",new Vector3(side*12.15f,2.25f,z),new Vector3(.3f,4.5f,30.1f),"Steel");
        foreach(int end in new[]{-1,1})Box(p,"RoomBulkhead",new Vector3(cx,2.25f,z+end*15),new Vector3(9.25f,4.5f,.28f),"Steel");
        foreach(int end in new[]{-1,1})Box(p,"CorridorWall",new Vector3(side*3.1f,2.25f,z+end*8.8f),new Vector3(.2f,4.5f,12.4f),"Graphite");
        Box(p,"FlushDoorHeader",new Vector3(side*3.1f,4.03f,z),new Vector3(.3f,.9f,5.2f),"Alloy");
        foreach(int end in new[]{-1,1}){Box(p,"FlushDoorJamb",new Vector3(side*3.1f,1.82f,z+end*2.62f),new Vector3(.3f,3.64f,.14f),"Alloy");Box(p,"DoorLight",new Vector3(side*2.91f,1.8f,z+end*2.62f),new Vector3(.025f,2.3f,.04f),"Cyan",false);}
        Label(p,side<0?port[index]:starboard[index],new Vector3(side*2.9f,3.97f,z),Quaternion.Euler(0,side<0?-90:90,0),.07f);
        for(int j=-2;j<=2;j++){
            float zz=z+j*5.6f;Panel(p,new Vector3(side*11.92f,2.15f,zz),new Vector2(5.25f,3.25f),Quaternion.Euler(0,side<0?90:-90,0),j%2==0?"Alloy":"Ivory");
            foreach(float xx in new[]{side*5.4f,side*9.3f})Box(p,"CeilingLuminaire",new Vector3(xx,4.28f,zz),new Vector3(.2f,.08f,3.6f),"WhiteLight",false);
            Lamp(p,new Vector3(cx,3.72f,zz),10,.72f);
            Box(p,"Skirting",new Vector3(side*11.9f,.2f,zz),new Vector3(.12f,.25f,5.5f),"Graphite",false);
        }
        for(int j=0;j<3;j++)Beam(p,"OverheadServices",new Vector3(side*(10.7f+j*.3f),3.7f,z-14.5f),new Vector3(side*(10.7f+j*.3f),3.7f,z+14.5f),.11f,j==0?"Copper":"Steel");
        if(side<0 && z==-30){for(int j=0;j<4;j++)Cargo(p,new Vector3(-8.5f,0,z-10+j*6),j);}
        if(side>0 && z==-30){for(int j=0;j<3;j++)Machinery(p,new Vector3(8.6f,0,z-8+j*8),"COOLANT");}
        if(side<0 && z==0){Machinery(p,new Vector3(-9,0,-9),"REPROCESSOR");Cargo(p,new Vector3(-9,0,9),3);}
        if(side>0 && z==0){Workbench(p,new Vector3(8,0,-11));Workbench(p,new Vector3(8,0,11));}
        if(side<0 && z==30){for(int j=0;j<3;j++){Box(p,"MedicalBed",new Vector3(-8.2f,.65f,z-9+j*8),new Vector3(2.6f,1.3f,1.3f),"Alloy");Box(p,"BedCushion",new Vector3(-8.2f,1.38f,z-9+j*8),new Vector3(2.5f,.2f,1.15f),"Blue",false);}}
        if(side>0 && z==30){for(int j=0;j<4;j++){float zz=z-10+j*6;Box(p,"BunkFrame",new Vector3(9.5f,1.2f,zz),new Vector3(3.1f,.12f,1.45f),"Steel");Box(p,"BunkUpper",new Vector3(9.5f,2.7f,zz),new Vector3(3.1f,.12f,1.45f),"Steel");foreach(float xx in new[]{8,11})Box(p,"BunkPost",new Vector3(xx,1.5f,zz-.7f),new Vector3(.12f,3,.12f),"Alloy");}}
    }
    static void Cargo(Transform p,Vector3 pos,int id){var c=Group(p,"CargoFixture",pos);Box(c,"Case",new Vector3(0,.9f,0),new Vector3(2.6f,1.8f,2.2f),"Blue");foreach(int side in new[]{-1,1})Box(c,"Strap",new Vector3(side*.95f,.93f,0),new Vector3(.13f,1.9f,2.29f),"Steel",false);Panel(c,new Vector3(0,1,1.13f),new Vector2(1.5f,.8f),Quaternion.identity,"Graphite");Label(c,"SJ / "+id,new Vector3(0,1,1.23f),Quaternion.Euler(0,180,0),.07f);}
    static void Machinery(Transform p,Vector3 pos,string label){var c=Group(p,label,pos);Box(c,"EquipmentHousing",new Vector3(0,1.35f,0),new Vector3(3.5f,2.7f,2.5f),"Steel");for(int i=0;i<7;i++)Box(c,"VentLouvre",new Vector3(-1.3f+i*.43f,1.5f,-1.3f),new Vector3(.12f,1.65f,.17f),"Alloy",false);Box(c,"EquipmentAccent",new Vector3(0,2.48f,-1.32f),new Vector3(3.2f,.12f,.06f),"Amber",false);Label(c,label,new Vector3(0,2.95f,-1.31f),Quaternion.identity,.1f);}
    static void Workbench(Transform p,Vector3 pos){var c=Group(p,"ServiceWorkbench",pos);Box(c,"Base",new Vector3(0,.65f,0),new Vector3(5,1.3f,1.5f),"Graphite");Box(c,"Top",new Vector3(0,1.37f,0),new Vector3(5.3f,.16f,1.8f),"Alloy");for(int i=0;i<4;i++)Panel(c,new Vector3(-1.9f+i*1.25f,.7f,-.81f),new Vector2(1.05f,.9f),Quaternion.Euler(0,180,0),"Steel");}
    static void Stair(Transform p,string name,float z0,float z1,float y0,float y1){
        var s=Group(p,name);float slope=(y1-y0)/(z1-z0),angle=-Mathf.Atan(slope)*Mathf.Rad2Deg,length=Mathf.Sqrt((z1-z0)*(z1-z0)+(y1-y0)*(y1-y0));float midz=(z0+z1)*.5f,midy=(y0+y1)*.5f;
        var floor=Box(s,"SealedRampSlab",new Vector3(0,midy-.21f,midz),new Vector3(5.5f,.42f,length+.5f),"Graphite");floor.transform.localRotation=Quaternion.Euler(angle,0,0);
        var roof=Box(s,"SealedSlopedRoof",new Vector3(0,midy+4.38f,midz),new Vector3(5.65f,.35f,length+.7f),"Steel");roof.transform.localRotation=Quaternion.Euler(angle,0,0);
        foreach(int side in new[]{-1,1}){
            // A generous full-height wall slab eliminates both above- and below-stair voids.
            Box(s,"SolidStairSide",new Vector3(side*2.75f,midy+2,midz),new Vector3(.35f,Mathf.Abs(y1-y0)+5,Mathf.Abs(z1-z0)+.4f),"Steel");
            Beam(s,"Handrail",new Vector3(side*2.47f,y0+1,z0),new Vector3(side*2.47f,y1+1,z1),.06f,"Alloy");
            Beam(s,"GuideLight",new Vector3(side*2.5f,y0+.4f,z0),new Vector3(side*2.5f,y1+.4f,z1),.055f,"Cyan");
            for(int i=0;i<4;i++){float t=(i+.5f)/4;Panel(s,new Vector3(side*2.55f,Mathf.Lerp(y0,y1,t)+2.25f,Mathf.Lerp(z0,z1,t)),new Vector2(2.6f,2.7f),Quaternion.Euler(0,side<0?90:-90,0),"Alloy");}
        }
        int steps=32;for(int i=0;i<steps;i++){float t=(i+.5f)/steps,z=Mathf.Lerp(z0,z1,t),y=Mathf.Lerp(y0,y1,t);Box(s,"StairTread",new Vector3(0,y-.045f,z),new Vector3(5.1f,.10f,Mathf.Abs(z1-z0)/steps+.012f),"Steel",false,.01f);Box(s,"Nosing",new Vector3(0,y+.012f,z),new Vector3(4.95f,.025f,.035f),"Amber",false,.004f);}
        for(int i=0;i<4;i++){float t=(i+.5f)/4;Lamp(s,new Vector3(0,Mathf.Lerp(y0,y1,t)+3.4f,Mathf.Lerp(z0,z1,t)),8,.9f);var light=Box(s,"StairRoofLight",new Vector3(0,Mathf.Lerp(y0,y1,t)+4.14f,Mathf.Lerp(z0,z1,t)),new Vector3(3.4f,.06f,.14f),"WhiteLight",false);light.transform.localRotation=Quaternion.Euler(angle,0,0);}
    }
    static void LowerDeck(){
        var p=Sector("LowerCargoDeck");Deck(p,-10,10,-62,30,-5);
        foreach(int side in new[]{-1,1}){Box(p,"SolidLowerSide",new Vector3(side*10.15f,-2.6f,-16),new Vector3(.3f,4.8f,92.3f),"Steel");
            for(int z=-54;z<=24;z+=6){Panel(p,new Vector3(side*9.91f,-2.65f,z),new Vector2(5.65f,3.65f),Quaternion.Euler(0,side<0?90:-90,0),"Alloy");if(z>-40)Cargo(p,new Vector3(side*7.5f,-5,z),z+55);}}
        foreach(float z in new[]{-62.15f,30.15f})Box(p,"SolidEndWall",new Vector3(0,-2.6f,z),new Vector3(20.6f,4.8f,.3f),"Steel");
        Box(p,"CeilingMain",new Vector3(0,-.42f,-7),new Vector3(20.6f,.38f,74),"Graphite");
        Box(p,"CeilingAft",new Vector3(0,-.42f,-59.7f),new Vector3(20.6f,.38f,5.3f),"Graphite");
        foreach(int side in new[]{-1,1})Box(p,"CeilingStairSide",new Vector3(side*6.45f,-.42f,-51),new Vector3(7.5f,.38f,14.2f),"Graphite");
        Stair(p,"AftStairwell",-45,-57,0,-5);
        // Close narrow gaps between the stair roof and the main/lower deck slabs.
        for(int z=-57;z<=27;z+=6){Lamp(p,new Vector3(0,-1.15f,z),11,.95f);foreach(int side in new[]{-1,1}){Box(p,"CeilingStrip",new Vector3(side*5,-.66f,z),new Vector3(.17f,.07f,4.3f),"WhiteLight",false);Box(p,"ClearCargoLane",new Vector3(side*2.25f,-4.96f,z),new Vector3(.08f,.02f,4.8f),"Amber",false);}}
        Label(p,"LOWER DECK / SALVAGE OPERATIONS",new Vector3(0,-1.6f,29.93f),Quaternion.identity,.13f);
    }
    static void Bridge(){
        var p=Sector("UpperBridge");Stair(p,"BridgeStairwell",45,57,0,5);Deck(p,-12,12,57,70,5);
        Box(p,"BridgeRoof",new Vector3(0,9.55f,63.5f),new Vector3(24.8f,.45f,13.8f),"Steel");
        foreach(int side in new[]{-1,1}){
            Box(p,"BridgeSideBase",new Vector3(side*12.15f,5.8f,63.5f),new Vector3(.3f,1.6f,13.4f),"Alloy");Box(p,"BridgeSideGlazing",new Vector3(side*12.15f,7.9f,63.5f),new Vector3(.14f,3f,13.4f),"Glass");
            Box(p,"RearBridgeWall",new Vector3(side*7.45f,7.25f,56.92f),new Vector3(9.5f,4.5f,.3f),"Steel");
            for(int z=57;z<=70;z+=3)Box(p,"WindowMullion",new Vector3(side*12.15f,7.85f,z),new Vector3(.3f,2.85f,.14f),"Steel");
            Box(p,"CeilingLamp",new Vector3(side*6,9.27f,63.5f),new Vector3(.2f,.08f,10.5f),"WhiteLight",false);
            Workbench(p,new Vector3(side*6.7f,5,65));Panel(p,new Vector3(side*6.7f,7.15f,65.3f),new Vector2(3.7f,1.1f),Quaternion.Euler(0,180,0),"Screen");
            Label(p,"NAV / SYSTEMS",new Vector3(side*6.7f,7.17f,65.17f),Quaternion.identity,.09f);
        }
        Box(p,"FrontSill",new Vector3(0,5.8f,70.15f),new Vector3(24.6f,1.6f,.3f),"Alloy");Box(p,"ObservationWindow",new Vector3(0,7.95f,70.15f),new Vector3(24.4f,2.85f,.14f),"Glass");
        for(int x=-12;x<=12;x+=3)Box(p,"WindowPost",new Vector3(x,7.85f,70.15f),new Vector3(.16f,2.85f,.3f),"Steel");
        for(int z=60;z<=68;z+=4)for(int side=-1;side<=1;side++)Lamp(p,new Vector3(side*7,8.7f,z),11,.75f);
        Label(p,"KESTREL 07 / COMMAND",new Vector3(0,9.05f,70),Quaternion.identity,.15f);
    }
    static void Exterior(){
        var p=Sector("ExteriorArmor");
        // Closed interior slabs form the pressure hull; exterior plates only decorate it.
        foreach(int side in new[]{-1,1}){
            for(int z=-42;z<=42;z+=6){Panel(p,new Vector3(side*12.36f,2.2f,z),new Vector2(5.75f,3.5f),Quaternion.Euler(0,side*90,0),"Ivory");Box(p,"ServiceStripe",new Vector3(side*12.53f,.2f,z),new Vector3(.04f,.15f,5.4f),"Amber",false);}
            for(int z=-40;z<=40;z+=8)Panel(p,new Vector3(side*6,4.74f,z),new Vector2(10.5f,7.5f),Quaternion.Euler(-90,0,0),"Alloy");
            var e=Group(p,"EnginePod",new Vector3(side*7,-1,-61));Box(e,"Cowl",Vector3.zero,new Vector3(5.2f,5.4f,10),"Graphite");Box(e,"Armor",new Vector3(0,2.73f,0),new Vector3(5.3f,.25f,9.6f),"Alloy");
            Cylinder(e,"DeepNozzle",new Vector3(0,0,-5.02f),new Vector3(0,0,-5.22f),2.38f,"Rubber");Ring(e,"NozzleCollar",new Vector3(0,0,-5.4f),2.62f,2.29f,.65f,"Alloy");Ring(e,"HeatShield",new Vector3(0,0,-5.64f),2.25f,1.96f,.19f,"Copper");Ring(e,"IonRing",new Vector3(0,0,-5.32f),1.91f,1.78f,.06f,"Cyan");
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Beam(e,"TurbineVane",new Vector3(Mathf.Cos(a)*.4f,Mathf.Sin(a)*.4f,-5.3f),new Vector3(Mathf.Cos(a+.15f)*1.7f,Mathf.Sin(a+.15f)*1.7f,-5.3f),.1f,"Steel");}
            for(int z=-3;z<=3;z+=2)Box(e,"EngineTopRib",new Vector3(0,2.95f,z),new Vector3(5.4f,.22f,.24f),"Steel",false);
            Box(p,"LateralEquipmentPod",new Vector3(side*13.2f,2,-26),new Vector3(2.4f,3.5f,12),"Graphite",false,.18f);
            for(int z=-30;z<=-22;z+=2)Box(p,"RadiatorFin",new Vector3(side*14.45f,2,z),new Vector3(.25f,3.1f,.14f),"Alloy",false);
            Box(p,"ExternalWalkway",new Vector3(side*13.9f,-.14f,9),new Vector3(2.9f,.28f,18),"Floor");Beam(p,"SafetyRail",new Vector3(side*15.4f,1.1f,1),new Vector3(side*15.4f,1.1f,18),.065f,"Amber");
            for(int z=2;z<=18;z+=4)Beam(p,"RailPost",new Vector3(side*15.4f,0,z),new Vector3(side*15.4f,1.1f,z),.07f,"Steel");
        }
        // Sloped shoulders make the stepped pressure decks read as one continuous ship.
        Fairing(p,"ForwardRaisedFairing",45,56.8f,12.32f,12.32f,-.35f,4.65f,4.7f,9.68f);
        Fairing(p,"AftTaperedFairing",-62.5f,-45.05f,10.35f,12.32f,-5.45f,-5.45f,-.2f,4.68f);
        Fairing(p,"ArmoredBow",70.25f,76,12.35f,3.4f,4.6f,4.9f,6.5f,5.7f);
        Box(p,"BowEndCap",new Vector3(0,5.3f,76.02f),new Vector3(6.85f,.84f,.16f),"Alloy");
        // User requested sealing the accidental side opening; retain an exterior EVA test point (F3).
        Box(p,"SealedEVABulkhead",new Vector3(14.65f,2.2f,0),new Vector3(.3f,4.4f,6),"Steel");
        foreach(int side in new[]{-1,1})Box(p,"AirlockSide",new Vector3(13.4f,2.2f,side*3),new Vector3(2.7f,4.4f,.3f),"Graphite");
        Box(p,"AirlockRoof",new Vector3(13.4f,4.5f,0),new Vector3(2.7f,.3f,6.3f),"Steel");Deck(p,12,14.8f,-3,3,0);
        Label(p,"EVA / SEALED BULKHEAD",new Vector3(14.85f,3,0),Quaternion.Euler(0,90,0),.08f);
        Box(p,"DorsalSpine",new Vector3(0,4.92f,0),new Vector3(2.4f,.6f,70),"Steel");
        for(int z=-30;z<=30;z+=10)Box(p,"SpineServiceCap",new Vector3(0,5.27f,z),new Vector3(2,.12f,6),"Alloy",false);
        for(int i=0;i<2;i++){float x=i==0?-2:2;Beam(p,"AntennaMast",new Vector3(x,4.8f,30),new Vector3(x,9.5f+i,30),.09f,"Steel");Box(p,"SignalBeacon",new Vector3(x,9.6f+i,30),Vector3.one*.18f,"AmberLight",false);}
    }
    public static void Validate(){
        var log=new List<string>();Physics.SyncTransforms();
        foreach(int side in new[]{-1,1})foreach(int z in new[]{-30,0,30}){var point=new Vector3(side*8,1.6f,z);foreach(var d in new[]{Vector3.right*side,Vector3.up,Vector3.down}){if(!Physics.Raycast(point,d,out var hit,7,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Open room shell: "+point+" "+d);log.Add("PASS: Room shell "+point+" direction "+d+" -> "+hit.collider.name);}}
        if(GameObject.Find("CrewScaleReference"))throw new Exception("Mannequin still present");
        var items=Object.FindObjectsByType<SJInteractable>(FindObjectsSortMode.None);if(items.Count(x=>x.kind==SJInteractable.Kind.Waste)!=5 || items.Count(x=>x.kind==SJInteractable.Kind.Repair)!=2)throw new Exception("Mission props count changed");
        log.Add("PASS: five wastes and two repair panels preserved; mannequin removed");
        log.Add("Main deck 24 x 90 m; lower hold 20 x 92 m; upper bridge 24 x 13 m. Fixed gameplay radius preserved.");
        File.WriteAllLines(Output+"/MapValidation.txt",log);
    }
    public static void BuildPlayer(){
        Directory.CreateDirectory(Output+"/Windows");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/SpaceJanitors/Scenes/MainMenu.unity",Scene},locationPathName=Output+"/Windows/SpaceJanitors.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Map build failed");
    }
    public static void RenderSaved(){
        EditorSceneManager.OpenScene(Scene);var player=Object.FindFirstObjectByType<SJPlayer>();player.gameObject.SetActive(false);
        var camera=new GameObject("RevisionPreviewCamera",typeof(Camera)).GetComponent<Camera>();camera.farClipPlane=2500;camera.nearClipPlane=.08f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.003f,.005f,.012f);camera.fieldOfView=75;
        Shot(camera,new Vector3(0,1.7f,10),new Vector3(0,1.8f,-30),"01_Concourse");Shot(camera,new Vector3(-3.6f,1.8f,0),new Vector3(-11,2,5),"02_SealedRoom");Shot(camera,new Vector3(0,1.7f,44),new Vector3(0,6.8f,59),"03_EnclosedStairs");Shot(camera,new Vector3(0,-3.3f,-35),new Vector3(0,-2.9f,20),"04_LowerHold");Shot(camera,new Vector3(-8,6.7f,61),new Vector3(4,7.4f,70),"05_CommandBridge");Shot(camera,new Vector3(63,42,-74),new Vector3(0,0,-4),"06_Exterior");Shot(camera,new Vector3(-18,8,-12),new Vector3(-250,90,-340),"07_Planet");Shot(camera,new Vector3(18,8,12),new Vector3(350,100,200),"08_RingedWorld");Shot(camera,new Vector3(-18,8,12),new Vector3(-800,300,500),"09_Sun");Object.DestroyImmediate(camera.gameObject);player.gameObject.SetActive(true);Validate();Debug.Log("MAP_REVISION_RELOADED_RENDER_PASS");
    }
    static void Shot(Camera c,Vector3 eye,Vector3 target,string name){c.transform.position=eye;c.transform.LookAt(target);var rt=new RenderTexture(1600,1000,24){antiAliasing=4};c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
}
