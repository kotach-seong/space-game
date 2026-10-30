using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BuildSwimming
{
    public static void Build(){
        string scene="Assets/Frigate/Scenes/Kestrel_FirstPerson.unity";
        // Resolve the saved scene by filename to preserve the original art layout.
        foreach(var guid in AssetDatabase.FindAssets("Kestrel_FirstPerson t:Scene"))scene=AssetDatabase.GUIDToAssetPath(guid);
        EditorSceneManager.OpenScene(scene);
        Object.FindFirstObjectByType<SJPlayer>().zeroGravity=true;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Deliverables/Swimming/Windows"));Directory.CreateDirectory(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=Path.Combine(output,"SpaceJanitors_Swimming.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Swimming build failed");
        Debug.Log("SWIMMING_BUILD_SUCCESS");
    }
}
