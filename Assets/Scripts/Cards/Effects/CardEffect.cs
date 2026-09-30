using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    [SerializeField] private TargetType targetType;

    public TargetType Target => targetType;

    public abstract void Execute(CardEffectContext context);
}