using UnityEngine;

[CreateAssetMenu(fileName = "DrawCards", menuName = "Cards/Effects/Draw")]
public sealed class DrawCardsEffect : CardEffect
{
    [SerializeField] private int amount = 2;
    public int Amount => amount;
    public override void Execute(CardEffectContext context)
    {
        if (context != null && context.Player != null && context.Deck != null) context.Deck.DrawExtraCards(amount);
    }
}
