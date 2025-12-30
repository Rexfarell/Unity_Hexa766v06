using System.Collections;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class PlayerUnit : MonoBehaviour
{
    public int teamID;               // 1 = Player1, 2 = Player2
    public int energy = 100;
    public int movementPoints = 1;
    public Vector2Int currentTileCoord;
    public int currentVertexIndex = 0;

    private Map1HexGrid map;

    void Start()
    {
        map = FindObjectOfType<Map1HexGrid>();
        if (!map) Debug.LogError($"[{name}] Map1HexGrid missing!");
        else
        {
            string tileName = map.TileAt(transform.position);
            if (tileName == "OriginRed") currentTileCoord = new Vector2Int(0, 0);
            else if (tileName == "OriginBlue") currentTileCoord = new Vector2Int(5, 5);
            Debug.Log($"[{name}] start @ {currentTileCoord} v{currentVertexIndex}");
        }
    }

    public bool MoveToTile(Vector2Int tileCoord, int vertexIdx)
    {
        Debug.Log($"[MOVE] MoveToTile called — from {currentTileCoord} v{currentVertexIndex} to {tileCoord} v{vertexIdx}");

        // REVERSE LOOKUP: grid coord → tile name → tile object → vertex position
        string tileName = map.tileNameToGrid.FirstOrDefault(kvp => kvp.Value == tileCoord).Key;

        Vector3 pos;
        if (!string.IsNullOrEmpty(tileName))
        {
            pos = map.GetVertexPosition(tileName, vertexIdx);   // ← use tile name (string) version
        }
        else
        {
            Debug.LogWarning($"[{name}] No tile name for coord {tileCoord} — fallback to current position");
            pos = transform.position;
        }

        StartCoroutine(SmoothMove(pos));
        currentTileCoord = tileCoord;
        currentVertexIndex = vertexIdx;

        int dmg = Random.Range(20, 81);
        energy = Mathf.Max(0, energy - dmg);
        Debug.Log($"[{name}] moved to {tileCoord} v{vertexIdx} – dmg {dmg} – energy {energy}");

        if (energy <= 0)
        {
            gameObject.SetActive(false);
            Debug.Log($"{name} KO");
        }

        movementPoints--;
        return true;
    }

    public bool IsValidMove(Vector2Int tile, int v)
    {
        if (currentTileCoord == tile && currentVertexIndex == v) return false;

        int dq = tile.x - currentTileCoord.x;
        int dr = tile.y - currentTileCoord.y;
        if (Mathf.Abs(dq) > 1 || Mathf.Abs(dr) > 1 || Mathf.Abs(dq + dr) > 1) return false;  // ← THIS LINE IS KILLING YOU

        // ... rest
        return movementPoints > 0;
    }

    public void ResetTurn()
    {
        movementPoints = 1;
    }

    IEnumerator SmoothMove(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0, dur = 0.4f;
        while (t < dur)
        {
            transform.position = Vector3.Lerp(start, target, t / dur);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = target;
    }

    // ==============================================================
    // HELPER: Convert Vector2Int → World Position → Tile GameObject
    // ==============================================================
    private GameObject GetTileObject(Vector2Int gridPos)
    {
        // Convert grid coordinates to world position (hex grid layout)
        float cellSize = map.hexSize + map.gapSize;
        Vector3 worldPos = new Vector3(
            gridPos.x * 1.5f * cellSize,
            0f,
            gridPos.y * (Mathf.Sqrt(3f) * cellSize)
        );

        // Offset every other row
        if (gridPos.y % 2 != 0)
            worldPos.x += 0.75f * cellSize;

        // Use map.TileAt() to get tile name
        string tileName = map.TileAt(worldPos);
        if (string.IsNullOrEmpty(tileName))
            return null;

        // Try direct lookup
        if (map.tileGameObjectMap.TryGetValue(tileName, out GameObject tile))
            return tile;

        // Fallback
        return GameObject.Find(tileName);
    }
}