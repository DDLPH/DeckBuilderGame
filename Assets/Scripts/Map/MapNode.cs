using System.Collections.Generic;

public class MapNode
{
    //// Node Data ////

    public MapNodeType NodeType { get; }

    public int LayerIndex { get; }

    public IReadOnlyList<MapNode> Connections =>
        connections;

    public bool IsVisited => visited;

    //// Internal Data ////

    private List<MapNode> connections;

    private bool visited;

    //// Constructor ////

    public MapNode(
        MapNodeType nodeType,
        int layerIndex)
    {
        NodeType = nodeType;
        LayerIndex = layerIndex;

        connections =
            new List<MapNode>();
    }

    //// Connection Management ////

    public void AddConnection(
        MapNode node)
    {
        if (node == null)
        {
            return;
        }

        if (connections.Contains(node))
        {
            return;
        }

        connections.Add(node);
    }

    //// Node State ////

    public void MarkVisited()
    {
        visited = true;
    }
}