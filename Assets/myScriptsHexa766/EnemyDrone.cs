using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class EnemyDrone : MonoBehaviour
{
    [Header("ENEMY SPAWN")]
    [SerializeField]
    private string spawnTileName = "PyramidCenter";

    [Header("ENEMY COMBAT")]
    [SerializeField]
    private int droneAttackDamage = 120;

    [SerializeField]
    private float hoverHeight = 0.65f;

    [SerializeField]
    private float moveDuration = 1.0f;

    [SerializeField]
    private float carrierThreatMultiplier = 2f;

    [SerializeField]
    private float inactiveTextSize = 0.09f;

    [SerializeField]
    private float activeTextSize = 0.14f;

    [SerializeField]
    private float inactiveOpacity = 0.55f;

    [SerializeField]
    private float activeOpacity = 1.0f;

    private PlayerUnit unit;
    private TurnManager turnManager;

    private string previousTileName = null;

    public bool IsBusy { get; private set; }

    public string currentTileName { get; private set; }
    public Vector2Int currentTileCoord { get; private set; }

    private string reservedTileName = null;

    [Header("ATTACK TELEGRAPH")]
    [SerializeField]
    private float telegraphDuration = 0.75f;

    [SerializeField]
    private float telegraphMinScale = 0.12f;

    [SerializeField]
    private float telegraphMaxScale = 0.32f;

    [SerializeField]
    private float telegraphHeight = 0.8f;

    private GameObject attackWarningOrb;

    [Header("ATTACK BEAM")]
    [SerializeField]
    private float attackBeamDuration = 0.12f;

    [SerializeField]
    private float attackBeamWidth = 0.06f;

    [SerializeField]
    private float attackBeamOriginHeight = 0.2f;

    private LineRenderer attackBeamLine;

    [Header("ATTACK IMPACT")]
    [SerializeField]
    private float impactFlashDuration = 0.12f;

    [SerializeField]
    private float impactFlashSize = 0.18f;

    [Header("DRONE AUDIO")]
    [SerializeField]
    private AudioSource droneAudioSource;

    [SerializeField]
    private AudioClip spawnClip;

    [SerializeField]
    private AudioClip movementClip;

    [SerializeField]
    private AudioClip attackClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float spawnVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float movementVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField]
    private float attackVolume = 1f;
    private Map1HexGrid map;

    private IEnumerator Start()
    {
        map = FindFirstObjectByType<Map1HexGrid>();

        if (droneAudioSource == null)
        {
            droneAudioSource =
                GetComponent<AudioSource>();
        }

        if (map == null)
        {
            Debug.LogError(
                "[ENEMY DRONE] Map1HexGrid not found."
            );

            yield break;
        }

        Debug.Log(
            $"[ENEMY DRONE CREATED] " +
            $"{name} | ID={GetInstanceID()}"
        );

        Debug.Log(
            $"[ENEMY DRONE] Using Map1HexGrid instance: " +
            $"{map.name} | ID={map.GetInstanceID()}"
        );

        Debug.Log(
            "[ENEMY DRONE] Waiting for map generation..."
        );

        while (
            !map.hasGenerated ||
            map.tileGameObjectMap == null ||
            !map.tileGameObjectMap.ContainsKey("PyramidCenter")
        )
        {
            yield return null;
        }

        Debug.Log(
            "[ENEMY DRONE] Map ready."
        );

        PlaceAtSpawnCenter();

        CreateAttackWarningOrb();

        Debug.Log(
            $"[ENEMY DRONE READY] " +
            $"{name} | ID={GetInstanceID()} | " +
            $"Tile={currentTileName} | " +
            $"Grid={currentTileCoord}"
        );
    }

    private void CreateAttackWarningOrb()
    {
        if (attackWarningOrb != null)
            return;

        attackWarningOrb =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        attackWarningOrb.name =
            "AttackWarningOrb";

        attackWarningOrb.transform.SetParent(
            transform,
            false
        );


        attackWarningOrb.transform.localScale =
            Vector3.one * telegraphMinScale;

        Collider warningCollider =
            attackWarningOrb.GetComponent<Collider>();

        if (warningCollider != null)
            Destroy(warningCollider);

        Renderer warningRenderer =
            attackWarningOrb.GetComponent<Renderer>();

        if (warningRenderer != null)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                Material warningMaterial =
                    new Material(shader);

                Color warningColor =
                    new Color(
                        1f,
                        0.05f,
                        0.05f,
                        1f
                    );

                if (warningMaterial.HasProperty(
                        "_BaseColor"))
                {
                    warningMaterial.SetColor(
                        "_BaseColor",
                        warningColor
                    );
                }
                else
                {
                    warningMaterial.color =
                        warningColor;
                }

                warningRenderer.material =
                    warningMaterial;
            }
        }

        attackWarningOrb.SetActive(false);
    }

    private void PlayDroneSound(
    AudioClip clip,
    float volume)
    {
        if (droneAudioSource == null ||
            clip == null)
        {
            return;
        }

        droneAudioSource.PlayOneShot(
            clip,
            volume
        );
    }

    private void PlaceAtSpawnCenter()
    {
        if (!map.TryGetTileCenterPosition(
                spawnTileName,
                out Vector3 centerPosition))
        {
            Debug.LogError(
                $"[ENEMY DRONE] Could not find center of {spawnTileName}."
            );

            return;
        }

        currentTileName = spawnTileName;
        currentTileCoord =
            map.GetTileGridCoord(spawnTileName);

        centerPosition.y += hoverHeight;

        transform.position = centerPosition;

        PlayDroneSound(
            spawnClip,
            spawnVolume
        );

        Debug.Log(
            $"[ENEMY DRONE] {name} spawned at " +
            $"{currentTileName} center | " +
            $"Grid={currentTileCoord}"
        );
    }

    [ContextMenu("TEST - Move To Tile00")]
    private void TestMoveToTile00()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ENEMY DRONE] Test movement only works in Play Mode."
            );
            return;
        }

        StartCoroutine(
            MoveToTileCenter("Tile00")
        );
    }

    private IEnumerator MoveToTileCenter(
    string destinationTileName)
    {
        IsBusy = true;

        // Reserve destination for the entire movement.
        reservedTileName = destinationTileName;

        if (!map.TryGetTileCenterPosition(
                destinationTileName,
                out Vector3 destination))
        {
            Debug.LogError(
                $"[ENEMY DRONE] Cannot move to {destinationTileName}."
            );

            reservedTileName = null;
            IsBusy = false;

            yield break;
        }

        destination.y += hoverHeight;

        Vector3 start = transform.position;
        float elapsed = 0f;

        Debug.Log(
            $"[ENEMY DRONE] Moving from " +
            $"{currentTileName} to {destinationTileName}."
        );

        PlayDroneSound(
            movementClip,
            movementVolume
        );

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / moveDuration
            );

            transform.position =
                Vector3.Lerp(
                    start,
                    destination,
                    t
                );

            yield return null;
        }

        transform.position = destination;

        previousTileName = currentTileName;

        currentTileName = destinationTileName;

        currentTileCoord =
            map.GetTileGridCoord(
                destinationTileName
            );

        Debug.Log(
            $"[ENEMY DRONE] Arrived at " +
            $"{currentTileName} center | " +
            $"Grid={currentTileCoord}"
        );

        // Movement is truly complete now.
        reservedTileName = null;
        IsBusy = false;
    }



    [ContextMenu("TEST - Move To Adjacent Tile")]
    private void TestMoveToAdjacentTile()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ENEMY DRONE] Test movement only works in Play Mode."
            );
            return;
        }

        var adjacentCoords =
            map.GetAdjacentTileCoords(currentTileCoord);

        string fallbackTile = null;

        foreach (Vector2Int coord in adjacentCoords)
        {
            string tileName = null;

            foreach (var kvp in map.tileGameObjectMap)
            {
                if (map.GetTileGridCoord(kvp.Key) == coord)
                {
                    tileName = kvp.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(tileName))
                continue;

            // Enemy can never occupy AI Station center.
            if (tileName == "AIStation")
                continue;

            // Remember the previous tile as an emergency fallback.
            if (tileName == previousTileName)
            {
                fallbackTile = tileName;
                continue;
            }

            Debug.Log(
                $"[ENEMY DRONE] Moving to new adjacent tile: {tileName}"
            );

            StartCoroutine(
                MoveToTileCenter(tileName)
            );

            return;
        }

        // Dead end: going backward is allowed.
        if (!string.IsNullOrEmpty(fallbackTile))
        {
            Debug.Log(
                $"[ENEMY DRONE] Dead end. Backtracking to {fallbackTile}."
            );

            StartCoroutine(
                MoveToTileCenter(fallbackTile)
            );

            return;
        }

        Debug.LogWarning(
            "[ENEMY DRONE] No legal adjacent destination found."
        );
    }

    [ContextMenu("TEST - Move One Step Toward Player1")]
    private void TestMoveOneStepTowardPlayer1()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ENEMY DRONE] Test only works in Play Mode."
            );
            return;
        }

        if (map == null || map.Player1 == null)
        {
            Debug.LogError(
                "[ENEMY DRONE] Player1 or map is missing."
            );
            return;
        }

        PlayerUnit player1 =
            map.Player1.GetComponent<PlayerUnit>();

        if (player1 == null)
        {
            Debug.LogError(
                "[ENEMY DRONE] Player1 has no PlayerUnit."
            );
            return;
        }

        Vector2Int targetTile =
            player1.currentTileCoord;

        List<Vector2Int> path =
            FindEnemyPath(
                currentTileCoord,
                targetTile
            );

        if (path == null || path.Count < 2)
        {
            Debug.Log(
                $"[ENEMY DRONE] No movement needed or " +
                $"no legal route toward Player1 at {targetTile}."
            );
            return;
        }

        Vector2Int nextCoord = path[1];

        string nextTileName =
            GetEnemyTileName(nextCoord);

        if (string.IsNullOrEmpty(nextTileName))
        {
            Debug.LogError(
                $"[ENEMY DRONE] Could not resolve tile {nextCoord}."
            );
            return;
        }

        Debug.Log(
            $"[ENEMY DRONE] Player1 target={targetTile} | " +
            $"Next step={nextTileName} {nextCoord}"
        );

        StartCoroutine(
            MoveToTileCenter(nextTileName)
        );
    }


    private List<Vector2Int> FindEnemyPath(
    Vector2Int start,
    Vector2Int target)
    {
        Queue<Vector2Int> frontier =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom =
            new Dictionary<Vector2Int, Vector2Int>();

        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            Vector2Int current =
                frontier.Dequeue();

            if (current == target)
                break;

            List<Vector2Int> neighbors =
                map.GetAdjacentTileCoords(current);

            foreach (Vector2Int next in neighbors)
            {
                if (visited.Contains(next))
                    continue;

                string tileName =
                    GetEnemyTileName(next);

                if (string.IsNullOrEmpty(tileName))
                    continue;

                // -----------------------------------------
                // PERMANENT ENEMY RULE:
                // AI STATION CENTER IS FORBIDDEN
                // -----------------------------------------
                if (tileName == "AIStation")
                    continue;

                // -----------------------------------------
                // COLLAPSED HEX = REMOVED FROM ENEMY GRAPH
                // -----------------------------------------
                if (map.tileGameObjectMap.TryGetValue(
                        tileName,
                        out GameObject tileObject))
                {
                    HexCapacity capacity =
                        tileObject.GetComponent<HexCapacity>();

                    if (capacity != null &&
                        capacity.IsCollapsed)
                    {
                        continue;
                    }
                }

                visited.Add(next);
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!visited.Contains(target))
            return null;

        List<Vector2Int> path =
            new List<Vector2Int>();

        Vector2Int step = target;

        path.Add(step);

        while (step != start)
        {
            step = cameFrom[step];
            path.Add(step);
        }

        path.Reverse();

        return path;
    }


    private string GetEnemyTileName(
        Vector2Int coord)
    {
        foreach (var kvp in map.tileGameObjectMap)
        {
            if (map.GetTileGridCoord(kvp.Key) == coord)
                return kvp.Key;
        }

        return null;
    }

    public void ExecuteAction()
    {
        if (IsBusy)
        {
            Debug.Log(
                $"[ENEMY DRONE] {name} is busy. Action ignored."
            );

            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ENEMY DRONE] Test only works in Play Mode."
            );

            return;
        }

        if (map == null)
        {
            Debug.LogError(
                "[ENEMY DRONE] Map reference is missing."
            );

            return;
        }

        // -----------------------------------------
        // FIND ALL LIVING PLAYER UNITS
        // -----------------------------------------
        PlayerUnit[] allPlayers =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        List<PlayerUnit> livingPlayers =
            new List<PlayerUnit>();

        foreach (PlayerUnit player in allPlayers)
        {
            if (player == null)
                continue;

            if (!player.isActiveAndEnabled)
                continue;

            if (player.IsDead)
                continue;

            livingPlayers.Add(player);
        }

        if (livingPlayers.Count == 0)
        {
            Debug.LogWarning(
                "[ENEMY DRONE] No living player units found."
            );

            return;
        }

        // -----------------------------------------
        // IMMEDIATE SAME-HEX ATTACK PRIORITY
        // -----------------------------------------
        List<PlayerUnit> sameHexPlayers =
            new List<PlayerUnit>();

        foreach (PlayerUnit player in livingPlayers)
        {
            if (player.currentTileCoord == currentTileCoord)
            {
                sameHexPlayers.Add(player);
            }
        }

        if (sameHexPlayers.Count > 0)
        {
            // Preserve the existing carrier hard-priority rule:
            // if one or more carriers share the drone's hex,
            // attack a carrier before any non-carrier.
            List<PlayerUnit> sameHexCarriers =
                new List<PlayerUnit>();

            foreach (PlayerUnit player in sameHexPlayers)
            {
                if (player.IsCarryingBox)
                {
                    sameHexCarriers.Add(player);
                }
            }

            List<PlayerUnit> immediatePool =
                sameHexCarriers.Count > 0
                ? sameHexCarriers
                : sameHexPlayers;

            PlayerUnit immediateTarget =
                immediatePool[
                    Random.Range(
                        0,
                        immediatePool.Count
                    )
                ];

            Debug.Log(
                $"[ENEMY TARGET] SAME HEX priority → " +
                $"{immediateTarget.name} | " +
                $"Team={immediateTarget.teamID} " +
                $"Unit={immediateTarget.unitID} | " +
                $"Carrier={immediateTarget.IsCarryingBox} | " +
                $"Tile={currentTileCoord}"
            );

            StartCoroutine(
                AttackSequence(immediateTarget)
            );

            return;
        }

        // -----------------------------------------
        // CALCULATE LEGAL PATH TO EVERY LIVING UNIT
        // -----------------------------------------
        Dictionary<PlayerUnit, List<Vector2Int>>
            reachablePaths =
                new Dictionary<PlayerUnit, List<Vector2Int>>();

        foreach (PlayerUnit player in livingPlayers)
        {
            List<Vector2Int> path =
                FindEnemyPath(
                    currentTileCoord,
                    player.currentTileCoord
                );

            if (path == null ||
                path.Count == 0)
            {
                continue;
            }

            reachablePaths[player] = path;

            Debug.Log(
                $"[ENEMY THREAT] {player.name} | " +
                $"Team={player.teamID} " +
                $"Unit={player.unitID} | " +
                $"Distance={path.Count - 1} | " +
                $"Carrier={player.IsCarryingBox}"
            );
        }

        if (reachablePaths.Count == 0)
        {
            Debug.LogWarning(
                "[ENEMY DRONE] No player unit has a legal enemy route."
            );

            return;
        }

        // -----------------------------------------
        // OBJECTIVE-CARRIER HARD PRIORITY
        // -----------------------------------------
        List<PlayerUnit> candidatePlayers =
            new List<PlayerUnit>();

        foreach (var entry in reachablePaths)
        {
            if (entry.Key.IsCarryingBox)
            {
                candidatePlayers.Add(entry.Key);
            }
        }

        bool usingCarrierPriority =
            candidatePlayers.Count > 0;

        // If no reachable carrier exists, all reachable
        // living units become valid candidates.
        if (!usingCarrierPriority)
        {
            foreach (var entry in reachablePaths)
            {
                candidatePlayers.Add(entry.Key);
            }
        }

        if (usingCarrierPriority)
        {
            Debug.Log(
                $"[ENEMY TARGET] BOX CARRIER PRIORITY | " +
                $"Candidates={candidatePlayers.Count}"
            );
        }

        // -----------------------------------------
        // CHOOSE NEAREST CANDIDATE
        // Random tie-break preserves previous behavior.
        // -----------------------------------------
        int bestDistance = int.MaxValue;

        List<PlayerUnit> bestPlayers =
            new List<PlayerUnit>();

        foreach (PlayerUnit player in candidatePlayers)
        {
            List<Vector2Int> path =
                reachablePaths[player];

            int distance =
                path.Count - 1;

            if (distance < bestDistance)
            {
                bestDistance = distance;

                bestPlayers.Clear();
                bestPlayers.Add(player);
            }
            else if (distance == bestDistance)
            {
                bestPlayers.Add(player);
            }
        }

        PlayerUnit targetPlayer =
            bestPlayers[
                Random.Range(
                    0,
                    bestPlayers.Count
                )
            ];

        List<Vector2Int> chosenPath =
            reachablePaths[targetPlayer];

        int chosenDistance =
            chosenPath.Count - 1;

        Debug.Log(
            $"[ENEMY TARGET] Selected {targetPlayer.name} | " +
            $"Team={targetPlayer.teamID} " +
            $"Unit={targetPlayer.unitID} | " +
            $"Tile={targetPlayer.currentTileCoord} | " +
            $"Distance={chosenDistance} | " +
            $"Carrier={targetPlayer.IsCarryingBox}"
        );

        // -----------------------------------------
        // DRONE ATTACK RULE
        // Attack ONLY if drone and player
        // occupy the same hex.
        // -----------------------------------------
        if (chosenDistance == 0)
        {
            Debug.Log(
                $"[ENEMY DRONE] {name} preparing attack " +
                $"against {targetPlayer.name}."
            );

            StartCoroutine(
                AttackSequence(targetPlayer)
            );

            return;
        }

        // -----------------------------------------
        // MOVEMENT
        // Take ONLY one step toward target.
        // -----------------------------------------
        Vector2Int nextCoord =
            chosenPath[1];

        string nextTileName =
            GetEnemyTileName(nextCoord);

        if (string.IsNullOrEmpty(nextTileName))
        {
            Debug.LogError(
                $"[ENEMY DRONE] Could not resolve next tile {nextCoord}."
            );

            return;
        }

        // -----------------------------------------
        // DRONE OCCUPANCY RULE
        // Only ONE drone may occupy a hex center.
        // -----------------------------------------
        if (IsDroneTileOccupied(nextTileName))
        {
            Debug.Log(
                $"[ENEMY DRONE] {name} BLOCKED | " +
                $"{nextTileName} is already occupied " +
                $"by another drone."
            );

            return;
        }

        Debug.Log(
            $"[ENEMY DRONE] Pursuing {targetPlayer.name} | " +
            $"Next={nextTileName} {nextCoord}"
        );

        StartCoroutine(
            MoveToTileCenter(nextTileName)
        );
    }


    private bool IsDroneTileOccupied(
    string tileName)
    {
        EnemyDrone[] drones =
            FindObjectsByType<EnemyDrone>(
                FindObjectsSortMode.None
            );

        foreach (EnemyDrone drone in drones)
        {
            if (drone == null)
                continue;

            if (drone == this)
                continue;

            if (!drone.isActiveAndEnabled)
                continue;

            // Already standing on this hex.
            if (drone.currentTileName == tileName)
            {
                Debug.Log(
                    $"[ENEMY OCCUPANCY] {tileName} occupied by " +
                    $"{drone.name}"
                );

                return true;
            }

            // Another drone is already moving toward this hex.
            if (drone.reservedTileName == tileName)
            {
                Debug.Log(
                    $"[ENEMY OCCUPANCY] {tileName} reserved by " +
                    $"{drone.name}"
                );

                return true;
            }
        }

        return false;
    }

    private IEnumerator PlayAttackTelegraph()
    {
        if (attackWarningOrb == null)
        {
            CreateAttackWarningOrb();
        }

        if (attackWarningOrb == null)
            yield break;

        attackWarningOrb.SetActive(true);

        float elapsed = 0f;

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;

            // Keep the orb at the desired WORLD height
            // above the drone for the entire telegraph.
            attackWarningOrb.transform.position =
                transform.position +
                Vector3.up * telegraphHeight;

            float pulse =
                Mathf.PingPong(
                    elapsed * 6f,
                    1f
                );

            float scale =
                Mathf.Lerp(
                    telegraphMinScale,
                    telegraphMaxScale,
                    pulse
                );

            attackWarningOrb.transform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        attackWarningOrb.SetActive(false);

        attackWarningOrb.transform.localScale =
            Vector3.one * telegraphMinScale;
    }
    private void CreateAttackBeam()
    {
        if (attackBeamLine != null)
            return;

        GameObject beamObject =
            new GameObject("DroneAttackBeam");

        beamObject.transform.SetParent(
            transform,
            false
        );

        attackBeamLine =
            beamObject.AddComponent<LineRenderer>();

        attackBeamLine.positionCount = 2;
        attackBeamLine.useWorldSpace = true;

        attackBeamLine.startWidth =
            attackBeamWidth;

        attackBeamLine.endWidth =
            attackBeamWidth;

        attackBeamLine.numCapVertices = 4;

        attackBeamLine.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        attackBeamLine.receiveShadows = false;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            Material beamMaterial =
                new Material(shader);

            Color beamColor =
                new Color(
                    1f,
                    0.08f,
                    0.02f,
                    1f
                );

            if (beamMaterial.HasProperty("_BaseColor"))
            {
                beamMaterial.SetColor(
                    "_BaseColor",
                    beamColor
                );
            }
            else
            {
                beamMaterial.color =
                    beamColor;
            }

            attackBeamLine.material =
                beamMaterial;
        }

        attackBeamLine.startColor =
            Color.white;

        attackBeamLine.endColor =
            Color.red;

        attackBeamLine.enabled = false;
    }


    private IEnumerator PlayAttackBeam(
        PlayerUnit targetPlayer)
    {
        if (attackBeamLine == null)
            CreateAttackBeam();

        if (attackBeamLine == null ||
            targetPlayer == null)
        {
            yield break;
        }

        attackBeamLine.enabled = true;

        float elapsed = 0f;

        while (elapsed < attackBeamDuration)
        {
            elapsed += Time.deltaTime;

            Vector3 startPosition =
                transform.position +
                Vector3.up * attackBeamOriginHeight;

            Vector3 targetPosition =
                targetPlayer.HitPoint != null
                ? targetPlayer.HitPoint.position
                : targetPlayer.transform.position +
                  Vector3.up * 0.5f;

            attackBeamLine.SetPosition(
                0,
                startPosition
            );

            attackBeamLine.SetPosition(
                1,
                targetPosition
            );

            // Beam quickly burns out.
            float progress =
                Mathf.Clamp01(
                    elapsed / attackBeamDuration
                );

            float width =
                Mathf.Lerp(
                    attackBeamWidth,
                    0.01f,
                    progress
                );

            attackBeamLine.startWidth = width;
            attackBeamLine.endWidth = width;

            yield return null;
        }

        attackBeamLine.enabled = false;
    }

    private IEnumerator PlayImpactFlash(
    PlayerUnit targetPlayer)
    {
        if (targetPlayer == null)
            yield break;

        Vector3 impactPosition =
            targetPlayer.HitPoint != null
            ? targetPlayer.HitPoint.position
            : targetPlayer.transform.position +
              Vector3.up * 0.5f;

        // Pull the flash slightly toward the camera
        // so it is not buried inside the robot model.
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            Vector3 towardCamera =
                (
                    mainCamera.transform.position -
                    impactPosition
                ).normalized;

            impactPosition +=
                towardCamera * 0.15f;
        }

        GameObject flash =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        flash.name = "DroneImpactFlash";

        flash.transform.position =
            impactPosition;

        flash.transform.localScale =
            Vector3.one * 0.03f;

        Collider flashCollider =
            flash.GetComponent<Collider>();

        if (flashCollider != null)
            Destroy(flashCollider);

        Renderer flashRenderer =
            flash.GetComponent<Renderer>();

        if (flashRenderer != null)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                Material flashMaterial =
                    new Material(shader);

                Color flashColor =
                    new Color(
                        1f,
                        0.85f,
                        0.25f,
                        1f
                    );

                if (flashMaterial.HasProperty(
                        "_BaseColor"))
                {
                    flashMaterial.SetColor(
                        "_BaseColor",
                        flashColor
                    );
                }
                else
                {
                    flashMaterial.color =
                        flashColor;
                }

                flashRenderer.material =
                    flashMaterial;
            }
        }

        float elapsed = 0f;

        while (elapsed < impactFlashDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / impactFlashDuration
                );

            // Grow quickly, then shrink.
            float pulse =
                Mathf.Sin(
                    progress * Mathf.PI
                );

            float scale =
                Mathf.Lerp(
                    0.03f,
                    impactFlashSize,
                    pulse
                );

            flash.transform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        Destroy(flash);
    }
    private IEnumerator AttackSequence(
    PlayerUnit targetPlayer)
    {
        IsBusy = true;

        Debug.Log(
            $"[ENEMY TELEGRAPH] {name} targeting " +
            $"{targetPlayer.name} on {currentTileName}."
        );

        // Visible warning / charge-up before damage.
        yield return StartCoroutine(
            PlayAttackTelegraph()
        );

        if (targetPlayer == null ||
            targetPlayer.IsDead)
        {
            Debug.Log(
                "[ENEMY ATTACK] Target no longer valid."
            );

            IsBusy = false;
            yield break;
        }

        // Safety check:
        // target must STILL be in the drone's hex.
        if (targetPlayer.currentTileCoord != currentTileCoord)
        {
            Debug.Log(
                $"[ENEMY ATTACK] {targetPlayer.name} is no longer " +
                $"on drone hex {currentTileName}. Attack cancelled."
            );

            IsBusy = false;
            yield break;
        }

        // Fire the visible attack beam.
        PlayDroneSound(
            attackClip,
            attackVolume
        );

        StartCoroutine(
            PlayAttackBeam(targetPlayer)
        );

        StartCoroutine(
            PlayImpactFlash(targetPlayer)
        );

        var damageResult =
            targetPlayer.TakeAttackDamage(
                droneAttackDamage
            );

        // Keep the drone busy until the
        // visible shot has finished.
        yield return new WaitForSeconds(
            attackBeamDuration
        );

        Debug.Log(
            $"[ENEMY ATTACK] {name} attacked " +
            $"{targetPlayer.name} on {currentTileName} | " +
            $"Damage={droneAttackDamage} | " +
            $"ShieldLost={damageResult.shieldLost} | " +
            $"EnergyLost={damageResult.energyLost}"
        );

        IsBusy = false;
    }


    public IEnumerator ExecuteActionAndWait()
    {
        ExecuteAction();

        while (IsBusy)
        {
            yield return null;
        }
    }
    [ContextMenu("TEST - Execute Drone Action")]
    private void TestExecuteDroneAction()
    {
        ExecuteAction();
    }

    private void OnDisable()
    {
        Debug.LogError(
            $"[ENEMY DRONE DISABLED] " +
            $"{name} | ID={GetInstanceID()} | " +
            $"Tile={currentTileName} | " +
            $"Grid={currentTileCoord}"
        );
    }

    private void OnDestroy()
    {
        Debug.LogError(
            $"[ENEMY DRONE DESTROYED] " +
            $"{name} | ID={GetInstanceID()} | " +
            $"Tile={currentTileName} | " +
            $"Grid={currentTileCoord}"
        );
    }
}