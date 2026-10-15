using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CosmicCampChecks
{
    public static void Run()
    {
        bool had = PlayerPrefs.HasKey("CosmicUI.Language"); int language = PlayerPrefs.GetInt("CosmicUI.Language", 0);
        string priorDirectory = RunSaveSystem.EditorTestDirectory;
        string directory = Path.Combine(Path.GetTempPath(), "cosmic-camp-check-" + Guid.NewGuid().ToString("N"));
        RunSaveSystem.EditorTestDirectory = directory;
        try
        {
            PlayerPrefs.SetInt("CosmicUI.Language", 0);
            EditorSceneManager.OpenScene("Assets/Scenes/ShopScene.unity");
            RunManager run = Fixture(MapNodeType.Shop); var shop = UnityEngine.Object.FindAnyObjectByType<ShopManager>();
            var ui = UnityEngine.Object.FindAnyObjectByType<CosmicCampUI>(); Invoke(ui, "Awake"); Invoke(shop, "Start");
            Require(shop.Ready && shop.Price == 45 && ui.Cards.Count == 4, "Shop scene/pool/price is incorrect.");
            Render(ui, "shop-en.png");
            ui.NextButton.onClick.Invoke(); Require(ui.Page == 1 && ui.Cards.Count == 4, "Shop second page unreachable.");
            ui.PreviousButton.onClick.Invoke(); Require(ui.Page == 0, "Shop previous page broken.");
            ui.Cards[0].Button.onClick.Invoke(); Modal().Close(false);
            Require(run.CurrentRun.Gold == 100 && run.CurrentRun.Deck.Count == 10, "Cancelled shop purchase changed run.");
            ui.Cards[0].Button.onClick.Invoke(); var modal = Modal(); modal.Close(true); modal.Close(true);
            Require(run.CurrentRun.Gold == 55 && run.CurrentRun.Deck.Count == 11, "Shop confirmation was not once-only.");
            ui.Cards[1].Button.onClick.Invoke(); Modal().Close(true);
            Require(run.CurrentRun.Gold == 10 && run.CurrentRun.Deck.Count == 12, "Second shop purchase failed.");
            foreach (var card in ui.Cards) Require(!card.Button.interactable, "Other buy buttons stayed enabled with insufficient gold.");
            Require(!shop.BuyCard(null) && !shop.BuyCard(shop.Offers[0]), "Invalid/unaffordable shop purchase accepted.");
            PlayerPrefs.SetInt("CosmicUI.Language", 1); ui.Refresh(); Render(ui, "shop-th-small.png", 960, 540);
            var pointer = new PointerEventData(UnityEngine.Object.FindAnyObjectByType<EventSystem>()) { position = RectTransformUtility.WorldToScreenPoint(null, ui.BackButton.transform.position) };
            var hits = new List<RaycastResult>(); ui.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Require(hits.Exists(hit => hit.gameObject.GetComponentInParent<Button>() == ui.BackButton), "Shop exit cannot receive pointer input.");

            EditorSceneManager.OpenScene("Assets/Scenes/RestScene.unity"); run = Fixture(MapNodeType.Rest); run.CurrentRun.Player.SetCurrentHP(30);
            var rest = UnityEngine.Object.FindAnyObjectByType<RestManager>(); ui = UnityEngine.Object.FindAnyObjectByType<CosmicCampUI>(); Invoke(ui, "Awake"); Invoke(rest, "Start");
            Require(rest.HealAmount == 26 && ui.HealButton.interactable && ui.UpgradeButton.interactable, "Rest choices not ready.");
            Render(ui, "rest-th.png");
            ui.HealButton.onClick.Invoke(); Modal().Close(false); Require(!rest.ActionUsed && run.CurrentRun.Player.CurrentHP == 30, "Cancel heal consumed rest action.");
            ui.HealButton.onClick.Invoke(); Modal().Close(true);
            Require(rest.ActionUsed && run.CurrentRun.Player.CurrentHP == 56 && !rest.TryHeal() && !rest.TryUpgrade(0), "Rest healing not capped/once-only/exclusive.");
            Invoke(rest, "Start"); Require(rest.ActionUsed && !ui.HealButton.interactable, "Rest startup reset the used action.");

            EditorSceneManager.OpenScene("Assets/Scenes/RestScene.unity"); run = Fixture(MapNodeType.Rest); run.CurrentRun.Player.SetCurrentHP(30);
            rest = UnityEngine.Object.FindAnyObjectByType<RestManager>(); ui = UnityEngine.Object.FindAnyObjectByType<CosmicCampUI>(); Invoke(ui, "Awake"); Invoke(rest, "Start");
            ui.UpgradeButton.onClick.Invoke(); Require(ui.Upgrading && ui.Cards.Count == 4, "Upgrade catalogue missing.");
            Render(ui, "rest-upgrade-th-small.png", 960, 540);
            ui.NextButton.onClick.Invoke(); ui.NextButton.onClick.Invoke(); Require(ui.Page == 2 && ui.Cards.Count == 2, "Later deck upgrade slots unreachable.");
            ui.PreviousButton.onClick.Invoke(); ui.PreviousButton.onClick.Invoke();
            CardData original = run.CurrentRun.Deck.GetCard(0);
            ui.Cards[0].Button.onClick.Invoke(); Render(ui, "rest-upgrade-confirm-th.png"); Modal().Close(false);
            Require(run.CurrentRun.Deck.GetCard(0) == original && !rest.ActionUsed, "Cancel upgrade mutated deck.");
            ui.Cards[0].Button.onClick.Invoke(); Modal().Close(true);
            Require(run.CurrentRun.Deck.GetCard(0) == original.UpgradedCard && rest.ActionUsed && !ui.Upgrading && run.CurrentRun.Player.CurrentHP == 30, "Upgrade consumed wrong card/health/action.");
            Require(!rest.TryUpgrade(0) && !rest.TryUpgrade(-1) && !rest.TryUpgrade(999), "Invalid/repeated upgrade accepted.");

            EditorSceneManager.OpenScene("Assets/Scenes/RestScene.unity"); run = Fixture(MapNodeType.Rest);
            rest = UnityEngine.Object.FindAnyObjectByType<RestManager>(); ui = UnityEngine.Object.FindAnyObjectByType<CosmicCampUI>(); Invoke(ui, "Awake"); Invoke(rest, "Start");
            Require(!ui.HealButton.interactable && !rest.TryHeal(), "Rest at full HP wasted an action.");
            foreach (CardData card in new List<CardData>(run.CurrentRun.Deck.Cards)) run.CurrentRun.Deck.RemoveCard(card);
            run.CurrentRun.Deck.AddCard(AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Strike+.asset")); ui.Refresh();
            Require(!ui.UpgradeButton.interactable, "Upgrade enabled for fully upgraded deck.");

            EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity"); run = Fixture(MapNodeType.Event); run.CurrentRun.Player.SetCurrentHP(30);
            Set(run, "eventPending", true); MapEventOverlay.Show(run); var overlay = UnityEngine.Object.FindAnyObjectByType<MapEventOverlay>();
            Require(overlay != null && !GameObject.Find("BackButton").GetComponent<Button>().interactable, "Event did not block map input.");
            CosmicAuditChecks.Render(GameObject.Find("MapCanvas").GetComponent<Canvas>(), "event-th.png");
            overlay.GoldButton.onClick.Invoke(); overlay.GoldButton.onClick.Invoke();
            Require(run.CurrentRun.Gold == 125 && !run.EventPending && RunSaveSystem.TryRead(out var saved) && saved.gold == 125 && !saved.pendingEvent, "Event gold/save repeated or missing.");
            Require(overlay.ContinueButton.gameObject.activeSelf, "Event receipt cannot continue."); overlay.Close();
            Require(GameObject.Find("BackButton").GetComponent<Button>().interactable, "Event did not unlock map.");
            Set(run, "eventPending", true); MapEventOverlay.Show(run); overlay = UnityEngine.Object.FindAnyObjectByType<MapEventOverlay>();
            overlay.HealButton.onClick.Invoke(); overlay.HealButton.onClick.Invoke();
            Require(run.CurrentRun.Player.CurrentHP == 42 && run.CurrentRun.Gold == 125, "Event healing not once-only."); overlay.Close();
            Debug.Log("Camp checks passed: Shop confirmation/cancel, all prices refresh, pagination, insufficient gold, Rest heal/upgrade exclusivity and reload guard, invalid indices, full HP/deck guards, Event input blocking, once-only choices and isolated checkpoint writes. TH/EN previews rendered.");
        }
        finally
        {
            RunSaveSystem.EditorTestDirectory = priorDirectory;
            if (had) PlayerPrefs.SetInt("CosmicUI.Language", language); else PlayerPrefs.DeleteKey("CosmicUI.Language");
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) { string path = Path.Combine(directory, "run-save.json" + suffix); if (File.Exists(path)) File.Delete(path); }
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
        CosmicRewardChecks.Run();
    }
    private static RunManager Fixture(MapNodeType type)
    {
        RunManager owner = UnityEngine.Object.FindAnyObjectByType<RunManager>(); if (owner == null) owner = new GameObject("CampFixture").AddComponent<RunManager>(); owner.enabled = false;
        var data = new RunData(); var map = new MapData(); var start = new MapNode(MapNodeType.NormalBattle, 0); var node = new MapNode(type, 1); var boss = new MapNode(MapNodeType.Boss, 2);
        start.AddConnection(node); node.AddConnection(boss); map.AddNode(start); map.AddNode(node); map.AddNode(boss); map.SetStartNode(start);
        start.MarkVisited(); node.MarkVisited(); data.SetMap(map); data.SetCurrentNode(node); data.StartRun(); data.AddGold(100);
        for (int i = 0; i < 5; i++) { data.Deck.AddCard(AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Strike.asset")); data.Deck.AddCard(AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Defend.asset")); }
        typeof(RunManager).GetProperty("CurrentRun").SetValue(owner, data); return owner;
    }
    private static CosmicModalDialog Modal() => UnityEngine.Object.FindAnyObjectByType<CosmicModalDialog>();
    private static void Invoke(object target, string name) { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static void Set(object target, string name, object value) { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Render(CosmicCampUI ui, string name, int w = 1920, int h = 1080) { Canvas canvas = ui.GetComponent<Canvas>(); CosmicAuditChecks.Render(canvas, name, w, h); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; Canvas.ForceUpdateCanvases(); }
}
