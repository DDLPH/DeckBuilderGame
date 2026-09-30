using System.Collections.Generic;
using UnityEngine;

public class BattleEncounterManager : MonoBehaviour
{
    [Header("Encounter Pools")]
    [SerializeField] private EncounterPool normalEncounterPool;
    [SerializeField] private EncounterPool eliteEncounterPool;
    [SerializeField] private EncounterPool bossEncounterPool;

    [Header("Enemy Spawn Points")]
    [SerializeField] private Transform[] enemySpawnPoints;

    public List<EnemyData> SpawnEnemies()
    {
        EncounterPool encounterPool =
            GetEncounterPool();

        if (encounterPool == null)
        {
            Debug.LogError(
                "BattleEncounterManager : Encounter Pool is null"
            );

            return new List<EnemyData>();
        }

        if (encounterPool.Encounters.Count == 0)
        {
            Debug.LogError(
                "BattleEncounterManager : Encounter Pool has no encounters"
            );

            return new List<EnemyData>();
        }

        if (enemySpawnPoints == null ||
            enemySpawnPoints.Length == 0)
        {
            Debug.LogError(
                "BattleEncounterManager : No Enemy Spawn Points assigned"
            );

            return new List<EnemyData>();
        }

        EncounterDefinition encounter =
            GetRandomEncounter(encounterPool);

        if (encounter == null)
        {
            Debug.LogError(
                "BattleEncounterManager : Encounter is null"
            );

            return new List<EnemyData>();
        }

        Debug.Log(
            "Encounter Selected : " +
            encounter.EncounterName
        );

        List<EnemyData> spawnedEnemies =
            new List<EnemyData>();

        foreach (EncounterEnemyGroup group
                 in encounter.EnemyGroups)
        {
            if (group == null)
            {
                continue;
            }

            if (group.EnemyPool == null)
            {
                Debug.LogError(
                    "BattleEncounterManager : Enemy Pool is null"
                );

                continue;
            }

            int count =
                Random.Range(
                    group.MinimumCount,
                    group.MaximumCount + 1
                );

            for (int i = 0; i < count; i++)
            {
                if (spawnedEnemies.Count >=
                    enemySpawnPoints.Length)
                {
                    Debug.LogError(
                        "BattleEncounterManager : " +
                        "Not enough Enemy Spawn Points"
                    );

                    return spawnedEnemies;
                }

                EnemyData enemy =
                    SpawnEnemyFromPool(
                        group.EnemyPool,
                        enemySpawnPoints[
                            spawnedEnemies.Count
                        ]
                    );

                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                }
            }
        }

        Debug.Log(
            "Total Enemies Spawned : " +
            spawnedEnemies.Count
        );

        return spawnedEnemies;
    }

    private EncounterPool GetEncounterPool()
    {
        RunManager runManager =
            FindFirstObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError(
                "BattleEncounterManager : RunManager not found"
            );

            return null;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError(
                "BattleEncounterManager : CurrentRun is null"
            );

            return null;
        }

        if (runManager.CurrentRun.CurrentNode == null)
        {
            Debug.LogError(
                "BattleEncounterManager : Current Node is null"
            );

            return null;
        }

        switch (
            runManager.CurrentRun.CurrentNode.NodeType
        )
        {
            case MapNodeType.NormalBattle:
                Debug.Log(
                    "Battle Encounter Type : Normal"
                );

                return normalEncounterPool;

            case MapNodeType.Elite:
                Debug.Log(
                    "Battle Encounter Type : Elite"
                );

                return eliteEncounterPool;

            case MapNodeType.Boss:
                Debug.Log(
                    "Battle Encounter Type : Boss"
                );

                return bossEncounterPool;

            default:
                Debug.LogError(
                    "BattleEncounterManager : " +
                    "Current Node is not a Battle Node"
                );

                return null;
        }
    }

    private EncounterDefinition GetRandomEncounter(
        EncounterPool encounterPool)
    {
        int randomIndex =
            Random.Range(
                0,
                encounterPool.Encounters.Count
            );

        return encounterPool.Encounters[randomIndex];
    }

    private EnemyData SpawnEnemyFromPool(
        EnemyPool enemyPool,
        Transform spawnPoint)
    {
        if (enemyPool.EnemyPrefabs.Count == 0)
        {
            Debug.LogError(
                "BattleEncounterManager : " +
                "Enemy Pool has no Prefabs"
            );

            return null;
        }

        if (spawnPoint == null)
        {
            Debug.LogError(
                "BattleEncounterManager : " +
                "Spawn Point is null"
            );

            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                enemyPool.EnemyPrefabs.Count
            );

        GameObject enemyPrefab =
            enemyPool.EnemyPrefabs[randomIndex];

        if (enemyPrefab == null)
        {
            Debug.LogError(
                "BattleEncounterManager : " +
                "Enemy Prefab is null"
            );

            return null;
        }

        GameObject enemyObject =
            Instantiate(
                enemyPrefab,
                spawnPoint.parent
            );
        
        RectTransform enemyRect =
            enemyObject.GetComponent<RectTransform>();
        
        RectTransform spawnRect =
            spawnPoint.GetComponent<RectTransform>();
        
        if (enemyRect != null && spawnRect != null)
        {
            enemyRect.anchorMin = spawnRect.anchorMin;
            enemyRect.anchorMax = spawnRect.anchorMax;
            enemyRect.pivot = spawnRect.pivot;
        
            enemyRect.anchoredPosition =
                spawnRect.anchoredPosition;
        
            enemyRect.localRotation =
                Quaternion.identity;
        
            enemyRect.localScale =
                Vector3.one;
        }
        else
        {
            enemyObject.transform.localPosition =
                spawnPoint.localPosition;
        
            enemyObject.transform.localRotation =
                Quaternion.identity;
        
            enemyObject.transform.localScale =
                Vector3.one;
        }

        EnemyData enemyData =
            enemyObject.GetComponent<EnemyData>();

        if (enemyData == null)
        {
            Debug.LogError(
                "BattleEncounterManager : " +
                "Spawned Enemy has no EnemyData"
            );

            Destroy(enemyObject);

            return null;
        }

        EnemyStatusUI enemyStatusUI =
            enemyObject.GetComponentInChildren<EnemyStatusUI>();

        if (enemyStatusUI != null)
        {
            enemyStatusUI.SetEnemy(enemyData);
        }
        else
        {
            Debug.LogWarning(
                "BattleEncounterManager : EnemyStatusUI not found on " +
                enemyObject.name
            );
        }

        Debug.Log(
            "Enemy Spawned : " +
            enemyObject.name
        );

        return enemyData;
    }
}