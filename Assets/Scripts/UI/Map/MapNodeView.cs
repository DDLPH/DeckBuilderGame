using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapNodeView : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private Button nodeButton;
    [SerializeField] private TMP_Text nodeText;
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset thaiFont;

    private MapNode node;
    private RunManager runManager;
    private Image background;
    private Outline border;
    private RawImage topRule;
    private RawImage sigilDiamond;
    private TMP_Text sigilLetter;
    private Vector3 baseScale;
    private float glow;
    private bool hovering;
    private bool selected;
    private bool pressing;
    private Image innerDisc;
    private TMP_Text iconText;
    private static Sprite circleSprite;
    private CosmicMapNodeArt astralArt;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (astralArt != null) return;
        if (nodeButton == null || node == null || background == null)
        {
            return;
        }

        bool available = nodeButton.IsInteractable();
        float targetGlow = available && (hovering || selected) ? 1f : 0f;
        float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
        glow = Mathf.Lerp(glow, targetGlow, blend);

        Color accent = GetAccentColor();
        Color dark = new Color(0.055f, 0.045f, 0.11f, 0.95f);
        background.color = available
            ? Color.Lerp(dark, accent * 0.40f + dark * 0.60f, 0.55f + glow * 0.25f)
            : new Color(0.075f, 0.065f, 0.13f, 0.88f);

        Color visibleAccent = available
            ? Color.Lerp(accent * 0.55f, accent, glow)
            : new Color(0.44f, 0.39f, 0.50f, 0.70f);

        border.effectColor = visibleAccent;
        if (topRule != null) topRule.color = visibleAccent;
        if (sigilDiamond != null) sigilDiamond.color = visibleAccent;
        if (sigilLetter != null) sigilLetter.color = available ? accent : visibleAccent;
        if (innerDisc != null) innerDisc.color = new Color(0.045f, 0.035f, 0.09f, 0.94f);
        if (iconText != null) iconText.color = available ? accent : visibleAccent;
        nodeText.color = available
            ? Color.Lerp(new Color(0.85f, 0.82f, 0.75f), Color.white, glow)
            : new Color(0.68f, 0.64f, 0.72f);

        transform.localScale = baseScale *
            (1f + glow * 0.045f - (pressing && available ? 0.055f : 0f));
    }

    private void OnEnable()
    {
        CosmicLanguage.Changed += RefreshLabel;
    }

    private void OnDisable()
    {
        CosmicLanguage.Changed -= RefreshLabel;
        hovering = false;
        selected = false;
        pressing = false;
        transform.localScale = baseScale;
    }

    public void Setup(MapNode targetNode, RunManager targetRunManager)
    {
        node = targetNode;
        runManager = targetRunManager;

        /*
        Debug.Log(
            "MapNodeView Setup | RunManager : " +
            (runManager != null)
        );
        
        Debug.Log(
            "MapNodeView Setup | CurrentRun : " +
            (
                runManager != null &&
                runManager.CurrentRun != null
            )
        );
        */

        if (nodeButton == null)
        {
            Debug.LogError("MapNodeView Button is not assigned");
            return;
        }

        if (nodeText == null)
        {
            Debug.LogError("MapNodeView Text is not assigned");
            return;
        }

        astralArt = gameObject.GetComponent<CosmicMapNodeArt>();
        if (astralArt == null) astralArt = gameObject.AddComponent<CosmicMapNodeArt>();
        if (astralArt != null)
        {
            astralArt.Setup(node, runManager, nodeButton, nodeText);
            nodeButton.onClick.RemoveAllListeners();
            nodeButton.onClick.AddListener(OnNodeClicked);
            RefreshLabel();
            RefreshInteractable();
            return;
        }

        nodeText.raycastTarget = false;
        nodeText.gameObject.SetActive(false);
        nodeText.enableAutoSizing = true;
        nodeText.fontSizeMin = 18f;
        nodeText.fontSizeMax = 24f;
        RectTransform labelRect = nodeText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(60f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);
        RefreshLabel();

        background = nodeButton.GetComponent<Image>();
        background.sprite = GetCircleSprite();
        background.type = Image.Type.Simple;
        border = nodeButton.GetComponent<Outline>();
        if (border == null)
        {
            border = nodeButton.gameObject.AddComponent<Outline>();
        }
        border.effectDistance = new Vector2(2f, -2f);
        border.useGraphicAlpha = true;
        nodeButton.transition = Selectable.Transition.None;
        CreateCircleDecorations();

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);

        RefreshInteractable();
        Update();
    }

    public void RefreshInteractable()
    {
        if (nodeButton == null)
        {
            return;
        }
    
        if (runManager == null || node == null)
        {
            nodeButton.interactable = false;
            return;
        }
    
        bool canMove =
            runManager.CanMoveToNode(node);


        /*
        Debug.Log(
            "MapNodeView : " +
            node.NodeType +
            " | CanMove : " +
            canMove
        );
        */
    
        nodeButton.interactable = canMove;
        if (astralArt != null) astralArt.RefreshArt();
    }

    public void OnPointerEnter(PointerEventData eventData) { hovering = true; }
    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        pressing = false;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = eventData.button == PointerEventData.InputButton.Left;
    }
    public void OnPointerUp(PointerEventData eventData) { pressing = false; }
    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; }

    private void CreateDecorations()
    {
        if (topRule != null)
        {
            return;
        }

        topRule = CreateRule("AstralRule");
        RectTransform ruleRect = topRule.rectTransform;
        ruleRect.anchorMin = new Vector2(0f, 1f);
        ruleRect.anchorMax = new Vector2(1f, 1f);
        ruleRect.offsetMin = new Vector2(14f, -4f);
        ruleRect.offsetMax = new Vector2(-14f, -2f);

        sigilDiamond = CreateRule("NodeSigil");
        RectTransform diamondRect = sigilDiamond.rectTransform;
        diamondRect.anchorMin = new Vector2(0f, 0.5f);
        diamondRect.anchorMax = diamondRect.anchorMin;
        diamondRect.anchoredPosition = new Vector2(29f, 0f);
        diamondRect.sizeDelta = new Vector2(32f, 32f);
        diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        GameObject letterObject = new GameObject(
            "NodeRune", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        letterObject.layer = gameObject.layer;
        letterObject.transform.SetParent(transform, false);
        sigilLetter = letterObject.GetComponent<TextMeshProUGUI>();
        sigilLetter.raycastTarget = false;
        sigilLetter.font = englishFont;
        sigilLetter.fontSize = 23f;
        sigilLetter.alignment = TextAlignmentOptions.Center;
        sigilLetter.text = GetRuneLetter();
        RectTransform letterRect = sigilLetter.rectTransform;
        letterRect.anchorMin = new Vector2(0f, 0.5f);
        letterRect.anchorMax = letterRect.anchorMin;
        letterRect.anchoredPosition = new Vector2(29f, 0f);
        letterRect.sizeDelta = new Vector2(38f, 38f);
    }

    private void CreateCircleDecorations()
    {
        if (innerDisc != null) return;
        GameObject disc = new GameObject(
            "InnerDisc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)
        );
        disc.transform.SetParent(transform, false);
        innerDisc = disc.GetComponent<Image>();
        innerDisc.sprite = GetCircleSprite();
        innerDisc.raycastTarget = false;
        RectTransform discRect = innerDisc.rectTransform;
        discRect.anchorMin = Vector2.zero;
        discRect.anchorMax = Vector2.one;
        discRect.offsetMin = new Vector2(6f, 6f);
        discRect.offsetMax = new Vector2(-6f, -6f);

        GameObject glyph = new GameObject(
            "NodeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)
        );
        glyph.transform.SetParent(transform, false);
        iconText = glyph.GetComponent<TextMeshProUGUI>();
        iconText.raycastTarget = false;
        iconText.font = englishFont;
        iconText.text = GetRuneLetter();
        iconText.fontSize = 51f;
        iconText.alignment = TextAlignmentOptions.Center;
        RectTransform iconRect = iconText.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - (size - 1) * 0.5f;
                float dy = y - (size - 1) * 0.5f;
                float alpha = Mathf.Clamp01((size * 0.5f - Mathf.Sqrt(dx * dx + dy * dy)) / 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    private RawImage CreateRule(string name)
    {
        GameObject item = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)
        );
        item.layer = gameObject.layer;
        item.transform.SetParent(transform, false);
        RawImage image = item.GetComponent<RawImage>();
        image.texture = Texture2D.whiteTexture;
        image.raycastTarget = false;
        return image;
    }

    private string GetRuneLetter()
    {
        switch (node.NodeType)
        {
            case MapNodeType.NormalBattle: return "X";
            case MapNodeType.Elite: return "!!";
            case MapNodeType.Rest: return "+";
            case MapNodeType.Shop: return "$";
            case MapNodeType.Boss: return "O";
            case MapNodeType.Event: return "?";
            default: return "?";
        }
    }

    private Color GetAccentColor()
    {
        switch (node.NodeType)
        {
            case MapNodeType.NormalBattle: return new Color(0.88f, 0.83f, 0.72f);
            case MapNodeType.Elite: return new Color(0.85f, 0.59f, 0.69f);
            case MapNodeType.Rest: return new Color(0.65f, 0.81f, 0.75f);
            case MapNodeType.Shop: return new Color(0.87f, 0.76f, 0.54f);
            case MapNodeType.Boss: return new Color(0.75f, 0.60f, 0.89f);
            case MapNodeType.Event: return new Color(0.78f, 0.68f, 0.91f);
            default: return Color.white;
        }
    }

    private void RefreshLabel()
    {
        if (node == null || nodeText == null)
        {
            return;
        }

        bool isThai = CosmicLanguage.IsThai;
        TMP_FontAsset selectedFont = isThai ? thaiFont : englishFont;

        if (selectedFont != null)
        {
            nodeText.font = selectedFont;
        }

        switch (node.NodeType)
        {
            case MapNodeType.NormalBattle:
                nodeText.text = isThai ? "ต่อสู้" : "BATTLE";
                break;
            case MapNodeType.Elite:
                nodeText.text = isThai ? "ศัตรูพิเศษ" : "ELITE";
                break;
            case MapNodeType.Rest:
                nodeText.text = isThai ? "พัก" : "REST";
                break;
            case MapNodeType.Shop:
                nodeText.text = isThai ? "ร้านค้า" : "SHOP";
                break;
            case MapNodeType.Boss:
                nodeText.text = isThai ? "บอส" : "BOSS";
                break;
            case MapNodeType.Event:
                nodeText.text = isThai ? "เหตุการณ์" : "EVENT";
                break;
        }
    }

    private void OnNodeClicked()
    {
        if (runManager == null || node == null)
        {
            return;
        }
    
        runManager.MoveToNode(node);
    
        MapUIManager mapUIManager = FindFirstObjectByType<MapUIManager>();
    
        if (mapUIManager != null)
        {
            mapUIManager.RefreshMapUI();
        }
    }
}
