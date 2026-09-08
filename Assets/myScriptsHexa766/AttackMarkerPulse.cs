using UnityEngine;

public class AttackMarkerPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.25f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float pulse =
            1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        transform.localScale = baseScale * pulse;
    }
}