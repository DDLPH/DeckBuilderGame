#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Editor-only opt-in smoke test. It cannot run in builds or ordinary Play mode.
public sealed class CosmicBattlePlayHarness : MonoBehaviour
{
    private RunManager fixture;
    private CardData strike, defend;
    private readonly Dictionary<string, string> saveHashes = new Dictionary<string, string>();
    private bool sawError;
    private string isolatedDirectory;
    private bool hadLanguage;
    private int originalLanguage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cosmic-battle-smoke") < 0) return;
        if (FindAnyObjectByType<CosmicBattlePlayHarness>() != null) return;
        var root = new GameObject("BattleSmokeHarness"); DontDestroyOnLoad(root);
        var harness = root.AddComponent<CosmicBattlePlayHarness>();
        harness.strike = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Strike.asset");
        harness.defend = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Defend.asset");
        harness.fixture = new GameObject("SmokeRunOnly").AddComponent<RunManager>();
        harness.fixture.enabled = false; // Prevent new-run startup and checkpoint writes.
        harness.Configure(MapNodeType.NormalBattle);
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
        {
            string path = Path.Combine(Application.persistentDataPath, "run-save.json" + suffix);
            harness.saveHashes[path] = Digest(path);
        }
        harness.isolatedDirectory = Path.Combine(Path.GetTempPath(), "cosmic-battle-check-" + Guid.NewGuid().ToString("N"));
        harness.hadLanguage = PlayerPrefs.HasKey("CosmicUI.Language");
        harness.originalLanguage = PlayerPrefs.GetInt("CosmicUI.Language", 0);
        Directory.CreateDirectory(harness.isolatedDirectory);
        RunSaveSystem.EditorTestDirectory = harness.isolatedDirectory;
        Application.logMessageReceived += harness.OnLog;
        harness.StartCoroutine(harness.Timeout());
    }

    private void Configure(MapNodeType kind)
    {
        if (fixture == null)
        {
            fixture = new GameObject("SmokeRunOnly").AddComponent<RunManager>();
            fixture.enabled = false;
        }
        var run = new RunData(); var map = new MapData();
        MapNode previous = null, selected = null;
        for (int layer = 0; layer <= 6; layer++)
        {
            var node = new MapNode(layer == 6 ? MapNodeType.Boss : layer == 1 && kind != MapNodeType.Boss ? kind : MapNodeType.NormalBattle, layer);
            map.AddNode(node);
            if (previous == null) map.SetStartNode(node); else previous.AddConnection(node);
            if (layer == (kind == MapNodeType.Boss ? 6 : 1)) selected = node;
            previous = node;
        }
        run.SetMap(map); run.SetCurrentNode(selected);
        for (int i = 0; i < 5; i++) { run.Deck.AddCard(strike); run.Deck.AddCard(defend); }
        run.AddGold(100); run.StartRun();
        typeof(RunManager).GetProperty("CurrentRun").SetValue(fixture, run);
    }

    private void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) sawError = true; }

    private IEnumerator Start()
    {
        IEnumerator steps = Steps();
        while (true)
        {
            bool more;
            try { more = steps.MoveNext(); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); yield break; }
            if (!more) break;
            yield return steps.Current;
        }
        try
        {
            foreach (var pair in saveHashes) Require(Digest(pair.Key) == pair.Value, "Player save changed during smoke test.");
            Require(!sawError, "Unexpected Unity error was logged during smoke test.");
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); yield break; }
        Debug.Log("Battle PLAY checks passed: pointer drag, click/self guard, invalid drops, pause, all encounters, Reward -> Map -> save -> Continue, boss victory and defeat result visibility/raycast/menu, missing-run recovery, asynchronous loading, boss music and single click audio. Player saves unchanged.");
        Application.logMessageReceived -= OnLog;
        Cleanup();
        EditorApplication.Exit(0);
    }

    private IEnumerator Steps()
    {
        foreach (MapNodeType kind in new[] { MapNodeType.NormalBattle, MapNodeType.Elite, MapNodeType.Boss })
        {
            if (kind != MapNodeType.NormalBattle)
            {
                Configure(kind); SceneManager.LoadScene("BattleScene");
            }
            yield return new WaitForSeconds(1);
            var ui = FindAnyObjectByType<CosmicBattleUI>(); var battle = ui.Battle;
            var deck = FindAnyObjectByType<DeckManager>(); var enemies = FindAnyObjectByType<EnemyManager>();
            Require(battle.State == BattleState.PlayerTurn && deck.Hand.Count == 5, "Battle did not start.");
            Require(ui.MusicSource != null && ui.MusicSource.isPlaying, "Battle music did not start.");
            Require(kind != MapNodeType.Boss || ui.MusicSource.clip == CosmicAudio.BossMusic, "Boss used the ordinary battle track.");
            var hand = (List<CardData>)typeof(DeckManager).GetField("hand", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(deck);
            hand.Clear(); hand.Add(strike); hand.Add(defend); ui.RefreshHand();
            var enemy = enemies.Enemies[0]; enemy.SetMaxHP(999); enemy.SetCurrentHP(999);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            var card = ui.HandArea.GetChild(0).GetComponent<CosmicBattleCard>();
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, card.transform.position);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.beginDragHandler);
            Require(card.Dragging && battle.InteractionLocked, "Pointer begin-drag failed.");
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, ui.Actors[1].transform.position);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.endDragHandler);
            yield return new WaitForSeconds(.65f);
            int damage = ((DamageEffect)strike.Effects[0]).DamageAmount;
            Require(enemy.GetCurrentHP() == 999 - damage && battle.Player.CurrentEnergy == 2 && hand.Count == 1, "Drag attack did not resolve exactly once.");
            card = ui.HandArea.GetChild(0).GetComponent<CosmicBattleCard>();
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Require(ui.Selected == card, "Click did not select Guard.");
            ExecuteEvents.Execute(ui.Actors[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSeconds(.65f);
            Require(battle.Player.GetBlock() == 5 && battle.Player.CurrentEnergy == 1 && hand.Count == 0, "Click/self Guard failed.");

            hand.Add(strike); ui.RefreshHand(); card = ui.HandArea.GetChild(0).GetComponent<CosmicBattleCard>();
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = new Vector2(-1000, -1000);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.endDragHandler);
            yield return new WaitForSeconds(.3f);
            Require(!ui.IsDragging && !battle.InteractionLocked && hand.Count == 1 && battle.Player.CurrentEnergy == 1, "Rejected drag consumed resources or stuck.");
            battle.EndPlayerTurn(); yield return new WaitForSeconds(.05f);
            int hp = battle.Player.GetCurrentHP(); ui.OpenPause();
            yield return new WaitForSecondsRealtime(.5f);
            Require(Time.timeScale == 0 && battle.Player.GetCurrentHP() == hp, "Enemy action continued during pause.");
            ui.CloseOverlay(); yield return new WaitForSeconds(3);
            Require(Time.timeScale == 1 && battle.State == BattleState.PlayerTurn && battle.TurnNumber == 2 && deck.Hand.Count == 5 && battle.Player.CurrentEnergy == 3, "Next turn failed to restore resources.");
            Debug.Log("Battle PLAY case passed: " + kind);
        }
        Configure(MapNodeType.NormalBattle); SceneManager.LoadScene("BattleScene");
        yield return new WaitForSeconds(1);
        var finalBattle = FindAnyObjectByType<BattleManager>();
        var finalDeck = FindAnyObjectByType<DeckManager>();
        var finalEnemies = FindAnyObjectByType<EnemyManager>();
        var finalHand = (List<CardData>)typeof(DeckManager).GetField("hand", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(finalDeck);
        var targets = new List<EnemyData>(finalEnemies.Enemies);
        finalBattle.Player.SetCurrentHP(37);
        foreach (EnemyData target in targets)
        {
            target.SetCurrentHP(1); finalHand.Clear(); finalHand.Add(strike); finalBattle.Player.ResetEnergy();
            Require(finalBattle.TryPlayCard(strike, target), "Victory setup attack failed.");
        }
        yield return new WaitForSeconds(1);
        Require(SceneManager.GetActiveScene().name == "RewardScene", "Normal victory did not enter Reward.");
        var reward = FindAnyObjectByType<RewardManager>();
        Require(reward != null && fixture.CurrentRun.Gold == 125 && fixture.CurrentRun.Player.CurrentHP == 37, "Gold reward or post-battle health missing.");
        var choices = new List<CardData>(reward.Offers);
        SceneManager.LoadScene("RewardScene"); yield return new WaitForSeconds(1);
        reward = FindAnyObjectByType<RewardManager>();
        Require(fixture.CurrentRun.Gold == 125, "Reloading Reward paid gold twice.");
        for (int i = 0; i < choices.Count; i++) Require(choices[i] == reward.Offers[i], "Reloading Reward rerolled the offers.");
        var rewardUI = FindAnyObjectByType<CosmicRewardUI>();
        Require(rewardUI != null && !rewardUI.ConfirmButton.interactable, "New Reward screen is missing or accepts an unselected card.");
        rewardUI.Cards[0].Button.onClick.Invoke();
        Require(fixture.CurrentRun.Deck.Count == 10 && rewardUI.ConfirmButton.interactable, "Reward preview mutated the deck.");
        yield return new WaitForSecondsRealtime(.3f);
        Capture(rewardUI.GetComponent<Canvas>(), "rewards-play-selected.png");
        rewardUI.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; rewardUI.GetComponent<Canvas>().worldCamera = null;
        yield return null;
        rewardUI.RequestSkip(); FindAnyObjectByType<CosmicModalDialog>().Close(false);
        Require(fixture.CurrentRun.Deck.Count == 10 && rewardUI.SelectedIndex == 0, "Cancelling skip lost the selection or granted a card.");
        rewardUI.ConfirmButton.onClick.Invoke();
        Require(fixture.CurrentRun.Deck.Count == 11, "Reward card was not added.");
        reward.SelectCardReward(0); // Double click must not grant a second reward.
        Require(fixture.CurrentRun.Deck.Count == 11, "Reward was granted twice.");
        reward.LeaveReward(); yield return WaitForScene("MapScene");
        Require(RunSaveSystem.TryRead(out var saved) && saved.cards.Count == 11 && saved.gold == 125 && saved.currentHP == 37, "Return to Map failed to save the reward or health.");
        fixture.SaveAndReturnToMenu(); yield return WaitForScene("MainMenu");
        Require(FindAnyObjectByType<MainMenuManager>() != null, "Main menu missing after save.");
        FindAnyObjectByType<MainMenuManager>().ContinueGame(); yield return WaitForScene("MapScene");
        fixture = FindAnyObjectByType<RunManager>();
        Require(fixture != null && fixture.IsRunActive() && fixture.CurrentRun.Deck.Count == 11 && fixture.CurrentRun.Gold == 125 && fixture.CurrentRun.Player.CurrentHP == 37,
            "Continue did not restore the rewarded checkpoint.");

        Configure(MapNodeType.NormalBattle); fixture.CurrentRun.PrepareBattleReward(fixture.CurrentRun.CurrentNode);
        CosmicSceneLoader.Load("RewardScene"); yield return WaitForScene("RewardScene");
        rewardUI = FindAnyObjectByType<CosmicRewardUI>(); rewardUI.SkipButton.onClick.Invoke();
        FindAnyObjectByType<CosmicModalDialog>().Close(true); yield return WaitForScene("MapScene");
        Require(fixture.CurrentRun.Deck.Count == 10 && fixture.CurrentRun.Gold == 125 &&
            RunSaveSystem.TryRead(out saved) && saved.cards.Count == 10 && saved.gold == 125, "Skip did not retain gold and save the unchanged deck.");
        CosmicSceneLoader.Load("RewardScene"); yield return WaitForScene("RewardScene");
        rewardUI = FindAnyObjectByType<CosmicRewardUI>();
        Require(!rewardUI.SkipButton.gameObject.activeSelf && fixture.CurrentRun.Gold == 125, "Resolved reward reopened or paid again.");
        rewardUI.ConfirmButton.onClick.Invoke(); yield return WaitForScene("MapScene");
        Debug.Log("Reward PLAY checks passed: preview/confirmation, cancel/confirm skip, stable reload, once-only gold/card, Map checkpoint and Continue.");

        Configure(MapNodeType.Shop); CosmicSceneLoader.Load("ShopScene"); yield return WaitForScene("ShopScene");
        var camp = FindAnyObjectByType<CosmicCampUI>(); var shop = FindAnyObjectByType<ShopManager>();
        Require(camp.Ready && camp.GetComponent<AudioSource>().isPlaying, "Shop UI/music did not start.");
        camp.NextButton.onClick.Invoke(); Require(camp.Page == 1, "Shop paging did not work in Play."); camp.PreviousButton.onClick.Invoke();
        yield return null; // New Graphic depths are registered on the next rendered frame.
        ClickCampCard(camp.Cards[0].Button); FindAnyObjectByType<CosmicModalDialog>().Close(false);
        Require(fixture.CurrentRun.Gold == 100 && fixture.CurrentRun.Deck.Count == 10, "Cancelling purchase changed resources.");
        ClickCampCard(camp.Cards[0].Button); FindAnyObjectByType<CosmicModalDialog>().Close(true);
        Require(fixture.CurrentRun.Gold == 55 && fixture.CurrentRun.Deck.Count == 11 && !shop.BuyCard(shop.Offers[0]), "Purchase missing or same-frame double purchase accepted.");
        yield return null;
        ClickCampCard(camp.Cards[1].Button); FindAnyObjectByType<CosmicModalDialog>().Close(true);
        Require(fixture.CurrentRun.Gold == 10 && fixture.CurrentRun.Deck.Count == 12, "Second purchase failed.");
        foreach (var card in camp.Cards) Require(!card.Button.interactable, "Unaffordable shop cards stayed enabled.");
        yield return null; Capture(camp.GetComponent<Canvas>(), "shop-play.png"); yield return null;
        camp.BackButton.onClick.Invoke(); yield return WaitForScene("MapScene");
        Require(RunSaveSystem.TryRead(out saved) && saved.gold == 10 && saved.cards.Count == 12, "Shop purchases not checkpointed on Map.");
        fixture.SaveAndReturnToMenu(); yield return WaitForScene("MainMenu"); FindAnyObjectByType<MainMenuManager>().ContinueGame(); yield return WaitForScene("MapScene"); fixture = FindAnyObjectByType<RunManager>();
        Require(fixture.CurrentRun.Gold == 10 && fixture.CurrentRun.Deck.Count == 12, "Continue lost shop purchases.");

        Configure(MapNodeType.Rest); fixture.CurrentRun.Player.SetCurrentHP(30); CosmicSceneLoader.Load("RestScene"); yield return WaitForScene("RestScene");
        camp = FindAnyObjectByType<CosmicCampUI>(); var rest = FindAnyObjectByType<RestManager>();
        camp.BackButton.onClick.Invoke(); FindAnyObjectByType<CosmicModalDialog>().Close(false);
        Require(SceneManager.GetActiveScene().name == "RestScene" && !rest.ActionUsed, "Cancelling unused-rest exit changed state.");
        camp.HealButton.onClick.Invoke(); FindAnyObjectByType<CosmicModalDialog>().Close(false);
        Require(fixture.CurrentRun.Player.CurrentHP == 30 && !rest.ActionUsed, "Cancelled rest consumed action.");
        camp.HealButton.onClick.Invoke(); FindAnyObjectByType<CosmicModalDialog>().Close(true);
        Require(fixture.CurrentRun.Player.CurrentHP == 56 && rest.ActionUsed && !rest.TryUpgrade(0), "Rest exclusivity failed.");
        CosmicSceneLoader.Load("RestScene"); yield return WaitForScene("RestScene");
        camp = FindAnyObjectByType<CosmicCampUI>(); rest = FindAnyObjectByType<RestManager>();
        Require(rest.ActionUsed && !camp.HealButton.interactable && !camp.UpgradeButton.interactable, "Reloading Rest enabled a second action.");
        camp.BackButton.onClick.Invoke(); yield return WaitForScene("MapScene");
        Require(RunSaveSystem.TryRead(out saved) && saved.currentHP == 56, "Rest HP not saved on Map.");

        Configure(MapNodeType.Rest); CosmicSceneLoader.Load("RestScene"); yield return WaitForScene("RestScene");
        camp = FindAnyObjectByType<CosmicCampUI>(); rest = FindAnyObjectByType<RestManager>();
        Require(!camp.HealButton.interactable, "Full-health heal was enabled.");
        camp.UpgradeButton.onClick.Invoke(); camp.NextButton.onClick.Invoke(); camp.NextButton.onClick.Invoke();
        Require(camp.Page == 2 && camp.Cards.Count == 2, "Upgrade pagination failed in Play.");
        camp.PreviousButton.onClick.Invoke(); camp.PreviousButton.onClick.Invoke();
        yield return null;
        ClickCampCard(camp.Cards[0].Button); FindAnyObjectByType<CosmicModalDialog>().Close(false);
        Require(fixture.CurrentRun.Deck.GetCard(0) == strike && !rest.ActionUsed, "Cancelled upgrade mutated deck.");
        ClickCampCard(camp.Cards[0].Button); FindAnyObjectByType<CosmicModalDialog>().Close(true);
        Require(fixture.CurrentRun.Deck.GetCard(0) == strike.UpgradedCard && rest.ActionUsed && !rest.TryUpgrade(1), "Upgrade was not once-only.");
        camp.BackButton.onClick.Invoke(); yield return WaitForScene("MapScene");
        Require(RunSaveSystem.TryRead(out saved) && saved.cards[0] == strike.UpgradedCard.name, "Upgraded card not saved on Map.");
        fixture.SaveAndReturnToMenu(); yield return WaitForScene("MainMenu"); FindAnyObjectByType<MainMenuManager>().ContinueGame(); yield return WaitForScene("MapScene"); fixture = FindAnyObjectByType<RunManager>();
        Require(fixture.CurrentRun.Deck.GetCard(0) == strike.UpgradedCard, "Continue lost the upgraded card.");

        Configure(MapNodeType.Event); fixture.CurrentRun.Player.SetCurrentHP(30);
        typeof(RunManager).GetField("eventPending", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fixture, true);
        CosmicSceneLoader.Load("MapScene"); yield return WaitForScene("MapScene");
        var eventUI = FindAnyObjectByType<MapEventOverlay>();
        Require(eventUI != null && !GameObject.Find("BackButton").GetComponent<Button>().interactable, "Event did not block Map in Play.");
        eventUI.HealButton.onClick.Invoke(); eventUI.GoldButton.onClick.Invoke();
        Require(fixture.CurrentRun.Player.CurrentHP == 42 && fixture.CurrentRun.Gold == 100 && !fixture.EventPending, "Event choices applied more than once.");
        Require(RunSaveSystem.TryRead(out saved) && saved.currentHP == 42 && !saved.pendingEvent, "Event result not saved.");
        yield return null; Capture(GameObject.Find("MapCanvas").GetComponent<Canvas>(), "event-play-result.png"); yield return null;
        eventUI.ContinueButton.onClick.Invoke(); yield return null;
        Require(FindAnyObjectByType<MapEventOverlay>() == null && GameObject.Find("BackButton").GetComponent<Button>().interactable, "Event receipt left map input locked.");
        fixture.SaveAndReturnToMenu(); yield return WaitForScene("MainMenu"); FindAnyObjectByType<MainMenuManager>().ContinueGame(); yield return WaitForScene("MapScene"); fixture = FindAnyObjectByType<RunManager>();
        Require(fixture.CurrentRun.Player.CurrentHP == 42 && FindAnyObjectByType<MapEventOverlay>() == null, "Continue replayed completed Event.");

        Configure(MapNodeType.Event); typeof(RunManager).GetField("eventPending", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fixture, true);
        CosmicSceneLoader.Load("MapScene"); yield return WaitForScene("MapScene"); eventUI = FindAnyObjectByType<MapEventOverlay>();
        Require(!eventUI.HealButton.interactable, "Full-health Event heal enabled.");
        eventUI.GoldButton.onClick.Invoke(); eventUI.GoldButton.onClick.Invoke(); Require(fixture.CurrentRun.Gold == 125, "Event gold granted twice.");
        eventUI.ContinueButton.onClick.Invoke(); yield return null;
        Debug.Log("Camp PLAY checks passed: actual pointer card clicks, music, shop cancel/confirm/insufficient gold and Continue, Rest heal/upgrade/reload/pagination and Continue, both Event choices once-only, receipt/map unlock and isolated saves.");

        foreach (bool won in new[] { true, false })
        {
            PlayerPrefs.SetInt("CosmicUI.Language", won ? 0 : 1);
            Configure(MapNodeType.Boss);
            Require(fixture.SaveCheckpoint(), "Isolated result checkpoint could not be written.");
            Require(CosmicSceneLoader.Load("BattleScene"), "Boss scene load was rejected.");
            Require(!CosmicSceneLoader.Load("ShopScene"), "Double scene transition was accepted.");
            yield return WaitForScene("BattleScene");
            var ui = FindAnyObjectByType<CosmicBattleUI>(); var battle = ui.Battle;
            Require(ui.MusicSource.clip == CosmicAudio.BossMusic, "Boss track missing.");
            if (won)
            {
                var enemies = FindAnyObjectByType<EnemyManager>(); enemies.Enemies[0].SetCurrentHP(1);
                var deck = FindAnyObjectByType<DeckManager>();
                var hand = (List<CardData>)typeof(DeckManager).GetField("hand", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(deck);
                hand.Clear(); hand.Add(strike); ui.RefreshHand();
                ui.Select(ui.HandArea.GetChild(0).GetComponent<CosmicBattleCard>()); ui.ClickActor(ui.Actors[1]);
                yield return new WaitForSecondsRealtime(.8f);
            }
            else
            {
                battle.Player.SetCurrentHP(1); battle.EndPlayerTurn();
                yield return new WaitForSecondsRealtime(2);
            }
            Require(battle.State == BattleState.End && ui.ResultOpen, "Battle did not open a run result.");
            Require(fixture.GetRunState() == (won ? RunState.Completed : RunState.Failed), "Wrong run outcome.");
            Require(!RunSaveSystem.HasSave, "Completed/failed checkpoint was not retired.");
            var modal = FindAnyObjectByType<CosmicModalDialog>();
            Require(modal != null && modal.GetComponent<Canvas>().rootCanvas == ui.GetComponent<Canvas>(), "Result chose the hidden enemy canvas.");
            foreach (CanvasGroup group in modal.GetComponentsInParent<CanvasGroup>()) Require(group.alpha > .99f, "Result has a transparent ancestor.");
            var accept = modal.transform.Find("ConfirmationPanel/Confirm").GetComponent<Button>();
            var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, accept.transform.position) };
            EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == accept, "Result button is not the top raycast target.");
            Capture(ui.GetComponent<Canvas>(), "battle-result-" + (won ? "victory" : "defeat") + ".png");
            ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; ui.GetComponent<Canvas>().worldCamera = null;
            yield return null;
            battle.EndPlayerTurn(); Require(battle.State == BattleState.End, "Finished battle restarted a turn.");
            accept.onClick.Invoke(); yield return WaitForScene("MainMenu");
            Require(!GameObject.Find("ContinueButton").GetComponent<Button>().interactable, "Continue was still enabled after run end.");
            Require(Time.timeScale == 1, "Result/menu retained a paused clock.");
            Debug.Log("Battle RESULT PLAY case passed: " + (won ? "victory" : "defeat"));
        }
        CosmicSceneLoader.Load("BattleScene"); yield return WaitForScene("BattleScene");
        var missing = FindAnyObjectByType<CosmicModalDialog>();
        Require(missing != null, "Opening Battle directly did not explain the missing run.");
        missing.transform.Find("ConfirmationPanel/Confirm").GetComponent<Button>().onClick.Invoke();
        yield return WaitForScene("MainMenu");
        CosmicSceneLoader.Load("RewardScene"); yield return WaitForScene("RewardScene");
        missing = FindAnyObjectByType<CosmicModalDialog>(); Require(missing != null, "Opening Reward without a won battle was accepted.");
        missing.transform.Find("ConfirmationPanel/Confirm").GetComponent<Button>().onClick.Invoke(); yield return WaitForScene("MainMenu");
        foreach (string room in new[] { "ShopScene", "RestScene" })
        {
            CosmicSceneLoader.Load(room); yield return WaitForScene(room);
            missing = FindAnyObjectByType<CosmicModalDialog>(); Require(missing != null, room + " accepted a missing run.");
            missing.transform.Find("ConfirmationPanel/Confirm").GetComponent<Button>().onClick.Invoke(); yield return WaitForScene("MainMenu");
        }
        var probe = BattleUIFactory.Button("AudioProbe", GameObject.Find("Canvas").transform, new Vector2(200, 60), Vector2.zero, "PROBE", GameObject.Find("StartButton").GetComponentInChildren<TMP_Text>().font, null);
        yield return null;
        int played = CosmicAudio.PlayedCount; probe.onClick.Invoke();
        Require(CosmicAudio.PlayedCount == played + 1, "One UI click did not produce exactly one sound."); Destroy(probe.gameObject);
    }

    private IEnumerator WaitForScene(string name)
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneManager.GetActiveScene().name != name || CosmicSceneLoader.IsLoading)
        {
            Require(Time.realtimeSinceStartup < deadline, "Scene transition timed out: " + name); yield return null;
        }
        yield return new WaitForSecondsRealtime(.5f);
    }

    private static void ClickCampCard(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Camp card is not the top pointer target: " + button.name + " hit " + (hits.Count > 0 ? hits[0].gameObject.name : "nothing") + " at " + pointer.position);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void Capture(Canvas canvas, string name)
    {
        var cameraObject = new GameObject("SmokeCaptureCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 5.4f;
        var texture = new RenderTexture(1920, 1080, 24); texture.Create(); camera.targetTexture = texture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases(); camera.Render();
        RenderTexture previous = RenderTexture.active; RenderTexture.active = texture;
        var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
        File.WriteAllBytes(Path.Combine("C:/Users/iiTzDemmy/Desktop/GameProject", name), image.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        texture.Release(); Destroy(image); Destroy(texture); Destroy(cameraObject);
    }

    private void Cleanup()
    {
        RunSaveSystem.EditorTestDirectory = null;
        if (isolatedDirectory == null) return;
        if (hadLanguage) PlayerPrefs.SetInt("CosmicUI.Language", originalLanguage); else PlayerPrefs.DeleteKey("CosmicUI.Language");
        PlayerPrefs.Save();
        // Only exact files created in this unique test directory; never touch player data.
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
        {
            string path = Path.Combine(isolatedDirectory, "run-save.json" + suffix);
            if (File.Exists(path)) File.Delete(path);
        }
        Directory.Delete(isolatedDirectory, false); isolatedDirectory = null;
    }
    private void OnDestroy() { Application.logMessageReceived -= OnLog; Cleanup(); }

    private static string Digest(string path)
    {
        if (!File.Exists(path)) return "absent";
        using (var sha = System.Security.Cryptography.SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    private IEnumerator Timeout()
    {
        yield return new WaitForSecondsRealtime(180);
        Debug.LogError("Battle PLAY smoke test timed out."); EditorApplication.Exit(1);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
#endif
