using System.Collections;
using UnityEngine;

public class PlayerClickFeedback : MonoBehaviour
{
    [Header("Visual Feedback")]
    public float hoverScale = 1.2f;
    public float clickScale = 1.4f;
    public float animationDuration = 0.1f;

    [Header("COLORS")]
    public Color hoverColor = Color.yellow;
    public Color normalColor = Color.white;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private Renderer playerRenderer;
    private Color originalColor;

    void Awake()
    {
        originalScale = transform.localScale;

        // FIXED: look in children for SkinnedMeshRenderer or MeshRenderer
        playerRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();

        if (playerRenderer != null && playerRenderer.material != null)
            originalColor = playerRenderer.material.color;
    }

    void OnMouseEnter()
    {
        StopScaleCoroutine();
        scaleCoroutine = StartCoroutine(ScaleTo(hoverScale, animationDuration));

        // COLOR CHANGE
        if (playerRenderer != null && playerRenderer.material != null)
        {
            playerRenderer.material.color = hoverColor;
        }

        Debug.Log($"[HOVER] {gameObject.name}");
    }

    void OnMouseExit()
    {
        StopScaleCoroutine();
        scaleCoroutine = StartCoroutine(ScaleTo(1f, animationDuration));

        // RESTORE COLOR
        if (playerRenderer != null && playerRenderer.material != null)
        {
            playerRenderer.material.color = originalColor;
        }
    }

    void OnMouseDown()
    {
        // ... your existing scale/click feedback code ...

        var grid = FindObjectOfType<Map1HexGrid>();
        if (grid == null) return;

        if (gameObject == grid.Player1)
            grid.ShowReachFromPlayer1();
        else if (gameObject == grid.Player2)
            grid.ShowReachFromPlayer2();   // ← now exists!
    }

    private IEnumerator ScaleTo(float targetScale, float duration)
    {
        Vector3 start = transform.localScale;
        Vector3 end = originalScale * targetScale;
        float t = 0;

        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(start, end, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = end;
    }

    private IEnumerator ClickPulse()
    {
        yield return ScaleTo(clickScale, animationDuration * 0.5f);
        yield return ScaleTo(1f, animationDuration);
    }

    private void StopScaleCoroutine()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }

    void OnDisable()
    {
        StopScaleCoroutine();
        transform.localScale = originalScale;
    }
}