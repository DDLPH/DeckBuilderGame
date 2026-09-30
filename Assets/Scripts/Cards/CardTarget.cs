public class CardTarget
{
    public PlayerData Player { get; }
    public EnemyData Enemy { get; }

    public CardTarget(
        PlayerData player,
        EnemyData enemy)
    {
        Player = player;
        Enemy = enemy;
    }
}