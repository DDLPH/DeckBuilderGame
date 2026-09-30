/*using UnityEngine;

public class MapManagerTest : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;

    private void Start()
    {
        MapNode startNode = new MapNode(MapNodeType.NormalBattle);
        MapNode battleEliteNode = new MapNode(MapNodeType.Elite);
        MapNode restNode = new MapNode(MapNodeType.Rest);
        MapNode bossNode = new MapNode(MapNodeType.Boss);
        

        startNode.AddConnection(battleEliteNode);
        startNode.AddConnection(restNode);

        battleEliteNode.AddConnection(bossNode);

        mapManager.SetCurrentNode(startNode);

        Debug.Log("Current Node : " + mapManager.CurrentNode.NodeType);
        Debug.Log("Available Nodes : " + mapManager.AvailableNodes.Count);

        foreach (MapNode node in mapManager.AvailableNodes)
        {
            Debug.Log("Available : " + node.NodeType);
        }

        mapManager.MoveToNode(battleEliteNode);

        Debug.Log("After Move Current Node : " + mapManager.CurrentNode.NodeType);
        Debug.Log("Start IsVisited : " + startNode.IsVisited);
        Debug.Log("Elite IsVisited : " + battleEliteNode.IsVisited);
        Debug.Log("After Move Available Nodes : " + mapManager.AvailableNodes.Count);

        foreach (MapNode node in mapManager.AvailableNodes)
        {
            Debug.Log("Next Available : " + node.NodeType);
        }

        mapManager.MoveToNode(bossNode);

        Debug.Log("After Second Move Current Node : " + mapManager.CurrentNode.NodeType);
        Debug.Log("Start IsVisited : " + startNode.IsVisited);
        Debug.Log("Elite IsVisited : " + battleEliteNode.IsVisited);
        Debug.Log("Boss IsVisited : " + bossNode.IsVisited);

    }
}*/