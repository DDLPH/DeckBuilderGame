using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Encounter Pool",
    menuName = "Encounters/Encounter Pool"
)]
public class EncounterPool : ScriptableObject
{
    [SerializeField]
    private List<EncounterDefinition> encounters;

    public IReadOnlyList<EncounterDefinition> Encounters =>
        encounters;
}