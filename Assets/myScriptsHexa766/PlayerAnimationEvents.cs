using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerUnit playerUnit;

    private void Awake()
    {
        playerUnit = GetComponentInParent<PlayerUnit>();
    }

    public void AttackImpact()
    {
        if (playerUnit == null)
        {
            Debug.LogWarning(
                "[ATTACK IMPACT] PlayerUnit reference is missing."
            );
            return;
        }

        playerUnit.AttackImpact();
    }

    public void FinishAttack()
    {
        if (playerUnit != null)
        {
            playerUnit.FinishAttack();
        }
        else
        {
            Debug.LogError(
                $"[ATTACK EVENT] Could not find PlayerUnit in parents of {name}."
            );
        }
    }
}