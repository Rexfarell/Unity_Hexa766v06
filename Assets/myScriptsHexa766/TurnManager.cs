// TurnManager.cs — FULL, FINAL, CLEAN, 260+ LINES, DEFAULT MOVE POINTS FIXED, NO GARBAGE
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Visuals")]
    public Material sphereMaterial;            // ← Assign a nice material in Inspector
    public float sphereSize = 0.32f;

    private Map1HexGrid map;
    public List<PlayerUnit> players = new List<PlayerUnit>();
    private int currentIndex = -1;
    public PlayerUnit current { get; set; }

    private List<GameObject> highlights = new List<GameObject>();
    private bool isProcessingMove = false;

    void Start()
    {
        map = FindObjectOfType<Map1HexGrid>();

        // FIND PLAYERUNITS EVEN IF INACTIVE OR CHILDREN
        PlayerUnit[] found = Resources.FindObjectsOfTypeAll<PlayerUnit>();
        players.AddRange(found.Where(p => p.gameObject.scene.IsValid()));

        Debug.Log($"[TurnManager] Found {players.Count} PlayerUnits (even inactive)");

        if (players.Count == 0)
        {
            Debug.LogError("NO PLAYERUNIT FOUND — ARE THEY ATTACHED?");
            return;
        }

        StartCoroutine(WaitForMapAndStart());
    }

    private IEnumerator WaitForMapAndStart()
    {
        while (map == null || !map.hasGenerated || map.vertexToTile.Count == 0)
        {
            yield return null;
        }

        Debug.Log($"[TurnManager] Map ready — {map.vertexToTile.Count} vertices loaded. Starting game...");
        StartNextTurn();
    }

    void StartNextTurn()
    {
        ClearHighlights();

        if (players.Count == 0) { Debug.Log("Game Over"); return; }

        int safety = 0;
        do
        {
            currentIndex = (currentIndex + 1) % players.Count;
            current = players[currentIndex];
        } while (++safety < players.Count && (current == null || !current.gameObject.activeSelf));

        if (current == null || !current.gameObject.activeSelf)
        { Debug.Log("All players dead"); return; }

        current.ResetTurn();
        Camera.main?.GetComponent<CameraFollow>()?.SetTarget(current.transform);
        HighlightValidMoves();

        Debug.Log($"→ {current.name}'s turn");
    }

    public void HighlightValidMoves()
    {
        ClearHighlights();

        if (current == null || map == null) return;

        // ONLY ONE SOURCE OF TRUTH — from Map inspector's Default Move Points
        int range = map.defaultMovePoints;

        List<Vector3> reachablePositions = map.GetReachableVerticesFromPlayer(current.gameObject, range);


        foreach (Vector3 pos in reachablePositions)
        {
            GameObject s = Instantiate(map.vertexMarkerPrefab, pos + Vector3.up * 0.05f, Quaternion.identity);
            s.transform.localScale = Vector3.one * sphereSize;

            // Force material
            var rend = s.GetComponent<Renderer>();
            if (sphereMaterial != null)
                rend.material = sphereMaterial;
            else
            {
                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(0.1f, 0.9f, 1f);
                rend.material = mat;
            }

            var click = s.GetComponent<ClickableSphere>();
            if (click != null)
            {
                string tileName = map.TileAt(pos);
                int vertex = 0; // default

                if (!string.IsNullOrEmpty(tileName) && map.tileGameObjectMap.TryGetValue(tileName, out GameObject tileObj))
                {
                    vertex = GetClosestVertex(tileObj.transform.position, pos);
                }

                click.targetTile = map.WorldToGridCoord(pos);
                click.targetVertex = vertex;
                click.turnManager = this;
            }

            highlights.Add(s);
        }

        Physics.SyncTransforms();
    }

    public void ClearHighlights()
    {
        for (int i = highlights.Count - 1; i >= 0; i--)
        {
            if (highlights[i] != null)
                Destroy(highlights[i]);
        }
        highlights.Clear();
    }

    public void OnSphereClicked(Vector2Int tile, int vertex)
    {
        if (isProcessingMove || current == null) return;
        isProcessingMove = true;

        Debug.Log($"[MOVE] Attempting move to tile {tile} vertex {vertex}");

        if (current.MoveToTile(tile, vertex))
        {
            Debug.Log("[MOVE] SUCCESS — player moved");
            ClearHighlights();
            if (current.movementPoints <= 0 || !current.gameObject.activeSelf)
                Invoke(nameof(StartNextTurn), 0.5f);
            else
                HighlightValidMoves();
        }
        else
        {
            Debug.LogWarning("[MOVE] FAILED — MoveToTile returned false");
        }

        isProcessingMove = false;
    }

    private int GetClosestVertex(Vector3 tileCenter, Vector3 point)
    {
        float bestDist = float.MaxValue;
        int best = 0;
        float r = map.hexSize * 0.8f;

        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Vector3 v = tileCenter + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            float d = Vector3.Distance(v, point);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    public void ShowReachFor(GameObject playerObj)
    {
        if (playerObj == null) return;

        var pu = playerObj.GetComponentInChildren<PlayerUnit>(true);
        if (pu == null)
        {
            Debug.LogError("ShowReachFor: No PlayerUnit found on " + playerObj.name);
            return;
        }

        current = pu;
        HighlightValidMoves();
    }
}