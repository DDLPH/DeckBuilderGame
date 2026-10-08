using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicUnifiedThemeBuilder
{
    [MenuItem("Tools/Cosmic UI/Apply Unified Map Theme")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Transform canvas = GameObject.Find("Canvas").transform;
        TMP_FontAsset english=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        TMP_FontAsset thai=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        canvas.Find("CosmicBackdrop").GetComponent<RawImage>().color=new Color(.70f,.66f,.88f,1);
        Transform shade=canvas.Find("MenuReadabilityShade");
        if(shade==null) shade=new GameObject("MenuReadabilityShade",typeof(RectTransform),typeof(CanvasRenderer),typeof(CosmicSidebarShade)).transform;
        shade.SetParent(canvas,false);Stretch((RectTransform)shade);
        ((RectTransform)shade).anchorMax=new Vector2(.52f,1);
        shade.GetComponent<CosmicSidebarShade>().color=new Color(.003f,.002f,.012f,.92f);
        shade.GetComponent<CosmicSidebarShade>().raycastTarget=false;shade.SetSiblingIndex(1);

        TMP_Text title=canvas.Find("GameTitle").GetComponent<TMP_Text>();
        title.color=CosmicUITheme.Ivory;title.fontSize=95;title.characterSpacing=3.5f;title.lineSpacing=-6;
        Place(title.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(95,-100),new Vector2(480,300));
        foreach(string rule in new[]{"TitleRuleTop","TitleRuleBottom"})
            canvas.Find(rule).GetComponent<Graphic>().color=CosmicUITheme.Border;
        ((RectTransform)canvas.Find("TitleRuleTop")).sizeDelta=new Vector2(480,1.2f);
        ((RectTransform)canvas.Find("TitleRuleBottom")).sizeDelta=new Vector2(480,1.2f);
        string[] names={"ContinueButton","StartButton","SettingsButton","QuitButton"};
        for(int i=0;i<names.Length;i++)
        {
            Button button=canvas.Find(names[i]).GetComponent<Button>();
            Place((RectTransform)button.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(95,-460-i*106),new Vector2(480,82));
            Style(button,34,i==1);
        }
        Style(canvas.Find("CosmicLanguageButton").GetComponent<Button>(),25,false);
        Place((RectTransform)canvas.Find("CosmicLanguageButton"),Vector2.one,Vector2.one,new Vector2(-48,-35),new Vector2(170,54));
        canvas.Find("VersionLabel").GetComponent<TMP_Text>().color=CosmicUITheme.Muted;
        canvas.Find("VersionLabel").GetComponent<TMP_Text>().fontSize=22;

        Transform panel=canvas.Find("CosmicSettingsPanel");
        CosmicUITheme.StylePanel(panel);
        Place((RectTransform)panel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(740,570));
        TMP_Text heading=panel.Find("SettingsTitle").GetComponent<TMP_Text>();heading.fontSize=46;heading.color=CosmicUITheme.Ivory;
        Place(heading.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-38),new Vector2(650,70));
        Transform divider=panel.Find("SettingsDivider");
        if(divider==null) divider=new GameObject("SettingsDivider",typeof(RectTransform),typeof(CanvasRenderer),typeof(AstralMapGraphic)).transform;
        divider.SetParent(panel,false);Place((RectTransform)divider,new Vector2(.5f,1),new Vector2(.5f,.5f),new Vector2(0,-122),new Vector2(560,20));
        var line=divider.GetComponent<AstralMapGraphic>();line.motif=AstralMapGraphic.Motif.Divider;line.color=CosmicUITheme.Border;line.raycastTarget=false;
        TMP_Text volumeLabel=panel.Find("VolumeLabel").GetComponent<TMP_Text>();volumeLabel.fontSize=27;volumeLabel.alignment=TextAlignmentOptions.Left;volumeLabel.color=CosmicUITheme.Ivory;
        Place(volumeLabel.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(95,-169),new Vector2(340,48));
        Slider slider=panel.Find("VolumeSlider").GetComponent<Slider>();
        Place((RectTransform)slider.transform,new Vector2(.5f,1),new Vector2(.5f,.5f),new Vector2(0,-246),new Vector2(550,40));
        foreach(string part in new[]{"Track"})
        {
            RectTransform rect=(RectTransform)slider.transform.Find(part);Stretch(rect);
            rect.anchorMin=new Vector2(0,.5f);rect.anchorMax=new Vector2(1,.5f);rect.sizeDelta=new Vector2(0,8);
            rect.GetComponent<Image>().color=part=="Fill"?CosmicUITheme.Border:new Color(.075f,.045f,.11f);
            rect.GetComponent<Image>().raycastTarget=false;
        }
        RectTransform fill=slider.fillRect;
        RectTransform fillArea=EnsureRect(slider.transform,"FillArea");
        Stretch(fillArea);fillArea.anchorMin=new Vector2(0,.5f);fillArea.anchorMax=new Vector2(1,.5f);fillArea.sizeDelta=new Vector2(0,8);
        fill.SetParent(fillArea,false);Stretch(fill);fill.GetComponent<Image>().color=CosmicUITheme.Border;fill.GetComponent<Image>().raycastTarget=false;
        slider.fillRect=null;slider.fillRect=fill;
        RectTransform handle=slider.handleRect;
        RectTransform handleArea=EnsureRect(slider.transform,"HandleArea");
        Stretch(handleArea);handleArea.anchorMin=new Vector2(0,.5f);handleArea.anchorMax=new Vector2(1,.5f);handleArea.sizeDelta=Vector2.zero;
        handle.SetParent(handleArea,false);handle.sizeDelta=new Vector2(12,28);
        slider.handleRect=null;slider.handleRect=handle;
        handle.GetComponent<Image>().color=CosmicUITheme.Ivory;
        TMP_Text percentage=EnsureText(panel,"VolumePercentage",english,"100%",26);
        percentage.alignment=TextAlignmentOptions.Right;
        Place(percentage.rectTransform,new Vector2(1,1),new Vector2(1,1),new Vector2(-95,-169),new Vector2(140,48));
        TMP_Text language=EnsureText(panel,"LanguageCaption",english,"LANGUAGE",26);
        var localized=language.GetComponent<CosmicLocalizedText>();if(localized==null)localized=language.gameObject.AddComponent<CosmicLocalizedText>();
        localized.english="LANGUAGE";localized.thai="ภาษา";localized.englishFont=english;localized.thaiFont=thai;localized.Refresh();
        language.alignment=TextAlignmentOptions.Left;
        Place(language.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(95,-330),new Vector2(300,58));
        Button languageButton=panel.Find("SettingsLanguageButton").GetComponent<Button>();Style(languageButton,25,false);
        Place((RectTransform)languageButton.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-95,-330),new Vector2(210,58));
        Button back=panel.Find("CloseSettingsButton").GetComponent<Button>();Style(back,26,false);
        Place((RectTransform)back.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-451),new Vector2(300,64));
        Transform scrim=canvas.Find("SettingsScrim");
        if(scrim==null)scrim=new GameObject("SettingsScrim",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).transform;
        scrim.SetParent(canvas,false);Stretch((RectTransform)scrim);scrim.GetComponent<Image>().color=CosmicUITheme.Scrim;scrim.gameObject.SetActive(false);
        CosmicSettingsPresentation presentation=panel.GetComponent<CosmicSettingsPresentation>();
        if(presentation==null)presentation=panel.gameObject.AddComponent<CosmicSettingsPresentation>();
        presentation.scrim=scrim.gameObject;presentation.volume=slider;presentation.percentage=percentage;
        panel.gameObject.SetActive(false);panel.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);

        scene=EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity");
        Transform chrome=GameObject.Find("MapCanvas").transform.Find("ReferenceMapChrome");
        foreach(string name in new[]{"StatusPanel","LegendPanel"})
        {
            Transform item=chrome.Find(name);item.GetComponent<Image>().color=new Color(.006f,.004f,.018f,.84f);
            item.Find("OrnateFrame").GetComponent<AstralMapGraphic>().color=CosmicUITheme.Border;
        }
        foreach(TMP_Text text in chrome.GetComponentsInChildren<TMP_Text>(true))text.color=CosmicUITheme.Ivory;
        Button mapBack=chrome.Find("BackButton").GetComponent<Button>();Style(mapBack,28,false);
        chrome.Find("BackButton/OrnateFrame").gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Unified map-based UI saved: main menu, settings, map chrome and shared button feedback.");
    }
    private static void Style(Button button,float size,bool primary)
    {
        CosmicUITheme.StyleButton(button);
        CosmicButtonEffect effect=button.GetComponent<CosmicButtonEffect>();if(effect==null)effect=button.gameObject.AddComponent<CosmicButtonEffect>();
        effect.primaryAction=primary;
        if(primary)
        {
            button.GetComponent<Image>().color=new Color(.043f,.027f,.078f,.97f);
            CosmicUITheme.Frame(button.transform,true).color=CosmicUITheme.Border*1.2f;
        }
        foreach(TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=size;text.characterSpacing=2;text.enableAutoSizing=true;text.fontSizeMin=21;text.fontSizeMax=size;}
    }
    private static RectTransform EnsureRect(Transform parent,string name)
    {
        Transform item=parent.Find(name);
        if(item==null){item=new GameObject(name,typeof(RectTransform)).transform;item.gameObject.layer=5;item.SetParent(parent,false);}
        return (RectTransform)item;
    }
    public static void Preview()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Canvas canvas=GameObject.Find("Canvas").GetComponent<Canvas>();
        CosmicAuditChecks.Render(canvas,"unified-main-menu.png");
        canvas.transform.Find("SettingsScrim").gameObject.SetActive(true);
        canvas.transform.Find("SettingsScrim").SetAsLastSibling();
        Transform settings=canvas.transform.Find("CosmicSettingsPanel");settings.gameObject.SetActive(true);settings.SetAsLastSibling();
        CosmicAuditChecks.Render(canvas,"unified-settings.png");
        Slider volume=settings.Find("VolumeSlider").GetComponent<Slider>();
        volume.value=.35f;Canvas.ForceUpdateCanvases();
        if(Mathf.Abs(volume.fillRect.rect.height-8)>1 || Mathf.Abs(volume.handleRect.rect.height-28)>1)
            throw new InvalidOperationException("Slider visuals are no longer fixed-height.");
        foreach(Button button in canvas.GetComponentsInChildren<Button>(true))
            if(button.GetComponent<CosmicButtonEffect>()==null || button.transform.Find("UnifiedButtonFrame")==null)
                throw new InvalidOperationException("A menu button is missing the unified feedback/frame: "+button.name);
        CosmicAuditChecks.Run();
        CosmicMapRenderCheck.Render();
        MapEventOverlay.Show(UnityEngine.Object.FindAnyObjectByType<RunManager>());
        CosmicAuditChecks.Render(GameObject.Find("MapCanvas").GetComponent<Canvas>(),"unified-event.png");
        Debug.Log("Unified UI previews and regression checks completed.");
    }
    private static TMP_Text EnsureText(Transform parent,string name,TMP_FontAsset font,string value,float size)
    {
        Transform item=parent.Find(name);
        if(item==null){item=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).transform;item.SetParent(parent,false);}
        TMP_Text text=item.GetComponent<TMP_Text>();text.font=font;text.text=value;text.fontSize=size;text.color=CosmicUITheme.Ivory;text.raycastTarget=false;return text;
    }
    private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    private static void Place(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;}
}
