using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CosmicMapSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MapScene.unity";

    [MenuItem("Tools/Cosmic UI/Build Reference-Style Map")]
    public static void BuildReferenceStyle()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("MapCanvas")?.transform;
        Transform chrome = canvas?.Find("MapChrome");
        if (chrome == null) throw new InvalidOperationException("MapChrome is missing.");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        if (font == null) throw new InvalidOperationException("Map font is missing.");

        RectTransform header = chrome.Find("HeaderPanel") as RectTransform;
        RectTransform status = chrome.Find("StatusPanel") as RectTransform;
        RectTransform footer = chrome.Find("FooterPanel") as RectTransform;
        if (header == null || status == null || footer == null)
            throw new InvalidOperationException("Build and polish the map screen first.");

        // The title sits over the eclipse, with clear space for the route.
        Anchor(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -13f), new Vector2(1030f, 100f));
        header.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        Transform title = header.Find("MapTitle");
        Anchor((RectTransform)title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1000f, 76f));
        title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TMP_Text>().fontSize = 58f;
        header.Find("MapSubtitle").gameObject.SetActive(false);
        header.Find("HeaderRule").gameObject.SetActive(false);

        // Left-hand player card. The old single-line status stays wired, but
        // the visible values are separate so the HP bar can move independently.
        Anchor(status, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(28f, -140f), new Vector2(265f, 575f));
        StylePanel(status, new Color32(8, 7, 23, 224));
        status.Find("RunStatus").gameObject.SetActive(false);
        status.Find("StatusCaption").gameObject.SetActive(false);
        MakeLabel(status, font, "FloorCaption", "FLOOR", 32f, -35f, 220f);
        MakeLabel(status, font, "FloorValue", "1", 55f, -95f, 220f);
        MakeLabel(status, font, "HpCaption", "HP", 27f, -220f, 220f);
        MakeLabel(status, font, "HpValue", "80 / 80", 25f, -311f, 220f);
        MakeLabel(status, font, "GoldCaption", "GOLD", 27f, -400f, 220f);
        MakeLabel(status, font, "GoldValue", "100", 27f, -463f, 220f);

        RectTransform hpTrack = CreatePanel("HpTrack", status, new Color32(31, 21, 49, 255));
        Anchor(hpTrack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -274f), new Vector2(204f, 17f));
        hpTrack.GetComponent<Image>().raycastTarget = false;
        RectTransform hpFill = CreatePanel("HpFill", hpTrack, new Color32(185, 127, 207, 255));
        hpFill.anchorMin = Vector2.zero;
        hpFill.anchorMax = Vector2.one;
        hpFill.offsetMin = hpFill.offsetMax = Vector2.zero;
        Image fill = hpFill.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;

        // The legend mirrors the reference's right-hand vertical key.
        RectTransform legendPanel = CreatePanel("LegendPanel", chrome, new Color32(8, 7, 23, 224));
        Anchor(legendPanel, Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-28f, -140f), new Vector2(265f, 630f));
        StylePanel(legendPanel, new Color32(8, 7, 23, 224));
        MakeLabel(legendPanel, font, "LegendTitle", "LEGEND", 33f, -25f, 220f);

        string[] names = { "Battle", "Elite", "Rest", "Shop", "Boss", "Event" };
        string[] icons = { "X", "!!", "+", "$", "O", "?" };
        TMP_Text[] legend = new TMP_Text[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            float y = -100f - i * 85f;
            TMP_Text icon = MakeLabel(legendPanel, font, "Icon" + names[i],
                icons[i], 36f, y, 58f);
            Anchor(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(58f, 58f));
            icon.color = new Color32(183, 143, 208, 255);
            legend[i] = MakeLabel(legendPanel, font, "Legend" + names[i],
                names[i].ToUpperInvariant(), 23f, y + 1f, 165f);
            Anchor(legend[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(87f, y + 2f), new Vector2(165f, 50f));
            legend[i].alignment = TextAlignmentOptions.Left;
        }

        footer.gameObject.SetActive(false);
        RectTransform back = CreatePanel("BackButton", chrome, new Color32(15, 11, 35, 240));
        Anchor(back, Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(28f, 28f), new Vector2(250f, 75f));
        StylePanel(back, new Color32(15, 11, 35, 240));
        Image backImage = back.GetComponent<Image>();
        backImage.raycastTarget = true;
        Button button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = backImage;
        MakeLabel(back, font, "BackLabel", "<  BACK", 29f, -10f, 235f);

        MapHudUI hud = chrome.GetComponent<MapHudUI>();
        SerializedObject serializedHud = new SerializedObject(hud);
        SerializedProperty legends = serializedHud.FindProperty("legendTexts");
        legends.arraySize = legend.Length;
        for (int i = 0; i < legend.Length; i++)
            legends.GetArrayElementAtIndex(i).objectReferenceValue = legend[i];
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Reference-style Map screen saved.");
    }

    private static TMP_Text MakeLabel(Transform parent, TMP_FontAsset font,
        string name, string value, float size, float y, float width)
    {
        TextMeshProUGUI label = CreateText(name, parent, font, value, size,
            new Color32(234, 215, 191, 255), TextAlignmentOptions.Center);
        Anchor(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(width, 58f));
        return label;
    }

    [MenuItem("Tools/Cosmic UI/Build Map Screen")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvasObject = GameObject.Find("MapCanvas");

        if (canvasObject == null)
        {
            throw new InvalidOperationException("MapCanvas was not found in MapScene.");
        }

        Transform canvas = canvasObject.transform;
        if (canvas.Find("MapChrome") != null)
        {
            Debug.Log("Map screen chrome already exists; leaving it unchanged.");
            return;
        }

        TMP_FontAsset englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Cosmic/Fonts/Cinzel SDF.asset"
        );
        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset"
        );

        if (englishFont == null || thaiFont == null)
        {
            throw new InvalidOperationException("Map fonts could not be loaded.");
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            throw new InvalidOperationException("MapCanvas has no CanvasScaler.");
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject chrome = new GameObject("MapChrome", typeof(RectTransform));
        RectTransform chromeRect = chrome.GetComponent<RectTransform>();
        chromeRect.SetParent(canvas, false);
        Stretch(chromeRect);

        RectTransform header = CreatePanel(
            "HeaderPanel",
            chromeRect,
            new Color32(10, 8, 27, 226)
        );
        Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 118f));

        RectTransform headerLine = CreatePanel(
            "HeaderRule", header, new Color32(128, 84, 159, 220)
        );
        Anchor(headerLine, Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 2f));

        TextMeshProUGUI title = CreateText(
            "MapTitle", header, englishFont, "THE PATH", 50,
            new Color32(235, 222, 195, 255), TextAlignmentOptions.Left
        );
        Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(58f, -10f), new Vector2(650f, 64f));

        TextMeshProUGUI subtitle = CreateText(
            "MapSubtitle", header, englishFont, "CHOOSE YOUR NEXT DESCENT", 24,
            new Color32(178, 147, 199, 255), TextAlignmentOptions.Left
        );
        Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(62f, -77f), new Vector2(680f, 30f));

        TextMeshProUGUI status = CreateText(
            "RunStatus", header, englishFont,
            "HP --/--   •   GOLD --   •   FLOOR --/--", 27,
            new Color32(220, 210, 191, 255), TextAlignmentOptions.Right
        );
        Anchor(status.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-58f, -37f), new Vector2(930f, 45f));

        RectTransform footer = CreatePanel(
            "FooterPanel",
            chromeRect,
            new Color32(10, 8, 27, 231)
        );
        Anchor(footer, Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 116f));

        RectTransform footerLine = CreatePanel(
            "FooterRule", footer, new Color32(128, 84, 159, 220)
        );
        Anchor(footerLine, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 2f));

        TextMeshProUGUI hint = CreateText(
            "MapHint", footer, englishFont,
            "SELECT AN AVAILABLE NODE TO CONTINUE", 25,
            new Color32(231, 216, 191, 255), TextAlignmentOptions.Center
        );
        Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(1450f, 38f));

        string[] names = { "Battle", "Elite", "Rest", "Shop", "Boss" };
        Color32[] colors =
        {
            new Color32(225, 218, 200, 255),
            new Color32(218, 151, 177, 255),
            new Color32(167, 205, 192, 255),
            new Color32(223, 194, 139, 255),
            new Color32(190, 152, 225, 255)
        };
        TMP_Text[] legend = new TMP_Text[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            TextMeshProUGUI label = CreateText(
                "Legend" + names[i], footer, englishFont,
                names[i].ToUpperInvariant(), 24, colors[i],
                TextAlignmentOptions.Center
            );

            Anchor(label.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2((i - 2) * 270f, 17f), new Vector2(235f, 37f));
            legend[i] = label;
        }

        MapHudUI hud = chrome.AddComponent<MapHudUI>();
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("titleText").objectReferenceValue = title;
        serializedHud.FindProperty("subtitleText").objectReferenceValue = subtitle;
        serializedHud.FindProperty("statusText").objectReferenceValue = status;
        serializedHud.FindProperty("hintText").objectReferenceValue = hint;
        serializedHud.FindProperty("englishFont").objectReferenceValue = englishFont;
        serializedHud.FindProperty("thaiFont").objectReferenceValue = thaiFont;

        SerializedProperty legendProperty = serializedHud.FindProperty("legendTexts");
        legendProperty.arraySize = legend.Length;
        for (int i = 0; i < legend.Length; i++)
        {
            legendProperty.GetArrayElementAtIndex(i).objectReferenceValue = legend[i];
        }
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Cosmic Map screen saved to " + ScenePath);
    }

    [MenuItem("Tools/Cosmic UI/Polish Map Screen")]
    public static void Polish()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvasObject = GameObject.Find("MapCanvas");
        if (canvasObject == null)
        {
            throw new InvalidOperationException("MapCanvas was not found.");
        }

        Transform chrome = canvasObject.transform.Find("MapChrome");
        if (chrome == null)
        {
            throw new InvalidOperationException("Build Map Screen before polishing it.");
        }

        RectTransform header = chrome.Find("HeaderPanel") as RectTransform;
        RectTransform footer = chrome.Find("FooterPanel") as RectTransform;
        if (header == null || footer == null)
        {
            throw new InvalidOperationException("Map header or footer was not found.");
        }

        TMP_FontAsset englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Cosmic/Fonts/Cinzel SDF.asset"
        );
        if (englishFont == null)
        {
            throw new InvalidOperationException("Cinzel font was not found.");
        }

        // Floating panels frame the eclipse instead of covering it.
        Anchor(header, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(36f, -28f),
            new Vector2(700f, 125f));
        StylePanel(header, new Color32(10, 8, 27, 213));

        RectTransform statusPanel = chrome.Find("StatusPanel") as RectTransform;
        if (statusPanel == null)
        {
            statusPanel = CreatePanel(
                "StatusPanel", chrome, new Color32(10, 8, 27, 213)
            );
        }
        Anchor(statusPanel, Vector2.one, Vector2.one,
            Vector2.one, new Vector2(-36f, -28f),
            new Vector2(730f, 125f));
        StylePanel(statusPanel, new Color32(10, 8, 27, 213));

        RectTransform title = header.Find("MapTitle") as RectTransform;
        RectTransform subtitle = header.Find("MapSubtitle") as RectTransform;
        RectTransform status = header.Find("RunStatus") as RectTransform;
        if (status == null)
        {
            status = statusPanel.Find("RunStatus") as RectTransform;
        }
        if (title == null || subtitle == null || status == null)
        {
            throw new InvalidOperationException("Map header labels are incomplete.");
        }

        Anchor(title, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(32f, -5f),
            new Vector2(630f, 67f));
        title.GetComponent<TextMeshProUGUI>().fontSize = 49f;
        Anchor(subtitle, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(38f, -70f),
            new Vector2(620f, 48f));

        status.SetParent(statusPanel, false);
        Anchor(status, Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-30f, -49f), new Vector2(670f, 48f));
        status.GetComponent<TextMeshProUGUI>().fontSize = 25f;

        RectTransform caption = statusPanel.Find("StatusCaption") as RectTransform;
        if (caption == null)
        {
            caption = CreateText(
                "StatusCaption", statusPanel, englishFont,
                "RUN STATUS", 20,
                new Color32(182, 145, 203, 255),
                TextAlignmentOptions.Right
            ).rectTransform;
        }
        Anchor(caption, Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-31f, -17f), new Vector2(670f, 27f));

        RectTransform statusRule = statusPanel.Find("StatusRule") as RectTransform;
        if (statusRule == null)
        {
            statusRule = CreatePanel(
                "StatusRule", statusPanel, new Color32(128, 84, 159, 220)
            );
        }
        Anchor(statusRule, Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 2f));

        Anchor(footer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 20f),
            new Vector2(1460f, 112f));
        StylePanel(footer, new Color32(10, 8, 27, 222));

        RectTransform hint = footer.Find("MapHint") as RectTransform;
        if (hint != null)
        {
            Anchor(hint, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f),
                new Vector2(1320f, 37f));
        }

        string[] legendNames =
        {
            "LegendBattle", "LegendElite", "LegendRest",
            "LegendShop", "LegendBoss"
        };
        for (int i = 0; i < legendNames.Length; i++)
        {
            RectTransform label = footer.Find(legendNames[i]) as RectTransform;
            if (label == null)
            {
                continue;
            }
            Anchor(label, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2((i - 2) * 250f, 17f),
                new Vector2(220f, 37f));

            string markName = "LegendMark" + legendNames[i].Substring(6);
            RectTransform mark = footer.Find(markName) as RectTransform;
            if (mark == null)
            {
                mark = CreatePanel(
                    markName, footer,
                    label.GetComponent<TextMeshProUGUI>().color
                );
            }
            Anchor(mark, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2((i - 2) * 250f - 102f, 35f),
                new Vector2(9f, 9f));
            mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        AddCornerDiamond(header, "HeaderDiamond");
        AddCornerDiamond(statusPanel, "StatusDiamond");

        MapHudUI hud = chrome.GetComponent<MapHudUI>();
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("statusCaptionText").objectReferenceValue =
            caption.GetComponent<TextMeshProUGUI>();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Polished cosmic Map screen saved to " + ScenePath);
    }

    private static void StylePanel(RectTransform panel, Color fill)
    {
        Image image = panel.GetComponent<Image>();
        image.color = fill;
        image.raycastTarget = false;
        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0.48f, 0.33f, 0.60f, 0.78f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static void AddCornerDiamond(RectTransform panel, string name)
    {
        RectTransform diamond = panel.Find(name) as RectTransform;
        if (diamond == null)
        {
            diamond = CreatePanel(
                name, panel, new Color32(149, 103, 179, 235)
            );
        }
        Anchor(diamond, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
            new Vector2(11f, 11f));
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static RectTransform CreatePanel(
        string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)
        );
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string value,
        float size,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(
        RectTransform rect,
        Vector2 min,
        Vector2 max,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
