using System.Collections;
using UnityEngine;

public class ElectricArcEffect : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LineRenderer coreRenderer;

    [Header("ARC SETTINGS")]
    [SerializeField] private int segments = 16;
    [SerializeField] private float jitterAmount = 0.12f;
    [SerializeField] private float lifetime = 0.22f;
    [SerializeField] private float refreshRate = 0.025f;

    private Transform startPoint;
    private Transform endPoint;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (coreRenderer == null)
        {
            LineRenderer[] renderers =
                GetComponentsInChildren<LineRenderer>(true);

            foreach (LineRenderer lr in renderers)
            {
                if (lr != lineRenderer)
                {
                    coreRenderer = lr;
                    break;
                }
            }
        }

        if (lineRenderer == null)
        {
            Debug.LogError(
                "[ELECTRIC ARC] Main LineRenderer missing.",
                this
            );
        }

        if (coreRenderer == null)
        {
            Debug.LogWarning(
                "[ELECTRIC ARC] Core LineRenderer missing.",
                this
            );
        }
    }

    public void Initialize(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;

        StartCoroutine(ArcRoutine());
    }

    private IEnumerator ArcRoutine()
    {
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            if (startPoint == null || endPoint == null)
                break;

            DrawArc();

            yield return new WaitForSeconds(refreshRate);
            elapsed += refreshRate;
        }

        Destroy(gameObject);
    }

    private void DrawArc()
    {
        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;

        Vector3 direction = end - start;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector3 side =
            Vector3.Cross(direction.normalized, Vector3.up);

        if (side.sqrMagnitude < 0.001f)
            side = Vector3.right;

        side.Normalize();

        Vector3 up =
            Vector3.Cross(direction.normalized, side).normalized;

        Vector3[] points = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            Vector3 pos = Vector3.Lerp(start, end, t);

            if (i != 0 && i != segments - 1)
            {
                float sideOffset =
                    Random.Range(-jitterAmount, jitterAmount);

                float upOffset =
                    Random.Range(-jitterAmount, jitterAmount);

                pos += side * sideOffset + up * upOffset;
            }

            points[i] = pos;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = segments;
            lineRenderer.SetPositions(points);
        }

        if (coreRenderer != null)
        {
            coreRenderer.positionCount = segments;
            coreRenderer.SetPositions(points);
        }
    }
}