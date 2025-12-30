using UnityEngine;

public class TestClick : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("TEST CUBE CLICKED!");
        GetComponent<Renderer>().material.color = Color.red;
    }
}