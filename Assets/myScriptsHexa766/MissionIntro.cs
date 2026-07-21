using System.Collections;
using UnityEngine;

public class MissionIntro : MonoBehaviour
{
    [Header("Camera")]
    public CameraFollow cameraFollow;

    [Header("Targets")]
    public Transform overviewPoint;
    public Transform pyramid;
    public Transform aiStation;
    public Transform player1;

    [Header("Gameplay")]
    public TurnManager turnManager;

    [Header("Timing")]
    public float moveDuration = 2f;
    public float pauseDuration = 1.5f;

    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // Disable gameplay camera
        cameraFollow.enabled = false;

        yield return MoveCamera(overviewPoint);

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveCamera(pyramid);

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveCamera(aiStation);

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveCamera(player1);

        // Hand control back to the gameplay camera
        cameraFollow.enabled = true;
        cameraFollow.SetTarget(player1);

        // Start the match
        turnManager.BeginMatch();

        Destroy(this);
    }

    IEnumerator MoveCamera(Transform destination)
    {
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        Vector3 endPos = destination.position;
        Quaternion endRot =
            Quaternion.LookRotation(
                destination.forward,
                Vector3.up);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / moveDuration;

            Camera.main.transform.position =
                Vector3.Lerp(startPos, endPos, t);

            Camera.main.transform.rotation =
                Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }
    }
}