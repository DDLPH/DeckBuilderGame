using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One palette and frame vocabulary for menus, map HUD and overlays.
public static class CosmicUITheme
{
    public static readonly Color Ivory = new Color(.92f, .82f, .72f, 1);
    public static readonly Color Muted = new Color(.56f, .50f, .61f, 1);
    public static readonly Color Border = new Color(.43f, .27f, .56f, .9f);
    public static readonly Color Accent = new Color(.71f, .48f, .88f, 1);
    public static readonly Color Panel = new Color(.014f, .01f, .035f, .96f);
    public static readonly Color ButtonFill = new Color(.025f, .017f, .054f, .94f);
    public static readonly Color Scrim = new Color(.004f, .002f, .014f, .78f);

    public static AstralMapGraphic Frame(Transform parent, bool button)
    {
        string name = button ? "UnifiedButtonFrame" : "UnifiedPanelFrame";
        Transform found = parent.Find(name);
        if (found == null)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AstralMapGraphic));
            item.layer = 5; item.transform.SetParent(parent, false); found = item.transform;
        }
        RectTransform rect = (RectTransform)found;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        AstralMapGraphic art = found.GetComponent<AstralMapGraphic>();
        art.motif = button ? AstralMapGraphic.Motif.ButtonFrame : AstralMapGraphic.Motif.Frame;
        art.color = Border; art.raycastTarget = false;
        return art;
    }

    public static void StyleButton(Button button)
    {
        Image image = button.GetComponent<Image>();
        if (image != null) { image.color = ButtonFill; image.sprite = null; button.targetGraphic = image; }
        Outline outline = button.GetComponent<Outline>(); if (outline != null) outline.enabled = false;
        button.transition = Selectable.Transition.None;
        Frame(button.transform, true);
        foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
        { text.color = button.interactable ? Ivory : Muted; text.raycastTarget = false; }
    }

    public static void StylePanel(Transform panel)
    {
        Image image = panel.GetComponent<Image>(); if (image != null) image.color = Panel;
        Outline outline = panel.GetComponent<Outline>(); if (outline != null) outline.enabled = false;
        Frame(panel, false);
    }
}
