using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RewardManager : MonoBehaviour
{
    [Header("Gold Reward")]
    [SerializeField] private int goldReward = 20;

    [Header("Reward UI")]
    [SerializeField] private TMP_Text goldRewardText;

    [Header("Card Reward")]
    [SerializeField] private CardData[] rewardCardPool;
    [SerializeField] private int rewardCardCount = 3;

    [Header("Card Reward UI")]
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Transform cardRewardContainer;

    private RunManager runManager;
    private List<CardData> currentCardRewards;
    private bool rewardSelected;

    private void Start()
    {
        rewardSelected = false;

        FindRunManager();

        if (runManager == null)
        {
            return;
        }

        GiveGoldReward();
        RefreshGoldRewardUI();
        GenerateCardRewards();
        BuildCardRewardUI();
    }

    private void FindRunManager()
    {
        runManager =
            FindFirstObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError(
                "RewardManager : RunManager not found"
            );

            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError(
                "RewardManager : CurrentRun is null"
            );

            return;
        }

        Debug.Log("Reward Scene Started");
    }

    private void GiveGoldReward()
    {
        runManager.CurrentRun.AddGold(goldReward);

        Debug.Log(
            "Gold Reward : +" +
            goldReward +
            " | Current Gold : " +
            runManager.CurrentRun.Gold
        );
    }

    private void RefreshGoldRewardUI()
    {
        if (goldRewardText == null)
        {
            Debug.LogError(
                "RewardManager : Gold Reward Text is null"
            );

            return;
        }

        goldRewardText.text =
            "+" + goldReward + " Gold";
    }

    private void GenerateCardRewards()
    {
        if (rewardCardPool == null ||
            rewardCardPool.Length == 0)
        {
            Debug.LogError(
                "RewardManager : Reward Card Pool is empty"
            );

            return;
        }

        currentCardRewards =
            new List<CardData>();

        List<CardData> availableCards =
            new List<CardData>(rewardCardPool);

        int rewardCount =
            Mathf.Min(
                rewardCardCount,
                availableCards.Count
            );

        for (int i = 0; i < rewardCount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availableCards.Count
                );

            CardData selectedCard =
                availableCards[randomIndex];

            currentCardRewards.Add(selectedCard);

            availableCards.RemoveAt(randomIndex);
        }

        Debug.Log(
            "Card Rewards Generated : " +
            currentCardRewards.Count
        );

        for (int i = 0; i < currentCardRewards.Count; i++)
        {
            Debug.Log(
                "Reward Card " +
                i +
                " : " +
                currentCardRewards[i].CardName
            );
        }
    }

    public void SelectCardReward(int index)
    {
        if (rewardSelected)
        {
            Debug.LogWarning(
                "RewardManager : Reward already selected"
            );

            return;
        }

        if (currentCardRewards == null)
        {
            Debug.LogError(
                "RewardManager : Card Rewards not generated"
            );

            return;
        }

        if (index < 0 ||
            index >= currentCardRewards.Count)
        {
            Debug.LogError(
                "RewardManager : Invalid Card Reward Index"
            );

            return;
        }

        CardData selectedCard =
            currentCardRewards[index];

        if (selectedCard == null)
        {
            Debug.LogError(
                "RewardManager : Selected Card is null"
            );

            return;
        }

        if (runManager == null ||
            runManager.CurrentRun == null ||
            runManager.CurrentRun.Deck == null)
        {
            Debug.LogError(
                "RewardManager : Player Deck is not available"
            );

            return;
        }

        runManager.CurrentRun.Deck.AddCard(
            selectedCard
        );

        rewardSelected = true;

        DisableAllRewardButtons();

        Debug.Log(
            "Card Reward Selected : " +
            selectedCard.CardName
        );

        Debug.Log(
            "Player Deck Count After Reward : " +
            runManager.CurrentRun.Deck.Count
        );
    }

    private void BuildCardRewardUI()
    {
        if (rewardCardPrefab == null)
        {
            Debug.LogError(
                "RewardManager : Reward Card Prefab is null"
            );

            return;
        }

        if (cardRewardContainer == null)
        {
            Debug.LogError(
                "RewardManager : Card Reward Container is null"
            );

            return;
        }

        if (currentCardRewards == null)
        {
            Debug.LogError(
                "RewardManager : Current Card Rewards is null"
            );

            return;
        }

        for (int i = 0; i < currentCardRewards.Count; i++)
        {
            CardData card =
                currentCardRewards[i];

            GameObject rewardCardObject =
                Instantiate(
                    rewardCardPrefab,
                    cardRewardContainer
                );

            RewardCardUI rewardCardUI =
                rewardCardObject.GetComponent<RewardCardUI>();

            if (rewardCardUI == null)
            {
                Debug.LogError(
                    "RewardManager : RewardCardUI not found"
                );

                continue;
            }

            rewardCardUI.Setup(
                card,
                i,
                this
            );
        }

        Debug.Log(
            "Card Reward UI Built : " +
            currentCardRewards.Count +
            " cards"
        );
    }

    private void DisableAllRewardButtons()
    {
        if (cardRewardContainer == null)
        {
            Debug.LogError(
                "RewardManager : Card Reward Container is null"
            );

            return;
        }

        RewardCardUI[] rewardCards =
            cardRewardContainer.GetComponentsInChildren<RewardCardUI>();

        for (int i = 0; i < rewardCards.Length; i++)
        {
            rewardCards[i].SetInteractable(false);
        }

        Debug.Log(
            "RewardManager : All Reward Buttons Disabled"
        );
    }

    public void LeaveReward()
    {
        if (runManager == null)
            return;
    
        runManager.ReturnToMap();
    }
}