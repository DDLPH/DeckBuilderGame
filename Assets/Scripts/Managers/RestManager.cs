using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestManager : MonoBehaviour
{
    [Header("Rest UI")]
    [SerializeField] private Button healButton;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject restPanel;

    [Header("Upgrade UI")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject restCardPrefab;
    [SerializeField] private Button upgradeButton;

    private RunManager runManager;
    private bool restActionUsed;

    private void Start()
    {
        restActionUsed = false;

        FindRunManager();
    }

    /////Heal Hp/////

    private void FindRunManager()
    {
        runManager =
            FindFirstObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError("RestManager : RunManager not found");
            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError("RestManager : CurrentRun is null");
            return;
        }

        Debug.Log(
            "Rest Started | Player HP : " +
            runManager.CurrentRun.Player.CurrentHP +
            " / " +
            runManager.CurrentRun.Player.MaxHP
        );

        RefreshHPText();
    }

    public void HealPlayer()
    {
        if (restActionUsed)
        {
            Debug.Log("Rest Action Already Used");
            return;
        }

        if (runManager == null)
        {
            Debug.LogError("RestManager : RunManager is null");
            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError("RestManager : CurrentRun is null");
            return;
        }

        if (runManager.CurrentRun.Player == null)
        {
            Debug.LogError("RestManager : PlayerRunData is null");
            return;
        }

        PlayerRunData player =
            runManager.CurrentRun.Player;

        int healAmount =
            player.MaxHP / 3;

        player.Heal(healAmount);
        RefreshHPText();

        restActionUsed = true;

        DisableRestActions();

        Debug.Log(
            "Player Rest Heal : +" +
            healAmount +
            " HP | Current HP : " +
            player.CurrentHP +
            " / " +
            player.MaxHP
        );
    }

    private void RefreshHPText()
    {
        if (hpText == null)
        {
            return;
        }

        if (runManager == null ||
            runManager.CurrentRun == null ||
            runManager.CurrentRun.Player == null)
        {
            return;
        }

        PlayerRunData player =
            runManager.CurrentRun.Player;

        hpText.text =
            "HP : " +
            player.CurrentHP +
            " / " +
            player.MaxHP;
    }

    /////Upgrade Card/////

    public void OpenUpgradePanel()
    {
        if (restActionUsed)
        {
            Debug.Log("Rest Action Already Used");
            return;
        }

        if (runManager == null ||
            runManager.CurrentRun == null ||
            runManager.CurrentRun.Deck == null)
        {
            Debug.LogError("RestManager : Player Deck is not available");
            return;
        }

        BuildUpgradeCardList();

        if (restPanel != null)
        {
            restPanel.SetActive(false);
        }

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }
    }

    private void BuildUpgradeCardList()
    {
        if (cardContainer == null)
        {
            Debug.LogError("RestManager : Card Container is null");
            return;
        }

        if (restCardPrefab == null)
        {
            Debug.LogError("RestManager : Rest Card Prefab is null");
            return;
        }

        PlayerDeck deck =
            runManager.CurrentRun.Deck;

        ClearUpgradeCardList();

        for (int i = 0; i < deck.Count; i++)
        {
            CardData card = deck.GetCard(i);

            if (card == null)
            {
                continue;
            }

            if (card.UpgradedCard == null)
            {
                continue;
            }

            GameObject cardObject =
                Instantiate(
                    restCardPrefab,
                    cardContainer
                );

            RestCardUI cardUI =
                cardObject.GetComponent<RestCardUI>();

            if (cardUI == null)
            {
                Debug.LogError(
                    "RestManager : RestCardUI not found on prefab"
                );

                continue;
            }

            cardUI.Setup(
                card,
                i,
                this
            );
        }
    }

    private void ClearUpgradeCardList()
    {
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(cardContainer.GetChild(i).gameObject);
        }
    }

    public void SelectCardForUpgrade(int index)
    {
        if (restActionUsed)
        {
            Debug.Log("Rest Action Already Used");
            return;
        }

        if (runManager == null ||
            runManager.CurrentRun == null ||
            runManager.CurrentRun.Deck == null)
        {
            Debug.LogError("RestManager : Player Deck is not available");
            return;
        }

        PlayerDeck deck =
            runManager.CurrentRun.Deck;

        CardData currentCard =
            deck.GetCard(index);

        if (currentCard == null)
        {
            Debug.LogError("RestManager : Card is null");
            return;
        }

        bool upgraded =
            deck.UpgradeCard(index);

        if (!upgraded)
        {
            Debug.LogError(
                "RestManager : Card Upgrade Failed"
            );

            return;
        }

        restActionUsed = true;

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        if (restPanel != null)
        {
            restPanel.SetActive(true);
        }

        DisableRestActions();

        Debug.Log(
            "Card Upgraded : " +
            currentCard.CardName +
            " -> " +
            currentCard.UpgradedCard.CardName
        );
    }

    /////Button/////

    private void DisableRestActions()
    {
        if (healButton != null)
        {
            healButton.interactable = false;
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
        }
    }

    /////Leave/////

    public void LeaveRest()
    {
        if (runManager == null)
        {
            Debug.LogError("RestManager : RunManager is null");
            return;
        }

        Debug.Log("Leave Rest");

        runManager.ReturnToMap();
    }

}