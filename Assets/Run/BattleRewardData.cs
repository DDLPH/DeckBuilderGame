using System.Collections.Generic;

// Run-owned receipt. Reloading RewardScene must not reroll choices or pay gold twice.
// It is intentionally not a mid-battle disk save; the existing Map checkpoint saves the result.
public sealed class BattleRewardData
{
    private readonly List<CardData> offers = new List<CardData>();
    public MapNode Node { get; }
    public IReadOnlyList<CardData> Offers => offers;
    public bool Initialized { get; private set; }
    public bool Resolved { get; private set; }
    public bool Skipped { get; private set; }
    public int GoldGranted { get; private set; }
    public int ChosenIndex { get; private set; } = -1;

    public BattleRewardData(MapNode node) { Node = node; }

    internal bool Initialize(int gold, IReadOnlyList<CardData> choices)
    {
        if (Initialized) return false;
        if (choices != null) foreach (CardData card in choices)
            if (card != null && !offers.Contains(card)) offers.Add(card);
        GoldGranted = System.Math.Max(0, gold); Initialized = true;
        return true;
    }

    internal bool Choose(int index, out CardData card)
    {
        card = null;
        if (!Initialized || Resolved || index < 0 || index >= offers.Count) return false;
        card = offers[index]; ChosenIndex = index; Resolved = true;
        return true;
    }

    internal bool Skip()
    {
        if (!Initialized || Resolved) return false;
        Skipped = true; Resolved = true; return true;
    }
}
