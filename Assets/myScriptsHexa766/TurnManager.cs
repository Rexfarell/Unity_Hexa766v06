using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour

{
    // ---------------- SINGLETON ----------------
    public static TurnManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ---------------- CONFIG ----------------
    [Header("Visuals")]
    public Material sphereMaterial;
    public float sphereSize = 0.32f;

    // ---------------- STATE ----------------
    private Map1HexGrid map;
    public List<PlayerUnit> players = new List<PlayerUnit>();
    
    
    private int currentIndex = -1;
    public PlayerUnit current { get; private set; }

    private List<GameObject> highlights = new List<GameObject>();
    private bool isProcessingMove = false;

    // ---------------- INIT ----------------
    void Start()
    {
        Debug.Log("[TURN] TurnManager Start");
        StartCoroutine(WaitForMapThenStartTurns());
    }


    IEnumerator DelayedStart()
    {
        yield return null; // wait ONE frame for PlayerUnit.Awake()

        if (players.Count == 0)
        {
            Debug.LogError("[TURN] No players registered after delay");
            yield break;
        }

        Debug.Log($"[TURN] {players.Count} players registered. Starting turns.");
        StartNextTurn();
    }


    // ---------------- TURN FLOW ----------------
    void StartNextTurn()
    {
        Debug.Log("=== START NEXT TURN CALLED ===");

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("[TURN] No players registered");
            return;
        }

        ClearHighlights();
        isProcessingMove = false;

        currentIndex = (currentIndex + 1) % players.Count;
        current = players[currentIndex];

        if (current == null)
        {
            Debug.LogError("[TURN] Current player is NULL");
            return;
        }

        current.ResetTurn(); // ✅ THIS EXISTS

        CameraFollow cam = FindObjectOfType<CameraFollow>();
        if (cam != null)
        {
            cam.SetTarget(current.transform);
        }

        Debug.Log($"[TURN] Current player: {current.name}");

        HighlightValidMoves();
    }





    public void TryEndTurn()
    {
        if (isProcessingMove || current == null)
            return;

        Debug.Log($"[TURN END] {current.name}");
        StartNextTurn();
    }

    // ---------------- MOVEMENT ----------------
    public void OnSphereClicked(Vector2Int tile, int vertex)
    {
        Debug.Log("=== SPHERE CLICK RECEIVED ===");
        Debug.Log($"Processing={isProcessingMove}");
        if (isProcessingMove || current == null)
            return;
        
        isProcessingMove = true;

        Debug.Log($"[MOVE] {current.name} → {tile} v{vertex}");

        bool moved = current.MoveToTile(tile, vertex);
        if (!moved)
        {
            isProcessingMove = false;
            return;
        }

        ClearHighlights();

    }

    public void HandleMoveFinished()
    {
        if (current == null)
            return;

        Debug.Log("[TURN] Move finished.");

        isProcessingMove = false;

        StartNextTurn();
    }



    // ---------------- VISUALS ----------------
    void HighlightValidMoves()
    {
        Debug.Log("=== HIGHLIGHT VALID MOVES ===");

        ClearHighlights();

        // 🔒 HARD GUARD: current player
        if (current == null)
        {
            Debug.LogError("[HIGHLIGHT] current player is NULL");
            return;
        }

        // 🔑 HARD FIX: resolve map HERE, not in Start()
        if (map == null)
        {
            map = FindObjectOfType<Map1HexGrid>();
            Debug.Log("[HIGHLIGHT] map was NULL — attempting late bind");
        }

        if (map == null)
        {
            Debug.LogError("[HIGHLIGHT] map STILL NULL — aborting highlight");
            return;
        }

        if (!map.hasGenerated)
        {
            Debug.LogWarning("[HIGHLIGHT] map exists but not generated yet");
            return;
        }

        Debug.Log($"[HIGHLIGHT] Current player: {current.name}");
        Debug.Log($"[HIGHLIGHT] Move Range = {map.defaultMovePoints}");

        var reachable = map.GetReachableVerticesFromPlayerAsPairs(
            current.gameObject,
            map.defaultMovePoints
        );

        Debug.Log($"[HIGHLIGHT] Reachable count = {reachable.Count}");

        foreach (var pair in reachable)
        {
            Vector3 pos = map.GetVertexPosition(pair.tileName, pair.vertexIndex);

            GameObject sphere = Instantiate(
                map.vertexMarkerPrefab,
                pos + Vector3.up * 0.05f,
                Quaternion.identity
            );

            sphere.transform.localScale = Vector3.one * 0.03f;

            ClickableSphere click = sphere.GetComponent<ClickableSphere>();
            if (click == null)
            {
                Debug.LogError("[HIGHLIGHT] ClickableSphere missing on prefab");
                Destroy(sphere);
                continue;
            }

            click.targetTile = map.GetTileGridCoord(pair.tileName);
            click.targetVertex = pair.vertexIndex;
            click.turnManager = this;

            highlights.Add(sphere);
        }

        Physics.SyncTransforms();

        Debug.Log($"[HIGHLIGHT] Spawned {highlights.Count} highlight spheres");
    }




    void ClearHighlights()
    {
        foreach (var h in highlights)
            Destroy(h);

        highlights.Clear();
    }

    public bool IsVertexOccupied(Vector2Int tile, int vertex, PlayerUnit ignore = null)
    {
        foreach (var p in players)
        {
            if (p == null || p == ignore) continue;
            if (!p.gameObject.activeSelf) continue;

            if (p.currentTileCoord == tile &&
                p.currentVertexIndex == vertex)
            {
                return true;
            }
        }
        return false;
    }

    public void ShowReachFor(GameObject playerObj)
    {
        if (playerObj == null) return;

        PlayerUnit pu = playerObj.GetComponentInChildren<PlayerUnit>(true);
        if (pu == null)
        {
            Debug.LogError("ShowReachFor: No PlayerUnit on " + playerObj.name);
            return;
        }

        PlayerUnit previous = current;

        current = pu;
        HighlightValidMoves();
        current = previous;
    }

    public void RestartTurnsFromBeginning()
    {
        StopAllCoroutines();
        StartCoroutine(RestartTurnsNextFrame());
    }

    IEnumerator RestartTurnsNextFrame()
    {
        yield return null; // wait ONE frame

        ClearHighlights();
        isProcessingMove = false;

        currentIndex = -1;
        current = null;

        Debug.Log("=== TURN SYSTEM RESTARTED AFTER MAP READY ===");
        StartNextTurn();
    }

    IEnumerator WaitForMapThenStartTurns()
    {
        // wait for map instance
        while (map == null)
        {
            map = FindObjectOfType<Map1HexGrid>();
            yield return null;
        }

        Debug.Log("[TURN] Map found, waiting for generation...");

        // wait for FULL generation (vertex graph ready)
        while (!map.hasGenerated || map.vertexToTile == null || map.vertexToTile.Count == 0)
        {
            yield return null;
        }

        Debug.Log("[TURN] Map fully generated. Vertex graph ready.");

        // wait one extra frame for safety
        yield return null;

        if (players.Count == 0)
        {
            Debug.LogError("[TURN] No players registered after map ready!");
            yield break;
        }

        Debug.Log($"[TURN] {players.Count} players registered. Starting turns.");

        StartNextTurn();
    }
    

}
