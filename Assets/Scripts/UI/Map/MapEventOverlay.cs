using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small, playable Event screen on top of the map. No separate scene is
// needed, so the current procedural route remains visible in the background.
public sealed class MapEventOverlay : MonoBehaviour
{
    private RunManager runManager;
    private TMP_Text title;
    private TMP_Text description;
    private TMP_Text goldLabel;
    private TMP_Text healLabel;

    public static void Show(RunManager manager)
    {
        if (manager == null) return;
        MapEventOverlay existing = FindAnyObjectByType<MapEventOverlay>();
        if (existing != null) return;

        GameObject canvas = GameObject.Find("MapCanvas");
        if (canvas == null)
        {
            Debug.LogError("Event cannot open: MapCanvas is missing.");
            return;
        }

        GameObject overlay = new GameObject(
            "EventOverlay", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(MapEventOverlay)
        );
        overlay.transform.SetParent(canvas.transform, false);
        overlay.transform.SetAsLastSibling();
        RectTransform rect = overlay.GetComponent<RectTransform>();
        Stretch(rect);
        overlay.GetComponent<Image>().color = new Color(0.015f, 0.01f, 0.045f, 0.83f);
        overlay.GetComponent<MapEventOverlay>().Build(manager);
    }

    private void Build(RunManager manager)
    {
        runManager = manager;
        TMP_FontAsset font = GameObject.Find("MapTitle")?.GetComponent<TMP_Text>()?.font;

        GameObject card = new GameObject(
            "EventPanel", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Outline)
        );
        card.transform.SetParent(transform, false);
        RectTransform panel = card.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(700f, 450f);
        card.GetComponent<Image>().color = new Color(0.065f, 0.045f, 0.13f, 0.98f);
        card.GetComponent<Outline>().effectColor = new Color(0.58f, 0.39f, 0.71f);

        title = AddText(panel, font, 43f, -66f, 620f, 62f);
        description = AddText(panel, font, 27f, -164f, 620f, 105f);
        goldLabel = AddButton(panel, font, -256f, true);
        healLabel = AddButton(panel, font, -338f, false);
        RefreshLanguage();
    }

    private TMP_Text AddButton(RectTransform panel, TMP_FontAsset font, float y, bool gold)
    {
        GameObject buttonObject = new GameObject(
            gold ? "TakeGold" : "Heal", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline)
        );
        buttonObject.transform.SetParent(panel, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(560f, 66f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.10f, 0.25f, 1f);
        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.38f, 0.70f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.78f, 0.69f, 0.88f);
        colors.pressedColor = new Color(0.54f, 0.44f, 0.68f);
        button.colors = colors;
        button.onClick.AddListener(() => Choose(gold));

        TMP_Text label = AddText(rect, font, 25f, 0f, 540f, 58f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        return label;
    }

    private static TMP_Text AddText(
        RectTransform parent, TMP_FontAsset font, float size, float y, float width, float height)
    {
        GameObject item = new GameObject(
            "Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)
        );
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = new Color(0.92f, 0.85f, 0.75f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 19f;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnEnable() { CosmicLanguage.Changed += RefreshLanguage; }
    private void OnDisable() { CosmicLanguage.Changed -= RefreshLanguage; }

    private void RefreshLanguage()
    {
        if (title == null) return;
        bool thai = CosmicLanguage.IsThai;
        MapHudUI hud = FindAnyObjectByType<MapHudUI>();
        TMP_FontAsset font = hud == null ? null : hud.GetFont(thai);
        if (font != null)
        {
            title.font = font;
            description.font = font;
            goldLabel.font = font;
            healLabel.font = font;
        }
        title.text = thai ? "เสียงเรียกจากความว่างเปล่า" : "A WHISPER IN THE VOID";
        description.text = thai
            ? "แท่นบูชาโบราณยื่นข้อเสนอให้คุณ เลือกรับได้หนึ่งอย่าง"
            : "An ancient altar offers a single gift. Choose one.";
        goldLabel.text = thai ? "รับทอง 25" : "TAKE 25 GOLD";
        healLabel.text = thai ? "ฟื้นฟู 12 HP" : "RESTORE 12 HP";
    }

    private void Choose(bool gold)
    {
        if (runManager == null) return;
        runManager.ResolveEvent(gold);
        Destroy(gameObject);
    }
}
