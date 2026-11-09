
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructuralReinforcementVFX : MonoBehaviour
{
    private const float Duration = 2.3f;
    private const float LineWidth = 0.065f;

    private HexCapacity target;
    private Material effectMaterial;

    private readonly List<LineRenderer> segments =
        new List<LineRenderer>();

    private readonly List<Vector3> corners =
        new List<Vector3>();

    public static void Play(HexCapacity hex)
    {
        if (hex == null || hex.IsCollapsed)
            return;

        GameObject obj = new GameObject(
            "STRUCTURAL_REINFORCEMENT_VFX"
        );

        var effect =
            obj.AddComponent<StructuralReinforcementVFX>();

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
                "[REINFORCEMENT VFX] Hex mesh unavailable."
            );

            Destroy(gameObject);
            return;
        }

        Mesh mesh = filter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Collect local XZ coordinates.
        List<Vector2> points = new List<Vector2>();

        foreach (Vector3 vertex in vertices)
        {
            Vector2 point =
                new Vector2(vertex.x, vertex.z);

            if (!points.Exists(p =>
                Vector2.Distance(p, point) < 0.001f))
            {
                points.Add(point);
            }
        }

        // Convex hull of the hex footprint.
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

        float topY = mesh.bounds.max.y + 0.045f;

        foreach (Vector2 p in hull)
        {
            corners.Add(new Vector3(
                p.x, topY, p.y
            ));
        }

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Destroy(gameObject);
            return;
        }

        effectMaterial = new Material(shader);

        for (int i = 0; i < corners.Count; i++)
        {
            GameObject lineObj =
                new GameObject("ReinforcementSegment_" + i);

            lineObj.transform.SetParent(transform, false);

            LineRenderer line =
                lineObj.AddComponent<LineRenderer>();

            line.material = effectMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = LineWidth;
            line.endWidth = LineWidth;
            line.numCapVertices = 2;
            line.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = 100;

            segments.Add(line);
        }

        StartCoroutine(ReinforcementRoutine());
    }

    private static float Cross(
        Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) -
               (b.y - a.y) * (c.x - a.x);
    }

    private IEnumerator ReinforcementRoutine()
    {
        float elapsed = 0f;

        while (elapsed < Duration && target != null)
        {
            float t = Mathf.Clamp01(elapsed / Duration);

            // Segments assemble during the first 65%.
            float assembly =
                Mathf.SmoothStep(
                    0f, 1f,
                    Mathf.Clamp01(t / 0.65f)
                );

            // Brief completed structure, then fade.
            float alpha =
                t < 0.70f
                    ? Mathf.Lerp(0.30f, 0.95f, assembly)
                    : 0.95f * (1f -
                        Mathf.SmoothStep(
                            0f, 1f,
                            (t - 0.70f) / 0.30f
                        ));

            Color color = new Color(
                0.30f, 1f, 0.78f, alpha
            );

            for (int i = 0; i < segments.Count; i++)
            {
                Vector3 a = target.transform.TransformPoint(
                    corners[i]
                );

                Vector3 b = target.transform.TransformPoint(
                    corners[(i + 1) % corners.Count]
                );

                Vector3 middle = (a + b) * 0.5f;

                // Assemble outward from each edge midpoint.
                Vector3 start =
                    Vector3.Lerp(middle, a, assembly);

                Vector3 end =
                    Vector3.Lerp(middle, b, assembly);

                // Descend into the hex structure.
                float lift =
                    (1f - assembly) * 0.35f;

                start += Vector3.up * lift;
                end += Vector3.up * lift;

                segments[i].SetPosition(0, start);
                segments[i].SetPosition(1, end);

                segments[i].startColor = color;
                segments[i].endColor = color;
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
