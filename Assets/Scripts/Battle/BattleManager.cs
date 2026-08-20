using UnityEngine;

public enum BattleState
{
    BattleStart,
    PlayerTurn,
    EnemyTurn,
    BattleEnd
}

public class BattleManager : MonoBehaviour
{
    [SerializeField] private BattleState currentState;

    [Header("Combat References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EnemyStats enemyStats;

    private void Start()
    {
        Debug.Log("BattleManager started");
        StartBattle();
    }

    public void StartBattle()
    {
        currentState = BattleState.BattleStart;

        Debug.Log("Battle started");

        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        if (CheckBattleEnd())
        {
            return;
        }

        currentState = BattleState.PlayerTurn;

        if (playerStats != null)
        {
            playerStats.ResetBlock();
            playerStats.ResetEnergy();
        }

        Debug.Log("Player turn started");
    }

    public void EndPlayerTurn()
    {
        if (currentState != BattleState.PlayerTurn)
        {
            Debug.LogWarning(
                "Cannot end player turn because it is not the player's turn."
            );

            return;
        }

        if (CheckBattleEnd())
        {
            return;
        }

        Debug.Log("Player turn ended");

        StartEnemyTurn();
    }

    public void StartEnemyTurn()
    {
        if (CheckBattleEnd())
        {
            return;
        }

        currentState = BattleState.EnemyTurn;

        Debug.Log("Enemy turn started");

        EnemyAttack();

        // สำคัญ:
        // ตรวจทันทีหลัง Enemy โจมตี
        if (CheckBattleEnd())
        {
            return;
        }

        EndEnemyTurn();
    }

    private void EnemyAttack()
    {
        if (playerStats == null || enemyStats == null)
        {
            Debug.LogWarning(
                "PlayerStats or EnemyStats is not assigned."
            );

            return;
        }

        int damage = enemyStats.GetAttackDamage();

        Debug.Log($"Enemy attacks for {damage} damage");

        playerStats.TakeDamage(damage);
    }

    public void EndEnemyTurn()
    {
        if (currentState != BattleState.EnemyTurn)
        {
            Debug.LogWarning(
                "Cannot end enemy turn because it is not the enemy's turn."
            );

            return;
        }

        if (CheckBattleEnd())
        {
            return;
        }

        Debug.Log("Enemy turn ended");

        StartPlayerTurn();
    }

    public bool CheckBattleEnd()
    {
        if (enemyStats != null && enemyStats.IsDead())
        {
            Debug.Log("Player wins!");

            EndBattle();

            return true;
        }

        if (playerStats != null && playerStats.IsDead())
        {
            Debug.Log("Player defeated!");

            EndBattle();

            return true;
        }

        return false;
    }

    public void EndBattle()
    {
        currentState = BattleState.BattleEnd;

        Debug.Log("Battle ended");
    }
}