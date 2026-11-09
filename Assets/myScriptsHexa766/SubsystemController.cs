using UnityEngine;

public class SubsystemController : MonoBehaviour
{
    private Camera gameplayCamera;
    private EnemyDroneDirector enemyDroneDirector;

    private TopologyScanner topologyScanner;

    private TopologyScanHUD topologyScanHUD;

    private PlayerUnit lastTurnUnit;
    private bool subsystemUsedThisTurn = false;

    private SubsystemController subsystemController;

    public bool SubsystemUsedThisTurn =>
    subsystemUsedThisTurn;

    private void Start()
    {
        gameplayCamera = Camera.main;

        if (gameplayCamera == null)
        {
            gameplayCamera =
                FindFirstObjectByType<Camera>();
        }

        enemyDroneDirector =
            FindFirstObjectByType<EnemyDroneDirector>();

        topologyScanner =
    GetComponent<TopologyScanner>();

        if (topologyScanner == null)
        {
            topologyScanner =
                FindFirstObjectByType<TopologyScanner>();
        }

        topologyScanHUD =
    GetComponent<TopologyScanHUD>();

        if (topologyScanHUD == null)
        {
            topologyScanHUD =
                FindFirstObjectByType<TopologyScanHUD>();
        }
    }



    private void Update()
    {
        TurnManager turnManager =
            TurnManager.Instance;

        if (turnManager == null)
            return;

        PlayerUnit activeUnit =
            turnManager.current;

        if (activeUnit == null)
            return;


        // ---------------------------------------------
        // NEW PLAYER TURN
        // ---------------------------------------------
        if (activeUnit != lastTurnUnit)
        {
            lastTurnUnit =
                activeUnit;

            subsystemUsedThisTurn =
                false;

            Debug.Log(
                $"[SUBSYSTEM] Ready for {activeUnit.name}."
            );
        }


        // No player Subsystems during Enemy Phase.
        if (enemyDroneDirector != null &&
            enemyDroneDirector.IsRunning)
        {
            return;
        }

        if (activeUnit.IsDead)
            return;

        if (activeUnit.IsMoving)
            return;


        // ---------------------------------------------
        // 5 — TOPOLOGY SCAN
        // Free analysis interface.
        // Does NOT consume the intervention opportunity.
        // ---------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TryTopologyScan(
                activeUnit
            );

