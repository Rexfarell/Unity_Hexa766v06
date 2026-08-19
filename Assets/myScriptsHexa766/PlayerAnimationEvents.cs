using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerUnit playerUnit;

    private void Awake()
    {
        playerUnit = GetComponentInParent<PlayerUnit>();
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