using System.Collections.Generic;

public class CardEffectContext
{
    public PlayerData Player { get; }

    public IReadOnlyList<EnemyData> Enemies { get; }

    public EnemyData SelectedEnemy { get; }
    public DeckManager Deck { get; }

    public CardEffectContext(
        PlayerData player,
        IReadOnlyList<EnemyData> enemies,
        EnemyData selectedEnemy,
        DeckManager deck = null)
    {
        Player = player;
        Enemies = enemies;
        SelectedEnemy = selectedEnemy;
        Deck = deck;
    }
}
