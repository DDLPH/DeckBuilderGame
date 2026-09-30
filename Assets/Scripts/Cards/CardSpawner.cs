// TODO: ย้ายไป UI
using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;

    private void Start()
    {
        SpawnCard();
    }

    private void SpawnCard()
    {
        Instantiate(cardPrefab);
    }
}