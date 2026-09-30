using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Damage Effect",
    menuName = "Cards/Effects/Damage"
)]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damageAmount;

    public int DamageAmount => damageAmount;

    public override void Execute(CardEffectContext context)
    {
        if (context == null)
        {
            return;
        }

        switch (Target)
        {
            case TargetType.Player:

                if (context.Player == null)
                {
                    return;
                }

                context.Player.TakeDamage(damageAmount);

                Debug.Log(
                    "Player HP: " +
                    context.Player.GetCurrentHP()
                );

                break;

            case TargetType.SingleEnemy:

                DamageSingleEnemy(
                    context.SelectedEnemy
                );

                break;

            case TargetType.AllEnemies:

                DamageAllEnemies(
                    context.Enemies
                );

                break;

            case TargetType.RandomEnemy:

                DamageRandomEnemies(
                    context.Enemies
                );

                break;
        }
    }

    private void DamageSingleEnemy(
        EnemyData enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (enemy.IsDead())
        {
            return;
        }

        enemy.TakeDamage(damageAmount);

        Debug.Log(
            enemy.name +
            " Damage: " +
            damageAmount
        );

        Debug.Log(
            enemy.name +
            " HP: " +
            enemy.GetCurrentHP()
        );
    }

    private void DamageAllEnemies(
        IReadOnlyList<EnemyData> enemies)
    {
        if (enemies == null)
        {
            return;
        }

        foreach (EnemyData enemy in enemies)
        {
            DamageSingleEnemy(enemy);
        }
    }

    private void DamageRandomEnemies(
        IReadOnlyList<EnemyData> enemies)
    {
        if (enemies == null)
        {
            return;
        }

        List<EnemyData> availableEnemies =
            new List<EnemyData>();

        foreach (EnemyData enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead())
            {
                continue;
            }

            availableEnemies.Add(enemy);
        }

        if (availableEnemies.Count == 0)
        {
            return;
        }

        int randomIndex =
            Random.Range(
                0,
                availableEnemies.Count
            );

        DamageSingleEnemy(
            availableEnemies[randomIndex]
        );
    }
}