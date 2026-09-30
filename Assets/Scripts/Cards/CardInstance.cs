public class CardInstance
{
    public CardData CardData { get; private set; }

    public bool IsUpgraded { get; private set; }

    public CardInstance(CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        CardData = cardData;
        IsUpgraded = false;
    }

    public void Upgrade()
    {
        if (IsUpgraded)
        {
            return;
        }

        IsUpgraded = true;
    }
}