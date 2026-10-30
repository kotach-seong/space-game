using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
public class SJMenuTest : MonoBehaviour {
    string output;List<string> checks=new List<string>();bool errors;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--sj-menu-test");if(i<0 || i+1>=args.Length)return;var go=new GameObject("MenuTests");DontDestroyOnLoad(go);var t=go.AddComponent<SJMenuTest>();t.output=args[i+1];SJProfile.TestDirectory=Path.Combine(t.output,"IsolatedSave");SJProfile.Reload();}
    void Log(string msg,string stack,LogType type){if(type==LogType.Exception || type==LogType.Error){errors=true;checks.Add("ERROR: "+msg);}}
    IEnumerator Start(){Directory.CreateDirectory(output);Application.logMessageReceived+=Log;var it=Run();bool failed=false;while(true){bool next;object step=null;try{next=it.MoveNext();if(next)step=it.Current;}catch(Exception e){checks.Add("FAIL: "+e);failed=true;break;}if(!next)break;yield return step;}checks.Add(failed||errors?"RESULT: FAIL":"RESULT: PASS");File.WriteAllLines(Path.Combine(output,"menu-tests.txt"),checks);Application.logMessageReceived-=Log;Application.Quit(failed||errors?1:0);}
    void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks.Add("PASS: "+msg);Debug.Log("MENU_TEST: "+msg);}
    void Capture(string name,int w=1600,int h=900){
        var menu=FindFirstObjectByType<SJMenu>();var canvas=menu.GetComponentInParent<Canvas>();var cam=FindFirstObjectByType<Camera>();
        var rt=new RenderTexture(w,h,24);cam.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();cam.Render();
        var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.ForceUpdateCanvases();rt.Release();Destroy(rt);Destroy(tex);
    }
    IEnumerator Run(){
        yield return null;yield return null;var menu=FindFirstObjectByType<SJMenu>();Check(menu && menu.home.activeSelf,"Main menu is startup scene");
        Capture("01_MainMenu");Capture("01b_MainMenu_4x3",1200,900);
        var soloButton=menu.home.transform.Find("SoloButton").GetComponent<Button>();var corners=new Vector3[4];soloButton.GetComponent<RectTransform>().GetWorldCorners(corners);var pointer=new PointerEventData(EventSystem.current){position=(corners[0]+corners[2])*.5f};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==soloButton,"Main button receives UI pointer raycast");
        soloButton.onClick.Invoke();Check(menu.solo.activeSelf && menu.slotLabels.Length==4,"Solo opens four independent slots");Capture("02_SaveSlots");
        menu.ShowOnline();Check(!menu.roomPreview.activeSelf,"No fake public server list");Capture("03_Online");
        menu.ShowCreate();menu.roomTitle.text="";menu.CreatePreview();Check(menu.createRoom.activeSelf,"Empty room title rejected");
        menu.roomTitle.text="우주 청소 야간반";menu.privateRoom.isOn=true;menu.roomPassword.text="";menu.CreatePreview();Check(menu.createRoom.activeSelf,"Private room requires password");
        menu.roomPassword.text="test-secret";Capture("04_CreateRoom");menu.CreatePreview();Check(menu.online.activeSelf && menu.lockIcon.activeSelf && menu.previewTitle.text.Contains("1 / 4"),"Private room preview shows lock and capacity four");Capture("05_LockedRoom");
        menu.search.text="없는 방";menu.FilterRooms();Check(!menu.roomPreview.activeSelf,"Room title search filters rows");menu.search.text="야간";menu.FilterRooms();Check(menu.roomPreview.activeSelf,"Room title search finds matching room");
        menu.JoinPreview();menu.joinPassword.text="wrong";menu.CheckPassword();Check(menu.message.text.Contains("일치하지"),"Wrong password rejected");menu.joinPassword.text="test-secret";menu.CheckPassword();Check(menu.message.text.Contains("연결이 필요"),"Correct password does not pretend online connection");
        menu.ShowSettings();var s=menu.settings;yield return null;Capture("06_Settings_Display");s.resolution.Show();yield return null;Check(s.resolution.transform.Find("Dropdown List")!=null,"Resolution dropdown opens valid selectable list");s.resolution.Hide();yield return new WaitForSecondsRealtime(.2f);s.ShowAudio();Capture("07_Settings_Audio");s.ShowControls();Capture("08_Settings_Controls");
        s.sensitivity.value=2.6f;s.master.value=.45f;s.Apply();Check(Mathf.Abs(SJProfile.Data.settings.sensitivity-2.6f)<.01f && Mathf.Abs(AudioListener.volume-.45f)<.01f,"Settings apply to sensitivity and master audio");
        SJProfile.Reload();Check(Mathf.Abs(SJProfile.Data.settings.sensitivity-2.6f)<.01f,"Preferences persist across reload");
        s.Defaults();s.Back();Check(Mathf.Abs(SJProfile.Data.settings.sensitivity-2.6f)<.01f,"Back discards unapplied defaults");
        SJProfile.SelectSlot(0);Check(!File.Exists(Path.Combine(SJProfile.TestDirectory,"slot0.json")) && !SJProfile.Data.slots[0].occupied,"New run does not create checkpoint before shop");
        SJProfile.Run.credits=120;Check(SJProfile.ArriveAtShop(),"Shop arrival saves active run: "+SJProfile.LastError);SJProfile.Reload();Check(SJProfile.Data.slots[0].occupied && SJProfile.Data.slots[0].credits==120 && SJProfile.Data.slots[0].stage==2,"Shop progress survives reload");
        SJProfile.SelectSlot(1);Check(SJProfile.ArriveAtShop(),"Second slot saves independently");SJProfile.SelectSlot(0);Check(SJProfile.GameOver(),"Game over clears active slot");SJProfile.Reload();Check(!SJProfile.Data.slots[0].occupied && SJProfile.Data.slots[1].occupied && Mathf.Abs(SJProfile.Data.settings.sensitivity-2.6f)<.01f,"Death preserves other slots and preferences");
        menu.Select(1);Check(menu.shop.activeSelf,"Continue returns to saved shop");Capture("09_ShopCheckpoint");menu.StartFlight();
        yield return null;yield return null;yield return new WaitForSecondsRealtime(.5f);
        var game=SJGame.Instance;game.player.automated=true;Check(game && game.canvasHUD.settings,"Gameplay scene loads with settings connection");
        game.canvasHUD.OpenSettings();Check(SJSettingsPanel.IsOpen && game.paused,"In-game settings keep game paused");game.canvasHUD.settings.Back();Check(!SJSettingsPanel.IsOpen && game.paused,"Settings back returns to pause");
        game.collected=5;game.repairs=2;game.ArriveAtShop();yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
        menu=FindFirstObjectByType<SJMenu>();Check(menu && menu.shop.activeSelf && SJProfile.Run.stage==3,"Mission complete enters shop and auto-saves next stage");
    }
}
