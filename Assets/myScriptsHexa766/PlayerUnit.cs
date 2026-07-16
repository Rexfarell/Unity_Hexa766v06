using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;

public class PlayerUnit : MonoBehaviour
{
    public int teamID;               // 1 = Player1, 2 = Player2
    public int energy = 100;
    public int shield = 100;  // ← depletes before energy
    
    public Vector2Int currentTileCoord;
    public int currentVertexIndex = 0;
    public System.Action OnMoveFinished;
    public System.Action onActionResolved;
    private bool isResolvingAction = false;
    private TurnManager turnManager;
    private Map1HexGrid map;
    private bool isMoving = false;

    [SerializeField] private Animator animator;
    [SerializeField] private GameObject carriedBox; // robot internal box
    [SerializeField] private float moveSpeed = 3f;
    private bool isCarryingBox = false;

    void Start()
    {
        map = FindObjectOfType<Map1HexGrid>();
        turnManager = FindObjectOfType<TurnManager>();

        if (!map)
        {
            Debug.LogError($"[{name}] Map1HexGrid missing!");
            return;
        }

        // 🔑 REGISTER THIS UNIT WITH THE TURN MANAGER
        if (turnManager != null && !turnManager.players.Contains(this))
        {
            turnManager.players.Add(this);
            Debug.Log($"[TURN] Registered player unit: {name}");
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

        isMoving = false;

        TryPickupBox();

        if (!isCarryingBox)
        {
            turnManager.HandleMoveFinished();
        }
    }


    void TryPickupBox()
    {
        if (isCarryingBox) return;

        WorldBox box = FindBoxOnMyVertex();
        if (box == null) return;

        StartCoroutine(PickupSequence(box));
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
        

        Debug.Log("[PICKUP] Triggering BoxUp", animator);

        if (animator != null)
        {
            animator.enabled = true;

            // HARD RESET — fixes Player2 desync
            animator.Rebind();
            animator.Update(0f);

            Debug.Log($"[BOXUP] Trigger fired by {name} at frame {Time.frameCount}");
            animator.ResetTrigger("BoxDown");
            animator.ResetTrigger("BoxUp");
            animator.SetTrigger("BoxUp");
        }

        else
        {
            Debug.LogError($"[PICKUP] No Animator found for {name}");
        }

        yield return new WaitForSeconds(0.8f);

        box.gameObject.SetActive(false);

        if (carriedBox != null)
            carriedBox.SetActive(true);

        isCarryingBox = true;

        // PICKUP CONSUMES THE TURN
 

        if (turnManager != null)
        {
            Debug.Log("[PICKUP] Pickup complete. Ending turn.");
            turnManager.HandleMoveFinished();
        }
    }

    

    IEnumerator DropBoxFinalize()
    {
        yield return new WaitForSeconds(0.7f); // match animation length

        if (carriedBox != null)
            carriedBox.SetActive(false);

        isCarryingBox = false;

        Debug.Log($"[BOXDOWN] {name} finished drop");

        onActionResolved?.Invoke();
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
        if (!isCarryingBox) return;

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.ResetTrigger("BoxUp");
            animator.SetTrigger("BoxDown");
        }

        // Spawn new world box at current vertex
        WorldBox newBox = Instantiate(
            FindObjectOfType<WorldBox>(),
            transform.position,
            Quaternion.identity
        );

        newBox.tileX = currentTileCoord.x;
        newBox.tileY = currentTileCoord.y;
        newBox.vertexIndex = currentVertexIndex;

        // Team color
        Renderer r = newBox.GetComponentInChildren<Renderer>();
        if (r != null)
            r.material.color = teamID == 1 ? Color.blue : Color.red;

        if (carriedBox != null)
            carriedBox.SetActive(false);

        isCarryingBox = false;

        Debug.Log($"[BOXDOWN] {name} dropped box");
    }


}
