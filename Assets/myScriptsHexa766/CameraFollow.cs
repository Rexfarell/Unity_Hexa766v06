using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("=== TARGET ===")]
    public Transform target;

    [Header("=== OFFSET (Relative to Player) ===")]
    public Vector3 offset = new Vector3(0, 5f, -8f);

    [Header("=== SMOOTHNESS ===")]
    [Range(0.01f, 1f)] public float smoothSpeed = 0.3f;

    [Header("=== DEBUG ===")]
    public bool showGizmos = true;

    void Start()
    {
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
    }

    void FollowTarget()
    {
        Vector3 desired = target.position + offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed);
        transform.position = smoothed;
        transform.LookAt(target);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
        Debug.Log($"[CameraFollow] Now following: {target.name} | Offset: {offset}");
    }

    void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }

    // VISUALIZE OFFSET IN SCENE VIEW
    void OnDrawGizmos()
    {
        if (!showGizmos || target == null) return;

        Vector3 desired = target.position + offset;

        // Player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, 0.5f);

        // Desired camera pos
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(desired, 0.4f);
        Gizmos.DrawLine(target.position, desired);

        // Label
#if UNITY_EDITOR
        UnityEditor.Handles.Label(desired + Vector3.up * 0.6f, $"Offset: {offset}");
#endif
    }

    // PRESS 'R' TO TEST OFFSET
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SnapToTarget();
            Debug.Log($"[CameraFollow] OFFSET RESET: {offset}");
        }
    }
}