using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Callbacks;
[DefaultExecutionOrder(-100)]  // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
public class Map1HexGrid : MonoBehaviour

{
    void OnEnable()
    {
        Debug.Log($"[MAP] OnEnable() called — hasGenerated = {hasGenerated}");
    }

    void Start()
    {
        if (!hasGenerated)
        {
            Debug.Log("[MAP] Start() — generating map now...");
            GenerateAndPosition();
        }
    }

    
    private const float GROUND_Y = 0.0f;
    private const float PLAYER_MODEL_PIVOT_TO_FEET = 0.0f;
    private const float PLAYER_GROUND_CLEARANCE = 0.00f;
    private float PlayerTargetY => GROUND_Y + PLAYER_MODEL_PIVOT_TO_FEET + PLAYER_GROUND_CLEARANCE; [Header("BASE SETTINGS")]
    public float hexSize = 1f;
    public float hexHeight = 0.1f;
    [Range(0f, 0.3f)]
    public float gapSize = 0.05f;

    [HideInInspector] public bool hasGenerated = false;
    [Header("TILE MATERIALS")]
    public Material hexMaterial;
    public Material centerMaterial;

    [Header("HOME BASE MATERIALS")]
    public Material homeBase1Material;
    public Material homeBase2Material;

    [Header("PYRAMID STATION MATERIAL")]
    public Material pyramidWallMaterial;

    [Header("AI STATION MATERIAL")]
    public Material aiStationMaterial;

    [Header("PATHFINDING SETTINGS")]
    public Material reachableMaterial;
    public int defaultMovePoints = 5;
    public bool showReachableTiles = false;

    [Header("VERTEX MARKER SETTINGS")]
    public GameObject vertexMarkerPrefab;
    public Material vertexMarkerMaterial;
    public Color reachableVertexColor = Color.cyan;
    public Color selectedVertexColor = Color.yellow;

    [Header("PYRAMID SETTINGS")]
    public float wallHeight = 1.5f;
    public float wallThickness = 0.15f;
    public float wallLength = 0.8f;
    public float platformRadius = 0.7f;
    public bool makeWallsTransparent = true;

    [Header("MINIMAL AI STATION SETTINGS")]
    [Range(0.3f, 1.5f)]
    public float aiStationRadius = 0.7f;
    public float aiStationHeight = 1.8f;
    public float aiStationWallThickness = 0.15f;

    [Header("FALLBACK COLORS")]
    public Color homeBase1Color = Color.red;
    public Color homeBase2Color = Color.blue;
    public Color pyramidColor = Color.green;
    public Color aiStationColor = Color.yellow;

    [Header("EDITOR CONTROL")]
    public bool allowEditModeGeneration = true;

    [Header("PLAYER SETUP")]
    public GameObject Player1;
    public GameObject Player2;

    [Header("CUSTOM PLAYER PLACEMENT")]
    public TileSelection player1Tile = TileSelection.OriginBlue;
    [Range(0, 5)]
    public int player1VertexIndex = 0;

    [Header("PLAYER 2 PLACEMENT")]
    public TileSelection player2Tile = TileSelection.OriginRed;
    [Range(0, 5)]
    public int player2VertexIndex = 0;

    [SerializeField, HideInInspector] private int _player1TileBackup;
    [SerializeField, HideInInspector] private int _player2TileBackup;

    public enum TileSelection
    {
        OriginRed, OriginBlue, PyramidCenter, AIStation,
        Tile00, Tile01, Tile02, Tile03, Tile04, Tile05, Tile06, Tile07, Tile08, Tile09,
        Tile10, Tile11, Tile12, Tile13, Tile14, Tile15, Tile16, Tile17, Tile18, Tile19,
        Tile20, Tile21, Tile22, Tile23, Tile24, Tile25, Tile26, Tile27, Tile28, Tile29,
        Tile30, Tile31, Tile32, Tile33,
        NNW1, SSE1
    }

    private Dictionary<TileSelection, string> tileNameMap = new Dictionary<TileSelection, string>()
{
    { TileSelection.PyramidCenter, "PyramidCenter" },
    { TileSelection.AIStation, "AIStation" },
    { TileSelection.Tile00, "Tile00" }, { TileSelection.Tile01, "Tile01" }, { TileSelection.Tile02, "Tile02" },
    { TileSelection.Tile03, "Tile03" }, { TileSelection.Tile04, "Tile04" }, { TileSelection.Tile05, "Tile05" },
    { TileSelection.Tile06, "Tile06" }, { TileSelection.Tile07, "Tile07" }, { TileSelection.Tile08, "Tile08" },
    { TileSelection.Tile09, "Tile09" }, { TileSelection.Tile10, "Tile10" }, { TileSelection.Tile11, "Tile11" },
    { TileSelection.Tile12, "Tile12" }, { TileSelection.Tile13, "Tile13" }, { TileSelection.Tile14, "Tile14" },
    { TileSelection.Tile15, "Tile15" }, { TileSelection.Tile16, "Tile16" }, { TileSelection.Tile17, "Tile17" },
    { TileSelection.Tile18, "Tile18" }, { TileSelection.Tile19, "Tile19" }, { TileSelection.Tile20, "Tile20" },
    { TileSelection.Tile21, "Tile21" }, { TileSelection.Tile22, "Tile22" }, { TileSelection.Tile23, "Tile23" },
    { TileSelection.Tile24, "Tile24" }, { TileSelection.Tile25, "Tile25" }, { TileSelection.Tile26, "Tile26" },
    { TileSelection.Tile27, "Tile27" }, { TileSelection.Tile28, "Tile28" }, { TileSelection.Tile29, "Tile29" },
    { TileSelection.Tile30, "Tile30" }, { TileSelection.Tile31, "Tile31" }, { TileSelection.Tile32, "Tile32" },
    { TileSelection.Tile33, "Tile33" },
    { TileSelection.OriginRed, "OriginRed" },
    { TileSelection.OriginBlue, "OriginBlue" },
    { TileSelection.NNW1, "Tile20" },
    { TileSelection.SSE1, "Tile21" }
};

    public Vector2Int TileCoordFromName(string tileName)
    {
        switch (tileName)
        {
            case "OriginRed": return new Vector2Int(0, 0);
            case "OriginBlue": return new Vector2Int(5, 5);
            default:
                Debug.LogWarning($"[MAP] No coord for {tileName}, using (0,0)");
                return new Vector2Int(0, 0);
        }
    }

