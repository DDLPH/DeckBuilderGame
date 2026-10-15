using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public sealed class CosmicCampUI : MonoBehaviour
{
    public bool restSite;
    public TMP_FontAsset englishFont, thaiFont;
    public Sprite background;
    public AudioClip musicClip;
    [SerializeField] private RectTransform board, catalogue, restOptions;
    [SerializeField] private TMP_Text title, subtitle, hud, feedback, pageText, healText, upgradeText;
    [SerializeField] private Button back, previous, next, heal, upgrade, cancelUpgrade;
    private ShopManager shop;
    private RestManager rest;
    private CosmicModalDialog modal;
    private readonly List<CosmicCampCard> cards = new List<CosmicCampCard>();
    private readonly List<int> indices = new List<int>();
    private int page;
    private bool upgrading, unavailable;
    private string notice;
    public TMP_FontAsset Font => CosmicLanguage.IsThai && thaiFont != null ? thaiFont : englishFont;
    private bool ModalOpen => modal != null && modal.gameObject.activeSelf;
    public RunData Run => restSite ? rest == null ? null : rest.Run : shop == null ? null : shop.Run;
    public bool Ready => restSite ? rest != null && rest.Ready : shop != null && shop.Ready;
    public IReadOnlyList<CosmicCampCard> Cards => cards;
    public Button HealButton => heal;
    public Button UpgradeButton => upgrade;
    public Button BackButton => back;
    public Button NextButton => next;
    public Button PreviousButton => previous;
    public bool Upgrading => upgrading;
    public int Page => page;
    private bool ControlsReady => Ready && !ModalOpen && !CosmicSceneLoader.IsLoading;

    private void Awake()
    {
        if (board == null) BuildLayout();
        back.onClick.AddListener(RequestLeave);
        previous.onClick.AddListener(() => ChangePage(-1)); next.onClick.AddListener(() => ChangePage(1));
        heal.onClick.AddListener(RequestHeal); upgrade.onClick.AddListener(OpenUpgrades);
        cancelUpgrade.onClick.AddListener(CloseUpgrades);
        if (Application.isPlaying) StartCoroutine(Enter());
    }
    private void OnEnable() { CosmicLanguage.Changed += Refresh; }
    private void OnDisable() { CosmicLanguage.Changed -= Refresh; }
    private void OnDestroy() { if (shop != null) shop.Changed -= Refresh; if (rest != null) rest.Changed -= Refresh; }
    public void Bind(ShopManager owner) { if (shop != null) shop.Changed -= Refresh; shop = owner; shop.Changed += Refresh; Refresh(); }
    public void Bind(RestManager owner) { if (rest != null) rest.Changed -= Refresh; rest = owner; rest.Changed += Refresh; Refresh(); }

    public void BuildLayout()
    {
        board = BattleUIFactory.Rect("CampBoard", transform, new Vector2(1920, 1080), Vector2.zero);
        var backdrop = BattleUIFactory.Image("Backdrop", board, new Vector2(1920, 1080), Vector2.zero, restSite ? new Color(.45f, .46f, .64f) : new Color(.60f, .44f, .66f));
        backdrop.sprite = background;
        BattleUIFactory.Image("Shade", board, new Vector2(1920, 1080), Vector2.zero, new Color(.005f, .003f, .022f, .68f));
        BattleUIFactory.Image("Hud", board, new Vector2(1920, 90), new Vector2(0, 495), CosmicUITheme.Panel);
        hud = BattleUIFactory.Text("RunStatus", board, new Vector2(1780, 70), new Vector2(0, 495), "", 28, Font);
        BattleUIFactory.Image("HudRule", board, new Vector2(1800, 2), new Vector2(0, 450), CosmicUITheme.Border);
        title = BattleUIFactory.Text("Title", board, new Vector2(1450, 85), new Vector2(0, 388), "", 48, Font);
        var rule = BattleUIFactory.Rect("Rule", board, new Vector2(1000, 20), new Vector2(0, 331)).gameObject.AddComponent<AstralMapGraphic>();
        rule.motif = AstralMapGraphic.Motif.Divider; rule.color = CosmicUITheme.Border; rule.raycastTarget = false;
        subtitle = BattleUIFactory.Text("Subtitle", board, new Vector2(1500, 60), new Vector2(0, 285), "", 26, Font);
        catalogue = BattleUIFactory.Rect("Catalogue", board, new Vector2(1460, 438), new Vector2(0, -10));
        restOptions = BattleUIFactory.Rect("RestChoices", board, new Vector2(1400, 480), new Vector2(0, -10));
        heal = Choice(restOptions, "Heal", -320, AstralMapGraphic.Motif.Flame, out healText);
        upgrade = Choice(restOptions, "Upgrade", 320, AstralMapGraphic.Motif.Swords, out upgradeText);
        previous = BattleUIFactory.Button("PreviousPage", board, new Vector2(180, 55), new Vector2(-330, -303), "", Font, null);
        next = BattleUIFactory.Button("NextPage", board, new Vector2(180, 55), new Vector2(330, -303), "", Font, null);
        pageText = BattleUIFactory.Text("Page", board, new Vector2(330, 55), new Vector2(0, -303), "", 25, Font);
        feedback = BattleUIFactory.Text("Feedback", board, new Vector2(1450, 65), new Vector2(0, -370), "", 26, Font);
        back = BattleUIFactory.Button("ReturnToMap", board, new Vector2(420, 70), new Vector2(0, -465), "", Font, null);
        cancelUpgrade = BattleUIFactory.Button("CancelUpgrade", board, new Vector2(320, 65), new Vector2(-640, -465), "", Font, null);
    }
    private Button Choice(Transform parent, string name, float x, AstralMapGraphic.Motif motif, out TMP_Text message)
    {
        var panel = BattleUIFactory.Image(name + "Panel", parent, new Vector2(560, 470), new Vector2(x, 0), CosmicUITheme.Panel);
        CosmicUITheme.Frame(panel.transform, false);
        var icon = BattleUIFactory.Rect("Emblem", panel.transform, new Vector2(140, 140), new Vector2(0, 107)).gameObject.AddComponent<AstralMapGraphic>();
        icon.motif = motif; icon.color = CosmicUITheme.Accent; icon.raycastTarget = false;
        message = BattleUIFactory.Text("Description", panel.transform, new Vector2(480, 125), new Vector2(0, -35), "", 29, Font);
        return BattleUIFactory.Button(name, panel.transform, new Vector2(400, 70), new Vector2(0, -145), "", Font, null);
    }
    private IEnumerator Enter()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("CosmicUI.Volume", 1));
        var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.loop = true;
        source.clip = musicClip; source.volume = 0; if (source.clip != null) source.Play();
        var group = board.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0; t < .5f; t += Time.unscaledDeltaTime) { group.alpha = t / .5f; source.volume = .13f * t / .5f; yield return null; }
        group.alpha = 1; source.volume = .13f;
    }
    private void Update()
    {
        RefreshControls();
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (ModalOpen) modal.Close(false); else if (upgrading) CloseUpgrades(); else RequestLeave();
    }
    private void Label(Button button, string text) { button.GetComponentInChildren<TMP_Text>().text = text; }
    public void Refresh()
    {
        if (title == null) return;
        bool th = CosmicLanguage.IsThai;
        foreach (TMP_Text text in board.GetComponentsInChildren<TMP_Text>(true)) text.font = Font;
        if (restSite && rest != null && rest.ActionUsed) upgrading = false;
        title.text = restSite ? (th ? (upgrading ? "เลือกการ์ดเพื่ออัปเกรด" : "แสงไฟกลางความว่างเปล่า") : (upgrading ? "REFORGE A CARD" : "A LIGHT IN THE VOID")) : (th ? "ร้านค้าในเงามืด" : "THE VEILED EXCHANGE");
        subtitle.text = restSite ? (th ? "พักฟื้น หรืออัปเกรดการ์ดได้หนึ่งครั้งในจุดพักนี้" : "Rest or upgrade one card. One action at this rest site.") : (th ? "เลือกการ์ด ตรวจสอบราคา แล้วกดยืนยันซื้อ" : "Choose a card and confirm its price before purchasing.");
        hud.text = !Ready ? "" : (th ? "ชั้น " : "FLOOR ") + Run.CurrentNode.LayerIndex + "     |     HP " + Run.Player.CurrentHP + " / " + Run.Player.MaxHP + "     |     " + (th ? "ทอง " : "GOLD ") + Run.Gold + "     |     " + (th ? "เด็ค " : "DECK ") + Run.Deck.Count;
        Label(back, th ? "กลับสู่แผนที่" : "RETURN TO MAP"); Label(previous, th ? "ก่อนหน้า" : "PREVIOUS"); Label(next, th ? "ถัดไป" : "NEXT"); Label(cancelUpgrade, th ? "กลับไปเลือกการพัก" : "BACK TO REST");
        int amount = rest == null ? 0 : rest.HealAmount;
        healText.text = th ? "พักฟื้น\nฟื้นฟูพลังชีวิต " + amount + " HP" : "REST\nRestore " + amount + " HP";
        upgradeText.text = th ? "ขัดเกลาพลัง\nอัปเกรดการ์ดหนึ่งใบ" : "REFORGE\nUpgrade one card";
        Label(heal, th ? "พักฟื้น" : "REST & HEAL"); Label(upgrade, th ? "เลือกการ์ดอัปเกรด" : "CHOOSE AN UPGRADE");
        restOptions.gameObject.SetActive(restSite && !upgrading);
        catalogue.gameObject.SetActive(!restSite || upgrading); cancelUpgrade.gameObject.SetActive(restSite && upgrading);
        BuildCards();
        feedback.text = notice ?? (restSite ? (rest != null && rest.ActionUsed ? (th ? "ใช้การพักครั้งนี้แล้ว เดินทางต่อได้" : "Rest action used. Your journey can continue.") : (th ? "จะบันทึกเมื่อกลับ Map • เลือกได้หนึ่งอย่าง" : "Saved on return to the map • Choose one action")) : (th ? "ทองและการ์ดจะถูกบันทึกเมื่อกลับสู่แผนที่" : "Gold and cards are saved when you return to the map."));
        RefreshControls();
    }
    private void BuildCards()
    {
        BattleUIFactory.Clear(catalogue); cards.Clear(); indices.Clear();
        if (Ready && (!restSite || upgrading))
        {
            if (restSite) { for (int i = 0; i < Run.Deck.Count; i++) if (Run.Deck.GetCard(i)?.UpgradedCard != null) indices.Add(i); }
            else for (int i = 0; i < shop.Offers.Count; i++) indices.Add(i);
        }
        page = Mathf.Clamp(page, 0, Mathf.Max(0, (indices.Count - 1) / 4));
        int count = Mathf.Min(4, indices.Count - page * 4);
        for (int slot = 0; slot < count; slot++)
        {
            int index = indices[page * 4 + slot]; CardData data = restSite ? Run.Deck.GetCard(index) : shop.Offers[index];
            var rect = BattleUIFactory.Rect("CampCard_" + index, catalogue, new Vector2(304, 438), new Vector2((slot - (count - 1) * .5f) * 342, 0));
            var card = rect.gameObject.AddComponent<CosmicCampCard>(); card.Build(this, data, index); cards.Add(card);
        }
        bool catalogueShown = !restSite || upgrading;
        previous.gameObject.SetActive(catalogueShown); next.gameObject.SetActive(catalogueShown); pageText.gameObject.SetActive(catalogueShown);
        pageText.text = indices.Count == 0 ? (CosmicLanguage.IsThai ? "ไม่มีการ์ดให้เลือก" : "NO CARDS AVAILABLE") : (page + 1) + " / " + ((indices.Count + 3) / 4);
    }
    private void RefreshControls()
    {
        if (back == null) return;
        back.interactable = cancelUpgrade.interactable = ControlsReady;
        previous.interactable = ControlsReady && page > 0; next.interactable = ControlsReady && (page + 1) * 4 < indices.Count;
        heal.interactable = ControlsReady && rest != null && rest.CanHeal;
        upgrade.interactable = ControlsReady && rest != null && rest.CanAct && rest.HasUpgradeableCard();
        foreach (var card in cards) card.RefreshState();
    }
    public bool CanUseCard(CardData card) => ControlsReady && (restSite ? rest.CanAct && card != null && card.UpgradedCard != null : shop.CanBuyCard(card));
    public string CardCaption(CardData card)
    {
        bool th = CosmicLanguage.IsThai;
        if (restSite) return (th ? "อัปเกรด: " : "UPGRADE: ") + CosmicCardText.Name(card.UpgradedCard);
        return (th ? "ซื้อ " : "BUY ") + (shop == null ? 0 : shop.Price) + (th ? " ทอง" : " GOLD") + (shop != null && !shop.CanBuyCard(card) ? (th ? " • ทองไม่พอ" : " • TOO COSTLY") : "");
    }
    public void ChangePage(int direction) { if (!ControlsReady) return; page += direction; Refresh(); }
    public void OpenUpgrades() { if (!ControlsReady || rest == null || !rest.CanAct || !rest.HasUpgradeableCard()) return; upgrading = true; page = 0; notice = null; Refresh(); }
    public void CloseUpgrades() { if (ModalOpen || CosmicSceneLoader.IsLoading) return; upgrading = false; notice = null; page = 0; Refresh(); }
    public void RequestCard(int index)
    {
        if (!ControlsReady) return;
        CardData card = restSite ? Run.Deck.GetCard(index) : index >= 0 && index < shop.Offers.Count ? shop.Offers[index] : null;
        if (!CanUseCard(card)) return;
        bool th = CosmicLanguage.IsThai;
        string message = restSite ? CosmicCardText.Name(card) + " → " + CosmicCardText.Name(card.UpgradedCard) + "\n" + CosmicCardText.Description(card) + "\n→ " + CosmicCardText.Description(card.UpgradedCard) :
            CosmicCardText.Name(card) + "\n" + CosmicCardText.Description(card) + "\n\n" + (th ? "ราคา " : "PRICE ") + shop.Price + (th ? " ทอง" : " GOLD");
        modal = CosmicModalDialog.Show(GetComponent<Canvas>(), Font, restSite ? (th ? "ยืนยันอัปเกรดการ์ด?" : "UPGRADE THIS CARD?") : (th ? "ยืนยันซื้อการ์ด?" : "PURCHASE THIS CARD?"), message,
            th ? "ยืนยัน" : "CONFIRM", () =>
            {
                bool success = restSite ? rest.TryUpgrade(index) : shop.BuyCard(card);
                notice = success ? (th ? "สำเร็จ: " : "DONE: ") + CosmicCardText.Name(restSite ? card.UpgradedCard : card) : (th ? "ทำรายการไม่ได้ กรุณาลองใหม่" : "Action unavailable. Please try again.");
                Refresh();
            }, th ? "ยกเลิก" : "CANCEL");
    }
    public void RequestHeal()
    {
        if (!ControlsReady || rest == null || !rest.CanHeal) return;
        bool th = CosmicLanguage.IsThai;
        modal = CosmicModalDialog.Show(GetComponent<Canvas>(), Font, th ? "พักฟื้นที่นี่?" : "REST AT THIS FIRE?",
            (th ? "ฟื้นฟู " : "Restore ") + rest.HealAmount + (th ? " HP\nเมื่อพักแล้วจะอัปเกรดการ์ดที่จุดพักนี้ไม่ได้" : " HP\nYou cannot upgrade a card at this rest site afterwards."),
            th ? "พักฟื้น" : "REST", () => { rest.TryHeal(); Refresh(); }, th ? "ยกเลิก" : "CANCEL");
    }
    public void RequestLeave()
    {
        if (!ControlsReady) return;
        if (!restSite) { shop.LeaveShop(); return; }
        if (rest.ActionUsed) { rest.LeaveRest(); return; }
        bool th = CosmicLanguage.IsThai;
        modal = CosmicModalDialog.Show(GetComponent<Canvas>(), Font, th ? "ออกจากจุดพัก?" : "LEAVE THIS REST SITE?",
            th ? "คุณยังไม่ได้พักหรืออัปเกรดการ์ด ต้องการเดินทางต่อเลยหรือไม่?" : "You have not rested or upgraded a card. Continue without using this rest site?",
            th ? "เดินทางต่อ" : "LEAVE", rest.LeaveRest, th ? "กลับไปเลือก" : "STAY");
    }
    public void ShowUnavailable()
    {
        if (unavailable) return; unavailable = true;
        bool th = CosmicLanguage.IsThai;
        CosmicModalDialog.Show(GetComponent<Canvas>(), Font, th ? "ยังไม่มีรอบเล่นสำหรับหน้านี้" : "NO ACTIVE ENCOUNTER",
            th ? "เริ่มจาก Main Menu แล้วเลือกด่านที่ตรงกันบนแผนที่" : "Start from Main Menu, then select the matching node on the map.",
            th ? "กลับเมนูหลัก" : "MAIN MENU", () => CosmicSceneLoader.Load("MainMenu"));
    }
}
