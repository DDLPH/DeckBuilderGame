using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private CardData cardData;
    private RewardManager rewardManager;
    private int rewardIndex;

    public void Setup(
        CardData card,
        int index,
        RewardManager manager)
    {
        cardData = card;
        rewardIndex = index;
        rewardManager = manager;

        if (cardData == null)
        {
            Debug.LogError(
                "RewardCardUI : CardData is null"
            );

            return;
        }

        if (cardNameText != null)
        {
            cardNameText.text =
                cardData.CardName;
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                cardData.Description;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();

            selectButton.onClick.AddListener(
                OnSelectButtonClicked
            );
        }
    }

    private void OnSelectButtonClicked()
    {
        if (rewardManager == null)
        {
            Debug.LogError(
                "RewardCardUI : RewardManager is null"
            );

            return;
        }

        rewardManager.SelectCardReward(
            rewardIndex
        );
    }

    public void SetInteractable(bool interactable)
    {
        if (selectButton == null)
        {
            return;
        }
    
        selectButton.interactable = interactable;
    }
}