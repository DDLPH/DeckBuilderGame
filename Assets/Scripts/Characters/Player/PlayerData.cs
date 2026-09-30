using UnityEngine;

public class PlayerData : CharacterData
{
    [Header("Energy")]
    [SerializeField] private int baseEnergy = 3;
    [SerializeField] private int currentEnergy = 3;

    public int BaseEnergy => baseEnergy;
    public int CurrentEnergy => currentEnergy;

    public void ResetEnergy()
    {
        currentEnergy = baseEnergy;
        //Debug.Log("Energy Reset : " + currentEnergy);
    }

    public bool TryUseEnergy(int amount)
    {
        if (amount < 0)
        {
            return false;
        }
    
        if (currentEnergy < amount)
        {
            return false;
        }
    
        currentEnergy -= amount;
        return true;
    }

    public void GainEnergy(int amount)
    {
        currentEnergy += amount;
    }

    public bool SpendEnergy(int amount)
    {
        if (currentEnergy < amount)
        {
            return false;
        }

        currentEnergy -= amount;
        return true;
    }
}