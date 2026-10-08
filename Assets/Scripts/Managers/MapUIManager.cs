using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapUIManager : MonoBehaviour
{
    //// References ////

    [SerializeField] private RunManager runManager;
    [SerializeField] private GameObject mapNodePrefab;

    [SerializeField] private Transform mapNodeContainer;
    [SerializeField] private GameObject mapLayerPrefab;

    [SerializeField] private RectTransform connectionLayer;
    [SerializeField] private GameObject mapConnectionPrefab;

    [SerializeField] private ScrollRect mapScrollRect;
    [SerializeField] private bool showWholeMap = true;


    //// Map Layout Settings ////

    private const float LayerVerticalSpacing = 124f;

    private const float NodeHorizontalSpacing = 280f;

    private const float RandomHorizontalOffset = 30f;

    private const float StartY = 174f;

    private const float BottomPadding = 120f;

    private const float TopPadding = 120f;


    //// Map State ////

    private bool hasLoggedInitialMapUI;
    private bool hasBuiltMapUI;

    private Dictionary<MapNode, RectTransform> nodeUIMap =
        new Dictionary<MapNode, RectTransform>();


    //// Unity Lifecycle ////

    private void Start()
    {
        if (runManager == null)
        {
            Debug.LogError(
                "MapUIManager RunManager is not assigned"
            );

            return;
        }

        if (mapNodePrefab == null)
        {
            Debug.LogError(
                "MapUIManager Map Node Prefab is not assigned"
            );

            return;
        }

        if (mapNodeContainer == null)
        {
            Debug.LogError(
                "MapUIManager Map Node Container is not assigned"
            );

            return;
        }

        if (mapLayerPrefab == null)
        {
            Debug.LogError(
                "MapUIManager Map Layer Prefab is not assigned"
            );

            return;
        }

        EnsureScrollView();

        Debug.Log(
            "MapUIManager Ready"
        );
    }


    //// RunManager Setup ////

    public void SetRunManager(
        RunManager targetRunManager)
    {
        if (targetRunManager == null)
        {
            Debug.LogError(
                "MapUIManager cannot set RunManager because it is null"
            );

            return;
        }

        runManager =
            targetRunManager;
    }


    //// Refresh Map UI ////

    public void RefreshMapUI()
    {
        if (!hasBuiltMapUI)
        {
            BuildMapUI();

            hasBuiltMapUI = true;

            return;
        }

        UpdateNodeStates();
    }


    //// Build Map UI ////

    private void BuildMapUI()
    {
        if (runManager == null)
        {
            Debug.LogError(
                "BuildMapUI : RunManager is null"
            );

            return;
        }

        if (mapNodePrefab == null)
        {
            Debug.LogError(
                "BuildMapUI : Map Node Prefab is null"
            );

            return;
        }

        if (mapNodeContainer == null)
        {
            Debug.LogError(
                "BuildMapUI : Map Node Container is null"
            );

            return;
        }

        if (mapLayerPrefab == null)
        {
            Debug.LogError(
                "BuildMapUI : Map Layer Prefab is null"
            );

            return;
        }

        EnsureScrollView();

        ClearMapUI();

        IReadOnlyList<MapNode> allNodes =
            runManager.GetAllMapNodes();

        if (allNodes == null)
        {
            Debug.LogWarning(
                "All Map Nodes is null"
            );

            return;
        }


        //// Prepare Map Container ////

        RectTransform containerRect =
            mapNodeContainer as RectTransform;

        if (containerRect == null)
        {
            Debug.LogError(
                "Map Node Container must have RectTransform"
            );

            return;
        }


        //// Find Highest Layer ////

        int highestLayerIndex =
            GetHighestLayerIndex(
                allNodes
            );


        //// Create Visible Layers ////
        //// Layer 0 Start Is Intentionally Hidden ////

        for (
            int layerIndex = 0;
            layerIndex <= highestLayerIndex;
            layerIndex++
        )
        {
            List<MapNode> nodesInLayer =
                GetNodesInLayer(
                    allNodes,
                    layerIndex
                );

            if (nodesInLayer.Count == 0)
            {
                continue;
            }

            CreateLayerUI(
                nodesInLayer,
                layerIndex
            );
        }


        //// Resize Map Container ////

        ResizeMapContainer(
            highestLayerIndex
        );


        //// Update Canvas Before Connections ////

        Canvas.ForceUpdateCanvases();


        //// Create Connections ////

        CreateMapConnections(
            allNodes
        );

        ScrollToAvailableNodes();


        //// Finish ////

        if (!hasLoggedInitialMapUI)
        {
            Debug.Log(
                "Created All Map UI Node Count : " +
                GetDisplayedNodeCount()
            );

            hasLoggedInitialMapUI = true;
        }
    }


    //// Create Layer UI ////

    private void CreateLayerUI(
        List<MapNode> nodesInLayer,
        int layerIndex)
    {
        if (nodesInLayer == null)
        {
            return;
        }

        if (nodesInLayer.Count == 0)
        {
            return;
        }


        //// Create Layer Container ////

        GameObject layerObject =
            Instantiate(
                mapLayerPrefab,
                mapNodeContainer
            );

        RectTransform layerRect =
            layerObject.GetComponent<RectTransform>();

        if (layerRect == null)
        {
            Debug.LogError(
                "Map Layer Prefab does not have RectTransform"
            );

            Destroy(
                layerObject
            );

            return;
        }

        // Nodes are positioned by this class, not by the prefab's layout group.
        HorizontalLayoutGroup layerLayout =
            layerObject.GetComponent<HorizontalLayoutGroup>();

        if (layerLayout != null)
        {
            layerLayout.enabled = false;
        }


        //// Set Layer Position ////

        float layerY =
            StartY +
            (layerIndex - 1) *
            LayerVerticalSpacing;

        layerRect.anchorMin =
            new Vector2(
                0.5f,
                0f
            );

        layerRect.anchorMax =
            new Vector2(
                0.5f,
                0f
            );

        layerRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        layerRect.anchoredPosition =
            new Vector2(
                0f,
                layerY
            );

        layerRect.sizeDelta =
            new Vector2(
                1400f,
                180f
            );


        //// Create Nodes ////

        for (
            int i = 0;
            i < nodesInLayer.Count;
            i++
        )
        {
            CreateNodeUI(
                nodesInLayer[i],
                i,
                nodesInLayer.Count,
                layerRect,
                layerIndex
            );
        }
    }


    //// Create Node UI ////

    private void CreateNodeUI(
        MapNode node,
        int nodeIndex,
        int nodeCount,
        RectTransform layerRect,
        int layerIndex)
    {
        if (node == null)
        {
            return;
        }

        GameObject nodeObject =
            Instantiate(
                mapNodePrefab,
                layerRect
            );

        RectTransform nodeRect =
            nodeObject.GetComponent<RectTransform>();

        if (nodeRect == null)
        {
            Debug.LogError(
                "Map Node Prefab does not have RectTransform"
            );

            Destroy(
                nodeObject
            );

            return;
        }

        MapNodeView nodeView =
            nodeObject.GetComponent<MapNodeView>();

        if (nodeView == null)
        {
            Debug.LogError(
                "Map Node Prefab does not have MapNodeView"
            );

            Destroy(
                nodeObject
            );

            return;
        }


        //// Position Node ////

        float x =
            GetNodeXPosition(
                nodeIndex,
                nodeCount
            );

        // A restored map retains the same visual route instead of jumping.
        float randomOffset = Mathf.Sin(layerIndex * 1.7f + nodeIndex * 2.4f) * 16f;

        if (nodeCount <= 1)
        {
            randomOffset = 0f;
        }

        x += randomOffset;


        //// Boss Stay Centered ////

        if (
            node.NodeType ==
            MapNodeType.Boss
        )
        {
            x = 0f;
        }


        //// Set RectTransform ////

        nodeRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        nodeRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        nodeRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        nodeRect.anchoredPosition =
            new Vector2(
                x,
                0f
            );

        float diameter = node.NodeType == MapNodeType.Boss ? 178f : layerIndex == 0 ? 124f : 102f;
        nodeRect.sizeDelta = new Vector2(diameter, diameter);


        //// Setup Node ////

        nodeUIMap[node] =
            nodeRect;

        nodeView.Setup(
            node,
            runManager
        );
    }


    //// Calculate Node X Position ////

    private float GetNodeXPosition(
        int nodeIndex,
        int nodeCount)
    {
        if (nodeCount <= 1)
        {
            return 0f;
        }

        float center =
            (nodeCount - 1) *
            0.5f;

        return (
            nodeIndex - center
        ) *
        NodeHorizontalSpacing;
    }


    //// Resize Map Container ////

    private void ResizeMapContainer(
        int highestLayerIndex)
    {
        RectTransform containerRect =
            mapNodeContainer as RectTransform;

        if (containerRect == null)
        {
            return;
        }

        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = Vector2.zero;


        //// Calculate Height ////

        int visibleLayerCount =
            Mathf.Max(
                1,
                highestLayerIndex
            );

        float totalHeight =
            BottomPadding +
            TopPadding +
            (
                (visibleLayerCount - 1) *
                LayerVerticalSpacing
            );

        float width =
            1600f;

        containerRect.sizeDelta =
            new Vector2(
                width,
                totalHeight
            );

        if (showWholeMap)
        {
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0f, -30f);
            containerRect.sizeDelta = new Vector2(1200f, 860f);
        }


        //// Resize Connection Layer ////

        if (connectionLayer != null)
        {
            connectionLayer.anchorMin =
                new Vector2(
                    0f,
                    0f
                );

            connectionLayer.anchorMax =
                new Vector2(
                    1f,
                    1f
                );

            connectionLayer.offsetMin =
                Vector2.zero;

            connectionLayer.offsetMax =
                Vector2.zero;
        }
    }


    //// Update Node States ////

    private void UpdateNodeStates()
    {
        foreach (
            MapNode node
            in nodeUIMap.Keys)
        {
            if (
                !nodeUIMap.ContainsKey(node)
            )
            {
                continue;
            }

            RectTransform nodeRect =
                nodeUIMap[node];

            if (nodeRect == null)
            {
                continue;
            }

            MapNodeView nodeView =
                nodeRect.GetComponent<MapNodeView>();

            if (nodeView == null)
            {
                continue;
            }

            nodeView.RefreshInteractable();
        }

        ScrollToAvailableNodes();
    }


    //// Create a viewport around the existing map content at runtime. ////

    private void EnsureScrollView()
    {
        RectTransform content = mapNodeContainer as RectTransform;

        if (content == null)
        {
            return;
        }

        VerticalLayoutGroup contentLayout =
            content.GetComponent<VerticalLayoutGroup>();

        if (contentLayout != null)
        {
            contentLayout.enabled = false;
        }

        if (showWholeMap)
        {
            if (mapScrollRect != null) mapScrollRect.enabled = false;
            return;
        }

        if (mapScrollRect == null)
        {
            Transform parent = content.parent;
            int siblingIndex = content.GetSiblingIndex();

            GameObject viewport = new GameObject(
                "MapViewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect)
            );

            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();

            viewportRect.SetParent(parent, false);
            viewportRect.SetSiblingIndex(siblingIndex);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            // Leave room for the scene's header and legend/footer.
            viewportRect.offsetMin = new Vector2(300f, 100f);
            viewportRect.offsetMax = new Vector2(-300f, -125f);

            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            content.SetParent(viewportRect, false);

            mapScrollRect = viewport.GetComponent<ScrollRect>();
            mapScrollRect.viewport = viewportRect;
            mapScrollRect.content = content;
        }

        mapScrollRect.horizontal = false;
        mapScrollRect.vertical = true;
        mapScrollRect.movementType = ScrollRect.MovementType.Clamped;
        mapScrollRect.scrollSensitivity = 35f;
    }


    //// Keep the next reachable layer visible after returning to the map. ////

    private void ScrollToAvailableNodes()
    {
        if (showWholeMap) return;
        if (mapScrollRect == null || runManager == null)
        {
            return;
        }

        RectTransform content = mapScrollRect.content;
        RectTransform viewport = mapScrollRect.viewport;

        if (content == null || viewport == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float scrollableHeight = Mathf.Max(
            0f,
            content.rect.height - viewport.rect.height
        );

        if (scrollableHeight <= 0f)
        {
            mapScrollRect.verticalNormalizedPosition = 0f;
            return;
        }

        MapNode currentNode = runManager.GetCurrentNode();
        int nextLayer = currentNode == null
            ? 1
            : currentNode.LayerIndex + 1;

        float nextLayerY = StartY +
            (Mathf.Max(1, nextLayer) - 1) * LayerVerticalSpacing;

        float bottomOffset = Mathf.Clamp(
            nextLayerY - viewport.rect.height * 0.45f,
            0f,
            scrollableHeight
        );

        mapScrollRect.StopMovement();
        mapScrollRect.verticalNormalizedPosition =
            bottomOffset / scrollableHeight;
    }


    //// Create Map Connections ////

    private void CreateMapConnections(
        IReadOnlyList<MapNode> allNodes)
    {
        if (connectionLayer == null)
        {
            Debug.LogError(
                "Connection Layer is not assigned"
            );

            return;
        }

        if (mapConnectionPrefab == null)
        {
            Debug.LogError(
                "Map Connection Prefab is not assigned"
            );

            return;
        }

        for (
            int i = 0;
            i < allNodes.Count;
            i++
        )
        {
            MapNode startNode =
                allNodes[i];

            //// Start Node Has No UI But Its Connections Do ////

            IReadOnlyList<MapNode> connections =
                startNode.Connections;

            for (
                int j = 0;
                j < connections.Count;
                j++
            )
            {
                MapNode endNode =
                    connections[j];

                //// Skip Connection If Either Side Has No UI ////

                if (
                    !nodeUIMap.ContainsKey(
                        startNode
                    ) ||
                    !nodeUIMap.ContainsKey(
                        endNode
                    )
                )
                {
                    continue;
                }

                GameObject connectionObject =
                    Instantiate(
                        mapConnectionPrefab,
                        connectionLayer
                    );

                MapConnectionUI connectionUI =
                    connectionObject.GetComponent<MapConnectionUI>();

                if (connectionUI == null)
                {
                    Debug.LogError(
                        "Map Connection Prefab does not have MapConnectionUI"
                    );

                    Destroy(
                        connectionObject
                    );

                    continue;
                }

                connectionUI.Setup(
                    nodeUIMap[startNode],
                    nodeUIMap[endNode]
                );
            }
        }
    }


    //// Find Highest Layer ////

    private int GetHighestLayerIndex(
        IReadOnlyList<MapNode> allNodes)
    {
        int highestLayerIndex = 0;

        if (allNodes == null)
        {
            return highestLayerIndex;
        }

        for (
            int i = 0;
            i < allNodes.Count;
            i++
        )
        {
            if (
                allNodes[i].LayerIndex >
                highestLayerIndex
            )
            {
                highestLayerIndex =
                    allNodes[i].LayerIndex;
            }
        }

        return highestLayerIndex;
    }


    //// Get Nodes In Layer ////

    private List<MapNode> GetNodesInLayer(
        IReadOnlyList<MapNode> allNodes,
        int layerIndex)
    {
        List<MapNode> nodesInLayer =
            new List<MapNode>();

        if (allNodes == null)
        {
            return nodesInLayer;
        }

        for (
            int i = 0;
            i < allNodes.Count;
            i++
        )
        {
            if (
                allNodes[i].LayerIndex ==
                layerIndex
            )
            {
                nodesInLayer.Add(
                    allNodes[i]
                );
            }
        }

        return nodesInLayer;
    }


    //// Clear Map UI ////

    private void ClearMapUI()
    {
        if (connectionLayer != null)
        {
            for (
                int i =
                    connectionLayer.childCount - 1;
                i >= 0;
                i--
            )
            {
                Destroy(
                    connectionLayer.GetChild(i).gameObject
                );
            }
        }

        if (mapNodeContainer != null)
        {
            for (
                int i =
                    mapNodeContainer.childCount - 1;
                i >= 0;
                i--
            )
            {
                Transform child =
                    mapNodeContainer.GetChild(i);

                if (
                    child == connectionLayer
                )
                {
                    continue;
                }

                Destroy(
                    child.gameObject
                );
            }
        }

        nodeUIMap.Clear();
    }


    //// Get Displayed Node Count ////

    public int GetDisplayedNodeCount()
    {
        int totalCount = 0;

        if (mapNodeContainer == null)
        {
            return 0;
        }

        for (
            int i = 0;
            i < mapNodeContainer.childCount;
            i++
        )
        {
            Transform child =
                mapNodeContainer.GetChild(i);

            if (
                child == connectionLayer
            )
            {
                continue;
            }

            totalCount +=
                child.childCount;
        }

        return totalCount;
    }


    //// Hide Map UI ////

    public void HideMapUI()
    {
        ClearMapUI();
    }
}
