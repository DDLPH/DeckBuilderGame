using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Controls reward transactions; presentation lives in CosmicRewardUI.
public class RewardManager : MonoBehaviour
{
    [Header("Gold Reward")]
    [SerializeField] private int goldReward = 25;
    [Header("Legacy UI (preserved)")]
    [SerializeField] private TMP_Text goldRewardText;
    [Header("Card Reward")]
    [SerializeField] private CardData[] rewardCardPool;
    [SerializeField, Range(0, 3)] private int rewardCardCount = 3;
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Transform cardRewardContainer;

    private RunManager runManager;
    private bool ready, leaving;
    private static readonly CardData[] Empty = new CardData[0];
    public RunData Run => runManager == null ? null : runManager.CurrentRun;
    public BattleRewardData Receipt => Run == null ? null : Run.PendingReward;
    public IReadOnlyList<CardData> Offers => Receipt == null ? Empty : Receipt.Offers;
    public bool Ready => ready && Run != null && runManager.IsRunActive() && Receipt != null && Receipt.Node == Run.CurrentNode;
    public bool Resolved => Receipt != null && Receipt.Resolved;
    public bool CanChoose => Ready && !Resolved && !leaving && !CosmicSceneLoader.IsLoading;
    public event Action Changed;

    private void Start()
    {
        runManager = FindAnyObjectByType<RunManager>();
        CosmicRewardUI view = FindAnyObjectByType<CosmicRewardUI>();
        if (view != null) view.Bind(this);
        if (runManager == null || !runManager.IsRunActive() || Receipt == null || Receipt.Node != Run.CurrentNode)
        {
            if (view != null) view.ShowUnavailable();
            else
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null) CosmicModalDialog.Show(canvas, null, "NO BATTLE REWARD",
                    "Win a normal or Elite battle from the map first.", "MAIN MENU", () => CosmicSceneLoader.Load("MainMenu"));
            }
            return;
        }

        if (!Receipt.Initialized)
        {
            var available = new List<CardData>();
            if (rewardCardPool != null) foreach (CardData card in rewardCardPool)
                if (card != null && !available.Contains(card)) available.Add(card);
            var choices = new List<CardData>();
            int count = Mathf.Min(Mathf.Clamp(rewardCardCount, 0, 3), available.Count);
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, available.Count);
                choices.Add(available[index]); available.RemoveAt(index);
            }
            Run.InitializeBattleReward(Mathf.Max(0, goldReward), choices);
        }
        ready = Receipt.Initialized;
        if (view != null) view.ShowRewards();
        else BuildLegacyCards();
        Refresh();
    }

    private void Refresh()
    {
        if (goldRewardText != null && Receipt != null)
            goldRewardText.text = "+" + Receipt.GoldGranted + (CosmicLanguage.IsThai ? " ทอง" : " GOLD");
        Changed?.Invoke();
    }

    public bool TryTakeReward(int index)
    {
        if (!CanChoose || !Run.TakeBattleReward(index)) return false;
        DisableLegacyCards(); Refresh();
        Debug.Log("Reward claimed once: " + Offers[index].name);
        return true;
    }

    // Kept for old prefab callbacks and the existing Battle smoke tests.
    public void SelectCardReward(int index) { TryTakeReward(index); }

    public void TakeAndReturnToMap(int index)
    {
        if (TryTakeReward(index)) LeaveReward();
    }

    public void SkipAndReturnToMap()
    {
        if (!CanChoose || !Run.SkipBattleReward()) return;
        Refresh(); LeaveReward();
    }

    public void LeaveReward()
    {
        if (!Ready || !Resolved || leaving || CosmicSceneLoader.IsLoading) return;
        leaving = true; runManager.ReturnToMap();
        if (!CosmicSceneLoader.IsLoading) leaving = false;
    }

    private void BuildLegacyCards()
    {
        if (rewardCardPrefab == null || cardRewardContainer == null) return;
        for (int i = 0; i < Offers.Count; i++)
        {
            var item = Instantiate(rewardCardPrefab, cardRewardContainer);
            var view = item.GetComponent<RewardCardUI>();
            if (view != null) { view.Setup(Offers[i], i, this); view.SetInteractable(!Resolved); }
        }
    }
    private void DisableLegacyCards()
    {
        if (cardRewardContainer == null) return;
        foreach (RewardCardUI card in cardRewardContainer.GetComponentsInChildren<RewardCardUI>(true))
            card.SetInteractable(false);
    }
}
