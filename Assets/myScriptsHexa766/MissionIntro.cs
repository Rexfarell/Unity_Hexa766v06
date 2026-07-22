using System.Collections;
using UnityEngine;

public class MissionIntro : MonoBehaviour
{
    [Header("Camera")]
    public CameraFollow cameraFollow;

    [Header("Targets")]
    public Transform overviewPoint;

    public Transform overviewTarget;

    public Transform pyramidPoint;
    public Transform aiStationPoint;
    public Transform player1Point;
    public Transform player2Point;

    public Transform pyramid;
    public Transform aiStation;
    public Transform player1;
    public Transform player2;

    [Header("Gameplay")]
    public TurnManager turnManager;

    [Header("Timing")]
    public float moveDuration = 2f;
    public float pauseDuration = 1.5f;

    void Start()
    {
        Debug.Log("===== MISSION INTRO STARTED =====");

        cameraFollow.enabled = false;

        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {


        yield return OrbitToTarget(
            overviewPoint,
            overviewTarget,
            6f,
            25f);

        yield return new WaitForSeconds(pauseDuration);

        yield return OrbitToTarget(
            pyramidPoint,//where the camera should end.
            pyramid,//what the camera should look at.
            4f,
            35f);

        yield return new WaitForSeconds(pauseDuration);

        yield return OrbitToTarget(
            aiStationPoint,
            aiStation,
            5f,
            25f);

        yield return new WaitForSeconds(pauseDuration);

        yield return OrbitToTarget(
            player1Point,
            player1,
            5f,
            12f);

        yield return new WaitForSeconds(0.5f);

        yield return OrbitToTarget(
            player2Point,
            player2,
            5f,
            12f);

        yield return new WaitForSeconds(0.5f);

        cameraFollow.SetTarget(player1);
        cameraFollow.enabled = true;

        turnManager.BeginMatch();

        Destroy(this);
    }

    IEnumerator OrbitToTarget(Transform viewPoint, Transform lookTarget, float duration, float orbitAngle = 35f)
    {
        Debug.Log($"[Orbit] ViewPoint = {viewPoint.name}");
        Debug.Log($"[Orbit] Position = {viewPoint.position}");
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        Vector3 center = lookTarget.position;

        // Final camera position
        Vector3 endPos = viewPoint.position;

        // Build an arc around the target
        Vector3 startDir = (startPos - center).normalized;
        Vector3 endDir = (endPos - center).normalized;

        float startRadius = Vector3.Distance(startPos, center);
        float endRadius = Vector3.Distance(endPos, center);

        float elapsed = 0f;

        Debug.Log($"Angle = {Vector3.Angle(startDir, endDir)}");
        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Smooth ease in/out
            float s = t * t * (3f - 2f * t);

            Quaternion rot =
                Quaternion.AngleAxis(
                    Mathf.Lerp(orbitAngle, 0f, s),
                    Vector3.up);

            Vector3 dir =
                Vector3.Slerp(
                    rot * startDir,
                    endDir,
                    s);

            float radius =
                Mathf.Lerp(startRadius, endRadius, s);

            Camera.main.transform.position =
                center + dir * radius;

            Camera.main.transform.LookAt(lookTarget.position);

            elapsed += Time.deltaTime;

            yield return null;
        }

        Camera.main.transform.position = endPos;
        Camera.main.transform.LookAt(lookTarget.position);
    }
}