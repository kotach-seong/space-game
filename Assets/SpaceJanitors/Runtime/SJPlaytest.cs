using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Explicit opt-in smoke test in the actual standalone player. Normal play never runs this.
public class SJPlaytest : MonoBehaviour
{
    string output;
    readonly List<string> results=new List<string>();
    int errors;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--sj-selftest");
        if(i>=0 && i+1<args.Length){if(!UnityEngine.Object.FindFirstObjectByType<SJGame>())UnityEngine.SceneManagement.SceneManager.LoadScene("Kestrel_FirstPerson");var host=new GameObject("AutomatedPlaytest");DontDestroyOnLoad(host);var test=host.AddComponent<SJPlaytest>();test.output=args[i+1];}
    }
    void OnError(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception){errors++;results.Add("RUNTIME ERROR: "+message);}}
    IEnumerator Start()
    {
        Directory.CreateDirectory(output);Application.logMessageReceived+=OnError;
        var routine=Run();bool failed=false;
        while(true){bool next;object current=null;
            try {next=routine.MoveNext();if(next)current=routine.Current;}
            catch(Exception e){results.Add("FAIL: "+e);failed=true;break;}
            if(!next)break;yield return current;
        }
        if(errors>0)failed=true;
        results.Add(failed?"RESULT: FAIL":"RESULT: PASS");
        File.WriteAllLines(Path.Combine(output,"playtest.txt"),results);
        Debug.Log(failed?"SJ_PLAYTEST_FAILED":"SJ_PLAYTEST_PASSED");
        Application.logMessageReceived-=OnError;
        Application.Quit(failed?1:0);
    }
    void Check(bool condition,string message){if(!condition)throw new Exception(message);results.Add("PASS: "+message);Debug.Log("SJ_TEST: "+message);}
    void Capture(string name)
    {
        // Render directly so QA also works while the test window is hidden.
        var camera=SJGame.Instance.player.view;
        var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(image);
    }
    IEnumerator Run()
    {
        yield return null;yield return null;
        var game=SJGame.Instance;var p=game.player;p.automated=true;game.started=true;game.SetPaused(false);
        Check(p.zeroGravity,"Swimming is enabled by default");
        p.zeroGravity=false;
        yield return new WaitForSecondsRealtime(.5f);
        Check(p.view!=null&&!p.view.orthographic,"Perspective first-person camera exists");
        Check(Mathf.Abs(p.view.transform.localPosition.y-1.68f)<.01f,"Eye height is 1.68 m");
        Capture("01_FirstPerson_Interior");yield return new WaitForSecondsRealtime(.4f);
        Vector3 start=p.transform.position;
        for(int i=0;i<120;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
        Check(start.z-p.transform.position.z>5.5f,"Walk at least 5.5 m along corridor");
        Check(p.transform.position.y>-.1f,"Player remains on floor");
        p.Teleport(new Vector3(0,.1f,6),Quaternion.Euler(0,90,0));
        for(int i=0;i<80;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
        Check(p.transform.position.x<1.8f,"Character capsule cannot walk through corridor wall");

        var waste=GameObject.Find("Waste_0").GetComponent<SJInteractable>();
        p.Teleport(new Vector3(.65f,.05f,8.8f),Quaternion.Euler(0,180,0));
        p.view.transform.rotation=Quaternion.LookRotation(waste.Body.worldCenterOfMass-p.view.transform.position);
        yield return null;
        Check(p.Target()==waste,"Crosshair ray selects waste");
        Check(p.TryGrab(p.Target()),"Grab acquires waste rigidbody");
        yield return new WaitForSecondsRealtime(.7f);
        Check(p.held==waste&&!waste.Body.useGravity,"Grab holds waste against gravity");
        Check((waste.Body.worldCenterOfMass-(p.view.transform.position+p.view.transform.forward*2)).magnitude<.6f,"Held waste follows camera target");
        Capture("02_FirstPerson_Holding");yield return new WaitForSecondsRealtime(.3f);
        p.Release();Check(p.held==null&&waste.Body.useGravity,"Release restores gravity");
        Check(p.Push(waste),"Push accepts dynamic item");yield return new WaitForFixedUpdate();
        Check(waste.Body.linearVelocity.magnitude>1,"Push applies physical velocity");

        var bin=UnityEngine.Object.FindFirstObjectByType<SJBin>();
        waste.Body.linearVelocity=Vector3.zero;waste.Body.position=bin.transform.position;Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(.35f);
        Check(game.collected==1&&!waste.gameObject.activeSelf,"Bin physics trigger counts deposited waste");
        Check(!bin.Collect(waste)&&game.collected==1,"Same waste cannot be counted twice");
        foreach(var item in UnityEngine.Object.FindObjectsByType<SJInteractable>(FindObjectsSortMode.None))if(item.kind==SJInteractable.Kind.Waste)bin.Collect(item);
        Check(game.collected==5,"All five waste items can be collected");
        foreach(var item in UnityEngine.Object.FindObjectsByType<SJInteractable>(FindObjectsSortMode.None))if(item.kind==SJInteractable.Kind.Repair){item.RepairFor(1);Check(!item.completed,"Repair requires sustained work");item.RepairFor(1.1f);item.RepairFor(1);}
        Check(game.repairs==2&&game.Finished,"Two repairs counted exactly once; mission completes");
        if(game.canvasHUD){game.canvasHUD.Refresh();Check(game.canvasHUD.missionCounts.text.Contains("5/5") && game.canvasHUD.missionCounts.text.Contains("2/2"),"Canvas mission counters reflect gameplay");}

        p.Teleport(new Vector3(9,.1f,0),Quaternion.Euler(0,90,0));
        for(int i=0;i<180;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
        Check(p.transform.position.x>15.5f&&p.InSpace,"Capsule traverses real airlock into EVA: "+p.transform.position);
        float oldY=p.transform.position.y;
        for(int i=0;i<50;i++){p.MoveStep(Vector2.zero,1,false,false,1f/60);yield return null;}
        Check(p.transform.position.y>oldY+1,"EVA upward thrust changes altitude");
        p.tethered=true;
        p.Teleport(new Vector3(30,2,2.5f),Quaternion.Euler(0,90,0));
        for(int i=0;i<90;i++){p.MoveStep(Vector2.up,0,true,false,1f/60);yield return null;}
        Check(Vector3.Distance(p.transform.position,p.tetherOrigin)<=SJPlayer.TetherLength+.15f,"Tether limits outward travel to 18 m");
        p.Teleport(new Vector3(16,2.5f,10),Quaternion.Euler(0,-90,0));
        p.view.transform.rotation=Quaternion.LookRotation(new Vector3(12.6f,1.9f,8)-p.view.transform.position);
        Capture("03_FirstPerson_Exterior");yield return new WaitForSecondsRealtime(.5f);
        p.Respawn();Check(!p.InSpace&&!p.tethered&&Vector3.Distance(p.transform.position,new Vector3(0,.1f,10))<.1f,"Emergency return resets position and tether");
        if(p.useInteriorVolumes){
            p.Teleport(new Vector3(0,.08f,18),Quaternion.identity);
            for(int i=0;i<275;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
            Check(p.transform.position.z>29&&p.transform.position.y>4.35f&&!p.InSpace,"Walk upstairs to bridge: "+p.transform.position);
            Capture("04_Bridge_Runtime");yield return new WaitForSecondsRealtime(.3f);
            p.Teleport(new Vector3(0,.08f,-18),Quaternion.Euler(0,180,0));
            for(int i=0;i<245;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
            Check(p.transform.position.z < -28.5f && p.transform.position.y < -4.2f && !p.InSpace,"Walk downstairs to cargo hold: "+p.transform.position);
            p.transform.rotation=Quaternion.identity;
            for(int i=0;i<62;i++){p.MoveStep(Vector2.right,0,false,false,1f/60);yield return null;}
            for(int i=0;i<290;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
            Check(p.transform.position.z > -18 && p.transform.position.y < -4.2f && !p.InSpace,"Walk from lower landing into cargo bay: "+p.transform.position);
            Capture("05_Lower_Deck_Runtime");yield return new WaitForSecondsRealtime(.3f);
            p.Respawn();
        }
        p.zeroGravity=true;p.tethered=false;
        p.Teleport(new Vector3(25,8,0),Quaternion.identity);
        for(int i=0;i<60;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
        Check(p.SwimVelocity.z>2,"Swimming accelerates forward");
        float coastZ=p.transform.position.z;
        for(int i=0;i<30;i++){p.MoveStep(Vector2.zero,0,false,false,1f/60);yield return null;}
        Check(p.transform.position.z>coastZ+.5f && p.SwimVelocity.z>0,"Released input preserves drifting inertia");
        for(int i=0;i<60;i++)p.Brake(1f/60);
        Check(p.SwimVelocity.magnitude<.01f,"Brake stops drift");
        p.view.transform.localRotation=Quaternion.Euler(-45,0,0);
        float swimY=p.transform.position.y;
        for(int i=0;i<60;i++){p.MoveStep(Vector2.up,0,false,false,1f/60);yield return null;}
        Check(p.transform.position.y>swimY+.5f,"Swimming follows upward camera direction");
        var probe=new GameObject("ZeroG_QA_Probe");probe.transform.position=new Vector3(25,8,8);probe.AddComponent<SphereCollider>();var rb=probe.AddComponent<Rigidbody>();probe.AddComponent<SJFloatingBody>();
        yield return new WaitForSeconds(.5f);
        Check(!rb.useGravity && rb.position.y>8.1f && rb.angularVelocity.magnitude>.01f,"Loose object floats and rotates");
        rb.position=new Vector3(0,2,5);p.zeroGravity=false;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Check(rb.useGravity,"Object regains gravity inside ship");
        p.zeroGravity=true;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Check(!rb.useGravity,"Object returns to zero gravity");Destroy(probe);
        p.Respawn();Capture("06_ZeroG_Swimming");
        game.SetPaused(true);Check(Time.timeScale==0&&Cursor.lockState==CursorLockMode.None,"Pause releases mouse and freezes physics");
        if(game.canvasHUD){
            var hud=game.canvasHUD;hud.Refresh();
            Check(hud.pausePanel.activeSelf && !hud.crosshair.activeSelf,"Canvas pause menu visible and crosshair hidden");
            Check(hud.continueButton.onClick.GetPersistentEventCount()==1 && hud.restartButton.onClick.GetPersistentEventCount()==1 && hud.quitButton.onClick.GetPersistentEventCount()==1,"Canvas buttons have saved event bindings");
            var color=hud.missionCounts.color;var size=hud.missionCounts.fontSize;var position=hud.missionCounts.rectTransform.anchoredPosition;
            hud.Refresh();Check(hud.missionCounts.color==color && hud.missionCounts.fontSize==size && hud.missionCounts.rectTransform.anchoredPosition==position,"HUD updates preserve designer layout and style");
            hud.continueButton.onClick.Invoke();Check(!game.paused && !hud.pausePanel.activeSelf,"Canvas continue button resumes game");
            game.SetPaused(true);hud.Refresh();
        }
        yield return new WaitForSecondsRealtime(.5f);
        game.SetPaused(false);Check(Time.timeScale==1,"Resume restores simulation");
    }
}
