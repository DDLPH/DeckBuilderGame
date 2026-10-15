using System;
using System.Collections.Generic;
using UnityEngine;

// Transactions only. CosmicCampUI owns the modern presentation.
public class ShopManager : MonoBehaviour
{
    [Header("Shop Cards")]
    [SerializeField] private CardData[] shopCards;
    [Header("Card Prices")]
    [SerializeField] private int cardPrice = 50;
    [Header("Legacy UI (preserved)")]
    [SerializeField] private GameObject shopCardPrefab;
    [SerializeField] private Transform shopCardContainer;
    private RunManager runManager;
    private bool leaving;
    private int lastPurchaseFrame = -1;
    private readonly List<CardData> offers = new List<CardData>();
    public RunData Run => runManager == null ? null : runManager.CurrentRun;
    public bool Ready => Run != null && runManager.IsRunActive() && Run.CurrentNode != null && Run.CurrentNode.NodeType == MapNodeType.Shop;
    public IReadOnlyList<CardData> Offers => offers;
    public int Price => cardPrice;
    public event Action Changed;

    private void Start()
    {
        runManager = FindAnyObjectByType<RunManager>();
        offers.Clear();
        if (shopCards != null) foreach (CardData card in shopCards)
            if (card != null && !offers.Contains(card)) offers.Add(card);
        CosmicCampUI view = FindAnyObjectByType<CosmicCampUI>();
        if (view != null) { view.Bind(this); if (!Ready) view.ShowUnavailable(); return; }
        if (Ready) BuildShop();
    }
    public bool CanBuyCard(CardData card)
    {
        return Ready && !leaving && !CosmicSceneLoader.IsLoading && card != null && offers.Contains(card) &&
            cardPrice > 0 && Run.Gold >= cardPrice;
    }
    public bool BuyCard(CardData card)
    {
        if (!CanBuyCard(card) || (Application.isPlaying && lastPurchaseFrame == Time.frameCount)) return false;
        if (!Run.SpendGold(cardPrice)) return false;
        Run.Deck.AddCard(card); lastPurchaseFrame = Time.frameCount;
        // All remaining prices/buttons must refresh, not just the card that was bought.
        if (shopCardContainer != null) foreach (ShopCardUI old in shopCardContainer.GetComponentsInChildren<ShopCardUI>(true))
            old.RefreshBuyButton();
        Changed?.Invoke(); return true;
    }
    private void BuildShop()
    {
        if (shopCardPrefab == null || shopCardContainer == null) return;
        BattleUIFactory.Clear(shopCardContainer);
        foreach (CardData card in offers)
        {
            var item = Instantiate(shopCardPrefab, shopCardContainer);
            var ui = item.GetComponent<ShopCardUI>(); if (ui != null) ui.Setup(card, cardPrice, this);
        }
    }
    public void LeaveShop()
    {
        if (!Ready || leaving || CosmicSceneLoader.IsLoading) return;
        leaving = true; runManager.ReturnToMap();
        if (!CosmicSceneLoader.IsLoading) leaving = false;
    }
}
