using TMPro;
using UnityEngine;

public class EnemyStatusUI : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private EnemyData enemy;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI blockText;
    [SerializeField] private TextMeshProUGUI intentText;

    public void SetEnemy(EnemyData targetEnemy)
    {
        enemy = targetEnemy;

        Refresh();
    }

    public void Refresh()
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

        if (intentText != null)
        {
            if (enemy.CurrentAction == null)
            {
                intentText.text = "";
            }
            else
            {
                intentText.text =
                    enemy.CurrentAction.ActionType +
                    " " +
                    enemy.CurrentAction.Value;
            }
        }
    }
}