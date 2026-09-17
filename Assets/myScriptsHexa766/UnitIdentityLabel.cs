using UnityEngine;

public class UnitIdentityLabel : MonoBehaviour
{
    [SerializeField]
    private float height = 1.4f;

    [SerializeField]
    private float textSize = 0.12f;

    private TextMesh label;
    private Camera mainCamera;

    private void Start()
    {
        PlayerUnit unit =
            GetComponent<PlayerUnit>();

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
            textSize;

        label.anchor =
            TextAnchor.MiddleCenter;

        label.alignment =
            TextAlignment.Center;

        label.color = Color.white;

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

        // Keep the label a fixed WORLD distance
        // above the robot.
        label.transform.position =
            transform.position +
            Vector3.up * height;

        Vector3 direction =
            label.transform.position -
            mainCamera.transform.position;

        label.transform.rotation =
            Quaternion.LookRotation(direction);
    }
}