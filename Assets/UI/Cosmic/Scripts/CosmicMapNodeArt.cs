using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Appearance only: MapNodeView continues to own movement and button callbacks.
public sealed class CosmicMapNodeArt : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public MapNode Node { get; private set; }
    public bool IsCurrent => manager != null && manager.GetCurrentNode() == Node;
    public bool IsAvailable => button != null && button.interactable;
    private RunManager manager;
    private Button button;
    private TMP_Text label;
    private AstralMapGraphic ring, icon;
    private bool hovering, pressing, selected;
    private float hover;

    public void Setup(MapNode node, RunManager runManager, Button target, TMP_Text text)
    {
        Node = node; manager = runManager; button = target; label = text;
        Image background = target.GetComponent<Image>();
        background.sprite = null; background.color = Color.clear; background.raycastTarget = true;
        Outline outline = target.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        target.transition = Selectable.Transition.None;
        ring = AddArt("AstralRing", AstralMapGraphic.Motif.NodeRing, 1f);
        ring.detailColor = new Color(.004f, .002f, .014f, .96f);
        icon = AddArt("EncounterIcon", MotifFor(node.NodeType), node.NodeType == MapNodeType.Boss ? 1f : .48f);
        if (node.NodeType == MapNodeType.Boss) ring.gameObject.SetActive(false);
        if (label != null)
        {
            label.raycastTarget = false; label.gameObject.SetActive(false);
            label.fontSize = 18f; label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 1f); rect.anchoredPosition = new Vector2(0f, -2f);
            rect.sizeDelta = new Vector2(170f, 27f);
        }
        Update();
    }

    public static AstralMapGraphic.Motif MotifFor(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Elite: return AstralMapGraphic.Motif.Skull;
            case MapNodeType.Rest: return AstralMapGraphic.Motif.Flame;
            case MapNodeType.Shop: return AstralMapGraphic.Motif.Pouch;
            case MapNodeType.Boss: return AstralMapGraphic.Motif.Eclipse;
            case MapNodeType.Event: return AstralMapGraphic.Motif.Question;
            default: return AstralMapGraphic.Motif.Swords;
        }
    }

    private AstralMapGraphic AddArt(string name, AstralMapGraphic.Motif motif, float scale)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AstralMapGraphic));
        item.transform.SetParent(transform, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        float inset = (1f - scale) * .5f;
        rect.anchorMin = new Vector2(inset, inset); rect.anchorMax = new Vector2(1f - inset, 1f - inset);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        AstralMapGraphic art = item.GetComponent<AstralMapGraphic>();
        art.motif = motif; art.raycastTarget = false;
        return art;
    }

    private void Update()
    {
        if (Node == null || ring == null) return;
        hover = Mathf.Lerp(hover, IsAvailable && (hovering || selected) ? 1f : 0f,
            1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        float pulse = IsCurrent ? .07f * Mathf.Sin(Time.unscaledTime * 1.8f) : 0f;
        ring.emphasis = IsCurrent ? .90f + pulse : IsAvailable ? .25f + hover * .65f : 0f;
        ring.color = IsCurrent ? new Color(.78f, .48f, 1f)
            : IsAvailable ? Color.Lerp(new Color(.46f, .24f, .62f), new Color(.78f, .55f, .92f), hover)
            : new Color(.35f, .21f, .46f, .88f);
        icon.color = IsCurrent || IsAvailable ? new Color(.88f, .74f, .63f)
            : new Color(.62f, .52f, .56f, .88f);
        if (Node.IsVisited && !IsCurrent) icon.color = new Color(.46f, .35f, .31f, .74f);
        icon.emphasis = ring.emphasis;
        ring.SetVerticesDirty(); icon.SetVerticesDirty();
        transform.localScale = Vector3.one * (1f + hover * .065f - (pressing && IsAvailable ? .045f : 0f));
        if (label != null) label.gameObject.SetActive(hover > .15f);
    }

    public void RefreshArt() { Update(); }

    public void OnPointerEnter(PointerEventData e) { hovering = true; }
    public void OnPointerExit(PointerEventData e) { hovering = pressing = false; }
    public void OnPointerDown(PointerEventData e) { pressing = e.button == PointerEventData.InputButton.Left; }
    public void OnPointerUp(PointerEventData e) { pressing = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
}
