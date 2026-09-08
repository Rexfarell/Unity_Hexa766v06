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

    [Header("Orbit")]
    public float rotationSpeed = 150f;

    [Header("Delivery Focus")]
    [Range(0.2f, 1f)]
    public float deliveryZoomMultiplier = 0.45f;

    public float deliveryExtraHeight = 0.35f;

    [Range(0.01f, 1f)]
    public float deliverySmoothSpeed = 0.25f;

    private Vector3 defaultOffset;
    private float zoomFactor = 1f;

    // Delivery camera state
    private bool deliveryMode = false;

    private Transform savedTarget;
    private Vector3 savedOffset;
    private float savedZoomFactor;

    // Delivery completion timer
    private PlayerUnit pendingDeliveryPlayer;
    private float pendingDeliveryFinishTime = -1f;


    void Awake()
    {
        defaultOffset = offset;
    }


    void Start()
    {
        SnapToTarget();
    }


    void Update()
    {
        // DELIVERY TIMER MUST RUN EVEN WHILE
        // deliveryMode blocks manual camera input.
        if (pendingDeliveryPlayer != null &&
            pendingDeliveryFinishTime > 0f &&
            Time.unscaledTime >= pendingDeliveryFinishTime)
        {
            PlayerUnit playerToFinish =
                pendingDeliveryPlayer;

            pendingDeliveryPlayer = null;
            pendingDeliveryFinishTime = -1f;

            Debug.Log(
                $"[CAMERA] Delivery timer completed for " +
                $"{playerToFinish.name}."
            );

            playerToFinish.CompleteAIStationDelivery();
        }

        // During delivery, prevent zoom/orbit/reset input.
        if (deliveryMode)
            return;

        HandleZoom();
        HandleRotation();

        if (Input.GetKeyDown(KeyCode.R))
        {
            zoomFactor = 1f;
            offset = defaultOffset;

            SnapToTarget();
        }
    }


    void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
    }


    void FollowTarget()
    {
        Vector3 desiredPosition =
            target.position + offset;

        float speed =
            deliveryMode
            ? deliverySmoothSpeed
            : smoothSpeed;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            speed
        );

        transform.LookAt(target.position);
    }


    void HandleZoom()
    {
        float wheel =
            Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(wheel) < 0.001f)
            return;

        zoomFactor -=
            wheel * zoomSpeed * zoomFactor;

        zoomFactor = Mathf.Clamp(
            zoomFactor,
            minZoom,
            maxZoom
        );

        Vector3 direction =
            offset.normalized;

        float currentDistance =
            defaultOffset.magnitude * zoomFactor;

        offset =
            direction * currentDistance;
    }


    void HandleRotation()
    {
        if (!Input.GetMouseButton(1))
            return;

        float delta =
            Input.GetAxis("Mouse X") *
            rotationSpeed *
            Time.deltaTime;

        offset =
            Quaternion.AngleAxis(
                delta,
                Vector3.up
            ) * offset;
    }


    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        SnapToTarget();
    }


    public void BeginDeliveryFocus(
        Transform focusTarget
    )
    {
        if (deliveryMode)
            return;

        deliveryMode = true;

        savedTarget = target;
        savedOffset = offset;
        savedZoomFactor = zoomFactor;

        target = focusTarget;

        // Preserve current camera angle,
        // but move closer.
        offset =
            savedOffset *
            deliveryZoomMultiplier;

        offset +=
            Vector3.up *
            deliveryExtraHeight;

        Debug.Log(
            $"[CAMERA] Delivery focus started on " +
            $"{focusTarget.name}."
        );
    }


    public void ScheduleDeliveryFinish(
    PlayerUnit player,
    float delay
)
    {
        pendingDeliveryPlayer = player;

        pendingDeliveryFinishTime =
            Time.unscaledTime + delay;

        Debug.Log(
            $"[CAMERA] Delivery completion scheduled for " +
            $"{player.name} in {delay} seconds. | " +
            $"enabled={enabled} | " +
            $"activeInHierarchy={gameObject.activeInHierarchy} | " +
            $"isActiveAndEnabled={isActiveAndEnabled}"
        );
    }


    public void EndDeliveryFocus()
    {
        if (!deliveryMode)
            return;

        target = savedTarget;
        offset = savedOffset;
        zoomFactor = savedZoomFactor;

        deliveryMode = false;

        SnapToTarget();

        Debug.Log(
            "[CAMERA] Delivery focus ended and " +
            "normal camera restored."
        );
    }


    void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position =
            target.position + offset;

        transform.LookAt(
            target.position
        );
    }
}