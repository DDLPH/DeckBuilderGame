using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    private CardData cardData;
    private ShopManager shopManager;


    public void Setup(CardData card, int price, ShopManager manager)
    {
        cardData = card;
        shopManager = manager;

        if (cardData == null)
        {
            Debug.LogError("ShopCardUI : CardData is null");
            return;
        }

        if (cardNameText != null)
            cardNameText.text = cardData.CardName;

        if (descriptionText != null)
            descriptionText.text = cardData.Description;

        if (priceText != null)
            priceText.text = price.ToString();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        RefreshBuyButton();
    }


    public void RefreshBuyButton()
    {
        if (buyButton == null)
            return;

        if (shopManager == null || cardData == null)
        {
            buyButton.interactable = false;
            return;
        }

        buyButton.interactable = shopManager.CanBuyCard(cardData);
    }


    private void OnBuyButtonClicked()
    {
        if (shopManager == null || cardData == null)
            return;

        bool bought = shopManager.BuyCard(cardData);

        if (bought)
        {
            RefreshBuyButton();
        }
    }
}