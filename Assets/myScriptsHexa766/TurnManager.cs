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

    [Header("ATTACK MARKER")]
    [SerializeField]
    private GameObject attackMarkerPrefab;

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

    [Header("ATTACK VISUAL EFFECTS")]
    [SerializeField] private ElectricArcEffect electricArcPrefab;
    [Header("DAMAGE FEEDBACK")]
    [SerializeField] private FloatingDamageText damageTextPrefab;

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

        currentIndex = (currentIndex + 1) % players.Count;
        current = players[currentIndex];

        if (current == null)
        {
            Debug.LogError("[TURN] Current player is NULL");
            return;
        }

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

        Debug.Log(
            $"players[0]={players[0].name}, " +
            $"players[1]={players[1].name}, " +
            $"currentIndex={currentIndex}, " +
            $"current={current.name}"
        );

        // -------------------------------------------------
        // CRITICAL FIX:
        // Do NOT create a turn for a unit that is still dead.
        // -------------------------------------------------
        if (current.IsDead)
        {
            Debug.Log(
                $"[TURN WAIT] {current.name} is still dead. " +
                $"Waiting for respawn before starting its turn."
            );

            // Lock interaction while waiting.
            isProcessingMove = true;

            StartCoroutine(
                WaitForRespawnThenBeginTurn(current)
            );

            return;
        }

        BeginCurrentTurn();
    }

    private IEnumerator WaitForRespawnThenBeginTurn(PlayerUnit player)
    {
        if (player == null)
            yield break;

        Debug.Log(
            $"[TURN WAIT] Waiting for {player.name} respawn..."
        );

        while (player != null && player.IsDead)
        {
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning(
                "[TURN WAIT] Player disappeared while waiting for respawn."
            );

            yield break;
        }

        // Safety check:
        // only continue if this is still the player whose turn
        // we were waiting to start.
        if (current != player)
        {
            Debug.LogWarning(
                $"[TURN WAIT] {player.name} respawned, " +
                $"but current player changed to " +
                $"{current?.name}. Turn start cancelled."
            );

            yield break;
        }

        Debug.Log(
            $"[TURN WAIT] {player.name} respawn complete at " +
            $"{player.transform.position}. Starting turn now."
        );

        BeginCurrentTurn();
    }

    private void BeginCurrentTurn()
    {
        if (current == null)
        {
            Debug.LogError(
                "[TURN] BeginCurrentTurn called with current == NULL"
            );

            return;
        }

        // Interaction is now safe again.
        isProcessingMove = false;

        Debug.Log(
            $"[PROCESSING STATE] FALSE — TURN READY | " +
            $"Current={current.name}"
        );

        current.ResetTurn();

        CameraFollow cam = FindObjectOfType<CameraFollow>();

        if (cam != null)
        {
            cam.SetTarget(current.transform);
        }

        Debug.Log(
            $"[TURN] Current player: {current.name} | " +
            $"World={current.transform.position} | " +
            $"IsDead={current.IsDead}"
        );

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

            Debug.Log(
                $"[ATTACK SELECT] {current.name} selected " +
                $"{clickedHostile.name} as adjacent attack target."
            );

            // Adjacent attack now happens immediately.
            ExecuteSelectedAdjacentAttack();

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

        Debug.Log(
            $"[PROCESSING STATE] TRUE — MOVEMENT START | " +
            $"Current={current?.name}"
        );

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

            if (attackMarkerPrefab == null)
            {
                Debug.LogError(
                    "[ATTACK HIGHLIGHT] Attack Marker Prefab is not assigned!"
                );
                return;
            }

            GameObject marker = Instantiate(
                attackMarkerPrefab,
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

        Debug.Log(
            $"[PROCESSING STATE] TRUE — MOVEMENT START | " +
            $"Current={current?.name}" +
            $"Target={selectedAttackTarget?.name}"
        );
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

        Debug.Log(
            $"[PROCESSING STATE] TRUE — MOVEMENT START | " +
            $"Attacker={attacker.name}"
        );
        attacker.Attack();

        return true;
    }

    public void HandleAttackImpact()
    {
        if (current == null)
            return;

        Debug.Log(
            $"[ATTACK IMPACT] Resolving damage for {current.name}. " +
            $"Targets={pendingAttackTargets.Count}"
        );

        foreach (PlayerUnit target in pendingAttackTargets)
        {
            if (target == null)
                continue;

            if (!target.gameObject.activeSelf)
                continue;

            Debug.Log(
                $"[ATTACK IMPACT] Damage target: {target.name}"
            );

            // Spawn the electric arc at the exact impact frame.
            if (electricArcPrefab != null &&
                current.AttackOrigin != null &&
                target.HitPoint != null)
            {

                Debug.Log(
    $"[ARC DEBUG] Attacker={current.name} | " +
    $"Origin={current.AttackOrigin?.name} " +
    $"Pos={(current.AttackOrigin != null ? current.AttackOrigin.position.ToString() : "NULL")} | " +
    $"Target={target.name} | " +
    $"HitPoint={target.HitPoint?.name} " +
    $"Pos={(target.HitPoint != null ? target.HitPoint.position.ToString() : "NULL")}"
);
                ElectricArcEffect arc =
    Instantiate(
        electricArcPrefab,
        Vector3.zero,
        Quaternion.identity
            );

                arc.Initialize(
                    current.AttackOrigin,
                    target.HitPoint
                );
            }
            else
            {
                if (electricArcPrefab == null)
                    Debug.LogWarning("[ELECTRIC ARC] Prefab is not assigned.");

                if (current.AttackOrigin == null)
                    Debug.LogWarning(
                        $"[ELECTRIC ARC] {current.name} has no AttackOrigin."
                    );

                if (target.HitPoint == null)
                    Debug.LogWarning(
                        $"[ELECTRIC ARC] {target.name} has no HitPoint."
                    );
            }

            // Gameplay damage remains exactly where it already worked.
            var damageResult =
                target.TakeAttackDamage(attackDamage);

            ShowDamageText(
                target,
                damageResult.shieldLost,
                damageResult.energyLost
            );
        }
    }
    public void HandleAttackFinished()
    {
        if (current == null)
            return;

        Debug.Log(
            $"[ATTACK] {current.name} attack finished. " +
            $"Targets={pendingAttackTargets.Count}"
        );

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

        DetectWorldBoxAtCurrentPlayer();

        Debug.Log(
            $"[PROCESSING STATE] MOVE FINISHED CALLBACK | " +
            $"Current={current.name} | Processing={isProcessingMove}"
        );

        // Check for automatic same-hex attack first.
        if (TryAutomaticSameHexAttack(current))
        {
            Debug.Log(
                $"[PROCESSING STATE] MOVE FINISHED → SAME HEX ATTACK STARTED | " +
                $"Current={current.name} | Processing={isProcessingMove}"
            );

            return;
        }

        isProcessingMove = false;

        Debug.Log(
            $"[PROCESSING STATE] FALSE — NORMAL MOVE FINISHED | " +
            $"Current={current.name}"
        );

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

            // AIStation center marker must sit above the center platform.
            float markerYOffset =
                (pair.tileName == "AIStation" && pair.vertexIndex == 6)
                ? 0.35f
                : 0.05f;

            GameObject sphere = Instantiate(
                map.vertexMarkerPrefab,
                pos + Vector3.up * markerYOffset,
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

    private void ShowDamageText(
    PlayerUnit target,
    int shieldLost,
    int energyLost)
    {
        if (damageTextPrefab == null)
            return;

        if (target == null || target.HitPoint == null)
            return;

        int totalDamage = shieldLost + energyLost;

        if (totalDamage <= 0)
            return;

        Vector3 basePosition =
            target.HitPoint.position + Vector3.up * 0.15f;

        if (Camera.main != null)
        {
            Vector3 towardCamera =
                (Camera.main.transform.position -
                 target.HitPoint.position).normalized;

            basePosition += towardCamera * 0.35f;
        }

        FloatingDamageText damageText =
            Instantiate(
                damageTextPrefab,
                basePosition,
                Quaternion.identity
            );

        damageText.Show($"-{totalDamage}");
    }

    private void DetectWorldBoxAtCurrentPlayer()
    {
        if (current == null)
        {
            Debug.LogWarning("[BOX CHECK] Current player is NULL");
            return;
        }

        WorldBox[] boxes =
            FindObjectsByType<WorldBox>(FindObjectsSortMode.None);

        Debug.Log(
            $"[BOX CHECK] Player={current.name} | " +
            $"Tile={current.currentTileCoord} | " +
            $"Vertex={current.currentVertexIndex} | " +
            $"WorldBoxes found={boxes.Length}"
        );

        foreach (WorldBox box in boxes)
        {
            if (box == null)
                continue;

            Debug.Log(
                $"[BOX CHECK] Box={box.name} | " +
                $"Tile=({box.tileX}, {box.tileY}) | " +
                $"Vertex={box.vertexIndex} | " +
                $"World={box.transform.position}"
            );

            bool sameTile =
                box.tileX == current.currentTileCoord.x &&
                box.tileY == current.currentTileCoord.y;

            bool sameVertex =
                box.vertexIndex == current.currentVertexIndex;

            Debug.Log(
                $"[BOX CHECK] {box.name} | " +
                $"SameTile={sameTile} | SameVertex={sameVertex}"
            );

            if (sameTile && sameVertex)
            {
                Debug.Log(
                    $"<color=lime><b>[BOX DETECTED]</b></color> " +
                    $"{current.name} found {box.name} at " +
                    $"tile ({box.tileX}, {box.tileY}) " +
                    $"vertex {box.vertexIndex}"
                );
            }
        }
    }
    private void Update()
    {
    }
}
