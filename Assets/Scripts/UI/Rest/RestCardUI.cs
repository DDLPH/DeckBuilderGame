using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private int cardIndex;
    private RestManager restManager;

    public void Setup(
        CardData card,
        int index,
        RestManager manager)
    {
        if (card == null)
        {
            Debug.LogError("RestCardUI : CardData is null");
            return;
        }

        cardIndex = index;
        restManager = manager;

        if (cardNameText != null)
        {
            cardNameText.text = card.CardName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = card.Description;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    private void OnSelectButtonClicked()
    {
        if (restManager == null)
        {
            return;
        }

        restManager.SelectCardForUpgrade(cardIndex);
    }
}