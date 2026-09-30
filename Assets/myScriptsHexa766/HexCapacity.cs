using System.Collections;
using UnityEngine;
public enum HexLoadState
{
    Safe,
    Full,
    Overloaded,
    Collapsing,
    Collapsed
}
public class HexCapacity : MonoBehaviour
{
    [Header("HEX CAPACITY")]

    [Min(1)]
    [SerializeField]
    private int baseCapacity = 6;

    [SerializeField]
    private int capacityModifier = 0;


    public int BaseCapacity =>
        baseCapacity;

    public int CapacityModifier =>
        capacityModifier;

    public int EffectiveCapacity =>
        Mathf.Max(
            0,
            baseCapacity +
            capacityModifier
        );

    public bool ApplyCapacityModifier(int amount)
    {
        if (isCollapsed ||
            collapseSequenceRunning ||
            amount == 0)
        {
            return false;
        }

        int before =
            EffectiveCapacity;

        int target =
            Mathf.Max(
                0,
                before + amount
            );

        if (target == before)
            return false;

        capacityModifier =
            target - baseCapacity;

        Debug.Log(
            $"[SUBSYSTEM CAPACITY] {name} | " +
            $"{before} → {EffectiveCapacity} | " +
            $"Modifier={CapacityModifier} | " +
            $"Load={CurrentLoad}"
        );

        return true;
    }

    [ContextMenu("DEBUG — Reinforce Capacity +3")]
    private void DebugReinforceCapacity()
    {
        capacityModifier += 3;

        Debug.Log(
            $"[CAPACITY DEBUG] {name} | " +
            $"Base={BaseCapacity} | " +
            $"Modifier={CapacityModifier} | " +
            $"Effective={EffectiveCapacity} | " +
            $"CurrentLoad={CurrentLoad}"
        );
    }

    [ContextMenu("DEBUG — Stress Capacity -3")]
    private void DebugStressCapacity()
    {
        capacityModifier -= 3;

        Debug.Log(
            $"[CAPACITY DEBUG] {name} | " +
            $"Base={BaseCapacity} | " +
            $"Modifier={CapacityModifier} | " +
            $"Effective={EffectiveCapacity} | " +
            $"CurrentLoad={CurrentLoad}"
        );
    }

    [ContextMenu("DEBUG — Reset Capacity Modifier")]
    private void DebugResetCapacityModifier()
    {
        capacityModifier = 0;

        Debug.Log(
            $"[CAPACITY DEBUG] {name} | " +
            $"Base={BaseCapacity} | " +
            $"Modifier={CapacityModifier} | " +
            $"Effective={EffectiveCapacity} | " +
            $"CurrentLoad={CurrentLoad}"
        );
    }
    [Header("STRUCTURAL FAILURE")]

    [SerializeField]
    private bool isCollapsed = false;

    public bool IsCollapsed =>
        isCollapsed;

    private bool collapseSequenceRunning = false;

    public bool IsCollapseSequenceRunning =>
        collapseSequenceRunning;

    [Header("OVERLOAD TRACKING")]

    [SerializeField]
    private int consecutiveOverloadedRoundChecks = 0;

    public int ConsecutiveOverloadedRoundChecks =>
        consecutiveOverloadedRoundChecks;

    public bool EligibleForStructuralFailure =>
        consecutiveOverloadedRoundChecks >= 2;
    // =====================================================
    // CURRENT LOAD
    // =====================================================

    [ContextMenu("DEBUG — Evaluate Round End")]
    public void EvaluateRoundEnd()
    {
        if (isCollapsed)
        {
            return;
        }

        if (IsOverloaded)
        {
            consecutiveOverloadedRoundChecks++;

            if (EligibleForStructuralFailure)
            {
                TriggerStructuralFailure();
            }
        }
        else
        {
            consecutiveOverloadedRoundChecks = 0;
        }

        Debug.Log(
            $"[HEX OVERLOAD] {name} | " +
            $"Load={CurrentLoad} / " +
            $"Capacity={EffectiveCapacity} | " +
            $"Overloaded={IsOverloaded} | " +
            $"Checks={consecutiveOverloadedRoundChecks} | " +
            $"FailureEligible={EligibleForStructuralFailure} | " +
            $"State={LoadState}"
        );
    }
    public int CurrentLoad
    {
        get
        {
            return CalculateCurrentLoad();
        }
    }


    public bool IsOverloaded =>
        CurrentLoad >
        EffectiveCapacity;


    public int CalculateCurrentLoad()
    {
        Map1HexGrid map =
            GetComponentInParent<Map1HexGrid>();

        if (map == null)
        {
            return 0;
        }

        Vector2Int thisTileCoord =
            map.GetTileGridCoord(
                gameObject.name
            );

        int totalLoad = 0;

        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        foreach (PlayerUnit unit in units)
        {
            if (unit == null)
                continue;

            // Dead units contribute no load.
            if (unit.IsDead)
                continue;

            // Capacity uses the unit's LOGICAL tile,
            // not shared world-space vertex geometry.
            if (unit.currentTileCoord !=
                thisTileCoord)
            {
                continue;
            }

            totalLoad +=
                unit.EffectiveLoad;
        }

        return totalLoad;
    }

    public HexLoadState LoadState
    {
        get
        {
            if (isCollapsed)
                return HexLoadState.Collapsed;

            int load = CurrentLoad;
            int capacity = EffectiveCapacity;

            if (load > capacity)
            {
                if (EligibleForStructuralFailure)
                    return HexLoadState.Collapsing;

                return HexLoadState.Overloaded;
            }

            if (load == capacity)
                return HexLoadState.Full;

            return HexLoadState.Safe;
        }
    }
    // =====================================================
    // CAPACITY MODIFICATION
    // =====================================================

