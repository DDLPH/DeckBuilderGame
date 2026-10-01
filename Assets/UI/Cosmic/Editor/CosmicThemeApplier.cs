using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CosmicThemeApplier
{
    private const string BasePath = "Assets/UI/Cosmic";
    private const string BackgroundPath = BasePath + "/Art/CosmicMenuBackground.png";
    private const string LatinPath = BasePath + "/Fonts/Cinzel SDF.asset";
    private const string ThaiPath = BasePath + "/Fonts/NotoSerifThai SDF.asset";

    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    private static readonly Color Ivory = ColorOf("#D8D1C0");
    private static readonly Color MutedIvory = ColorOf("#AFA4B7");
    private static readonly Color Border = ColorOf("#75558E");
    private static readonly Color ButtonFill = ColorOf("#100F22E8");
    private static readonly Color PanelFill = ColorOf("#100F22E8");

    [MenuItem("Tools/Cosmic UI/Apply Theme to Main Menu")]
    public static void ApplyMainMenu()
    {
        AssetDatabase.Refresh();
        Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        if (background == null)
        {
            throw new InvalidOperationException("Missing CosmicMenuBackground.png");
        }

        TMP_FontAsset latin = EnsureFont("Cinzel.ttf", LatinPath);
        TMP_FontAsset thai = EnsureFont("NotoSerifThai.ttf", ThaiPath);
        if (latin.fallbackFontAssetTable == null)
        {
            latin.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }
        if (!latin.fallbackFontAssetTable.Contains(thai))
        {
            latin.fallbackFontAssetTable.Add(thai);
            EditorUtility.SetDirty(latin);
        }

        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
        {
            throw new InvalidOperationException("MainMenu has no Canvas.");
        }
        EnsureBackdrop(canvas, background, 1f);
        StyleExistingScene(canvas, latin);
        BuildMainMenu(canvas, latin, thai);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cosmic UI theme applied to MainMenu.");
    }

    private static Color ColorOf(string html)
    {
        ColorUtility.TryParseHtmlString(html, out Color color);
        return color;
    }

    private static TMP_FontAsset EnsureFont(string sourceName, string outputPath)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
        if (existing != null && existing.material != null && existing.atlasTexture != null) return existing;
        if (existing != null) AssetDatabase.DeleteAsset(outputPath);

        Font source = AssetDatabase.LoadAssetAtPath<Font>(BasePath + "/Fonts/" + sourceName);
        if (source == null) throw new InvalidOperationException("Missing font: " + sourceName);

        TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source);
        if (created == null) throw new InvalidOperationException("Could not create font asset: " + sourceName);
        Material material = created.material;
        Texture2D atlas = created.atlasTexture;
        if (material == null || atlas == null)
        {
            throw new InvalidOperationException("Font asset has no material or atlas: " + sourceName);
        }
        created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        AssetDatabase.CreateAsset(created, outputPath);
        AssetDatabase.AddObjectToAsset(material, created);
        AssetDatabase.AddObjectToAsset(atlas, created);
        created.material = material;
        created.atlasTextures = new[] { atlas };
        EditorUtility.SetDirty(created);
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        return created;
    }

    private static Canvas FindCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null) return canvas;
        }
        return null;
    }

    private static void EnsureBackdrop(Canvas canvas, Texture2D texture, float opacity)
    {
        Transform existing = canvas.transform.Find("CosmicBackdrop");
        GameObject go = existing != null ? existing.gameObject :
            new GameObject("CosmicBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();
        RectTransform rect = (RectTransform)go.transform;
        Stretch(rect);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = new Color(1f, 1f, 1f, opacity);
        image.raycastTarget = false;
    }

    private static void StyleExistingScene(Canvas canvas, TMP_FontAsset latin)
    {
        foreach (TMP_Text label in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            label.color = Ivory;
            label.font = latin;
            label.raycastTarget = false;
        }

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            StyleButton(button, latin);
        }

        foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponent<Button>() != null) continue;
            string name = image.gameObject.name;
            if (name.Contains("Panel") || name == "Viewport")
            {
                image.color = PanelFill;
            }
        }
    }

    private static void BuildMainMenu(Canvas canvas, TMP_FontAsset latin, TMP_FontAsset thai)
    {
        Transform titleTransform = canvas.transform.Find("GameTitle");
        Transform startTransform = canvas.transform.Find("StartButton");
        if (titleTransform == null || startTransform == null)
        {
            throw new InvalidOperationException("MainMenu is missing GameTitle or StartButton.");
        }

        TMP_Text title = titleTransform.GetComponent<TMP_Text>();
        title.text = "THE\nHOLLOW\nBEYOND";
        title.font = latin;
        title.fontSize = 106;
        title.color = Ivory;
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 5;
        title.lineSpacing = -8;
        title.enableWordWrapping = false;
        SetRect((RectTransform)titleTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(82f, -76f), new Vector2(500f, 310f));

        EnsureLine(canvas.transform, "TitleRuleTop", new Vector2(92f, -65f), 480f);
        EnsureLine(canvas.transform, "TitleRuleBottom", new Vector2(92f, -402f), 480f);

        Button start = startTransform.GetComponent<Button>();
        ConfigureMenuButton(start, "NEW RUN", "เริ่มรอบใหม่", -570f, latin, thai);
        GameObject managerObject = GameObject.Find("MainMenuManager");
        if (managerObject == null)
        {
            throw new InvalidOperationException("MainMenu is missing MainMenuManager.");
        }
        MainMenuManager manager = managerObject.GetComponent<MainMenuManager>();
        if (manager == null) manager = managerObject.AddComponent<MainMenuManager>();
        for (int i = start.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(start.onClick, i);
        }
        UnityEventTools.AddPersistentListener(start.onClick, manager.StartGame);
        Button continueButton = EnsureMenuButton(canvas.transform, "ContinueButton", "CONTINUE", "เล่นต่อ", -445f, latin, thai);
        continueButton.interactable = false;
        TMP_Text continueLabel = continueButton.GetComponentInChildren<TMP_Text>();
        continueLabel.color = MutedIvory;

        Button settingsButton = EnsureMenuButton(canvas.transform, "SettingsButton", "SETTINGS", "ตั้งค่า", -695f, latin, thai);
        Button quitButton = EnsureMenuButton(canvas.transform, "QuitButton", "QUIT", "ออกจากเกม", -820f, latin, thai);
        Button languageButton = BuildLanguageButton(canvas, latin);
        TMP_Text version = EnsureText(canvas.transform, "VersionLabel", "v0.1", latin, 30, MutedIvory);
        SetRect((RectTransform)version.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-50f, 25f), new Vector2(150f, 45f));
        version.alignment = TextAlignmentOptions.Right;

        GameObject panel = BuildSettingsPanel(canvas, latin, thai, out Button settingsLanguage, out Button close, out Slider volume);
        CosmicMenuActions actions = canvas.GetComponent<CosmicMenuActions>();
        if (actions == null) actions = canvas.gameObject.AddComponent<CosmicMenuActions>();
        actions.settingsPanel = panel;
        actions.volumeSlider = volume;
        actions.settingsButton = settingsButton;
        actions.quitButton = quitButton;
        actions.languageButton = languageButton;
        actions.settingsLanguageButton = settingsLanguage;
        actions.closeSettingsButton = close;
        panel.SetActive(false);
    }

    private static GameObject BuildSettingsPanel(Canvas canvas, TMP_FontAsset latin, TMP_FontAsset thai,
        out Button language, out Button close, out Slider volume)
    {
        Transform existing = canvas.transform.Find("CosmicSettingsPanel");
        GameObject panel = existing != null ? existing.gameObject :
            new GameObject("CosmicSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();
        SetRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 650f));
        Image image = panel.GetComponent<Image>();
        image.color = ColorOf("#0C0B1CF5");
        AddOutline(image, Border);

        TMP_Text heading = EnsureText(panel.transform, "SettingsTitle", "SETTINGS", latin, 76, Ivory);
        AddLocalization(heading, "SETTINGS", "ตั้งค่า", latin, thai);
        SetRect((RectTransform)heading.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(700f, 100f));
        heading.alignment = TextAlignmentOptions.Center;

        TMP_Text volumeLabel = EnsureText(panel.transform, "VolumeLabel", "VOLUME", latin, 42, Ivory);
        AddLocalization(volumeLabel, "VOLUME", "ระดับเสียง", latin, thai);
        SetRect((RectTransform)volumeLabel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -194f), new Vector2(650f, 70f));
        volumeLabel.alignment = TextAlignmentOptions.Center;

        volume = EnsureVolumeSlider(panel.transform);
        language = EnsureMenuButton(panel.transform, "SettingsLanguageButton", "TH / EN", "TH / EN", -390f, latin, thai);
        SetRect((RectTransform)language.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(470f, 84f));
        close = EnsureMenuButton(panel.transform, "CloseSettingsButton", "BACK", "กลับ", -500f, latin, thai);
        SetRect((RectTransform)close.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(470f, 84f));
        return panel;
    }

    private static Slider EnsureVolumeSlider(Transform parent)
    {
        Transform existing = parent.Find("VolumeSlider");
        GameObject go = existing != null ? existing.gameObject :
            new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(520f, 48f));

        Image track = EnsureImage(go.transform, "Track", ColorOf("#392C50"));
        Stretch((RectTransform)track.transform);
        Image fill = EnsureImage(go.transform, "Fill", Border);
        Stretch((RectTransform)fill.transform);
        Image handle = EnsureImage(go.transform, "Handle", Ivory);
        SetRect((RectTransform)handle.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 62f));

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = (RectTransform)fill.transform;
        slider.handleRect = (RectTransform)handle.transform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static Button BuildLanguageButton(Canvas canvas, TMP_FontAsset latin)
    {
        Transform existing = canvas.transform.Find("CosmicLanguageButton");
        Button button = existing != null ? existing.GetComponent<Button>() :
            CreateButton(canvas.transform, "CosmicLanguageButton", "TH / EN", latin);
        SetRect((RectTransform)button.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-48f, -30f), new Vector2(180f, 62f));
        button.GetComponent<Image>().color = ColorOf("#100F2200");
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.fontSize = 31;
        label.color = MutedIvory;
        label.alignment = TextAlignmentOptions.Center;
        CosmicMenuActions actions = canvas.GetComponent<CosmicMenuActions>();
        if (actions == null) actions = canvas.gameObject.AddComponent<CosmicMenuActions>();
        actions.languageButton = button;
        return button;
    }

    private static void StyleButton(Button button, TMP_FontAsset latin)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = ButtonFill;
            AddOutline(image, Border);
            button.targetGraphic = image;
        }
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = ColorOf("#BFA6D5");
        colors.pressedColor = ColorOf("#9273AA");
        colors.disabledColor = ColorOf("#777080AA");
        colors.colorMultiplier = 1f;
        button.colors = colors;
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.font = latin;
            label.color = Ivory;
            label.fontSize = Mathf.Max(30f, label.fontSize);
            label.alignment = TextAlignmentOptions.Center;
        }
    }

    private static Button EnsureMenuButton(Transform parent, string name, string english, string thai,
        float y, TMP_FontAsset latin, TMP_FontAsset thaiFont)
    {
        Transform existing = parent.Find(name);
        Button button = existing != null ? existing.GetComponent<Button>() : CreateButton(parent, name, english, latin);
        ConfigureMenuButton(button, english, thai, y, latin, thaiFont);
        return button;
    }

    private static void ConfigureMenuButton(Button button, string english, string thai,
        float y, TMP_FontAsset latin, TMP_FontAsset thaiFont)
    {
        StyleButton(button, latin);
        SetRect((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(90f, y), new Vector2(500f, 98f));
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        label.fontSize = 48;
        label.text = english;
        label.characterSpacing = 6;
        AddLocalization(label, english, thai, latin, thaiFont);
        Stretch((RectTransform)label.transform);
    }

    private static Button CreateButton(Transform parent, string name, string text, TMP_FontAsset latin)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Button button = go.GetComponent<Button>();
        StyleButton(button, latin);
        TMP_Text label = EnsureText(go.transform, "Label", text, latin, 42, Ivory);
        Stretch((RectTransform)label.transform);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static TMP_Text EnsureText(Transform parent, string name, string value,
        TMP_FontAsset font, float size, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject :
            new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = value;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Image EnsureImage(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject :
            new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void EnsureLine(Transform parent, string name, Vector2 topLeft, float width)
    {
        Image line = EnsureImage(parent, name, Border);
        SetRect((RectTransform)line.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), topLeft, new Vector2(width, 3f));
    }

    private static void AddLocalization(TMP_Text label, string english, string thai,
        TMP_FontAsset latin, TMP_FontAsset thaiFont)
    {
        CosmicLocalizedText localized = label.GetComponent<CosmicLocalizedText>();
        if (localized == null) localized = label.gameObject.AddComponent<CosmicLocalizedText>();
        localized.english = english;
        localized.thai = thai;
        localized.englishFont = latin;
        localized.thaiFont = thaiFont;
        label.text = english;
    }

    private static void AddOutline(Image image, Color color)
    {
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

}
