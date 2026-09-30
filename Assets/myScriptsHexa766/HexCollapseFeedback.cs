using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCollapseFeedback : MonoBehaviour
{
    [Header("COLLAPSE GLITCH")]

    [SerializeField]
    private float duration = 4f;

    [SerializeField]
    private float refreshInterval = 0.045f;

    [SerializeField]
    private int crackCount = 7;

    [SerializeField]
    private int pointsPerCrack = 5;

    [SerializeField]
    private float lineWidth = 0.035f;

    [SerializeField]
    private float channelSeparation = 0.025f;

    [SerializeField]
    private float surfaceOffset = 0.04f;

    public float Duration => duration;


    private readonly List<GameObject> activeLines =
        new List<GameObject>();

    private Material glitchMaterial;

    private Coroutine playRoutine;

    private Vector3 surfaceCenter;
    private float effectRadius = 0.75f;

    private void Awake()
    {
        CacheHexGeometry();
        CreateMaterial();
    }


    private void CacheHexGeometry()
    {
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            surfaceCenter =
                transform.position +
                Vector3.up * surfaceOffset;

            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }

        surfaceCenter = new Vector3(
            bounds.center.x,
            bounds.max.y + surfaceOffset,
            bounds.center.z
        );

        effectRadius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z
            );

        effectRadius =
            Mathf.Max(
                effectRadius,
                0.3f
            );
    }



    private void CreateMaterial()
    {
        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader =
                Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            Debug.LogWarning(
                $"[HEX GLITCH] {name} could not find " +
                $"a line shader."
            );

            return;
        }

        glitchMaterial =
            new Material(shader);

        glitchMaterial.name =
            "Runtime_HexCollapseGlitch";
    }



    public void Play()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);

            ClearLines();
        }

        playRoutine =
            StartCoroutine(
                PlayRoutine()
            );
    }



    private IEnumerator PlayRoutine()
    {
        Debug.Log(
            $"[HEX GLITCH] {name} structural corruption START."
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            BuildGlitchFrame();

            yield return
                new WaitForSeconds(
                    refreshInterval
                );

            elapsed +=
                refreshInterval;
        }

        ClearLines();

        playRoutine = null;

        Debug.Log(
            $"[HEX GLITCH] {name} structural corruption END."
        );
    }



    private void BuildGlitchFrame()
    {
        ClearLines();

        for (int i = 0; i < crackCount; i++)
        {
            Vector3[] crack =
                CreateJaggedCrack();

            // White core
            CreateLine(
                crack,
                Color.white,
                Vector3.zero,
                lineWidth
            );

            // Cyan displaced channel
            CreateLine(
                crack,
                new Color(
                    0.1f,
                    1f,
                    1f,
                    0.85f
                ),
                new Vector3(
                    channelSeparation,
                    0f,
                    0f
                ),
                lineWidth * 0.8f
            );

            // Blue displaced channel
            CreateLine(
                crack,
                new Color(
                    0.15f,
                    0.4f,
                    1f,
                    0.8f
                ),
                new Vector3(
                    -channelSeparation,
                    0f,
                    channelSeparation
                ),
                lineWidth * 0.8f
            );
        }

        // A few short "data tear" streaks.
        int tearCount =
            Random.Range(2, 5);

        for (int i = 0; i < tearCount; i++)
        {
            CreateDataTear();
        }
    }



    private Vector3[] CreateJaggedCrack()
    {
        int count =
            Mathf.Max(
                3,
                pointsPerCrack
            );

        Vector3[] points =
            new Vector3[count];

        Vector2 direction2D =
            Random.insideUnitCircle;

        if (direction2D.sqrMagnitude < 0.01f)
        {
            direction2D =
                Vector2.right;
        }

        direction2D.Normalize();

        Vector2 perpendicular =
            new Vector2(
                -direction2D.y,
                direction2D.x
            );

        Vector2 startOffset =
            Random.insideUnitCircle *
            effectRadius *
            0.45f;

        float length =
            Random.Range(
                effectRadius * 0.45f,
                effectRadius * 1.15f
            );

        for (int i = 0; i < count; i++)
        {
            float t =
                (float)i /
                (count - 1);

            float forward =
                (t - 0.5f) *
                length;

            float jag =
                Random.Range(
                    -effectRadius * 0.10f,
                    effectRadius * 0.10f
                );

            Vector2 offset =
                startOffset +
                direction2D * forward +
                perpendicular * jag;

            points[i] =
                surfaceCenter +
                new Vector3(
                    offset.x,
                    Random.Range(
                        -0.015f,
                        0.015f
                    ),
                    offset.y
                );
        }

        return points;
    }



    private void CreateDataTear()
    {
        Vector2 centerOffset =
            Random.insideUnitCircle *
            effectRadius *
            0.65f;

        float length =
            Random.Range(
                effectRadius * 0.20f,
                effectRadius * 0.55f
            );

        Vector3 center =
            surfaceCenter +
            new Vector3(
                centerOffset.x,
                Random.Range(
                    0.01f,
                    0.05f
                ),
                centerOffset.y
            );

        Vector3 direction;

        if (Random.value > 0.5f)
        {
            direction =
                Vector3.right;
        }
        else
        {
            direction =
                Vector3.forward;
        }

        Vector3[] tear =
        {
            center -
            direction * length * 0.5f,

            center +
            direction * length * 0.5f
        };

        Color tearColor =
            Random.value > 0.5f
            ? Color.cyan
            : new Color(
                0.2f,
                0.45f,
                1f,
                1f
            );

        CreateLine(
            tear,
            tearColor,
            Vector3.zero,
            lineWidth *
            Random.Range(
                1.2f,
                2.2f
            )
        );
    }



    private void CreateLine(
        Vector3[] points,
        Color color,
        Vector3 offset,
        float width
    )
    {
        GameObject lineObject =
            new GameObject(
                "CollapseGlitchLine"
            );

        lineObject.transform.SetParent(
            transform,
            true
        );

        LineRenderer line =
            lineObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;

        line.positionCount =
            points.Length;

        line.startWidth = width;
        line.endWidth = width;

        line.startColor = color;
        line.endColor = color;

        line.numCapVertices = 0;
        line.numCornerVertices = 0;

        if (glitchMaterial != null)
        {
            line.sharedMaterial =
                glitchMaterial;
        }

        for (int i = 0; i < points.Length; i++)
        {
            line.SetPosition(
                i,
                points[i] + offset
            );
        }

        activeLines.Add(
            lineObject
        );
    }



    private void ClearLines()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i] != null)
            {
                Destroy(
                    activeLines[i]
                );
            }
        }

        activeLines.Clear();
    }



    private void OnDestroy()
    {
        ClearLines();

        if (glitchMaterial != null)
        {
            Destroy(
                glitchMaterial
            );
        }
    }
}