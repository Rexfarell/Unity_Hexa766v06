using UnityEngine;

public class PlayerClickFeedback : MonoBehaviour
{
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnMouseEnter()
    {
        // Hover feedback (scale up)
        transform.localScale = originalScale * 1.2f;

        var grid = FindObjectOfType<Map1HexGrid>();
        if (grid == null || !grid.hasGenerated) return;   // ← THIS LINE PREVENTS EARLY CALL

        if (gameObject == grid.Player1)
            grid.ShowReachFromPlayer1();
        else if (gameObject == grid.Player2)
            grid.ShowReachFromPlayer2();
    }

    private void OnMouseExit()
    {
        // Return to original scale
        transform.localScale = originalScale;
    }

    // NO OnMouseDown — clicks pass through to spheres
}