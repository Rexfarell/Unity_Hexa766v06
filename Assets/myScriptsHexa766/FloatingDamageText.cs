using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private float riseSpeed = 0.08f;

    private float timer;

    public void Show(string message)
    {
        if (textMesh != null)
            textMesh.text = message;

        timer = lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
            Destroy(gameObject);
    }
}