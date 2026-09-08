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
        // OnMouse events can still arrive while this component is disabled.
        if (!enabled)
            return;

        transform.localScale = originalScale * 2.4f;
    }

    private void OnMouseExit()
    {
        if (!enabled)
            return;

        transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        // Prevent the player remaining enlarged if feedback
        // gets disabled while the mouse is over it.
        transform.localScale = originalScale;
    }

    // NO OnMouseDown — clicks pass through to spheres
}