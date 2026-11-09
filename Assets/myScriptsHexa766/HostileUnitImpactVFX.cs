
using System.Collections;
using UnityEngine;

public class HostileUnitImpactVFX : MonoBehaviour
{
    private const float Duration = 2.3f;
    private const int BeamCount = 3;

    private Transform target;
    private Material effectMaterial;

    private readonly LineRenderer[] beams =
        new LineRenderer[BeamCount];

    private LineRenderer pressureLine;

    private readonly float[] beamOffsets =
    {
        -0.75f, -0.38f, 0f, 0.38f, 0.75f
    };

    public static void Play(PlayerUnit unit)
    {
        if (unit == null || unit.IsDead)
            return;

        GameObject effect = new GameObject(
            "HOSTILE_GRAVITY_COMPRESSION_VFX"
        );

        HostileUnitImpactVFX vfx =
            effect.AddComponent<HostileUnitImpactVFX>();

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
                "[GRAVITY VFX] Shader unavailable."
            );

            Destroy(gameObject);
            return;
        }

        effectMaterial = new Material(shader);

        for (int i = 0; i < BeamCount; i++)
        {
            beams[i] = CreateLine(
                "GravityBeam_" + i,
                2,
                0.045f
            );
        }

        pressureLine = CreateLine(
            "GravityPressure",
            2,
            0.085f
        );

        StartCoroutine(GravityRoutine());
    }

    private LineRenderer CreateLine(
        string objectName,
        int points,
        float width
    )
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(transform, false);

        LineRenderer line =
            obj.AddComponent<LineRenderer>();

        line.material = effectMaterial;
        line.useWorldSpace = true;
        line.positionCount = points;
        // Gravitational pressure streams:
        // diffuse at the top, concentrated at the bottom.
        line.startWidth = width * 3.5f;
        line.endWidth = width * 0.35f;
        line.numCapVertices = 0;

        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        line.receiveShadows = false;

        line.sortingOrder = 100;

        return line;
    }

    private IEnumerator GravityRoutine()
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
                bounds.size.y,
                0.6f
            );

            float radius = Mathf.Clamp(
                Mathf.Max(
                    bounds.extents.x,
                    bounds.extents.z
                ),
                0.3f,
                0.85f
            );

            float groundY = bounds.min.y + 0.06f;

            Camera cam = Camera.main;

            Vector3 cameraRight =
                cam != null
                    ? cam.transform.right
                    : Vector3.right;

            cameraRight.y = 0f;

            if (cameraRight.sqrMagnitude < 0.001f)
                cameraRight = Vector3.right;

            cameraRight.Normalize();

            // -----------------------------------------
            // PHASE 1: DOWNWARD GRAVITY STREAMS
            // -----------------------------------------

            // GRAVITATIONAL DECELERATION
            // Fast initial descent, increasingly heavy slowdown,
            // then a complete stop against the target.

            float descentTime =
                Mathf.Clamp01(t / 0.78f);

            float fallProgress =
                1f - Mathf.Pow(
                    1f - descentTime,
                    4f
                );

            float intensity =
                t < 0.40f
                    ? Mathf.SmoothStep(0f, 1f, t / 0.40f)
                    : 1f - Mathf.SmoothStep(
                        0f, 1f,
                        (t - 0.40f) / 0.60f
                    );

            for (int i = 0; i < BeamCount; i++)
            {
                float lateral =
                    beamOffsets[i] * radius;

                Vector3 column =
                    center + cameraRight * lateral;

                float stagger =
                    i * 0.075f;

                float progress =
                    Mathf.Clamp01(
                        (fallProgress - stagger) /
                        (1f - stagger)
                    );

                float startingHeight =
                    groundY + height * 3.5f;

                float endingHeight =
                    groundY + height * 0.18f;

                float beamTipY =
                    Mathf.Lerp(
                        startingHeight,
                        endingHeight,
                        progress
                    );

                float length =
                    height * 1.8f;

                Vector3 tip = new Vector3(
                    column.x,
                    beamTipY,
                    column.z
                );

                Vector3 top =
                    tip + Vector3.up * length;

                beams[i].SetPosition(0, top);
                beams[i].SetPosition(1, tip);

                Color beamColor = new Color(
                    0.75f,
                    0.25f,
                    0.45f,
                    Mathf.Clamp01(intensity) * 0.45f
                );

                beams[i].startColor = beamColor;
                beams[i].endColor = beamColor;

                beams[i].widthMultiplier =
                    0.85f;
            }

            // -----------------------------------------
            // PHASE 2: PRESSURE AT THE ROBOT'S FEET
            // -----------------------------------------

            float impactProgress =
                Mathf.Clamp01(
                    (t - 0.30f) / 0.35f
                );

            float pressureIntensity =
                Mathf.Sin(
                    impactProgress * Mathf.PI
                );

            Vector3 groundCenter = new Vector3(
                center.x,
                groundY,
                center.z
            );

            float pressureWidth =
                Mathf.Lerp(
                    radius * 0.4f,
                    radius * 1.35f,
                    impactProgress
                );

            pressureLine.SetPosition(
                0,
                groundCenter -
                cameraRight * pressureWidth
            );

            pressureLine.SetPosition(
                1,
                groundCenter +
                cameraRight * pressureWidth
            );

            Color pressureColor = new Color(
                1f,
                0.28f,
                0.12f,
                pressureIntensity * 0.85f
            );

            pressureLine.startColor = pressureColor;
            pressureLine.endColor = pressureColor;

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        Destroy(gameObject);
    }

    private Bounds GetTargetBounds()
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        bool found = false;

        Bounds result = new Bounds(
            target.position + Vector3.up * 0.5f,
            new Vector3(0.7f, 1.2f, 0.7f)
        );

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
