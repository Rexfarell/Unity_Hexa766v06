
using UnityEngine;

public class StructuralReinforcementVFXTester : MonoBehaviour
{
    [Header("DEVELOPER TEST")]
    [SerializeField]
    private HexCapacity[] testTargets;

    [SerializeField]
    private KeyCode testKey = KeyCode.F9;

    private void Update()
    {
        if (!Input.GetKeyDown(testKey))
            return;

        if (testTargets == null ||
            testTargets.Length == 0)
        {
            Debug.LogWarning(
                "[REINFORCEMENT TEST] No targets assigned."
            );

            return;
        }

        foreach (HexCapacity target in testTargets)
        {
            if (target == null)
                continue;

            StructuralReinforcementVFX.Play(target);

            Debug.Log(
                $"[REINFORCEMENT TEST] Triggered on {target.name}"
            );
        }
    }
}
