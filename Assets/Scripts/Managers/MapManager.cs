using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    //// Current Node ////

    public MapNode CurrentNode =>
        currentNode;

    private MapNode currentNode;

    //// Available Nodes ////

    public IReadOnlyList<MapNode> AvailableNodes
    {
        get
        {
            if (currentNode == null)
            {
                return new List<MapNode>();
            }

            return currentNode.Connections;
        }
    }

    //// Node Movement ////

    public void SetCurrentNode(
        MapNode node)
    {
        currentNode = node;
    }

    public bool MoveToNode(
        MapNode node)
    {
        if (currentNode == null)
        {
            return false;
        }

        if (node == null)
        {
            return false;
        }

        if (!AvailableNodes.Contains(node))
        {
            Debug.LogWarning(
                "Target node is not connected to Current Node"
            );

            return false;
        }

        currentNode.MarkVisited();

        currentNode = node;

        Debug.Log(
            "MapManager Moved To Node"
        );

        Debug.Log(
            "Map Current Node Type : " +
            currentNode.NodeType
        );

        return true;
    }

    //// Movement Validation ////

    public bool CanMoveToNode(
        MapNode node)
    {
        if (currentNode == null)
        {
            return false;
        }

        if (node == null)
        {
            return false;
        }

        return AvailableNodes.Contains(node);
    }

    //// Start Node ////

    public void SetStartNode(
        MapNode node)
    {
        if (node == null)
        {
            Debug.LogError(
                "Cannot set start node because node is null"
            );

            return;
        }

        currentNode = node;
    }

    //// Debug ////

    public void LogAvailableNodes()
    {
        if (currentNode == null)
        {
            Debug.LogError(
                "Cannot log available nodes because Current Node is null"
            );

            return;
        }
    }
}