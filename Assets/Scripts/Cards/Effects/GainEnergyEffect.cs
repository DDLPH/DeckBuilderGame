using UnityEngine;

[CreateAssetMenu(fileName = "GainEnergy", menuName = "Cards/Effects/Energy")]
public sealed class GainEnergyEffect : CardEffect
{
    [SerializeField] private int amount = 1;
    public int Amount => amount;
    public override void Execute(CardEffectContext context)
    { if (context != null && context.Player != null && amount > 0) context.Player.GainEnergy(amount); }
}
