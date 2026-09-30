using System.Collections.Generic;

public class PlayerDeck
{
    public IReadOnlyList<CardData> Cards => cards;

    private List<CardData> cards;

    public int Count => cards.Count;

    public PlayerDeck()
    {
        cards = new List<CardData>();
    }

    public void AddCard(CardData card)
    {
        if (card == null) return;

        cards.Add(card);
    }

    public bool RemoveCard(CardData card)
    {
        if (card == null) return false;

        return cards.Remove(card);
    }

    public bool HasCard(CardData card)
    {
        if (card == null) return false;

        return cards.Contains(card);
    }

    public bool UpgradeCard(int index)
    {
        if (index < 0 || index >= cards.Count)
        {
            return false;
        }

        CardData currentCard = cards[index];

        if (currentCard == null)
        {
            return false;
        }

        if (currentCard.UpgradedCard == null)
        {
            return false;
        }

        cards[index] = currentCard.UpgradedCard;

        return true;
    }

    public CardData GetCard(int index)
    {
        if (index < 0 || index >= cards.Count)
        {
            return null;
        }
    
        return cards[index];
    }
}