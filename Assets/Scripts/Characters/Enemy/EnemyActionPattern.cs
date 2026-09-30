using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Enemy Action Pattern",
    menuName = "Enemy/Action Pattern"
)]
public class EnemyActionPattern : ScriptableObject
{
    [SerializeField] private List<EnemyAction> actions = new();

    public IReadOnlyList<EnemyAction> Actions => actions;
}