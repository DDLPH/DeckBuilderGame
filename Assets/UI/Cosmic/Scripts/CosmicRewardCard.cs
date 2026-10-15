using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CosmicRewardCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private CosmicRewardUI screen;
    private CardData data;
    private int index;
    private Button button;
    private Image background;
    private AstralMapGraphic frame;
    private TMP_Text title, description, type, rarity, cost, status;
    private Vector2 origin;
    private bool hovering, focused;
    private float emphasis;
    public Button Button => button;
    public CardData Data => data;
    public int Index => index;

    public void Build(CosmicRewardUI owner, CardData card, int slot)
    {
        screen = owner; data = card; index = slot; origin = ((RectTransform)transform).anchoredPosition;
        background = gameObject.AddComponent<Image>(); background.color = CosmicUITheme.Panel;
        button = gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.targetGraphic = background;
        button.onClick.AddListener(() => screen.Select(index));
        frame = CosmicUITheme.Frame(transform, true);
        title = BattleUIFactory.Text("Name", transform, new Vector2(260, 66), new Vector2(0, 128), "", 30, screen.Font);
        cost = BattleUIFactory.Text("Cost", transform, new Vector2(65, 46), new Vector2(-108, 181), data.EnergyCost.ToString(), 32, screen.englishFont);
        var orb = BattleUIFactory.Image("EnergyOrb", transform, new Vector2(55, 46), new Vector2(-108, 181), new Color(.18f, .09f, .27f));
        CosmicUITheme.Frame(orb.transform, true); orb.transform.SetSiblingIndex(1); cost.transform.SetAsLastSibling();
        rarity = BattleUIFactory.Text("Rarity", transform, new Vector2(160, 35), new Vector2(45, 179), "", 18, screen.Font);
        rarity.color = CosmicUITheme.Muted;
        var icon = BattleUIFactory.Rect("Emblem", transform, new Vector2(105, 105), new Vector2(0, 28)).gameObject.AddComponent<AstralMapGraphic>();
        icon.motif = CosmicCardText.Icon(data); icon.color = CosmicUITheme.Accent; icon.raycastTarget = false;
        type = BattleUIFactory.Text("Type", transform, new Vector2(240, 30), new Vector2(0, -48), "", 20, screen.Font);
        type.color = CosmicUITheme.Muted;
        description = BattleUIFactory.Text("Description", transform, new Vector2(260, 112), new Vector2(0, -112), "", 26, screen.Font);
        description.textWrappingMode = TextWrappingModes.Normal;
        status = BattleUIFactory.Text("Status", transform, new Vector2(260, 36), new Vector2(0, -190), "", 20, screen.Font);
        status.color = CosmicUITheme.Accent; Refresh();
    }

    public void Refresh()
    {
        title.font = description.font = type.font = rarity.font = status.font = screen.Font;
        cost.font = screen.englishFont;
        title.text = CosmicCardText.Name(data); description.text = CosmicCardText.Description(data);
        type.text = CosmicCardText.Type(data); rarity.text = CosmicCardText.Rarity(data);
        bool chosen = screen.SelectedIndex == index;
        status.text = chosen ? (CosmicLanguage.IsThai ? "เลือกแล้ว" : "SELECTED") : (CosmicLanguage.IsThai ? "คลิกเพื่อเลือก" : "CLICK TO CHOOSE");
        button.interactable = screen.CanSelect;
    }
    private void Update()
    {
        if (screen == null || frame == null) return;
        bool chosen = screen.SelectedIndex == index;
        float target = chosen || (button.IsInteractable() && (hovering || focused)) ? 1 : 0;
        emphasis = Mathf.Lerp(emphasis, target, 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        frame.color = Color.Lerp(CosmicUITheme.Border, CosmicUITheme.Ivory, emphasis);
        frame.emphasis = emphasis; frame.SetVerticesDirty();
        background.color = Color.Lerp(CosmicUITheme.Panel, new Color(.075f, .035f, .115f, .98f), emphasis);
        ((RectTransform)transform).anchoredPosition = origin + Vector2.up * (emphasis * 10);
        transform.localScale = Vector3.one * (1 + emphasis * .018f);
        button.interactable = screen.CanSelect;
    }
    public void OnPointerEnter(PointerEventData e) { hovering = true; if (button.IsInteractable()) CosmicAudio.Play(CosmicCue.Hover); }
    public void OnPointerExit(PointerEventData e) { hovering = false; }
    public void OnSelect(BaseEventData e) { focused = true; }
    public void OnDeselect(BaseEventData e) { focused = false; }
}
