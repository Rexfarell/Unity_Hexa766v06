using UnityEngine;

public class HealthBarPulse : MonoBehaviour
{
    [Header("Glow Animation")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.20f;

    Renderer rend;
    Material mat;

    Color baseEmission;

    void Start()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            // Creates an instance so each bar pulses independently
            mat = rend.material;

            if (mat.HasProperty("_EmissionColor"))
                baseEmission = mat.GetColor("_EmissionColor");
        }
    }

    void Update()
    {
        if (mat == null)
            return;

        float speed = pulseSpeed;
        float amount = pulseAmount;

        PlayerUnit player = GetComponentInParent<PlayerUnit>();

        if (player != null)
        {
            int value = gameObject.name.Contains("Shield")
                ? player.shield
                : player.energy;

            if (value <= 25)
            {
                speed = pulseSpeed * 3.5f;
                amount = pulseAmount * 2.5f;
            }
            else if (value <= 50)
            {
                speed = pulseSpeed * 2f;
                amount = pulseAmount * 1.6f;
            }
            else if (value <= 75)
            {
                speed = pulseSpeed * 1.3f;
                amount = pulseAmount * 1.2f;
            }
        }

        float pulse =
            1f +
            Mathf.Sin(Time.time * speed + GetInstanceID()) * amount;

        mat.SetColor("_EmissionColor", baseEmission * pulse);
    }
}