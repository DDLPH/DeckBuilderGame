using UnityEngine;

public class CardDisplayManager : MonoBehaviour
{
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform handArea;

    public void RefreshHand()
    {
        if (deckManager == null)
        {
            Debug.LogError("CardDisplayManager : DeckManager is null");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("CardDisplayManager : Card Prefab is null");
            return;
        }

        if (handArea == null)
        {
            Debug.LogError("CardDisplayManager : Hand Area is null");
            return;
        }

        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        foreach (CardData cardData in deckManager.Hand)
        {
            Card card = Instantiate(cardPrefab, handArea);

            card.SetCardData(cardData);
        }
    }
}