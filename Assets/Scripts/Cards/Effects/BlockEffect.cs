using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Block Effect",
    menuName = "Cards/Effects/Block"
)]
public class BlockEffect : CardEffect
{
    [SerializeField] private int blockAmount;

    public int BlockAmount => blockAmount;

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

                context.Player.AddBlock(blockAmount);

                Debug.Log(
                    "Player Block: " +
                    context.Player.GetBlock()
                );

                break;

            case TargetType.SingleEnemy:

                AddBlockToEnemy(
                    context.SelectedEnemy
                );

                break;

            case TargetType.AllEnemies:

                AddBlockToAllEnemies(
                    context.Enemies
                );

                break;

            case TargetType.RandomEnemy:

                AddBlockToRandomEnemy(
                    context.Enemies
                );

                break;
        }
    }

    private void AddBlockToEnemy(
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

        enemy.AddBlock(blockAmount);

        Debug.Log(
            enemy.name +
            " Block: " +
            enemy.GetBlock()
        );
    }

    private void AddBlockToAllEnemies(
        IReadOnlyList<EnemyData> enemies)
    {
        if (enemies == null)
        {
            return;
        }

        foreach (EnemyData enemy in enemies)
        {
            AddBlockToEnemy(enemy);
        }
    }

    private void AddBlockToRandomEnemy(
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

        AddBlockToEnemy(
            availableEnemies[randomIndex]
        );
    }
}