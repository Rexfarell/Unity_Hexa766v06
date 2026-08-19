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
    [Header("ENVIRONMENT DAMAGE")]
    [SerializeField]
    private int environmentalDamage = 25;

    [SerializeField]
    private int attackDamage = 25;
    public Material sphereMaterial;
    public float sphereSize = 0.32f;

    // ---------------- STATE ----------------
    private Map1HexGrid map;
    public List<PlayerUnit> players = new List<PlayerUnit>();
    
    
    private int currentIndex = -1;
    public PlayerUnit current { get; private set; }

    private List<GameObject> highlights = new List<GameObject>();
    private bool isProcessingMove = false;



    // ---------------- ATTACK STATE ----------------
    private PlayerUnit selectedAttackTarget;
    private List<PlayerUnit> pendingAttackTargets = new List<PlayerUnit>();
    private bool isSelectingAdjacentAttack = false;
    private List<GameObject> attackHighlights = new List<GameObject>();

    // ---------------- INIT ----------------
    void Start()
    {
        Debug.Log("[TURN] TurnManager Ready");
    }

    public void BeginMatch()
    {
        Debug.Log("[TURN] Beginning Match");

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
        // TEMPORARY OCCUPANCY CHECK
        foreach (PlayerUnit unit in players)
        {
            if (unit != null)
            {
                Debug.Log(
                    $"[OCCUPANCY CHECK] {unit.name} | " +
                    $"World={unit.transform.position} | " +
                    $"Hex={unit.currentTileCoord} | " +
                    $"Vertex={unit.currentVertexIndex}"
                );
            }
        }

        Debug.Log($"Current = {current.name}");
        Debug.Log($"players[0]={players[0].name}, players[1]={players[1].name}, currentIndex={currentIndex}, current={current.name}");

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
        HighlightAdjacentAttackTargets();
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

        // ---------------------------------------------------------
        // ADJACENT ATTACK TARGET SELECTION
        // ---------------------------------------------------------

        PlayerUnit clickedHostile =
            GetHostileAtClickedVertex(current, tile, vertex);

        if (clickedHostile != null)
        {
            selectedAttackTarget = clickedHostile;
            isSelectingAdjacentAttack = true;

            Debug.Log(
                $"[ATTACK SELECT] {current.name} selected " +
                $"{clickedHostile.name} as adjacent attack target."
            );

            return;
        }
        
        if (current.energy <= 0)
        {
            Debug.Log(
                $"<color=red><b>☠ {current.name} is depleted and cannot move.</b></color>"
            );
            return;
        }

        isProcessingMove = true;

        // =========================================================
        // TEMPORARY ATTACK ADJACENCY TEST
        // =========================================================

        Map1HexGrid map = FindObjectOfType<Map1HexGrid>();

        if (map != null)
        {
            List<Vector2Int> adjacent =
                map.GetAdjacentTileCoords(current.currentTileCoord);

            Debug.Log(
                $"[ATTACK TEST] {current.name} at {current.currentTileCoord} " +
                $"has {adjacent.Count} adjacent hexes."
            );

            foreach (Vector2Int adjacentTile in adjacent)
            {
                Debug.Log(
                    $"[ATTACK TEST] Adjacent tile: {adjacentTile}"
                );
            }
        }
        else
        {
            Debug.LogError("[ATTACK TEST] Map1HexGrid not found!");
        }

        // =========================================================
        // EXISTING MOVEMENT CODE
        // =========================================================

        // 🔊 Play click/movement sound
        if (map != null)
        {
            map.PlayBurstSound(current.transform.position);
        }

        Debug.Log($"[MOVE] {current.name} → {tile} v{vertex}");

        bool moved = current.MoveToTile(tile, vertex);

        Debug.Log(
        $"[POST-MOVE OCCUPANCY] {current.name} " +
        $"requested={tile} v{vertex} | " +
        $"currentTileCoord={current.currentTileCoord} | " +
        $"currentVertexIndex={current.currentVertexIndex}"
        );

        if (!moved)
        {
            isProcessingMove = false;
            return;
        }


        // Environmental damage
        current.TakeEnvironmentalDamage(environmentalDamage);

        ClearHighlights();
    }


    private List<PlayerUnit> GetHostileUnitsOnSameHex(PlayerUnit attacker)
    {
        List<PlayerUnit> targets = new List<PlayerUnit>();

        if (attacker == null)
            return targets;

        foreach (PlayerUnit unit in players)
        {
            if (unit == null)
                continue;

            if (unit == attacker)
                continue;

            if (unit.teamID == attacker.teamID)
                continue;

            if (!unit.gameObject.activeSelf)
                continue;

            if (unit.currentTileCoord == attacker.currentTileCoord)
            {
                targets.Add(unit);

                Debug.Log(
                    $"[ATTACK SAME HEX] {attacker.name} → {unit.name} " +
                    $"on {attacker.currentTileCoord}"
                );
            }
        }

        Debug.Log(
            $"[ATTACK SAME HEX] Total targets = {targets.Count}"
        );

        return targets;
    }

    private List<PlayerUnit> GetHostileUnitsInAdjacentHexes(PlayerUnit attacker)
    {
        List<PlayerUnit> targets = new List<PlayerUnit>();

        if (attacker == null)
            return targets;

        Map1HexGrid map = FindObjectOfType<Map1HexGrid>();

        if (map == null)
        {
            Debug.LogError("[ATTACK ADJACENT] Map1HexGrid not found!");
            return targets;
        }

        List<Vector2Int> adjacentTiles =
            map.GetAdjacentTileCoords(attacker.currentTileCoord);

        foreach (PlayerUnit unit in players)
        {
            if (unit == null)
                continue;

            if (unit == attacker)
                continue;

            if (unit.teamID == attacker.teamID)
                continue;

            if (!unit.gameObject.activeSelf)
                continue;

            if (adjacentTiles.Contains(unit.currentTileCoord))
            {
                targets.Add(unit);

                Debug.Log(
                    $"[ATTACK ADJACENT] Candidate: " +
                    $"{attacker.name} → {unit.name} " +
                    $"at {unit.currentTileCoord} v{unit.currentVertexIndex}"
                );
            }
        }

        Debug.Log(
            $"[ATTACK ADJACENT] Total candidates = {targets.Count}"
        );

        return targets;
    }
    private void HighlightAdjacentAttackTargets()
    {
        if (current == null)
            return;

        Map1HexGrid attackMap = FindObjectOfType<Map1HexGrid>();

        if (attackMap == null)
        {
            Debug.LogError("[ATTACK HIGHLIGHT] Map1HexGrid not found!");
            return;
        }

        List<PlayerUnit> targets =
            GetHostileUnitsInAdjacentHexes(current);

        Debug.Log(
            $"[ATTACK HIGHLIGHT] {current.name} has " +
            $"{targets.Count} adjacent attack target(s)."
        );

        foreach (PlayerUnit target in targets)
        {
            if (target == null)
                continue;

            // The target already occupies the exact vertex we want.
            // Use its existing world position instead of converting
            // its coordinates back through the map.
            Vector3 targetPos = target.transform.position;

            GameObject marker = Instantiate(
                attackMap.vertexMarkerPrefab,
                targetPos + Vector3.up * 0.60f,
                Quaternion.identity
            );

            marker.name = "AttackMarker";

            marker.transform.localScale = Vector3.one * 0.07f;

            ClickableSphere click =
                marker.GetComponent<ClickableSphere>();

            if (click == null)
            {
                Debug.LogError(
                    "[ATTACK HIGHLIGHT] ClickableSphere missing on marker prefab!"
                );

                Destroy(marker);
                continue;
            }

            // The marker represents this occupied enemy vertex.
            click.targetTile = target.currentTileCoord;
            click.targetVertex = target.currentVertexIndex;
            click.turnManager = this;

            attackHighlights.Add(marker);

            Debug.Log(
                $"[ATTACK HIGHLIGHT] Marker created for " +
                $"{target.name} at " +
                $"{target.currentTileCoord} v{target.currentVertexIndex}"
            );
        }

        Physics.SyncTransforms();
    }
    private PlayerUnit GetHostileAtClickedVertex(
    PlayerUnit attacker,
    Vector2Int tile,
    int vertex)
    {
        if (attacker == null)
            return null;

        Map1HexGrid map = FindObjectOfType<Map1HexGrid>();

        if (map == null)
            return null;

        List<Vector2Int> adjacentTiles =
            map.GetAdjacentTileCoords(attacker.currentTileCoord);

        if (!adjacentTiles.Contains(tile))
            return null;

        foreach (PlayerUnit unit in players)
        {
            if (unit == null)
                continue;

            if (unit == attacker)
                continue;

            if (unit.teamID == attacker.teamID)
                continue;

            if (!unit.gameObject.activeSelf)
                continue;

            if (unit.currentTileCoord == tile &&
                unit.currentVertexIndex == vertex)
            {
                return unit;
            }
        }

        return null;
    }

    private void ExecuteSelectedAdjacentAttack()
    {
        if (current == null)
            return;

        if (selectedAttackTarget == null)
        {
            Debug.Log("[ATTACK] K pressed, but no adjacent target is selected.");
            return;
        }

        if (!selectedAttackTarget.gameObject.activeSelf)
        {
            Debug.Log("[ATTACK] Selected target is no longer active.");
            selectedAttackTarget = null;
            return;
        }

        Debug.Log(
            $"[ATTACK] {current.name} attacking selected target " +
            $"{selectedAttackTarget.name}"
        );

        pendingAttackTargets.Clear();
        pendingAttackTargets.Add(selectedAttackTarget);

        isProcessingMove = true;
        isSelectingAdjacentAttack = false;

        current.Attack();
    }

    public bool TryAutomaticSameHexAttack(PlayerUnit attacker)
    {
        if (attacker == null)
            return false;

        List<PlayerUnit> targets =
            GetHostileUnitsOnSameHex(attacker);

        if (targets.Count == 0)
            return false;

        pendingAttackTargets.Clear();
        pendingAttackTargets.AddRange(targets);

        Debug.Log(
            $"[ATTACK SAME HEX] Automatic attack by {attacker.name} " +
            $"against {targets.Count} hostile unit(s)."
        );

        isProcessingMove = true;
        attacker.Attack();

        return true;
    }

    public void HandleAttackFinished()
    {
        if (current == null)
            return;

        Debug.Log(
            $"[ATTACK] {current.name} attack resolved. " +
            $"Targets={pendingAttackTargets.Count}"
        );

        foreach (PlayerUnit target in pendingAttackTargets)
        {
            if (target == null)
                continue;

            if (!target.gameObject.activeSelf)
                continue;

            Debug.Log(
                $"[ATTACK] Damage target: {target.name}"
            );

            target.TakeAttackDamage(attackDamage);
        }

        pendingAttackTargets.Clear();
        selectedAttackTarget = null;
        isSelectingAdjacentAttack = false;

        isProcessingMove = false;

        StartNextTurn();
    }
    public void HandleMoveFinished()
    {
        if (current == null)
            return;

        Debug.Log("[TURN] HandleMoveFinished() called");

        // Check for automatic same-hex attack first.
        if (TryAutomaticSameHexAttack(current))
        {
            return;
        }

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
        {
            if (h == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(h);
            else
                Destroy(h);
#else
        Destroy(h);
#endif
        }

        highlights.Clear();

        foreach (var h in attackHighlights)
        {
            if (h == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(h);
            else
                Destroy(h);
#else
        Destroy(h);
#endif
        }

        attackHighlights.Clear();
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

    [ContextMenu("Cycle Environmental Damage")]
    void CycleEnvironmentalDamage()
    {
        switch (environmentalDamage)
        {
            case 5:
                environmentalDamage = 10;
                break;

            case 10:
                environmentalDamage = 15;
                break;

            case 15:
                environmentalDamage = 20;
                break;

            case 20:
                environmentalDamage = 25;
                break;

            case 25:
                environmentalDamage = 50;
                break;

            default:
                environmentalDamage = 5;
                break;
        }

        Debug.Log($"[ENV] Environmental Damage = {environmentalDamage}");
    }

    void Update()
    {
        if (current == null)
            return;

        if (isProcessingMove)
            return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            ExecuteSelectedAdjacentAttack();
        }
    }
}
