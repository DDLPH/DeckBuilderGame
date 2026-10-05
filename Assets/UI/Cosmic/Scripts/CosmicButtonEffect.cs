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

    private static readonly Color NormalFill = new Color(0.065f, 0.055f, 0.13f, 0.94f);
    private static readonly Color HoverFill = new Color(0.15f, 0.10f, 0.23f, 0.98f);
    private static readonly Color PressFill = new Color(0.23f, 0.14f, 0.30f, 1f);
    private static readonly Color DisabledFill = new Color(0.06f, 0.05f, 0.11f, 0.70f);
    private static readonly Color NormalBorder = new Color(0.46f, 0.34f, 0.57f, 0.75f);
    private static readonly Color BrightBorder = new Color(0.77f, 0.60f, 0.89f, 1f);
    private static readonly Color NormalText = new Color(0.85f, 0.82f, 0.75f, 1f);
    private static readonly Color BrightText = new Color(1f, 0.95f, 0.84f, 1f);
    private static readonly Color DisabledText = new Color(0.58f, 0.55f, 0.61f, 1f);

    private Button button;
    private Image background;
    private Outline outline;
    private TMP_Text label;
    private RawImage topRule;
    private RawImage leftDiamond;
    private RawImage rightDiamond;
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
        outline = GetComponent<Outline>();
        label = GetComponentInChildren<TMP_Text>(true);
        originalScale = transform.localScale;

        // Unity's Color Tint would compete with the smooth animation below.
        button.transition = Selectable.Transition.None;
        topRule = CreateTopRule();
        leftDiamond = CreateDiamond("LeftStar", true);
        rightDiamond = CreateDiamond("RightStar", false);
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
            if (outline != null) outline.effectColor = NormalBorder * 0.45f;
            if (label != null) label.color = DisabledText;
            SetAccents(0.12f);
            transform.localScale = originalScale;
            return;
        }

        background.color = Color.Lerp(Color.Lerp(NormalFill, HoverFill, glow), PressFill, pushed);
        if (outline != null) outline.effectColor = Color.Lerp(NormalBorder, BrightBorder, glow);
        if (label != null) label.color = Color.Lerp(NormalText, BrightText, glow);
        SetAccents(0.20f + glow * 0.70f);
        transform.localScale = originalScale * (1f + glow * 0.025f - pushed * 0.045f);
    }

    private void SetAccents(float alpha)
    {
        Color accent = new Color(0.77f, 0.59f, 0.92f, alpha);
        topRule.color = accent;
        leftDiamond.color = accent;
        rightDiamond.color = accent;
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
