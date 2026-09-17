using System.Collections.Generic;
using UnityEngine;

public class TeamController : MonoBehaviour
{
    [Header("TEAM IDENTITY")]
    [SerializeField]
    private int teamID = 1;

    [SerializeField]
    private string teamName = "Blue";

    [Header("TEAM UNITS")]
    [SerializeField]
    private List<PlayerUnit> units =
        new List<PlayerUnit>();

    public int TeamID => teamID;

    public string TeamName => teamName;

    public IReadOnlyList<PlayerUnit> Units =>
        units;

    private void Start()
    {
        RefreshUnits();
    }

    [ContextMenu("Refresh Team Units")]
    public void RefreshUnits()
    {
        units.Clear();

        PlayerUnit[] allUnits =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        foreach (PlayerUnit unit in allUnits)
        {
            if (unit == null)
                continue;

            if (unit.teamID != teamID)
                continue;

            units.Add(unit);

            // NEW:
            // Tell the unit which TeamController owns it.
            unit.AssignTeam(this);
        }

        units.Sort(
            (a, b) =>
                a.unitID.CompareTo(b.unitID)
        );

        Debug.Log(
            $"[TEAM] {teamName} refreshed | " +
            $"TeamID={teamID} | " +
            $"Units={units.Count}"
        );

        foreach (PlayerUnit unit in units)
        {
            Debug.Log(
                $"[TEAM] {teamName} contains " +
                $"{unit.name} | UnitID={unit.unitID}"
            );
        }
    }

    public void RegisterUnit(
        PlayerUnit unit)
    {
        if (unit == null)
            return;

        if (unit.teamID != teamID)
            return;

        if (units.Contains(unit))
        {
            unit.AssignTeam(this);
            return;
        }

        units.Add(unit);

        unit.AssignTeam(this);

        units.Sort(
            (a, b) =>
                a.unitID.CompareTo(b.unitID)
        );

        Debug.Log(
            $"[TEAM] {unit.name} registered to " +
            $"{teamName} | TeamID={teamID}"
        );
    }

    public void UnregisterUnit(
        PlayerUnit unit)
    {
        if (unit == null)
            return;

        if (!units.Contains(unit))
            return;

        units.Remove(unit);

        Debug.Log(
            $"[TEAM] {unit.name} removed from " +
            $"{teamName} | TeamID={teamID}"
        );
    }

    public bool ContainsUnit(
        PlayerUnit unit)
    {
        return units.Contains(unit);
    }
}