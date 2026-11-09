
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerClickFeedback : MonoBehaviour
{
    [Header("HOVER FEEDBACK")]
    [SerializeField]
    private float hoverMultiplier = 1.3f;

    [SerializeField]
    private float hoverSpeed = 8f;

    [SerializeField]
    private float pointerPadding = 12f;

    private Vector3 originalScale;
    private Renderer[] visualRenderers;
    private float hoverBlend;

    private static readonly List<PlayerClickFeedback> units =
        new List<PlayerClickFeedback>();

    private static PlayerClickFeedback hoveredUnit;

    public static PlayerUnit HoveredPlayerUnit
    {
        get
        {
            if (hoveredUnit == null ||
                !hoveredUnit.isActiveAndEnabled)
            {
                return null;
            }

            PlayerUnit unit =
                hoveredUnit.GetComponentInParent<PlayerUnit>();

            if (unit == null)
            {
                unit =
                    hoveredUnit.GetComponentInChildren<PlayerUnit>(
                        true
                    );
            }

            return unit;
        }
    }
    private static int lastCheckedFrame = -1;

    private void Awake()
    {
        originalScale = transform.localScale;

        visualRenderers =
            GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        if (!units.Contains(this))
            units.Add(this);
    }

    private void Update()
    {
        // Resolve the hovered robot only once per frame.
        if (lastCheckedFrame != Time.frameCount)
        {
            lastCheckedFrame = Time.frameCount;
            ResolveHoveredUnit();
        }

        float targetBlend =
            hoveredUnit == this ? 1f : 0f;

        hoverBlend = Mathf.MoveTowards(
            hoverBlend,
            targetBlend,
            hoverSpeed * Time.deltaTime
        );

        transform.localScale = Vector3.Lerp(
            originalScale,
            originalScale * hoverMultiplier,
            hoverBlend
        );
    }

    private static void ResolveHoveredUnit()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            hoveredUnit = null;
            return;
        }

        Vector2 mouse = Input.mousePosition;

        if (mouse.x < 0 || mouse.y < 0 ||
            mouse.x > Screen.width ||
            mouse.y > Screen.height)
        {
            hoveredUnit = null;
            return;
        }

        
        PlayerClickFeedback best = null;
        float bestScore = float.PositiveInfinity;

        foreach (PlayerClickFeedback unit in units)
        {
            if (unit == null || !unit.isActiveAndEnabled)
                continue;

            float score =
                unit.GetScreenHoverScore(cam, mouse);

            if (score < bestScore)
            {
                bestScore = score;
                best = unit;
            }
        }

        // Small hysteresis prevents flickering when
        // two robots overlap on screen.
        if (hoveredUnit != null &&
            hoveredUnit.isActiveAndEnabled)
        {
            float previousScore =
                hoveredUnit.GetScreenHoverScore(cam, mouse);

            if (!float.IsInfinity(previousScore) &&
                previousScore <= bestScore + 12f)
            {
                return;
            }
        }

        hoveredUnit = best;
    }


    private float GetScreenHoverScore(
        Camera cam,
        Vector2 mouse)
    {
        Renderer mainRenderer = null;
        float largestSize = -1f;
        bool foundSkinned = false;

        foreach (Renderer renderer in visualRenderers)
        {
            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            bool isSkinned =
                renderer is SkinnedMeshRenderer;

            if (!isSkinned &&
                !(renderer is MeshRenderer))
            {
                continue;
            }

            // Prefer the actual animated robot mesh.
            if (foundSkinned && !isSkinned)
                continue;

            if (isSkinned && !foundSkinned)
            {
                foundSkinned = true;
                largestSize = -1f;
            }

            float size =
                renderer.bounds.size.sqrMagnitude;

            if (size > largestSize)
            {
                largestSize = size;
                mainRenderer = renderer;
            }
        }

        if (mainRenderer == null)
            return float.PositiveInfinity;

        Vector3 screen =
            cam.WorldToScreenPoint(
                mainRenderer.bounds.center
            );

        if (screen.z <= 0f)
            return float.PositiveInfinity;

        float distance =
            Vector2.Distance(
                mouse,
                new Vector2(screen.x, screen.y)
            );

        // Generous selectable area around the robot.
        const float hoverRadius = 65f;

        if (distance > hoverRadius)
            return float.PositiveInfinity;

        return distance;
    }


    private void OnDisable()
    {
        units.Remove(this);

        if (hoveredUnit == this)
            hoveredUnit = null;

        hoverBlend = 0f;
        transform.localScale = originalScale;
    }

    // No OnMouseDown: movement-marker clicks are unchanged.
}
