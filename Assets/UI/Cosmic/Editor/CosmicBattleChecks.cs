using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CosmicBattleChecks
{
    public static void RunFinal()
    {
        Run();
        CosmicAuditChecks.Run();
        Debug.Log("Final Battle regression checks passed, including every CardData save-catalogue entry.");
    }

    public static void Run()
    {
        try
        {
            foreach (MapNodeType kind in new[] { MapNodeType.NormalBattle, MapNodeType.Elite, MapNodeType.Boss })
                CheckKind(kind);
            Debug.Log("Battle checks passed: normal/elite/boss pools, hand/pile conservation, target validation, energy, pause locks, drag cancellation, bilingual layouts and raycast targets. No player save was written.");
        }
        finally { Time.timeScale = 1; }
    }

    private static void CheckKind(MapNodeType kind)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        var runManager = new GameObject("InMemoryRunFixture").AddComponent<RunManager>();
        runManager.enabled = false; // Never call StartNewRun or SaveCheckpoint in this fixture.
        var data = new RunData();
        var map = new MapData(); var node = new MapNode(kind, kind == MapNodeType.Boss ? 6 : 1);
        map.AddNode(node); map.SetStartNode(node); data.SetMap(map); data.SetCurrentNode(node);
        var strike = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Strike.asset");
        var defend = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Defend.asset");
        for (int i = 0; i < 5; i++) { data.Deck.AddCard(strike); data.Deck.AddCard(defend); }
        data.AddGold(100); data.StartRun();
        typeof(RunManager).GetProperty("CurrentRun").SetValue(runManager, data);
        var battle = UnityEngine.Object.FindAnyObjectByType<BattleManager>();
        var ui = UnityEngine.Object.FindAnyObjectByType<CosmicBattleUI>();
        Invoke(ui, "Awake"); Invoke(battle, "Awake"); Invoke(battle, "StartBattle");
        var enemies = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
        var deck = UnityEngine.Object.FindAnyObjectByType<DeckManager>();
        Require(enemies.Enemies.Count > 0 && ui.Actors.Count == enemies.Enemies.Count + 1, "Actors missing for " + kind);
        Require(deck.Hand.Count == 5 && deck.DrawPile.Count == 5, "Initial hand must be five cards.");
        Require(ui.HandArea.childCount == 5, "Card views do not match hand.");
        foreach (var actor in ui.Actors) Require(actor.Portrait.sprite != null, "Missing combatant sprite.");
        EnemyData enemy = enemies.Enemies[0]; int enemyMax = enemy.GetMaxHP();
        enemy.SetMaxHP(999); enemy.SetCurrentHP(999);

        var hand = (List<CardData>)typeof(DeckManager).GetField("hand", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(deck);
        hand.Clear(); hand.Add(strike); hand.Add(defend); ui.RefreshHand();
        int energy = battle.Player.CurrentEnergy, health = enemy.GetCurrentHP();
        Require(!battle.TryPlayCard(strike, null), "Attack without target was accepted.");
        var stranger = new GameObject("ForeignEnemy").AddComponent<EnemyData>();
        Require(!battle.TryPlayCard(strike, stranger), "Foreign enemy was accepted.");
        UnityEngine.Object.DestroyImmediate(stranger.gameObject);
        Require(!battle.TryPlayCard(defend, enemy), "Player skill accepted an enemy target.");
        Require(battle.Player.CurrentEnergy == energy && enemy.GetCurrentHP() == health, "Invalid target mutated resources.");
        battle.SetPaused(true); Require(!battle.TryPlayCard(strike, enemy), "Pause permitted a card."); battle.SetPaused(false);
        battle.InteractionLocked = true; Require(!battle.TryPlayCard(strike, enemy), "Animation lock permitted a card."); battle.InteractionLocked = false;
        Set(battle, "currentState", BattleState.EnemyTurn); Require(!battle.TryPlayCard(strike, enemy), "Enemy turn permitted a card."); Set(battle, "currentState", BattleState.PlayerTurn);
        Require(battle.TryPlayCard(strike, enemy), "Valid attack failed.");
        int damage = ((DamageEffect)strike.Effects[0]).DamageAmount;
        Require(enemy.GetCurrentHP() == health - damage && battle.Player.CurrentEnergy == energy - strike.EnergyCost, "Attack damage/energy mismatch.");
        Require(!battle.TryPlayCard(strike, enemy), "Removed card was reused.");
        Require(battle.TryPlayCard(defend, null) && battle.Player.GetBlock() == 5, "Guard did not grant Block.");
        Require(hand.Count == 0 && deck.DiscardPile.Count == 2, "Played cards not moved exactly once.");
        hand.Add(strike); battle.Player.TryUseEnergy(battle.Player.CurrentEnergy);
        energy = battle.Player.CurrentEnergy; health = enemy.GetCurrentHP();
        Require(!battle.TryPlayCard(strike, enemy) && hand.Count == 1 && enemy.GetCurrentHP() == health && battle.Player.CurrentEnergy == energy, "Insufficient energy changed gameplay.");
        battle.Player.ResetEnergy(); hand.Clear(); hand.Add(strike); hand.Add(defend); ui.RefreshHand();
        var view = ui.HandArea.GetChild(0).GetComponent<CosmicBattleCard>();
        Require(ui.BeginDrag(view), "Drag could not begin.");
        Require(battle.InteractionLocked, "Dragging did not lock shortcuts.");
        Require(!ui.EndDrag(view, new Vector2(-10000, -10000)) && !battle.InteractionLocked && hand.Count == 2, "Invalid drop consumed or locked a card.");
        var focus = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Focus.asset");
        var insight = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Insight.asset");
        var starfall = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Starfall.asset");
        Require(focus != null && insight != null && starfall != null, "New card catalogue missing.");
        hand.Clear(); hand.Add(focus); battle.Player.ResetEnergy();
        Require(battle.TryPlayCard(focus, null) && battle.Player.CurrentEnergy == 4 && deck.ExhaustPile.Count == 1, "Focus energy/exhaust failed.");
        hand.Add(insight); int beforeDraw = deck.DrawPile.Count;
        Require(battle.TryPlayCard(insight, null) && hand.Count == 2 && deck.DrawPile.Count == beforeDraw - 2, "Insight draw failed.");
        hand.Clear(); hand.Add(starfall); battle.Player.ResetEnergy();
        foreach (EnemyData member in enemies.Enemies) { member.SetMaxHP(999); member.SetCurrentHP(999); member.ResetBlock(); }
        Require(battle.TryPlayCard(starfall, null), "All-enemy card failed.");
        foreach (EnemyData member in enemies.Enemies) Require(member.GetCurrentHP() == 992, "Starfall did not hit every enemy exactly once.");
        foreach (EnemyData member in enemies.Enemies)
        {
            int hp = member.name.StartsWith("Boss") ? 100 : member.name.StartsWith("Elite") ? 52 : 22;
            member.SetMaxHP(hp); member.SetCurrentHP(hp);
        }
        enemy.SetMaxHP(enemyMax); enemy.SetCurrentHP(enemyMax);
        battle.Player.ResetBlock(); battle.Player.ResetEnergy();
        deck.StartBattleSetup(); ui.RefreshHand();
        foreach (var actor in ui.Actors) actor.ResetFeedback();
        Set(ui, "hintUntil", 0f);
        var canvas = ui.GetComponent<Canvas>();
        Invoke(ui, "Update");
        CosmicAuditChecks.Render(canvas, "battle-" + kind + "-en.png");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(UnityEngine.Object.FindAnyObjectByType<EventSystem>());
        var results = new List<RaycastResult>();
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, ui.HandArea.GetChild(0).position);
        canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, results);
        Require(results.Exists(hit => hit.gameObject.GetComponent<CosmicBattleCard>() != null), "Card raycast blocked.");
        results.Clear(); pointer.position = RectTransformUtility.WorldToScreenPoint(null, ui.Actors[1].transform.position);
        canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, results);
        Require(results.Exists(hit => hit.gameObject.GetComponent<CosmicBattleActor>() != null), "Actor raycast blocked.");
        if (kind == MapNodeType.NormalBattle)
        {
            bool had = PlayerPrefs.HasKey("CosmicUI.Language"); int original = PlayerPrefs.GetInt("CosmicUI.Language", 0);
            try
            {
                PlayerPrefs.SetInt("CosmicUI.Language", 1); ui.RefreshLanguage(); Invoke(ui, "Update");
                CosmicAuditChecks.Render(canvas, "battle-normal-th.png");
                ui.OpenPause(); Require(Time.timeScale == 0 && !battle.CanInteract, "Pause failed to stop combat.");
                CosmicAuditChecks.Render(canvas, "battle-pause-th.png"); ui.CloseOverlay();
                Require(Time.timeScale == 1 && battle.CanInteract, "Resume left combat locked.");
                ui.OpenPile(0); CosmicAuditChecks.Render(canvas, "battle-draw-pile.png"); ui.CloseOverlay();
            }
            finally { if (had) PlayerPrefs.SetInt("CosmicUI.Language", original); else PlayerPrefs.DeleteKey("CosmicUI.Language"); }
        }
        UnityEngine.Object.DestroyImmediate(runManager.gameObject);
    }

    private static void Set(object target, string name, object value) { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Invoke(object target, string name) { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    public static void RunPlay()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        EditorApplication.EnterPlaymode();
    }
}
