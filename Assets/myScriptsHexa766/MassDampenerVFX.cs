
using System.Collections;
using UnityEngine;

public class MassDampenerVFX : MonoBehaviour
{
    private const float Duration = 2.3f;
    private const int BeamCount = 3;

    private Transform target;
    private Material effectMaterial;

    private readonly LineRenderer[] beams =
        new LineRenderer[BeamCount];

    private readonly float[] beamOffsets =
    {
        -0.65f, 0f, 0.65f
    };

    public static void Play(PlayerUnit unit)
    {
        if (unit == null || unit.IsDead)
            return;

        GameObject effect = new GameObject(
            "MASS_DAMPENER_GRAVITY_RELEASE_VFX"
        );

        MassDampenerVFX vfx =
            effect.AddComponent<MassDampenerVFX>();

        vfx.Initialize(unit.transform);
    }

    private void Initialize(Transform unit)
    {
        target = unit;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning(
                "[MASS DAMPENER VFX] Shader unavailable."
            );

            Destroy(gameObject);
            return;
        }

        effectMaterial = new Material(shader);

        for (int i = 0; i < BeamCount; i++)
        {
            GameObject beamObject =
                new GameObject("MassReleaseBeam_" + i);

            beamObject.transform.SetParent(
                transform, false
            );

            LineRenderer line =
                beamObject.AddComponent<LineRenderer>();

            line.material = effectMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.045f * 3.5f;
            line.endWidth = 0.045f * 0.35f;
            line.widthMultiplier = 0.85f;
            line.numCapVertices = 0;

            line.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.sortingOrder = 100;

            beams[i] = line;
        }

        StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        float elapsed = 0f;

        while (elapsed < Duration && target != null)
        {
            float t = Mathf.Clamp01(
                elapsed / Duration
            );

            Bounds bounds = GetTargetBounds();

            Vector3 center = bounds.center;

            float height = Mathf.Max(
                bounds.size.y, 0.6f
            );

            float radius = Mathf.Clamp(
                Mathf.Max(
                    bounds.extents.x,
                    bounds.extents.z
                ),
                0.3f, 0.85f
            );

            float groundY =
                bounds.min.y + 0.06f;

            Camera cam = Camera.main;

            Vector3 right =
                cam != null
                    ? cam.transform.right
                    : Vector3.right;

            right.y = 0f;

            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;

            right.Normalize();

            // Slow initial rise, accelerating upward.
            float riseTime =
                Mathf.Clamp01(t / 0.85f);

            float riseProgress =
                Mathf.Pow(riseTime, 3f);

            for (int i = 0; i < BeamCount; i++)
            {
                float stagger = i * 0.075f;

                float progress =
                    Mathf.Clamp01(
                        (riseProgress - stagger) /
                        (1f - stagger)
                    );

                float lateral =
                    beamOffsets[i] * radius;

                Vector3 column =
                    center + right * lateral;

                float startY =
                    groundY + height * 0.15f;

                float endY =
                    groundY + height * 3.5f;

                float beamBaseY =
                    Mathf.Lerp(
                        startY,
                        endY,
                        progress
                    );

                float beamLength =
                    height * 1.8f;

                Vector3 bottom =
                    new Vector3(
                        column.x,
                        beamBaseY,
                        column.z
                    );

                Vector3 top =
                    bottom + Vector3.up * beamLength;

                beams[i].SetPosition(0, bottom);
                beams[i].SetPosition(1, top);

                float intensity =
                    Mathf.Sin(t * Mathf.PI);

                Color color = new Color(
                    0.20f,
                    0.85f,
                    1f,
                    intensity * 0.45f
                );

                beams[i].startColor = color;
                beams[i].endColor = color;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private Bounds GetTargetBounds()
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        Bounds result = new Bounds(
            target.position + Vector3.up * 0.5f,
            new Vector3(0.7f, 1.2f, 0.7f)
        );

        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                renderer is ParticleSystemRenderer ||
                renderer is LineRenderer)
            {
                continue;
            }

            if (!found)
            {
                result = renderer.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        return result;
    }

    private void OnDestroy()
    {
        if (effectMaterial != null)
            Destroy(effectMaterial);
    }
}
