using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SJSettingsPanel : MonoBehaviour {
    public static bool IsOpen;
    public GameObject displayTab,audioTab,controlTab;
    public Slider master,music,effects,voice,mic,sensitivity,brightness;
    public Text[] sliderValues;
    public Toggle fullscreen,invert,shake,pushToTalk;
    public Dropdown resolution,quality,microphone;
    public Button[] keyButtons;
    public Text[] keyLabels;
    public Text status;
    public SJMenu menu;
    SJPreferences draft;
    int capture=-1;
    readonly List<Vector2Int> sizes=new List<Vector2Int>();
    void Awake(){
        var sliders=new[]{master,music,effects,voice,mic,sensitivity,brightness};
        foreach(var slider in sliders)slider.onValueChanged.AddListener(_=>UpdateValues());
        for(int i=0;i<keyButtons.Length;i++){int index=i;keyButtons[i].onClick.AddListener(()=>{capture=index;status.text="새 키를 누르세요. Esc: 취소 (중복 키는 사용 불가)";});}
    }
    public void Open(){IsOpen=true;capture=-1;draft=SJProfile.Clone(SJProfile.Data.settings);Populate();ShowDisplay();status.text="변경 후 적용을 눌러 저장하세요.";}
    void OnDisable(){IsOpen=false;capture=-1;}
    public void ShowDisplay(){displayTab.SetActive(true);audioTab.SetActive(false);controlTab.SetActive(false);capture=-1;}
    public void ShowAudio(){displayTab.SetActive(false);audioTab.SetActive(true);controlTab.SetActive(false);capture=-1;}
    public void ShowControls(){displayTab.SetActive(false);audioTab.SetActive(false);controlTab.SetActive(true);}
    void Populate(){
        master.value=draft.master;music.value=draft.music;effects.value=draft.effects;voice.value=draft.voice;mic.value=draft.mic;sensitivity.value=draft.sensitivity;brightness.value=draft.brightness;
        fullscreen.isOn=draft.fullscreen;invert.isOn=draft.invertY;shake.isOn=draft.shake;pushToTalk.isOn=draft.pushToTalk;
        sizes.Clear();sizes.Add(new Vector2Int(1280,720));sizes.Add(new Vector2Int(1600,900));sizes.Add(new Vector2Int(1920,1080));
        foreach(var r in Screen.resolutions){var v=new Vector2Int(r.width,r.height);if(r.width>=800 && !sizes.Contains(v))sizes.Add(v);}
        var saved=new Vector2Int(draft.width,draft.height);if(!sizes.Contains(saved))sizes.Add(saved);
        resolution.ClearOptions();resolution.AddOptions(sizes.ConvertAll(v=>$"{v.x} × {v.y}"));resolution.value=sizes.IndexOf(saved);
        quality.ClearOptions();quality.AddOptions(new List<string>{"낮음","보통","높음"});quality.value=Mathf.Clamp(draft.quality,0,2);
        var devices=new List<string>{"기본 장치"};devices.AddRange(Microphone.devices);microphone.ClearOptions();microphone.AddOptions(devices);microphone.value=Mathf.Max(0,devices.IndexOf(draft.microphone));
        RefreshKeys();UpdateValues();
    }
    void RefreshKeys(){for(int i=0;i<keyLabels.Length;i++)keyLabels[i].text=draft.keys[i];}
    void UpdateValues(){var sliders=new[]{master,music,effects,voice,mic,sensitivity,brightness};for(int i=0;i<sliderValues.Length;i++)sliderValues[i].text=i==5?sliders[i].value.ToString("0.0"):Mathf.RoundToInt(sliders[i].value*100)+"%";}
    void Update(){
        if(capture<0 || !Input.anyKeyDown)return;
        if(Input.GetKeyDown(KeyCode.Escape)){capture=-1;status.text="키 변경 취소";return;}
        foreach(KeyCode code in Enum.GetValues(typeof(KeyCode))){
            if(code==KeyCode.None || code>=KeyCode.Mouse0 || !Input.GetKeyDown(code))continue;
            if(code==KeyCode.F2 || code==KeyCode.F3){status.text="F2/F3는 테스트 이동 키로 예약되어 있습니다.";return;}
            string key=code.ToString();for(int j=0;j<draft.keys.Length;j++)if(j!=capture && draft.keys[j]==key){status.text="이미 다른 동작에 사용 중인 키입니다.";return;}
            draft.keys[capture]=key;capture=-1;RefreshKeys();status.text="키 변경 완료. 적용을 눌러 저장하세요.";return;
        }
    }
    public void Defaults(){draft=new SJPreferences();Populate();status.text="기본값을 불러왔습니다. 적용을 눌러 저장하세요.";}
    public void Apply(){
        draft.master=master.value;draft.music=music.value;draft.effects=effects.value;draft.voice=voice.value;draft.mic=mic.value;draft.sensitivity=sensitivity.value;draft.brightness=brightness.value;
        draft.fullscreen=fullscreen.isOn;draft.invertY=invert.isOn;draft.shake=shake.isOn;draft.pushToTalk=pushToTalk.isOn;
        draft.width=sizes[resolution.value].x;draft.height=sizes[resolution.value].y;draft.quality=quality.value;draft.microphone=microphone.options[microphone.value].text;
        var old=SJProfile.Data.settings;SJProfile.Data.settings=SJProfile.Clone(draft);
        if(!SJProfile.Write()){SJProfile.Data.settings=old;status.text=SJProfile.LastError;return;}
        SJProfile.ApplySettings();status.text="설정을 저장했습니다.";
    }
    public void Back(){capture=-1;gameObject.SetActive(false);if(menu)menu.ShowHome();}
}
