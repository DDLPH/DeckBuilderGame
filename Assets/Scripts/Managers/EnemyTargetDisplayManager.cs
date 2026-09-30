using System.Collections.Generic;
using UnityEngine;

public class EnemyTargetDisplayManager : MonoBehaviour
{
    [Header("Enemy Target")]
    [SerializeField] private EnemyTargetUI enemyTargetPrefab;
    [SerializeField] private Transform[] targetSlots;
    [SerializeField] private BattleTargetManager battleTargetManager;

    private readonly List<EnemyTargetUI> targetUIs =
        new List<EnemyTargetUI>();

    public void RefreshTargets(
        IReadOnlyList<EnemyData> enemies)
    {
        ClearTargets();

        if (enemies == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (i >= targetSlots.Length)
            {
                Debug.LogError(
                    "EnemyTargetDisplayManager : " +
                    "Not enough target slots"
                );

                break;
            }

            EnemyData enemy = enemies[i];

            if (enemy == null)
            {
                continue;
            }

            EnemyTargetUI targetUI =
                Instantiate(
                    enemyTargetPrefab,
                    targetSlots[i]
                );

            targetUI.transform.localPosition =
                Vector3.zero;

            targetUI.transform.localRotation =
                Quaternion.identity;

            targetUI.transform.localScale =
                Vector3.one;

            targetUI.Setup(
                enemy,
                battleTargetManager
            );

            targetUIs.Add(targetUI);
        }
    }

    public void RefreshEnemyUI()
    {
        foreach (EnemyTargetUI targetUI in targetUIs)
        {
            if (targetUI == null)
            {
                continue;
            }

            targetUI.RefreshUI();
        }
    }

    public void SetTargetSelectionEnabled(
        bool enabled)
    {
        foreach (EnemyTargetUI targetUI in targetUIs)
        {
            if (targetUI == null)
            {
                continue;
            }

            targetUI.SetInteractable(enabled);
        }
    }

    public void RemoveDeadEnemies()
    {
        foreach (EnemyTargetUI targetUI in targetUIs)
        {
            if (targetUI == null)
            {
                continue;
            }

            if (targetUI.Enemy == null)
            {
                continue;
            }

            if (targetUI.Enemy.IsDead())
            {
                targetUI.SetVisible(false);
            }
        }
    }

    private void ClearTargets()
    {
        foreach (EnemyTargetUI targetUI in targetUIs)
        {
            if (targetUI != null)
            {
                Destroy(targetUI.gameObject);
            }
        }

        targetUIs.Clear();
    }
}