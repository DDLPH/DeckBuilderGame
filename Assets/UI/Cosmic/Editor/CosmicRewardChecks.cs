using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CosmicRewardChecks
{
    public static void Run()
    {
        bool had = PlayerPrefs.HasKey("CosmicUI.Language"); int language = PlayerPrefs.GetInt("CosmicUI.Language", 0);
        try
        {
            PlayerPrefs.SetInt("CosmicUI.Language", 0);
            EditorSceneManager.OpenScene("Assets/Scenes/RewardScene.unity");
            RunManager run = Fixture(); RewardManager manager = UnityEngine.Object.FindAnyObjectByType<RewardManager>();
            CosmicRewardUI ui = UnityEngine.Object.FindAnyObjectByType<CosmicRewardUI>();
            Invoke(ui, "Awake"); Invoke(manager, "Start");
            Require(manager.Ready && manager.Offers.Count == 3 && run.CurrentRun.Gold == 125, "Initial reward missing.");
            Require(new HashSet<CardData>(manager.Offers).Count == 3 && ui.Cards.Count == 3, "Reward options duplicated or views missing.");
            Require(!ui.ConfirmButton.interactable && ui.SkipButton.interactable, "Unselected reward controls are incorrect.");
            Require(!manager.TryTakeReward(-1) && !manager.TryTakeReward(99), "Invalid reward index accepted.");
            Require(run.CurrentRun.Deck.Count == 10 && run.CurrentRun.Gold == 125, "Invalid selection mutated run.");
            Render(ui, "rewards-en.png");
            ui.Cards[1].Button.onClick.Invoke();
            Require(ui.SelectedIndex == 1 && ui.ConfirmButton.interactable && run.CurrentRun.Deck.Count == 10, "Preview claimed a card or confirmation stayed locked.");
            Render(ui, "rewards-selected-en.png");
            ui.RequestSkip(); var modal = UnityEngine.Object.FindAnyObjectByType<CosmicModalDialog>();
            Require(modal != null && !ui.Cards[0].Button.IsInteractable(), "Skip confirmation did not block background input.");
            modal.Close(false); ui.Refresh();
            Require(ui.SelectedIndex == 1 && manager.CanChoose && run.CurrentRun.Deck.Count == 10, "Cancel skip changed rewards.");
            Canvas canvas = ui.GetComponent<Canvas>(); Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(UnityEngine.Object.FindAnyObjectByType<EventSystem>());
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, ui.Cards[0].transform.position);
            var hits = new List<RaycastResult>(); canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Require(hits.Exists(hit => hit.gameObject.GetComponent<CosmicRewardCard>() != null), "Reward card cannot receive pointer input.");

            var chosen = new List<CardData>(manager.Offers);
            Invoke(manager, "Start"); // Startup replay models scene reload without rerolling or repaying.
            Require(run.CurrentRun.Gold == 125, "Reward startup paid gold twice.");
            for (int i = 0; i < chosen.Count; i++) Require(chosen[i] == manager.Offers[i], "Reward startup rerolled choices.");
            Require(manager.TryTakeReward(0) && !manager.TryTakeReward(1), "Card receipt accepted multiple picks.");
            Require(run.CurrentRun.Deck.Count == 11 && !run.CurrentRun.SkipBattleReward(), "Claim count or skip-after-claim guard failed.");
            Invoke(manager, "Start"); Require(run.CurrentRun.Gold == 125 && run.CurrentRun.Deck.Count == 11, "Resolved startup changed rewards.");
            UnityEngine.Object.DestroyImmediate(run.gameObject);

            EditorSceneManager.OpenScene("Assets/Scenes/RewardScene.unity"); run = Fixture();
            manager = UnityEngine.Object.FindAnyObjectByType<RewardManager>(); ui = UnityEngine.Object.FindAnyObjectByType<CosmicRewardUI>();
            Invoke(ui, "Awake"); Invoke(manager, "Start");
            PlayerPrefs.SetInt("CosmicUI.Language", 1); ui.Refresh();
            Require(ui.Cards[0].Data != null && CosmicCardText.Name(ui.Cards[0].Data) != ui.Cards[0].Data.CardName, "Reward card name was not localized.");
            Render(ui, "rewards-th.png"); Render(ui, "rewards-th-small.png", 960, 540);
            ui.RequestSkip(); Render(ui, "rewards-skip-th.png"); UnityEngine.Object.FindAnyObjectByType<CosmicModalDialog>().Close(false);
            Require(run.CurrentRun.SkipBattleReward() && !run.CurrentRun.TakeBattleReward(0) && run.CurrentRun.Deck.Count == 10, "Skip did not retire the card reward.");
            UnityEngine.Object.DestroyImmediate(run.gameObject);

            EditorSceneManager.OpenScene("Assets/Scenes/RewardScene.unity"); run = Fixture();
            manager = UnityEngine.Object.FindAnyObjectByType<RewardManager>(); ui = UnityEngine.Object.FindAnyObjectByType<CosmicRewardUI>();
            typeof(RewardManager).GetField("rewardCardPool", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, null);
            Invoke(ui, "Awake"); Invoke(manager, "Start");
            Require(manager.Ready && manager.Offers.Count == 0 && ui.ConfirmButton.interactable, "Empty reward pool stranded the player.");
            Render(ui, "rewards-empty-th.png"); UnityEngine.Object.DestroyImmediate(run.gameObject);
            Debug.Log("Reward checks passed: three unique offers, preview/confirm, skip cancellation, once-only gold/card receipts, stable reload, empty-pool exit, pointer targets and EN/TH responsive previews. No player save written.");
        }
        finally { if (had) PlayerPrefs.SetInt("CosmicUI.Language", language); else PlayerPrefs.DeleteKey("CosmicUI.Language"); }
        CosmicPolishChecks.Run();
    }

    private static RunManager Fixture()
    {
        var manager = new GameObject("InMemoryRewardFixture").AddComponent<RunManager>(); manager.enabled = false;
        var data = new RunData(); var map = new MapData();
        var start = new MapNode(MapNodeType.NormalBattle, 0); var node = new MapNode(MapNodeType.NormalBattle, 1); var boss = new MapNode(MapNodeType.Boss, 2);
        start.AddConnection(node); node.AddConnection(boss); map.AddNode(start); map.AddNode(node); map.AddNode(boss); map.SetStartNode(start);
        data.SetMap(map); data.SetCurrentNode(node); data.AddGold(100); data.StartRun();
        CardData strike = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Strike.asset");
        CardData defend = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Defend.asset");
        for (int i = 0; i < 5; i++) { data.Deck.AddCard(strike); data.Deck.AddCard(defend); }
        Require(data.PrepareBattleReward(node), "Reward eligibility fixture failed.");
        typeof(RunManager).GetProperty("CurrentRun").SetValue(manager, data); return manager;
    }
    private static void Render(CosmicRewardUI ui, string name, int width = 1920, int height = 1080)
    {
        Canvas canvas = ui.GetComponent<Canvas>(); CosmicAuditChecks.Render(canvas, name, width, height);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; Canvas.ForceUpdateCanvases();
    }
    private static void Invoke(object target, string method) { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
