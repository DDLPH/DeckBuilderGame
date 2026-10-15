using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public sealed class CosmicBattleUI : MonoBehaviour
{
    public TMP_FontAsset englishFont, thaiFont;
    public Sprite background, wanderer, pilgrim, sovereign;
    public AudioClip musicClip, clickClip;
    public AudioClip bossMusicClip;
    [SerializeField] private RectTransform board, handArea, actorArea, dragLayer;
    [SerializeField] private TMP_Text floorText, goldText, encounterText, turnText, energyText, hintText;
    [SerializeField] private Button endTurn, drawButton, discardButton, exhaustButton, pauseButton;
    [SerializeField] private CanvasGroup boardGroup;
    private DeckManager deck;
    private EnemyManager enemies;
    private readonly List<CosmicBattleActor> actors = new List<CosmicBattleActor>();
    private AudioSource battleMusic;
    public bool ResultOpen { get; private set; }
    public AudioSource MusicSource => battleMusic;
    private CosmicBattleCard dragging;
    private bool busy, waitingConfirmation, missingRunShown;
    private GameObject overlay;
    private CosmicModalDialog confirmation;
    private float previousTimeScale = 1;
    private bool ownsPause;
    private string temporaryHint;
    private float hintUntil;
    private Vector2 pointer;
    public BattleManager Battle { get; private set; }
    public CosmicBattleCard Selected { get; private set; }
    public TMP_FontAsset Font => CosmicLanguage.IsThai && thaiFont != null ? thaiFont : englishFont;
    public bool IsDragging => dragging != null;
    public bool IsBusy => busy || overlay != null || waitingConfirmation;
    public RectTransform HandArea => handArea;
    public IReadOnlyList<CosmicBattleActor> Actors => actors;

    private void Awake()
    {
        if (board == null) BuildLayout();
        Bind();
        WireButtons();
        if (Application.isPlaying)
        {
            battleMusic = gameObject.AddComponent<AudioSource>();
            battleMusic.playOnAwake = false; battleMusic.loop = true; battleMusic.spatialBlend = 0;
            battleMusic.volume = 0;
            AudioListener.volume = PlayerPrefs.GetFloat("CosmicUI.Volume", 1);
            StartCoroutine(StartMusic());
        }
    }
    private void OnEnable() { CosmicLanguage.Changed += RefreshLanguage; }
    private void OnDisable() { CosmicLanguage.Changed -= RefreshLanguage; CancelSelection(); ResumeClock(); }
    private void OnDestroy()
    {
        if (Battle != null) { Battle.CardPlayed -= OnCardPlayed; Battle.EnemyActing -= OnEnemyActing; }
        ResumeClock();
    }

    public void Bind()
    {
        if (Battle != null) { Battle.CardPlayed -= OnCardPlayed; Battle.EnemyActing -= OnEnemyActing; }
        Battle = FindAnyObjectByType<BattleManager>();
        deck = FindAnyObjectByType<DeckManager>(); enemies = FindAnyObjectByType<EnemyManager>();
        if (Battle != null) { Battle.CardPlayed += OnCardPlayed; Battle.EnemyActing += OnEnemyActing; }
    }

    private void WireButtons()
    {
        endTurn.onClick.AddListener(() => { if (CanInput) { CancelSelection(); CosmicAudio.Play(CosmicCue.Turn); Battle.EndPlayerTurn(); } });
        drawButton.onClick.AddListener(() => OpenPile(0));
        discardButton.onClick.AddListener(() => OpenPile(1));
        exhaustButton.onClick.AddListener(() => OpenPile(2));
        pauseButton.onClick.AddListener(OpenPause);
        foreach (Button button in board.GetComponentsInChildren<Button>())
        {
            CosmicButtonEffect effect = button.GetComponent<CosmicButtonEffect>();
            if (effect != null) effect.clickSound = null; // One sound owner, no duplicate click.
        }
    }

    public void BuildLayout()
    {
        board = BattleUIFactory.Rect("BattleBoard", transform, new Vector2(1920, 1080), Vector2.zero);
        boardGroup = board.gameObject.AddComponent<CanvasGroup>();
        var back = BattleUIFactory.Image("Backdrop", board, new Vector2(1920, 1080), Vector2.zero, new Color(.78f, .75f, .88f));
        back.sprite = background;
        BattleUIFactory.Image("TopShade", board, new Vector2(1920, 82), new Vector2(0, 499), CosmicUITheme.Panel);
        BattleUIFactory.Image("TopRule", board, new Vector2(1840, 2.5f), new Vector2(0, 457), CosmicUITheme.Border);
        floorText = BattleUIFactory.Text("Floor", board, new Vector2(260, 60), new Vector2(-780, 497), "FLOOR 1", 27, Font);
        goldText = BattleUIFactory.Text("Gold", board, new Vector2(270, 60), new Vector2(-500, 497), "GOLD 100", 27, Font);
        encounterText = BattleUIFactory.Text("Encounter", board, new Vector2(620, 60), new Vector2(0, 497), "BATTLE", 30, Font);
        pauseButton = BattleUIFactory.Button("PauseButton", board, new Vector2(160, 52), new Vector2(820, 497), "PAUSE", Font, null);
        turnText = BattleUIFactory.Text("Turn", board, new Vector2(320, 80), new Vector2(-135, 270), "YOUR TURN", 29, Font);
        actorArea = BattleUIFactory.Rect("Actors", board, new Vector2(1920, 1080), Vector2.zero);
        var tray = BattleUIFactory.Image("HandTray", board, new Vector2(1920, 378), new Vector2(0, -351), CosmicUITheme.Panel);
        BattleUIFactory.Image("HandRule", tray.transform, new Vector2(1840, 2.5f), new Vector2(0, 189), CosmicUITheme.Border);
        handArea = BattleUIFactory.Rect("Hand", board, new Vector2(1300, 340), new Vector2(0, -347));
        energyText = BattleUIFactory.Text("Energy", board, new Vector2(250, 110), new Vector2(-806, -264), "3 / 3", 46, Font);
        drawButton = BattleUIFactory.Button("DrawPile", board, new Vector2(228, 60), new Vector2(-806, -397), "DRAW 5", Font, null);
        discardButton = BattleUIFactory.Button("DiscardPile", board, new Vector2(228, 60), new Vector2(806, -285), "DISCARD 0", Font, null);
        exhaustButton = BattleUIFactory.Button("ExhaustPile", board, new Vector2(228, 54), new Vector2(806, -354), "EXHAUST 0", Font, null);
        endTurn = BattleUIFactory.Button("EndTurn", board, new Vector2(228, 80), new Vector2(806, -454), "END TURN", Font, null);
        endTurn.GetComponent<CosmicButtonEffect>().primaryAction = true;
        hintText = BattleUIFactory.Text("Hint", board, new Vector2(1380, 40), new Vector2(0, -127), "", 24, Font);
        hintText.color = CosmicUITheme.Ivory;
        dragLayer = BattleUIFactory.Rect("DragLayer", board, new Vector2(1920, 1080), Vector2.zero);
    }

    private bool CanInput => Battle != null && Battle.CanInteract && !IsBusy && !IsDragging && !ResultOpen && !CosmicSceneLoader.IsLoading;

    private IEnumerator StartMusic()
    {
        yield return null; // RunManager and encounter data are ready after scene startup.
        RunManager run = FindAnyObjectByType<RunManager>();
        bool boss = run != null && run.IsCurrentNodeBoss();
        AudioClip bossTrack = bossMusicClip != null ? bossMusicClip : CosmicAudio.BossMusic;
        battleMusic.clip = boss && bossTrack != null ? bossTrack : musicClip;
        if (battleMusic.clip == null) yield break;
        battleMusic.Play();
        for (float t = 0; t < .8f && !ResultOpen; t += Time.unscaledDeltaTime)
        { battleMusic.volume = (boss ? .28f : .15f) * t / .8f; yield return null; }
        if (!ResultOpen) battleMusic.volume = boss ? .28f : .15f;
    }

    public void PrepareRunResult(bool won)
    {
        if (ResultOpen) return;
        CloseOverlay(); CancelSelection(); ResultOpen = true;
        boardGroup.interactable = false; boardGroup.blocksRaycasts = false;
        CosmicAudio.Play(won ? CosmicCue.Victory : CosmicCue.Defeat);
        if (battleMusic != null) StartCoroutine(FadeResultMusic());
    }
    private IEnumerator FadeResultMusic()
    {
        float volume = battleMusic.volume;
        for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
        { battleMusic.volume = volume * (1 - t / .5f); yield return null; }
        battleMusic.Stop();
    }

    public void RefreshHand()
    {
        if (board == null || deck == null || Battle == null) { if (board != null) Bind(); else return; }
        if (deck == null || handArea == null) return;
        Selected = null; dragging = null;
        BattleUIFactory.Clear(handArea);
        int count = deck.Hand.Count;
        // Five cards have full spacing; larger hands compress within the safe hand region.
        float spacing = count <= 1 ? 0 : Mathf.Min(242, 1120f / (count - 1));
        for (int i = 0; i < count; i++)
        {
            if (deck.Hand[i] == null) continue;
            RectTransform rect = BattleUIFactory.Rect("Card_" + i, handArea, new Vector2(212, 330), new Vector2((i - (count - 1) * .5f) * spacing, 0));
            var view = rect.gameObject.AddComponent<CosmicBattleCard>();
            view.Build(this, deck.Hand[i], i);
        }
        EnsureActors(); RefreshLanguage();
    }

    public void EnsureActors()
    {
        if (Battle == null || enemies == null || actorArea == null) return;
        if (actors.Count > 0) return;
        if (enemies.Enemies.Count == 0 || Battle.Player == null) return;
        CreateActor(Battle.Player, wanderer, "WANDERER", "นักเดินทาง", new Vector2(-570, 155), false);
        RunManager run = FindAnyObjectByType<RunManager>();
        MapNodeType nodeType = run != null && run.CurrentRun != null && run.CurrentRun.CurrentNode != null
            ? run.CurrentRun.CurrentNode.NodeType : MapNodeType.NormalBattle;
        int count = enemies.Enemies.Count;
        for (int i = 0; i < count; i++)
        {
            bool boss = enemies.Enemies[i].name.StartsWith("Boss", System.StringComparison.OrdinalIgnoreCase);
            bool elite = enemies.Enemies[i].name.StartsWith("Elite", System.StringComparison.OrdinalIgnoreCase);
            float x = count == 1 ? 480 : (count == 2 ? 260 + i * 430 : 80 + i * 330);
            CreateActor(enemies.Enemies[i], boss ? sovereign : pilgrim,
                boss ? "ECLIPSE SOVEREIGN" : elite ? "ASTRAL PILGRIM" : "LOST PILGRIM",
                boss ? "ราชันแห่งคราส" : elite ? "ผู้แสวงบุญหลงดาว" : "ผู้แสวงบุญหลงทาง",
                new Vector2(x, 155), boss);
        }
    }

    private void CreateActor(CharacterData data, Sprite sprite, string en, string th, Vector2 position, bool boss)
    {
        RectTransform rect = BattleUIFactory.Rect("Actor_" + data.name, actorArea, new Vector2(boss ? 520 : 320, 590), position);
        var actor = rect.gameObject.AddComponent<CosmicBattleActor>();
        actor.Build(this, data, sprite, en, th, boss); actors.Add(actor);
    }

    public string CardName(CardData data)
    {
        if (!CosmicLanguage.IsThai) return data.CardName;
        bool upgraded = data.UpgradedCard == null && data.name.Contains("+");
        if (data.name.StartsWith("Strike")) return "ฟันโจมตี" + (upgraded ? "+" : "");
        if (data.name.StartsWith("Defend")) return "ตั้งรับ" + (upgraded ? "+" : "");
        switch (data.name)
        {
            case "AstralCut": return "คมดาบดารา";
            case "MoonWard": return "ม่านจันทรา";
            case "Starfall": return "ดาวถล่ม";
            case "Eclipse": return "คมคราส";
            case "Insight": return "หยั่งรู้";
            case "Focus": return "รวมสมาธิ";
        }
        return data.CardName;
    }
    public string CardDescription(CardData data)
    {
        var lines = new List<string>();
        if (data.Effects != null) foreach (CardEffect effect in data.Effects)
        {
            if (effect is DamageEffect damage) lines.Add(CosmicLanguage.IsThai ? "สร้างความเสียหาย " + damage.DamageAmount : "Deal " + damage.DamageAmount + " damage.");
            else if (effect is BlockEffect block) lines.Add(CosmicLanguage.IsThai ? "ได้รับเกราะ " + block.BlockAmount : "Gain " + block.BlockAmount + " Block.");
            else if (effect is DrawCardsEffect draw) lines.Add(CosmicLanguage.IsThai ? "จั่ว " + draw.Amount + " ใบ" : "Draw " + draw.Amount + " cards.");
            else if (effect is GainEnergyEffect energy) lines.Add(CosmicLanguage.IsThai ? "ได้รับพลังงาน " + energy.Amount : "Gain " + energy.Amount + " Energy.");
            else if (effect != null) return data.Description;
        }
        if (data.Exhausts) lines.Add(CosmicLanguage.IsThai ? "เผาทิ้งหลังใช้" : "Exhaust.");
        if (BattleManager.TargetsEnemySide(data) && !BattleManager.NeedsEnemy(data)) lines.Add(CosmicLanguage.IsThai ? "ศัตรูทั้งหมด" : "All enemies.");
        return lines.Count == 0 ? data.Description : string.Join("\n", lines);
    }

    private void Update()
    {
        if (waitingConfirmation && confirmation == null) { waitingConfirmation = false; ResumeClock(); }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (confirmation != null) confirmation.Close(false);
                else if (overlay != null) CloseOverlay();
                else if (Selected != null || IsDragging) CancelSelection();
                else OpenPause();
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !busy) CancelSelection();
        }
        if (Battle == null) return;
        EnsureActors();
        bool thai = CosmicLanguage.IsThai;
        RunManager run = FindAnyObjectByType<RunManager>();
        if (run != null && run.CurrentRun != null)
        {
            if (run.CurrentRun.CurrentNode == null) return;
            floorText.text = (thai ? "ชั้น " : "FLOOR ") + run.CurrentRun.CurrentNode.LayerIndex;
            goldText.text = (thai ? "ทอง " : "GOLD ") + run.CurrentRun.Gold;
            MapNodeType kind = run.CurrentRun.CurrentNode.NodeType;
            encounterText.text = kind == MapNodeType.Boss ? (thai ? "ศึกบอส" : "BOSS ENCOUNTER") :
                kind == MapNodeType.Elite ? (thai ? "ศึกอีลิท" : "ELITE ENCOUNTER") : (thai ? "การต่อสู้" : "BATTLE");
        }
        turnText.text = Battle.State == BattleState.End ? (thai ? "การต่อสู้สิ้นสุด" : "BATTLE ENDED") :
            (Battle.State == BattleState.PlayerTurn ? (thai ? "เทิร์นของคุณ" : "YOUR TURN") : (thai ? "เทิร์นศัตรู" : "ENEMY TURN")) +
            "\n<size=70%>" + (thai ? "รอบ " : "TURN ") + Battle.TurnNumber + "</size>";
        if (Battle.Player != null) energyText.text = Battle.Player.CurrentEnergy + " / " + Battle.Player.BaseEnergy + "\n<size=45%>" + (thai ? "พลังงาน" : "ENERGY") + "</size>";
        if (deck != null)
        {
            Caption(drawButton, (thai ? "กองจั่ว " : "DRAW ") + deck.DrawPile.Count);
            Caption(discardButton, (thai ? "กองทิ้ง " : "DISCARD ") + deck.DiscardPile.Count);
            Caption(exhaustButton, (thai ? "กองเผา " : "EXHAUST ") + deck.ExhaustPile.Count);
        }
        endTurn.interactable = CanInput;
        pauseButton.interactable = !busy && !IsDragging && Battle.State != BattleState.End;
        hintText.text = Time.unscaledTime < hintUntil ? temporaryHint : Selected != null || IsDragging ?
            (thai ? "ปล่อยบนเป้าหมายที่เรืองแสง • คลิกขวาหรือ Esc เพื่อยกเลิก" : "Choose a glowing target • Right click or Esc to cancel") :
            (thai ? "ลากการ์ดไปยังเป้าหมาย หรือคลิกการ์ดแล้วคลิกเป้าหมาย" : "Drag a card onto its target, or click a card then a target");
    }

    public void RefreshLanguage()
    {
        if (board == null) return;
        foreach (TMP_Text text in board.GetComponentsInChildren<TMP_Text>(true)) text.font = Font;
        Caption(endTurn, CosmicLanguage.IsThai ? "จบเทิร์น" : "END TURN");
        Caption(pauseButton, CosmicLanguage.IsThai ? "พักเกม" : "PAUSE");
        foreach (var card in handArea.GetComponentsInChildren<CosmicBattleCard>()) card.RefreshLanguage();
        foreach (var actor in actors) actor.Refresh();
    }
    private static void Caption(Button button, string value) { button.GetComponentInChildren<TMP_Text>().text = value; }
    private void ClickSound() { CosmicAudio.Play(CosmicCue.Card); }
    public void Hint(string en, string th) { temporaryHint = CosmicLanguage.IsThai ? th : en; hintUntil = Time.unscaledTime + 2; CosmicAudio.Play(CosmicCue.Reject); }

    public void Select(CosmicBattleCard card)
    {
        if (!CanInput || !Battle.CanUseCard(card.Data)) { Hint("Not enough energy or not your turn.", "พลังงานไม่พอ หรือยังไม่ใช่เทิร์นของคุณ"); return; }
        Selected = Selected == card ? null : card; ClickSound();
    }
    public bool BeginDrag(CosmicBattleCard card)
    {
        if (!CanInput || !Battle.CanUseCard(card.Data)) { Hint("Not enough energy or not your turn.", "พลังงานไม่พอ หรือยังไม่ใช่เทิร์นของคุณ"); return false; }
        Selected = card; dragging = card; Battle.InteractionLocked = true;
        return true;
    }
    public void TrackDrag(Vector2 value) { pointer = value; }
    public CosmicBattleActor TargetAt(Vector2 screenPoint)
    {
        Canvas canvas = GetComponent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        foreach (CosmicBattleActor actor in actors)
            if (actor.Character != null && !actor.Character.IsDead() && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)actor.transform, screenPoint, camera)) return actor;
        return null;
    }
    public bool EndDrag(CosmicBattleCard card, Vector2 point)
    {
        dragging = null; Battle.InteractionLocked = false;
        CosmicBattleActor target = TargetAt(point);
        if (!CanInput || !IsValidTarget(card.Data, target)) { Selected = null; Hint("Release on a valid target.", "ปล่อยการ์ดบนเป้าหมายที่ใช้ได้"); return false; }
        StartCoroutine(PlayMotion(card, target)); return true;
    }
    public bool IsValidTarget(CardData data, CosmicBattleActor actor)
    {
        if (Battle == null || actor == null || actor.Character == null || actor.Character.IsDead()) return false;
        if (BattleManager.TargetsEnemySide(data) != (actor.Enemy != null)) return false;
        return Battle.CanUseCardOn(data, BattleManager.NeedsEnemy(data) ? actor.Enemy : null);
    }
    public bool IsValidHighlight(CosmicBattleActor actor)
    {
        if (Selected == null || IsBusy || Battle == null) return false;
        // Drag locks keyboard/end-turn, not visual feedback.
        if (!IsDragging) return IsValidTarget(Selected.Data, actor);
        return Battle.State == BattleState.PlayerTurn && !actor.Character.IsDead() &&
            BattleManager.TargetsEnemySide(Selected.Data) == (actor.Enemy != null) && TargetAt(pointer) == actor;
    }
    public void ClickActor(CosmicBattleActor actor)
    {
        if (CanInput && Selected != null && IsValidTarget(Selected.Data, actor)) StartCoroutine(PlayMotion(Selected, actor));
    }
    private IEnumerator PlayMotion(CosmicBattleCard card, CosmicBattleActor target)
    {
        busy = true; Battle.InteractionLocked = true; Selected = null;
        CardData data = card.Data;
        card.SetAnimating();
        card.transform.SetParent(dragLayer, true);
        Vector3 start = card.transform.position, end = target.transform.position;
        var group = card.GetComponent<CanvasGroup>(); group.blocksRaycasts = false;
        for (float t = 0; t < .20f; t += Time.deltaTime)
        {
            card.transform.position = Vector3.Lerp(start, end, t / .20f);
            card.transform.localScale = Vector3.one * Mathf.Lerp(1, .35f, t / .20f);
            group.alpha = 1 - t / .20f; yield return null;
        }
        Battle.InteractionLocked = false;
        bool played = Battle.TryPlayCard(data, BattleManager.NeedsEnemy(data) ? target.Enemy : null);
        if (card != null) Destroy(card.gameObject);
        if (!played) { Hint("Card cancelled; no energy spent.", "ยกเลิกการ์ดแล้ว ไม่เสียพลังงาน"); RefreshHand(); }
        // Keep control locked while the hit/Block feedback finishes.
        Battle.InteractionLocked = true;
        yield return new WaitForSecondsRealtime(.16f);
        Battle.InteractionLocked = false; busy = false;
    }
    private void OnCardPlayed(CardData card, EnemyData target)
    {
        CosmicAudio.Play(BattleManager.TargetsEnemySide(card) ? CosmicCue.Attack : CosmicCue.Guard);
        if (actors.Count > 0) actors[0].Act(); foreach (var actor in actors) actor.Refresh();
    }
    private void OnEnemyActing(EnemyData enemy)
    { CosmicAudio.Play(enemy.CurrentAction != null && enemy.CurrentAction.ActionType == EnemyActionType.Defend ? CosmicCue.Guard : CosmicCue.Attack); foreach (var actor in actors) if (actor.Enemy == enemy) actor.Act(); }
    public void CancelSelection()
    {
        if (dragging != null) dragging.CancelDrag();
        dragging = null; Selected = null;
        if (Battle != null && !busy) Battle.InteractionLocked = false;
    }
    private void OnApplicationFocus(bool focus) { if (!focus && !busy) CancelSelection(); }

    private void PauseClock()
    {
        if (ownsPause) return;
        ownsPause = true; previousTimeScale = Time.timeScale; Time.timeScale = 0;
        if (Battle != null) Battle.SetPaused(true);
    }
    private void ResumeClock()
    {
        if (!ownsPause) return;
        Time.timeScale = previousTimeScale; ownsPause = false;
        if (Battle != null) Battle.SetPaused(false);
    }
    private RectTransform OpenOverlay(string title)
    {
        CancelSelection(); PauseClock(); boardGroup.interactable = false; boardGroup.blocksRaycasts = false;
        var shade = BattleUIFactory.Image("BattleOverlay", transform, new Vector2(1920, 1080), Vector2.zero, CosmicUITheme.Scrim, true);
        var shadeRect = shade.rectTransform; shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one; shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        overlay = shade.gameObject;
        var panel = BattleUIFactory.Image("Panel", overlay.transform, new Vector2(840, 720), Vector2.zero, CosmicUITheme.Panel, true);
        CosmicUITheme.StylePanel(panel.transform);
        BattleUIFactory.Text("Title", panel.transform, new Vector2(740, 70), new Vector2(0, 280), title, 34, Font);
        BattleUIFactory.Button("Close", panel.transform, new Vector2(260, 62), new Vector2(0, -283), CosmicLanguage.IsThai ? "กลับ" : "BACK", Font, CloseOverlay);
        return panel.rectTransform;
    }
    public void CloseOverlay()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
            if (Application.isPlaying) Destroy(overlay); else DestroyImmediate(overlay);
            overlay = null;
        }
        if (boardGroup != null) { boardGroup.interactable = true; boardGroup.blocksRaycasts = true; }
        ResumeClock();
    }
    public void OpenPause()
    {
        if (IsBusy || IsDragging || busy || Battle == null || Battle.State == BattleState.End) return;
        bool th = CosmicLanguage.IsThai;
        var panel = OpenOverlay(th ? "พักเกม" : "PAUSED");
        BattleUIFactory.Text("Help", panel, new Vector2(730, 170), new Vector2(0, 110),
            th ? "ลากการ์ดไปยังเป้าหมาย หรือคลิกการ์ดแล้วคลิกเป้าหมาย\nคลิกขวา / Esc เพื่อยกเลิก\nบันทึกเฉพาะจุดปลอดภัย ไม่บันทึกกลางการต่อสู้" :
            "Drag cards to targets, or click a card then a target.\nRight click / Esc cancels selection.\nProgress saves at checkpoints, not during battle.", 25, Font);
        BattleUIFactory.Button("Resume", panel, new Vector2(360, 70), new Vector2(0, -35), th ? "เล่นต่อ" : "RESUME", Font, CloseOverlay);
        BattleUIFactory.Button("MainMenu", panel, new Vector2(360, 70), new Vector2(0, -130), th ? "กลับเมนูหลัก" : "MAIN MENU", Font, ConfirmMenu);
    }
    private void ConfirmMenu()
    {
        CloseOverlay(); PauseClock(); waitingConfirmation = true;
        bool th = CosmicLanguage.IsThai;
        confirmation = CosmicModalDialog.Show(GetComponent<Canvas>(), Font, th ? "ออกจากการต่อสู้?" : "LEAVE BATTLE?",
            th ? "การต่อสู้นี้จะไม่ถูกบันทึก เมื่อเล่นต่อจะกลับไปจุดเซฟก่อนเข้าด่าน ต้องการออกหรือไม่?" :
            "This battle will not be saved. Continue restores the checkpoint from before this encounter. Leave now?",
            th ? "ออก" : "LEAVE", () => { waitingConfirmation = false; ResumeClock(); CosmicSceneLoader.Load("MainMenu"); }, th ? "ยกเลิก" : "CANCEL");
    }
    public void OpenPile(int kind)
    {
        if (IsBusy || IsDragging || busy || deck == null || Battle.State == BattleState.End) return;
        IReadOnlyList<CardData> pile = kind == 0 ? deck.DrawPile : kind == 1 ? deck.DiscardPile : deck.ExhaustPile;
        string title = kind == 0 ? (CosmicLanguage.IsThai ? "กองจั่ว" : "DRAW PILE") : kind == 1 ?
            (CosmicLanguage.IsThai ? "กองทิ้ง" : "DISCARD PILE") : (CosmicLanguage.IsThai ? "กองเผา" : "EXHAUST PILE");
        var panel = OpenOverlay(title + " — " + pile.Count);
        var viewport = BattleUIFactory.Rect("Viewport", panel, new Vector2(700, 400), new Vector2(0, 0));
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .001f);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = viewport;
        var content = BattleUIFactory.Rect("Content", viewport, new Vector2(680, Mathf.Max(400, pile.Count * 48)), Vector2.zero);
        content.anchorMin = content.anchorMax = new Vector2(.5f, 1); content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        scroll.content = content; scroll.movementType = ScrollRect.MovementType.Clamped;
        var names = new List<string>(); foreach (CardData card in pile) if (card != null) names.Add(CardName(card));
        // Alphabetical display deliberately hides the future random draw order.
        names.Sort(System.StringComparer.Ordinal);
        for (int i = 0; i < names.Count; i++)
        {
            var label = BattleUIFactory.Text("CardName", content, new Vector2(640, 43), Vector2.zero, names[i], 26, Font, TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1);
            label.rectTransform.anchoredPosition = new Vector2(0, -24 - i * 48);
        }
        if (names.Count == 0) BattleUIFactory.Text("Empty", viewport, new Vector2(600, 100), Vector2.zero, CosmicLanguage.IsThai ? "ยังไม่มีการ์ดในกองนี้" : "This pile is empty.", 27, Font);
    }
    public void ShowMissingRun()
    {
        if (missingRunShown) return;
        missingRunShown = true;
        CosmicModalDialog.Show(GetComponent<Canvas>(), Font, CosmicLanguage.IsThai ? "ยังไม่มีรอบเล่น" : "NO ACTIVE RUN",
            CosmicLanguage.IsThai ? "กรุณาเริ่มจาก Main Menu → New Run แล้วเลือกด่านบน Map" : "Start from Main Menu → New Run, then choose a battle on the map.",
            CosmicLanguage.IsThai ? "ไปเมนูหลัก" : "MAIN MENU", () => CosmicSceneLoader.Load("MainMenu"));
    }
}
