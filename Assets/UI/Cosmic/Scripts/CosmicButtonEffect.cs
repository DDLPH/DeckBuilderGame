using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Subtle hover and press feedback for the cosmic menu. Decorative graphics never
/// receive raycasts, so the Button's existing click handlers remain in control.
/// </summary>
[RequireComponent(typeof(Button), typeof(Image))]
public sealed class CosmicButtonEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    // Only assigned to buttons without an existing AudioSource.Play callback.
    public AudioSource clickSound;
    public bool primaryAction;
    private AstralMapGraphic frame;

    private static readonly Color NormalFill = CosmicUITheme.ButtonFill;
    private static readonly Color HoverFill = new Color(.075f, .044f, .12f, .98f);
    private static readonly Color PressFill = new Color(.115f, .066f, .18f, 1f);
    private static readonly Color DisabledFill = new Color(.018f, .014f, .036f, .72f);
    private static readonly Color NormalBorder = CosmicUITheme.Border;
    private static readonly Color BrightBorder = CosmicUITheme.Accent;
    private static readonly Color NormalText = CosmicUITheme.Ivory;
    private static readonly Color BrightText = new Color(1f, 0.95f, 0.84f, 1f);
    private static readonly Color DisabledText = new Color(0.58f, 0.55f, 0.61f, 1f);

    private Button button;
    private Image background;
    private TMP_Text label;
    private Vector3 originalScale;
    private float emphasis;
    private float press;
    private bool hovering;
    private bool selected;
    private bool pressing;

    private void Awake()
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>(true);
        originalScale = transform.localScale;

        // Unity's Color Tint would compete with the smooth animation below.
        button.transition = Selectable.Transition.None;
        CosmicUITheme.StyleButton(button);
        frame = CosmicUITheme.Frame(transform, true);
        Apply(0f, 0f, true);
    }

    private void Update()
    {
        bool interactable = button.IsInteractable();
        float targetEmphasis = interactable && (hovering || selected) ? 1f : 0f;
        float targetPress = interactable && pressing ? 1f : 0f;
        float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
        emphasis = Mathf.Lerp(emphasis, targetEmphasis, blend);
        press = Mathf.Lerp(press, targetPress, blend);
        Apply(emphasis, press, interactable);
    }

    private void OnDisable()
    {
        hovering = false;
        selected = false;
        pressing = false;
        if (originalScale != Vector3.zero) transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData) { hovering = true; }
    public void OnPointerExit(PointerEventData eventData) { hovering = false; pressing = false; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
        pressing = true;
        PlayClick();
    }
    public void OnPointerUp(PointerEventData eventData) { pressing = false; }
    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; pressing = false; }
    public void OnSubmit(BaseEventData eventData) { PlayClick(); }

    private void PlayClick()
    {
        if (button.IsInteractable() && clickSound != null && clickSound.clip != null)
        {
            clickSound.PlayOneShot(clickSound.clip);
        }
    }

    private void Apply(float glow, float pushed, bool interactable)
    {
        if (!interactable)
        {
            background.color = DisabledFill;
            if (frame != null) frame.color = NormalBorder * .45f;
            if (label != null) label.color = DisabledText;
            SetAccents(0.12f);
            transform.localScale = originalScale;
            return;
        }

        Color restingFill = primaryAction ? Color.Lerp(NormalFill, HoverFill, .35f) : NormalFill;
        background.color = Color.Lerp(Color.Lerp(restingFill, HoverFill, glow), PressFill, pushed);
        if (frame != null) frame.color = Color.Lerp(primaryAction ? NormalBorder * 1.2f : NormalBorder, BrightBorder, glow);
        if (label != null) label.color = Color.Lerp(NormalText, BrightText, glow);
        SetAccents(0.20f + glow * 0.70f);
        transform.localScale = originalScale * (1f + glow * .012f - pushed * .022f);
    }

    private void SetAccents(float alpha)
    {
        if (frame != null) { frame.emphasis = Mathf.Clamp01((alpha-.2f)/.7f); frame.SetVerticesDirty(); }
    }

    private RawImage CreateTopRule()
    {
        RawImage image = CreateDecoration("AstralRule");
        RectTransform rect = (RectTransform)image.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(16f, -4f);
        rect.offsetMax = new Vector2(-16f, -2f);
        return image;
    }

    private RawImage CreateDiamond(string name, bool left)
    {
        RawImage image = CreateDecoration(name);
        RectTransform rect = (RectTransform)image.transform;
        float x = left ? 0f : 1f;
        rect.anchorMin = new Vector2(x, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.anchoredPosition = new Vector2(left ? 13f : -13f, 0f);
        rect.sizeDelta = new Vector2(7f, 7f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        return image;
    }

    private RawImage CreateDecoration(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = Texture2D.whiteTexture;
        image.raycastTarget = false;
        return image;
    }
}
