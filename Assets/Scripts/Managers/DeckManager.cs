using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Draw Settings")]
    [SerializeField] private int startingHandSize = 5;

    [Header("Card Piles")]
    private List<CardData> drawPile = new();
    private List<CardData> hand = new();
    private List<CardData> discardPile = new();
    private List<CardData> exhaustPile = new();


    public void StartBattleSetup()
    {
        ClearBattleDeck();

        RunManager runManager = FindFirstObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError("DeckManager : RunManager not found");
            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError("DeckManager : CurrentRun is null");
            return;
        }

        if (runManager.CurrentRun.Deck == null)
        {
            Debug.LogError("DeckManager : PlayerDeck is null");
            return;
        }

        LoadPlayerDeck(runManager.CurrentRun.Deck);

        ShuffleDeck();

        SetupCards();
    }


    private void ClearBattleDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        Debug.Log("Battle Deck Cleared");
    }


    private void LoadPlayerDeck(PlayerDeck playerDeck)
    {
        if (playerDeck == null)
        {
            Debug.LogError("DeckManager : PlayerDeck is null");
            return;
        }

        drawPile.AddRange(playerDeck.Cards);

        Debug.Log(
            "Player Deck Loaded : " +
            playerDeck.Count +
            " cards"
        );

        Debug.Log(
            "Draw Pile Count : " +
            drawPile.Count
        );
    }


    private void SetupCards()
    {
        BeforeDraw();

        DrawCards(GetStartingHandSize());

        AfterDraw();
    }


    public void StartPlayerTurnDraw()
    {
        int cardsToDraw = GetStartingHandSize() - hand.Count;

        if (cardsToDraw <= 0)
            return;

        BeforeDraw();

        DrawCards(cardsToDraw);

        AfterDraw();
    }


    private int GetStartingHandSize()
    {
        return startingHandSize;
    }


    private void ShuffleDeck()
    {
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            CardData temp = drawPile[i];

            drawPile[i] = drawPile[randomIndex];

            drawPile[randomIndex] = temp;
        }
    }


    private void ReshuffleDiscardIntoDrawPile()
    {
        drawPile.AddRange(discardPile);

        discardPile.Clear();

        ShuffleDeck();

        Debug.Log("Discard reshuffled into Draw Pile");
    }


    private void BeforeDraw()
    {
    }


    private void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                    return;

                ReshuffleDiscardIntoDrawPile();
            }

            int lastIndex = drawPile.Count - 1;

            CardData drawnCard = drawPile[lastIndex];

            hand.Add(drawnCard);

            drawPile.RemoveAt(lastIndex);
        }

        Debug.Log("Deck Count: " + drawPile.Count);
        Debug.Log("Hand Count: " + hand.Count);

        foreach (CardData cardInHand in hand)
        {
            Debug.Log("Hand : " + cardInHand.CardName);
        }

        Debug.Log("Discard Count: " + discardPile.Count);
    }


    private void AfterDraw()
    {
    }


    public bool CanPlayCard(CardData card)
    {
        return hand.Contains(card);
    }


    public void PlayCard(CardData card)
    {
        if (!hand.Contains(card))
            return;

        DiscardCard(card);

        Debug.Log("Card Played: " + card.CardName);
        Debug.Log("Hand Count: " + hand.Count);
        Debug.Log("Discard Count: " + discardPile.Count);
    }


    private void DiscardCard(CardData card)
    {
        hand.Remove(card);

        discardPile.Add(card);
    }

    public void DiscardHand()
    {
        if (hand.Count == 0)
        {
            return;
        }
    
        discardPile.AddRange(hand);
        hand.Clear();
    
        Debug.Log(
            "All cards in hand discarded. " +
            "Discard Count : " +
            discardPile.Count
        );
    }


    public IReadOnlyList<CardData> Hand => hand;
}