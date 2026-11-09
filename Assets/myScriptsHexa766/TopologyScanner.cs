using System.Collections.Generic;
using UnityEngine;

public class TopologyScanner : MonoBehaviour
{
    private Map1HexGrid map;

    public bool RunScan()
    {
        map = FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
        {
            Debug.LogError(
                "[TOPOLOGY SCAN] Map1HexGrid not found."
            );

            return false;
        }

        if (map.tileGameObjectMap == null ||
            map.tileGameObjectMap.Count == 0)
        {
            Debug.LogWarning(
                "[TOPOLOGY SCAN] Map contains no active tiles."
            );

            return false;
        }

        Dictionary<string, HashSet<string>> graph =
            BuildLogicalHexGraph();

        if (graph.Count == 0)
        {
            Debug.LogWarning(
                "[TOPOLOGY SCAN] No active graph nodes found."
            );

            return false;
        }

        HashSet<string> articulationPoints =
            FindArticulationPoints(graph);
        HashSet<string> removalTestPoints =
            FindArticulationPointsByRemoval(graph);
        int edgeCount = 0;

        foreach (var pair in graph)
        {
            edgeCount += pair.Value.Count;
        }

        // Every undirected edge was counted twice.
        edgeCount /= 2;

        int connectedComponents =
    CountConnectedComponents(graph);

        lastGraph =
    graph;

        lastArticulationPoints =
            articulationPoints;

        lastConnectedComponents =
            connectedComponents;

        bool validationMatches =
            articulationPoints.SetEquals(
                removalTestPoints
            );

        Debug.Log(
            $"[TOPOLOGY VALIDATION] " +
            $"Tarjan={articulationPoints.Count} | " +
            $"RemovalTest={removalTestPoints.Count} | " +
            $"Match={validationMatches}"
        );



        Debug.Log(
            $"[TOPOLOGY SCAN] COMPLETE | " +
            $"Nodes={graph.Count} | " +
            $"Edges={edgeCount} | " +
            $"Components={connectedComponents} | " +
            $"ArticulationPoints={articulationPoints.Count}"
        );

        if (articulationPoints.Count == 0)
        {
            Debug.Log(
                "[TOPOLOGY SCAN] No articulation points detected."
            );
        }
        else
        {
            foreach (string tileName in articulationPoints)
            {
                int componentsAfterRemoval =
    CountConnectedComponentsWithoutNode(
        graph,
        tileName
    );

                Debug.Log(
                    $"[TOPOLOGY CRITICAL NODE] {tileName} | " +
                    $"Degree={graph[tileName].Count} | " +
                    $"RemovalComponents={componentsAfterRemoval} | " +
                    $"ComponentIncrease=" +
                    $"{componentsAfterRemoval - connectedComponents}"
                );
            }
        }

        return true;
    }

    private Dictionary<string, HashSet<string>> lastGraph =
    new Dictionary<string, HashSet<string>>();

    private HashSet<string> lastArticulationPoints =
        new HashSet<string>();

    private int lastConnectedComponents = 0;

    public IReadOnlyDictionary<string, HashSet<string>> LastGraph =>
        lastGraph;

    public IReadOnlyCollection<string> LastArticulationPoints =>
        lastArticulationPoints;

    public int LastConnectedComponents =>
        lastConnectedComponents;

    public bool HasScanData =>
        lastGraph.Count > 0;

    public bool IsArticulationPoint(string tileName)
    {
        return lastArticulationPoints.Contains(tileName);
    }
    // =====================================================
    // BUILD LOGICAL HEX GRAPH
    // =====================================================

    private Dictionary<string, HashSet<string>>
        BuildLogicalHexGraph()
    {
        Dictionary<string, HashSet<string>> graph =
            new Dictionary<string, HashSet<string>>();

        // ---------------------------------------------
        // REGISTER ACTIVE HEXES
        // ---------------------------------------------
        foreach (var pair in map.tileGameObjectMap)
        {
            string tileName = pair.Key;
            GameObject tile = pair.Value;

            if (tile == null)
                continue;

            HexCapacity capacity =
                tile.GetComponent<HexCapacity>();

            if (capacity != null &&
                capacity.IsCollapsed)
            {
                continue;
            }

            graph[tileName] =
                new HashSet<string>();
        }


        // ---------------------------------------------
        // CONVERT EXISTING VERTEX CONNECTIONS
        // INTO LOGICAL HEX CONNECTIONS
        // ---------------------------------------------
        foreach (var pair in map.vertexGraph)
        {
            Vector3 sourceVertex = pair.Key;

            if (!map.vertexToTile.TryGetValue(
                    sourceVertex,
                    out var sourceInfo))
            {
                continue;
            }

            string sourceTile =
                sourceInfo.tileName;

            if (!graph.ContainsKey(sourceTile))
                continue;

            foreach (Vector3 targetVertex in pair.Value)
            {
                if (!map.vertexToTile.TryGetValue(
                        targetVertex,
                        out var targetInfo))
                {
                    continue;
                }

                string targetTile =
                    targetInfo.tileName;

                if (sourceTile == targetTile)
                    continue;

                if (!graph.ContainsKey(targetTile))
                    continue;

                graph[sourceTile].Add(targetTile);
                graph[targetTile].Add(sourceTile);
            }
        }

        return graph;
    }


