using UnityEngine;

public class CharacterTester : MonoBehaviour
{
    [SerializeField] private CharacterData character;

    void Start()
    {
        Debug.Log("===== IsDead Test =====");
    
        character.TakeDamage(100);
    
        Debug.Log("HP = " + character.GetCurrentHP());
    
        Debug.Log("Dead = " + character.IsDead());
    }
}
