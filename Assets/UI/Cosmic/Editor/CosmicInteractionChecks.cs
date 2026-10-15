using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CosmicInteractionChecks
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Canvas canvas=GameObject.Find("Canvas").GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        CosmicMenuActions actions=canvas.GetComponent<CosmicMenuActions>();
        actions.settingsPanel.SetActive(true);
        actions.settingsPanel.transform.SetAsLastSibling();
        Slider slider=actions.volumeSlider;
        // Batch edit mode has no normal Game-view frame. Render once so UI
        // meshes acquire their draw depth before GraphicRaycaster runs.
        CosmicAuditChecks.Render(canvas,"interaction-preflight.png",640,480);
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        Canvas.ForceUpdateCanvases();
        Image hitArea=slider.GetComponent<Image>();
        Require(hitArea!=null && hitArea.raycastTarget,"Slider has no whole-track hit area");
        float oldAudio=AudioListener.volume;
        bool hadPreference=PlayerPrefs.HasKey("CosmicUI.Volume");
        float oldPreference=PlayerPrefs.GetFloat("CosmicUI.Volume",1);
        slider.onValueChanged.AddListener(actions.SetVolume);
        try
        {
            var pointer=new PointerEventData(UnityEngine.Object.FindAnyObjectByType<EventSystem>());
            pointer.button=PointerEventData.InputButton.Left;
            RectTransform area=(RectTransform)slider.handleRect.parent;
            pointer.position=ScreenPoint(area,.8f);
            var hits=new List<RaycastResult>();
            canvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            Debug.Log("Slider raycast diagnostic: screen="+Screen.width+"x"+Screen.height+" point="+pointer.position+
                " canvas="+canvas.pixelRect+" depth="+hitArea.depth+" culled="+hitArea.canvasRenderer.cull+
                " contains="+RectTransformUtility.RectangleContainsScreenPoint(hitArea.rectTransform,pointer.position,null)+
                " hits="+string.Join(",",hits.ConvertAll(hit=>hit.gameObject.name)));
            Require(hits.Exists(hit=>hit.gameObject==slider.gameObject),"Track click does not raycast to Slider");
            slider.value=.05f;
            slider.OnPointerDown(pointer);
            Require(Mathf.Abs(slider.value-.8f)<.015f,"Track click failed to raise volume");
            Require(Mathf.Abs(AudioListener.volume-.8f)<.015f,"Master audio did not follow the track click");
            pointer.position=ScreenPoint(area,.2f);
            slider.OnDrag(pointer);
            Require(Mathf.Abs(slider.value-.2f)<.015f,"Drag failed to lower volume");
            Require(Mathf.Abs(AudioListener.volume-.2f)<.015f,"Master audio did not follow dragging");
            slider.OnPointerUp(pointer);
            Debug.Log("Pointer/audio checks passed: whole-track raycast, click 5% -> 80%, drag -> 20%, AudioListener follows both.");
        }
        finally
        {
            slider.onValueChanged.RemoveListener(actions.SetVolume);
            AudioListener.volume=oldAudio;
            if(hadPreference)PlayerPrefs.SetFloat("CosmicUI.Volume",oldPreference);
            else PlayerPrefs.DeleteKey("CosmicUI.Volume");
            PlayerPrefs.Save();
        }
        actions.settingsPanel.GetComponent<CosmicSettingsPresentation>().percentage.text=Mathf.RoundToInt(slider.value*100)+"%";
        CosmicAuditChecks.Render(canvas,"fixed-settings.png");
        CosmicAuditChecks.Render(canvas,"fixed-settings-small.png",960,540);
        actions.settingsPanel.SetActive(false);
        CosmicAuditChecks.Render(canvas,"fixed-main-menu.png");
        CosmicAuditChecks.Render(canvas,"fixed-main-menu-small.png",960,540);
        CosmicAuditChecks.Run();
    }
    private static Vector2 ScreenPoint(RectTransform rect,float position)
    { return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+rect.rect.width*position,0,0))); }
    private static void Require(bool value,string message)
    { if(!value)throw new InvalidOperationException(message); }
}
