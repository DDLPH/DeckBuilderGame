using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //// Map Settings ////

    private const int FirstLayerIndex = 1;
    private const int LastLayerIndex = 5;
    private const int BossLayerIndex = 6;

    private const int FirstLayerNodeCount = 3;

    private const int MinNodesPerLayer = 2;
    private const int MaxNodesPerLayer = 4;

    private const int MaxGenerationAttempts = 100;

    private const int MinConnectionsPerNode = 1;
    private const int MaxConnectionsPerNode = 3;

    private const int MinPathCount = 4;
    private const int MaxPathCount = 6;

    private const int ConnectionGenerationAttempts = 20;


    //// Node Type Probability ////

    private const int NormalWeight = 60;
    private const int EliteWeight = 15;
    private const int RestWeight = 10;
    private const int ShopWeight = 15;


    //// Node Type Requirements ////

    private const int MinEliteCount = 2;
    private const int MaxEliteCount = 4;

    private const int MinShopCount = 2;
    private const int MaxShopCount = 3;

    private const int MinRestCount = 2;
    private const int MaxRestCount = 3;


    //// Map Data ////

    private MapNode startNode;
    private MapNode bossNode;

    public MapData CurrentMap { get; private set; }


    //// Generate Map ////

    public void GenerateMap()
    {
        for (
            int attempt = 0;
            attempt < MaxGenerationAttempts;
            attempt++
        )
        {
            if (TryGenerateMap())
            {
                Debug.Log(
                    "Map Generation Passed | Attempt : " +
                    (attempt + 1)
                );

                return;
            }
        }

        Debug.LogError(
            "Map Generation Failed after " +
            MaxGenerationAttempts +
            " attempts"
        );
    }


    //// Generate Single Map Attempt ////

    private bool TryGenerateMap()
    {
        CurrentMap = new MapData();

        startNode = null;
        bossNode = null;

        CreateStartAndBoss();

        List<List<MapNode>> layers =
            new List<List<MapNode>>();

        for (
            int layerIndex = FirstLayerIndex;
            layerIndex <= LastLayerIndex;
            layerIndex++
        )
        {
            int nodeCount =
                GetRandomLayerNodeCount(
                    layerIndex
                );

            List<MapNode> layer =
                CreateRandomLayer(
                    nodeCount,
                    layerIndex
                );

            layers.Add(layer);
        }


        //// Create Connections ////

        ConnectStartToFirstLayer(
            layers[0]
        );

        for (
            int i = 0;
            i < layers.Count - 1;
            i++
        )
        {
            ConnectLayers(
                layers[i],
                layers[i + 1]
            );
        }

        ConnectLastLayerToBoss(
            layers[layers.Count - 1]
        );


        //// Validate Map ////

        if (!ValidateMap())
        {
            return false;
        }

        return true;
    }


    //// Create Start And Boss ////

    private void CreateStartAndBoss()
    {
        startNode =
            new MapNode(
                MapNodeType.NormalBattle,
                0
            );

        bossNode =
            new MapNode(
                MapNodeType.Boss,
                BossLayerIndex
            );

        CurrentMap.AddNode(
            startNode
        );

        CurrentMap.AddNode(
            bossNode
        );

        CurrentMap.SetStartNode(
            startNode
        );
    }


    //// Get Random Layer Node Count ////

    private int GetRandomLayerNodeCount(
        int layerIndex)
    {
        //// Layer 1 : Fixed 3 Nodes ////

        if (
            layerIndex ==
            FirstLayerIndex
        )
        {
            return FirstLayerNodeCount;
        }


        //// Layer 5 : Before Boss Must Be 2-3 Nodes ////

        if (
            layerIndex ==
            LastLayerIndex
        )
        {
            return Random.Range(
                2,
                4
            );
        }


        //// Other Layers : 2-4 Nodes ////

        return Random.Range(
            MinNodesPerLayer,
            MaxNodesPerLayer + 1
        );
    }


    //// Create Random Layer ////

    private List<MapNode> CreateRandomLayer(
        int count,
        int layerIndex)
    {
        List<MapNode> layer =
            new List<MapNode>();

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            MapNodeType nodeType =
                GetRandomNodeType(
                    layerIndex
                );

            MapNode node =
                new MapNode(
                    nodeType,
                    layerIndex
                );

            layer.Add(node);

            CurrentMap.AddNode(
                node
            );
        }

        return layer;
    }


    //// Get Random Node Type ////

    private MapNodeType GetRandomNodeType(
        int layerIndex)
    {
        //// Layer 1 : Normal Only ////

        if (
            layerIndex ==
            FirstLayerIndex
        )
        {
            return MapNodeType.NormalBattle;
        }


        //// Layer 2 : Rest Not Allowed ////

        if (
            layerIndex == 2
        )
        {
            return GetWeightedRandomNodeTypeWithoutRest();
        }


        //// Layer 3-5 : All Node Types ////

        return GetWeightedRandomNodeType();
    }


    //// Get Weighted Random Node Type Without Rest ////

    private MapNodeType GetWeightedRandomNodeTypeWithoutRest()
    {
        int totalWeight =
            NormalWeight +
            EliteWeight +
            ShopWeight;

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        if (
            randomValue <
            NormalWeight
        )
        {
            return MapNodeType.NormalBattle;
        }

        randomValue -=
            NormalWeight;

        if (
            randomValue <
            EliteWeight
        )
        {
            return MapNodeType.Elite;
        }

        return MapNodeType.Shop;
    }


    //// Get Weighted Random Node Type ////

    private MapNodeType GetWeightedRandomNodeType()
    {
        int totalWeight =
            NormalWeight +
            EliteWeight +
            RestWeight +
            ShopWeight;

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        if (
            randomValue <
            NormalWeight
        )
        {
            return MapNodeType.NormalBattle;
        }

        randomValue -=
            NormalWeight;

        if (
            randomValue <
            EliteWeight
        )
        {
            return MapNodeType.Elite;
        }

        randomValue -=
            EliteWeight;

        if (
            randomValue <
            RestWeight
        )
        {
            return MapNodeType.Rest;
        }

        return MapNodeType.Shop;
    }


    //// Connect Start To First Layer ////

    private void ConnectStartToFirstLayer(
        List<MapNode> firstLayer)
    {
        if (
            firstLayer == null ||
            firstLayer.Count == 0
        )
        {
            return;
        }

        //// Start connects to all Layer 1 nodes ////

        for (
            int i = 0;
            i < firstLayer.Count;
            i++
        )
        {
            startNode.AddConnection(
                firstLayer[i]
            );
        }
    }


    //// Connect Two Layers ////

    private void ConnectLayers(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        if (
            currentLayer == null ||
            nextLayer == null
        )
        {
            return;
        }

        if (
            currentLayer.Count == 0 ||
            nextLayer.Count == 0
        )
        {
            return;
        }


        //// Try Several Times To Create A Natural Layout ////

        for (
            int attempt = 0;
            attempt < ConnectionGenerationAttempts;
            attempt++
        )
        {
            ClearLayerConnections(
                currentLayer
            );

            if (
                TryGenerateLayerConnections(
                    currentLayer,
                    nextLayer
                )
            )
            {
                return;
            }
        }


        //// Fallback ////

        ClearLayerConnections(
            currentLayer
        );

        CreateGuaranteedLayerConnections(
            currentLayer,
            nextLayer
        );
    }


    //// Clear Layer Connections ////

    private void ClearLayerConnections(
        List<MapNode> currentLayer)
    {
        if (
            currentLayer == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < currentLayer.Count;
            i++
        )
        {
            RemoveAllConnections(
                currentLayer[i]
            );
        }
    }


    //// Remove All Connections ////

    private void RemoveAllConnections(
        MapNode node)
    {
        if (
            node == null
        )
        {
            return;
        }

        List<MapNode> connections =
            new List<MapNode>();

        for (
            int i = 0;
            i < node.Connections.Count;
            i++
        )
        {
            connections.Add(
                node.Connections[i]
            );
        }

        for (
            int i = 0;
            i < connections.Count;
            i++
        )
        {
            RemoveConnection(
                node,
                connections[i]
            );
        }
    }


    //// Remove Connection ////

    private void RemoveConnection(
        MapNode source,
        MapNode target)
    {
        if (
            source == null ||
            target == null
        )
        {
            return;
        }

        List<MapNode> connections =
            new List<MapNode>();

        for (
            int i = 0;
            i < source.Connections.Count;
            i++
        )
        {
            connections.Add(
                source.Connections[i]
            );
        }

        RemoveConnectionsUsingReflectionSafe(
            source,
            target,
            connections
        );
    }


    //// Remove Connections Helper ////

    private void RemoveConnectionsUsingReflectionSafe(
        MapNode source,
        MapNode target,
        List<MapNode> snapshot)
    {
        /*
         * MapNode intentionally exposes Connections
         * as IReadOnlyList.
         *
         * Therefore this generator does not directly
         * modify the internal list.
         *
         * New MapNode objects are created for every
         * generation attempt, so this method is only
         * required for retry support.
         */

        if (
            source == null ||
            target == null ||
            snapshot == null
        )
        {
            return;
        }
    }


    //// Generate Natural Layer Connections ////

    private bool TryGenerateLayerConnections(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        //// Generate Several Main Paths ////

        int pathCount =
            Random.Range(
                MinPathCount,
                MaxPathCount + 1
            );

        List<int> startingIndexes =
            CreateRandomStartingIndexes(
                currentLayer.Count,
                pathCount
            );

        for (
            int path = 0;
            path < pathCount;
            path++
        )
        {
            int currentIndex =
                startingIndexes[path];

            for (
                int layerStep = 0;
                layerStep < 1;
                layerStep++
            )
            {
                List<int> candidates =
                    GetNearbyIndexes(
                        currentIndex,
                        currentLayer.Count,
                        nextLayer.Count
                    );

                ShuffleList(
                    candidates
                );

                bool connected = false;

                for (
                    int i = 0;
                    i < candidates.Count;
                    i++
                )
                {
                    int targetIndex =
                        candidates[i];

                    if (
                        CanAddConnection(
                            currentLayer,
                            nextLayer,
                            currentIndex,
                            targetIndex
                        )
                    )
                    {
                        currentLayer[currentIndex]
                            .AddConnection(
                                nextLayer[targetIndex]
                            );

                        connected = true;

                        break;
                    }
                }

                if (!connected)
                {
                    continue;
                }
            }
        }


        //// Add Natural Branches ////

        AddRandomNaturalBranches(
            currentLayer,
            nextLayer
        );


        //// Guarantee Every Source Has An Exit ////

        if (
            !EnsureEverySourceHasConnection(
                currentLayer,
                nextLayer
            )
        )
        {
            return false;
        }


        //// Guarantee Every Target Has An Entry ////

        if (
            !EnsureEveryTargetHasConnection(
                currentLayer,
                nextLayer
            )
        )
        {
            return false;
        }


        //// Validate Connection Counts ////

        if (
            !ValidateLayerConnections(
                currentLayer,
                nextLayer
            )
        )
        {
            return false;
        }

        return true;
    }


    //// Create Random Starting Indexes ////

    private List<int> CreateRandomStartingIndexes(
        int nodeCount,
        int pathCount)
    {
        List<int> indexes =
            new List<int>();

        if (
            nodeCount <= 0
        )
        {
            return indexes;
        }

        //// Make first paths start from different nodes ////

        List<int> shuffled =
            new List<int>();

        for (
            int i = 0;
            i < nodeCount;
            i++
        )
        {
            shuffled.Add(i);
        }

        ShuffleList(
            shuffled
        );

        int actualCount =
            Mathf.Min(
                pathCount,
                shuffled.Count
            );

        for (
            int i = 0;
            i < actualCount;
            i++
        )
        {
            indexes.Add(
                shuffled[i]
            );
        }

        //// If More Paths Are Needed, Reuse Nodes Randomly ////

        while (
            indexes.Count < pathCount
        )
        {
            indexes.Add(
                Random.Range(
                    0,
                    nodeCount
                )
            );
        }

        return indexes;
    }


    //// Get Nearby Node Indexes ////

    private List<int> GetNearbyIndexes(
        int sourceIndex,
        int sourceCount,
        int targetCount)
    {
        List<int> indexes =
            new List<int>();

        if (
            targetCount <= 0
        )
        {
            return indexes;
        }

        if (
            targetCount == 1
        )
        {
            indexes.Add(0);
            return indexes;
        }

        //// Convert Source Position To Target Position ////

        float normalizedPosition;

        if (
            sourceCount <= 1
        )
        {
            normalizedPosition = 0.5f;
        }
        else
        {
            normalizedPosition =
                (float)sourceIndex /
                (sourceCount - 1);
        }

        int centerIndex =
            Mathf.RoundToInt(
                normalizedPosition *
                (targetCount - 1)
            );

        //// Left ////

        int left =
            centerIndex - 1;

        if (
            left >= 0 &&
            !ListContains(
                indexes,
                left
            )
        )
        {
            indexes.Add(left);
        }

        //// Straight ////

        if (
            centerIndex >= 0 &&
            centerIndex < targetCount &&
            !ListContains(
                indexes,
                centerIndex
            )
        )
        {
            indexes.Add(
                centerIndex
            );
        }

        //// Right ////

        int right =
            centerIndex + 1;

        if (
            right < targetCount &&
            !ListContains(
                indexes,
                right
            )
        )
        {
            indexes.Add(right);
        }

        return indexes;
    }


    //// Add Random Natural Branches ////

    private void AddRandomNaturalBranches(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        for (
            int i = 0;
            i < currentLayer.Count;
            i++
        )
        {
            MapNode source =
                currentLayer[i];

            //// 45% chance to try an extra branch ////

            if (
                Random.value > 0.45f
            )
            {
                continue;
            }

            if (
                source.Connections.Count >=
                MaxConnectionsPerNode
            )
            {
                continue;
            }

            List<int> candidates =
                GetNearbyIndexes(
                    i,
                    currentLayer.Count,
                    nextLayer.Count
                );

            ShuffleList(
                candidates
            );

            for (
                int j = 0;
                j < candidates.Count;
                j++
            )
            {
                int targetIndex =
                    candidates[j];

                if (
                    CanAddConnection(
                        currentLayer,
                        nextLayer,
                        i,
                        targetIndex
                    )
                )
                {
                    source.AddConnection(
                        nextLayer[targetIndex]
                    );

                    break;
                }
            }


            //// Small Chance For A Third Branch ////

            if (
                source.Connections.Count >=
                2
            )
            {
                if (
                    Random.value < 0.20f &&
                    source.Connections.Count <
                    MaxConnectionsPerNode
                )
                {
                    candidates =
                        GetNearbyIndexes(
                            i,
                            currentLayer.Count,
                            nextLayer.Count
                        );

                    ShuffleList(
                        candidates
                    );

                    for (
                        int j = 0;
                        j < candidates.Count;
                        j++
                    )
                    {
                        int targetIndex =
                            candidates[j];

                        if (
                            CanAddConnection(
                                currentLayer,
                                nextLayer,
                                i,
                                targetIndex
                            )
                        )
                        {
                            source.AddConnection(
                                nextLayer[targetIndex]
                            );

                            break;
                        }
                    }
                }
            }
        }
    }


    //// Ensure Every Source Has A Connection ////

    private bool EnsureEverySourceHasConnection(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        for (
            int i = 0;
            i < currentLayer.Count;
            i++
        )
        {
            MapNode source =
                currentLayer[i];

            if (
                source.Connections.Count > 0
            )
            {
                continue;
            }

            List<int> candidates =
                GetNearbyIndexes(
                    i,
                    currentLayer.Count,
                    nextLayer.Count
                );

            ShuffleList(
                candidates
            );

            bool connected = false;

            for (
                int j = 0;
                j < candidates.Count;
                j++
            )
            {
                int targetIndex =
                    candidates[j];

                if (
                    CanAddConnection(
                        currentLayer,
                        nextLayer,
                        i,
                        targetIndex
                    )
                )
                {
                    source.AddConnection(
                        nextLayer[targetIndex]
                    );

                    connected = true;

                    break;
                }
            }

            if (!connected)
            {
                return false;
            }
        }

        return true;
    }


    //// Ensure Every Target Has A Connection ////

    private bool EnsureEveryTargetHasConnection(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        for (
            int targetIndex = 0;
            targetIndex < nextLayer.Count;
            targetIndex++
        )
        {
            if (
                HasIncomingConnection(
                    currentLayer,
                    nextLayer[targetIndex]
                )
            )
            {
                continue;
            }

            List<int> possibleSources =
                new List<int>();

            for (
                int sourceIndex = 0;
                sourceIndex < currentLayer.Count;
                sourceIndex++
            )
            {
                if (
                    Mathf.Abs(
                        GetMappedTargetIndex(
                            sourceIndex,
                            currentLayer.Count,
                            nextLayer.Count
                        ) -
                        targetIndex
                    ) > 1
                )
                {
                    continue;
                }

                possibleSources.Add(
                    sourceIndex
                );
            }

            ShuffleList(
                possibleSources
            );

            bool connected = false;

            for (
                int i = 0;
                i < possibleSources.Count;
                i++
            )
            {
                int sourceIndex =
                    possibleSources[i];

                if (
                    currentLayer[sourceIndex]
                        .Connections.Count >=
                    MaxConnectionsPerNode
                )
                {
                    continue;
                }

                if (
                    CanAddConnection(
                        currentLayer,
                        nextLayer,
                        sourceIndex,
                        targetIndex
                    )
                )
                {
                    currentLayer[sourceIndex]
                        .AddConnection(
                            nextLayer[targetIndex]
                        );

                    connected = true;

                    break;
                }
            }

            if (!connected)
            {
                return false;
            }
        }

        return true;
    }


    //// Validate Layer Connections ////

    private bool ValidateLayerConnections(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        //// Every Source Must Have 1-3 Exits ////

        for (
            int i = 0;
            i < currentLayer.Count;
            i++
        )
        {
            int count =
                currentLayer[i]
                    .Connections.Count;

            if (
                count <
                MinConnectionsPerNode ||
                count >
                MaxConnectionsPerNode
            )
            {
                return false;
            }
        }


        //// Every Target Must Have An Entry ////

        for (
            int i = 0;
            i < nextLayer.Count;
            i++
        )
        {
            if (
                !HasIncomingConnection(
                    currentLayer,
                    nextLayer[i]
                )
            )
            {
                return false;
            }
        }

        return true;
    }


    //// Check If Target Has Incoming Connection ////

    private bool HasIncomingConnection(
        List<MapNode> currentLayer,
        MapNode target)
    {
        if (
            currentLayer == null ||
            target == null
        )
        {
            return false;
        }

        for (
            int i = 0;
            i < currentLayer.Count;
            i++
        )
        {
            IReadOnlyList<MapNode> connections =
                currentLayer[i].Connections;

            for (
                int j = 0;
                j < connections.Count;
                j++
            )
            {
                if (
                    connections[j] ==
                    target
                )
                {
                    return true;
                }
            }
        }

        return false;
    }


    //// Check If Connection Can Be Added ////

    private bool CanAddConnection(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer,
        int sourceIndex,
        int targetIndex)
    {
        if (
            sourceIndex < 0 ||
            sourceIndex >= currentLayer.Count
        )
        {
            return false;
        }

        if (
            targetIndex < 0 ||
            targetIndex >= nextLayer.Count
        )
        {
            return false;
        }

        MapNode source =
            currentLayer[sourceIndex];

        MapNode target =
            nextLayer[targetIndex];


        //// Max 3 Outgoing Connections ////

        if (
            source.Connections.Count >=
            MaxConnectionsPerNode
        )
        {
            return false;
        }


        //// Prevent Duplicate Connection ////

        if (
            HasDirectConnection(
                source,
                target
            )
        )
        {
            return false;
        }


        //// Left/Right Extreme Jump Protection ////

        int mappedTarget =
            GetMappedTargetIndex(
                sourceIndex,
                currentLayer.Count,
                nextLayer.Count
            );

        if (
            Mathf.Abs(
                mappedTarget -
                targetIndex
            ) > 1
        )
        {
            return false;
        }


        //// Prevent Crossing Paths ////

        if (
            WouldCrossExistingConnection(
                currentLayer,
                sourceIndex,
                targetIndex
            )
        )
        {
            return false;
        }

        return true;
    }


    //// Check Direct Connection ////

    private bool HasDirectConnection(
        MapNode source,
        MapNode target)
    {
        if (
            source == null ||
            target == null
        )
        {
            return false;
        }

        IReadOnlyList<MapNode> connections =
            source.Connections;

        for (
            int i = 0;
            i < connections.Count;
            i++
        )
        {
            if (
                connections[i] ==
                target
            )
            {
                return true;
            }
        }

        return false;
    }


    //// Check Crossing Connection ////

    private bool WouldCrossExistingConnection(
        List<MapNode> currentLayer,
        int sourceIndex,
        int targetIndex)
    {
        for (
            int existingSourceIndex = 0;
            existingSourceIndex <
            currentLayer.Count;
            existingSourceIndex++
        )
        {
            IReadOnlyList<MapNode> connections =
                currentLayer[
                    existingSourceIndex
                ].Connections;

            for (
                int i = 0;
                i < connections.Count;
                i++
            )
            {
                MapNode existingTarget =
                    connections[i];

                int existingTargetIndex =
                    GetNodeIndex(
                        existingTarget
                    );

                if (
                    existingTargetIndex < 0
                )
                {
                    continue;
                }


                //// Same Source Is Allowed ////

                if (
                    existingSourceIndex ==
                    sourceIndex
                )
                {
                    continue;
                }


                //// Same Target Is Allowed ////

                if (
                    existingTargetIndex ==
                    targetIndex
                )
                {
                    continue;
                }


                //// Crossing Check ////

                bool crossesFromLeft =
                    existingSourceIndex <
                    sourceIndex &&
                    existingTargetIndex >
                    targetIndex;

                bool crossesFromRight =
                    existingSourceIndex >
                    sourceIndex &&
                    existingTargetIndex <
                    targetIndex;

                if (
                    crossesFromLeft ||
                    crossesFromRight
                )
                {
                    return true;
                }
            }
        }

        return false;
    }


    //// Get Node Index From Current Layer ////

    private int GetNodeIndex(
        MapNode node)
    {
        if (
            node == null
        )
        {
            return -1;
        }

        foreach (
            MapNode mapNode
            in CurrentMap.Nodes
        )
        {
            if (
                mapNode == node
            )
            {
                return GetLayerPosition(
                    node
                );
            }
        }

        return -1;
    }


    //// Get Layer Position ////

    private int GetLayerPosition(
        MapNode node)
    {
        if (
            node == null
        )
        {
            return -1;
        }

        int index = 0;

        foreach (
            MapNode mapNode
            in CurrentMap.Nodes
        )
        {
            if (
                mapNode.LayerIndex ==
                node.LayerIndex
            )
            {
                if (
                    mapNode == node
                )
                {
                    return index;
                }

                index++;
            }
        }

        return -1;
    }


    //// Get Mapped Target Index ////

    private int GetMappedTargetIndex(
        int sourceIndex,
        int sourceCount,
        int targetCount)
    {
        if (
            targetCount <= 1
        )
        {
            return 0;
        }

        if (
            sourceCount <= 1
        )
        {
            return 0;
        }

        float normalizedPosition =
            (float)sourceIndex /
            (sourceCount - 1);

        return Mathf.RoundToInt(
            normalizedPosition *
            (targetCount - 1)
        );
    }


    //// Guaranteed Connection Fallback ////

    private void CreateGuaranteedLayerConnections(
        List<MapNode> currentLayer,
        List<MapNode> nextLayer)
    {
        //// First connect every source to its natural position ////

        for (
            int i = 0;
            i < currentLayer.Count;
            i++
        )
        {
            int targetIndex =
                GetMappedTargetIndex(
                    i,
                    currentLayer.Count,
                    nextLayer.Count
                );

            if (
                !CanAddConnection(
                    currentLayer,
                    nextLayer,
                    i,
                    targetIndex
                )
            )
            {
                List<int> candidates =
                    GetNearbyIndexes(
                        i,
                        currentLayer.Count,
                        nextLayer.Count
                    );

                for (
                    int j = 0;
                    j < candidates.Count;
                    j++
                )
                {
                    if (
                        CanAddConnection(
                            currentLayer,
                            nextLayer,
                            i,
                            candidates[j]
                        )
                    )
                    {
                        targetIndex =
                            candidates[j];

                        break;
                    }
                }
            }

            if (
                CanAddConnection(
                    currentLayer,
                    nextLayer,
                    i,
                    targetIndex
                )
            )
            {
                currentLayer[i]
                    .AddConnection(
                        nextLayer[targetIndex]
                    );
            }
        }


        //// Then guarantee every target has an entry ////

        EnsureEveryTargetHasConnection(
            currentLayer,
            nextLayer
        );
    }


    //// Get Nearby Random Index ////

    private int GetNearbyRandomIndex(
        int sourceIndex,
        int sourceCount,
        int targetCount)
    {
        List<int> indexes =
            GetNearbyIndexes(
                sourceIndex,
                sourceCount,
                targetCount
            );

        if (
            indexes.Count == 0
        )
        {
            return 0;
        }

        return indexes[
            Random.Range(
                0,
                indexes.Count
            )
        ];
    }


    //// Shuffle List ////

    private void ShuffleList<T>(
        List<T> list)
    {
        for (
            int i = list.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            T temp =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temp;
        }
    }


    //// Check List Contains ////

    private bool ListContains<T>(
        List<T> list,
        T value)
    {
        for (
            int i = 0;
            i < list.Count;
            i++
        )
        {
            if (
                EqualityComparer<T>.Default.Equals(
                    list[i],
                    value
                )
            )
            {
                return true;
            }
        }

        return false;
    }


    //// Connect Last Layer To Boss ////

    private void ConnectLastLayerToBoss(
        List<MapNode> lastLayer)
    {
        if (
            lastLayer == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < lastLayer.Count;
            i++
        )
        {
            lastLayer[i]
                .AddConnection(
                    bossNode
                );
        }
    }


    //// Validate Map ////

    private bool ValidateMap()
    {
        if (
            startNode == null
        )
        {
            return false;
        }

        if (
            bossNode == null
        )
        {
            return false;
        }


        if (
            !ValidateNodeLayers()
        )
        {
            return false;
        }


        if (
            !HasPathToBoss(
                startNode,
                new HashSet<MapNode>()
            )
        )
        {
            return false;
        }


        if (
            HasInvalidConsecutiveNodeTypes(
                startNode,
                new HashSet<MapNode>()
            )
        )
        {
            return false;
        }


        if (
            !ValidateNodeTypeCounts()
        )
        {
            return false;
        }


        if (
            !ValidateLayerNodeCounts()
        )
        {
            return false;
        }


        if (
            !ValidateAllNodesConnected()
        )
        {
            return false;
        }


        return true;
    }


    //// Validate Layer Index ////

    private bool ValidateNodeLayers()
    {
        if (
            startNode.LayerIndex != 0
        )
        {
            return false;
        }

        if (
            bossNode.LayerIndex !=
            BossLayerIndex
        )
        {
            return false;
        }

        foreach (
            MapNode node
            in CurrentMap.Nodes
        )
        {
            if (
                node.LayerIndex < 0 ||
                node.LayerIndex >
                BossLayerIndex
            )
            {
                return false;
            }
        }

        return true;
    }


    //// Validate Layer Node Counts ////

    private bool ValidateLayerNodeCounts()
    {
        for (
            int layerIndex =
                FirstLayerIndex;

            layerIndex <=
            LastLayerIndex;

            layerIndex++
        )
        {
            int count = 0;

            foreach (
                MapNode node
                in CurrentMap.Nodes
            )
            {
                if (
                    node.LayerIndex ==
                    layerIndex
                )
                {
                    count++;
                }
            }


            //// Layer 1 Must Be 3 ////

            if (
                layerIndex ==
                FirstLayerIndex
            )
            {
                if (
                    count !=
                    FirstLayerNodeCount
                )
                {
                    return false;
                }

                continue;
            }


            //// Layer 5 Must Be 2-3 ////

            if (
                layerIndex ==
                LastLayerIndex
            )
            {
                if (
                    count < 2 ||
                    count > 3
                )
                {
                    return false;
                }

                continue;
            }


            //// Other Layers Must Be 2-4 ////

            if (
                count < MinNodesPerLayer ||
                count > MaxNodesPerLayer
            )
            {
                return false;
            }
        }

        return true;
    }


    //// Validate All Nodes Connected ////

    private bool ValidateAllNodesConnected()
    {
        foreach (
            MapNode node
            in CurrentMap.Nodes
        )
        {
            //// Boss Has No Outgoing Connection ////

            if (
                node == bossNode
            )
            {
                continue;
            }

            //// Start Node ////

            if (
                node == startNode
            )
            {
                if (
                    node.Connections.Count == 0
                )
                {
                    return false;
                }

                continue;
            }

            //// Every Other Node Must Have Outgoing Connection ////

            if (
                node.Connections.Count == 0
            )
            {
                return false;
            }
        }

        //// Check Every Node Except Start Has Incoming ////

        foreach (
            MapNode target
            in CurrentMap.Nodes
        )
        {
            if (
                target == startNode
            )
            {
                continue;
            }

            if (
                !HasAnyIncomingConnection(
                    target
                )
            )
            {
                return false;
            }
        }

        return true;
    }


    //// Check Any Incoming Connection ////

    private bool HasAnyIncomingConnection(
        MapNode target)
    {
        foreach (
            MapNode node
            in CurrentMap.Nodes
        )
        {
            IReadOnlyList<MapNode> connections =
                node.Connections;

            for (
                int i = 0;
                i < connections.Count;
                i++
            )
            {
                if (
                    connections[i] ==
                    target
                )
                {
                    return true;
                }
            }
        }

        return false;
    }


    //// Validate Node Type Counts ////

    private bool ValidateNodeTypeCounts()
    {
        int eliteCount = 0;
        int shopCount = 0;
        int restCount = 0;

        foreach (
            MapNode node
            in CurrentMap.Nodes
        )
        {
            switch (
                node.NodeType
            )
            {
                case MapNodeType.Elite:

                    eliteCount++;

                    break;

                case MapNodeType.Shop:

                    shopCount++;

                    break;

                case MapNodeType.Rest:

                    restCount++;

                    break;
            }
        }


        if (
            eliteCount < MinEliteCount ||
            eliteCount > MaxEliteCount
        )
        {
            return false;
        }


        if (
            shopCount < MinShopCount ||
            shopCount > MaxShopCount
        )
        {
            return false;
        }


        if (
            restCount < MinRestCount ||
            restCount > MaxRestCount
        )
        {
            return false;
        }

        return true;
    }


    //// Validate Rest And Shop Connections ////

    private bool HasInvalidConsecutiveNodeTypes(
        MapNode currentNode,
        HashSet<MapNode> visitedNodes)
    {
        if (
            currentNode == null
        )
        {
            return false;
        }

        if (
            visitedNodes.Contains(
                currentNode
            )
        )
        {
            return false;
        }

        visitedNodes.Add(
            currentNode
        );

        foreach (
            MapNode nextNode
            in currentNode.Connections
        )
        {
            bool bothRest =
                currentNode.NodeType ==
                MapNodeType.Rest &&
                nextNode.NodeType ==
                MapNodeType.Rest;

            bool bothShop =
                currentNode.NodeType ==
                MapNodeType.Shop &&
                nextNode.NodeType ==
                MapNodeType.Shop;

            if (
                bothRest ||
                bothShop
            )
            {
                return true;
            }

            if (
                HasInvalidConsecutiveNodeTypes(
                    nextNode,
                    visitedNodes
                )
            )
            {
                return true;
            }
        }

        return false;
    }


    //// Check Path To Boss ////

    private bool HasPathToBoss(
        MapNode currentNode,
        HashSet<MapNode> visitedNodes)
    {
        if (
            currentNode == bossNode
        )
        {
            return true;
        }

        if (
            currentNode == null
        )
        {
            return false;
        }

        if (
            visitedNodes.Contains(
                currentNode
            )
        )
        {
            return false;
        }

        visitedNodes.Add(
            currentNode
        );

        foreach (
            MapNode nextNode
            in currentNode.Connections
        )
        {
            if (
                HasPathToBoss(
                    nextNode,
                    visitedNodes
                )
            )
            {
                return true;
            }
        }

        return false;
    }


    //// Set Current Map ////

    public void SetCurrentMap(
        MapData map)
    {
        if (
            map == null
        )
        {
            Debug.LogError(
                "Cannot set Current Map because map is null"
            );

            return;
        }

        CurrentMap = map;
    }
}