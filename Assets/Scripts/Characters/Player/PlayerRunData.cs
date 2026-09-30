using UnityEngine;

public class PlayerRunData
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }

    public PlayerRunData()
    {
        MaxHP = 80;
        CurrentHP = MaxHP;
    }

    public void SetCurrentHP(int hp)
    {
        CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHP += amount;

        if (CurrentHP > MaxHP)
        {
            CurrentHP = MaxHP;
        }
    }
}