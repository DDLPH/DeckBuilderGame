using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int currentHealth;

    [Header("Combat")]
    [SerializeField] private int attackDamage = 10;

    private void Start()
    {
        currentHealth = maxHealth;

        Debug.Log(
            $"Enemy initialized | HP: {currentHealth}/{maxHealth} | Attack: {attackDamage}"
        );
    }

    public void TakeDamage(int damage)
    {
        damage = Mathf.Max(0, damage);

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log(
            $"Enemy took damage | HP: {currentHealth}/{maxHealth}"
        );
    }

    public int GetAttackDamage()
    {
        return attackDamage;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }
}