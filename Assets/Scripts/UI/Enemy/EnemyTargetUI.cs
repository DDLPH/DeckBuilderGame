using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyTargetUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Button targetButton;

    [Header("Enemy UI")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI blockText;
    [SerializeField] private TextMeshProUGUI intentTypeText;
    [SerializeField] private TextMeshProUGUI intentValueText;

    private EnemyData enemy;
    private BattleTargetManager battleTargetManager;

    public EnemyData Enemy => enemy;

    public void Setup(
        EnemyData targetEnemy,
        BattleTargetManager targetManager)
    {
        enemy = targetEnemy;
        battleTargetManager = targetManager;

        if (targetButton == null)
        {
            Debug.LogError(
                "EnemyTargetUI : Target Button is null"
            );

            return;
        }

        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(
            SelectTarget
        );

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (enemy == null)
        {
            return;
        }

        if (hpText != null)
        {
            hpText.text =
                "HP: " +
                enemy.GetCurrentHP() +
                " / " +
                enemy.GetMaxHP();
        }

        if (blockText != null)
        {
            blockText.text =
                "Block: " +
                enemy.GetBlock();
        }

        EnemyAction action =
            enemy.CurrentAction;

        if (action == null)
        {
            if (intentTypeText != null)
            {
                intentTypeText.text = "";
            }

            if (intentValueText != null)
            {
                intentValueText.text = "";
            }

            return;
        }

        if (intentTypeText != null)
        {
            intentTypeText.text =
                action.ActionType.ToString();
        }

        if (intentValueText != null)
        {
            intentValueText.text =
                action.Value.ToString();
        }
    }

    public void SetInteractable(bool value)
    {
        if (targetButton != null)
        {
            targetButton.interactable = value;
        }
    }

    public void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }

    private void SelectTarget()
    {
        if (enemy == null)
        {
            return;
        }

        if (battleTargetManager == null)
        {
            return;
        }

        battleTargetManager.SelectEnemy(enemy);
    }
}