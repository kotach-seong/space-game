using System;
using System.IO;
using UnityEngine;

[Serializable] public class SJRunSave { public bool occupied; public int stage=1, credits; public string savedAt=""; }
[Serializable] public class SJPreferences {
    public float master=1,music=.65f,effects=.8f,voice=1,mic=1, sensitivity=1.8f,brightness=1;
    public bool fullscreen=true,invertY,shake=true,pushToTalk=true;
    public int width=1920,height=1080,quality=1;
    public string microphone="기본 장치";
    public string[] keys={"W","S","A","D","Space","LeftControl","LeftShift","Q","E","F","L","R","G","V"};
}
[Serializable] public class SJProfileData { public int version=1; public SJRunSave[] slots={new SJRunSave(),new SJRunSave(),new SJRunSave(),new SJRunSave()}; public SJPreferences settings=new SJPreferences(); }
public static class SJProfile {
    public static string TestDirectory;
    public static string PathName=>Path.Combine(TestDirectory??Application.persistentDataPath,"space-janitors-profile.json");
    static SJProfileData data;
    public static SJProfileData Data {get {if(data==null)Reload();return data;}}
    public static int ActiveSlot=-1;
    public static SJRunSave Run;
    public static bool ReturnToShop;
    public static string LastError="";
    public static SJPreferences Clone(SJPreferences p)=>JsonUtility.FromJson<SJPreferences>(JsonUtility.ToJson(p));
    public static void Reload(){
        data=new SJProfileData();LastError="";
        try {if(File.Exists(PathName)){var loaded=JsonUtility.FromJson<SJProfileData>(File.ReadAllText(PathName));if(loaded==null || loaded.slots==null || loaded.slots.Length!=4 || loaded.settings==null)throw new Exception("Invalid save");data=loaded;}}
        catch(Exception){LastError="저장 파일을 읽지 못했습니다. 원본 파일은 보존됩니다.";}
        if(data.settings.keys==null || data.settings.keys.Length!=14)data.settings.keys=new SJPreferences().keys;
        for(int i=0;i<4;i++)if(data.slots[i]==null)data.slots[i]=new SJRunSave();
    }
    public static bool Write(){
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName));string temp=PathName+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(Data,true));
            if(File.Exists(PathName)){
                // Some Unity/Windows installations cannot replace file metadata atomically.
                // Retain a recoverable previous checkpoint before the compatible overwrite.
                File.Copy(PathName,PathName+".bak",true);
                try{File.Replace(temp,PathName,null,true);}
                catch(IOException){File.Copy(temp,PathName,true);File.Delete(temp);}
                catch(PlatformNotSupportedException){File.Copy(temp,PathName,true);File.Delete(temp);}
            }else File.Move(temp,PathName);
            LastError="";return true;
        }catch(Exception e){LastError="저장하지 못했습니다: "+e.Message;return false;}
    }
    public static void SelectSlot(int index){
        if(index<0 || index>=4)throw new ArgumentOutOfRangeException();ActiveSlot=index;
        Run=JsonUtility.FromJson<SJRunSave>(JsonUtility.ToJson(Data.slots[index]));ReturnToShop=Run.occupied;
    }
    public static bool ArriveAtShop(){
        if(ActiveSlot<0 || Run==null)return false;
        var next=JsonUtility.FromJson<SJRunSave>(JsonUtility.ToJson(Run));next.occupied=true;next.stage++;next.savedAt=DateTime.Now.ToString("yyyy.MM.dd HH:mm");
        var old=Data.slots[ActiveSlot];Data.slots[ActiveSlot]=next;
        if(!Write()){Data.slots[ActiveSlot]=old;return false;}Run=next;ReturnToShop=true;return true;
    }
    public static bool GameOver(){
        if(ActiveSlot<0)return false;var old=Data.slots[ActiveSlot];Data.slots[ActiveSlot]=new SJRunSave();
        if(!Write()){Data.slots[ActiveSlot]=old;return false;}Run=new SJRunSave();ReturnToShop=false;return true;
    }
    public static KeyCode Key(int index){return Enum.TryParse<KeyCode>(Data.settings.keys[index],out var k)?k:KeyCode.None;}
    public static void ApplySettings(bool display=true){
        var p=Data.settings;AudioListener.volume=Mathf.Clamp01(p.master);
        QualitySettings.SetQualityLevel(Mathf.RoundToInt(Mathf.Clamp(p.quality,0,2)*(QualitySettings.names.Length-1)/2f),true);
        if(display && !Application.isEditor)Screen.SetResolution(p.width,p.height,p.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
    }
}
