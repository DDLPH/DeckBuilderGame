using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public sealed class CosmicRewardUI : MonoBehaviour
{
    public TMP_FontAsset englishFont, thaiFont;
    public Sprite background;
    public AudioClip musicClip;
    [SerializeField] private RectTransform board, cardArea;
    [SerializeField] private TMP_Text title, subtitle, goldReceipt, floor, health, gold, deck, selection, saveHint;
    [SerializeField] private Button confirm, skip;
    private readonly List<CosmicRewardCard> cards = new List<CosmicRewardCard>();
    private RewardManager manager;
    private CosmicModalDialog confirmation;
    private bool unavailableShown;
    public int SelectedIndex { get; private set; } = -1;
    public TMP_FontAsset Font => CosmicLanguage.IsThai && thaiFont != null ? thaiFont : englishFont;
    private bool ConfirmationOpen => confirmation != null && confirmation.gameObject.activeSelf;
    public bool CanSelect => manager != null && manager.CanChoose && !ConfirmationOpen;
    public IReadOnlyList<CosmicRewardCard> Cards => cards;
    public Button ConfirmButton => confirm;
    public Button SkipButton => skip;

    private void Awake()
    {
        if (board == null) BuildLayout();
        confirm.onClick.AddListener(ConfirmSelection); skip.onClick.AddListener(RequestSkip);
        if (Application.isPlaying) StartCoroutine(EnterScreen());
    }
    private void OnEnable() { CosmicLanguage.Changed += Refresh; }
    private void OnDisable() { CosmicLanguage.Changed -= Refresh; }
    private void OnDestroy() { if (manager != null) manager.Changed -= Refresh; }

    public void Bind(RewardManager owner)
    {
        if (manager != null) manager.Changed -= Refresh;
        manager = owner; manager.Changed += Refresh;
    }

    public void BuildLayout()
    {
        board = BattleUIFactory.Rect("RewardBoard", transform, new Vector2(1920, 1080), Vector2.zero);
        var backdrop = BattleUIFactory.Image("Backdrop", board, new Vector2(1920, 1080), Vector2.zero, new Color(.63f, .58f, .75f));
        backdrop.sprite = background;
        BattleUIFactory.Image("Shade", board, new Vector2(1920, 1080), Vector2.zero, new Color(.005f, .003f, .022f, .72f));
        BattleUIFactory.Image("Hud", board, new Vector2(1920, 90), new Vector2(0, 495), CosmicUITheme.Panel);
        BattleUIFactory.Image("HudRule", board, new Vector2(1800, 2), new Vector2(0, 450), CosmicUITheme.Border);
        floor = BattleUIFactory.Text("Floor", board, new Vector2(330, 60), new Vector2(-730, 495), "", 28, Font);
        health = BattleUIFactory.Text("Health", board, new Vector2(400, 60), new Vector2(-280, 495), "", 28, Font);
        gold = BattleUIFactory.Text("Gold", board, new Vector2(330, 60), new Vector2(310, 495), "", 28, Font);
        deck = BattleUIFactory.Text("Deck", board, new Vector2(330, 60), new Vector2(745, 495), "", 28, Font);
        title = BattleUIFactory.Text("Title", board, new Vector2(1300, 85), new Vector2(0, 390), "VICTORY SPOILS", 50, Font);
        var rule = BattleUIFactory.Rect("TitleRule", board, new Vector2(860, 20), new Vector2(0, 335)).gameObject.AddComponent<AstralMapGraphic>();
        rule.motif = AstralMapGraphic.Motif.Divider; rule.color = CosmicUITheme.Border; rule.raycastTarget = false;
        subtitle = BattleUIFactory.Text("Subtitle", board, new Vector2(1300, 50), new Vector2(0, 300), "", 26, Font);
        var reward = BattleUIFactory.Image("GoldReceipt", board, new Vector2(590, 68), new Vector2(0, 235), CosmicUITheme.ButtonFill);
        CosmicUITheme.Frame(reward.transform, true);
        var coin = BattleUIFactory.Rect("Coin", reward.transform, new Vector2(40, 40), new Vector2(-240, 0)).gameObject.AddComponent<AstralMapGraphic>();
        coin.motif = AstralMapGraphic.Motif.Coin; coin.color = CosmicUITheme.Ivory; coin.raycastTarget = false;
        goldReceipt = BattleUIFactory.Text("GoldReceived", reward.transform, new Vector2(450, 56), new Vector2(20, 0), "", 28, Font);
        cardArea = BattleUIFactory.Rect("Cards", board, new Vector2(1450, 445), new Vector2(0, -42));
        selection = BattleUIFactory.Text("Selection", board, new Vector2(1350, 44), new Vector2(0, -303), "", 26, Font);
        confirm = BattleUIFactory.Button("ConfirmReward", board, new Vector2(490, 76), new Vector2(0, -374), "TAKE CARD", Font, null);
        confirm.GetComponent<CosmicButtonEffect>().primaryAction = true;
        skip = BattleUIFactory.Button("SkipReward", board, new Vector2(300, 54), new Vector2(0, -461), "SKIP CARD", Font, null);
        saveHint = BattleUIFactory.Text("SaveHint", board, new Vector2(1500, 34), new Vector2(0, -517), "", 22, Font);
        saveHint.color = CosmicUITheme.Muted;
    }

    public void ShowRewards()
    {
        BattleUIFactory.Clear(cardArea); cards.Clear();
        SelectedIndex = manager.Resolved ? manager.Receipt.ChosenIndex : -1;
        for (int i = 0; i < manager.Offers.Count; i++)
        {
            var rect = BattleUIFactory.Rect("RewardCard_" + i, cardArea, new Vector2(312, 438), new Vector2((i - (manager.Offers.Count - 1) * .5f) * 372, 0));
            var card = rect.gameObject.AddComponent<CosmicRewardCard>(); card.Build(this, manager.Offers[i], i); cards.Add(card);
        }
        Refresh();
        if (Application.isPlaying) CosmicAudio.Play(CosmicCue.Victory);
    }

    private IEnumerator EnterScreen()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("CosmicUI.Volume", 1));
        var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.clip = musicClip; source.spatialBlend = 0; source.loop = true; source.volume = 0;
        if (source.clip != null) source.Play();
        var group = board.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0; t < .65f; t += Time.unscaledDeltaTime)
        { group.alpha = Mathf.Clamp01(t / .4f); source.volume = .13f * t / .65f; yield return null; }
        group.alpha = 1; source.volume = .13f;
    }

    private void Update()
    {
        RefreshControls();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (ConfirmationOpen) confirmation.Close(false);
            else if (CanSelect) { SelectedIndex = -1; Refresh(); }
        }
    }
    private void RefreshControls()
    {
        if (confirm == null) return;
        bool ready = manager != null && manager.Ready && !CosmicSceneLoader.IsLoading && !ConfirmationOpen;
        confirm.interactable = ready && (manager.Resolved || manager.Offers.Count == 0 || SelectedIndex >= 0);
        skip.interactable = ready && !manager.Resolved;
    }

    public void Select(int index)
    {
        if (!CanSelect || index < 0 || index >= manager.Offers.Count) return;
        SelectedIndex = index; CosmicAudio.Play(CosmicCue.Card); Refresh();
    }
    public void ConfirmSelection()
    {
        if (manager == null || !manager.Ready || CosmicSceneLoader.IsLoading || ConfirmationOpen) return;
        if (manager.Resolved) { manager.LeaveReward(); return; }
        if (manager.Offers.Count == 0) { manager.SkipAndReturnToMap(); return; }
        if (SelectedIndex >= 0) manager.TakeAndReturnToMap(SelectedIndex);
    }
    public void RequestSkip()
    {
        if (!CanSelect) return;
        if (manager.Offers.Count == 0) { manager.SkipAndReturnToMap(); return; }
        bool th = CosmicLanguage.IsThai;
        confirmation = CosmicModalDialog.Show(GetComponent<Canvas>(), Font, th ? "ข้ามรางวัลการ์ด?" : "SKIP THIS CARD REWARD?",
            th ? "คุณยังได้รับทอง แต่จะไม่ได้รับการ์ดจากการต่อสู้ครั้งนี้ ต้องการข้ามหรือไม่?" :
            "You keep the gold, but will not receive a card from this battle. Skip this reward?",
            th ? "ข้ามการ์ด" : "SKIP CARD", manager.SkipAndReturnToMap, th ? "เลือกต่อ" : "KEEP CHOOSING");
    }

    public void Refresh()
    {
        if (title == null) return;
        bool th = CosmicLanguage.IsThai;
        foreach (TMP_Text text in board.GetComponentsInChildren<TMP_Text>(true)) text.font = Font;
        title.text = th ? "รางวัลจากการต่อสู้" : "VICTORY SPOILS";
        subtitle.text = th ? "เลือกการ์ดหนึ่งใบเพิ่มลงเด็ค หรือข้ามเพื่อเดินทางต่อ" : "Choose one card for your deck, or leave it behind.";
        saveHint.text = th ? "ทองและการ์ดจะถูกบันทึกเมื่อกลับสู่แผนที่" : "Gold and cards are saved when you return to the map.";
        bool valid = manager != null && manager.Ready;
        if (valid)
        {
            RunData run = manager.Run;
            floor.text = (th ? "ชั้น " : "FLOOR ") + run.CurrentNode.LayerIndex;
            health.text = (th ? "พลังชีวิต " : "HP ") + run.Player.CurrentHP + " / " + run.Player.MaxHP;
            gold.text = (th ? "ทอง " : "GOLD ") + run.Gold;
            deck.text = (th ? "เด็ค " : "DECK ") + run.Deck.Count;
            goldReceipt.text = "+" + manager.Receipt.GoldGranted + (th ? " ทอง — รับแล้ว" : " GOLD — COLLECTED");
            if (manager.Resolved)
            {
                SelectedIndex = manager.Receipt.ChosenIndex;
                selection.text = manager.Receipt.Skipped ? (th ? "ข้ามรางวัลการ์ดแล้ว" : "Card reward skipped.") :
                    (th ? "เพิ่มลงเด็คแล้ว: " : "Added to deck: ") + CosmicCardText.Name(manager.Offers[SelectedIndex]);
            }
            else selection.text = SelectedIndex >= 0 ? (th ? "เลือกไว้: " : "Selected: ") + CosmicCardText.Name(manager.Offers[SelectedIndex]) :
                (manager.Offers.Count == 0 ? (th ? "ไม่มีการ์ดในชุดรางวัลนี้ รับทองแล้วเดินทางต่อได้" : "No cards available. Keep the gold and continue.") :
                (th ? "คลิกการ์ดเพื่อดูและเลือก ก่อนกดยืนยันรับ" : "Click a card, then confirm to take it."));
        }
        else { floor.text = health.text = gold.text = deck.text = ""; goldReceipt.text = "—"; selection.text = ""; }
        confirm.GetComponentInChildren<TMP_Text>().text = valid && (manager.Resolved || manager.Offers.Count == 0) ?
            (th ? "กลับสู่แผนที่" : "RETURN TO MAP") : (th ? "รับการ์ดและเดินทางต่อ" : "TAKE CARD & CONTINUE");
        skip.GetComponentInChildren<TMP_Text>().text = th ? "ข้ามรางวัลการ์ด" : "SKIP CARD";
        skip.gameObject.SetActive(!valid || (!manager.Resolved && manager.Offers.Count > 0));
        foreach (var card in cards) card.Refresh();
        RefreshControls();
    }

    public void ShowUnavailable()
    {
        if (unavailableShown) return;
        unavailableShown = true; Refresh();
        bool th = CosmicLanguage.IsThai;
        CosmicModalDialog.Show(GetComponent<Canvas>(), Font, th ? "ยังไม่มีรางวัลการต่อสู้" : "NO BATTLE REWARD",
            th ? "เริ่มจากเมนูหลัก แล้วเอาชนะด่านธรรมดาหรือ Elite บนแผนที่ก่อน" : "Start from the menu, then win a normal or Elite battle on the map.",
            th ? "กลับเมนูหลัก" : "MAIN MENU", () => CosmicSceneLoader.Load("MainMenu"));
    }
}
