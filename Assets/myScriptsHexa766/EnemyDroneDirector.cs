using System.Collections;
using UnityEngine;

public class EnemyDroneDirector : MonoBehaviour
{
    [Header("DRONE HIVE")]
    [SerializeField]
    private EnemyDrone dronePrefab;

    [SerializeField]
    private int maxActiveDrones = 3;

    public bool IsRunning { get; private set; }
    
    public IEnumerator ExecuteEnemyPhaseAndWait()
    {
        if (IsRunning)
        {
            Debug.Log(
                "[ENEMY PHASE] Already running."
            );

            yield break;
        }

        IsRunning = true;

        EnemyDrone[] drones =
            FindObjectsByType<EnemyDrone>(
                FindObjectsSortMode.None
            );

        Debug.Log(
            $"[ENEMY PHASE] Starting | " +
            $"Active drones={drones.Length}"
        );

        foreach (EnemyDrone drone in drones)
        {
            if (drone == null ||
                !drone.isActiveAndEnabled)
            {
                continue;
            }

            Debug.Log(
                $"[ENEMY PHASE] Activating {drone.name}"
            );

            yield return
                drone.ExecuteActionAndWait();
        }

        IsRunning = false;

        Debug.Log(
            "[ENEMY PHASE] Complete."
        );
    }

    [ContextMenu("TEST - Spawn Drone")]
    private void TestSpawnDrone()
    {
        SpawnDrone();
    }

    private void SpawnDrone()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ENEMY HIVE] Drone spawning only works in Play Mode."
            );

            return;
        }

        if (dronePrefab == null)
        {
            Debug.LogError(
                "[ENEMY HIVE] Drone Prefab is not assigned."
            );

            return;
        }

        EnemyDrone[] activeDrones =
            FindObjectsByType<EnemyDrone>(
                FindObjectsSortMode.None
            );

        if (activeDrones.Length >= maxActiveDrones)
        {
            Debug.Log(
                $"[ENEMY HIVE] Maximum active drones reached " +
                $"({activeDrones.Length}/{maxActiveDrones})."
            );

            return;
        }

        EnemyDrone newDrone =
            Instantiate(
                dronePrefab,
                transform
            );

        newDrone.name =
            $"EnemyDrone_{activeDrones.Length + 1}";

        Debug.Log(
            $"[ENEMY HIVE] Spawned {newDrone.name} | " +
            $"Active drones={activeDrones.Length + 1}/" +
            $"{maxActiveDrones}"
        );
    }
}