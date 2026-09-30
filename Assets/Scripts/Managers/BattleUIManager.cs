using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [Header("Battle UI")]
    [SerializeField] private Button endTurnButton;

    private BattleManager battleManager;

    private void Awake()
    {
        battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager == null)
        {
            Debug.LogError(
                "BattleUIManager : BattleManager not found"
            );

            return;
        }

        if (endTurnButton == null)
        {
            Debug.LogError(
                "BattleUIManager : End Turn Button is null"
            );

            return;
        }

        endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    private void OnDestroy()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }
    }

    private void OnEndTurnClicked()
    {
        if (battleManager == null)
        {
            Debug.LogError(
                "BattleUIManager : BattleManager is null"
            );

            return;
        }

        battleManager.EndPlayerTurn();
    }
}