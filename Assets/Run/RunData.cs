public class RunData
{
    public int Gold { get; private set; }

    public MapNode CurrentNode { get; private set; }

    public bool HasStarted => CurrentNode != null;

    public RunState State { get; private set; }

    public MapData CurrentMap { get; private set; }

    public PlayerDeck Deck { get; private set; }

    public PlayerRunData Player { get; private set; }
    private readonly System.Collections.Generic.HashSet<MapNode> usedRestNodes = new System.Collections.Generic.HashSet<MapNode>();
    public bool RestActionUsed => CurrentNode != null && usedRestNodes.Contains(CurrentNode);
    public bool CanUseRest => State == RunState.Playing && CurrentNode != null && CurrentNode.NodeType == MapNodeType.Rest && !RestActionUsed;

    public bool TryRestHeal()
    {
        if (!CanUseRest || Player.CurrentHP >= Player.MaxHP) return false;
        usedRestNodes.Add(CurrentNode); Player.Heal(System.Math.Max(1, Player.MaxHP / 3)); return true;
    }

    public bool TryRestUpgrade(int index)
    {
        if (!CanUseRest || !Deck.UpgradeCard(index)) return false;
        usedRestNodes.Add(CurrentNode); return true;
    }
    public BattleRewardData PendingReward { get; private set; }

    public bool PrepareBattleReward(MapNode node)
    {
        if (State != RunState.Playing || node == null || node != CurrentNode ||
            (node.NodeType != MapNodeType.NormalBattle && node.NodeType != MapNodeType.Elite)) return false;
        if (PendingReward != null && PendingReward.Node == node) return !PendingReward.Resolved;
        if (PendingReward != null && !PendingReward.Resolved) return false;
        PendingReward = new BattleRewardData(node); return true;
    }

    public bool InitializeBattleReward(int gold, System.Collections.Generic.IReadOnlyList<CardData> choices)
    {
        if (State != RunState.Playing || PendingReward == null || PendingReward.Node != CurrentNode ||
            !PendingReward.Initialize(gold, choices)) return false;
        AddGold(PendingReward.GoldGranted); return true;
    }

    public bool TakeBattleReward(int index)
    {
        if (State != RunState.Playing || PendingReward == null || PendingReward.Node != CurrentNode ||
            !PendingReward.Choose(index, out CardData card)) return false;
        Deck.AddCard(card); return true;
    }

    public bool SkipBattleReward()
    {
        return State == RunState.Playing && PendingReward != null && PendingReward.Node == CurrentNode && PendingReward.Skip();
    }

    public RunData()
    {
        Gold = 0;
        CurrentNode = null;
        State = RunState.NotStarted;
        CurrentMap = null;

        Deck = new PlayerDeck();
        Player = new PlayerRunData();
    }


    public void SetCurrentNode(MapNode node)
    {
        if (node == null) return;

        CurrentNode = node;
    }


    public void SetMap(MapData map)
    {
        if (map == null) return;

        CurrentMap = map;
    }


    public void StartRun()
    {
        State = RunState.Playing;
    }


    public void CompleteRun()
    {
        State = RunState.Completed;
    }

    public void FailRun() { State = RunState.Failed; }


    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        Gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return false;
    
        if (Gold < amount)
            return false;
    
        Gold -= amount;
    
        return true;
    }
}
