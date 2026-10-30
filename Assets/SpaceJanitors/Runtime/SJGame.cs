using UnityEngine;
using UnityEngine.SceneManagement;

public class SJGame : MonoBehaviour
{
    public static SJGame Instance;
    public SJPlayer player;
    public int collected,repairs;
    public bool paused=true;
    public bool started;
    public SJCanvasHUD canvasHUD;
    public string ActiveNotice => Time.unscaledTime<noticeUntil?notice:"";
    public bool Finished=>collected>=5&&repairs>=2;
    string notice="";float noticeUntil;
    Font font;GUIStyle text,small,title,button;Texture2D panel;
    void Awake(){Instance=this;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;}
    void Start(){foreach(var item in FindObjectsByType<SJInteractable>(FindObjectsSortMode.None))if(item.CanGrab && item.Body && !item.GetComponent<SJFloatingBody>())item.gameObject.AddComponent<SJFloatingBody>();SetPaused(true);}
    void Update(){if(!player.automated && !SJSettingsPanel.IsOpen && Input.GetKeyDown(KeyCode.Escape))SetPaused(!paused);}
    public void ArriveAtShop(){if(!Finished){Notify("아직 임무가 완료되지 않았습니다.");return;}if(SJProfile.ActiveSlot<0){Notify("메인화면에서 세이브 슬롯을 선택한 뒤 시작하세요.");return;}if(SJProfile.ArriveAtShop()){Time.timeScale=1;SceneManager.LoadScene("MainMenu");}else Notify(SJProfile.LastError);}
    public void GameOver(){if(SJProfile.ActiveSlot>=0 && !SJProfile.GameOver()){Notify(SJProfile.LastError);return;}SJProfile.ReturnToShop=false;Time.timeScale=1;SceneManager.LoadScene("MainMenu");}
    public void SetPaused(bool value){paused=value;Time.timeScale=value?0:1;Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
    public void Notify(string message){notice=message;noticeUntil=Time.unscaledTime+4;}
    void Styles()
    {
        if(text!=null)return;
        font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},18);
        text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,normal={textColor=new Color(.92f,.96f,.95f)},wordWrap=true};
        small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=32,fontStyle=FontStyle.Bold};
        button=new GUIStyle(GUI.skin.button){font=font,fontSize=18};
        panel=new Texture2D(1,1);panel.SetPixel(0,0,new Color(.025f,.07f,.09f,.91f));panel.Apply();
    }
    void Panel(Rect rect){GUI.DrawTexture(rect,panel);}
    void OnGUI()
    {
        if(canvasHUD && canvasHUD.isActiveAndEnabled)return;
        Styles();float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
        float w=Screen.width/scale,h=Screen.height/scale;
        Panel(new Rect(22,20,340,118));
        GUI.Label(new Rect(40,31,315,29),"SPACE JANITORS  /  1인칭 테스트",text);
        GUI.Label(new Rect(40,66,315,28),$"폐기물 {collected}/5     수리 {repairs}/2",text);
        GUI.Label(new Rect(40,102,315,23),Finished?"복구 성공!  자유 탐색 가능":"목표: 청소 → 내부 수리 → 외부 수리",small);
        Panel(new Rect(w-272,20,250,93));
        GUI.Label(new Rect(w-255,31,228,25),player.Swimming?"무중력 / 수영 이동":"내부 / 인공중력",text);
        GUI.Label(new Rect(w-255,66,230,28),player.tethered?$"안전줄 연결  {Vector3.Distance(player.transform.position,player.tetherOrigin):0.0}/18m":"안전줄 미연결",small);
        if(!paused){
            GUI.Label(new Rect(w*.5f-7,h*.5f-13,30,30),"+",text);
            var target=player.Target();string hint="";
            if(player.held)hint="좌클릭 유지: 잡기    놓기: 버튼 떼기    E: 밀기";
            else if(target){
                hint=target.displayName+"\n";
                if(target.CanGrab)hint+="좌클릭 유지: 잡기  /  E: 밀기";
                else if(target.kind==SJInteractable.Kind.Repair)hint+=target.completed?"수리 완료":"F 누르기 유지: 수리 "+Mathf.RoundToInt(target.repairProgress/2*100)+"%";
                else if(target.kind==SJInteractable.Kind.Tether)hint+="F: 안전줄 연결 / 해제";
                else hint+="F: 임무 확인";
            }
            if(hint!=""){Panel(new Rect(w*.5f-260,h*.5f+40,520,72));GUI.Label(new Rect(w*.5f-245,h*.5f+46,490,63),hint,text);}
        }
        Panel(new Rect(22,h-78,w-44,58));
        GUI.Label(new Rect(38,h-72,w-70,48),"WASD 수영  ·  Space 상승  ·  Ctrl 하강  ·  Shift 가속  ·  Q 제동  ·  G 실내 중력 전환\n좌클릭 잡기  ·  E 밀기  ·  F 상호작용  ·  L 손전등  ·  R 복귀  ·  Esc 메뉴",small);
        if(Time.unscaledTime<noticeUntil){Panel(new Rect(w*.5f-350,152,700,48));GUI.Label(new Rect(w*.5f-334,162,668,30),notice,text);}
        if(paused){
            Panel(new Rect(w*.5f-305,h*.5f-235,610,470));
            GUI.Label(new Rect(w*.5f-276,h*.5f-210,552,55),"우주선 청소부들",title);
            GUI.Label(new Rect(w*.5f-276,h*.5f-153,552,110),"1인칭 싱글플레이 프로토타입\n\n바닥의 쓰레기를 주황색 수거함에 넣고,\n작업실과 외벽의 노란 패널을 F로 수리하세요.",text);
            GUI.Label(new Rect(w*.5f-276,h*.5f-28,552,65),"외부로 나가기 전 에어록에서 안전줄을 연결하세요.\n길을 잃으면 R로 복귀할 수 있습니다.",small);
            if(GUI.Button(new Rect(w*.5f-276,h*.5f+43,552,45),started?"계속하기":"테스트 시작",button)){started=true;SetPaused(false);}
            if(GUI.Button(new Rect(w*.5f-276,h*.5f+99,270,38),"처음부터 다시",button)){Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
            if(GUI.Button(new Rect(w*.5f+6,h*.5f+99,270,38),"종료",button))Application.Quit();
            GUI.Label(new Rect(w*.5f-276,h*.5f+153,552,65),"빠른 탐색: F2 에어록 입구 / F3 안전줄 연결 후 외부\n이번 버전: 이동·잡기·수리 테스트. 멀티플레이 미포함.",small);
        }
    }
    void OnDestroy(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
}
