using UnityEngine;

public class WorldBox : MonoBehaviour
{
    public Vector3 WorldVertexPosition;
    public int tileX;
    public int tileY;
    public int vertexIndex;

    public Transform visualRoot;


    void Start()
    {

        Map1HexGrid map = FindObjectOfType<Map1HexGrid>();
        if (!map)
        {
            Debug.LogError($"[WorldBox] Map1HexGrid not found", this);
            return;
        }

        Vector2Int coord = map.WorldToGridCoord(transform.position);
        tileX = coord.x;
        tileY = coord.y;

        WorldVertexPosition = transform.position;


        Debug.Log($"[WorldBox] {name} registered at tile ({tileX}, {tileY}) vertex {vertexIndex}", this);
    }

    [ContextMenu("DEBUG — Box vs Movement Marker")]
    private void DebugBoxVsMovementMarker()
    {
        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
        {
            Debug.LogError(
                $"[BOX MARKER DEBUG] {name} — Map1HexGrid missing."
            );

            return;
        }

        Vector2Int boxCoord =
            new Vector2Int(
                tileX,
                tileY
            );

        Debug.Log(
            $"[BOX MARKER DEBUG] {name} | " +
            $"Logical={boxCoord} v{vertexIndex} | " +
            $"World={transform.position}"
        );

        ClickableSphere[] markers =
            FindObjectsByType<ClickableSphere>(
                FindObjectsSortMode.None
            );

        ClickableSphere matchingMarker = null;

        foreach (ClickableSphere marker in markers)
        {
            if (marker == null)
                continue;

            if (marker.targetTile == boxCoord &&
                marker.targetVertex == vertexIndex)
            {
                matchingMarker = marker;
                break;
            }
        }

        if (matchingMarker == null)
        {
            Debug.LogWarning(
                $"[BOX MARKER DEBUG] {name} | " +
                $"NO movement marker exists for " +
                $"{boxCoord} v{vertexIndex}."
            );

            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                matchingMarker.transform.position
            );

        Debug.Log(
            $"[BOX MARKER DEBUG] MATCH FOUND | " +
            $"Marker={matchingMarker.name} | " +
            $"MarkerWorld={matchingMarker.transform.position} | " +
            $"BoxWorld={transform.position} | " +
            $"Distance={distance:F4}"
        );
    }
}