            return;
        }


        // One successful INTERVENTION per unit turn.
        // Tactical Scanner remains available above.
        if (subsystemUsedThisTurn)
            return;


        // ---------------------------------------------
        // 1 — MASS DAMPENER
        // Allied unit Effective Load -1
        // ---------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryMassDampener(
                activeUnit
            );

            return;
        }


        // ---------------------------------------------
        // 2 — BALLAST INJECTION
        // Enemy unit Effective Load +1
        // ---------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryBallastInjection(
                activeUnit
            );

            return;
        }


        // ---------------------------------------------
        // 3 — STRUCTURAL REINFORCEMENT
        // Target hex Capacity +3
        // ---------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TryStructuralReinforcement(
                activeUnit
            );

            return;
        }


        // ---------------------------------------------
        // 4 — NETWORK STRESS
        // Target hex Capacity -3
        // ---------------------------------------------
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TryNetworkStress(
                activeUnit
            );

            return;
        }
    }


    // =================================================
    // MASS DAMPENER
    // =================================================

    private void TryMassDampener(
    PlayerUnit activeUnit
)
    {
        PlayerUnit target =
            GetHoveredPlayerUnit(
                activeUnit.teamID,
                true
            );

        if (target == null)
        {
            // Check whether the cursor is actually over an allied
            // robot that has already reached minimum load.
            PlayerUnit blockedTarget =
                GetHoveredPlayerUnit(
                    activeUnit.teamID,
                    false
                );

            if (blockedTarget != null &&
                blockedTarget.EffectiveLoad <= 1)
            {
                Debug.Log(
                    $"[SUBSYSTEM] MASS DAMPENER unavailable — " +
                    $"{blockedTarget.name} is already at minimum Effective Load."
                );

                if (SubsystemFeedbackUI.Instance != null)
                {
                    SubsystemFeedbackUI.Instance.ShowMessage(
                        "<b><size=110%>HEXA_766 // MASS DAMPENER</size></b>\n" +
                        "<b>NO EFFECT</b>\n" +
                        "MINIMUM LOAD REACHED  //  EFFECTIVE LOAD 1"
                    );
                }

                return;
            }

            Debug.Log(
                "[SUBSYSTEM] MASS DAMPENER failed — " +
                "no valid allied target detected."
            );

            if (SubsystemFeedbackUI.Instance != null)
            {
                SubsystemFeedbackUI.Instance.ShowMessage(
                    "<b><size=110%>HEXA_766 // MASS DAMPENER</size></b>\n" +
                    "<b>NO VALID TARGET</b>\n" +
                    "ALLIED UNIT REQUIRED"
                );
            }

            return;
        }

        if (target.IsDead)
            return;

        if (target.teamID !=
            activeUnit.teamID)
        {
            Debug.Log(
                "[SUBSYSTEM] MASS DAMPENER requires " +
                "an allied target."
            );

            return;
        }

        int loadBefore =
            target.EffectiveLoad;

        bool applied =
            target.ApplyLoadModifier(-1);

        if (!applied)
        {
            Debug.Log(
                $"[SUBSYSTEM] MASS DAMPENER had no effect on " +
                $"{target.name}."
            );

            return;
        }

        int actualReduction =
            loadBefore -
            target.EffectiveLoad;

        subsystemUsedThisTurn =
            true;

        Debug.Log(
            $"[SUBSYSTEM] MASS DAMPENER | " +
            $"{activeUnit.name} → {target.name} | " +
            $"EffectiveLoad={target.EffectiveLoad}"
        );

        ShowUnitSubsystemFeedback(
            "MASS DAMPENER",
            $"EFFECTIVE LOAD  -{actualReduction}  →  {target.EffectiveLoad}",
            target
        );
    }


    // =================================================
    // BALLAST INJECTION
    // =================================================

    private void TryBallastInjection(
    PlayerUnit activeUnit
)
    {
        PlayerUnit target =
            GetHoveredPlayerUnit(
                activeUnit.teamID,
                requireLoadAboveMinimum: false,
                requireEnemy: true,
                requireLoadBelowMaximum: true
            );

        if (target == null)
        {
            // Was the cursor over an enemy that simply
            // cannot receive any more load?
            PlayerUnit blockedTarget =
                GetHoveredPlayerUnit(
                    activeUnit.teamID,
                    requireLoadAboveMinimum: false,
                    requireEnemy: true,
                    requireLoadBelowMaximum: false
                );

            if (blockedTarget != null &&
                blockedTarget.EffectiveLoad >= 5)
            {
                Debug.Log(
                    $"[SUBSYSTEM] BALLAST INJECTION unavailable — " +
                    $"{blockedTarget.name} is already at maximum Effective Load."
                );

                if (SubsystemFeedbackUI.Instance != null)
                {
                    SubsystemFeedbackUI.Instance.ShowMessage(
                        "<b><size=110%>HEXA_766 // BALLAST INJECTION</size></b>\n" +
                        "<b>NO EFFECT</b>\n" +
                        "MAXIMUM LOAD REACHED  //  EFFECTIVE LOAD 5"
                    );
                }

                return;
            }

            Debug.Log(
                "[SUBSYSTEM] BALLAST INJECTION failed — " +
                "no valid enemy target detected."
            );

            if (SubsystemFeedbackUI.Instance != null)
            {
                SubsystemFeedbackUI.Instance.ShowMessage(
                    "<b><size=110%>HEXA_766 // BALLAST INJECTION</size></b>\n" +
                    "<b>NO VALID TARGET</b>\n" +
                    "ENEMY UNIT REQUIRED"
                );
            }

            return;
        }

        // Safety check.
        if (target.teamID ==
            activeUnit.teamID)
        {
            return;
        }

        int loadBefore =
            target.EffectiveLoad;

        bool applied =
            target.ApplyLoadModifier(+1);

        if (!applied)
        {
            return;
        }

        int actualIncrease =
            target.EffectiveLoad -
            loadBefore;

        subsystemUsedThisTurn =
            true;

        Debug.Log(
            $"[SUBSYSTEM] BALLAST INJECTION | " +
            $"{activeUnit.name} → {target.name} | " +
            $"EffectiveLoad={target.EffectiveLoad}"
        );

        ShowUnitSubsystemFeedback(
            "BALLAST INJECTION",
            $"EFFECTIVE LOAD  +{actualIncrease}  →  {target.EffectiveLoad}",
            target
        );
    }


    // =================================================
    // STRUCTURAL REINFORCEMENT
    // =================================================

    private void TryStructuralReinforcement(
    PlayerUnit activeUnit
    )
    {
        HexCapacity target =
            GetHoveredHex();

        if (target == null)
        {
            Debug.Log(
                "[SUBSYSTEM] STRUCTURAL REINFORCEMENT failed — " +
                "hover a hex."
            );

            return;
        }

        int capacityBefore =
            target.EffectiveCapacity;

        bool applied =
            target.ApplyCapacityModifier(+3);

        if (!applied)
        {
            Debug.Log(
                $"[SUBSYSTEM] STRUCTURAL REINFORCEMENT had no effect " +
                $"on {target.name}."
            );

            return;
        }

        int actualIncrease =
            target.EffectiveCapacity -
            capacityBefore;

        subsystemUsedThisTurn =
            true;

        Debug.Log(
            $"[SUBSYSTEM] STRUCTURAL REINFORCEMENT | " +
            $"{activeUnit.name} → {target.name} | " +
            $"Capacity={target.EffectiveCapacity}"
        );

        ShowHexSubsystemFeedback(
             "STRUCTURAL REINFORCEMENT",
             $"STRUCTURAL CAPACITY  +{actualIncrease}",
             target
        );
    }

    public bool TryStructuralReinforcementOnHex(
    HexCapacity target
    )
    {
        TurnManager turnManager =
            TurnManager.Instance;

        if (turnManager == null)
            return false;

        PlayerUnit activeUnit =
            turnManager.current;

        if (activeUnit == null)
            return false;


        // Keep the subsystem turn state synchronized.
        if (activeUnit != lastTurnUnit)
        {
            lastTurnUnit =
                activeUnit;

            subsystemUsedThisTurn =
                false;
        }


        if (enemyDroneDirector == null)
        {
            enemyDroneDirector =
                FindFirstObjectByType<EnemyDroneDirector>();
        }

        if (enemyDroneDirector != null &&
            enemyDroneDirector.IsRunning)
        {
            return false;
        }

        if (activeUnit.IsDead)
            return false;

        if (activeUnit.IsMoving)
            return false;

        if (subsystemUsedThisTurn)
            return false;

        if (target == null)
            return false;

        if (target.IsCollapsed ||
            target.IsCollapseSequenceRunning)
        {
            return false;
        }


        int capacityBefore =
            target.EffectiveCapacity;

        bool applied =
            target.ApplyCapacityModifier(
                +3
            );

        if (!applied)
            return false;


        int actualIncrease =
            target.EffectiveCapacity -
            capacityBefore;

        subsystemUsedThisTurn =
            true;


        Debug.Log(
            $"[SUBSYSTEM] STRUCTURAL REINFORCEMENT FROM SCANNER | " +
            $"{activeUnit.name} → {target.name} | " +
            $"Capacity={capacityBefore} → {target.EffectiveCapacity}"
        );


        ShowHexSubsystemFeedback(
            "STRUCTURAL REINFORCEMENT",
            $"STRUCTURAL CAPACITY  +{actualIncrease}",
            target
        );


        return true;
    }

    public bool TryNetworkStressOnHex(
    HexCapacity target
    )
    {
        TurnManager turnManager =
            TurnManager.Instance;

        if (turnManager == null)
            return false;

        PlayerUnit activeUnit =
            turnManager.current;

        if (activeUnit == null)
            return false;


        // Keep turn tracking synchronized even when
        // execution comes from the Tactical Scanner.
        if (activeUnit != lastTurnUnit)
        {
            lastTurnUnit =
                activeUnit;

            subsystemUsedThisTurn =
                false;
        }


        if (enemyDroneDirector != null &&
            enemyDroneDirector.IsRunning)
        {
            return false;
        }

        if (activeUnit.IsDead)
            return false;

        if (activeUnit.IsMoving)
            return false;

        if (subsystemUsedThisTurn)
            return false;

        if (target == null)
            return false;

        if (target.IsCollapsed ||
            target.IsCollapseSequenceRunning)
        {
            return false;
        }


        int capacityBefore =
            target.EffectiveCapacity;

        bool applied =
            target.ApplyCapacityModifier(
                -3
            );

        if (!applied)
        {
            Debug.Log(
                $"[SUBSYSTEM] NETWORK STRESS had no effect " +
                $"on {target.name}."
            );

            return false;
        }


        int actualReduction =
            capacityBefore -
            target.EffectiveCapacity;

        subsystemUsedThisTurn =
            true;


        Debug.Log(
            $"[SUBSYSTEM] NETWORK STRESS | " +
            $"{activeUnit.name} → {target.name} | " +
            $"Capacity={capacityBefore} → {target.EffectiveCapacity} | " +
            $"Load={target.CurrentLoad}"
        );


        ShowHexSubsystemFeedback(
            "NETWORK STRESS",
            $"STRUCTURAL CAPACITY  -{actualReduction}",
            target
        );

        return true;
    }


    public bool TryMassDampenerOnUnit(
    PlayerUnit target
)
    {
        TurnManager turnManager =
            TurnManager.Instance;

        if (turnManager == null)
            return false;

        PlayerUnit activeUnit =
            turnManager.current;

        if (activeUnit == null)
            return false;


        // Keep turn tracking synchronized when the
        // command originates from the Tactical Scanner.
        if (activeUnit != lastTurnUnit)
        {
            lastTurnUnit =
                activeUnit;

            subsystemUsedThisTurn =
                false;
        }


        if (enemyDroneDirector != null &&
            enemyDroneDirector.IsRunning)
        {
            return false;
        }

        if (activeUnit.IsDead)
            return false;

        if (activeUnit.IsMoving)
            return false;

        if (subsystemUsedThisTurn)
            return false;

        if (target == null)
            return false;

        if (target.IsDead)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;


        // MASS DAMPENER may only affect an allied unit.
        if (target.teamID !=
            activeUnit.teamID)
        {
            Debug.Log(
                "[SUBSYSTEM] MASS DAMPENER rejected — " +
                "target is not allied."
            );

            return false;
        }


        // Effective Load cannot be reduced below 1.
        if (target.EffectiveLoad <= 1)
        {
            Debug.Log(
                $"[SUBSYSTEM] MASS DAMPENER unavailable — " +
                $"{target.name} is already at minimum Effective Load."
            );

            return false;
        }


        int loadBefore =
            target.EffectiveLoad;


        bool applied =
            target.ApplyLoadModifier(
                -1
            );

        if (!applied)
        {
            Debug.Log(
                $"[SUBSYSTEM] MASS DAMPENER had no effect on " +
                $"{target.name}."
            );

            return false;
        }


        int actualReduction =
            loadBefore -
            target.EffectiveLoad;


        subsystemUsedThisTurn =
            true;


        Debug.Log(
            $"[SUBSYSTEM] MASS DAMPENER | " +
            $"{activeUnit.name} → {target.name} | " +
            $"EffectiveLoad={loadBefore} → {target.EffectiveLoad}"
        );


        ShowUnitSubsystemFeedback(
            "MASS DAMPENER",
            $"EFFECTIVE LOAD  -{actualReduction}  →  {target.EffectiveLoad}",
            target
        );


        return true;
    }

    public bool TryBallastInjectionOnUnit(
    PlayerUnit target
)
    {
        TurnManager turnManager =
            TurnManager.Instance;

        if (turnManager == null)
            return false;

        PlayerUnit activeUnit =
            turnManager.current;

        if (activeUnit == null)
            return false;


        // Keep turn tracking synchronized when the
        // command originates from the Tactical Scanner.
        if (activeUnit != lastTurnUnit)
        {
            lastTurnUnit =
                activeUnit;

            subsystemUsedThisTurn =
                false;
        }


        if (enemyDroneDirector != null &&
            enemyDroneDirector.IsRunning)
        {
            return false;
        }

        if (activeUnit.IsDead)
            return false;

        if (activeUnit.IsMoving)
            return false;

        if (subsystemUsedThisTurn)
            return false;

        if (target == null)
            return false;

        if (target.IsDead)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;


        // BALLAST INJECTION may only affect an enemy unit.
        if (target.teamID ==
            activeUnit.teamID)
        {
            Debug.Log(
                "[SUBSYSTEM] BALLAST INJECTION rejected — " +
                "target is not an enemy."
            );

            return false;
        }


        // Effective Load cannot be increased above 5.
        if (target.EffectiveLoad >= 5)
        {
            Debug.Log(
                $"[SUBSYSTEM] BALLAST INJECTION unavailable — " +
                $"{target.name} is already at maximum Effective Load."
            );

            return false;
        }


        int loadBefore =
            target.EffectiveLoad;


        bool applied =
            target.ApplyLoadModifier(
                +1
            );

        if (!applied)
        {
            Debug.Log(
                $"[SUBSYSTEM] BALLAST INJECTION had no effect on " +
                $"{target.name}."
            );

            return false;
        }


        int actualIncrease =
            target.EffectiveLoad -
            loadBefore;


        subsystemUsedThisTurn =
            true;


        Debug.Log(
            $"[SUBSYSTEM] BALLAST INJECTION | " +
            $"{activeUnit.name} → {target.name} | " +
            $"EffectiveLoad={loadBefore} → {target.EffectiveLoad}"
        );


        ShowUnitSubsystemFeedback(
            "BALLAST INJECTION",
            $"EFFECTIVE LOAD  +{actualIncrease}  →  {target.EffectiveLoad}",
            target
        );


        return true;
    }
    // =================================================
    // NETWORK STRESS
    // =================================================

    private void TryNetworkStress(
    PlayerUnit activeUnit
    )
    {
        HexCapacity target =
            GetHoveredHex();

        if (target == null)
        {
            Debug.Log(
                "[SUBSYSTEM] NETWORK STRESS failed — " +
                "hover a hex."
            );

            return;
        }

        int capacityBefore =
            target.EffectiveCapacity;

        bool applied =
            target.ApplyCapacityModifier(-3);

        if (!applied)
        {
            Debug.Log(
                $"[SUBSYSTEM] NETWORK STRESS had no effect " +
                $"on {target.name}."
            );

            return;
        }

        int actualReduction =
            capacityBefore -
            target.EffectiveCapacity;

        subsystemUsedThisTurn =
            true;

        Debug.Log(
            $"[SUBSYSTEM] NETWORK STRESS | " +
            $"{activeUnit.name} → {target.name} | " +
            $"Capacity={target.EffectiveCapacity} | " +
            $"Load={target.CurrentLoad}"
        );

        ShowHexSubsystemFeedback(
            "NETWORK STRESS",
            $"STRUCTURAL CAPACITY  -{actualReduction}",
            target
        );
    }

    // =================================================
    // TOPOLOGY SCAN
    // =================================================

    private void TryTopologyScan(
     PlayerUnit activeUnit
 )
    {
        if (topologyScanner == null)
        {
            Debug.LogWarning(
                "[SUBSYSTEM] TOPOLOGY SCAN unavailable — " +
                "TopologyScanner not found."
            );

            return;
        }

        bool scanSucceeded =
            topologyScanner.RunScan();

        if (!scanSucceeded)
        {
            return;
        }

        // Tactical Scanner is an analysis interface.
        // Opening/running it does NOT consume the
        // unit's subsystem intervention opportunity.

        if (topologyScanHUD != null)
        {
            topologyScanHUD.Show(
                topologyScanner
            );
        }

        Debug.Log(
            $"[SUBSYSTEM] TOPOLOGY SCAN | " +
            $"{activeUnit.name} completed network analysis."
        );

        if (SubsystemFeedbackUI.Instance != null)
        {
            SubsystemFeedbackUI.Instance.ShowMessage(
                "<b><size=110%>HEXA_766 // TOPOLOGY SCAN</size></b>\n" +
                "<b>NETWORK ANALYSIS COMPLETE</b>\n" +
                "CRITICAL NODE DATA ACQUIRED"
            );
        }
    }
    // =================================================
    // TARGETING
    // =================================================

    private PlayerUnit GetHoveredPlayerUnit(
    int requiredTeamID = -1,
    bool requireLoadAboveMinimum = false,
    bool requireEnemy = false,
    bool requireLoadBelowMaximum = false
    )
    {
        if (gameplayCamera == null)
            return null;

        if (TurnManager.Instance == null)
            return null;

        Vector2 mousePosition =
            Input.mousePosition;

        PlayerUnit bestTarget =
            null;

        float bestDistance =
            float.MaxValue;

        foreach (PlayerUnit unit in
                 TurnManager.Instance.players)
        {
            if (unit == null)
                continue;

            if (unit.IsDead)
                continue;

            if (!unit.gameObject.activeInHierarchy)
                continue;

            // Optional team restriction.
            if (requiredTeamID >= 0)
            {
                if (requireEnemy)
                {
                    if (unit.teamID == requiredTeamID)
                        continue;
                }
                else
                {
                    if (unit.teamID != requiredTeamID)
                        continue;
                }
            }

            // MASS DAMPENER cannot reduce below 1.
            // Ignore units that cannot receive the effect.
            if (requireLoadAboveMinimum &&
                unit.EffectiveLoad <= 1)
            {
                continue;
            }

            // BALLAST INJECTION cannot increase above 5.
            if (requireLoadBelowMaximum &&
                unit.EffectiveLoad >= 5)
            {
                continue;
            }

            Renderer[] renderers =
                unit.GetComponentsInChildren<Renderer>();

            if (renderers == null ||
                renderers.Length == 0)
            {
                continue;
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;

            bool foundVisibleRenderer =
                false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!renderer.enabled)
                    continue;

                if (!renderer.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds =
                    renderer.bounds;

                Vector3 center =
                    bounds.center;

                Vector3 extents =
                    bounds.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner =
                                center +
                                Vector3.Scale(
                                    extents,
                                    new Vector3(
                                        x,
                                        y,
                                        z
                                    )
                                );

                            Vector3 screen =
                                gameplayCamera
                                .WorldToScreenPoint(
                                    corner
                                );

                            if (screen.z <= 0f)
                                continue;

                            foundVisibleRenderer =
                                true;

                            minX =
                                Mathf.Min(
                                    minX,
                                    screen.x
                                );

                            maxX =
                                Mathf.Max(
                                    maxX,
                                    screen.x
                                );

                            minY =
                                Mathf.Min(
                                    minY,
                                    screen.y
                                );

                            maxY =
                                Mathf.Max(
                                    maxY,
                                    screen.y
                                );
                        }
                    }
                }
            }

            if (!foundVisibleRenderer)
                continue;

            const float padding = 35f;

            minX -= padding;
            maxX += padding;
            minY -= padding;
            maxY += padding;

            bool mouseInside =
                mousePosition.x >= minX &&
                mousePosition.x <= maxX &&
                mousePosition.y >= minY &&
                mousePosition.y <= maxY;

            if (!mouseInside)
                continue;

            Vector2 screenCenter =
                new Vector2(
                    (minX + maxX) * 0.5f,
                    (minY + maxY) * 0.5f
                );

            float distance =
                Vector2.Distance(
                    mousePosition,
                    screenCenter
                );

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestTarget =
                    unit;
            }
        }

        return bestTarget;
    }


    private HexCapacity GetHoveredHex()
    {
        if (gameplayCamera == null)
            return null;

        Ray ray =
            gameplayCamera.ScreenPointToRay(
                Input.mousePosition
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                1000f
            );

        foreach (RaycastHit hit in hits)
        {
            HexCapacity capacity =
                hit.collider.GetComponentInParent<HexCapacity>();

            if (capacity != null &&
                !capacity.IsCollapsed)
            {
                return capacity;
            }
        }

        return null;
    }

    // =================================================
    // SUBSYSTEM UI FEEDBACK
    // =================================================

    private void ShowHexSubsystemFeedback(
    string subsystemName,
    string changeDescription,
    HexCapacity target
)
    {
        if (SubsystemFeedbackUI.Instance == null ||
            target == null)
        {
            return;
        }

        int capacity =
            target.EffectiveCapacity;

        int load =
            target.CurrentLoad;

        string stateLine;

        if (load > capacity)
        {
            int overload =
                load - capacity;

            stateLine =
                $"LOAD INDEX  {load}  //  " +
                $"<b>OVERLOAD +{overload}</b>";
        }
        else if (load == capacity)
        {
            stateLine =
                $"LOAD INDEX  {load}  //  " +
                $"<b>MAXIMUM LOAD</b>";
        }
        else
        {
            stateLine =
                $"LOAD INDEX  {load}  //  " +
                $"<b>STABLE</b>";
        }

        string message =
            $"<b><size=110%>HEXA_766 // {subsystemName}</size></b>\n" +
            $"{changeDescription}  →  {capacity}\n" +
            $"{stateLine}";

        SubsystemFeedbackUI.Instance.ShowMessage(
            message
        );
    }

    private void ShowUnitSubsystemFeedback(
    string subsystemName,
    string changeDescription,
    PlayerUnit target
)
    {
        if (SubsystemFeedbackUI.Instance == null ||
            target == null)
        {
            return;
        }

        string message =
            $"<b><size=110%>HEXA_766 // {subsystemName}</size></b>\n" +
            $"TARGET  //  {target.name}\n" +
            $"{changeDescription}";

        SubsystemFeedbackUI.Instance.ShowMessage(
            message
        );
    }
}
