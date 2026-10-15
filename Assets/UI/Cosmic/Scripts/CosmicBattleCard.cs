using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One instance represents one slot even when several slots share a CardData asset.
public sealed class CosmicBattleCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CardData Data { get; private set; }
    public int HandIndex { get; private set; }
    public RectTransform Rect => (RectTransform)transform;
    public bool Dragging { get; private set; }
    private CosmicBattleUI screen;
    private CanvasGroup group;
    private Vector2 origin;
    private TMP_Text title, cost, description, type;
    private AstralMapGraphic frame;
    private bool hover, moving;
    private int suppressClickFrame = -10;

    public void Build(CosmicBattleUI owner, CardData card, int index)
    {
        screen = owner; Data = card; HandIndex = index; origin = Rect.anchoredPosition;
        group = gameObject.AddComponent<CanvasGroup>();
        var image = gameObject.AddComponent<Image>(); image.color = new Color(.055f, .035f, .085f, .98f);
        frame = CosmicUITheme.Frame(transform, true);
        title = BattleUIFactory.Text("Title", transform, new Vector2(178, 43), new Vector2(0, 111), "", 26, owner.Font);
        cost = BattleUIFactory.Text("Cost", transform, new Vector2(48, 42), new Vector2(-78, 150), card.EnergyCost.ToString(), 28, owner.Font);
        var costBox = BattleUIFactory.Image("CostBackground", transform, new Vector2(45, 42), new Vector2(-78, 150), new Color(.22f, .12f, .34f));
        costBox.transform.SetSiblingIndex(1); cost.transform.SetAsLastSibling();
        var icon = BattleUIFactory.Rect("Icon", transform, new Vector2(82, 82), new Vector2(0, 28)).gameObject.AddComponent<AstralMapGraphic>();
        icon.motif = BattleManager.TargetsEnemySide(card) ? AstralMapGraphic.Motif.Swords : AstralMapGraphic.Motif.Shield;
        if (card.Effects != null) foreach (CardEffect effect in card.Effects)
        {
            if (effect is DrawCardsEffect) icon.motif = AstralMapGraphic.Motif.Crescent;
            if (effect is GainEnergyEffect) icon.motif = AstralMapGraphic.Motif.Eclipse;
        }
        icon.color = CosmicUITheme.Accent; icon.raycastTarget = false;
        type = BattleUIFactory.Text("Type", transform, new Vector2(175, 30), new Vector2(0, -39), "", 18, owner.Font);
        type.color = CosmicUITheme.Muted;
        description = BattleUIFactory.Text("Description", transform, new Vector2(178, 94), new Vector2(0, -103), "", 23, owner.Font);
        description.textWrappingMode = TextWrappingModes.Normal;
        RefreshLanguage();
    }

    public void RefreshLanguage()
    {
        title.font = cost.font = description.font = type.font = screen.Font;
        title.text = screen.CardName(Data);
        description.text = screen.CardDescription(Data);
        type.text = CosmicLanguage.IsThai ? (BattleManager.TargetsEnemySide(Data) ? "โจมตี" : "ทักษะ") : Data.CardType.ToString().ToUpperInvariant();
    }

    private void Update()
    {
        if (screen == null || Data == null || group == null) return;
        bool available = screen.Battle != null && screen.Battle.CanUseCard(Data);
        if (!moving) group.alpha = available || Dragging ? 1 : .48f;
        frame.color = screen.Selected == this || hover || Dragging ? CosmicUITheme.Accent : CosmicUITheme.Border;
        if (!Dragging && !moving)
        {
            bool raised = available && (hover || screen.Selected == this);
            Rect.localScale = Vector3.Lerp(Rect.localScale, Vector3.one * (raised ? 1.045f : 1), Time.unscaledDeltaTime * 15);
            Rect.anchoredPosition = Vector2.Lerp(Rect.anchoredPosition, origin + Vector2.up * (raised ? 15 : 0), Time.unscaledDeltaTime * 15);
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || !screen.BeginDrag(this)) return;
        Dragging = true; group.blocksRaycasts = false; Rect.SetAsLastSibling();
        Rect.localScale = Vector3.one * 1.08f;
        OnDrag(e);
    }
    public void OnDrag(PointerEventData e)
    {
        if (!Dragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, e.position,
            e.pressEventCamera, out Vector2 point)) Rect.anchoredPosition = point;
        screen.TrackDrag(e.position);
    }
    public void OnEndDrag(PointerEventData e)
    {
        if (!Dragging) return;
        Dragging = false; group.blocksRaycasts = true; suppressClickFrame = Time.frameCount;
        if (!screen.EndDrag(this, e.position)) ReturnToHand();
    }
    public void ReturnToHand()
    {
        if (moving) return;
        StartCoroutine(ReturnMotion());
    }
    private IEnumerator ReturnMotion()
    {
        moving = true;
        Vector2 start = Rect.anchoredPosition;
        for (float t = 0; t < .18f; t += Time.unscaledDeltaTime)
        { Rect.anchoredPosition = Vector2.Lerp(start, origin, Mathf.SmoothStep(0, 1, t / .18f)); yield return null; }
        Rect.anchoredPosition = origin; Rect.localScale = Vector3.one; moving = false;
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left && !Dragging && !moving && Time.frameCount > suppressClickFrame + 1)
            screen.Select(this);
    }
    public void OnPointerEnter(PointerEventData e)
    { hover = true; if (screen.Battle != null && screen.Battle.CanUseCard(Data)) CosmicAudio.Play(CosmicCue.Hover); }
    public void OnPointerExit(PointerEventData e) { hover = false; }
    public void CancelDrag()
    { Dragging = false; group.blocksRaycasts = true; if (isActiveAndEnabled) ReturnToHand(); }
    public void SetAnimating() { moving = true; }
}
