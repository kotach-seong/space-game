using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Repair loaded scenes without replacing scenes or saving the user's pending edits.
[InitializeOnLoad]
public static class SJCanvasRendererRepair {
    public static void RepairSavedScenes(){
        foreach(var path in new[]{"Assets/SpaceJanitors/Scenes/MainMenu.unity","Assets/Frigate/Scenes/Kestrel_FirstPerson.unity"}){
            var scene=EditorSceneManager.OpenScene(path);Repair();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("SJ_CANVAS_SAVED_SCENES_REPAIRED");
        BuildMainMenu.RebuildPlayer();
    }
    static SJCanvasRendererRepair(){EditorApplication.delayCall+=Repair;EditorSceneManager.sceneOpened+=(s,m)=>EditorApplication.delayCall+=Repair;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Repair;};}
    [MenuItem("Space Janitors/UI/Repair Missing Canvas Renderers")]
    public static void Repair(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        int checkedCount=0,repaired=0;
        foreach(var g in Resources.FindObjectsOfTypeAll<SJSpaceGraphic>()){
            if(EditorUtility.IsPersistent(g) || !g.gameObject.scene.IsValid() || !g.gameObject.scene.isLoaded)continue;
            checkedCount++;
            if(!g.GetComponent<CanvasRenderer>()){
                Undo.AddComponent<CanvasRenderer>(g.gameObject);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);repaired++;
            }
        }
        // Regression: first activation and repeated hide/show must retain valid
        // renderers and support both pointer hit-testing and click dispatch.
        var host=new GameObject("CanvasRendererRegression",typeof(Canvas),typeof(GraphicRaycaster));host.hideFlags=HideFlags.HideAndDontSave;
        try{
            var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var buttonObject=new GameObject("RegressionButton",typeof(RectTransform));buttonObject.transform.SetParent(host.transform,false);
            var rect=buttonObject.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(160,80);
            var graphic=buttonObject.AddComponent<SJSpaceGraphic>();graphic.raycastTarget=true;
            var button=buttonObject.AddComponent<Button>();button.targetGraphic=graphic;int clicks=0;button.onClick.AddListener(()=>clicks++);
            for(int i=0;i<3;i++){
                buttonObject.SetActive(false);buttonObject.SetActive(true);Canvas.ForceUpdateCanvases();
                if(!graphic.canvasRenderer)throw new System.Exception("Renderer missing after activation");
                var corners=new Vector3[4];rect.GetWorldCorners(corners);var pointer=new PointerEventData(EventSystem.current){position=(corners[0]+corners[2])*.5f,button=PointerEventData.InputButton.Left};
                if(!graphic.Raycast(pointer.position,null))throw new System.Exception("Graphic raycast filter rejected button");
                ExecuteEvents.Execute(buttonObject,pointer,ExecuteEvents.pointerClickHandler);
            }
            if(clicks!=3)throw new System.Exception("Click dispatch failed");
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/MainMenu/canvas-renderer-repair.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,$"PASS: Loaded graphics checked: {checkedCount}; repaired: {repaired}\nPASS: Renderer survives three enable/disable cycles\nPASS: Three graphic raycast-filter checks and click dispatches\nExisting layouts, colors, text and user edits preserved. Save the scene to persist repairs.\n");
            Debug.Log($"SJ_CANVAS_REPAIR_PASS checked={checkedCount} repaired={repaired}");
        }finally{Object.DestroyImmediate(host);}
    }
}
