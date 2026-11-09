using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemyTeamAI : MonoBehaviour
{
    [Header("ENEMY TEAM")]
    [SerializeField]
    private int enemyTeamID = 2;


    [Header("BEHAVIOR")]
    [Range(0f, 1f)]
    [SerializeField]
    private float aggression = 0.80f;

    [SerializeField]
    private float thinkDelay = 0.55f;


    [Header("DEBUG")]
    [SerializeField]
    private bool verboseLogging = true;


    private TurnManager turnManager;
    private Map1HexGrid map;

    private PlayerUnit observedTurnUnit;

    private PlayerUnit focusTarget;

    private enum GoalKind
    {
        DeliverCore,
        InterceptCarrier,
        ProtectCarrier,
        AcquireCore,
        FocusFire,
        HuntWeakest
    }


    private struct EnemyGoal
    {
        public GoalKind kind;

        public string label;

        public Vector3 worldPosition;

        public PlayerUnit targetUnit;

        public WorldBox targetBox;

        public string targetTileName;

        public int targetVertex;
    }


    private struct MoveChoice
    {
        public bool valid;

        public string tileName;

        public int vertexIndex;

        public float score;
    }


    private void Start()
    {
        turnManager =
            TurnManager.Instance;

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }


        map =
            FindFirstObjectByType<Map1HexGrid>();


        if (turnManager == null)
        {
            Debug.LogError(
                "[ENEMY AI] TurnManager not found."
            );
        }

        if (map == null)
        {
            Debug.LogError(
                "[ENEMY AI] Map1HexGrid not found."
            );
        }
    }


    private void Update()
    {
        if (turnManager == null ||
            map == null)
        {
            return;
        }


        PlayerUnit activeUnit =
            turnManager.current;


        if (activeUnit == null)
            return;


        // Human/allied turn.
        if (activeUnit.teamID != enemyTeamID)
        {
            observedTurnUnit =
                activeUnit;

            return;
        }


        // A dead enemy may still be in its respawn sequence.
        // Do not mark the turn as handled yet.
        if (activeUnit.IsDead)
            return;


        // Already made a decision for this occurrence
        // of this unit's turn.
        if (activeUnit == observedTurnUnit)
            return;


        observedTurnUnit =
            activeUnit;


        StartCoroutine(
            ThinkAndAct(
                activeUnit
            )
        );
    }


    private IEnumerator ThinkAndAct(
        PlayerUnit unit
    )
    {
        yield return
            new WaitForSeconds(
                thinkDelay
            );


        // Turn may have changed while thinking.
        if (turnManager == null ||
            turnManager.current != unit ||
            unit == null ||
            unit.IsDead)
        {
            yield break;
        }


        // PRIORITY 1 — Protect threatened allied hexes.
        PlayerUnit reinforcementTarget =
            TryTacticalStructuralReinforcement(unit);

        // PRIORITY 2 — Ballast Injection.
        PlayerUnit interventionTarget =
            TryTacticalBallastInjection(unit);

        // PRIORITY 3 — Mass Dampener.
        PlayerUnit massTarget =
            TryTacticalMassDampener(unit);

        if (massTarget != null)
        {
            MassDampenerVFX.Play(massTarget);
        }

        // PRIORITY 4 — Network Stress.
        PlayerUnit stressTarget =
            TryTacticalNetworkStress(unit);

        // Choose the affected unit for camera presentation.
        PlayerUnit cameraTarget =
            reinforcementTarget != null
                ? reinforcementTarget
                : interventionTarget != null
                    ? interventionTarget
                    : massTarget != null
                        ? massTarget
                        : stressTarget;

        if (cameraTarget != null)
        {
            yield return StartCoroutine(
                ShowEnemyInterventionCamera(
                    unit,
                    cameraTarget
                )
            );

            // Verify that the same enemy still owns the turn.
            if (turnManager == null ||
                turnManager.current != unit ||
                unit == null ||
                unit.IsDead)
            {
                yield break;
            }
        }

        // =====================================================
        // PRIORITY 0
        // ATTACK IMMEDIATELY IF ALREADY SHARING A HEX
        // =====================================================

        if (turnManager.TryAutomaticSameHexAttack(
                unit
            ))
        {
            Log(
                unit,
                "ATTACK",
                "Hostile already on current node."
            );

            yield break;
        }

        // =====================================================
        // PRIORITY 1
        // ATTACK AN ADJACENT HOSTILE BEFORE MOVING
        // =====================================================

        ValidateFocusTarget(
            unit
        );


        PlayerUnit attackTarget =
            FindBestAttackableTarget(
                unit
            );


        if (attackTarget != null)
        {
            focusTarget =
                attackTarget;


            if (turnManager.TryAIAttack(
                    unit,
                    attackTarget
                ))
            {
                Log(
                    unit,
                    "FOCUS ATTACK",
                    $"{attackTarget.name} | " +
                    $"SHIELD={attackTarget.shield} | " +
                    $"ENERGY={attackTarget.energy} | " +
                    $"CORE={attackTarget.IsCarryingBox}"
                );

                yield break;
            }
        }

        // =====================================================
        // CHOOSE STRATEGIC GOAL
        // =====================================================

        EnemyGoal goal =
            ChooseGoal(
                unit
            );


        if (string.IsNullOrEmpty(
                goal.label
            ))
        {
            Log(
                unit,
                "HOLD",
                "No meaningful target available."
            );

            turnManager.TryEndTurn();

            yield break;
        }


        // =====================================================
        // GET LEGAL MOVEMENT OPTIONS
        // =====================================================

        List<(
            string tileName,
            int vertexIndex
        )> reachable =
            map.GetReachableVerticesFromPlayerAsPairs(
                unit.gameObject,
                map.defaultMovePoints
            );


        if (reachable == null ||
            reachable.Count == 0)
        {
            Log(
                unit,
                "HOLD",
                "No reachable movement vertices."
            );

            turnManager.TryEndTurn();

            yield break;
        }


        // =====================================================
        // SCORE LEGAL MOVES
        // =====================================================

        MoveChoice choice =
            ChooseBestMove(
                unit,
                goal,
                reachable
            );


        if (!choice.valid)
        {
            Log(
                unit,
                "HOLD",
                "No legal unoccupied destination."
            );

            turnManager.TryEndTurn();

            yield break;
        }


        Log(
            unit,
            goal.kind.ToString().ToUpperInvariant(),
            $"{goal.label} | " +
            $"MOVE={choice.tileName} " +
            $"v{choice.vertexIndex} | " +
            $"SCORE={choice.score:F1}"
        );


        // =====================================================
        // USE NORMAL TURNMANAGER MOVEMENT ENTRY
        // =====================================================

        Vector2Int destinationCoord =
            map.GetTileGridCoord(
                choice.tileName
            );


        turnManager.OnSphereClicked(
            destinationCoord,
            choice.vertexIndex
        );
    }


    private PlayerUnit TryTacticalBallastInjection(PlayerUnit unit)
    {
        if (unit == null ||
            unit.IsDead ||
            turnManager == null ||
            map == null ||
            turnManager.current != unit)
        {
            return null;
        }

        SubsystemController subsystem =
            FindFirstObjectByType<SubsystemController>();

        if (subsystem == null ||
            subsystem.SubsystemUsedThisTurn)
        {
            return null;
        }

        PlayerUnit bestTarget = null;

        float bestScore = float.NegativeInfinity;

        foreach (PlayerUnit target in turnManager.players)
        {
            if (!IsLivingUnit(target))
                continue;

            if (target.teamID == unit.teamID)
                continue;

            if (target.EffectiveLoad >= 5)
                continue;

            var location =
                map.GetPlayerTileAndVertex(
                    target.gameObject
                );

            if (string.IsNullOrEmpty(location.tileName))
                continue;

            if (!map.tileGameObjectMap.TryGetValue(
                    location.tileName,
                    out GameObject tile))
            {
                continue;
            }

            if (tile == null)
                continue;

            HexCapacity capacity =
                tile.GetComponent<HexCapacity>();

            if (capacity == null ||
                capacity.IsCollapsed ||
                capacity.IsCollapseSequenceRunning)
            {
                continue;
            }

            // One extra unit of load must be enough
            // to threaten or exceed structural capacity.
            int projectedLoad =
                capacity.CurrentLoad + 1;

            if (projectedLoad < capacity.EffectiveCapacity)
                continue;

            float score = 100f;

            // Prefer attacks that push the structure
            // beyond its capacity.
            score +=
                (projectedLoad - capacity.EffectiveCapacity)
                * 100f;

            // The AI Core carrier is a high-value target.
            if (target.IsCarryingBox)
                score += 200f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        if (bestTarget == null)
            return null;

        int loadBefore =
            bestTarget.EffectiveLoad;

        bool success =
            subsystem.TryBallastInjectionOnUnit(
                bestTarget
            );

        // Only present an impact if the intervention succeeded.
        if (!success)
            return null;

        // Localized impact on the affected robot.
        // Visual only — no additional gameplay damage.
        HostileUnitImpactVFX.Play(
            bestTarget
        );

        if (SubsystemFeedbackUI.Instance != null)
        {
            string dangerStatus =
                "STRUCTURAL LOAD INCREASED";

            var location =
                map.GetPlayerTileAndVertex(
                    bestTarget.gameObject
                );

            if (!string.IsNullOrEmpty(location.tileName) &&
                map.tileGameObjectMap.TryGetValue(
                    location.tileName,
                    out GameObject tile
                ) &&
                tile != null)
            {
                HexCapacity capacity =
                    tile.GetComponent<HexCapacity>();

                if (capacity != null)
                {
                    if (capacity.CurrentLoad >
                        capacity.EffectiveCapacity)
                    {
                        dangerStatus =
                            "CRITICAL // STRUCTURAL OVERLOAD";
                    }
                    else if (capacity.CurrentLoad ==
                             capacity.EffectiveCapacity)
                    {
                        dangerStatus =
                            "WARNING // STRUCTURAL LIMIT REACHED";
                    }
                }
            }

            SubsystemFeedbackUI.Instance.ShowHostileMessage(
                "<b>ENEMY BALLAST INJECTION</b>\n" +
                $"TARGET // {bestTarget.UnitCode}\n" +
                $"EFFECTIVE LOAD // {loadBefore} → {bestTarget.EffectiveLoad}\n" +
                $"<color=#FF7777><b>{dangerStatus}</b></color>"
            );
        }
        
        Log(
           unit,
           "TACTICAL BALLAST",
           $"TARGET={bestTarget.UnitCode} | " +
           $"LOAD={loadBefore} → {bestTarget.EffectiveLoad}"
    );

    return bestTarget;

    }


    private PlayerUnit TryTacticalNetworkStress(PlayerUnit unit)
    {
        if (unit == null ||
            unit.IsDead ||
            turnManager == null ||
            map == null ||
            turnManager.current != unit)
        {
            return null;
        }

        SubsystemController subsystem =
            FindFirstObjectByType<SubsystemController>();

        if (subsystem == null ||
            subsystem.SubsystemUsedThisTurn)
        {
            return null;
        }

        HexCapacity bestHex = null;
        PlayerUnit bestVictim = null;
        float bestScore = float.NegativeInfinity;

        foreach (PlayerUnit target in turnManager.players)
        {
            if (!IsLivingUnit(target) ||
                target.teamID == unit.teamID)
            {
                continue;
            }

            var location =
                map.GetPlayerTileAndVertex(target.gameObject);

            if (string.IsNullOrEmpty(location.tileName))
                continue;

            if (!map.tileGameObjectMap.TryGetValue(
                    location.tileName,
                    out GameObject tile) ||
                tile == null)
            {
                continue;
            }

            HexCapacity hex =
                tile.GetComponent<HexCapacity>();

            if (hex == null ||
                hex.IsCollapsed ||
                hex.IsCollapseSequenceRunning)
            {
                continue;
            }

            // Avoid destabilizing hexes occupied by allies.
            bool alliedUnitPresent = false;

            foreach (PlayerUnit ally in turnManager.players)
            {
                if (!IsLivingUnit(ally))
                    continue;

                if (ally.teamID != unit.teamID)
                    continue;

                if (ally.currentTileCoord ==
                    target.currentTileCoord)
                {
                    alliedUnitPresent = true;
                    break;
                }
            }

            if (alliedUnitPresent)
                continue;

            int projectedCapacity =
                Mathf.Max(0, hex.EffectiveCapacity - 3);

            int currentLoad =
                hex.CurrentLoad;

            // Attack only when Stress would push the hex
            // to its structural limit or beyond it.
            if (currentLoad < projectedCapacity)
                continue;

            float score = 100f;

            if (currentLoad > projectedCapacity)
            {
                score +=
                    (currentLoad - projectedCapacity) * 100f;
            }

            if (target.IsCarryingBox)
                score += 200f;

            if (score > bestScore)
            {
                bestScore = score;
                bestHex = hex;
                bestVictim = target;
            }
        }

        if (bestHex == null || bestVictim == null)
            return null;

        int capacityBefore =
            bestHex.EffectiveCapacity;

        bool success =
            subsystem.TryNetworkStressOnHex(bestHex);

        if (!success)
            return null;

        // NETWORK STRESS — ENEMY VFX
        NetworkStressVFX.Play(bestHex);

        if (SubsystemFeedbackUI.Instance != null)
        {
            SubsystemFeedbackUI.Instance.ShowHostileMessage(
                "<b>ENEMY NETWORK STRESS</b>\n" +
                $"HEX // {bestHex.name}\n" +
                $"CAPACITY // {capacityBefore} → {bestHex.EffectiveCapacity}\n" +
                $"LOAD // {bestHex.CurrentLoad}\n" +
                $"STATE // {bestHex.LoadState.ToString().ToUpperInvariant()}"
            );
        }

        Log(
            unit,
            "TACTICAL NETWORK STRESS",
            $"HEX={bestHex.name} | " +
            $"CAPACITY={capacityBefore} → {bestHex.EffectiveCapacity}"
        );

        return bestVictim;
    }


    private PlayerUnit TryTacticalStructuralReinforcement(
        PlayerUnit unit
    )
    {
        if (unit == null ||
            unit.IsDead ||
            turnManager == null ||
            map == null ||
            turnManager.current != unit)
        {
            return null;
        }

        SubsystemController subsystem =
            FindFirstObjectByType<SubsystemController>();

        if (subsystem == null ||
            subsystem.SubsystemUsedThisTurn)
        {
            return null;
        }

        HexCapacity bestHex = null;
        PlayerUnit bestAlly = null;
        float bestScore = float.NegativeInfinity;

        foreach (PlayerUnit ally in turnManager.players)
        {
            if (!IsLivingUnit(ally))
                continue;

            if (ally.teamID != unit.teamID)
                continue;

            var location =
                map.GetPlayerTileAndVertex(
                    ally.gameObject
                );

            if (string.IsNullOrEmpty(location.tileName))
                continue;

            if (!map.tileGameObjectMap.TryGetValue(
                    location.tileName,
                    out GameObject tile) ||
                tile == null)
            {
                continue;
            }

            HexCapacity hex =
                tile.GetComponent<HexCapacity>();

            if (hex == null ||
                hex.IsCollapsed ||
                hex.IsCollapseSequenceRunning)
            {
                continue;
            }

            // Do not reinforce a hex occupied by enemies.
            bool hostilePresent = false;

            foreach (PlayerUnit other in turnManager.players)
            {
                if (!IsLivingUnit(other))
                    continue;

                if (other.teamID == unit.teamID)
                    continue;

                if (other.currentTileCoord ==
                    ally.currentTileCoord)
                {
                    hostilePresent = true;
                    break;
                }
            }

            if (hostilePresent)
                continue;

            int load = hex.CurrentLoad;
            int capacity = hex.EffectiveCapacity;

            // Reinforce only full or overloaded hexes.
            if (load < capacity)
                continue;

            // The reinforcement should be sufficient
            // to eliminate the overload.
            if (load > capacity + 3)
                continue;

            float score = 100f;

            // Overloaded hexes have priority.
            if (load > capacity)
            {
                score += 200f;
                score += (load - capacity) * 50f;
            }

            // Prefer rescuing hexes already carrying
            // accumulated structural failure risk.
            score +=
                hex.ConsecutiveOverloadedRoundChecks * 150f;

            // Protect AI Core carriers.
            if (ally.IsCarryingBox)
                score += 200f;

            if (score > bestScore)
            {
                bestScore = score;
                bestHex = hex;
                bestAlly = ally;
            }
        }

        if (bestHex == null || bestAlly == null)
            return null;

        int capacityBefore =
            bestHex.EffectiveCapacity;

        bool success =
            subsystem.TryStructuralReinforcementOnHex(
                bestHex
            );

        if (!success)
            return null;

        // STRUCTURAL REINFORCEMENT — ENEMY VFX
        StructuralReinforcementVFX.Play(bestHex);

        if (SubsystemFeedbackUI.Instance != null)
        {
            SubsystemFeedbackUI.Instance.ShowHostileMessage(
                "<b>ENEMY STRUCTURAL REINFORCEMENT</b>\n" +
                $"HEX // {bestHex.name}\n" +
                $"CAPACITY // {capacityBefore} → {bestHex.EffectiveCapacity}\n" +
                $"LOAD // {bestHex.CurrentLoad}\n" +
                $"STATE // {bestHex.LoadState.ToString().ToUpperInvariant()}"
            );
        }

        Log(
            unit,
            "TACTICAL STRUCTURAL REINFORCEMENT",
            $"HEX={bestHex.name} | " +
            $"CAPACITY={capacityBefore} → {bestHex.EffectiveCapacity}"
        );

        return bestAlly;
    }


    private IEnumerator ShowEnemyInterventionCamera(
    PlayerUnit actingEnemy,
    PlayerUnit affectedUnit
)
    {
        CameraFollow cameraFollow =
            FindFirstObjectByType<CameraFollow>();

        if (cameraFollow == null ||
            affectedUnit == null)
        {
            yield break;
        }

        // Show the unit affected by the enemy intervention.
        cameraFollow.SetTarget(
            affectedUnit.transform
        );

        // Match the Tactical Scanner's current timing.
        yield return new WaitForSeconds(
            1.4f
        );

        // Only restore the camera if the same enemy
        // still owns the current turn.
        if (actingEnemy == null ||
            turnManager == null ||
            turnManager.current != actingEnemy)
        {
            yield break;
        }

        cameraFollow.SetTarget(
            actingEnemy.transform
        );

        Debug.Log(
            $"[ENEMY AI CAMERA] Returned to {actingEnemy.UnitCode}"
        );
    }


    private PlayerUnit TryTacticalMassDampener(PlayerUnit unit)
    {
        if (unit == null ||
            unit.IsDead ||
            turnManager == null ||
            map == null ||
            turnManager.current != unit)
        {
            return null;
        }

        SubsystemController subsystem =
            FindFirstObjectByType<SubsystemController>();

        if (subsystem == null ||
            subsystem.SubsystemUsedThisTurn)
        {
            return null;
        }

        PlayerUnit bestTarget = null;
        float bestScore = float.NegativeInfinity;

        foreach (PlayerUnit ally in turnManager.players)
        {
            if (!IsLivingUnit(ally))
                continue;

            if (ally.teamID != unit.teamID)
                continue;

            if (ally.EffectiveLoad <= 1)
                continue;

            var location =
                map.GetPlayerTileAndVertex(
                    ally.gameObject
                );

            if (string.IsNullOrEmpty(location.tileName))
                continue;

            if (!map.tileGameObjectMap.TryGetValue(
                    location.tileName,
                    out GameObject tile) ||
                tile == null)
            {
                continue;
            }

            HexCapacity capacity =
                tile.GetComponent<HexCapacity>();

            if (capacity == null ||
                capacity.IsCollapsed ||
                capacity.IsCollapseSequenceRunning)
            {
                continue;
            }

            int currentLoad =
                capacity.CurrentLoad;

            int effectiveCapacity =
                capacity.EffectiveCapacity;

            // Use defensive intervention only when
            // the structure has reached or exceeded capacity.
            if (currentLoad < effectiveCapacity)
                continue;

            float score = 100f;

            // Prioritize overloaded structures.
            if (currentLoad > effectiveCapacity)
            {
                score +=
                    (currentLoad - effectiveCapacity) * 100f;
            }

            // Protect an allied AI Core carrier.
            if (ally.IsCarryingBox)
                score += 250f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = ally;
            }
        }

        if (bestTarget == null)
            return null;

        int loadBefore =
            bestTarget.EffectiveLoad;

        bool success =
            subsystem.TryMassDampenerOnUnit(
                bestTarget
            );

        if (!success)
            return null;

        Log(
            unit,
            "TACTICAL MASS DAMPENER",
            $"ALLY={bestTarget.UnitCode} | " +
            $"LOAD={loadBefore} → {bestTarget.EffectiveLoad}"
        );

        return bestTarget;
    }


    // =========================================================
    // GOAL SELECTION
    // =========================================================

    private EnemyGoal ChooseGoal(
        PlayerUnit unit
    )
    {
        // -----------------------------------------------------
        // 1. THIS UNIT HAS THE CORE → DELIVER
        // -----------------------------------------------------

        if (unit.IsCarryingBox)
        {
            return new EnemyGoal
            {
                kind =
                    GoalKind.DeliverCore,

                label =
                    "DELIVER AI CORE",

                worldPosition =
                    map.GetVertexPosition(
                        "AIStation",
                        6
                    ),

                targetTileName =
                    "AIStation",

                targetVertex =
                    6
            };
        }


        // -----------------------------------------------------
        // 2. PLAYER HAS THE CORE → INTERCEPT
        // -----------------------------------------------------

        PlayerUnit hostileCarrier =
            FindHostileCarrier(
                unit
            );


        if (hostileCarrier != null)
        {
            return BuildPlayerGoal(
                GoalKind.InterceptCarrier,
                "INTERCEPT CORE CARRIER " +
                hostileCarrier.name,
                hostileCarrier
            );
        }


        // -----------------------------------------------------
        // 3. ALLY HAS THE CORE → PROTECT / PRESSURE THREAT
        // -----------------------------------------------------

        PlayerUnit alliedCarrier =
            FindAlliedCarrier(
                unit
            );


        if (alliedCarrier != null &&
            alliedCarrier != unit)
        {
            PlayerUnit threat =
                FindNearestHostile(
                    alliedCarrier.transform.position,
                    unit.teamID
                );


            if (threat != null)
            {
                return BuildPlayerGoal(
                    GoalKind.ProtectCarrier,
                    "PROTECT CARRIER / PRESSURE " +
                    threat.name,
                    threat
                );
            }
        }


        // -----------------------------------------------------
        // WOUNDED FOCUS TARGET
        // Continue coordinated pressure if the team has
        // already substantially weakened somebody.
        // -----------------------------------------------------

        ValidateFocusTarget(
            unit
        );


        if (focusTarget != null)
        {
            float focusDurabilityRatio =
    focusTarget.DurabilityRatio;


            bool highValueFocus =
                focusTarget.IsCarryingBox ||
                focusDurabilityRatio <= 0.50f;


            if (highValueFocus)
            {
                return BuildPlayerGoal(
                    GoalKind.FocusFire,
                    "FOCUS FIRE " +
                    focusTarget.name,
                    focusTarget
                );
            }
        }

        // -----------------------------------------------------
        // 4. CORE IS FREE → ACQUIRE IT
        // -----------------------------------------------------

        WorldBox freeCore =
            FindNearestFreeCore(
                unit.transform.position
            );


        if (freeCore != null)
        {
            Vector3 corePosition =
                freeCore.visualRoot != null
                    ? freeCore.visualRoot.position
                    : freeCore.transform.position;


            return new EnemyGoal
            {
                kind =
                    GoalKind.AcquireCore,

                label =
                    "ACQUIRE AI CORE",

                worldPosition =
                    corePosition,

                targetBox =
                    freeCore,

                targetVertex =
                    freeCore.vertexIndex
            };
        }


        // -----------------------------------------------------
        // 5. NO OBJECTIVE OPPORTUNITY → HUNT WEAKEST PLAYER
        // -----------------------------------------------------

        PlayerUnit weakest =
            FindWeakestHostile(
                unit
            );


        if (weakest != null)
        {
            return BuildPlayerGoal(
                GoalKind.HuntWeakest,
                "HUNT " +
                weakest.name,
                weakest
            );
        }


        return default;
    }


    private EnemyGoal BuildPlayerGoal(
        GoalKind kind,
        string label,
        PlayerUnit target
    )
    {
        var resolved =
            map.GetPlayerTileAndVertex(
                target.gameObject
            );


        return new EnemyGoal
        {
            kind =
                kind,

            label =
                label,

            worldPosition =
                target.transform.position,

            targetUnit =
                target,

            targetTileName =
                resolved.tileName,

            targetVertex =
                resolved.vertexIndex
        };
    }


    // =========================================================
    // MOVE SCORING
    // =========================================================

    private MoveChoice ChooseBestMove(
        PlayerUnit unit,
        EnemyGoal goal,
        List<(
            string tileName,
            int vertexIndex
        )> reachable
    )
    {
        MoveChoice best =
            new MoveChoice
            {
                valid = false,
                score = float.NegativeInfinity
            };


        foreach (var candidate in reachable)
        {
            Vector2Int candidateCoord =
                map.GetTileGridCoord(
                    candidate.tileName
                );


            // Never try to move onto an occupied vertex.
            if (turnManager.IsVertexOccupied(
                    candidateCoord,
                    candidate.vertexIndex,
                    unit
                ))
            {
                continue;
            }


            float score =
                ScoreMove(
                    unit,
                    goal,
                    candidate.tileName,
                    candidate.vertexIndex
                );


            if (!best.valid ||
                score > best.score)
            {
                best.valid =
                    true;

                best.tileName =
                    candidate.tileName;

                best.vertexIndex =
                    candidate.vertexIndex;

                best.score =
                    score;
            }
        }


        return best;
    }


    private float ScoreMove(
        PlayerUnit unit,
        EnemyGoal goal,
        string tileName,
        int vertexIndex
    )
    {
        Vector3 candidatePosition =
            map.GetVertexPosition(
                tileName,
                vertexIndex
            );


        float distance =
            Vector3.Distance(
                candidatePosition,
                goal.worldPosition
            );


        // Higher aggression means distance matters more:
        // the enemy closes ground more decisively.
        float distanceWeight =
            Mathf.Lerp(
                65f,
                135f,
                aggression
            );


        float score =
            1000f -
            distance * distanceWeight;


        // =====================================================
        // OBJECTIVE BONUSES
        // =====================================================

        if (goal.kind ==
            GoalKind.DeliverCore)
        {
            if (tileName == "AIStation" &&
                vertexIndex == 6)
            {
                score += 6000f;
            }
        }


        if (goal.kind ==
                GoalKind.AcquireCore &&
            goal.targetBox != null)
        {
            Vector2Int candidateCoord =
                map.GetTileGridCoord(
                    tileName
                );


            bool exactCoreLocation =
                candidateCoord.x ==
                    goal.targetBox.tileX &&
                candidateCoord.y ==
                    goal.targetBox.tileY &&
                vertexIndex ==
                    goal.targetBox.vertexIndex;


            if (exactCoreLocation)
            {
                score += 5000f;
            }
        }


        // =====================================================
        // COMBAT BONUSES
        // =====================================================

        if (goal.targetUnit != null)
        {
            // Use exact tile name here rather than relying only
            // on grid coordinates.
            if (!string.IsNullOrEmpty(
                    goal.targetTileName
                ) &&
                tileName ==
                    goal.targetTileName)
            {
                // Entering the hostile's logical hex is highly
                // desirable because the existing combat system
                // can resolve a same-hex attack afterward.
                score +=
                    Mathf.Lerp(
                        1600f,
                        3000f,
                        aggression
                    );
            }


            float targetDamageRatio =
     1f -
     goal.targetUnit.DurabilityRatio;


            // Damaged targets become increasingly attractive,
            // independent of their configured maximum health.
            score +=
                targetDamageRatio *
                200f *
                Mathf.Lerp(
                    1.0f,
                    2.5f,
                    aggression
                );


            if (goal.targetUnit.IsCarryingBox)
            {
                score += 1200f;
            }
        }


        return score;
    }


    // =========================================================
    // TARGET SEARCH
    // =========================================================

    private PlayerUnit FindBestAttackableTarget(
    PlayerUnit attacker
)
    {
        if (attacker == null)
            return null;


        List<Vector2Int> adjacentTiles =
            map.GetAdjacentTileCoords(
                attacker.currentTileCoord
            );


        PlayerUnit bestTarget =
            null;

        float bestScore =
            float.NegativeInfinity;


        foreach (
            PlayerUnit target
            in turnManager.players
        )
        {
            if (!IsLivingUnit(target))
                continue;

            if (target.teamID ==
                attacker.teamID)
            {
                continue;
            }


            bool sameHex =
                target.currentTileCoord ==
                attacker.currentTileCoord;


            bool adjacentHex =
                adjacentTiles.Contains(
                    target.currentTileCoord
                );


            if (!sameHex &&
                !adjacentHex)
            {
                continue;
            }


            float score =
                ScoreAttackTarget(
                    target
                );


            if (score > bestScore)
            {
                bestScore =
                    score;

                bestTarget =
                    target;
            }
        }


        return bestTarget;
    }


    private float ScoreAttackTarget(
    PlayerUnit target
)
    {
        if (target == null)
            return float.NegativeInfinity;


        float score =
            100f;


        // AI Core carrier remains the highest-priority
        // combat target.
        if (target.IsCarryingBox)
        {
            score +=
                300f;
        }


        // Continue coordinated pressure.
        if (target ==
            focusTarget)
        {
            score +=
                120f;
        }


        // Only reward a broken shield if this unit
        // actually has a shield system.
        if (target.MaxShield > 0 &&
            target.shield <= 0)
        {
            score +=
                90f;
        }


        // Energy vulnerability is percentage-based.
        if (target.EnergyRatio <= 0.50f)
        {
            score +=
                120f;
        }


        // 0.0 = untouched
        // 1.0 = completely depleted
        float damageRatio =
            1f -
            target.DurabilityRatio;


        score +=
            damageRatio *
            250f;


        return score;
    }


    private void ValidateFocusTarget(
        PlayerUnit attacker
    )
    {
        if (focusTarget == null)
            return;


        if (!IsLivingUnit(
                focusTarget
            ))
        {
            focusTarget =
                null;

            return;
        }


        if (attacker != null &&
            focusTarget.teamID ==
            attacker.teamID)
        {
            focusTarget =
                null;
        }
    }

    private PlayerUnit FindHostileCarrier(
        PlayerUnit unit
    )
    {
        foreach (
            PlayerUnit other
            in turnManager.players
        )
        {
            if (!IsLivingUnit(other))
                continue;

            if (other.teamID ==
                unit.teamID)
            {
                continue;
            }

            if (other.IsCarryingBox)
                return other;
        }


        return null;
    }


    private PlayerUnit FindAlliedCarrier(
        PlayerUnit unit
    )
    {
        foreach (
            PlayerUnit other
            in turnManager.players
        )
        {
            if (!IsLivingUnit(other))
                continue;

            if (other.teamID !=
                unit.teamID)
            {
                continue;
            }

            if (other.IsCarryingBox)
                return other;
        }


        return null;
    }


    private PlayerUnit FindWeakestHostile(
        PlayerUnit unit
    )
    {
        PlayerUnit best =
            null;

        float bestScore =
            float.PositiveInfinity;


        foreach (
            PlayerUnit other
            in turnManager.players
        )
        {
            if (!IsLivingUnit(other))
                continue;

            if (other.teamID ==
                unit.teamID)
            {
                continue;
            }


            float durabilityScore =
     other.DurabilityRatio *
     200f;


            float distance =
                Vector3.Distance(
                    unit.transform.position,
                    other.transform.position
                );


            float score =
                durabilityScore +
                distance * 4f;


            if (score < bestScore)
            {
                bestScore =
                    score;

                best =
                    other;
            }
        }


        return best;
    }


    private PlayerUnit FindNearestHostile(
        Vector3 origin,
        int alliedTeamID
    )
    {
        PlayerUnit best =
            null;

        float bestDistance =
            float.PositiveInfinity;


        foreach (
            PlayerUnit other
            in turnManager.players
        )
        {
            if (!IsLivingUnit(other))
                continue;

            if (other.teamID ==
                alliedTeamID)
            {
                continue;
            }


            float distance =
                Vector3.Distance(
                    origin,
                    other.transform.position
                );


            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                best =
                    other;
            }
        }


        return best;
    }


    private WorldBox FindNearestFreeCore(
        Vector3 origin
    )
    {
        WorldBox[] boxes =
            FindObjectsByType<WorldBox>(
                FindObjectsSortMode.None
            );


        WorldBox best =
            null;

        float bestDistance =
            float.PositiveInfinity;


        foreach (
            WorldBox box
            in boxes
        )
        {
            if (box == null ||
                !box.gameObject.activeInHierarchy)
            {
                continue;
            }


            Vector3 position =
                box.visualRoot != null
                    ? box.visualRoot.position
                    : box.transform.position;


            float distance =
                Vector3.Distance(
                    origin,
                    position
                );


            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                best =
                    box;
            }
        }


        return best;
    }


    private bool IsLivingUnit(
        PlayerUnit unit
    )
    {
        return
            unit != null &&
            !unit.IsDead &&
            unit.gameObject.activeInHierarchy;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void Log(
        PlayerUnit unit,
        string decision,
        string detail
    )
    {
        if (!verboseLogging)
            return;


        string unitName =
            unit != null
                ? unit.name
                : "UNKNOWN";


        Debug.Log(
            $"<color=#FF7043><b>[ENEMY AI]</b></color> " +
            $"<b>{unitName}</b> | " +
            $"{decision} | " +
            detail
        );
    }
}