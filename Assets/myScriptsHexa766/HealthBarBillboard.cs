using UnityEngine;

public class HealthBarBillboard : MonoBehaviour
{
    Camera cam;

    Vector3 startPos;

    [Header("Hover Animation")]
    public float hoverHeight = 0.03f;
    public float hoverSpeed = 2f;

    void Start()
    {
        cam = Camera.main;
        startPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (cam == null)
            return;

        // Face the camera
        transform.LookAt(
            transform.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up
        );

        Vector3 p = startPos;

        // Gentle breathing
        p.y += Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;

        // Critical shake
        PlayerUnit player = GetComponentInParent<PlayerUnit>();

        if (player != null)
        {
            if (player.energy <= 25)
            {
                float intensity =
                    Mathf.Lerp(
                        0.02f,
                        0.08f,
                        1f - player.energy / 25f);

                p.x += Random.Range(-intensity, intensity);
                p.y += Random.Range(-intensity, intensity);
            }
        }

        transform.localPosition = p;
    }
}