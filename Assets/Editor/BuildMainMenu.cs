using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class BuildMainMenu {
    const string MenuPath="Assets/SpaceJanitors/Scenes/MainMenu.unity",GamePath="Assets/Frigate/Scenes/Kestrel_FirstPerson.unity";
    static Font font;
    static Color Ink=new Color(.025f,.048f,.085f,.97f),Muted=new Color(.49f,.65f,.72f),Mint=new Color(.42f,.91f,.82f),White=new Color(.91f,.96f,.98f),Orange=new Color(1,.66f,.36f);
    static RectTransform Rect(Transform p,string name,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(p,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static SJSpaceGraphic Shape(Transform p,string name,float x,float y,float w,float h,Color color,SJSpaceGraphic.Shape shape=SJSpaceGraphic.Shape.RoundPanel){var g=Rect(p,name,x,y,w,h).gameObject.AddComponent<SJSpaceGraphic>();g.color=color;g.shape=shape;g.raycastTarget=false;return g;}
    static Text Text(Transform p,string name,string value,float x,float y,float w,float h,int size=22,Color? color=null,TextAnchor alignment=TextAnchor.MiddleLeft){var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=color??White;t.alignment=alignment;t.raycastTarget=false;t.supportRichText=false;return t;}
    static Button Btn(Transform p,string name,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,bool primary=false){
        var g=Shape(p,name,x,y,w,h,primary?Mint:new Color(.075f,.14f,.20f,.98f));g.raycastTarget=true;
        var b=g.gameObject.AddComponent<Button>();b.targetGraphic=g;var c=b.colors;c.highlightedColor=new Color(.8f,1,1);c.pressedColor=new Color(.55f,.77f,.82f);c.selectedColor=new Color(.86f,1,1);b.colors=c;
        Text(g.transform,"Label",label,24,0,w-48,h,24,primary?Ink:White);if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);return b;
    }
    static InputField Input(Transform p,string name,string placeholder,float x,float y,float w,bool password=false){var g=Shape(p,name,x,y,w,62,new Color(.015f,.032f,.06f));g.raycastTarget=true;var input=g.gameObject.AddComponent<InputField>();input.targetGraphic=g;input.textComponent=Text(g.transform,"Text","",20,0,w-40,62);var ph=Text(g.transform,"Placeholder",placeholder,20,0,w-40,62,22,Muted);input.placeholder=ph;input.characterLimit=password?32:40;input.contentType=password?InputField.ContentType.Password:InputField.ContentType.Standard;return input;}
    static Toggle Toggle(Transform p,string name,string label,float x,float y,float w){var r=Rect(p,name,x,y,w,44);var t=r.gameObject.AddComponent<Toggle>();var bg=Shape(r,"Box",0,6,30,30,new Color(.13f,.24f,.3f));bg.raycastTarget=true;var check=Shape(bg.transform,"Check",6,6,18,18,Mint);check.radius=5;t.targetGraphic=bg;t.graphic=check;Text(r,"Label",label,48,0,w-48,44);return t;}
    static Slider Slider(Transform p,string name,float x,float y,float w,float min=0,float max=1){var r=Rect(p,name,x,y,w,40);var s=r.gameObject.AddComponent<Slider>();s.minValue=min;s.maxValue=max;
        Shape(r,"Track",0,15,w,8,new Color(.14f,.24f,.29f));var fill=Shape(r,"Fill",0,15,w,8,Mint);s.fillRect=fill.rectTransform;
        var handle=Shape(r,"Handle",0,6,20,26,White);s.handleRect=handle.rectTransform;s.targetGraphic=handle;handle.raycastTarget=true;FixSlider(s);return s;}
    static void FixSlider(Slider s){
        float width=s.GetComponent<RectTransform>().rect.width;
        var area=s.transform.Find("FillArea") as RectTransform;if(!area)area=Rect(s.transform,"FillArea",0,16,width,8);
        var handleArea=s.transform.Find("HandleArea") as RectTransform;if(!handleArea)handleArea=Rect(s.transform,"HandleArea",0,7,width,26);
        var f=s.fillRect;f.SetParent(area,false);f.anchorMin=Vector2.zero;f.anchorMax=Vector2.one;f.pivot=new Vector2(.5f,.5f);f.anchoredPosition=Vector2.zero;f.sizeDelta=Vector2.zero;
        var h=s.handleRect;h.SetParent(handleArea,false);h.anchorMin=new Vector2(0,0);h.anchorMax=new Vector2(0,1);h.pivot=new Vector2(.5f,.5f);h.anchoredPosition=Vector2.zero;h.sizeDelta=new Vector2(20,0);
        var hit=s.GetComponent<Image>();if(!hit)hit=s.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
    }
    static Dropdown Drop(Transform p,string name,float x,float y,float w){
        var g=Shape(p,name,x,y,w,48,new Color(.075f,.14f,.20f));g.raycastTarget=true;var d=g.gameObject.AddComponent<Dropdown>();d.targetGraphic=g;
        d.captionText=Text(g.transform,"Label","선택",16,0,w-58,48,20);Text(g.transform,"Arrow","v",w-38,0,24,48,20,Mint);
        var template=Rect(g.transform,"Template",0,48,w,200);var im=template.gameObject.AddComponent<Image>();im.color=new Color(.045f,.085f,.13f);var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
        var viewport=Rect(template,"Viewport",0,0,w,200);viewport.gameObject.AddComponent<RectMask2D>();var content=Rect(viewport,"Content",0,0,w,40);
        var item=Rect(content,"Item",0,0,w,40);var toggle=item.gameObject.AddComponent<Toggle>();var ib=item.gameObject.AddComponent<Image>();ib.color=new Color(.09f,.19f,.24f);toggle.targetGraphic=ib;var mark=Text(item,"Selected",">",8,0,20,40,20,Mint);toggle.graphic=mark;
        d.itemText=Text(item,"ItemLabel","선택",32,0,w-40,40,18);scroll.viewport=viewport;scroll.content=content;scroll.movementType=ScrollRect.MovementType.Clamped;d.template=template;template.gameObject.SetActive(false);return d;
    }
    static RectTransform Page(Transform p,string name,string heading,string subtitle){var panel=Shape(p,name,180,260,1240,555,Ink);panel.raycastTarget=true;Text(panel.transform,"Heading",heading,38,20,1100,50,32);Text(panel.transform,"Subtitle",subtitle,40,76,1130,38,19,Muted);return panel.rectTransform;}
    static SJSettingsPanel Settings(Transform parent,SJMenu menu){
        var panel=Shape(parent,"SettingsPanel",160,236,1280,605,Ink);panel.raycastTarget=true;var s=panel.gameObject.AddComponent<SJSettingsPanel>();s.menu=menu;
        Text(panel.transform,"Heading","설정",36,18,900,46,32);
        Btn(panel.transform,"DisplayTabButton","화면",36,82,190,46,s.ShowDisplay);Btn(panel.transform,"AudioTabButton","소리",240,82,190,46,s.ShowAudio);Btn(panel.transform,"ControlsTabButton","조작",444,82,190,46,s.ShowControls);
        s.displayTab=Rect(panel.transform,"DisplayTab",36,146,1208,334).gameObject;s.audioTab=Rect(panel.transform,"AudioTab",36,146,1208,334).gameObject;s.controlTab=Rect(panel.transform,"ControlsTab",36,146,1208,334).gameObject;
        var dt=s.displayTab.transform;s.fullscreen=Toggle(dt,"Fullscreen","전체화면 (해제하면 창모드)",0,8,540);
        Text(dt,"ResolutionLabel","해상도",0,76,220,42);s.resolution=Drop(dt,"Resolution",240,76,330);
        Text(dt,"QualityLabel","그래픽 품질",0,148,220,42);s.quality=Drop(dt,"Quality",240,148,330);
        Text(dt,"BrightnessLabel","밝기",0,220,220,42);s.brightness=Slider(dt,"Brightness",240,220,300,.25f,1);var bright=Text(dt,"BrightnessValue","100%",568,220,110,40,20,Mint);
        Text(dt,"DisplayHint","밝기는 화면 어둡기 조절입니다.\n해상도 변경은 실행 파일에서 적용됩니다.",750,78,420,110,21,Muted);
        var at=s.audioTab.transform;string[] labels={"전체 음량","배경음악","효과음","다른 플레이어 음성","마이크 입력"};var sliders=new Slider[5];var values=new Text[7];
        for(int i=0;i<5;i++){Text(at,"VolumeLabel"+i,labels[i],0,i*55,260,40);sliders[i]=Slider(at,"Volume"+i,270,i*55,300);values[i]=Text(at,"Value"+i,"100%",595,i*55,95,40,20,Mint);}
        s.master=sliders[0];s.music=sliders[1];s.effects=sliders[2];s.voice=sliders[3];s.mic=sliders[4];
        Text(at,"MicLabel","마이크 장치",740,0,420,40);s.microphone=Drop(at,"Microphone",740,50,430);s.pushToTalk=Toggle(at,"PushToTalk","눌러서 말하기 (해제: 항상 켜기)",740,116,460);
        Text(at,"AudioHint","음악·효과음·음성 시스템은 연결 전입니다.\n개별 음량과 마이크 설정은 저장만 됩니다.\n현재 마이크를 녹음하거나 전송하지 않습니다.",740,183,455,105,17,Muted);
        var ct=s.controlTab.transform;Text(ct,"SensitivityLabel","마우스 감도",0,0,205,40);s.sensitivity=Slider(ct,"Sensitivity",205,0,270,.2f,5);values[5]=Text(ct,"SensitivityValue","1.8",492,0,65,40,20,Mint);values[6]=bright;s.sliderValues=values;
        s.invert=Toggle(ct,"InvertY","상하 반전",590,0,260);s.shake=Toggle(ct,"ScreenShake","화면 흔들림 허용",860,0,340);
        string[] actions={"앞으로","뒤로","왼쪽","오른쪽","상승 / 점프","하강","가속","제동","밀기","상호작용","손전등","실내 복귀","중력 전환","눌러서 말하기"};s.keyButtons=new Button[14];s.keyLabels=new Text[14];
        for(int i=0;i<14;i++){int col=i/5,row=i%5;float x=col*405,y=66+row*46;Text(ct,"Action"+i,actions[i],x,y,200,38,19);s.keyButtons[i]=Btn(ct,"Key"+i,"",x+202,y,175,38,null);s.keyLabels[i]=s.keyButtons[i].GetComponentInChildren<Text>();s.keyLabels[i].fontSize=18;}
        Text(ct,"ControlsHint","잡기: 좌클릭 고정 · 메뉴: Esc 고정 · 현재 카메라 흔들림 효과는 없음",0,303,1200,28,16,Muted);
        s.status=Text(panel.transform,"Status","",36,486,1208,34,18,Orange);
        Btn(panel.transform,"DefaultsButton","기본값으로",36,535,220,46,s.Defaults);Btn(panel.transform,"ApplyButton","적용",780,535,220,46,s.Apply,true);Btn(panel.transform,"BackButton","뒤로",1020,535,224,46,s.Back);
        s.displayTab.SetActive(true);s.audioTab.SetActive(false);s.controlTab.SetActive(false);panel.gameObject.SetActive(false);return s;
    }
    public static void Build(){
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/SpaceJanitors/UI/NanumGothic-Regular.ttf");if(!font)throw new Exception("Font missing");
        if(File.Exists(MenuPath))throw new Exception("MainMenu already exists; preserve designer edits and use RebuildPlayer instead.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("MenuCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.023f,.047f);camera.orthographic=true;
        var root=new GameObject("MainMenu_Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var frame=Rect(root.transform,"DesignFrame",0,0,1600,900);frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
        var menu=frame.gameObject.AddComponent<SJMenu>();
        Shape(frame,"Stars",0,0,1600,900,new Color(.65f,.85f,1),SJSpaceGraphic.Shape.Stars);
        Shape(frame,"Planet",-190,385,810,810,new Color(.12f,.43f,.48f),SJSpaceGraphic.Shape.Planet);
        Shape(frame,"Orbit_01",-254,321,938,938,new Color(.25f,.68f,.69f,.2f),SJSpaceGraphic.Shape.Ring);
        Shape(frame,"Orbit_02",1040,-355,780,780,new Color(.27f,.6f,.7f,.12f),SJSpaceGraphic.Shape.Ring);
        Shape(frame,"TopAccent",746,65,108,4,Mint).radius=0;
        Text(frame,"Title","우주선 청소부들",200,87,1200,95,64,White,TextAnchor.MiddleCenter);
        Text(frame,"EnglishTitle","S P A C E   J A N I T O R S",200,184,1200,38,21,Mint,TextAnchor.MiddleCenter);
        Text(frame,"TopLeftTag","KESTREL / CREW TERMINAL",44,30,550,30,16,Muted);
        Text(frame,"TopRightTag","01 — ORBITAL SHIFT",1140,30,410,30,16,Muted,TextAnchor.MiddleRight);
        Text(frame,"Footer","작은 청소부들. 아주 큰 우주.",44,847,700,32,19,Muted);
        Text(frame,"Version","PROTOTYPE  /  0.3",1190,847,366,32,16,Muted,TextAnchor.MiddleRight);
        menu.home=Rect(frame,"HomePanel",0,260,1600,545).gameObject;
        Text(menu.home.transform,"Subtitle","다음 근무를 시작할 준비가 됐나요?",400,0,800,46,23,Muted,TextAnchor.MiddleCenter);
        Btn(menu.home.transform,"SoloButton","01     혼자 게임",575,89,450,76,menu.ShowSolo,true);
        Btn(menu.home.transform,"OnlineButton","02     온라인 게임",575,181,450,76,menu.ShowOnline);
        Btn(menu.home.transform,"SettingsButton","03     설정",575,273,450,76,menu.ShowSettings);
        Btn(menu.home.transform,"QuitButton","04     나가기",575,365,450,76,menu.Quit);
        var solo=Page(frame,"SoloPanel","혼자 게임","상점 도착 시 자동 저장 · 게임 오버 시 해당 슬롯 초기화");menu.solo=solo.gameObject;menu.slotLabels=new Text[4];UnityEngine.Events.UnityAction[] slotActions={menu.Slot0,menu.Slot1,menu.Slot2,menu.Slot3};
        for(int i=0;i<4;i++){var b=Btn(solo,"SaveSlot_0"+(i+1),"",40+(i%2)*592,138+(i/2)*143,568,122,slotActions[i]);menu.slotLabels[i]=b.GetComponentInChildren<Text>();menu.slotLabels[i].fontSize=22;menu.slotLabels[i].text=$"SAVE 0{i+1}     새 게임\n비어 있는 저장 슬롯";}
        Btn(solo,"BackButton","뒤로",970,464,230,56,menu.ShowHome);
        var online=Page(frame,"OnlinePanel","온라인 게임","모든 방 · 공개 / 비공개 · 최대 4명");menu.online=online.gameObject;
        Text(online,"Columns","방 제목                                                                        인원 / 상태",40,134,1150,40,19,Muted);
        Shape(online,"Divider",40,179,1160,1,new Color(.2f,.36f,.42f));
        menu.roomStatus=Text(online,"ConnectionStatus","연결된 온라인 서비스가 없습니다.",40,292,1160,80,22,Muted,TextAnchor.MiddleCenter);
        var row=Btn(online,"RoomPreview","",40,194,1160,80,menu.JoinPreview);menu.roomPreview=row.gameObject;menu.previewTitle=row.GetComponentInChildren<Text>();menu.previewLock=Text(row.transform,"Privacy","공개",945,0,180,80,21,Orange);
        var lockRoot=Rect(row.transform,"LockIcon",895,22,32,36);menu.lockIcon=lockRoot.gameObject;Shape(lockRoot,"Shackle",6,0,20,20,Orange,SJSpaceGraphic.Shape.Ring);Shape(lockRoot,"Body",0,13,32,23,Orange).radius=4;Shape(lockRoot,"Keyhole",14,19,4,11,Ink).radius=2;
        menu.search=Input(online,"SearchInput","방 제목으로 찾기",40,414,580);Btn(online,"SearchButton","찾기",638,414,150,62,menu.FilterRooms);Btn(online,"CreateButton","온라인 게임방 만들기",810,414,390,62,menu.ShowCreate,true);Btn(online,"BackButton","뒤로",970,488,230,44,menu.ShowHome);row.gameObject.SetActive(false);
        var create=Page(frame,"CreateRoomPanel","온라인 게임방 만들기","UI 미리보기 · 서버 연결 전에는 실제 방이 생성되지 않습니다.");menu.createRoom=create.gameObject;
        Text(create,"RoomTitleLabel","방 제목",40,138,180,50);menu.roomTitle=Input(create,"RoomTitleInput","함께 청소할 방 이름",250,138,910);
        menu.privateRoom=Toggle(create,"PrivateRoomToggle","비공개방 (해제하면 공개방)",250,225,700);UnityEventTools.AddPersistentListener(menu.privateRoom.onValueChanged,menu.OnPrivacyChanged);
        var pw=Rect(create,"PasswordGroup",40,292,1160,64);menu.privatePasswordGroup=pw.gameObject;Text(pw,"Label","비밀번호",0,0,180,50);menu.roomPassword=Input(pw,"PasswordInput","비공개방 비밀번호",210,0,910,true);
        Text(create,"Capacity","정원 4명 · 비공개방도 목록에 표시되며 잠금 표시가 붙습니다.",40,385,1150,40,20,Muted);
        Btn(create,"CreateButton","로컬 방 미리보기",650,464,310,56,menu.CreatePreview,true);Btn(create,"BackButton","뒤로",980,464,220,56,menu.ShowOnline);
        var password=Page(frame,"JoinPasswordPanel","잠긴 방 입장","비공개방의 비밀번호를 입력하세요.");menu.passwordRoom=password.gameObject;menu.joinPassword=Input(password,"PasswordInput","비밀번호",220,190,800,true);Btn(password,"JoinButton","확인",640,345,300,60,menu.CheckPassword,true);Btn(password,"BackButton","뒤로",960,345,240,60,menu.ShowOnline);
        var shop=Page(frame,"ShopCheckpointPanel","상점에 도착했습니다","AUTO SAVE / 마지막 상점에서 이어하기");menu.shop=shop.gameObject;menu.shopStatus=Text(shop,"SaveStatus","",40,145,1120,210,25);Btn(shop,"NextFlightButton","다음 우주선 출발",650,454,320,62,menu.StartFlight,true);Btn(shop,"HomeButton","메인화면",990,454,210,62,menu.ShowHome);
        menu.settings=Settings(frame,menu);menu.settingsPanel=menu.settings.gameObject;
        menu.message=Text(frame,"Feedback","",190,814,1220,32,18,Orange,TextAnchor.MiddleCenter);
        foreach(var p in new[]{menu.solo,menu.online,menu.createRoom,menu.passwordRoom,menu.shop})p.SetActive(false);
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene,MenuPath);
        UpgradeGameplay();EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MenuPath,true),new EditorBuildSettingsScene(GamePath,true)};
        AssetDatabase.SaveAssets();RebuildPlayer();
    }
    static void UpgradeGameplay(){
        if(!File.Exists("Assets/Frigate/Scenes/Kestrel_BeforeMainMenu.unity"))AssetDatabase.CopyAsset(GamePath,"Assets/Frigate/Scenes/Kestrel_BeforeMainMenu.unity");
        var scene=EditorSceneManager.OpenScene(GamePath);var hud=Object.FindFirstObjectByType<SJCanvasHUD>();if(!hud)throw new Exception("Editable HUD missing");
        var frame=Rect(hud.transform,"MenuSettingsFrame",0,0,1600,900);frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
        hud.settings=Settings(frame,null);
        // Append buttons without altering existing designer-owned HUD children.
        Btn(hud.pausePanel.transform,"SettingsButton","설정",40,644,255,52,hud.OpenSettings);
        Btn(hud.pausePanel.transform,"MainMenuButton","메인화면",315,644,255,52,hud.MainMenu);
        Btn(hud.pausePanel.transform,"ShopButton","완료 후 상점",590,644,270,52,hud.Shop,true);
        var pr=hud.pausePanel.GetComponent<RectTransform>();pr.sizeDelta=new Vector2(pr.sizeDelta.x,735);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void RebuildPlayer(){
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/MainMenu/Windows"));Directory.CreateDirectory(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{MenuPath,GamePath},locationPathName=Path.Combine(output,"SpaceJanitors.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Menu build failed");Debug.Log("MAIN_MENU_BUILD_SUCCESS");
    }
    public static void PolishAndRebuild(){
        foreach(string path in new[]{MenuPath,GamePath}){
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var slider in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include,FindObjectsSortMode.None))FixSlider(slider);
            var hud=Object.FindFirstObjectByType<SJCanvasHUD>();if(hud){foreach(string name in new[]{"SettingsButton","MainMenuButton","ShopButton"}){var r=hud.pausePanel.transform.Find(name).GetComponent<RectTransform>();r.anchoredPosition=new Vector2(r.anchoredPosition.x,-668);}var pr=hud.pausePanel.GetComponent<RectTransform>();pr.sizeDelta=new Vector2(pr.sizeDelta.x,750);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        RebuildPlayer();
    }
}
