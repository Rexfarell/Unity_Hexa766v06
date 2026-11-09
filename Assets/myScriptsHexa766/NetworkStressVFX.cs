
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkStressVFX : MonoBehaviour
{
    private const float Duration = 2.3f;
    private const float LineWidth = 0.075f;

    private HexCapacity target;
    private Material effectMaterial;

    private readonly List<Vector3> corners =
        new List<Vector3>();

    private readonly List<LineRenderer> fractures =
        new List<LineRenderer>();

    public static void Play(HexCapacity hex)
    {
        if (hex == null || hex.IsCollapsed)
            return;

        GameObject effectObject = new GameObject(
            "NETWORK_STRESS_FRACTURE_VFX"
        );

        NetworkStressVFX effect =
            effectObject.AddComponent<NetworkStressVFX>();

        effect.Initialize(hex);
    }

    private void Initialize(HexCapacity hex)
    {
        target = hex;

        MeshFilter filter =
            hex.GetComponent<MeshFilter>();

        if (filter == null ||
            filter.sharedMesh == null)
        {
            Debug.LogWarning(
                "[NETWORK STRESS VFX] Hex mesh unavailable."
            );

            Destroy(gameObject);
            return;
        }

        Mesh mesh = filter.sharedMesh;

        List<Vector2> points = new List<Vector2>();

        foreach (Vector3 vertex in mesh.vertices)
        {
            Vector2 point =
                new Vector2(vertex.x, vertex.z);

            if (!points.Exists(p =>
                Vector2.Distance(p, point) < 0.001f))
            {
                points.Add(point);
            }
        }

        points.Sort((a, b) =>
        {
            int x = a.x.CompareTo(b.x);
            return x != 0 ? x : a.y.CompareTo(b.y);
        });

        if (points.Count < 3)
        {
            Destroy(gameObject);
            return;
        }

        // Extract outer perimeter.
        List<Vector2> hull = new List<Vector2>();

        foreach (Vector2 p in points)
        {
            while (hull.Count >= 2 &&
                Cross(
                    hull[hull.Count - 2],
                    hull[hull.Count - 1],
                    p) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(p);
        }

        int lowerCount = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            Vector2 p = points[i];

            while (hull.Count > lowerCount &&
                Cross(
                    hull[hull.Count - 2],
                    hull[hull.Count - 1],
                    p) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(p);
        }

        hull.RemoveAt(hull.Count - 1);

        float topY =
            mesh.bounds.max.y + 0.055f;

        foreach (Vector2 p in hull)
        {
            corners.Add(
                new Vector3(p.x, topY, p.y)
            );
        }

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning(
                "[NETWORK STRESS VFX] Shader unavailable."
            );

            Destroy(gameObject);
            return;
        }

        effectMaterial = new Material(shader);

        // Two fractured sections per perimeter edge.
        int segmentCount = corners.Count * 2;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segmentObject =
                new GameObject("StressFracture_" + i);

            segmentObject.transform.SetParent(
                transform, false
            );

            LineRenderer line =
                segmentObject.AddComponent<LineRenderer>();

            line.material = effectMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = LineWidth;
            line.endWidth = LineWidth * 0.65f;
            line.numCapVertices = 0;

            line.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.sortingOrder = 100;

            fractures.Add(line);
        }

        StartCoroutine(StressRoutine());
    }

    private static float Cross(
        Vector2 a,
        Vector2 b,
        Vector2 c
    )
    {
        return (b.x - a.x) * (c.y - a.y) -
               (b.y - a.y) * (c.x - a.x);
    }

    private IEnumerator StressRoutine()
    {
        float elapsed = 0f;

        while (elapsed < Duration && target != null)
        {
            float t =
                Mathf.Clamp01(elapsed / Duration);

            // First phase: energy arrives.
            float activation =
                Mathf.SmoothStep(
                    0f, 1f,
                    Mathf.Clamp01(t / 0.22f)
                );

            // Second phase: structural instability grows.
            float instability =
                Mathf.SmoothStep(
                    0f, 1f,
                    Mathf.Clamp01(
                        (t - 0.22f) / 0.38f
                    )
                );

            // Third phase: fade out.
            float fade =
                1f - Mathf.SmoothStep(
                    0f, 1f,
                    Mathf.Clamp01(
                        (t - 0.72f) / 0.28f
                    )
                );

            float flicker =
                0.82f +
                0.18f * Mathf.Sin(
                    elapsed * 37f
                );

            float alpha =
                activation * fade * flicker;

            Color color = new Color(
                1f,
                0.20f + 0.20f * flicker,
                0.08f,
                alpha
            );

            int edgeCount = corners.Count;

            for (int edge = 0; edge < edgeCount; edge++)
            {
                Vector3 start =
                    target.transform.TransformPoint(
                        corners[edge]
                    );

                Vector3 end =
                    target.transform.TransformPoint(
                        corners[
                            (edge + 1) % edgeCount
                        ]
                    );

                Vector3 direction = end - start;

                Vector3 outward =
                    Vector3.Cross(
                        Vector3.up,
                        direction.normalized
                    );

                // Small, changing displacements.
                float jitter =
                    instability * 0.035f *
                    Mathf.Sin(
                        elapsed * 29f + edge * 2.4f
                    );

                Vector3 displacement =
                    outward * jitter +
                    Vector3.up *
                    Mathf.Abs(jitter) * 0.5f;

                // Intentionally leave visible gaps.
                float gap =
                    Mathf.Lerp(
                        0.015f,
                        0.14f,
                        instability
                    );

                // Two separated fragments per edge.
                Vector3 a0 = Vector3.Lerp(
                    start, end, 0.03f
                );

                Vector3 a1 = Vector3.Lerp(
                    start, end, 0.50f - gap
                );

                Vector3 b0 = Vector3.Lerp(
                    start, end, 0.50f + gap
                );

                Vector3 b1 = Vector3.Lerp(
                    start, end, 0.97f
                );

                int index = edge * 2;

                LineRenderer first = fractures[index];
                LineRenderer second = fractures[index + 1];

                first.SetPosition(0, a0 + displacement);
                first.SetPosition(1, a1 + displacement);

                second.SetPosition(
                    0, b0 - displacement
                );

                second.SetPosition(
                    1, b1 - displacement
                );

                first.startColor = color;
                first.endColor = color;

                second.startColor = color;
                second.endColor = color;

                float widthPulse =
                    0.85f + instability * flicker * 0.35f;

                first.widthMultiplier = widthPulse;
                second.widthMultiplier = widthPulse;
            }

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (effectMaterial != null)
            Destroy(effectMaterial);
    }
}