    private void SetupPathLines()
    {
        if (pathLine == null)
        {
            pathLine = gameObject.AddComponent<LineRenderer>();
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startWidth = pathLine.endWidth = 0.15f;
            pathLine.alignment = LineAlignment.View;
            pathLine.positionCount = 0;
            pathLine.startColor = pathLine.endColor = Color.yellow;
        }

        if (pathLine2 == null)
        {
            pathLine2 = gameObject.AddComponent<LineRenderer>();
            pathLine2.material = new Material(Shader.Find("Sprites/Default"));
            pathLine2.startWidth = pathLine2.endWidth = 0.15f;
            pathLine2.alignment = LineAlignment.View;
            pathLine2.positionCount = 0;
            pathLine2.startColor = pathLine2.endColor = Color.cyan;
        }

        if (pathMaterial != null)
        {
            pathLine.material = pathMaterial;
            pathLine2.material = pathMaterial;
        }
    }
    private List<GameObject> hexTiles = new List<GameObject>();
    private Dictionary<Vector2, GameObject> hexMap = new Dictionary<Vector2, GameObject>();
    public Dictionary<string, GameObject> tileGameObjectMap = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> landmarks = new Dictionary<string, GameObject>();


    private List<GameObject> wallObjects = new List<GameObject>();
    private GameObject platformObject;

    public Dictionary<Vector3, List<Vector3>> vertexGraph = new Dictionary<Vector3, List<Vector3>>();
    public Dictionary<Vector3, (string tileName, int vertexIndex)> vertexToTile = new Dictionary<Vector3, (string, int)>();

    private List<GameObject> vertexMarkers = new List<GameObject>();
    private LineRenderer pathLine;
    private List<Vector3> currentPath = new List<Vector3>();
    private GameObject selectedVertexMarker;
    private bool isMoving = false;

    private LineRenderer pathLine2;
    private List<Vector3> currentPath2 = new List<Vector3>();
    private GameObject selectedVertexMarker2;
    private bool isMoving2 = false;
    public GameObject activePlayer;
    private const float PLAYER_HEIGHT_OFFSET = 0.0f;
    private const float GROUND_Y_CONST = 0.0f;

    [Header("POWER-UP")]
    [SerializeField] private GameObject powerUpPrefab;
    [SerializeField] private bool player1ExtendedReach = false;
    [SerializeField] private bool player2ExtendedReach = false;
    private GameObject powerUpInstance;

    [Header("PATH VISUALS")]
    [SerializeField] private Material pathMaterial;

    public readonly Dictionary<string, Vector2Int> tileNameToGrid = new Dictionary<string, Vector2Int>();

    [ContextMenu("DEBUG: Set Move Points to 1")]
    void SetMovePointsTo1()
    {
        defaultMovePoints = 1;
        Debug.Log("Move points set to 1. Click 'Show 1-Step Reach from Player1'");
    }

    [ContextMenu("Show Reach from Player1")]
    public void ShowReachFromPlayer1()
    {
        if (!hasGenerated)
        {
            Debug.Log("Map not ready — generating once...");
            GenerateAndPosition();   // only runs the first time
            return;
        }

        var pu = Player1?.GetComponentInChildren<PlayerUnit>(true);
        if (pu == null) return;

        var tm = FindObjectOfType<TurnManager>();
        tm?.ShowReachFor(Player1);
    }

    [ContextMenu("Show Reach from Player 2")]
    public void ShowReachFromPlayer2()
    {
        if (!hasGenerated)
        {
            Debug.Log("Map not ready — generating once...");
            GenerateAndPosition();
            return;
        }

        var pu = Player2?.GetComponentInChildren<PlayerUnit>(true);
        if (pu == null) return;

        var tm = FindObjectOfType<TurnManager>();
        tm?.ShowReachFor(Player2);
    }

