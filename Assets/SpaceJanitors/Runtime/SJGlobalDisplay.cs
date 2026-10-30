using UnityEngine;
using UnityEngine.UI;
public class SJGlobalDisplay : MonoBehaviour {
    Image shade;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){var go=new GameObject("BrightnessOverlay");DontDestroyOnLoad(go);go.AddComponent<SJGlobalDisplay>();}
    void Awake(){var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;var go=new GameObject("Brightness",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);var r=go.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;shade=go.GetComponent<Image>();shade.raycastTarget=false;}
    void Update(){shade.color=new Color(0,0,0,Mathf.Clamp01(1-SJProfile.Data.settings.brightness)*.7f);}
}
