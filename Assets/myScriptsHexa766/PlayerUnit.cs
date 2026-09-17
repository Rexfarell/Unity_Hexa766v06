using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;

public class PlayerUnit : MonoBehaviour
{
    [Header("UNIT IDENTITY")]
    public int teamID;   // 1 = Blue, 2 = Red

    public int unitID = 1;   // 1-4 within the team

    public TeamController Team { get; private set; }

    public int energy = 100;
    public int shield = 100;

    public bool IsDead => isDead;
    public Vector2Int currentTileCoord;
    public int currentVertexIndex = 0;
    public bool IsCarryingBox => isCarryingBox;


    public System.Action OnMoveFinished;
    public System.Action onActionResolved;
    private bool isResolvingAction = false;
    private TurnManager turnManager;
    private Map1HexGrid map;
    private bool isMoving = false;
    private Vector3 shieldBarStartPos;
    private Vector3 energyBarStartPos;
    private float shieldBarFullWidth;
    private float energyBarFullWidth;

    private string spawnTileName;
    private int spawnVertexIndex;
    private bool isDead = false;


    public float respawnDelay = 3f;

    private bool turnAlreadyFinished = false;

    [SerializeField] private Animator animator;
    [SerializeField] private GameObject carriedBox; // robot internal box
    [SerializeField] private float moveSpeed = 3f;
    private bool isCarryingBox = false;
    private WorldBox carriedWorldBox;
    [Header("Health Bars")]
    [SerializeField] private Transform shieldBar;
    [SerializeField] private Transform energyBar;
    [Header("ATTACK AUDIO")]
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioClip attackImpactClip;

    [Header("PICKUP AUDIO")]
    [SerializeField]
    private AudioSource pickupAudioSource;

    [SerializeField]
    private AudioClip pickupClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float pickupVolume = 1f;

    [Header("ATTACK EFFECT POINTS")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Transform hitPoint;

    [Header("AI STATION VISUAL")]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private float normalVisualLocalY = -0.5f;

    [Header("DELIVERY CAMERA")]
    [SerializeField]
    private CameraFollow cameraFollow;

    [SerializeField]
    private float deliveryCameraLeadIn = 0.35f;

    [SerializeField]
    private float deliveryAnimationDuration = 0.7f;

    [SerializeField]
    private float aiStationVisualLift = 0.12f;

    private bool isDeliveringBox = false;

    private Vector3 normalVisualLocalPosition;

    public Transform AttackOrigin => attackOrigin;
    public Transform HitPoint => hitPoint;

    [Header("AI STATION FEEDBACK")]
    [SerializeField]
    private AIStationFeedback aiStationFeedback;

    [Header("DEATH AUDIO")]
    [SerializeField]
    private AudioSource deathAudioSource;

    [SerializeField]
    private AudioClip deathClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float deathVolume = 1f;

    public void AssignTeam(TeamController team)
    {
        if (team == null)
            return;

        if (team.TeamID != teamID)
        {
            Debug.LogWarning(
                $"[TEAM] Cannot assign {name} to {team.TeamName}. " +
                $"Unit TeamID={teamID}, " +
                $"Controller TeamID={team.TeamID}"
            );

            return;
        }

        Team = team;

        Debug.Log(
            $"[TEAM] {name} linked to {team.TeamName} | " +
            $"Team={teamID} Unit={unitID}"
        );
    }


    void Start()
    {
        map = FindObjectOfType<Map1HexGrid>();
        turnManager = FindObjectOfType<TurnManager>();

        if (GetComponent<UnitIdentityLabel>() == null)
        {
            gameObject.AddComponent<UnitIdentityLabel>();
        }

        if (!map)
        {
            Debug.LogError($"[{name}] Map1HexGrid missing!");
            return;
        }

        if (aiStationFeedback == null)
        {
            aiStationFeedback =
                FindFirstObjectByType<AIStationFeedback>();
        }

        // REGISTER THIS UNIT WITH THE TURN MANAGER
        // Sort by team first, then by unit number
        if (turnManager != null && !turnManager.players.Contains(this))
        {
            turnManager.players.Add(this);

            turnManager.players.Sort(
                (a, b) =>
                {
                    int teamCompare =
                        a.teamID.CompareTo(b.teamID);

                    if (teamCompare != 0)
                        return teamCompare;

                    return a.unitID.CompareTo(b.unitID);
                }
            );

            Debug.Log(
                $"[TURN] Registered player unit: {name}"
            );
        }
        else if (turnManager == null)
        {
            Debug.LogError($"[{name}] TurnManager missing!");
        }

        currentTileCoord = map.WorldToGridCoord(transform.position);
        currentVertexIndex = 0;

        Debug.Log($"[{name}] start @ {currentTileCoord} v{currentVertexIndex}");

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
            Debug.Log($"[{name}] Animator bound to {animator?.gameObject.name}", this);
        }

        if (shieldBar != null)
        {
            shieldBarStartPos = shieldBar.localPosition;
            shieldBarFullWidth = shieldBar.localScale.x;
        }

        if (energyBar != null)
        {
            energyBarStartPos = energyBar.localPosition;
            energyBarFullWidth = energyBar.localScale.x;
        }

        if (carriedBox != null)
        {
            Debug.Log(
                $"[CARRIED BOX TEST] {name} reference = {carriedBox.name}",
                carriedBox
            );

            carriedBox.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                $"[CARRIED BOX TEST] {name} has NO carriedBox assigned."
            );
        }