    // =====================================================
    // TARJAN ARTICULATION-POINT SEARCH
    // =====================================================

    private HashSet<string> FindArticulationPoints(
        Dictionary<string, HashSet<string>> graph
    )
    {
        HashSet<string> articulationPoints =
            new HashSet<string>();

        Dictionary<string, int> discovery =
            new Dictionary<string, int>();

        Dictionary<string, int> low =
            new Dictionary<string, int>();

        Dictionary<string, string> parent =
            new Dictionary<string, string>();

        HashSet<string> visited =
            new HashSet<string>();

        int time = 0;

        foreach (string node in graph.Keys)
        {
            if (visited.Contains(node))
                continue;

            DFSArticulation(
                node,
                graph,
                visited,
                discovery,
                low,
                parent,
                articulationPoints,
                ref time
            );
        }

        return articulationPoints;
    }


    private void DFSArticulation(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> visited,
        Dictionary<string, int> discovery,
        Dictionary<string, int> low,
        Dictionary<string, string> parent,
        HashSet<string> articulationPoints,
        ref int time
    )
    {
        visited.Add(node);

        discovery[node] = ++time;
        low[node] = discovery[node];

        int childCount = 0;

        foreach (string neighbor in graph[node])
        {
            if (!visited.Contains(neighbor))
            {
                childCount++;

                parent[neighbor] = node;

                DFSArticulation(
                    neighbor,
                    graph,
                    visited,
                    discovery,
                    low,
                    parent,
                    articulationPoints,
                    ref time
                );

                low[node] =
                    Mathf.Min(
                        low[node],
                        low[neighbor]
                    );

                bool nodeHasParent =
                    parent.ContainsKey(node);

                // Root node:
                // critical if DFS root has >1 child.
                if (!nodeHasParent &&
                    childCount > 1)
                {
                    articulationPoints.Add(node);
                }

                // Non-root:
                // child cannot reach an ancestor
                // without passing through this node.
                if (nodeHasParent &&
                    low[neighbor] >= discovery[node])
                {
                    articulationPoints.Add(node);
                }
            }
            else
            {
                bool isParent =
                    parent.ContainsKey(node) &&
                    parent[node] == neighbor;

                if (!isParent)
                {
                    low[node] =
                        Mathf.Min(
                            low[node],
                            discovery[neighbor]
                        );
                }
            }
        }
    }


    // =====================================================
    // CONNECTED COMPONENT COUNT
    // =====================================================

    private int CountConnectedComponents(
        Dictionary<string, HashSet<string>> graph
    )
    {
        HashSet<string> visited =
            new HashSet<string>();

        int components = 0;

        foreach (string node in graph.Keys)
        {
            if (visited.Contains(node))
                continue;

            components++;

            Stack<string> stack =
                new Stack<string>();

            stack.Push(node);

            while (stack.Count > 0)
            {
                string current =
                    stack.Pop();

                if (!visited.Add(current))
                    continue;

                foreach (string neighbor in graph[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }

        return components;
    }


    // =====================================================
    // ARTICULATION VALIDATION BY NODE REMOVAL
    // =====================================================

    private HashSet<string> FindArticulationPointsByRemoval(
        Dictionary<string, HashSet<string>> graph
    )
    {
        HashSet<string> result =
            new HashSet<string>();

        int baseComponents =
            CountConnectedComponents(graph);

        foreach (string node in graph.Keys)
        {
            int componentsAfterRemoval =
                CountConnectedComponentsWithoutNode(
                    graph,
                    node
                );

            if (componentsAfterRemoval >
                baseComponents)
            {
                result.Add(node);
            }
        }

        return result;
    }


    private int CountConnectedComponentsWithoutNode(
        Dictionary<string, HashSet<string>> graph,
        string removedNode
    )
    {
        HashSet<string> visited =
            new HashSet<string>();

        int components = 0;

        foreach (string node in graph.Keys)
        {
            if (node == removedNode)
                continue;

            if (visited.Contains(node))
                continue;

            components++;

            Stack<string> stack =
                new Stack<string>();

            stack.Push(node);

            while (stack.Count > 0)
            {
                string current =
                    stack.Pop();

                if (current == removedNode)
                    continue;

                if (!visited.Add(current))
                    continue;

                foreach (string neighbor in graph[current])
                {
                    if (neighbor == removedNode)
                        continue;

                    if (!visited.Contains(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }

        return components;
    }
}
