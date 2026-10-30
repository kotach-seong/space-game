using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class BuildCanvasUI
{
    const string ScenePath="Assets/Frigate/Scenes/Kestrel_FirstPerson.unity";
    static Font font;
    static RectTransform Box(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size){
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size){
        var r=Box(parent,name,anchor,pos,size);var im=r.gameObject.AddComponent<Image>();im.color=new Color(.025f,.07f,.09f,.94f);im.raycastTarget=false;return r;
    }
    static Text Label(Transform parent,string name,string value,float x,float y,float width,float height,int size=22){
        var r=Box(parent,name,new Vector2(0,1),new Vector2(x,-y),new Vector2(width,height));
        var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.92f,.96f,.95f);t.raycastTarget=false;t.supportRichText=false;return t;
    }
    static Button Button(Transform parent,string name,string caption,float x,float y,float width,UnityEngine.Events.UnityAction action){
        var r=Panel(parent,name,new Vector2(0,1),new Vector2(x,-y),new Vector2(width,58));
        var im=r.GetComponent<Image>();im.color=new Color(.10f,.30f,.35f);im.raycastTarget=true;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;UnityEventTools.AddPersistentListener(b.onClick,action);
        var t=Label(r,"Label",caption,8,8,width-16,42,24);t.alignment=TextAnchor.MiddleCenter;return b;
    }
    [MenuItem("Space Janitors/UI/Select Editable Canvas")]
    public static void SelectCanvas(){var h=Object.FindFirstObjectByType<SJCanvasHUD>();if(h)Selection.activeGameObject=h.gameObject;}
    public static void Build(){
        EditorSceneManager.OpenScene(ScenePath);
        if(Object.FindFirstObjectByType<SJCanvasHUD>())throw new System.Exception("Canvas already exists; do not overwrite designer edits.");
        Directory.CreateDirectory("Assets/SpaceJanitors/UI");
        // Keep a recoverable copy of the pre-migration scene.
        if(!File.Exists("Assets/Frigate/Scenes/Kestrel_BeforeCanvas.unity"))AssetDatabase.CopyAsset(ScenePath,"Assets/Frigate/Scenes/Kestrel_BeforeCanvas.unity");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/SpaceJanitors/UI/NanumGothic-Regular.ttf");
        if(!font)throw new System.Exception("Nanum Gothic font missing");
        var root=new GameObject("UI_Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(SJCanvasHUD));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var hud=root.GetComponent<SJCanvasHUD>();var game=Object.FindFirstObjectByType<SJGame>();game.canvasHUD=hud;
        var mission=Panel(root.transform,"MissionPanel",new Vector2(0,1),new Vector2(32,-30),new Vector2(560,178));
        Label(mission,"Title","SPACE JANITORS / 미션",24,18,515,38,26);
        hud.missionCounts=Label(mission,"MissionCounts","폐기물 0/5     수리 0/2",24,66,515,40,28);
        hud.missionStatus=Label(mission,"MissionStatus",hud.activeMission,24,120,515,38,21);
        var status=Panel(root.transform,"StatusPanel",Vector2.one,new Vector2(-32,-30),new Vector2(365,135));
        hud.movementStatus=Label(status,"MovementStatus","무중력 / 수영 이동",22,22,323,40,26);
        hud.tetherStatus=Label(status,"TetherStatus","안전줄 미연결",22,76,323,35);
        var controls=Panel(root.transform,"ControlsPanel",Vector2.zero,new Vector2(32,28),new Vector2(1450,100));
        Label(controls,"ControlsText","WASD 수영 · Space 상승 · Ctrl 하강 · Shift 가속 · Q 제동 · G 실내 중력 전환\n좌클릭 잡기 · E 밀기 · F 상호작용 · L 손전등 · R 복귀 · Esc 메뉴",22,16,1406,72,23);
        var cross=Box(root.transform,"Crosshair",new Vector2(.5f,.5f),Vector2.zero,new Vector2(32,40));
        var ct=cross.gameObject.AddComponent<Text>();ct.font=font;ct.text="+";ct.fontSize=30;ct.alignment=TextAnchor.MiddleCenter;ct.raycastTarget=false;hud.crosshair=cross.gameObject;
        var ip=Panel(root.transform,"InteractionPanel",new Vector2(.5f,.5f),new Vector2(0,-110),new Vector2(850,100));
        hud.interactionPanel=ip.gameObject;hud.interaction=Label(ip,"InteractionText","상호작용 안내",22,16,806,70);
        var np=Panel(root.transform,"NoticePanel",new Vector2(.5f,1),new Vector2(0,-230),new Vector2(1050,65));
        hud.noticePanel=np.gameObject;hud.notice=Label(np,"NoticeText","알림 메시지",22,16,1006,40);
        var pause=Panel(root.transform,"PausePanel",new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,680));hud.pausePanel=pause.gameObject;
        pause.GetComponent<Image>().raycastTarget=true;
        Label(pause,"Title","우주선 청소부들",40,32,820,68,44);
        Label(pause,"Description","1인칭 무중력 테스트\n\n떠다니는 쓰레기를 수거함에 넣고,\n작업실과 외벽의 패널을 F로 수리하세요.",40,126,820,175,28);
        Label(pause,"SafetyTip","외부로 나가기 전 안전줄을 연결하세요.\n길을 잃으면 R로 복귀할 수 있습니다.",40,312,820,86,24);
        hud.continueButton=Button(pause,"ContinueButton","시작 / 계속하기",40,414,820,hud.Continue);
        hud.restartButton=Button(pause,"RestartButton","처음부터 다시",40,488,398,hud.Restart);
        hud.quitButton=Button(pause,"QuitButton","종료",462,488,398,hud.Quit);
        Label(pause,"TestInfo","F2 에어록 입구 / F3 안전줄 연결 후 외부\n싱글플레이 프로토타입",40,577,820,74,22);
        if(!Object.FindFirstObjectByType<EventSystem>())new GameObject("UI_EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        // Hidden in play unless needed; all elements remain directly editable in the scene.
        ip.gameObject.SetActive(false);np.gameObject.SetActive(false);pause.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();BuildPlayer();
    }
    public static void BuildPlayer(){
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/CanvasUI/Windows"));Directory.CreateDirectory(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(output,"SpaceJanitors_Canvas.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Canvas build failed");Debug.Log("CANVAS_BUILD_SUCCESS");
    }
    public static void VerifySaved(){
        EditorSceneManager.OpenScene(ScenePath);
        var hud=Object.FindFirstObjectByType<SJCanvasHUD>();
        if(!hud || Object.FindFirstObjectByType<SJGame>().canvasHUD!=hud)throw new System.Exception("HUD scene reference missing");
        var canvas=hud.GetComponent<Canvas>();var camera=Object.FindFirstObjectByType<SJPlayer>().view;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/CanvasUI"));
        foreach(bool menu in new[]{false,true}){
            hud.pausePanel.SetActive(menu);Canvas.ForceUpdateCanvases();
            var rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,menu?"Menu.png":"HUD.png"),tex.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
        }
        Debug.Log("CANVAS_SAVED_SCENE_VERIFIED");
    }
    public static void FixFontAndBuild(){
        AssetDatabase.Refresh();EditorSceneManager.OpenScene(ScenePath);
        var f=AssetDatabase.LoadAssetAtPath<Font>("Assets/SpaceJanitors/UI/NanumGothic-Regular.ttf");if(!f)throw new System.Exception("Korean font missing");
        var hud=Object.FindFirstObjectByType<SJCanvasHUD>();
        foreach(var t in hud.GetComponentsInChildren<Text>(true))t.font=f;
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
        VerifySaved();BuildPlayer();
    }
}
