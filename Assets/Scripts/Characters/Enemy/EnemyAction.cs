using System;
using UnityEngine;

public enum EnemyActionType
{
    Attack,
    Defend
}

[Serializable]
public class EnemyAction
{
    [SerializeField] private EnemyActionType actionType;
    [SerializeField] private int value;

    public EnemyActionType ActionType => actionType;
    public int Value => value;

    public EnemyAction(EnemyActionType actionType, int value)
    {
        this.actionType = actionType;
        this.value = value;
    }
}