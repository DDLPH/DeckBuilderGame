using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Defense")]
    [SerializeField] private int block;

    [Header("Energy")]
    [SerializeField] private int maxEnergy = 3;
    [SerializeField] private int currentEnergy;

    private void Start()
    {
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        block = 0;

        Debug.Log(
            $"Player initialized | HP: {currentHealth}/{maxHealth} | " +
            $"Block: {block} | Energy: {currentEnergy}/{maxEnergy}"
        );
    }

    // =========================
    // HEALTH
    // =========================

    public void TakeDamage(int damage)
    {
        damage = Mathf.Max(0, damage);

        int blockedDamage = Mathf.Min(block, damage);

        block -= blockedDamage;
        damage -= blockedDamage;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log(
            $"Player took damage | HP: {currentHealth}/{maxHealth} | Block: {block}"
        );
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    // =========================
    // BLOCK
    // =========================

    public void AddBlock(int amount)
    {
        amount = Mathf.Max(0, amount);

        block += amount;

        Debug.Log($"Player gained block | Block: {block}");
    }

    public void ResetBlock()
    {
        block = 0;

        Debug.Log("Player block reset");
    }

    // =========================
    // ENERGY
    // =========================

    public bool UseEnergy(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (currentEnergy < amount)
        {
            Debug.LogWarning(
                $"Not enough energy | Current: {currentEnergy} | Required: {amount}"
            );

            return false;
        }

        currentEnergy -= amount;

        Debug.Log(
            $"Energy used | Current Energy: {currentEnergy}/{maxEnergy}"
        );

        return true;
    }

    public void GainEnergy(int amount)
    {
        amount = Mathf.Max(0, amount);

        currentEnergy += amount;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);

        Debug.Log(
            $"Energy gained | Current Energy: {currentEnergy}/{maxEnergy}"
        );
    }

    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;

        Debug.Log(
            $"Energy reset | Current Energy: {currentEnergy}/{maxEnergy}"
        );
    }

    // =========================
    // DEBUG / TEST
    // =========================

    public void TestUseEnergy(int amount)
    {
        UseEnergy(amount);
    }
}