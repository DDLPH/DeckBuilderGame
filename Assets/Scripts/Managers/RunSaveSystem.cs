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

    public static bool HasSave => File.Exists(SavePath);

    public static bool TryRead(out SaveData data)
    {
        data = null;
        try
        {
            if (!HasSave) return false;
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null || data.version != 1 || data.nodes == null ||
                data.nodes.Count < 2 || data.cards == null ||
                data.currentNode < 0 || data.currentNode >= data.nodes.Count ||
                data.startNode < 0 || data.startNode >= data.nodes.Count ||
                data.maxHP <= 0 || data.currentHP < 0 || data.currentHP > data.maxHP ||
                data.gold < 0)
            {
                Debug.LogError("Run save is invalid or from an unsupported version.");
                data = null;
                return false;
            }
            foreach (NodeSave node in data.nodes)
            {
                if (node == null || !Enum.IsDefined(typeof(MapNodeType), node.type) ||
                    node.layer < 0 || node.connections == null)
                    return false;
                foreach (int connection in node.connections)
                    if (connection < 0 || connection >= data.nodes.Count) return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not read run save: " + exception.Message);
            data = null;
            return false;
        }
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

            Directory.CreateDirectory(Application.persistentDataPath);
            string temporary = SavePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            File.Copy(temporary, SavePath, true);
            File.Delete(temporary);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save run: " + exception.Message);
            return false;
        }
    }

    public static void Delete()
    {
        try { if (HasSave) File.Delete(SavePath); }
        catch (Exception exception) { Debug.LogError("Could not delete run save: " + exception.Message); }
    }

    private static int IndexOf(IReadOnlyList<MapNode> nodes, MapNode sought)
    {
        for (int i = 0; i < nodes.Count; i++)
            if (ReferenceEquals(nodes[i], sought)) return i;
        return -1;
    }
}
