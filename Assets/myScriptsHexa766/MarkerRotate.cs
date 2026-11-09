using UnityEngine;

public class MarkerRotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.World
        );
    }
}