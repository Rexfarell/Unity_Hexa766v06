using UnityEngine;

public class PlayerClickFeedback : MonoBehaviour
{
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnMouseEnter()
    {
        // 🔒 HARD GUARD — never allow hover reach during turns
        if (TurnManager.Instance != null && TurnManager.Instance.current != null)
            return;

        Map1HexGrid map = FindObjectOfType<Map1HexGrid>();
        if (map == null) return;

        // Use EXISTING methods only
        if (gameObject.name.Contains("Player1"))
            map.ShowReachFromPlayer1();
        else if (gameObject.name.Contains("Player2"))
            map.ShowReachFromPlayer2();
    }



    private void OnMouseExit()
    {
        // Return to original scale
        transform.localScale = originalScale;
    }

    // NO OnMouseDown — clicks pass through to spheres
}