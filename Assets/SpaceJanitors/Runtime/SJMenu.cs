using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SJMenu : MonoBehaviour {
    public GameObject home,solo,online,createRoom,passwordRoom,shop,settingsPanel;
    public SJSettingsPanel settings;
    public Text[] slotLabels;
    public Text message,shopStatus,roomStatus;
    public InputField roomTitle,roomPassword,search,joinPassword;
    public Toggle privateRoom;
    public GameObject privatePasswordGroup,roomPreview;
    public Text previewTitle,previewLock;
    public GameObject lockIcon;
    public string gameplayScene="Assets/Frigate/Scenes/Kestrel_FirstPerson.unity";
    string localTitle="",localPassword="";bool localPrivate;
    public void Start(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;SJProfile.ApplySettings();if(SJProfile.ReturnToShop)ShowShop();else ShowHome();}
    void Hide(){foreach(var p in new[]{home,solo,online,createRoom,passwordRoom,shop,settingsPanel})if(p)p.SetActive(false);message.text="";}
    public void ShowHome(){Hide();home.SetActive(true);}
    public void ShowSolo(){Hide();solo.SetActive(true);for(int i=0;i<4;i++){var s=SJProfile.Data.slots[i];slotLabels[i].text=s.occupied?$"SAVE 0{i+1}     이어하기\n다음 우주선 {s.stage} · 상점\n{s.savedAt}":$"SAVE 0{i+1}     새 게임\n비어 있는 저장 슬롯";}if(SJProfile.LastError!="")message.text=SJProfile.LastError;}
    public void Slot0()=>Select(0);public void Slot1()=>Select(1);public void Slot2()=>Select(2);public void Slot3()=>Select(3);
    public void Select(int index){SJProfile.SelectSlot(index);if(SJProfile.ReturnToShop)ShowShop();else StartFlight();}
    public void StartFlight(){SJProfile.ReturnToShop=false;Time.timeScale=1;SceneManager.LoadScene(gameplayScene);}
    public void ShowShop(){Hide();shop.SetActive(true);shopStatus.text=$"슬롯 {SJProfile.ActiveSlot+1} · 다음 우주선 {SJProfile.Run?.stage??1}\n마지막 상점 도착 시점이 저장되어 있습니다.\n\n상점 상품·경제 시스템은 아직 개발 전입니다.";}
    public void ShowOnline(){Hide();online.SetActive(true);FilterRooms();}
    public void FilterRooms(){bool visible=localTitle!="" && (search.text.Trim()=="" || localTitle.IndexOf(search.text.Trim(),System.StringComparison.OrdinalIgnoreCase)>=0);roomPreview.SetActive(visible);roomStatus.text=visible?"로컬 UI 미리보기 · 실제 온라인 방이 아닙니다.":"연결된 온라인 서비스가 없습니다.\n실제 방 목록은 네트워크 연결 후 표시됩니다.";}
    public void ShowCreate(){Hide();createRoom.SetActive(true);roomPassword.text="";UpdatePrivacy();}
    public void UpdatePrivacy(){privatePasswordGroup.SetActive(privateRoom.isOn);}
    public void OnPrivacyChanged(bool value){UpdatePrivacy();}
    public void CreatePreview(){
        string title=roomTitle.text.Trim(),pass=roomPassword.text;
        if(title.Length<1){message.text="방 제목을 입력하세요.";return;}
        if(privateRoom.isOn && string.IsNullOrWhiteSpace(pass)){message.text="비공개방은 비밀번호가 필요합니다.";return;}
        localTitle=title;localPrivate=privateRoom.isOn;localPassword=localPrivate?pass:"";
        previewTitle.text=localTitle+"     1 / 4";previewLock.text=localPrivate?"잠김":"공개";
        lockIcon.SetActive(localPrivate);
        search.text="";ShowOnline();message.text="방 디자인 미리보기만 생성했습니다. 다른 플레이어는 접속할 수 없습니다.";
    }
    public void JoinPreview(){if(localPrivate){Hide();passwordRoom.SetActive(true);joinPassword.text="";}else message.text="실제 입장은 온라인 서비스 연결 후 사용할 수 있습니다.";}
    public void CheckPassword(){message.text=joinPassword.text==localPassword?"비밀번호 확인 완료. 실제 입장은 온라인 연결이 필요합니다.":"비밀번호가 일치하지 않습니다.";}
    public void ShowSettings(){Hide();settingsPanel.SetActive(true);settings.Open();}
    public void Quit(){Application.Quit();}
}
