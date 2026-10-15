using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation shared by the shop and the rest-site upgrade catalogue.
public sealed class CosmicCampCard : MonoBehaviour
{
    public Button Button { get; private set; }
    public CardData Data { get; private set; }
    public int Index { get; private set; }
    private CosmicCampUI screen;
    private TMP_Text caption;
    public void Build(CosmicCampUI owner, CardData card, int index)
    {
        screen = owner; Data = card; Index = index;
        var image = gameObject.AddComponent<Image>(); image.color = CosmicUITheme.Panel;
        Button = gameObject.AddComponent<Button>(); Button.targetGraphic = image;
        CosmicUITheme.Frame(transform, true);
        BattleUIFactory.Text("Name", transform, new Vector2(250, 66), new Vector2(0, 126), CosmicCardText.Name(card), 29, owner.Font);
        var orb = BattleUIFactory.Image("EnergyOrb", transform, new Vector2(56, 44), new Vector2(-105, 178), new Color(.18f, .09f, .27f));
        CosmicUITheme.Frame(orb.transform, true);
        BattleUIFactory.Text("Cost", orb.transform, new Vector2(50, 40), Vector2.zero, card.EnergyCost.ToString(), 29, owner.englishFont);
        var rarity = BattleUIFactory.Text("Rarity", transform, new Vector2(155, 35), new Vector2(40, 178), CosmicCardText.Rarity(card), 18, owner.Font);
        rarity.color = CosmicUITheme.Muted;
        var icon = BattleUIFactory.Rect("Emblem", transform, new Vector2(100, 100), new Vector2(0, 30)).gameObject.AddComponent<AstralMapGraphic>();
        icon.motif = CosmicCardText.Icon(card); icon.color = CosmicUITheme.Accent; icon.raycastTarget = false;
        BattleUIFactory.Text("Type", transform, new Vector2(250, 28), new Vector2(0, -43), CosmicCardText.Type(card), 20, owner.Font).color = CosmicUITheme.Muted;
        BattleUIFactory.Text("Description", transform, new Vector2(250, 112), new Vector2(0, -106), CosmicCardText.Description(card), 25, owner.Font);
        caption = BattleUIFactory.Text("Action", transform, new Vector2(260, 48), new Vector2(0, -183), "", 21, owner.Font);
        Button.onClick.AddListener(() => screen.RequestCard(Index));
        gameObject.AddComponent<CosmicButtonEffect>();
        RefreshState();
    }
    public void RefreshState()
    {
        if (screen == null) return;
        bool enabled = screen.CanUseCard(Data);
        Button.interactable = enabled;
        caption.text = screen.CardCaption(Data);
        caption.color = enabled ? CosmicUITheme.Accent : CosmicUITheme.Muted;
    }
}
