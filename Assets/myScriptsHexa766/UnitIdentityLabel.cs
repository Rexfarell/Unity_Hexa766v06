using UnityEngine;

public class UnitIdentityLabel : MonoBehaviour
{
    [Header("LABEL POSITION")]
    [SerializeField]
    private float height = 2.0f;

    [Header("LABEL SIZE")]
    [SerializeField]
    private float inactiveTextSize = 0.10f;

    [SerializeField]
    private float activeTextSize = 0.22f;

    [Header("LABEL OPACITY")]
    [SerializeField]
    private float inactiveOpacity = 0.55f;

    [SerializeField]
    private float activeOpacity = 1.0f;

    private TextMesh label;
    private Camera mainCamera;

    // These were missing.
    private PlayerUnit unit;
    private TurnManager turnManager;

    private void Start()
    {
        unit =
            GetComponent<PlayerUnit>();

        turnManager =
            FindFirstObjectByType<TurnManager>();

        if (unit == null)
        {
            Debug.LogWarning(
                $"[UNIT LABEL] {name} has no PlayerUnit."
            );

            return;
        }

        GameObject labelObject =
            new GameObject("UnitIdentityLabel");

        labelObject.transform.SetParent(
            transform,
            false
        );

        label =
            labelObject.AddComponent<TextMesh>();

        string teamPrefix =
            unit.teamID == 1
            ? "B"
            : "R";

        string binaryID =
            unit.unitID switch
            {
                1 => "00",
                2 => "01",
                3 => "10",
                4 => "11",
                _ => unit.unitID.ToString()
            };

        label.text =
            $"{teamPrefix} {binaryID}";

        label.fontSize = 64;

        label.characterSize =
            inactiveTextSize;

        label.anchor =
            TextAnchor.MiddleCenter;

        label.alignment =
            TextAlignment.Center;

        label.color =
            new Color(
                1f,
                1f,
                1f,
                inactiveOpacity
            );

        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (label == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        // Is this the unit whose turn is currently active?
        bool isActiveUnit =
            turnManager != null &&
            turnManager.current == unit;

        float targetSize =
            isActiveUnit
            ? activeTextSize
            : inactiveTextSize;

        float targetOpacity =
            isActiveUnit
            ? activeOpacity
            : inactiveOpacity;

        // Smooth size transition.
        label.characterSize =
            Mathf.Lerp(
                label.characterSize,
                targetSize,
                Time.deltaTime * 8f
            );

        // Smooth opacity transition.
        Color currentColor =
            label.color;

        currentColor.a =
            Mathf.Lerp(
                currentColor.a,
                targetOpacity,
                Time.deltaTime * 8f
            );

        label.color =
            currentColor;

        // Fixed WORLD-space height above the robot.
        label.transform.position =
            transform.position +
            Vector3.up * height;

        // Always face the camera.
        Vector3 direction =
            label.transform.position -
            mainCamera.transform.position;

        label.transform.rotation =
            Quaternion.LookRotation(direction);
    }
}