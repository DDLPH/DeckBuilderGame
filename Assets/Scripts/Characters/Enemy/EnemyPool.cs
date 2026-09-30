using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Enemy Pool",
    menuName = "Enemies/Enemy Pool"
)]
public class EnemyPool : ScriptableObject
{
    [SerializeField]
    private List<GameObject> enemyPrefabs;

    public IReadOnlyList<GameObject> EnemyPrefabs =>
        enemyPrefabs;
}