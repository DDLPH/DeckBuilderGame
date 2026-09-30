using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Cards")]
    [SerializeField] private CardData[] shopCards;

    [Header("Card Prices")]
    [SerializeField] private int cardPrice = 50;

    [Header("Shop UI")]
    [SerializeField] private GameObject shopCardPrefab;
    [SerializeField] private Transform shopCardContainer;

    private RunManager runManager;

    private void Start()
    {
        runManager = FindFirstObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError("ShopManager : RunManager not found");
            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError("ShopManager : CurrentRun is null");
            return;
        }

        Debug.Log("ShopManager Ready");
        Debug.Log("Player Gold : " + runManager.CurrentRun.Gold);

        BuildShop();
        //TestBuyCard();
    }


    public bool CanBuyCard(CardData card)
    {
        if (card == null)
            return false;

        if (runManager == null)
            return false;

        if (runManager.CurrentRun == null)
            return false;

        return runManager.CurrentRun.Gold >= cardPrice;
    }


    public bool BuyCard(CardData card)
    {
        if (!CanBuyCard(card))
        {
            Debug.Log("Cannot Buy Card : " + 
                (card != null ? card.CardName : "NULL"));

            return false;
        }

        runManager.CurrentRun.SpendGold(cardPrice);

        runManager.CurrentRun.Deck.AddCard(card);

        Debug.Log(
            "Card Purchased : " +
            card.CardName +
            " | Price : " +
            cardPrice
        );

        Debug.Log(
            "Player Deck Count : " +
            runManager.CurrentRun.Deck.Count
        );

        Debug.Log(
            "Player Gold : " +
            runManager.CurrentRun.Gold
        );

        return true;
    }

    private void BuildShop()
    {
        if (shopCardPrefab == null)
        {
            Debug.LogError("ShopManager : Shop Card Prefab is missing");
            return;
        }

        if (shopCardContainer == null)
        {
            Debug.LogError("ShopManager : Shop Card Container is missing");
            return;
        }

        if (shopCards == null || shopCards.Length == 0)
        {
            Debug.LogWarning("ShopManager : No shop cards assigned");
            return;
        }

        foreach (CardData card in shopCards)
        {
            if (card == null)
                continue;

            GameObject cardObject =
                Instantiate(shopCardPrefab, shopCardContainer);

            ShopCardUI cardUI =
                cardObject.GetComponent<ShopCardUI>();

            if (cardUI == null)
            {
                Debug.LogError(
                    "ShopManager : Shop Card Prefab does not have ShopCardUI"
                );

                continue;
            }

            cardUI.Setup(card, cardPrice, this);
        }

        Debug.Log("Shop Built : " + shopCards.Length + " cards");
    }

    public void LeaveShop()
    {
        if (runManager == null)
            return;
    
        runManager.ReturnToMap();
    }

    private void TestBuyCard()
    {
        if (shopCards == null || shopCards.Length == 0)
        {
            Debug.LogWarning("ShopManager : No shop cards assigned");
            return;
        }

        BuyCard(shopCards[0]);
    }

}