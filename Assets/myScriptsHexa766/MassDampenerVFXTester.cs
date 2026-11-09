
using UnityEngine;

public class MassDampenerVFXTester : MonoBehaviour
{
    [Header("DEVELOPER TEST")]
    [SerializeField]
    private PlayerUnit[] testTargets;

    [SerializeField]
    private KeyCode testKey = KeyCode.F8;

    private void Update()
    {
        if (!Input.GetKeyDown(testKey))
            return;

        if (testTargets == null ||
            testTargets.Length == 0)
        {
            Debug.LogWarning(
                "[MASS DAMPENER TEST] No targets assigned."
            );

            return;
        }

        foreach (PlayerUnit target in testTargets)
        {
            if (target == null)
                continue;

            MassDampenerVFX.Play(target);
        }
    }
}
