using UnityEngine;

[System.Serializable]
public class EncounterEnemyGroup
{
    [SerializeField] private EnemyPool enemyPool;

    [SerializeField] private int minimumCount = 1;

    [SerializeField] private int maximumCount = 1;

    public EnemyPool EnemyPool => enemyPool;

    public int MinimumCount => minimumCount;

    public int MaximumCount => maximumCount;
}