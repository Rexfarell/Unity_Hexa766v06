using UnityEngine;

public class ActiveUnitRing : MonoBehaviour
{
    [Header("RING")]
    [SerializeField]
    private float radius = 0.55f;

    [SerializeField]
    private float heightOffset = 0.03f;

    [SerializeField]
    private float lineWidth = 0.06f;

    [SerializeField]
    private int segments = 48;

    [Header("PULSE")]
    [SerializeField]
    private bool pulse = true;

    [SerializeField]
    private float pulseSpeed = 3.0f;

    [SerializeField]
    private float pulseAmount = 0.08f;

    private PlayerUnit unit;
    private TurnManager turnManager;
    private LineRenderer ring;

    private float baseRadius;

    private void Start()
    {
        unit =
            GetComponent<PlayerUnit>();

        turnManager =
            FindFirstObjectByType<TurnManager>();

        if (unit == null)
        {
            Debug.LogWarning(
                $"[ACTIVE RING] {name} has no PlayerUnit."
            );

            enabled = false;
            return;
        }

        baseRadius = radius;

        CreateRing();
    }

    private void CreateRing()
    {
        GameObject ringObject =
            new GameObject("ActiveUnitRing");

        ringObject.transform.SetParent(
            transform,
            false
        );

        ring =
            ringObject.AddComponent<LineRenderer>();

        ring.loop = true;

        ring.positionCount =
            segments;

        ring.useWorldSpace = true;

        ring.startWidth =
            lineWidth;

        ring.endWidth =
            lineWidth;

        ring.numCornerVertices = 4;

        ring.numCapVertices = 4;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            ring.material =
                new Material(shader);
        }

        Color teamColor =
            unit.teamID == 1
            ? new Color(0.2f, 0.65f, 1.0f, 0.9f)
            : new Color(1.0f, 0.25f, 0.25f, 0.9f);

        ring.startColor =
            teamColor;

        ring.endColor =
            teamColor;

        ring.enabled = false;
    }

    private void LateUpdate()
    {
        if (ring == null)
            return;

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        bool isActiveUnit =
            turnManager != null &&
            turnManager.current == unit;

        ring.enabled =
            isActiveUnit;

        if (!isActiveUnit)
            return;

        float currentRadius =
            baseRadius;

        if (pulse)
        {
            currentRadius +=
                Mathf.Sin(
                    Time.time * pulseSpeed
                ) * pulseAmount;
        }

        Vector3 center =
            transform.position +
            Vector3.up * heightOffset;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                ((float)i / segments) *
                Mathf.PI *
                2f;

            Vector3 point =
                new Vector3(
                    Mathf.Cos(angle) *
                    currentRadius,

                    0f,

                    Mathf.Sin(angle) *
                    currentRadius
                );

            ring.SetPosition(
                i,
                center + point
            );
        }
    }
}