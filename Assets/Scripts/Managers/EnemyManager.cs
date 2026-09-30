using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private List<EnemyData> enemies =
        new List<EnemyData>();

    private PlayerData player;

    public IReadOnlyList<EnemyData> Enemies =>
        enemies;

    public void SetEnemies(
        IReadOnlyList<EnemyData> targetEnemies)
    {
        enemies.Clear();

        if (targetEnemies == null)
        {
            Debug.LogError(
                "EnemyManager : Enemy List is null"
            );

            return;
        }

        foreach (EnemyData enemy in targetEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            enemies.Add(enemy);
        }

        Debug.Log(
            "EnemyManager : Total Enemies Set = " +
            enemies.Count
        );
    }

    public void SetPlayer(PlayerData targetPlayer)
    {
        if (targetPlayer == null)
        {
            Debug.LogError(
                "EnemyManager : Cannot set Player because Player is null"
            );

            return;
        }

        player = targetPlayer;

        Debug.Log(
            "EnemyManager Player Set"
        );
    }

    public void DecideActions()
    {
        if (enemies == null ||
            enemies.Count == 0)
        {
            Debug.LogError(
                "EnemyManager : No Enemies"
            );

            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];

            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead())
            {
                continue;
            }

            enemy.DecideAction();

            Debug.Log(
                "Enemy " +
                i +
                " Action Decided : " +
                enemy.CurrentAction?.ActionType
            );

            EnemyIntentDisplayManager intentDisplay =
                enemy.GetComponent<EnemyIntentDisplayManager>();

            if (intentDisplay != null)
            {
                intentDisplay.ShowIntent(
                    enemy.CurrentAction
                );
            }
        }
    }

    public void ResetEnemyBlocks()
    {
        if (enemies == null ||
            enemies.Count == 0)
        {
            return;
        }

        foreach (EnemyData enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead())
            {
                continue;
            }

            enemy.ResetBlock();

            Debug.Log(
                "Enemy Block Reset : " +
                enemy.name
            );
        }
    }

    public void PerformTurn()
    {
        if (enemies == null ||
            enemies.Count == 0)
        {
            Debug.LogError(
                "EnemyManager : No Enemies"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "EnemyManager : Player is null"
            );

            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];

            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead())
            {
                continue;
            }

            EnemyAction action =
                enemy.CurrentAction;

            ExecuteAction(
                enemy,
                action
            );
        }
    }

    private void ExecuteAction(
        EnemyData enemy,
        EnemyAction action)
    {
        if (enemy == null)
        {
            return;
        }

        if (action == null)
        {
            Debug.LogWarning(
                "EnemyManager : Enemy Action is null"
            );

            return;
        }

        switch (action.ActionType)
        {
            case EnemyActionType.Attack:

                player.TakeDamage(
                    action.Value
                );

                Debug.Log(
                    enemy.name +
                    " Enemy Attack : " +
                    action.Value
                );

                Debug.Log(
                    "Player HP : " +
                    player.GetCurrentHP()
                );

                break;

            case EnemyActionType.Defend:

                enemy.AddBlock(
                    action.Value
                );

                Debug.Log(
                    enemy.name +
                    " Enemy Defend : " +
                    action.Value
                );

                Debug.Log(
                    enemy.name +
                    " Enemy Block : " +
                    enemy.GetBlock()
                );

                break;

            default:

                Debug.LogWarning(
                    "EnemyManager : Unknown Enemy Action"
                );

                break;
        }
    }

    public void RefreshEnemyUI()
    {
        if (enemies == null)
        {
            return;
        }

        foreach (EnemyData enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            EnemyStatusUI enemyStatusUI =
                enemy.GetComponentInChildren<EnemyStatusUI>();

            if (enemyStatusUI == null)
            {
                continue;
            }

            enemyStatusUI.Refresh();
        }
    }

    public void RemoveDeadEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyData enemy = enemies[i];
    
            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }
    
            if (!enemy.IsDead())
            {
                continue;
            }
    
            enemy.gameObject.SetActive(false);
    
            enemies.RemoveAt(i);
        }
    }

}