using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicMapReferenceBuilder
{
    private static readonly Color Ivory = new Color(.92f, .82f, .72f, 1f);
    private static readonly Color Violet = new Color(.33f, .18f, .46f, .95f);
    private static TMP_FontAsset english, thai;

    [MenuItem("Tools/Cosmic UI/Apply Illustrated Map")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity", OpenSceneMode.Single);
        GameObject canvasObject = GameObject.Find("MapCanvas");
        if (canvasObject == null) throw new InvalidOperationException("MapCanvas is missing.");
        Transform canvas = canvasObject.transform;
        english = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        thai = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        if (english == null || thai == null) throw new InvalidOperationException("Map fonts are missing.");

        // Keep the earlier draft in the scene as an inactive, recoverable layout.
        Transform old = canvas.Find("MapChrome");
        if (old != null) old.gameObject.SetActive(false);
        Transform previous = canvas.Find("ReferenceMapChrome");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        RectTransform chrome = ObjectRect("ReferenceMapChrome", canvas);
        Stretch(chrome);

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        RawImage background = canvas.Find("MapBackground")?.GetComponent<RawImage>();
        Texture2D landscape = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Cosmic/Art/CosmicMapLandscape.png");
        if (background == null || landscape == null) throw new InvalidOperationException("Map landscape is missing.");
        background.texture = landscape;
        background.color = new Color(.46f,.42f,.58f,1f);
        background.raycastTarget = false;
        Stretch(background.rectTransform);
        background.transform.SetAsFirstSibling();

        TMP_Text title = Text("MapTitle", chrome, "CHOOSE YOUR PATH", 58f,
            new Vector2(.5f,1f), new Vector2(0,-12), new Vector2(1330,82));
        title.characterSpacing = 7f;
        Art("TitleRule", chrome, AstralMapGraphic.Motif.Divider, Violet,
            new Vector2(.5f,1f), new Vector2(0,-98), new Vector2(880,28));

        RectTransform status = Panel("StatusPanel",chrome,new Vector2(0,1),new Vector2(27,-145),new Vector2(310,450));
        Text("FloorCaption",status,"FLOOR",32,new Vector2(0,1),new Vector2(38,-56),new Vector2(175,55),TextAlignmentOptions.Left);
        TMP_Text floor = Text("FloorValue",status,"1",51,new Vector2(0,1),new Vector2(222,-47),new Vector2(56,62));
        Art("StatusDivider",status,AstralMapGraphic.Motif.Divider,Violet,new Vector2(.5f,1),new Vector2(0,-125),new Vector2(238,22));
        Art("Heart",status,AstralMapGraphic.Motif.Heart,Ivory,new Vector2(0,1),new Vector2(54,-185),new Vector2(33,33));
        Text("HpCaption",status,"HP",26,new Vector2(0,1),new Vector2(92,-164),new Vector2(164,44),TextAlignmentOptions.Left);

        RectTransform track = ObjectRect("HpTrack",status);
        Place(track,new Vector2(.5f,1),new Vector2(0,-224),new Vector2(234,21));
        Image trackImage = track.gameObject.AddComponent<Image>();
        trackImage.color = new Color(.015f,.005f,.027f,1f); trackImage.raycastTarget=false;
        Art("HpOutline",track,AstralMapGraphic.Motif.Frame,Violet,new Vector2(.5f,.5f),Vector2.zero,new Vector2(245,28));
        RectTransform fill = ObjectRect("HpFill",track); Stretch(fill);
        fill.offsetMin = new Vector2(3,3); fill.offsetMax = new Vector2(-3,-3);
        Image fillImage=fill.gameObject.AddComponent<Image>();
        fillImage.type=Image.Type.Simple;
        fillImage.color=new Color(.36f,.16f,.49f);fillImage.raycastTarget=false;
        Text("HpValue",status,"80 / 80",26,new Vector2(0,1),new Vector2(148,-248),new Vector2(127,40),TextAlignmentOptions.Right);

        Art("Coin",status,AstralMapGraphic.Motif.Coin,new Color(.70f,.45f,.18f),new Vector2(0,1),new Vector2(55,-330),new Vector2(40,40));
        Text("GoldCaption",status,"GOLD",27,new Vector2(0,1),new Vector2(92,-304),new Vector2(160,45),TextAlignmentOptions.Left);
        Text("GoldValue",status,"100",27,new Vector2(0,1),new Vector2(175,-354),new Vector2(99,42),TextAlignmentOptions.Right);
        TMP_Text hidden = Text("RunStatus",status,"",1,new Vector2(.5f,.5f),Vector2.zero,Vector2.one);
        hidden.gameObject.SetActive(false);

        RectTransform legend = Panel("LegendPanel",chrome,Vector2.one,new Vector2(-27,-145),new Vector2(310,770));
        Text("LegendTitle",legend,"LEGEND",33,new Vector2(.5f,1),new Vector2(0,-28),new Vector2(254,57));
        Art("LegendRule",legend,AstralMapGraphic.Motif.Divider,Violet,new Vector2(.5f,1),new Vector2(0,-91),new Vector2(240,20));
        MapNodeType[] types={MapNodeType.NormalBattle,MapNodeType.Elite,MapNodeType.Event,MapNodeType.Rest,MapNodeType.Shop,MapNodeType.Boss};
        string[] names={"BATTLE","ELITE","EVENT","REST","SHOP","BOSS"};
        TMP_Text[] labels=new TMP_Text[6];
        for(int i=0;i<types.Length;i++)
        {
            float y=-162-i*99;
            if(types[i]!=MapNodeType.Boss)
                Art("Ring"+names[i],legend,AstralMapGraphic.Motif.NodeRing,new Color(.44f,.23f,.60f),new Vector2(0,1),new Vector2(91,y),new Vector2(87,87));
            float size=types[i]==MapNodeType.Boss?88:40;
            Art("Icon"+names[i],legend,CosmicMapNodeArt.MotifFor(types[i]),Ivory,new Vector2(0,1),new Vector2(91,y),new Vector2(size,size));
            labels[i]=Text("Legend"+names[i],legend,names[i],24,new Vector2(0,1),new Vector2(154,y+26),new Vector2(145,50),TextAlignmentOptions.Left);
            labels[i].characterSpacing=3f;
        }

        RectTransform back = Panel("BackButton",chrome,Vector2.zero,new Vector2(31,42),new Vector2(270,86));
        Image backImage=back.GetComponent<Image>();backImage.raycastTarget=true;
        Button backButton=back.gameObject.AddComponent<Button>();backButton.targetGraphic=backImage;
        ColorBlock colors=backButton.colors;colors.highlightedColor=new Color(.65f,.42f,.80f);
        colors.pressedColor=new Color(.42f,.26f,.55f);backButton.colors=colors;
        Art("BackArrow",back,AstralMapGraphic.Motif.Chevron,Ivory,new Vector2(0,.5f),new Vector2(37,0),new Vector2(23,23));
        TMP_Text backLabel=Text("BackLabel",back,"BACK",28,new Vector2(.5f,.5f),new Vector2(18,0),new Vector2(173,58));
        backLabel.characterSpacing=5f;

        MapHudUI hud=chrome.gameObject.AddComponent<MapHudUI>();
        SerializedObject serialized=new SerializedObject(hud);
        serialized.FindProperty("titleText").objectReferenceValue=title;
        serialized.FindProperty("statusText").objectReferenceValue=hidden;
        serialized.FindProperty("englishFont").objectReferenceValue=english;
        serialized.FindProperty("thaiFont").objectReferenceValue=thai;
        SerializedProperty entries=serialized.FindProperty("legendTexts");entries.arraySize=6;
        for(int i=0;i<6;i++)entries.GetArrayElementAtIndex(i).objectReferenceValue=labels[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        chrome.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Illustrated cosmic Map applied and saved.");
    }

    private static RectTransform ObjectRect(string name,Transform parent)
    {
        GameObject item=new GameObject(name,typeof(RectTransform));item.layer=5;
        item.transform.SetParent(parent,false);return item.GetComponent<RectTransform>();
    }
    private static RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
    {
        RectTransform rect=ObjectRect(name,parent);Place(rect,anchor,position,size);
        rect.pivot=anchor; // Edge-anchored panels extend inward from the screen edge.
        Image fill=rect.gameObject.AddComponent<Image>();fill.color=new Color(0f,0f,0f,.76f);fill.raycastTarget=false;
        Art("OrnateFrame",rect,AstralMapGraphic.Motif.Frame,Violet,new Vector2(.5f,.5f),Vector2.zero,size+new Vector2(5,5));
        return rect;
    }
    private static AstralMapGraphic Art(string name,Transform parent,AstralMapGraphic.Motif motif,Color tint,Vector2 anchor,Vector2 position,Vector2 size)
    {
        RectTransform rect=ObjectRect(name,parent);Place(rect,anchor,position,size);
        rect.gameObject.AddComponent<CanvasRenderer>();
        AstralMapGraphic art=rect.gameObject.AddComponent<AstralMapGraphic>();
        art.motif=motif;art.color=tint;art.raycastTarget=false;return art;
    }
    private static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 anchor,Vector2 position,Vector2 bounds,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        RectTransform rect=ObjectRect(name,parent);Place(rect,anchor,position,bounds);
        if(anchor.y==1)rect.pivot=new Vector2(anchor.x==0?0:.5f,1f);
        TextMeshProUGUI text=rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=english;text.text=value;text.fontSize=size;text.color=Ivory;text.alignment=align;
        text.raycastTarget=false;text.overflowMode=TextOverflowModes.Overflow;
        return text;
    }
    private static void Place(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;rect.sizeDelta=size;
    }
    private static void Stretch(RectTransform rect)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
