using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableSphere : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int targetTile;
    public int targetVertex;
    public TurnManager turnManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[CLICK] Sphere clicked! Tile: " + targetTile + " Vertex: " + targetVertex);
        turnManager?.OnSphereClicked(targetTile, targetVertex);
    }
}