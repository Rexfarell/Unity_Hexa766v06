using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Default Offset")]
    public Vector3 offset = new Vector3(3f, 2f, 4f);

    [Header("Follow")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;

    [Header("Zoom")]
    public float zoomSpeed = 0.5f;
    public float minZoom = 0.20f;
    public float maxZoom = 3.5f;

    private Vector3 defaultOffset;
    private float zoomFactor = 1f;

    void Awake()
    {
        defaultOffset = offset;
    }

    void Start()
    {
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
    }

    void Update()
    {
        HandleZoom();

        if (Input.GetKeyDown(KeyCode.R))
        {
            zoomFactor = 1f;
            offset = defaultOffset;
            SnapToTarget();
        }
    }

    void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed);

        transform.LookAt(target.position);
    }

    void HandleZoom()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(wheel) < 0.001f)
            return;

        zoomFactor -= wheel * zoomSpeed * zoomFactor;

        zoomFactor = Mathf.Clamp(
            zoomFactor,
            minZoom,
            maxZoom);

        offset = defaultOffset * zoomFactor;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}