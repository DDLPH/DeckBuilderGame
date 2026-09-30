using System.Collections.Generic;
using UnityEngine;

public class CardEffectManager : MonoBehaviour
{
    public void Resolve(
        CardData card,
        PlayerData player,
        IReadOnlyList<EnemyData> enemies,
        EnemyData selectedEnemy)
    {
        if (card == null)
        {
            Debug.LogError(
                "CardEffectManager : Card is null"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "CardEffectManager : Player is null"
            );

            return;
        }

        if (enemies == null)
        {
            Debug.LogError(
                "CardEffectManager : Enemy list is null"
            );

            return;
        }

        CardEffectContext context =
            new CardEffectContext(
                player,
                enemies,
                selectedEnemy
            );

        foreach (CardEffect effect in card.Effects)
        {
            if (effect == null)
            {
                continue;
            }

            effect.Execute(context);
        }
    }
}