using System.Collections.Generic;
using UnityEngine;

public class TeamRegroupController : MonoBehaviour
{
    private bool blueRegroupActive = false;
    private bool redRegroupActive = false;


    private void Update()
    {
        if (TurnManager.Instance == null)
            return;

        CheckTeamRegroup(
            1,
            ref blueRegroupActive,
            "BLUE"
        );

        CheckTeamRegroup(
            2,
            ref redRegroupActive,
            "RED"
        );
    }


    private void CheckTeamRegroup(
        int teamID,
        ref bool regroupActive,
        string teamName
    )
    {
        List<PlayerUnit> teamUnits =
            new List<PlayerUnit>();

        foreach (PlayerUnit unit in
                 TurnManager.Instance.players)
        {
            if (unit == null)
                continue;

            if (unit.teamID != teamID)
                continue;

            teamUnits.Add(unit);
        }

        // The rule requires the complete 4-unit team.
        if (teamUnits.Count != 4)
        {
            regroupActive = false;
            return;
        }


        // Every team member must be alive and stationary.
        foreach (PlayerUnit unit in teamUnits)
        {
            if (unit.IsDead ||
                unit.IsMoving)
            {
                regroupActive = false;
                return;
            }
        }


        Vector2Int regroupHex =
            teamUnits[0].currentTileCoord;

        // All four must occupy the SAME logical hex.
        foreach (PlayerUnit unit in teamUnits)
        {
            if (unit.currentTileCoord != regroupHex)
            {
                regroupActive = false;
                return;
            }
        }


        // They were already grouped.
        // Do not repeatedly heal every frame.
        if (regroupActive)
            return;

        regroupActive = true;


        bool restoredAnyShield = false;

        foreach (PlayerUnit unit in teamUnits)
        {
            if (unit.RestoreShieldToFull())
            {
                restoredAnyShield = true;
            }
        }

        // NEW — AUDIO + VISUAL REGROUP FEEDBACK
        TriggerRegroupFeedback(
            teamID,
            teamUnits
        );

        Debug.Log(
            $"[TEAM REGROUP] {teamName} TEAM regrouped at " +
            $"{regroupHex} | Shield restoration triggered."
        );


        if (restoredAnyShield &&
            SubsystemFeedbackUI.Instance != null)
        {
            SubsystemFeedbackUI.Instance.ShowMessage(
                $"<b><size=110%>HEXA_766 // TEAM REGROUP</size></b>\n" +
                $"<b>{teamName} COHESION LINK ESTABLISHED</b>\n" +
                $"SHIELDS RESTORED  //  HEX {regroupHex}"
            );
        }
    }

    [ContextMenu("DEBUG — Dump Team Regroup State")]
    private void DebugDumpRegroupState()
    {
        if (TurnManager.Instance == null)
        {
            Debug.Log(
                "[REGROUP DEBUG] TurnManager.Instance = NULL"
            );

            return;
        }

        Debug.Log(
            $"[REGROUP DEBUG] Registered players = " +
            $"{TurnManager.Instance.players.Count}"
        );

        foreach (PlayerUnit unit in
                 TurnManager.Instance.players)
        {
            if (unit == null)
            {
                Debug.Log(
                    "[REGROUP DEBUG] NULL PlayerUnit"
                );

                continue;
            }

            Debug.Log(
                $"[REGROUP DEBUG] {unit.name} | " +
                $"Team={unit.teamID} | " +
                $"Unit={unit.unitID} | " +
                $"Tile={unit.currentTileCoord} | " +
                $"Vertex={unit.currentVertexIndex} | " +
                $"Dead={unit.IsDead} | " +
                $"Moving={unit.IsMoving} | " +
                $"Shield={unit.shield} | " +
                $"Active={unit.gameObject.activeInHierarchy}"
            );
        }

    }

    private void TriggerRegroupFeedback(
    int teamID,
    List<PlayerUnit> teamUnits
)
    {
        if (TeamRegroupFeedback.Instance == null)
            return;

        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (PlayerUnit unit in teamUnits)
        {
            if (unit == null)
                continue;

            center += unit.transform.position;
            count++;
        }

        if (count > 0)
        {
            center /= count;
        }

        TeamRegroupFeedback.Instance.ShowTeamRegroup(
            teamID,
            teamUnits,
            center
        );
    }
}