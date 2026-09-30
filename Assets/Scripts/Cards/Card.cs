using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("Card Data")]
    [SerializeField] private CardData cardData;

    [Header("UI")]
    [SerializeField] private Button cardButton;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI energyCostText;

    private BattleManager battleManager;

    public CardData CardData => cardData;

    public string CardName => cardData.CardName;
    public string Description => cardData.Description;

    public CardType CardType => cardData.CardType;
    public CardRarity Rarity => cardData.Rarity;

    public int EnergyCost => cardData.EnergyCost;

    private void Awake()
    {
        battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager == null)
        {
            Debug.LogError(
                "Card : BattleManager not found"
            );
        }

        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
        }

        if (cardButton == null)
        {
            Debug.LogError(
                "Card : Button component is missing"
            );

            return;
        }

        cardButton.onClick.AddListener(OnCardClicked);
    }

    private void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardClicked);
        }
    }

    public void SetCardData(CardData newCardData)
    {
        if (newCardData == null)
        {
            Debug.LogError(
                "Card : CardData is null"
            );

            return;
        }

        cardData = newCardData;

        RefreshUI();
    }

    private void OnCardClicked()
    {
        if (battleManager == null)
        {
            Debug.LogError(
                "Card : Cannot select Card because BattleManager is null"
            );

            return;
        }

        if (cardData == null)
        {
            Debug.LogError(
                "Card : Cannot select Card because CardData is null"
            );

            return;
        }

        battleManager.SelectCard(cardData);

        Debug.Log(
            "Card Selected : " +
            cardData.CardName
        );
    }

    private void RefreshUI()
    {
        if (cardData == null)
        {
            return;
        }

        if (cardNameText != null)
        {
            cardNameText.text = cardData.CardName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = cardData.Description;
        }

        if (energyCostText != null)
        {
            energyCostText.text = cardData.EnergyCost.ToString();
        }
    }
}