using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Encounter",
    menuName = "Encounters/Encounter"
)]
public class EncounterDefinition : ScriptableObject
{
    [Header("Encounter Info")]
    [SerializeField] private string encounterName;

    [Header("Enemy Groups")]
    [SerializeField] private List<EncounterEnemyGroup> enemyGroups;

    public string EncounterName => encounterName;

    public IReadOnlyList<EncounterEnemyGroup> EnemyGroups =>
        enemyGroups;
}