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
}
