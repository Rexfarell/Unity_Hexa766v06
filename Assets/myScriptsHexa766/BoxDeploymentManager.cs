using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class BoxDeploymentManager : MonoBehaviour
{
    public static BoxDeploymentManager Instance { get; private set; }

    [Header("CURRENT OBJECTIVE BOX")]
    [SerializeField]
    private WorldBox objectiveBox;


    [Header("INITIAL OBJECTIVE")]
    [SerializeField]
    private WorldBox initialObjectiveBox;

    private Map1HexGrid map;

    // Safe fallback: the location that already worked.
    private Vector3 startingWorldPosition;
    private int startingTileX;
    private int startingTileY;
    private int startingVertexIndex;

    private bool startingLocationStored = false;


    private struct BoxSpawnCandidate
    {
        public Vector3 worldPosition;
        public string tileName;
        public int vertexIndex;
    }


    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        map = FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
        {
            Debug.LogError(
                "[BOX DEPLOYMENT] Map1HexGrid not found."
            );

            yield break;
        }

        if (objectiveBox == null)
        {
            Debug.LogWarning(
                "[BOX DEPLOYMENT] Objective Box is not assigned."
            );

            yield break;
        }

        // Preserve the already-working original position as fallback.
        startingWorldPosition =
            objectiveBox.transform.position;

        startingTileX =
            objectiveBox.tileX;

        startingTileY =
            objectiveBox.tileY;

        startingVertexIndex =
            objectiveBox.vertexIndex;

        startingLocationStored = true;

        Debug.Log(
            $"[BOX DEPLOYMENT] Objective box registered: " +
            $"{objectiveBox.name}"
        );

        // Wait until the automatically generated map is ready.
        Debug.Log(
            "[BOX DEPLOYMENT] Waiting for map generation..."
        );

        while (!map.hasGenerated)
        {
            yield return null;
        }

        // Important:
        // Allow WorldBox.Start() and the other scene Start() methods
        // to finish before writing the objective's final logical location.
        yield return null;

        Debug.Log(
            "[BOX DEPLOYMENT] Map ready. Deploying objective box."
        );

        RedeployDeliveredBox(objectiveBox);
    }



    public void RedeployDeliveredBox(
        WorldBox deliveredBox
    )
    {
        if (deliveredBox == null)
            return;

        objectiveBox = deliveredBox;

        if (map == null)
        {
            map = FindFirstObjectByType<Map1HexGrid>();
        }

        if (map == null ||
            !map.hasGenerated ||
            map.vertexToTile == null ||
            map.vertexToTile.Count == 0)
        {
            Debug.LogWarning(
                "[BOX DEPLOYMENT] Map graph unavailable. " +
                "Using original box location."
            );

            RedeployAtStartingLocation();
            return;
        }

        List<BoxSpawnCandidate> candidates =
            new List<BoxSpawnCandidate>();

        foreach (var pair in map.vertexToTile)
        {
            Vector3 worldPosition = pair.Key;

            string tileName =
                pair.Value.tileName;

            int vertexIndex =
                pair.Value.vertexIndex;

            // Do not deploy the objective on the AI Station.
            if (tileName == "AIStation")
                continue;

            Vector2Int tileCoord =
                map.GetTileGridCoord(tileName);

            // Do not place the box directly underneath a player.
            if (TurnManager.Instance != null &&
                TurnManager.Instance.IsVertexOccupied(
                    tileCoord,
                    vertexIndex
                ))
            {
                continue;
            }

            // Do not redeploy anywhere on the same hex tile.
            if (tileCoord.x == objectiveBox.tileX &&
                tileCoord.y == objectiveBox.tileY)
            {
                continue;
            }

            // Only use vertices that actually belong to the graph.
            if (!map.vertexGraph.TryGetValue(
                    worldPosition,
                    out List<Vector3> connections
                ))
            {
                continue;
            }

            if (connections == null ||
                connections.Count == 0)
            {
                continue;
            }

            BoxSpawnCandidate candidate =
                new BoxSpawnCandidate
                {
                    worldPosition = worldPosition,
                    tileName = tileName,
                    vertexIndex = vertexIndex
                };

            candidates.Add(candidate);
        }


        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                "[BOX DEPLOYMENT] No valid new location found. " +
                "Using original box location."
            );

            RedeployAtStartingLocation();
            return;
        }


        int randomIndex =
            Random.Range(
                0,
                candidates.Count
            );

        BoxSpawnCandidate selected =
            candidates[randomIndex];

        Vector2Int selectedCoord =
            map.GetTileGridCoord(
                selected.tileName
            );


        objectiveBox.transform.position =
            selected.worldPosition;

        objectiveBox.tileX =
            selectedCoord.x;

        objectiveBox.tileY =
            selectedCoord.y;

        objectiveBox.vertexIndex =
            selected.vertexIndex;

        objectiveBox.WorldVertexPosition =
            selected.worldPosition;

        objectiveBox.gameObject.SetActive(true);


        Debug.Log(
            $"[BOX DEPLOYMENT] {objectiveBox.name} redeployed → " +
            $"{selected.tileName} | " +
            $"Tile {selectedCoord} | " +
            $"Vertex {selected.vertexIndex}"
        );
    }


    private void RedeployAtStartingLocation()
    {
        if (!startingLocationStored ||
            objectiveBox == null)
        {
            return;
        }

        objectiveBox.transform.position =
            startingWorldPosition;

        objectiveBox.tileX =
            startingTileX;

        objectiveBox.tileY =
            startingTileY;

        objectiveBox.vertexIndex =
            startingVertexIndex;

        objectiveBox.WorldVertexPosition =
            startingWorldPosition;

        objectiveBox.gameObject.SetActive(true);

        Debug.Log(
            $"[BOX DEPLOYMENT] {objectiveBox.name} " +
            "redeployed at original fallback location."
        );
    }
}