    [ContextMenu("Generate and Position")]
    public void GenerateAndPosition()
    {
        GameObject preservedPlayer1 = Player1;
        GameObject preservedPlayer2 = Player2;

        ClearAllChildren();
        vertexToTile.Clear();     // ← ADD THIS
        vertexGraph.Clear();      // ← AND THIS
       

        try
        {
            GenerateMap();
            BuildVertexGraph();
            Debug.Log($"[MAP] AFTER BuildVertexGraph — tileGameObjectMap has {tileGameObjectMap.Count} tiles, vertexToTile has {vertexToTile.Count} entries");

            // Players back
            if (preservedPlayer1 != null)
            {
                Player1 = preservedPlayer1;
                PositionPlayer(Player1, player1Tile, player1VertexIndex);
                EnsurePlayerFeedback(Player1);
            }
            if (preservedPlayer2 != null)
            {
                Player2 = preservedPlayer2;
                PositionPlayer(Player2, player2Tile, player2VertexIndex);
                EnsurePlayerFeedback(Player2);
            }

            SetupPathLines();
            BuildVertexGraph();
            hasGenerated = true;
            Debug.Log($"[MAP] SUCCESS — vertexToTile has {vertexToTile.Count} entries");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MAP] FAILED: " + e);
            hasGenerated = false;
        }
    }

    private void EnsurePlayerFeedback(GameObject player)
    {
        if (player == null) return;

        var feedback = player.GetComponent<PlayerClickFeedback>();
        if (feedback == null)
        {
            feedback = player.AddComponent<PlayerClickFeedback>();
            Debug.Log($"[MAP] Added PlayerClickFeedback to {player.name}");
        }

        if (player.GetComponent<Collider>() == null)
        {
            player.AddComponent<CapsuleCollider>();
        }

        if (player.GetComponent<Renderer>() == null)
        {
            Debug.LogWarning($"[MAP] {player.name} has no Renderer!");
        }
    }

    public Vector2Int GetTileGridCoord(string tileName)
    {
        if (tileNameToGrid.TryGetValue(tileName, out Vector2Int coord))
            return coord;

        Debug.LogError($"[MAP] No grid coord for tile: {tileName}");
        return new Vector2Int(0, 0);
    }

    private void PositionPlayer(GameObject player, TileSelection tileSel, int vertexIdx)
    {
        if (player == null) return;

        string tileName = tileNameMap.ContainsKey(tileSel) ? tileNameMap[tileSel] : "OriginBlue";
        Vector3 pos = GetVertexPosition(tileName, vertexIdx);
        pos.y = PlayerTargetY;

        player.transform.position = pos;
        Debug.Log($"[MAP] Positioned {player.name} at {tileName} v{vertexIdx} = {pos}");

        var unit = player.GetComponent<PlayerUnit>();
        if (unit != null)
        {
            Vector2Int gridPos = GetTileGridCoord(tileName);
            unit.currentTileCoord = gridPos;
            unit.currentVertexIndex = vertexIdx;
            unit.ResetTurn();
            Debug.Log($"[MAP] Updated {player.name} unit: {gridPos} v{vertexIdx}");
        }
    }

    [ContextMenu("Position Both Players")]
    void PositionBothPlayers()
    {
        if (!hasGenerated)
        {
            Debug.LogWarning("Map not generated. Use 'Generate and Position' first.");
            return;
        }

        string p1TileName = tileNameMap.ContainsKey(player1Tile) ? tileNameMap[player1Tile] : null;
        string p2TileName = tileNameMap.ContainsKey(player2Tile) ? tileNameMap[player2Tile] : null;

        if (p1TileName == null || !tileGameObjectMap.ContainsKey(p1TileName))
        {
            Debug.LogError($"Tile for Player1 '{player1Tile}' not found or invalid.");
            return;
        }
        if (p2TileName == null || !tileGameObjectMap.ContainsKey(p2TileName))
        {
            Debug.LogError($"Tile for Player2 '{player2Tile}' not found or invalid.");
            return;
        }

        Vector3 p1Target = GetVertexPosition(p1TileName, player1VertexIndex);
        Vector3 p2Target = GetVertexPosition(p2TileName, player2VertexIndex);

        Vector3 p1Pos = RoundToGrid(p1Target);
        Vector3 p2Pos = RoundToGrid(p2Target);

        Debug.Log($"[POSITION CHECK] P1: {p1TileName} V{player1VertexIndex} → {p1Pos}");
        Debug.Log($"[POSITION CHECK] P2: {p2TileName} V{player2VertexIndex} → {p2Pos}");

        if (Vector3.Distance(p1Pos, p2Pos) < 0.01f)
        {
            Debug.LogError("BLOCKED: Both players selected the same vertex! Cannot place.");
            return;
        }

        aPlayerToVertex(Player1, p1TileName, player1VertexIndex);
        aPlayerToVertex(Player2, p2TileName, player2VertexIndex);
    }
    [Header("SOUND")]
    [SerializeField] private AudioClip cyberBurstSound;

    private void PlayBurstSound()
    {
        if (cyberBurstSound != null)
            AudioSource.PlayClipAtPoint(cyberBurstSound, Camera.main.transform.position, 0.7f);
        else
            Debug.Log("[SOUND] CyberBurst clip missing!");
    }
    [ContextMenu("Force Clean Dropdowns")]
    void ForceResetDropdowns()
    {
        player1Tile = TileSelection.OriginBlue;
        player2Tile = TileSelection.OriginRed;
        _player1TileBackup = 0;
        _player2TileBackup = 0;
        Debug.Log("DROPDOWNS FORCIBLY CLEANED — old ghost entries erased.");
    }

    private void RebuildVertexData()
    {
        vertexGraph.Clear();
        vertexToTile.Clear();

        foreach (var tileKvp in tileGameObjectMap)
        {
            string tileName = tileKvp.Key;
            GameObject tile = tileKvp.Value;

            for (int v = 0; v < 6; v++)
            {
                Vector3 vertexPos = GetVertexPosition(tileName, v);
                vertexToTile[RoundToGrid(vertexPos)] = (tileName, v);
            }
        }

        Debug.Log($"[MAP] Vertex data rebuilt: {vertexToTile.Count} vertices");
    }
    [ContextMenu("Clear Map")]
    void ClearMap()
    {
        ClearAllChildren();
        vertexGraph.Clear();
        vertexToTile.Clear();
        ClearPathAndMarkers();
        Debug.Log("Map cleared");
    }

    void Awake()
    {
        tileGameObjectMap = new Dictionary<string, GameObject>();
        landmarks = new Dictionary<string, GameObject>();
        ForceResetDropdowns();

        // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
        // ADD THIS LINE — THE ONLY THING MISSING
        if (!hasGenerated)
            GenerateAndPosition();
        // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            Debug.LogWarning("[EDITOR] Scripts reloaded → Stopping Play Mode to prevent bugs.");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        if (!UnityEditor.EditorApplication.isPlaying)
        {
            var map = UnityEngine.Object.FindObjectOfType<Map1HexGrid>();
            if (map != null)
            {
                map.ForceResetDropdowns();
            }
        }
    }

    void OnValidate()//default placement of players
    {
        if (!System.Enum.IsDefined(typeof(TileSelection), player1Tile))
            player1Tile = TileSelection.OriginBlue;
        if (!System.Enum.IsDefined(typeof(TileSelection), player2Tile))
            player2Tile = TileSelection.OriginRed;
    }

    void CreateMap1()
    {
        CreateHexTile(0, 0, "PyramidCenter", true);
        CreateFirstRing();
        CreateSecondRing();
        CreateAdditionalTiles();
        CreateThirdRing();
        CreateAdditionalTileBetween32And31();
        CreateFourthRing();
        CreateFifthRing();
        CreateSeventhRing();
        CreateTilesNextTo24();
    }

    void ClearAllChildren()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(children[i]);
            else
                DestroyImmediate(children[i]);
        }

        hexTiles.Clear();
        hexMap.Clear();
        tileGameObjectMap.Clear();
        landmarks.Clear();
        wallObjects.Clear();
        platformObject = null;
        powerUpInstance = null;
        hasGenerated = false;

        tileNameToGrid.Clear();
    }

    void CreateFirstRing()
    {
        float xOffset = 1.5f * (hexSize + gapSize);
        float zOffset = Mathf.Sqrt(3f) / 2f * (hexSize + gapSize);

        Vector2[] positions = {
        new Vector2(xOffset, zOffset),
        new Vector2(0, Mathf.Sqrt(3f) * (hexSize + gapSize)),
        new Vector2(-xOffset, zOffset),
        new Vector2(-xOffset, -zOffset),
        new Vector2(0, -Mathf.Sqrt(3f) * (hexSize + gapSize)),
        new Vector2(xOffset, -zOffset)
    };

        string[] names = { "Tile00", "Tile01", "Tile02", "Tile03", "Tile04", "Tile05" };

        for (int i = 0; i < positions.Length; i++)
        {
            CreateHexTile(positions[i].x, positions[i].y, names[i], false);
        }
    }

    void CreateSecondRing()
    {
        CreateHexTile(-3.0f * (hexSize + gapSize), 0f, "Tile06", false);
        CreateHexTile(-1.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile07", false);
        CreateHexTile(1.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile08", false);
        CreateHexTile(3.0f * (hexSize + gapSize), 0f, "Tile09", false);
        CreateHexTile(1.5f * (hexSize + gapSize), -Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile10", false);
        CreateHexTile(-1.5f * (hexSize + gapSize), -Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile11", false);
        CreateHexTile(-3.0f * (hexSize + gapSize), Mathf.Sqrt(3f) * (hexSize + gapSize), "Tile12", false);
        CreateHexTile(3.0f * (hexSize + gapSize), Mathf.Sqrt(3f) * (hexSize + gapSize), "Tile13", false);
        CreateHexTile(3.0f * (hexSize + gapSize), -Mathf.Sqrt(3f) * (hexSize + gapSize), "Tile14", false);
        CreateHexTile(-3.0f * (hexSize + gapSize), -Mathf.Sqrt(3f) * (hexSize + gapSize), "Tile15", false);
        CreateHexTile(-4.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile16", false);
        CreateHexTile(4.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile17", false);
    }

    void CreateAdditionalTiles()
    {
        CreateHexTile(-4.5f * (hexSize + gapSize), -Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile18", false);
        CreateHexTile(4.5f * (hexSize + gapSize), -Mathf.Sqrt(3f) * 1.5f * (hexSize + gapSize), "Tile19", false);
        CreateHexTile(-5.0f * (hexSize + gapSize), 0f, "Tile20", false);
        CreateHexTile(5.0f * (hexSize + gapSize), 0f, "Tile21", false);
        CreateHexTile(10.0f * (hexSize + gapSize) - 0.2f, 0f, "OriginBlue", false);
        CreateHexTile(-10.0f * (hexSize + gapSize), 0f, "OriginRed", false);
    }

    void CreateThirdRing()
    {
        CreateHexTile(-7.0f * (hexSize + gapSize), 0f, "Tile22", false);
        CreateHexTile(-4.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 2.53f * (hexSize + gapSize), "Tile23", false);
        CreateHexTile(-1.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 2.53f * (hexSize + gapSize), "Tile24", false);
        CreateHexTile(1.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 2.53f * (hexSize + gapSize), "Tile25", false);
        CreateHexTile(4.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 2.53f * (hexSize + gapSize), "Tile26", false);
        CreateHexTile(6.8f * (hexSize + gapSize), 0f, "Tile27", false);
    }

    void CreateAdditionalTileBetween32And31()
    {
        CreateHexTile(0f, -Mathf.Sqrt(3f) * 2.025f * (hexSize + gapSize), "Tile28", false);
    }

    void CreateFourthRing()
    {
        CreateHexTile(8.3f * (hexSize + gapSize), Mathf.Sqrt(3f) * 0.5f * (hexSize + gapSize), "Tile29", false);
        CreateHexTile(8.3f * (hexSize + gapSize), -Mathf.Sqrt(3f) * 0.5f * (hexSize + gapSize), "Tile30", false);
    }

    void CreateFifthRing()
    {
        CreateHexTile(0f, -Mathf.Sqrt(3f) * 3.025f * (hexSize + gapSize), "Tile31", false);
    }

    void CreateSeventhRing()
    {
        CreateHexTile(0f, -Mathf.Sqrt(3f) * 4.025f * (hexSize + gapSize), "AIStation", false);
    }

    void CreateTilesNextTo24()
    {
        CreateHexTile(-8.5f * (hexSize + gapSize), Mathf.Sqrt(3f) * 0.5f * (hexSize + gapSize), "Tile32", false);
        CreateHexTile(-8.5f * (hexSize + gapSize), -Mathf.Sqrt(3f) * 0.5f * (hexSize + gapSize), "Tile33", false);
    }

    void CreateLandmarks()
    {
        CreatePyramidStation(0, 0);
        CreateHomeBase("HomeBase_Red", "OriginRed", homeBase2Material, homeBase2Color);
        CreateHomeBase("HomeBase_Blue", "OriginBlue", homeBase1Material, homeBase1Color);
        CreateMinimalAIStation("A.I. Station", "AIStation", aiStationMaterial, aiStationColor);
    }

    void CreateHomeBase(string name, string tileName, Material material, Color fallbackColor)
    {
        if (!tileGameObjectMap.ContainsKey(tileName))
        {
            Debug.LogError("Tile " + tileName + " not found!");
            return;
        }

        GameObject tile = tileGameObjectMap[tileName];
        Vector3 position = tile.transform.position;

        GameObject homeBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        homeBase.name = name;
        homeBase.transform.SetParent(transform);
        homeBase.transform.position = new Vector3(position.x, 1f, position.z);
        homeBase.transform.localScale = new Vector3(hexSize * 1.2f, 0.5f, hexSize * 1.2f);

        Renderer renderer = homeBase.GetComponent<Renderer>();
        if (material != null)
            renderer.sharedMaterial = material;
        else
            renderer.sharedMaterial = CreateEnhancedMaterial(fallbackColor, true);

        landmarks[name] = homeBase;
    }

    void CreatePyramidStation(float x, float z)
    {
        Vector3 position = new Vector3(x, 0, z);
        GameObject pyramidStation = new GameObject("PyramidCenter");
        pyramidStation.transform.SetParent(transform);
        pyramidStation.transform.position = position;

        platformObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platformObject.name = "PyramidPlatform";
        platformObject.transform.SetParent(pyramidStation.transform);
        platformObject.transform.position = position + new Vector3(0, 0.1f, 0);
        platformObject.transform.localScale = new Vector3(hexSize * platformRadius, 0.1f, hexSize * platformRadius);

        Renderer platformRenderer = platformObject.GetComponent<Renderer>();
        if (pyramidWallMaterial != null)
            platformRenderer.sharedMaterial = pyramidWallMaterial;
        else
            platformRenderer.sharedMaterial = CreateEnhancedMaterial(pyramidColor, true);

        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            float wallDistance = hexSize * 0.6f;
            Vector3 wallPosition = position + new Vector3(
                Mathf.Cos(angle) * wallDistance,
                wallHeight / 2f,
                Mathf.Sin(angle) * wallDistance
            );

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "PyramidWall_" + i;
            wall.transform.SetParent(pyramidStation.transform);
            wall.transform.position = wallPosition;
            wall.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90f, 0);
            wall.transform.localScale = new Vector3(wallThickness, wallHeight, hexSize * wallLength);

            Renderer wallRenderer = wall.GetComponent<Renderer>();
            if (pyramidWallMaterial != null)
                wallRenderer.sharedMaterial = pyramidWallMaterial;
            else
                wallRenderer.sharedMaterial = CreateEnhancedMaterial(pyramidColor, false);

            wallObjects.Add(wall);
        }

        landmarks["PyramidCenter"] = pyramidStation;
    }

    void CreateMinimalAIStation(string name, string tileName, Material material, Color fallbackColor)
    {
        if (!tileGameObjectMap.ContainsKey(tileName))
        {
            Debug.LogError("Tile " + tileName + " not found!");
            return;
        }

        GameObject tile = tileGameObjectMap[tileName];
        Vector3 position = tile.transform.position;
        Vector3 pyramidPosition = landmarks.ContainsKey("PyramidCenter")
            ? landmarks["PyramidCenter"].transform.position
            : Vector3.zero;

        GameObject aiStation = new GameObject(name);
        aiStation.transform.SetParent(transform);
        aiStation.transform.position = position;

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "AIStationPlatform";
        platform.transform.SetParent(aiStation.transform);
        platform.transform.position = position + new Vector3(0, 0.05f, 0);
        platform.transform.localScale = new Vector3(hexSize * aiStationRadius * 0.8f, 0.05f, hexSize * aiStationRadius * 0.8f);

        Vector3 directionToPyramid = (pyramidPosition - position).normalized;
        float angleToPyramid = Mathf.Atan2(directionToPyramid.x, directionToPyramid.z) * Mathf.Rad2Deg;
        float wallDistance = hexSize * aiStationRadius * 0.6f;

        for (int i = 0; i < 4; i++)
        {
            float angle = (90f * i) * Mathf.Deg2Rad;
            float currentWallAngle = (angleToPyramid + 180f) % 360f;
            float wallAngleDegrees = i * 90f;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(wallAngleDegrees, currentWallAngle));

            if (angleDiff < 45f) continue;

            Vector3 wallPosition = position + new Vector3(
                Mathf.Cos(angle) * wallDistance,
                aiStationHeight / 2f,
                Mathf.Sin(angle) * wallDistance
            );

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "AIStationWall_" + i;
            wall.transform.SetParent(aiStation.transform);
            wall.transform.position = wallPosition;
            wall.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            wall.transform.localScale = new Vector3(aiStationWallThickness, aiStationHeight, hexSize * aiStationRadius * 0.5f);

            Renderer wallRenderer = wall.GetComponent<Renderer>();
            if (material != null)
                wallRenderer.sharedMaterial = material;
            else
                wallRenderer.sharedMaterial = CreateEnhancedMaterial(fallbackColor, false);
        }

        for (int i = 0; i < 4; i++)
        {
            float angle = (90f * i) * Mathf.Deg2Rad;
            float pillarDistance = hexSize * aiStationRadius * 0.7f;

            Vector3 pillarPosition = position + new Vector3(
                Mathf.Cos(angle) * pillarDistance,
                aiStationHeight / 2f,
                Mathf.Sin(angle) * pillarDistance
            );

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "AIStationPillar_" + i;
            pillar.transform.SetParent(aiStation.transform);
            pillar.transform.position = pillarPosition;
            pillar.transform.localScale = new Vector3(
                aiStationWallThickness * 1.5f,
                aiStationHeight * 1.1f,
                aiStationWallThickness * 1.5f
            );

            Renderer pillarRenderer = pillar.GetComponent<Renderer>();
            if (material != null)
                pillarRenderer.sharedMaterial = material;
            else
                pillarRenderer.sharedMaterial = CreateEnhancedMaterial(fallbackColor, false);
        }

        float entranceAngle = angleToPyramid * Mathf.Deg2Rad;
        float entranceMarkerDistance = hexSize * aiStationRadius * 0.6f;

        for (int i = -1; i <= 1; i += 2)
        {
            float markerAngle = entranceAngle + (i * 30f * Mathf.Deg2Rad);
            Vector3 markerPosition = position + new Vector3(
                Mathf.Cos(markerAngle) * entranceMarkerDistance,
                aiStationHeight * 0.3f,
                Mathf.Sin(markerAngle) * entranceMarkerDistance
            );

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "EntranceMarker_" + (i == -1 ? "Left" : "Right");
            marker.transform.SetParent(aiStation.transform);
            marker.transform.position = markerPosition;
            marker.transform.localScale = new Vector3(
                aiStationWallThickness * 0.8f,
                aiStationHeight * 0.4f,
                aiStationWallThickness * 0.8f
            );

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (material != null)
                markerRenderer.sharedMaterial = material;
            else
                markerRenderer.sharedMaterial = CreateEnhancedMaterial(fallbackColor, true);
        }

        Renderer platformRenderer = platform.GetComponent<Renderer>();
        if (material != null)
            platformRenderer.sharedMaterial = material;
        else
            platformRenderer.sharedMaterial = CreateEnhancedMaterial(fallbackColor, false);

        landmarks[name] = aiStation;
    }

    void CreateHexTile(float x, float z, string name, bool isCenter)
    {
        Vector3 position = new Vector3(x, 0, z);
        Vector2 key = new Vector2(x, z);

        if (hexMap.ContainsKey(key))
        {
            Debug.LogWarning("Position " + key + " already occupied by " + hexMap[key].name);
            return;
        }

        GameObject hex = new GameObject(name);
        hex.transform.SetParent(transform);
        hex.transform.position = position;
        hex.transform.rotation = Quaternion.Euler(0, 0, 180f);

        MeshFilter meshFilter = hex.AddComponent<MeshFilter>();
        MeshRenderer renderer = hex.AddComponent<MeshRenderer>();
        meshFilter.mesh = CreateHexMesh();

        renderer.sharedMaterial = isCenter ? centerMaterial : hexMaterial;

        if (Application.isPlaying)
        {
            MeshCollider collider = hex.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.mesh;
        }

        hexTiles.Add(hex);
        hexMap[key] = hex;
        tileGameObjectMap[name] = hex;

        Vector2Int gridCoord = WorldToGridCoord(position);
        tileNameToGrid[name] = gridCoord;
    }

    Mesh CreateHexMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HexMesh";

        Vector3[] vertices = new Vector3[7];
        int[] triangles = new int[18];

        vertices[0] = Vector3.zero;
        float visualHexSize = hexSize * (1f - gapSize);

        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * visualHexSize,
                hexHeight,
                Mathf.Sin(angle) * visualHexSize
            );
        }

        for (int i = 0; i < 6; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % 6 + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    Material CreateEnhancedMaterial(Color color, bool enableEmission = true)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", 0.85f);
        mat.SetFloat("_Metallic", 0.3f);

        if (enableEmission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.4f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return mat;
    }

    void BuildVertexGraph()
    {
        Debug.Log($"[VERTEX] BuildVertexGraph() called — tileGameObjectMap has {tileGameObjectMap.Count} tiles");

        if (tileGameObjectMap.Count == 0)
        {
            Debug.LogError("[VERTEX] tileGameObjectMap is EMPTY! Cannot build graph!");
            return;
        }
        vertexGraph.Clear();
        vertexToTile.Clear();

        foreach (var tileObj in tileGameObjectMap.Values)
        {
            string tileName = tileObj.name;
            Vector3 center = tileObj.transform.position;
            float radius = hexSize * 0.8f;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 vertex = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                vertex = RoundToGrid(vertex);

                vertexToTile[vertex] = (tileName, i);
                if (!vertexGraph.ContainsKey(vertex))
                    vertexGraph[vertex] = new List<Vector3>();
            }
        }

        float maxConnectDist = hexSize * 0.55f;

        foreach (var kvpA in vertexToTile)
        {
            Vector3 a = kvpA.Key;
            var (tileA, idxA) = kvpA.Value;

            foreach (var kvpB in vertexToTile)
            {
                Vector3 b = kvpB.Key;
                if (a == b) continue;

                var (tileB, idxB) = kvpB.Value;
                float dist = Vector3.Distance(a, b);

                bool sameTile = tileA == tileB;
                bool adjacentOnSameTile = sameTile &&
                    (Mathf.Abs(idxA - idxB) == 1 || (idxA == 0 && idxB == 5) || (idxB == 0 && idxA == 5));

                if (adjacentOnSameTile || (!sameTile && dist < maxConnectDist))
                {
                    vertexGraph[a].Add(b);
                }
            }
        }
    }

    // ================================================
    // MOVED: WorldToGridCoord — NOW ABOVE GetPlayerTileAndVertex
    // ================================================
    public Vector2Int WorldToGridCoord(Vector3 worldPos)
    {
        float cellSize = hexSize + gapSize;
        float invCellX = 1f / (1.5f * cellSize);
        float invCellZ = 1f / (Mathf.Sqrt(3f) * cellSize);

        float x = worldPos.x * invCellX;
        float z = worldPos.z * invCellZ;

        int q = Mathf.RoundToInt(x);
        int r = Mathf.RoundToInt(z - (q % 2 == 0 ? 0.5f : 0f) * 0.5f);

        return new Vector2Int(q, r);
    }

    private (string tileName, int vertexIndex) GetPlayerTileAndVertex(GameObject player)
    {
        if (player == null) return (null, -1);

        // ADD THESE TWO LINES — EARLY EXIT IF MAP NOT READY
        if (vertexToTile == null || vertexToTile.Count == 0)
        {
            Debug.LogWarning($"[REACH] vertexToTile dictionary is empty! Map generation not complete yet. Player: {player.name}");
            return (null, -1);
        }

        Vector3 playerPos = RoundToGrid(player.transform.position);
        Debug.Log($"[DEBUG] Player {player.name} at {playerPos}");

        float minDist = float.MaxValue;
        string bestTile = null;
        int bestVertex = -1;

        foreach (var kvp in vertexToTile)
        {
            Vector3 vertexPos = kvp.Key;
            float dist = Vector3.Distance(playerPos, vertexPos);
            if (dist < minDist)
            {
                minDist = dist;
                bestTile = kvp.Value.tileName;
                bestVertex = kvp.Value.vertexIndex;
            }
        }

        Debug.Log($"[DEBUG] Nearest: {bestTile} v{bestVertex} (dist: {minDist:F3})");

        if (minDist < 0.5f)
            return (bestTile, bestVertex);

        Debug.LogError($"[FATAL] Player {player.name} at {playerPos} has no vertex! (nearest dist: {minDist:F3}) Regenerate map.");
        return (null, -1);
    }

    public List<Vector3> GetReachableVerticesFromPlayer(GameObject player, int maxSteps)
    {
        var (tileName, vertexIndex) = GetPlayerTileAndVertex(player);
        if (tileName == null || vertexIndex == -1) return new List<Vector3>();

        Vector3 start = GetVertexPosition(tileName, vertexIndex);
        if (!vertexGraph.ContainsKey(start)) return new List<Vector3>();

        var reachable = new List<Vector3>();
        var queue = new Queue<(Vector3 pos, int steps)>();
        var visited = new HashSet<Vector3>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (pos, steps) = queue.Dequeue();

            // Add every visited vertex except the starting one
            if (steps > 0)
                reachable.Add(pos);

            // STOP if we've used all movement points
            if (steps >= maxSteps) continue;

            foreach (var neighbor in vertexGraph[pos])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, steps + 1));
                }
            }
        }

        return reachable;
    }

    void HighlightReachableVertices(List<Vector3> vertices)
    {
        foreach (var m in vertexMarkers)
        {
            if (m != null)
            {
                if (Application.isPlaying) Destroy(m);
                else DestroyImmediate(m);
            }
        }
        vertexMarkers.Clear();

        foreach (var v in vertices)
        {
            GameObject marker = Instantiate(vertexMarkerPrefab, v + Vector3.up * 0.05f, Quaternion.identity);
            marker.transform.SetParent(transform);
            marker.name = "VertexMarker";
            marker.GetComponent<Renderer>().sharedMaterial = vertexMarkerMaterial ?? CreateVertexMaterial(reachableVertexColor);
            marker.transform.localScale = Vector3.one * 0.15f;

            var collider = marker.AddComponent<SphereCollider>();
            collider.radius = 1.5f;

            var clickable = marker.AddComponent<VertexClickHandler>();
            clickable.onClicked += () => OnVertexClicked(v, marker, activePlayer);

            vertexMarkers.Add(marker);
        }
    }

    private void aPlayerToVertex(GameObject player, string tileName, int vertexIndex)
    {
        if (!tileGameObjectMap.ContainsKey(tileName))
        {
            Debug.LogError($"Tile '{tileName}' not found! Cannot place player.");
            return;
        }

        Vector3 vertexPos = GetVertexPosition(tileName, vertexIndex);
        Vector3 finalPos = new Vector3(vertexPos.x, 0.0f, vertexPos.z);
        player.transform.position = finalPos;

        if (player == Player1)
        {
            player1Tile = tileNameMap.FirstOrDefault(x => x.Value == tileName).Key;
            player1VertexIndex = vertexIndex;
        }
        else if (player == Player2)
        {
            player2Tile = tileNameMap.FirstOrDefault(x => x.Value == tileName).Key;
            player2VertexIndex = vertexIndex;
        }
        player.AddComponent<PlayerClickFeedback>();
        Debug.Log($"[PLAYER] {player.name} placed at {tileName} V{vertexIndex} (Y={finalPos.y:F3})");
    }

    void ClearPathAndMarkers()
    {
        foreach (var m in vertexMarkers)
        {
            if (m != null)
            {
                if (Application.isPlaying)
                    Destroy(m);
                else
                    DestroyImmediate(m);
            }
        }
        vertexMarkers.Clear();

        if (pathLine != null)
        {
            if (Application.isPlaying)
                Destroy(pathLine.gameObject);
            else
                DestroyImmediate(pathLine.gameObject);
        }
        pathLine = null;

        if (pathLine2 != null)
        {
            if (Application.isPlaying)
                Destroy(pathLine2.gameObject);
            else
                DestroyImmediate(pathLine2.gameObject);
        }
        pathLine2 = null;

        currentPath.Clear();
        currentPath2.Clear();
        selectedVertexMarker = null;
        selectedVertexMarker2 = null;
    }

    [ContextMenu("Generate Map")]
    void GenerateMap()
    {
        ClearAllChildren();

        tileNameToGrid.Clear();

        CreateMap1();
        CreateLandmarks();
       

        if (powerUpPrefab != null && powerUpInstance == null)
        {
            Vector3 powerUpPos = GetVertexPosition("Tile21", 0) + Vector3.up * 0.3f;
            powerUpInstance = Instantiate(powerUpPrefab, powerUpPos, Quaternion.identity, transform);
            powerUpInstance.name = "PowerUpExtendedReach";
        }

        InitializePathLines();
        BuildVertexGraph();
        List<Vector3> test = new List<Vector3>
    {
        transform.position,
        transform.position + Vector3.right * 2,
        transform.position + Vector3.right * 4 + Vector3.forward * 2
    };
        DrawPath(test, pathLine, Color.yellow);

        hasGenerated = true;
        Debug.Log($"MAP GENERATED! {tileNameToGrid.Count} tiles mapped to grid coordinates");
        hasGenerated = true;
        Debug.Log($"[MAP] Generation COMPLETE — vertexToTile has {vertexToTile.Count} entries. Ready for pathfinding.");
    }

    private void InitializePathLines()
    {
        if (pathLine == null)
        {
            GameObject go = new GameObject("PathLine_Player1");
            go.transform.SetParent(transform);
            pathLine = go.AddComponent<LineRenderer>();
        }

        if (pathLine2 == null)
        {
            GameObject go = new GameObject("PathLine_Player2");
            go.transform.SetParent(transform);
            pathLine2 = go.AddComponent<LineRenderer>();
        }

        if (pathMaterial != null)
        {
            pathLine.material = pathMaterial;
            pathLine2.material = pathMaterial;
        }

        pathLine.startWidth = pathLine.endWidth = 0.15f;
        pathLine2.startWidth = pathLine2.endWidth = 0.15f;
        pathLine.alignment = pathLine2.alignment = LineAlignment.View;
    }

    void DrawPath(List<Vector3> path, LineRenderer lineRenderer, Color color)
    {
        if (path == null || path.Count < 2 || lineRenderer == null) return;

        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
        lineRenderer.startColor = lineRenderer.endColor = color;

        if (pathMaterial != null)
            lineRenderer.material = pathMaterial;
    }

    public Vector3 GetVertexPosition(string tileName, int vertexIndex)
    {
        if (!tileGameObjectMap.ContainsKey(tileName))
        {
            Debug.LogError($"[ERROR] GetVertexPosition: Tile '{tileName}' not found in tileGameObjectMap!");
            return Vector3.zero;
        }

        GameObject tile = tileGameObjectMap[tileName];
        float radius = hexSize * 0.8f;
        float angle = vertexIndex * 60f * Mathf.Deg2Rad;
        Vector3 pos = tile.transform.position + new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );

        return RoundToGrid(pos);
    }

    public class VertexClickHandler : MonoBehaviour
    {
        public System.Action onClicked;
        void OnMouseDown() => onClicked?.Invoke();
    }

    Material CreateVertexMaterial(Color baseColor, bool enableEmission = true)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogError("URP Lit shader not found – using fallback.");
            lit = Shader.Find("Legacy Shaders/Diffuse");
        }

        var mat = new Material(lit);
        mat.color = baseColor;
        mat.SetFloat("_Metallic", 0.9f);
        mat.SetFloat("_Smoothness", 0.95f);

        if (enableEmission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * 3.5f);
        }
        return mat;
    }

    List<Vector3> FindShortestPath(Vector3 start, Vector3 goal)
    {
        var cameFrom = new Dictionary<Vector3, Vector3>();
        var queue = new Queue<Vector3>();
        var visited = new HashSet<Vector3>();

        start = RoundToGrid(start);
        goal = RoundToGrid(goal);

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal) break;

            foreach (var neighbor in vertexGraph[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        var path = new List<Vector3>();
        var step = goal;
        while (step != start)
        {
            path.Add(step);
            if (!cameFrom.ContainsKey(step)) break;
            step = cameFrom[step];
        }
        path.Reverse();
        path.Insert(0, start);
        return path;
    }

    void OnVertexClicked(Vector3 targetVertex, GameObject marker, GameObject player)
    {
        if ((player == Player1 && isMoving) || (player == Player2 && isMoving2)) return;

        GameObject opponent = player == Player1 ? Player2 : Player1;
        if (opponent != null)
        {
            Vector3 oppPos = RoundToGrid(opponent.transform.position);
            if (Vector3.Distance(RoundToGrid(targetVertex), oppPos) < 0.02f)
            {
                Debug.Log($"BLOCKED: {player.name} cannot move to {opponent.name}'s vertex!");
                return;
            }
        }

        if (player == Player1 && selectedVertexMarker != null)
            ResetMarkerMaterial(selectedVertexMarker);
        else if (player == Player2 && selectedVertexMarker2 != null)
            ResetMarkerMaterial(selectedVertexMarker2);

        if (player == Player1) selectedVertexMarker = marker;
        else selectedVertexMarker2 = marker;

        StartCoroutine(BurstAnimation(marker, player));

        var (startTile, startIdx) = GetPlayerTileAndVertex(player);
        if (startTile == null)
        {
            Debug.LogError("Player not on valid vertex!");
            return;
        }

        Vector3 startVertex = GetVertexPosition(startTile, startIdx);
        List<Vector3> path = FindShortestPath(startVertex, targetVertex);

        if (path.Count > defaultMovePoints + 1)
        {
            Debug.LogWarning($"Path too long ({path.Count} steps) for {player.name}");
            return;
        }

        if (player == Player1)
        {
            currentPath = path;
            StartCoroutine(MovePlayer(Player1, path, () => isMoving = false, pathLine));
        }
        else
        {
            currentPath2 = path;
            StartCoroutine(MovePlayer(Player2, path, () => isMoving2 = false, pathLine2));
        }

        Debug.Log($"{player.name} moving to {targetVertex}");
    }

    private IEnumerator MovePlayer(GameObject player, List<Vector3> path, System.Action onComplete, LineRenderer line)
    {
        bool isP1 = player == Player1;
        if (isP1) isMoving = true; else isMoving2 = true;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 pos = path[i];
            pos.y = 0.0f;
            player.transform.position = pos;
            yield return new WaitForSeconds(0.3f);
        }

        onComplete?.Invoke();
        if (line != null) line.positionCount = 0;
    }

    private void TryDrawPlayerPath()
    {
        if (currentPath != null && currentPath.Count >= 2)
        {
            DrawPath(currentPath, pathLine, Color.yellow);
        }

        if (currentPath2 != null && currentPath2.Count >= 2)
        {
            DrawPath(currentPath2, pathLine2, Color.cyan);
        }
    }

    void ClearPathLine(LineRenderer lineRenderer)
    {
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (showReachableTiles && hasGenerated)
        {
            showReachableTiles = false;
            ShowReachFromPlayer1();
        }
        TryDrawPlayerPath();
    }

    void OnDestroy()
    {
        ClearAllChildren();
    }

    public void ActivateExtendedReachForPlayer(GameObject player)
    {
        if (player == Player1)
        {
            player1ExtendedReach = true;
            Debug.Log("Player 1: Extended Reach ACTIVATED");
        }
        else if (player == Player2)
        {
            player2ExtendedReach = true;
            Debug.Log("Player 2: Extended Reach ACTIVATED");
        }
    }

    public Vector3 RoundToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x * 1000f) / 1000f,
            0f,
            Mathf.Round(pos.z * 1000f) / 1000f
        );
    }

    public string TileAt(Vector3 worldPos)
    {
        float cellSize = hexSize + gapSize;
        float invCellX = 1f / (1.5f * cellSize);
        float invCellZ = 1f / (Mathf.Sqrt(3f) * cellSize);

        float x = worldPos.x * invCellX;
        float z = worldPos.z * invCellZ;

        int q = Mathf.RoundToInt(x);
        int r = Mathf.RoundToInt(z - (q % 2 == 0 ? 0.5f : 0f) * 0.5f);

        Vector2 key = new Vector2(q * 1.5f * cellSize, r * Mathf.Sqrt(3f) * cellSize);
        if (q % 2 != 0) key.x += 0.75f * cellSize;

        if (hexMap.TryGetValue(key, out GameObject tile))
            return tile.name;

        return null;
    }

    public string GetTileName(Vector2Int gridPos)
    {
        float cellSize = hexSize + gapSize;
        Vector3 world = new Vector3(
            gridPos.x * 1.5f * cellSize,
            0f,
            gridPos.y * (Mathf.Sqrt(3f) * cellSize)
        );
        if (gridPos.y % 2 != 0) world.x += 0.75f * cellSize;

        return TileAt(world);
    }

    public GameObject GetTileObject(Vector2Int gridPos)
    {
        string name = GetTileName(gridPos);
        if (name == null) return null;
        tileGameObjectMap.TryGetValue(name, out GameObject obj);
        return obj;
    }

    private IEnumerator BurstAnimation(GameObject marker, GameObject player)
    {
        Renderer rend = marker.GetComponent<Renderer>();
        Vector3 originalScale = marker.transform.localScale;
        Vector3 bigScale = originalScale * 2.5f;

        Color baseColor = new Color(0.9f, 0.95f, 1.0f);
        rend.sharedMaterial = CreateVertexMaterial(baseColor, true);

        float growTime = 0.2f;
        for (float t = 0; t < growTime; t += Time.deltaTime)
        {
            float p = t / growTime;
            marker.transform.localScale = Vector3.Lerp(originalScale, bigScale, p);
            yield return null;
        }

        rend.sharedMaterial.SetColor("_EmissionColor", Color.white * 10f);
        PlayBurstSound();

        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = marker.transform.position;
        flash.transform.localScale = Vector3.one * 0.1f;
        Destroy(flash.GetComponent<Collider>());
        flash.GetComponent<Renderer>().sharedMaterial = CreateVertexMaterial(Color.white, true);
        Destroy(flash, 0.15f);

        yield return new WaitForSeconds(0.1f);

        vertexMarkers.Remove(marker);
        if (Application.isPlaying) Destroy(marker);
        else DestroyImmediate(marker);

        ClearOtherMarkers(player);

        if (player == Player1) selectedVertexMarker = null;
        else selectedVertexMarker2 = null;
    }

    private void ResetMarkerMaterial(GameObject marker)
    {
        if (marker == null) return;
        marker.GetComponent<Renderer>().sharedMaterial =
            vertexMarkerMaterial ?? CreateVertexMaterial(reachableVertexColor, false);
    }

    private void ClearOtherMarkers(GameObject player)
    {
        for (int i = vertexMarkers.Count - 1; i >= 0; i--)
        {
            GameObject m = vertexMarkers[i];
            if (m == null) continue;

            vertexMarkers.RemoveAt(i);
            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }

        Debug.Log("[MARKERS] ALL spheres destroyed!");
    }
    [ContextMenu("TEST — Show Player1 reach directly")]
    public void TestDirectReach()
    {
        if (Player1 == null) { Debug.LogError("Player1 missing"); return; }

        var list = GetReachableVerticesFromPlayer(Player1, 5);
        Debug.Log($"TEST: Found {list.Count} reachable vertices");

        foreach (var pos in list)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.position = pos + Vector3.up * 0.2f;
            s.transform.localScale = Vector3.one * 0.4f;
        }
    }
}

