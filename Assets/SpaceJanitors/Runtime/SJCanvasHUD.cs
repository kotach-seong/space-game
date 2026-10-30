using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Only dynamic contents and visibility change here. Layout, font and colors belong to the scene.
public class SJCanvasHUD : MonoBehaviour
{
    public Text missionCounts, missionStatus, movementStatus, tetherStatus, interaction, notice;
    public GameObject pausePanel, crosshair, interactionPanel, noticePanel;
    public Button continueButton, restartButton, quitButton;
    public SJSettingsPanel settings;
    [Header("Editable dynamic wording: {0} = collected, {1} = repaired")]
    public string missionFormat="폐기물 {0}/5     수리 {1}/2";
    public string activeMission="목표: 청소 → 내부 수리 → 외부 수리";
    public string finishedMission="복구 성공! 자유 탐색 가능";
    void LateUpdate(){Refresh();}
    public void Refresh(){
        var g=SJGame.Instance;if(!g || !g.player)return;
        var p=g.player;
        // Literal replacement also tolerates braces typed by a designer.
        missionCounts.text=missionFormat.Replace("{0}",g.collected.ToString()).Replace("{1}",g.repairs.ToString());
        missionStatus.text=g.Finished?finishedMission:activeMission;
        movementStatus.text=p.Swimming?"무중력 / 수영 이동":"내부 / 인공중력";
        tetherStatus.text=p.tethered?$"안전줄 연결 {Vector3.Distance(p.transform.position,p.tetherOrigin):0.0}/18m":"안전줄 미연결";
        pausePanel.SetActive(g.paused && !SJSettingsPanel.IsOpen);crosshair.SetActive(!g.paused);
        string hint="";var target=p.Target();
        if(p.held)hint="좌클릭 유지: 잡기  /  놓기: 버튼 떼기  /  E: 밀기";
        else if(target){
            hint=target.displayName+"\n";
            if(target.CanGrab)hint+="좌클릭 유지: 잡기 / E: 밀기";
            else if(target.kind==SJInteractable.Kind.Repair)hint+=target.completed?"수리 완료":"F 유지: 수리 "+Mathf.RoundToInt(target.repairProgress/2*100)+"%";
            else if(target.kind==SJInteractable.Kind.Tether)hint+="F: 안전줄 연결 / 해제";
            else hint+="F: 임무 확인";
        }
        interaction.text=hint;interactionPanel.SetActive(!g.paused && hint!="");
        notice.text=g.ActiveNotice;noticePanel.SetActive(notice.text!="");
    }
    public void Continue(){var g=SJGame.Instance;g.started=true;g.SetPaused(false);Refresh();}
    public void Restart(){Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().path);}
    public void Quit(){Application.Quit();}
    public void OpenSettings(){settings.gameObject.SetActive(true);settings.Open();}
    public void MainMenu(){SJProfile.ReturnToShop=false;Time.timeScale=1;SceneManager.LoadScene("MainMenu");}
    public void Shop(){SJGame.Instance.ArriveAtShop();}
}