        UpdateHealthBars();

        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();

            if (cameraFollow != null)
            {
                Debug.Log(
                    $"[CAMERA SETUP] {name} found CameraFollow on " +
                    $"{cameraFollow.gameObject.name}"
                );
            }
            else
            {
                Debug.LogError(
                    $"[CAMERA SETUP] {name} could not find CameraFollow."
                );
            }
        }
    }


    public bool IsValidMove(Vector2Int tile, int v)
    {
        if (currentTileCoord == tile && currentVertexIndex == v) return false;

        int dq = tile.x - currentTileCoord.x;
        int dr = tile.y - currentTileCoord.y;
        if (Mathf.Abs(dq) > 1 || Mathf.Abs(dr) > 1 || Mathf.Abs(dq + dr) > 1)
            return false;

        return !isMoving;
    }

    public void ResetTurn()
    {
        // Intentionally empty.
        // A player gets exactly one move per turn.
        // The movement range comes from map.defaultMovePoints,
        // not from a per-player movement counter.
    }

    public bool MoveToTile(Vector2Int tileCoord, int vertexIdx)
    {
        if (isMoving)
            return false;

        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null && tm.IsVertexOccupied(tileCoord, vertexIdx, this))
        {
            Debug.Log("[MOVE BLOCKED] Vertex already occupied");
            return false;
        }

        Debug.Log($"[MOVE] MoveToTile — {currentTileCoord} v{currentVertexIndex} → {tileCoord} v{vertexIdx}");

        var path = map.GetShortestPath(
            gameObject,
            tileCoord,
            vertexIdx);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("[MOVE] No path found.");
            return false;
        }

        StartCoroutine(SmoothMove(path));
        return true;
    }



    private IEnumerator SmoothMove(List<(string tileName, int vertexIndex)> path)
    {
        isMoving = true;
        turnAlreadyFinished = false;

        // Always return to normal visual height before movement begins.
        if (visualRoot != null)
        {
            normalVisualLocalPosition =
                visualRoot.localPosition;

            normalVisualLocalPosition.y =
                normalVisualLocalY;

            visualRoot.localPosition =
                normalVisualLocalPosition;
        }

        // Start walking animation
        if (animator != null)
            animator.SetBool("Walking", true);

        foreach (var step in path)
        {
            Vector3 targetPos = map.GetVertexPosition(step.tileName, step.vertexIndex);

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPos;

            // Update logical position as each vertex is reached.
            currentTileCoord = map.GetTileGridCoord(step.tileName);
            currentVertexIndex = step.vertexIndex;
        }

        Debug.Log(
            $"[MOVE OCCUPANCY FINAL] {name} FINAL = " +
            $"{currentTileCoord} v{currentVertexIndex}"
        );

        isMoving = false;

        // Stop walking animation
        if (animator != null)
            animator.SetBool("Walking", false);

        if (visualRoot != null)
        {
            // Always restore the robot's normal visual position first.
            visualRoot.localPosition = normalVisualLocalPosition;

            // AIStation v6 = raised center platform.
            // Apply lift in WORLD space so parent scale does not shrink it.
            if (currentVertexIndex == 6)
            {
                visualRoot.position +=
                    Vector3.up * aiStationVisualLift;
            }
        }
        // If the unit died during the move, do not attempt pickup.
        // The death routine has already started the respawn countdown.
        if (isDead)
        {
            Debug.Log(
                $"[{name}] SmoothMove finished, but unit is dead. " +
                $"DieRoutine will handle the turn."
            );

            yield break;
        }

        // -------------------------------------------------
        // AI STATION DELIVERY
        // AIStation vertex 6 = center delivery platform
        // -------------------------------------------------
        // -------------------------------------------------
        // AI STATION DELIVERY
        // Resolve where the player ACTUALLY ended up.
        // -------------------------------------------------
        var resolvedLocation = map.GetPlayerTileAndVertex(gameObject);

        Debug.Log(
            $"[DELIVERY CHECK] {name} | " +
            $"Tile={resolvedLocation.tileName} | " +
            $"Vertex={resolvedLocation.vertexIndex} | " +
            $"Carrying={isCarryingBox}"
        );

        if (isCarryingBox &&
            !isDeliveringBox &&
            resolvedLocation.tileName == "AIStation" &&
            resolvedLocation.vertexIndex == 6)
        {
            Debug.Log(
                $"### DELIVERY CALL SITE ### {name} is calling DeliverBoxAtAIStation NOW."
);

            DeliverBoxAtAIStation();

            yield break;
        }

        // Normal movement: continue with the existing pickup logic.
        bool startedPickup = TryPickupBox();

        if (startedPickup)
        {
            Debug.Log(
                $"[PICKUP FLOW] {name} pickup started. " +
                $"PickupSequence will end the turn."
            );

            yield break;
        }

        Debug.Log(
            $"[PICKUP FLOW] {name} no pickup. Ending turn normally."
        );

        turnAlreadyFinished = true;

        if (turnManager != null)
            turnManager.HandleMoveFinished();
    }


    private bool TryPickupBox()
    {
        if (isCarryingBox)
            return false;

        WorldBox[] boxes =
            FindObjectsByType<WorldBox>(FindObjectsSortMode.None);

        foreach (WorldBox box in boxes)
        {
            if (box == null)
                continue;

            bool sameTile =
                box.tileX == currentTileCoord.x &&
                box.tileY == currentTileCoord.y;

            bool sameVertex =
                box.vertexIndex == currentVertexIndex;

            if (sameTile && sameVertex)
            {
                Debug.Log($"[PICKUP] Found {box.name}");

                StartCoroutine(PickupSequence(box));

                return true;
            }
        }

        return false;
    }

    void DebugBoxesOnMyTile()
    {
        Debug.Log($"[DEBUG OWNER] {name}", this);

        WorldBox[] boxes = FindObjectsOfType<WorldBox>();

        Debug.Log(
            $"[PICKUP CHECK] Player at tile {currentTileCoord}, vertex {currentVertexIndex}. Boxes found: {boxes.Length}"
        );

        if (boxes.Length > 0)
        {
            Debug.Log("First box name: " + boxes[0].name);
        }

        foreach (WorldBox box in boxes)
        {
            Debug.Log(
                $"[BOX] tile ({box.tileX}, {box.tileY}), vertex {box.vertexIndex}, active={box.gameObject.activeSelf}"
            );
        }
    }

    WorldBox FindBoxOnMyVertex()
    {
        WorldBox[] boxes = FindObjectsOfType<WorldBox>();

        // Get the exact vertex world position the player moved to
        string tileName = map.tileNameToGrid
            .FirstOrDefault(kvp => kvp.Value == currentTileCoord).Key;

        if (string.IsNullOrEmpty(tileName))
            return null;

        Vector3 myVertexWorldPos = map.GetVertexPosition(tileName, currentVertexIndex);

        const float pickupRadius = 0.15f; // SMALL, deterministic

        foreach (WorldBox box in boxes)
        {
            if (!box.gameObject.activeSelf)
                continue;

            Transform t = box.visualRoot != null ? box.visualRoot : box.transform;

            float dist = Vector3.Distance(t.position, myVertexWorldPos);

            Debug.Log($"[PICKUP DIST] {box.name} → {dist}");

            if (dist <= pickupRadius)
                return box;
        }

        return null;
    }



    IEnumerator PickupSequence(WorldBox box)
    {
        Debug.Log($"[PICKUP] Picking up {box.name}", this);

        if (animator != null)
        {
            animator.enabled = true;

            // Keep this because it previously fixed Player2 desync
            animator.Rebind();
            animator.Update(0f);

            animator.SetBool("BoxDown", false);
            animator.SetBool("BoxUp", true);

            // Play pickup sound at the same moment
            // the BoxUp animation begins.
            if (pickupAudioSource != null &&
                pickupClip != null)
            {
                pickupAudioSource.PlayOneShot(
                    pickupClip,
                    pickupVolume
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[PICKUP AUDIO] {name} pickup audio not assigned."
                );
            }
        }
        else
        {
            Debug.LogError($"[PICKUP] No Animator found for {name}");
        }

        carriedWorldBox = box;
        // Remove world box
        box.gameObject.SetActive(false);

        // Show internal carried box
        if (carriedBox != null)
        {
            carriedBox.SetActive(true);

            Renderer carriedRenderer =
                carriedBox.GetComponentInChildren<Renderer>(true);

            Debug.Log(
                $"[CARRIED BOX A] IMMEDIATELY | " +
                $"activeSelf={carriedBox.activeSelf} | " +
                $"activeInHierarchy={carriedBox.activeInHierarchy} | " +
                $"RendererEnabled={(carriedRenderer != null ? carriedRenderer.enabled : false)}"
            );
        }

        isCarryingBox = true;

        // Wait exactly one frame
        yield return null;

        if (carriedBox != null)
        {
            Renderer carriedRenderer =
                carriedBox.GetComponentInChildren<Renderer>(true);

            Debug.Log(
                $"[CARRIED BOX B] AFTER 1 FRAME | " +
                $"activeSelf={carriedBox.activeSelf} | " +
                $"activeInHierarchy={carriedBox.activeInHierarchy} | " +
                $"RendererEnabled={(carriedRenderer != null ? carriedRenderer.enabled : false)}"
            );
        }

        // Wait a little longer
        yield return new WaitForSeconds(0.1f);

        if (carriedBox != null)
        {
            Renderer carriedRenderer =
                carriedBox.GetComponentInChildren<Renderer>(true);

            Debug.Log(
                $"[CARRIED BOX C] AFTER 0.1 SEC | " +
                $"activeSelf={carriedBox.activeSelf} | " +
                $"activeInHierarchy={carriedBox.activeInHierarchy} | " +
                $"RendererEnabled={(carriedRenderer != null ? carriedRenderer.enabled : false)}"
            );
        }

        // Finish approximately the original 0.8-second delay
        yield return new WaitForSeconds(0.7f);

        if (animator != null)
            animator.SetBool("BoxUp", false);

        if (carriedBox != null)
        {
            Renderer carriedRenderer =
                carriedBox.GetComponentInChildren<Renderer>(true);

            Debug.Log(
                $"[CARRIED BOX D] BEFORE TURN CHANGE | " +
                $"activeSelf={carriedBox.activeSelf} | " +
                $"activeInHierarchy={carriedBox.activeInHierarchy} | " +
                $"RendererEnabled={(carriedRenderer != null ? carriedRenderer.enabled : false)}"
            );
        }

        if (turnManager != null)
        {
            Debug.Log("[PICKUP] Pickup complete. Ending turn.");

            turnManager.HandleMoveFinished();

            if (carriedBox != null)
            {
                Renderer carriedRenderer =
                    carriedBox.GetComponentInChildren<Renderer>(true);

                Debug.Log(
                    $"[CARRIED BOX E] AFTER TURN CHANGE | " +
                    $"activeSelf={carriedBox.activeSelf} | " +
                    $"activeInHierarchy={carriedBox.activeInHierarchy} | " +
                    $"RendererEnabled={(carriedRenderer != null ? carriedRenderer.enabled : false)}"
                );
            }
        }
    }


    IEnumerator DropBoxFinalize()
    {
        yield return new WaitForSeconds(0.7f);

        if (animator != null)
            animator.SetBool("BoxDown", false);

        Debug.Log($"[BOXDOWN] {name} animation finished");
    }
    void Update()
    {
        if (turnManager == null) return;
        if (turnManager.current != this) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log($"[INPUT] B pressed by {name}");
            DropBox();
        }


    }

    void DropBox()
    {
        if (!isCarryingBox)
            return;

        if (animator != null)
        {
            animator.SetBool("BoxUp", false);
            animator.SetBool("BoxDown", true);
        }

        if (carriedWorldBox != null)
        {
            carriedWorldBox.transform.position = transform.position;

            carriedWorldBox.tileX = currentTileCoord.x;
            carriedWorldBox.tileY = currentTileCoord.y;
            carriedWorldBox.vertexIndex = currentVertexIndex;
            carriedWorldBox.WorldVertexPosition = transform.position;

            carriedWorldBox.gameObject.SetActive(true);
        }

        if (carriedBox != null)
            carriedBox.SetActive(false);

        carriedWorldBox = null;
        isCarryingBox = false;

        Debug.Log($"[BOXDOWN] {name} dropped box");
        StartCoroutine(DropBoxFinalize());
    }

    private void DropCarriedBoxOnDeath()
    {
        if (!isCarryingBox)
            return;

        Debug.Log(
            $"[DEATH BOX] {name} died while carrying a box. " +
            $"Dropping it at {currentTileCoord} v{currentVertexIndex}."
        );

        // Stop any carry/drop animation state.
        if (animator != null)
        {
            animator.SetBool("BoxUp", false);
            animator.SetBool("BoxDown", false);
        }

        // Return the ORIGINAL WorldBox to the map
        // at the exact vertex where the player died.
        if (carriedWorldBox != null)
        {
            carriedWorldBox.transform.position =
                transform.position;

            carriedWorldBox.tileX =
                currentTileCoord.x;

            carriedWorldBox.tileY =
                currentTileCoord.y;

            carriedWorldBox.vertexIndex =
                currentVertexIndex;

            carriedWorldBox.WorldVertexPosition =
                transform.position;

            carriedWorldBox.gameObject.SetActive(true);

            Debug.Log(
                $"[DEATH BOX] {carriedWorldBox.name} restored to world."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[DEATH BOX] {name} says it is carrying a box, " +
                $"but carriedWorldBox is NULL."
            );
        }

        // Hide the robot's internal carried-box visual.
        if (carriedBox != null)
            carriedBox.SetActive(false);

        // Player no longer owns the box.
        carriedWorldBox = null;
        isCarryingBox = false;
    }

    public void Die()
    {
        if (isDead)
            return;

        // If carrying a box, leave it at the death vertex.
        DropCarriedBoxOnDeath();

        isDead = true;
        isMoving = false;

        Debug.Log(
            $"[{name}] Unit destroyed."
        );

        StartCoroutine(DieRoutine());
    }
    private IEnumerator DieRoutine()
    {
        Debug.Log($"[{name}] DieRoutine STARTED");

        if (animator != null)
        {
            animator.SetBool("Walking", false);
            animator.SetTrigger("Die");
        }

        // Death audio
        if (deathAudioSource != null &&
            deathClip != null)
        {
            deathAudioSource.PlayOneShot(
                deathClip,
                deathVolume
            );
        }
        else
        {
            Debug.LogWarning(
                $"[DEATH AUDIO] {name} death audio not assigned."
            );
        }

        // Start ONE respawn countdown.
        StartCoroutine(RespawnRoutine());

        // Give the death animation time to play.
        yield return new WaitForSeconds(5.0f);

        // If the movement already ended the turn,
        // do NOT advance it again.
        if (turnAlreadyFinished)
        {
            Debug.Log(
                $"[{name}] DieRoutine: turn already finished. " +
                $"No second turn advance."
            );

            yield break;
        }

        Debug.Log(
            $"[{name}] DieRoutine: ending turn after death."
        );

        turnAlreadyFinished = true;

        if (turnManager != null)
            turnManager.HandleMoveFinished();
    }


    private IEnumerator RespawnRoutine()
    {
        Debug.Log(
            $"[{name}] Respawn timer START at {Time.time:F2}"
        );

        yield return new WaitForSeconds(respawnDelay);

        Debug.Log(
            $"[{name}] Respawn timer END at {Time.time:F2}"
        );

        // Restore stats
        energy = 100;
        shield = 100;

        // Hide carried box visual after respawn
        if (carriedBox != null)
        {
            carriedBox.SetActive(false);

            Debug.Log(
                $"[CARRIED BOX] {name} starting with carried box hidden."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[CARRIED BOX] {name} has no Carried Box assigned."
            );
        }

        UpdateHealthBars();

        // -------------------------------------------------
        // POSITION BEFORE MAP RESPAWN
        // -------------------------------------------------
        Debug.Log(
            $"[RESPAWN DEBUG] {name} BEFORE map.RespawnPlayer " +
            $"World={transform.position} " +
            $"StoredTile={currentTileCoord} " +
            $"StoredVertex={currentVertexIndex}"
        );

        // Ask the map to respawn this player
        if (map != null)
        {
            map.RespawnPlayer(this);

            // -------------------------------------------------
            // POSITION AFTER MAP RESPAWN
            // -------------------------------------------------
            Debug.Log(
                $"[RESPAWN DEBUG] {name} AFTER map.RespawnPlayer " +
                $"World={transform.position} " +
                $"StoredTile={currentTileCoord} " +
                $"StoredVertex={currentVertexIndex}"
            );

            // Ask the map which actual graph vertex it thinks
            // the respawned player occupies.
            var resolvedLocation =
                map.GetPlayerTileAndVertex(gameObject);

            Debug.Log(
                $"[RESPAWN DEBUG] {name} MAP RESOLVED = " +
                $"{resolvedLocation.tileName} " +
                $"v{resolvedLocation.vertexIndex}"
            );
        }
        else
        {
            Debug.LogError(
                $"[RESPAWN DEBUG] {name}: map reference is NULL!"
            );
        }

        // Reset animator
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        isDead = false;

        Debug.Log(
            $"[RESPAWN END] {name} final position = " +
            $"{transform.position}"
        );

        Debug.Log(
            $"[{name}] Respawned."
        );
    }

    public (int shieldLost, int energyLost) TakeAttackDamage(int amount)
    {
        if (amount <= 0)
            return (0, 0);

        if (isDead)
            return (0, 0);

        int shieldLost = 0;
        int energyLost = 0;

        // Shield absorbs damage first.
        if (shield > 0)
        {
            shieldLost = Mathf.Min(shield, amount);
            shield -= shieldLost;
            amount -= shieldLost;
        }

        // Remaining damage goes to energy.
        if (amount > 0)
        {
            energyLost = Mathf.Min(energy, amount);
            energy -= energyLost;
        }

        Debug.Log(
            $"<color=#FF3333><b>⚔ ATTACK DAMAGE</b></color> " +
            $"<color=cyan><b>{name}</b></color>  " +
            $"<color=yellow>-{shieldLost} Shield</color>  " +
            $"<color=red>-{energyLost} Energy</color>  " +
            $"→ <color=yellow>S:{shield}</color> " +
            $"<color=lime>E:{energy}</color>"
        );

        UpdateHealthBars();

        if (energy <= 0)
        {
            Debug.Log(
                $"<color=red><b>☠ ATTACK DEPLETED {name}</b></color>"
            );

            Die();
        }

        return (shieldLost, energyLost);
    }

    public void TakeEnvironmentalDamage(int amount)
    {
        if (amount <= 0)
            return;

        int originalAmount = amount;
        int shieldLost = 0;
        int energyLost = 0;

        // Shield absorbs damage first
        if (shield > 0)
        {
            shieldLost = Mathf.Min(shield, amount);
            shield -= shieldLost;
            amount -= shieldLost;
        }

        // Remaining damage goes to energy
        if (amount > 0)
        {
            energyLost = Mathf.Min(energy, amount);
            energy -= energyLost;
        }

        Debug.Log(
            $"<color=#FF5500><b>☣ CORROSION</b></color> " +
            $"<color=cyan><b>{name}</b></color>  " +
            $"<color=yellow>-{shieldLost} Shield</color>  " +
            $"<color=red>-{energyLost} Energy</color>  " +
            $"→  <color=yellow>S:{shield}</color>  " +
            $"<color=lime>E:{energy}</color>"
        );

        if (energy <= 0)
        {
            Debug.Log(
                $"<color=red><b>☠ UNIT DEPLETED</b></color> " +
                $"<color=cyan>{name}</color>"
            );

            Die();
        }

        UpdateHealthBars();
    }

    private void UpdateHealthBars()
    {
        Debug.Log($"[BARS] Unit = {name}");
        Debug.Log($"Shield Path Parent = {shieldBar.parent.name} | Energy Path Parent = {energyBar.parent.name}");
        Debug.Log($"[BARS] Shield={shield}  Energy={energy}");

        if (shieldBar != null)
        {
            Debug.Log($"Shield Root = {shieldBar.root.name}");
            Debug.Log($"Shield Scale BEFORE = {shieldBar.localScale}");
            float shieldPercent = Mathf.Clamp01(shield / 100f);

            Vector3 scale = shieldBar.localScale;
            scale.x = shieldBarFullWidth * shieldPercent;
            shieldBar.localScale = scale;
            Debug.Log($"Shield Scale AFTER = {shieldBar.localScale}");

            Vector3 pos = shieldBarStartPos;
            pos.x = shieldBarStartPos.x - (shieldBarFullWidth - scale.x) * 0.5f;
            shieldBar.localPosition = pos;
        }

        if (energyBar != null)
        {
            Debug.Log($"Energy Root = {energyBar.root.name}");
            Debug.Log($"Energy Scale BEFORE = {energyBar.localScale}");
            float energyPercent = Mathf.Clamp01(energy / 100f);

            Vector3 scale = energyBar.localScale;
            Debug.Log($"Energy Scale AFTER = {energyBar.localScale}");
            scale.x = energyBarFullWidth * energyPercent;
            energyBar.localScale = scale;


            Vector3 pos = energyBarStartPos;
            pos.x = energyBarStartPos.x - (energyBarFullWidth - scale.x) * 0.5f;
            energyBar.localPosition = pos;
        }
    }

    private void OnDisable()
    {
        Debug.LogError(
            $"### PLAYERUNIT DISABLED ### {name} | " +
            $"ActiveSelf={gameObject.activeSelf} | " +
            $"ActiveInHierarchy={gameObject.activeInHierarchy}"
        );
    }

    private void OnDestroy()
    {
        Debug.LogError(
            $"### PLAYERUNIT DESTROYED ### {name}"
        );
    }

    public void Attack()
    {
        if (isDead || isMoving)
            return;

        Debug.Log($"[ATTACK] {name} attack started.");

        if (animator != null)
        {
            Debug.Log(
                $"[ATTACK ANIM] Animator found on {animator.gameObject.name}"
            );

            animator.SetBool("Attack", true);

            Debug.Log(
                $"[ATTACK ANIM] Attack bool = {animator.GetBool("Attack")}"
            );
        }
        else
        {
            Debug.LogError(
                $"[ATTACK ANIM] {name} has NO Animator reference!"
            );
        }
    }

    public void AttackImpact()
    {
        Debug.Log($"[ATTACK IMPACT] {name} impact frame reached.");

        if (attackAudioSource != null && attackImpactClip != null)
        {
            attackAudioSource.PlayOneShot(attackImpactClip);
        }

        if (turnManager != null)
            turnManager.HandleAttackImpact();
    }
    public void FinishAttack()
    {
        if (animator != null)
        {
            Debug.Log(
                $"[ATTACK ANIM] FinishAttack reached on {name}"
            );

            animator.SetBool("Attack", false);
        }

        Debug.Log($"[ATTACK] {name} attack animation finished.");

        if (turnManager != null)
            turnManager.HandleAttackFinished();
    }

    private void DeliverBoxAtAIStation()
    {
        if (isDeliveringBox)
            return;

        isDeliveringBox = true;
        isMoving = false;

        Debug.Log(
            $"[DELIVERY] {name} reached AIStation v6 with a box."
        );

        // Start delivery camera.
        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow != null)
        {
            Debug.Log(
                $"[CAMERA DELIVERY] {name} starting delivery focus."
            );

            cameraFollow.BeginDeliveryFocus(
                transform
            );
        }
        else
        {
            Debug.LogError(
                $"[CAMERA DELIVERY] {name} CameraFollow not found."
            );
        }

        // IMPORTANT:
        // Start BoxDown directly instead of waiting through Invoke.
        StartAIStationBoxDown();
    }

    private void StartAIStationBoxDown()
    {
        Debug.Log(
            $"[DELIVERY] {name} StartAIStationBoxDown reached."
        );

        if (animator != null)
        {
            animator.SetBool(
                "BoxUp",
                false
            );

            animator.SetBool(
                "BoxDown",
                true
            );

            Debug.Log(
                $"[DELIVERY] {name} BoxDown started."
            );
        }
        else
        {
            Debug.LogError(
                $"[DELIVERY] {name} Animator is NULL."
            );
        }
        if (cameraFollow != null)
        {
            cameraFollow.ScheduleDeliveryFinish(
                this,
                deliveryAnimationDuration
            );
        }
        else
        {
            Debug.LogError(
                $"[DELIVERY] {name} cannot schedule finish because CameraFollow is NULL."
            );
        }
    }

    public void CompleteAIStationDelivery()
    {
        Debug.Log(
            $"[DELIVERY] {name} completion requested by CameraFollow."
        );

        StartCoroutine(
            FinishAIStationDeliveryRoutine()
        );
    }
    private IEnumerator FinishAIStationDeliveryRoutine()
    {
        Debug.Log(
            $"[DELIVERY] {name} FinishAIStationDeliveryRoutine reached."
        );

        // Finish BoxDown state.
        if (animator != null)
        {
            animator.SetBool(
                "BoxDown",
                false
            );
        }

        // Hide robot's internal carried box.
        if (carriedBox != null)
        {
            carriedBox.SetActive(false);
        }

        // Keep a reference to the delivered world box.
        // We will redeploy it ONLY if this was not the winning delivery.
        WorldBox deliveredBox = carriedWorldBox;
        carriedWorldBox = null;

        isCarryingBox = false;

        // -----------------------------------------
        // DELIVERY VISUAL FEEDBACK
        // -----------------------------------------
        if (aiStationFeedback == null)
        {
            aiStationFeedback =
                FindFirstObjectByType<AIStationFeedback>();
        }

        Debug.Log(
            $"[DELIVERY FEEDBACK TEST] {name} | " +
            $"Reference = " +
            $"{(aiStationFeedback != null ? aiStationFeedback.gameObject.name : "NULL")}"
        );

        if (aiStationFeedback != null)
        {
            aiStationFeedback.PlayDeliveryFlash();
        }

        // -----------------------------------------
        // SCORE + WIN CHECK
        // -----------------------------------------
        bool wonMatch = false;

        if (ScoreUI.Instance != null)
        {
            wonMatch = ScoreUI.Instance.AddScore(
                teamID,
                1
            );

            Debug.Log(
                $"[SCORE] {name} awarded Team {teamID} +1."
            );
        }
        else
        {
            Debug.LogError(
                "[SCORE ERROR] ScoreUI.Instance is NULL."
            );
        }

        // -----------------------------------------
        // REDEPLOY OBJECTIVE ONLY IF MATCH CONTINUES
        // -----------------------------------------
        if (!wonMatch && deliveredBox != null)
        {
            if (BoxDeploymentManager.Instance != null)
            {
                BoxDeploymentManager.Instance.RedeployDeliveredBox(
                    deliveredBox
                );
            }
            else
            {
                Debug.LogWarning(
                    "[BOX DEPLOYMENT] BoxDeploymentManager not found."
                );
            }
        }
        else if (wonMatch)
        {
            Debug.Log(
                $"[MATCH END] Player {teamID} completed the final delivery."
            );
        }

        // Keep delivery camera on station/player
        // while pulse and score feedback are visible.
        yield return new WaitForSecondsRealtime(1.0f);

        // Restore camera AFTER feedback.
        if (cameraFollow != null)
        {
            Debug.Log(
                $"[CAMERA DELIVERY] {name} restoring normal camera."
            );

            cameraFollow.EndDeliveryFocus();
        }
        else
        {
            Debug.LogError(
                $"[CAMERA DELIVERY] {name} cannot restore camera — cameraFollow is NULL."
            );
        }

        isDeliveringBox = false;
        turnAlreadyFinished = true;

        // Winning delivery stops the match here.
        if (wonMatch)
        {
            if (ScoreUI.Instance != null)
            {
                ScoreUI.Instance.ShowVictory(teamID);
            }
            else
            {
                Debug.LogError(
                    "[VICTORY UI] ScoreUI.Instance is NULL."
                );
            }

            Debug.Log(
                $"[MATCH END] Player {teamID} wins. No next turn."
            );

            yield break;
        }

        // Normal delivery: continue to next player.
        if (turnManager != null)
        {
            turnManager.HandleMoveFinished();
        }
        else
        {
            Debug.LogError(
                $"[DELIVERY] {name} TurnManager is NULL."
            );
        }
    }
}