    public void SetBaseCapacity(
        int value
    )
    {
        baseCapacity =
            Mathf.Max(
                1,
                value
            );
    }


    public void SetCapacityModifier(
        int value
    )
    {
        capacityModifier =
            value;
    }


    public void AddCapacityModifier(
        int amount
    )
    {
        capacityModifier +=
            amount;
    }


    public void ResetCapacityModifier()
    {
        capacityModifier = 0;
    }

    [ContextMenu("DEBUG — Trigger Structural Failure")]
    public void TriggerStructuralFailure()
    {
        if (isCollapsed)
            return;

        if (collapseSequenceRunning)
            return;

        if (!EligibleForStructuralFailure ||
            !IsOverloaded)
        {
            Debug.Log(
                $"[HEX FAILURE] {name} is not eligible to collapse."
            );

            return;
        }

        StartCoroutine(
            StructuralFailureSequence()
        );
    }

    private IEnumerator StructuralFailureSequence()
    {
        collapseSequenceRunning = true;

        Debug.Log(
            $"[HEX FAILURE] {name} entering COLLAPSING sequence."
        );

        // ---------------------------------------------
        // CAMERA ATTENTION
        // ---------------------------------------------
        CameraFollow cameraFollow =
            FindFirstObjectByType<CameraFollow>();

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);

            Debug.Log(
                $"[HEX FAILURE CAMERA] Focusing {name}."
            );
        }

        // Give the player a moment to register
        // what the camera is showing.
        yield return new WaitForSeconds(0.35f);

        // ---------------------------------------------
        // GLITCH FEEDBACK
        // ---------------------------------------------
        HexCollapseFeedback feedback =
            GetComponent<HexCollapseFeedback>();

        if (feedback == null)
        {
            feedback =
                gameObject.AddComponent<HexCollapseFeedback>();
        }

        feedback.Play();

        // The hex remains in the Collapsing state
        // while the corruption develops.
        yield return new WaitForSeconds(
            feedback.Duration
        );

        // ---------------------------------------------
        // FIND LOGICAL HEX
        // ---------------------------------------------
        Map1HexGrid map =
            GetComponentInParent<Map1HexGrid>();

        if (map == null)
        {
            Debug.LogError(
                $"[HEX FAILURE] {name} cannot resolve Map1HexGrid."
            );

            collapseSequenceRunning = false;
            yield break;
        }

        Vector2Int thisTileCoord =
            map.GetTileGridCoord(name);

        int destroyedUnits = 0;

        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        // ---------------------------------------------
        // DESTROY OCCUPANTS
        // ---------------------------------------------
        foreach (PlayerUnit unit in units)
        {
            if (unit == null)
                continue;

            if (unit.IsDead)
                continue;

            if (unit.currentTileCoord != thisTileCoord)
                continue;

            Debug.Log(
                $"[HEX COLLAPSE] {name} destroys {unit.name} | " +
                $"Hex={thisTileCoord}"
            );

            unit.DieFromStructuralCollapse();

            destroyedUnits++;
        }

        // Structural failure is permanent.
        isCollapsed = true;

        // ---------------------------------------------
        // OBJECTIVE BOX ON COLLAPSED HEX
        // ---------------------------------------------
        if (BoxDeploymentManager.Instance != null)
        {
            BoxDeploymentManager.Instance.HandleCollapsedHex(
                thisTileCoord
            );
        }

        // Remove the hex visually from the battlefield.
        // Keep the GameObject alive so IsCollapsed
        // can still be queried by movement/pathfinding.
        HideCollapsedHex();

        Debug.Log(
            $"[HEX FAILURE] {name} has structurally failed. " +
            $"UnitsDestroyed={destroyedUnits} | " +
            $"State={LoadState}"
        );

        // Do NOT immediately leave the scene.
        // Let the player see the consequence.
        yield return new WaitForSeconds(1.0f);

        collapseSequenceRunning = false;
    }
    // =====================================================
    // TEMPORARY TEST
    // =====================================================

    [ContextMenu("DEBUG — Print Load")]
    private void DebugPrintLoad()
    {
        Map1HexGrid map =
            GetComponentInParent<Map1HexGrid>();

        if (map == null)
        {
            Debug.Log(
                $"[HEX LOAD DEBUG] {name} — MAP NULL"
            );

            return;
        }

        Vector2Int thisTileCoord =
            map.GetTileGridCoord(
                gameObject.name
            );

        Debug.Log(
            $"[HEX LOAD DEBUG] {name} " +
            $"coord={thisTileCoord} | " +
            $"Load={CurrentLoad} / " +
            $"Capacity={EffectiveCapacity} | " +
            $"State={LoadState}"
        );

        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        foreach (PlayerUnit unit in units)
        {
            if (unit == null)
                continue;

            Debug.Log(
                $"[HEX LOAD UNIT] {unit.name} | " +
                $"tileCoord={unit.currentTileCoord} | " +
                $"vertex={unit.currentVertexIndex} | " +
                $"load={unit.EffectiveLoad} | " +
                $"dead={unit.IsDead}"
            );
        }
    }

    private void HideCollapsedHex()
    {
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        Collider[] colliders =
            GetComponentsInChildren<Collider>(
                true
            );

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        Debug.Log(
            $"[HEX COLLAPSE] {name} removed from visible map."
        );
    }
}