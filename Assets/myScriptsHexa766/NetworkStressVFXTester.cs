
using UnityEngine;

public class NetworkStressVFXTester : MonoBehaviour
{
    [Header("DEVELOPER TEST")]
    [SerializeField]
    private HexCapacity[] testTargets;

    [SerializeField]
    private KeyCode testKey = KeyCode.F10;

    private void Update()
    {
        if (!Input.GetKeyDown(testKey))
            return;

        if (testTargets == null ||
            testTargets.Length == 0)
        {
            Debug.LogWarning(
                "[NETWORK STRESS TEST] No targets assigned."
            );

            return;
        }

        foreach (HexCapacity target in testTargets)
        {
            if (target == null)
                continue;

            NetworkStressVFX.Play(target);

            Debug.Log(
                $"[NETWORK STRESS TEST] Triggered on {target.name}"
            );
        }
    }
}
