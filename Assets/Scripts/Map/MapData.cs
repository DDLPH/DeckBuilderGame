using System.Collections.Generic;

public class MapData
{
    //// Map Data ////

    public IReadOnlyList<MapNode> Nodes =>
        nodes;

    public MapNode StartNode { get; private set; }

    //// Internal Data ////

    private List<MapNode> nodes;

    //// Constructor ////

    public MapData()
    {
        nodes =
            new List<MapNode>();
    }

    //// Node Management ////

    public void AddNode(
        MapNode node)
    {
        if (node == null)
        {
            return;
        }

        if (nodes.Contains(node))
        {
            return;
        }

        nodes.Add(node);
    }

    //// Start Node ////

    public void SetStartNode(
        MapNode node)
    {
        StartNode = node;
    }
}