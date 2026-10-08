using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Only stable, serializable values go on disk. Unity objects are resolved from
// the card catalogue when a RunManager restores the snapshot.
public static class RunSaveSystem
{
    [Serializable]
    public sealed class NodeSave
    {
        public int type;
        public int layer;
        public bool visited;
        public List<int> connections = new List<int>();
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int gold;
        public int maxHP;
        public int currentHP;
        public int currentNode;
        public int startNode;
        public bool pendingEvent;
        public List<NodeSave> nodes = new List<NodeSave>();
        public List<string> cards = new List<string>();
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, "run-save.json");

    public static bool HasSave => File.Exists(SavePath) || File.Exists(SavePath + ".bak");

    public static bool TryRead(out SaveData data)
    {
        data = null;
        if (TryReadFile(SavePath, out data)) return true;
        if (TryReadFile(SavePath + ".bak", out data))
        {
            Debug.LogWarning("Recovered the previous run checkpoint from its backup.");
            return true;
        }
        return false;
    }

    private static bool TryReadFile(string path, out SaveData data)
    {
        data = null;
        try
        {
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length > 1024 * 1024) return false;
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (!Validate(data)) { data = null; return false; }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not read run checkpoint: " + exception.Message);
            data = null;
            return false;
        }
    }

    public static bool Validate(SaveData data)
    {
        if (data == null || data.version != 1 || data.nodes == null ||
            data.nodes.Count < 2 || data.nodes.Count > 128 || data.cards == null ||
            data.cards.Count == 0 || data.cards.Count > 1024 ||
            data.currentNode < 0 || data.currentNode >= data.nodes.Count ||
            data.startNode < 0 || data.startNode >= data.nodes.Count ||
            data.maxHP <= 0 || data.currentHP <= 0 || data.currentHP > data.maxHP || data.gold < 0)
            return false;
        foreach (string card in data.cards) if (string.IsNullOrWhiteSpace(card)) return false;
        int bossCount = 0;
        foreach (NodeSave node in data.nodes)
        {
            if (node == null || !Enum.IsDefined(typeof(MapNodeType), node.type) ||
                node.layer < 0 || node.layer > 32 || node.connections == null) return false;
            if (node.type == (int)MapNodeType.Boss) bossCount++;
        }
        if (bossCount != 1 || data.nodes[data.startNode].layer != 0 ||
            (data.pendingEvent && data.nodes[data.currentNode].type != (int)MapNodeType.Event)) return false;
        foreach (NodeSave node in data.nodes)
        {
            if (node.type == (int)MapNodeType.Boss)
            { if (node.connections.Count != 0) return false; }
            else if (node.connections.Count == 0) return false;
            var unique = new HashSet<int>();
            foreach (int next in node.connections)
                if (next < 0 || next >= data.nodes.Count || !unique.Add(next) ||
                    data.nodes[next].layer != node.layer + 1) return false;
        }
        // Forward-only layers rule out cycles. Reachability rules out orphan nodes.
        var reached = new HashSet<int>();
        var pending = new Stack<int>(); pending.Push(data.startNode);
        while (pending.Count > 0)
        {
            int index = pending.Pop(); if (!reached.Add(index)) continue;
            foreach (int next in data.nodes[index].connections) pending.Push(next);
        }
        return reached.Count == data.nodes.Count;
    }

    public static bool TryWrite(RunData run, bool pendingEvent = false)
    {
        if (run == null || run.CurrentMap == null || run.CurrentNode == null ||
            run.Player == null || run.Deck == null) return false;

        try
        {
            SaveData data = new SaveData
            {
                gold = run.Gold,
                maxHP = run.Player.MaxHP,
                currentHP = run.Player.CurrentHP,
                pendingEvent = pendingEvent
            };
            IReadOnlyList<MapNode> nodes = run.CurrentMap.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNode node = nodes[i];
                NodeSave saved = new NodeSave
                {
                    type = (int)node.NodeType,
                    layer = node.LayerIndex,
                    visited = node.IsVisited
                };
                foreach (MapNode next in node.Connections)
                {
                    int index = IndexOf(nodes, next);
                    if (index < 0) throw new InvalidDataException("Map has a disconnected node reference.");
                    saved.connections.Add(index);
                }
                data.nodes.Add(saved);
            }
            data.currentNode = IndexOf(nodes, run.CurrentNode);
            data.startNode = IndexOf(nodes, run.CurrentMap.StartNode);
            if (data.currentNode < 0 || data.startNode < 0) return false;
            foreach (CardData card in run.Deck.Cards)
            {
                if (card == null) throw new InvalidDataException("Deck contains a missing card.");
                data.cards.Add(card.name);
            }
            if (!Validate(data)) throw new InvalidDataException("Run checkpoint is incomplete or invalid.");

            Directory.CreateDirectory(Application.persistentDataPath);
            WriteSnapshot(data, SavePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save run: " + exception.Message);
            return false;
        }
    }

    private static void WriteSnapshot(SaveData data, string path)
    {
        if (!Validate(data)) throw new InvalidDataException("Invalid checkpoint.");
        string temporary = path + ".tmp";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
        using (FileStream stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
        else File.Move(temporary, path);
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".bak")) File.Delete(SavePath + ".bak");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }
        catch (Exception exception) { Debug.LogError("Could not delete run save: " + exception.Message); }
    }

    private static int IndexOf(IReadOnlyList<MapNode> nodes, MapNode sought)
    {
        for (int i = 0; i < nodes.Count; i++)
            if (ReferenceEquals(nodes[i], sought)) return i;
        return -1;
    }
}
