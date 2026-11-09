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

    // Tactical Scanner camera composition
    private bool tacticalScannerComposition = false;

    [Range(0f, 0.30f)]
    [SerializeField]
    private float tacticalScannerScreenShift = 0.16f;

    private Camera cameraComponent;

    [Header("TEMPORARY EVENT FOCUS")]
    [SerializeField]
    private float eventFocusZoomMultiplier = 0.65f;

    [SerializeField]
    private float eventFocusSmoothSpeed = 0.35f;

    private bool eventFocusMode = false;
    private Vector3 eventFocusPosition;
    private float eventFocusEndTime = -1f;
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

        cameraComponent =
            GetComponent<Camera>();
    }


    void Start()
    {
        SnapToTarget();
    }


    void Update()
    {
        // =====================================================
        // DELIVERY TIMER
        // =====================================================
        // Must continue running even while deliveryMode
        // blocks normal camera controls.
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


        // =====================================================
        // TEMPORARY EVENT FOCUS TIMER
        // =====================================================
        // Used by effects such as Team Regroup.
        if (eventFocusMode &&
            Time.unscaledTime >= eventFocusEndTime)
        {
            EndTemporaryFocus();
        }


        // =====================================================
        // BLOCK MANUAL CAMERA INPUT DURING SPECIAL FOCUS
        // =====================================================
        if (deliveryMode ||
            eventFocusMode)
        {
            return;
        }


        // =====================================================
        // NORMAL CAMERA CONTROLS
        // =====================================================
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
        Vector3 focusPosition =
            eventFocusMode
            ? eventFocusPosition
            : target.position;

        Vector3 activeOffset =
            eventFocusMode
            ? offset * eventFocusZoomMultiplier
            : offset;

        Vector3 desiredPosition =
            focusPosition + activeOffset;

        float speed;

        if (deliveryMode)
        {
            speed =
                deliverySmoothSpeed;
        }
        else if (eventFocusMode)
        {
            speed =
                eventFocusSmoothSpeed;
        }
        else
        {
            speed =
                smoothSpeed;
        }

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                speed
            );

        transform.LookAt(
            GetCompositionLookPoint()
    );
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


    public void SetTarget(
    Transform newTarget
)
    {
        target =
            newTarget;

        // During a temporary event focus,
        // remember the new normal target,
        // but do not move the camera away yet.
        if (eventFocusMode)
            return;

        SnapToTarget();
    }

    public void BeginTemporaryFocus(
    Vector3 worldPosition,
    float duration
)
    {
        if (deliveryMode)
            return;

        eventFocusPosition =
            worldPosition;

        eventFocusEndTime =
            Time.unscaledTime +
            Mathf.Max(
                0.1f,
                duration
            );

        eventFocusMode =
            true;

        Debug.Log(
            $"[CAMERA] Temporary event focus started at " +
            $"{worldPosition}"
        );
    }


    private void EndTemporaryFocus()
    {
        if (!eventFocusMode)
            return;

        eventFocusMode =
            false;

        eventFocusEndTime =
            -1f;

        Debug.Log(
            "[CAMERA] Temporary event focus ended."
        );
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


    public void SetTacticalScannerComposition(
    bool enabled
)
    {
        tacticalScannerComposition =
            enabled;
    }

    

    private Vector3 GetCompositionLookPoint()
    {
        if (target == null)
            return transform.position + transform.forward;


        // Delivery cinematics keep their normal framing.
        if (!tacticalScannerComposition ||
            deliveryMode)
        {
            return target.position;
        }


        if (cameraComponent == null)
        {
            cameraComponent =
                GetComponent<Camera>();
        }


        if (cameraComponent == null)
            return target.position;


        Vector3 viewDirection =
            target.position -
            transform.position;


        if (viewDirection.sqrMagnitude <
            0.0001f)
        {
            return target.position;
        }


        viewDirection.Normalize();


        // Horizontal camera-right direction.
        Vector3 cameraRight =
            Vector3.Cross(
                Vector3.up,
                viewDirection
            ).normalized;


        if (cameraRight.sqrMagnitude <
            0.0001f)
        {
            cameraRight =
                transform.right;
        }


        float distance =
            offset.magnitude;


        float halfVerticalView =
            Mathf.Tan(
                cameraComponent.fieldOfView *
                0.5f *
                Mathf.Deg2Rad
            ) *
            distance;


        float halfHorizontalView =
            halfVerticalView *
            cameraComponent.aspect;


        // Convert the desired screen-space displacement
        // into a world-space look offset.
        float worldShift =
            halfHorizontalView *
            tacticalScannerScreenShift *
            2f;


        // Look slightly RIGHT of the actual target.
        // The real target therefore appears LEFT on screen,
        // away from the Tactical Scanner.
        return
            target.position +
            cameraRight *
            worldShift;
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