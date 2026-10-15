using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]

    [SerializeField] private string cardName;
    [SerializeField] private string description;

    [SerializeField] private CardType cardType;
    [SerializeField] private CardRarity rarity;

    [Header("Card Stats")]
    [SerializeField] private int energyCost;
    [SerializeField] private bool exhausts;
    
    [Header("Card Effects")]

    [SerializeField] private List<CardEffect> effects;

    [Header("Upgrade")]
    [SerializeField] private CardData upgradedCard;

    public string CardName => cardName;
    public string Description => description;
    
    public CardType CardType => cardType;
    public CardRarity Rarity => rarity;
    
    public int EnergyCost => energyCost;
    public bool Exhausts => exhausts;

    public List<CardEffect> Effects => effects;

    public CardData UpgradedCard => upgradedCard;

}
