using UnityEngine;

public class AIStationFeedback : MonoBehaviour
{
    [Header("DELIVERY PULSE")]
    [SerializeField]
    private GameObject deliveryPulse;

    [SerializeField]
    private float pulseDuration = 0.8f;

    [SerializeField]
    private float startScaleMultiplier = 0.35f;

    [SerializeField]
    private float endScaleMultiplier = 1.8f;

    private Vector3 normalPulseScale;
    private float pulseStartTime;
    private bool isPulsing = false;

    [Header("DELIVERY AUDIO")]
    [SerializeField]
    private AudioSource deliveryAudioSource;

    [SerializeField]
    private AudioClip deliveryClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float deliveryVolume = 1f;

    private void Start()
    {
        if (deliveryPulse == null)
            return;

        normalPulseScale =
            deliveryPulse.transform.localScale;

        deliveryPulse.SetActive(false);
    }

    private void Update()
    {
        if (!isPulsing || deliveryPulse == null)
            return;

        float elapsed =
            Time.unscaledTime - pulseStartTime;

        float t =
            Mathf.Clamp01(elapsed / pulseDuration);

        float multiplier =
            Mathf.Lerp(
                startScaleMultiplier,
                endScaleMultiplier,
                t
            );

        Vector3 scale =
            normalPulseScale * multiplier;

        // Keep the cylinder thin vertically.
        scale.y = normalPulseScale.y;

        deliveryPulse.transform.localScale = scale;

        if (t >= 1f)
        {
            deliveryPulse.SetActive(false);

            deliveryPulse.transform.localScale =
                normalPulseScale;

            isPulsing = false;
        }
    }

    public void PlayDeliveryFlash()
    {
        if (deliveryPulse == null)
        {
            Debug.LogError(
                $"[AI STATION FEEDBACK] Object={gameObject.name} | DeliveryPulse is NULL."
            );

            return;
        }

        pulseStartTime = Time.unscaledTime;

        deliveryPulse.transform.localScale =
            new Vector3(
                normalPulseScale.x * startScaleMultiplier,
                normalPulseScale.y,
                normalPulseScale.z * startScaleMultiplier
            );

        deliveryPulse.SetActive(true);

        isPulsing = true;

        // Delivery sound
        if (deliveryAudioSource != null &&
            deliveryClip != null)
        {
            deliveryAudioSource.PlayOneShot(
                deliveryClip,
                deliveryVolume
            );
        }
        else
        {
            Debug.LogWarning(
                "[AI STATION FEEDBACK] Delivery audio not assigned."
            );
        }

        Debug.Log(
            "[AI STATION FEEDBACK] Delivery pulse + audio started."
        );
    }
}