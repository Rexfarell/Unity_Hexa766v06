using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;


public class TopologyScanHUD : MonoBehaviour
{
    private Canvas scanCanvas;
    private GameObject panelObject;
    private RectTransform graphArea;
    private TextMeshProUGUI statsText;

    private string analyzerBaseStatsText =
    string.Empty;

    private GameObject interventionDialObject;
    private TextMeshProUGUI interventionDialCenterText;
    private string interventionDialDefaultText = string.Empty;

    private SubsystemController subsystemController;

    private TextMeshProUGUI objectiveText;

    private Camera gameplayCamera;

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();
    private CameraFollow cameraFollow;

    private Transform inspectionReturnTarget;
    private bool inspectingMapNode = false;

    private int scanTeamID = -1;


    private EnemyDroneDirector enemyDroneDirector;

    private TopologyScanner activeScanner;

    private GameObject conceptPopupObject;
    private RectTransform conceptPopupRect;
    private TextMeshProUGUI conceptPopupText;

    private TextMeshProUGUI activeNodeLegendText;
    private TextMeshProUGUI articulationLegendText;

    private TextMeshProUGUI analyzerTitleText;

    private bool conceptPopupPinned = false;
    private string lastHoveredConcept = null;

    // CONCEPT NAVIGATION
    private readonly Stack<string> conceptBackStack =
        new Stack<string>();

    private string currentConceptPage = null;

    private string inspectedNodeTileName = null;

    // =====================================================
    // PATH ANALYZER — PHASE 1: TRACE INPUT (NO PATH DRAWING)
    // =====================================================
    private bool traceModeEnabled = false;
    private string traceStartTile = null;
    private string traceTargetTile = null;
    private Image traceToggleBackground;
    private TextMeshProUGUI traceToggleLabel;

    private GameObject realtimeUnitLocatorObject;
    private RectTransform realtimeUnitLocatorRect;
    private RectTransform realtimeUnitLocatorVisualRect;
    private TextMeshProUGUI realtimeUnitLocatorLabel;
    private PlayerUnit realtimeLocatorUnit = null;

    private GameObject realtimeCoreCarrierObject;
    private RectTransform realtimeCoreCarrierRect;

    private readonly List<Image>
        realtimeCoreCarrierImages =
            new List<Image>();

    // =====================================================
    // PATH ANALYZER — NODE MARGIN LAYER
    // =====================================================

    private sealed class NodeMarginVisual
    {
        public HexCapacity Hex;
        public RectTransform Badge;
        public TextMeshProUGUI Label;
    }

    private readonly Dictionary<string, NodeMarginVisual>
        nodeMarginVisuals =
            new Dictionary<string, NodeMarginVisual>();

    private Map1HexGrid nodeMarginMap;
    private float nextNodeMarginRefreshTime = 0f;

    private PlayerUnit lastObservedCoreCarrier = null;
    private string lastObservedCoreTile = null;
    private bool objectiveStateInitialized = false;

    private readonly Dictionary<string, Vector2>
    realtimeTopologyNodePositions =
        new Dictionary<string, Vector2>();

    // Live hostile occupancy: one non-interactive badge per occupied graph node.
    private readonly Dictionary<string, GameObject> liveEnemyMarkers =
        new Dictionary<string, GameObject>();
    private readonly Dictionary<string, TextMeshProUGUI> liveEnemyLabels =
        new Dictionary<string, TextMeshProUGUI>();
    private readonly Dictionary<string, int> liveEnemyCounts =
        new Dictionary<string, int>();
    private readonly List<string> liveEnemyRemoveBuffer =
        new List<string>();
    private Map1HexGrid liveEnemyMap;

    // Live friendly occupancy.
    private readonly Dictionary<string, GameObject> liveFriendlyMarkers =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, TextMeshProUGUI> liveFriendlyLabels =
        new Dictionary<string, TextMeshProUGUI>();

    private readonly Dictionary<string, int> liveFriendlyCounts =
        new Dictionary<string, int>();

    private readonly List<string> liveFriendlyRemoveBuffer =
        new List<string>();

    //PIP
    private bool nodePipMode = false;

    private bool pipLayoutSaved = false;

    private Vector2 savedPanelAnchorMin;
    private Vector2 savedPanelAnchorMax;
    private Vector2 savedPanelPivot;
    private Vector2 savedPanelSizeDelta;
    private Vector2 savedPanelAnchoredPosition;

    private Vector2 savedGraphAnchorMin;
    private Vector2 savedGraphAnchorMax;
    private Vector2 savedGraphPivot;
    private Vector2 savedGraphOffsetMin;
    private Vector2 savedGraphOffsetMax;
    private Vector2 savedGraphAnchoredPosition;

    private Vector2 savedPopupAnchorMin;
    private Vector2 savedPopupAnchorMax;
    private Vector2 savedPopupPivot;
    private Vector2 savedPopupSizeDelta;
    private Vector2 savedPopupAnchoredPosition;

    private PlayerUnit lastLocatorUnit = null;

    private Vector3 lastLocatorWorldPosition;
    private bool locatorSnapshotValid = false;

    private bool massTargetSelectorOpen =
    false;

    private string massTargetSelectorTileName =
        null;

    private bool ballastTargetSelectorOpen =
    false;

    private string ballastTargetSelectorTileName =
        null;
    // =====================================================
    // PUBLIC
    // =====================================================




    public void Show(
        TopologyScanner scanner
    )
    {
        if (scanner == null ||
            !scanner.HasScanData)
        {
            return;
        }

        EnsureUI();
        ResetTraceMode();

        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetTacticalScannerComposition(
                true
            );
        }

        activeScanner = scanner;

        objectiveStateInitialized =
            false;

        RestoreAnalyzerFromPip();

        currentConceptPage = null;
        conceptBackStack.Clear();

        inspectedNodeTileName = null;

        conceptPopupPinned = false;
        lastHoveredConcept = null;

        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(false);
        }

        if (TurnManager.Instance != null &&
            TurnManager.Instance.current != null)
        {
            scanTeamID =
                TurnManager.Instance.current.teamID;
        }
        else
        {
            scanTeamID = -1;
        }

        if (enemyDroneDirector == null)
        {
            enemyDroneDirector =
                FindFirstObjectByType<EnemyDroneDirector>();
        }

        ClearGraph();

        panelObject.SetActive(true);

        Canvas.ForceUpdateCanvases();

        DrawGraph(scanner);

        UpdateNodeMarginBadges();

        UpdateRealtimeUnitLocator();
        UpdateLiveEnemyPresence();
        UpdateLiveFriendlyPresence();
    }



    private void HideForTeamEnd()
    {
        ResetTraceMode();

        if (cameraFollow != null)
        {
            cameraFollow.SetTacticalScannerComposition(
                false
            );
        }
        // Do NOT restore the old inspection target here.
        // TurnManager / Enemy Phase now owns the camera.

        inspectionReturnTarget = null;
        inspectingMapNode = false;
        scanTeamID = -1;
        inspectedNodeTileName = null;


        activeScanner = null;
        conceptPopupPinned = false;
        lastHoveredConcept = null;

        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(false);
        }

        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    public void Hide()
    {
        ResetTraceMode();

        if (cameraFollow != null)
        {
            cameraFollow.SetTacticalScannerComposition(
                false
            );
        }
        if (inspectingMapNode &&
            cameraFollow != null &&
            inspectionReturnTarget != null)
        {
            cameraFollow.SetTarget(
                inspectionReturnTarget
            );
        }

        inspectionReturnTarget = null;
        inspectingMapNode = false;
        scanTeamID = -1;

        inspectedNodeTileName = null;
        lastLocatorUnit = null;
        locatorSnapshotValid = false;

        activeScanner = null;
        conceptPopupPinned = false;
        lastHoveredConcept = null;

        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(false);
        }

        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }


    }


    private void Update()
    {
        if (panelObject == null ||
            !panelObject.activeSelf)
        {
            return;
        }

        // ---------------------------------------------
        // MANUAL CLOSE
        // ---------------------------------------------

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ballastTargetSelectorOpen)
            {
                ReturnFromBallastTargetSelector();
                return;
            }

            if (massTargetSelectorOpen)
            {
                ReturnFromMassTargetSelector();
                return;
            }

            // Navigate backward through concept pages.
            if (conceptPopupPinned &&
                currentConceptPage != null)
            {
                if (conceptBackStack.Count > 0)
                {
                    string previousPage =
                        conceptBackStack.Pop();

                    currentConceptPage = previousPage;

                    ShowConceptDebrief(
                        previousPage
                    );
                }
                else
                {
                    // Return to the Analyzer overview.
                    HideConceptPopup();
                }

                return;
            }

            // Dismiss a temporary hover definition first.
            if (conceptPopupObject != null &&
                conceptPopupObject.activeSelf &&
                !conceptPopupPinned)
            {
                HideConceptPopup();
                return;
            }

            Hide();
            return;
        }


        // ---------------------------------------------
        // TEAM PHASE ENDED
        // ---------------------------------------------

        if (enemyDroneDirector != null &&
            enemyDroneDirector.IsRunning)
        {
            HideForTeamEnd();
            return;
        }


        if (TurnManager.Instance != null)
        {
            PlayerUnit activeUnit =
                TurnManager.Instance.current;

            if (activeUnit != null &&
                scanTeamID >= 0 &&
                activeUnit.teamID != scanTeamID)
            {
                HideForTeamEnd();
                return;
            }
        }


        // ---------------------------------------------
        // CLICK OUTSIDE — CLOSE TOPOLOGY ANALYZER
        // ---------------------------------------------

        if (Input.GetMouseButtonDown(0) &&
            !massTargetSelectorOpen &&
            !ballastTargetSelectorOpen)
        {
            Vector2 mousePosition =
                Input.mousePosition;

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();

            bool insideAnalyzer =
                RectTransformUtility.RectangleContainsScreenPoint(
                    panelRect,
                    mousePosition,
                    null
                );

            bool insideConceptPopup =
                conceptPopupObject != null &&
                conceptPopupObject.activeInHierarchy &&
                conceptPopupRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    conceptPopupRect,
                    mousePosition,
                    null
                );

            if (!insideAnalyzer &&
                !insideConceptPopup)
            {
                Hide();
                return;
            }
        }


        UpdateConceptInteraction();

        DebugMassTargetUIRaycast();

        UpdateObjectiveStateIfNeeded();

        UpdateGraphOrientation();

        UpdateNodeMarginBadges();

        UpdateRealtimeUnitLocator();

        UpdateRealtimeCoreCarrier();

        UpdateLiveEnemyPresence();


    }

    private void RefreshCurrentUnitLocatorIfNeeded()
    {
        if (activeScanner == null)
            return;

        if (TurnManager.Instance == null)
            return;


        PlayerUnit activeUnit =
            TurnManager.Instance.current;


        if (activeUnit == null)
            return;


        // Wait until movement is complete.
        // We want the locator to represent the settled position.
        if (activeUnit.IsMoving)
            return;


        bool unitChanged =
            activeUnit != lastLocatorUnit;


        bool positionChanged =
            !locatorSnapshotValid ||
            Vector3.Distance(
                activeUnit.transform.position,
                lastLocatorWorldPosition
            ) > 0.05f;


        if (!unitChanged &&
            !positionChanged)
        {
            return;
        }


        Debug.Log(
            $"[TACTICAL SCANNER] Refreshing current-unit locator | " +
            $"Unit={activeUnit.UnitCode} | " +
            $"World={activeUnit.transform.position}"
        );


        ClearGraph();

        Canvas.ForceUpdateCanvases();

        DrawGraph(
            activeScanner
        );
    }

    private bool TryWorldToGraphPosition(
    Vector3 worldPosition,
    out Vector2 graphPosition
)
    {
        graphPosition =
            Vector2.zero;


        if (activeScanner == null ||
            graphArea == null)
        {
            return false;
        }


        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();


        if (map == null)
            return false;


        float minX =
            float.MaxValue;

        float maxX =
            float.MinValue;

        float minZ =
            float.MaxValue;

        float maxZ =
            float.MinValue;


        bool foundAnyNode =
            false;


        foreach (string tileName
                 in activeScanner.LastGraph.Keys)
        {
            if (!map.tileGameObjectMap.TryGetValue(
                    tileName,
                    out GameObject tile))
            {
                continue;
            }


            if (tile == null)
                continue;


            Vector3 position =
                tile.transform.position;


            minX =
                Mathf.Min(
                    minX,
                    position.x
                );

            maxX =
                Mathf.Max(
                    maxX,
                    position.x
                );

            minZ =
                Mathf.Min(
                    minZ,
                    position.z
                );

            maxZ =
                Mathf.Max(
                    maxZ,
                    position.z
                );


            foundAnyNode =
                true;
        }


        if (!foundAnyNode)
            return false;


        float width =
            graphArea.rect.width;

        float height =
            graphArea.rect.height;


        if (width <= 1f ||
            height <= 1f)
        {
            return false;
        }


        float horizontalPadding =
            nodePipMode
                ? 18f
                : 35f;

        float verticalPadding =
            nodePipMode
                ? 18f
                : 35f;


        float normalizedX =
            Mathf.InverseLerp(
                minX,
                maxX,
                worldPosition.x
            );


        float normalizedY =
            Mathf.InverseLerp(
                minZ,
                maxZ,
                worldPosition.z
            );


        float x =
            Mathf.Lerp(
                -width * 0.5f +
                horizontalPadding,

                width * 0.5f -
                horizontalPadding,

                normalizedX
            );


        float y =
            Mathf.Lerp(
                -height * 0.5f +
                verticalPadding,

                height * 0.5f -
                verticalPadding,

                normalizedY
            );


        graphPosition =
            new Vector2(
                x,
                y
            );


        return true;
    }

    private void EnsureRealtimeUnitLocator()
    {
        if (graphArea == null)
            return;


        if (realtimeUnitLocatorObject != null)
            return;


        realtimeUnitLocatorObject =
            CreateUIObject(
                "RealtimeUnitLocator",
                graphArea
            );


        realtimeUnitLocatorRect =
            realtimeUnitLocatorObject
            .GetComponent<RectTransform>();


        realtimeUnitLocatorRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeUnitLocatorRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeUnitLocatorRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeUnitLocatorRect.sizeDelta =
            new Vector2(
                80f,
                70f
            );


        // =====================================================
        // VISUAL ROOT
        // This can bob without changing the real tracked position.
        // =====================================================

        GameObject visualObject =
            CreateUIObject(
                "RealtimeUnitLocatorVisual",
                realtimeUnitLocatorObject.transform
            );


        realtimeUnitLocatorVisualRect =
            visualObject
            .GetComponent<RectTransform>();


        realtimeUnitLocatorVisualRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeUnitLocatorVisualRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeUnitLocatorVisualRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeUnitLocatorVisualRect.sizeDelta =
            new Vector2(
                80f,
                70f
            );

        realtimeUnitLocatorVisualRect.anchoredPosition =
            Vector2.zero;


        Color locatorColor =
            new Color(
                0.62f,
                1f,
                1f,
                1f
            );


        // =====================================================
        // LEFT CHEVRON ARM
        // =====================================================

        GameObject leftArm =
            CreateUIObject(
                "RealtimeLocator_LeftArm",
                visualObject.transform
            );


        RectTransform leftRect =
            leftArm.GetComponent<RectTransform>();


        leftRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        leftRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        leftRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        leftRect.anchoredPosition =
            new Vector2(
                -6f,
                20f
            );

        leftRect.sizeDelta =
            new Vector2(
                17f,
                3f
            );

        leftRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -45f
            );


        Image leftImage =
            leftArm.AddComponent<Image>();

        leftImage.color =
            locatorColor;

        leftImage.raycastTarget =
            false;


        // =====================================================
        // RIGHT CHEVRON ARM
        // =====================================================

        GameObject rightArm =
            CreateUIObject(
                "RealtimeLocator_RightArm",
                visualObject.transform
            );


        RectTransform rightRect =
            rightArm.GetComponent<RectTransform>();


        rightRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rightRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rightRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rightRect.anchoredPosition =
            new Vector2(
                6f,
                20f
            );

        rightRect.sizeDelta =
            new Vector2(
                17f,
                3f
            );

        rightRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                45f
            );


        Image rightImage =
            rightArm.AddComponent<Image>();

        rightImage.color =
            locatorColor;

        rightImage.raycastTarget =
            false;


        // =====================================================
        // UNIT CODE
        // =====================================================

        realtimeUnitLocatorLabel =
            CreateText(
                "RealtimeUnitLocatorLabel",
                visualObject.transform,
                17f
            );


        realtimeUnitLocatorLabel.text =
            string.Empty;

        realtimeUnitLocatorLabel.alignment =
            TextAlignmentOptions.Center;

        realtimeUnitLocatorLabel.fontStyle =
            FontStyles.Bold;

        realtimeUnitLocatorLabel.color =
            locatorColor;

        realtimeUnitLocatorLabel.raycastTarget =
            false;


        RectTransform labelRect =
            realtimeUnitLocatorLabel.rectTransform;


        labelRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        labelRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        labelRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        labelRect.anchoredPosition =
            new Vector2(
                0f,
                42f
            );

        labelRect.sizeDelta =
            new Vector2(
                74f,
                24f
            );


        realtimeUnitLocatorObject.SetActive(
            false
        );
    }

    private bool TryGetUnitTopologyGraphPosition(
    PlayerUnit unit,
    out Vector2 graphPosition
)
    {
        graphPosition =
            Vector2.zero;


        if (unit == null)
            return false;


        // =====================================================
        // MOVING BETWEEN TOPOLOGICAL NODES
        // =====================================================

        if (unit.HasTopologySegment &&
            !string.IsNullOrEmpty(
                unit.TopologyFromTile
            ) &&
            !string.IsNullOrEmpty(
                unit.TopologyToTile
            ))
        {
            if (
                realtimeTopologyNodePositions
                .TryGetValue(
                    unit.TopologyFromTile,
                    out Vector2 fromPosition
                ) &&
                realtimeTopologyNodePositions
                .TryGetValue(
                    unit.TopologyToTile,
                    out Vector2 toPosition
                )
            )
            {
                bool validTopologyLink =
                    false;


                if (activeScanner != null &&
                    activeScanner.LastGraph.TryGetValue(
                        unit.TopologyFromTile,
                        out var neighbours
                    ))
                {
                    validTopologyLink =
                        neighbours.Contains(
                            unit.TopologyToTile
                        );
                }


                if (validTopologyLink)
                {
                    graphPosition =
                        Vector2.Lerp(
                            fromPosition,
                            toPosition,
                            unit.TopologySegmentProgress
                        );
                }
                else
                {
                    graphPosition =
                        unit.TopologySegmentProgress <
                        1f
                            ? fromPosition
                            : toPosition;
                }


                return true;
            }
        }


        // =====================================================
        // SETTLED ON A TOPOLOGICAL NODE
        // =====================================================

        if (!string.IsNullOrEmpty(
                unit.TopologySettledTile
            ))
        {
            if (
                realtimeTopologyNodePositions
                .TryGetValue(
                    unit.TopologySettledTile,
                    out Vector2 settledPosition
                )
            )
            {
                graphPosition =
                    settledPosition;

                return true;
            }
        }


        // =====================================================
        // FALLBACK
        // =====================================================

        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();


        if (map == null)
            return false;


        var resolved =
            map.GetPlayerTileAndVertex(
                unit.gameObject
            );


        if (string.IsNullOrEmpty(
                resolved.tileName
            ))
        {
            return false;
        }


        return
            realtimeTopologyNodePositions
            .TryGetValue(
                resolved.tileName,
                out graphPosition
            );
    }



    private void UpdateRealtimeUnitLocator()
    {
        if (TurnManager.Instance == null ||
            graphArea == null ||
            activeScanner == null)
        {
            return;
        }


        PlayerUnit activeUnit =
            TurnManager.Instance.current;


        if (activeUnit == null ||
            activeUnit.IsDead)
        {
            if (realtimeUnitLocatorObject != null)
            {
                realtimeUnitLocatorObject.SetActive(
                    false
                );
            }

            return;
        }


        EnsureRealtimeUnitLocator();


        if (realtimeUnitLocatorObject == null ||
            realtimeUnitLocatorRect == null)
        {
            return;
        }


        // =====================================================
        // UNIT CHANGE
        // =====================================================

        if (activeUnit != realtimeLocatorUnit)
        {
            realtimeLocatorUnit =
                activeUnit;

            Debug.Log(
                $"[TACTICAL SCANNER LIVE] Unit changed -> " +
                $"{activeUnit.UnitCode}"
            );
        }


        // Always keep binary unit code current.
        if (realtimeUnitLocatorLabel != null)
        {
            realtimeUnitLocatorLabel.text =
                activeUnit.UnitCode;
        }


        // =====================================================
        // DETERMINE TOPOLOGICAL POSITION
        // =====================================================

        Vector2 graphPosition =
            Vector2.zero;

        bool positionFound =
            false;


        // -----------------------------------------------------
        // UNIT IS CROSSING FROM ONE TOPOLOGICAL NODE TO ANOTHER
        // -----------------------------------------------------

        if (activeUnit.HasTopologySegment &&
            !string.IsNullOrEmpty(
                activeUnit.TopologyFromTile
            ) &&
            !string.IsNullOrEmpty(
                activeUnit.TopologyToTile
            ))
        {
            if (
                realtimeTopologyNodePositions
                .TryGetValue(
                    activeUnit.TopologyFromTile,
                    out Vector2 fromPosition
                ) &&
                realtimeTopologyNodePositions
                .TryGetValue(
                    activeUnit.TopologyToTile,
                    out Vector2 toPosition
                )
            )
            {
                bool validTopologyLink =
                    false;


                if (
                    activeScanner.LastGraph.TryGetValue(
                        activeUnit.TopologyFromTile,
                        out var neighbours
                    )
                )
                {
                    validTopologyLink =
                        neighbours.Contains(
                            activeUnit.TopologyToTile
                        );
                }


                if (validTopologyLink)
                {
                    graphPosition =
                        Vector2.Lerp(
                            fromPosition,
                            toPosition,
                            activeUnit
                                .TopologySegmentProgress
                        );
                }
                else
                {
                    // Never imply traversal across a nonexistent
                    // topology link.
                    graphPosition =
                        activeUnit.TopologySegmentProgress <
                        1f
                            ? fromPosition
                            : toPosition;
                }


                positionFound =
                    true;
            }
        }


        // -----------------------------------------------------
        // SAME TOPOLOGICAL NODE
        // -----------------------------------------------------

        if (!positionFound &&
            !string.IsNullOrEmpty(
                activeUnit.TopologySettledTile
            ))
        {
            if (
                realtimeTopologyNodePositions
                .TryGetValue(
                    activeUnit.TopologySettledTile,
                    out Vector2 settledPosition
                )
            )
            {
                graphPosition =
                    settledPosition;

                positionFound =
                    true;
            }
        }


        // -----------------------------------------------------
        // FALLBACK
        // Unit may not have moved since the match began.
        // -----------------------------------------------------

        if (!positionFound)
        {
            Map1HexGrid map =
                FindFirstObjectByType<Map1HexGrid>();


            if (map != null)
            {
                var resolved =
                    map.GetPlayerTileAndVertex(
                        activeUnit.gameObject
                    );


                if (
                    !string.IsNullOrEmpty(
                        resolved.tileName
                    ) &&
                    realtimeTopologyNodePositions
                    .TryGetValue(
                        resolved.tileName,
                        out Vector2 resolvedPosition
                    )
                )
                {
                    graphPosition =
                        resolvedPosition;

                    positionFound =
                        true;
                }
            }
        }


        if (!positionFound)
        {
            realtimeUnitLocatorObject.SetActive(
                false
            );

            return;
        }


        // =====================================================
        // APPLY POSITION
        // =====================================================

        realtimeUnitLocatorObject.SetActive(
            true
        );


        realtimeUnitLocatorRect.anchoredPosition =
            graphPosition;


        // Keep locator visually upright while graph rotates.
        realtimeUnitLocatorRect.localRotation =
            Quaternion.Inverse(
                graphArea.localRotation
            );


        // =====================================================
        // VISUAL MOTION SIGNAL
        // =====================================================

        if (realtimeUnitLocatorVisualRect != null)
        {
            float bobAmplitude =
                activeUnit.IsMoving
                    ? 3f
                    : 1.5f;


            float bob =
                (
                    Mathf.Sin(
                        Time.unscaledTime *
                        5f
                    ) +
                    1f
                ) *
                bobAmplitude;


            realtimeUnitLocatorVisualRect
                .anchoredPosition =
                new Vector2(
                    0f,
                    bob
                );
        }


        realtimeUnitLocatorObject
            .transform
            .SetAsLastSibling();
    }

    private void EnsureRealtimeCoreCarrierMarker()
    {
        if (graphArea == null)
            return;


        if (realtimeCoreCarrierObject != null)
            return;


        realtimeCoreCarrierObject =
            CreateUIObject(
                "RealtimeAICoreCarrier",
                graphArea
            );


        realtimeCoreCarrierRect =
            realtimeCoreCarrierObject
            .GetComponent<RectTransform>();


        realtimeCoreCarrierRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeCoreCarrierRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeCoreCarrierRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        realtimeCoreCarrierRect.sizeDelta =
            new Vector2(
                48f,
                48f
            );


        float halfSize =
            16f;

        float armLength =
            9f;

        float thickness =
            2.5f;


        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                -halfSize +
                armLength * 0.5f,
                halfSize
            ),
            new Vector2(
                armLength,
                thickness
            )
        );

        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                -halfSize,
                halfSize -
                armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            )
        );


        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                halfSize -
                armLength * 0.5f,
                halfSize
            ),
            new Vector2(
                armLength,
                thickness
            )
        );

        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                halfSize,
                halfSize -
                armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            )
        );


        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                -halfSize +
                armLength * 0.5f,
                -halfSize
            ),
            new Vector2(
                armLength,
                thickness
            )
        );

        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                -halfSize,
                -halfSize +
                armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            )
        );


        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                halfSize -
                armLength * 0.5f,
                -halfSize
            ),
            new Vector2(
                armLength,
                thickness
            )
        );

        CreateRealtimeCoreCarrierSegment(
            new Vector2(
                halfSize,
                -halfSize +
                armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            )
        );


        realtimeCoreCarrierObject.SetActive(
            false
        );
    }

    private void CreateRealtimeCoreCarrierSegment(
    Vector2 position,
    Vector2 size
)
    {
        GameObject segment =
            CreateUIObject(
                "RealtimeCoreCarrierSegment",
                realtimeCoreCarrierObject.transform
            );


        RectTransform rect =
            segment.GetComponent<RectTransform>();


        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;


        Image image =
            segment.AddComponent<Image>();


        image.raycastTarget =
            false;


        realtimeCoreCarrierImages.Add(
            image
        );
    }

    private void UpdateRealtimeCoreCarrier()
    {
        if (activeScanner == null ||
            graphArea == null)
        {
            return;
        }


        PlayerUnit carrier =
            GetBoxCarrier();


        if (carrier == null)
        {
            if (realtimeCoreCarrierObject != null)
            {
                realtimeCoreCarrierObject.SetActive(
                    false
                );
            }

            return;
        }


        EnsureRealtimeCoreCarrierMarker();


        if (realtimeCoreCarrierObject == null ||
            realtimeCoreCarrierRect == null)
        {
            return;
        }


        if (!TryGetUnitTopologyGraphPosition(
                carrier,
                out Vector2 carrierPosition))
        {
            realtimeCoreCarrierObject.SetActive(
                false
            );

            return;
        }


        realtimeCoreCarrierObject.SetActive(
            true
        );


        realtimeCoreCarrierRect.anchoredPosition =
            carrierPosition;


        // =====================================================
        // ALLIED / ENEMY CARRIER COLOR
        // =====================================================

        bool enemyCarrier =
            scanTeamID >= 0 &&
            carrier.teamID != scanTeamID;


        Color carrierColor =
            enemyCarrier
                ? new Color(
                    1f,
                    0.18f,
                    0.12f,
                    1f
                )
                : new Color(
                    1f,
                    0.82f,
                    0.10f,
                    1f
                );


        float pulse =
            0.88f +
            (
                Mathf.Sin(
                    Time.unscaledTime *
                    5f
                ) +
                1f
            ) * 0.06f;


        carrierColor.a =
            pulse;


        foreach (
            Image image
            in realtimeCoreCarrierImages
        )
        {
            if (image != null)
            {
                image.color =
                    carrierColor;
            }
        }


        float scale =
            1f +
            Mathf.Sin(
                Time.unscaledTime *
                5f
            ) * 0.05f;


        realtimeCoreCarrierRect.localScale =
            Vector3.one *
            scale;


        realtimeCoreCarrierObject
            .transform
            .SetAsLastSibling();
    }

    private void UpdateObjectiveStateIfNeeded()
    {
        if (activeScanner == null)
            return;


        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();


        if (map == null)
            return;


        PlayerUnit currentCarrier =
            GetBoxCarrier();


        string currentCoreTile =
            null;


        // Only track the free AI Core's tile while
        // nobody is carrying it.
        if (currentCarrier == null)
        {
            currentCoreTile =
                GetCurrentBoxTileName(
                    map
                );
        }


        // =====================================================
        // FIRST SNAPSHOT
        // =====================================================

        if (!objectiveStateInitialized)
        {
            lastObservedCoreCarrier =
                currentCarrier;

            lastObservedCoreTile =
                currentCoreTile;

            objectiveStateInitialized =
                true;

            return;
        }


        // =====================================================
        // HAS THE OBJECTIVE STATE CHANGED?
        // =====================================================

        bool carrierChanged =
            currentCarrier !=
            lastObservedCoreCarrier;


        bool coreTileChanged =
            currentCoreTile !=
            lastObservedCoreTile;


        if (!carrierChanged &&
            !coreTileChanged)
        {
            return;
        }


        lastObservedCoreCarrier =
            currentCarrier;

        lastObservedCoreTile =
            currentCoreTile;


        Debug.Log(
            $"[TACTICAL SCANNER] AI CORE STATE CHANGED | " +
            $"Carrier=" +
            $"{(currentCarrier != null ? currentCarrier.UnitCode : "NONE")} | " +
            $"Tile=" +
            $"{(currentCoreTile ?? "CARRIED / UNAVAILABLE")}"
        );


        // =====================================================
        // REFRESH GRAPH ONCE
        // =====================================================

        ClearGraph();

        Canvas.ForceUpdateCanvases();

        DrawGraph(
            activeScanner
        );

        Canvas.ForceUpdateCanvases();
    }
    private void UpdateGraphOrientation()
    {
        if (graphArea == null)
            return;

        if (gameplayCamera == null)
        {
            gameplayCamera =
                Camera.main;
        }

        if (gameplayCamera == null)
            return;

        Vector3 forward =
            gameplayCamera.transform.forward;

        // Ignore camera pitch.
        // We only want battlefield orientation.
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        float cameraYaw =
            Mathf.Atan2(
                forward.x,
                forward.z
            ) * Mathf.Rad2Deg;

        graphArea.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                cameraYaw
            );
    }
    // =====================================================
    // BUILD HUD
    // =====================================================

    private void EnsureUI()
    {
        if (scanCanvas != null)
            return;


        // ---------------------------------------------
        // CANVAS
        // ---------------------------------------------

        GameObject canvasObject =
            new GameObject(
                "TopologyScanCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        canvasObject.transform.SetParent(
            transform,
            false
        );

        scanCanvas =
            canvasObject.GetComponent<Canvas>();

        scanCanvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        scanCanvas.sortingOrder =
            250;


        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );

        scaler.matchWidthOrHeight =
            0.5f;


        // ---------------------------------------------
        // MAIN PANEL
        // ---------------------------------------------

        panelObject =
            CreateUIObject(
                "TopologyAnalyzerPanel",
                canvasObject.transform
            );

        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();

        panelRect.anchorMin =
            new Vector2(1f, 0.5f);

        panelRect.anchorMax =
            new Vector2(1f, 0.5f);

        panelRect.pivot =
            new Vector2(1f, 0.5f);

        panelRect.sizeDelta =
            new Vector2(
                570f,
                720f
            );

        panelRect.anchoredPosition =
            new Vector2(
                -35f,
                0f
            );


        Image panelImage =
            panelObject.AddComponent<Image>();

        panelImage.color =
            new Color(
                0.025f,
                0.045f,
                0.065f,
                0.94f
            );


        Outline panelOutline =
            panelObject.AddComponent<Outline>();

        panelOutline.effectColor =
            new Color(
                0.10f,
                0.75f,
                0.90f,
                0.65f
            );

        panelOutline.effectDistance =
            new Vector2(
                1.5f,
                -1.5f
            );

        // ---------------------------------------------
        // CONCEPT / DEBRIEF POPUP
        // ---------------------------------------------

        conceptPopupObject =
            CreateUIObject(
                "TopologyConceptPopup",
                canvasObject.transform
            );

        conceptPopupRect =
            conceptPopupObject.GetComponent<RectTransform>();

        conceptPopupRect.anchorMin =
            new Vector2(
                1f,
                0.5f
            );

        conceptPopupRect.anchorMax =
            new Vector2(
                1f,
                0.5f
            );

        conceptPopupRect.pivot =
            new Vector2(
                1f,
                0.5f
            );

        conceptPopupRect.sizeDelta =
            new Vector2(
                430f,
                180f
            );

        conceptPopupRect.anchoredPosition =
            new Vector2(
                -625f,
                0f
            );


        Image conceptBackground =
            conceptPopupObject.AddComponent<Image>();

        conceptBackground.color =
            new Color(
                0.018f,
                0.035f,
                0.052f,
                0.97f
            );


        Outline conceptOutline =
            conceptPopupObject.AddComponent<Outline>();

        conceptOutline.effectColor =
            new Color(
                0.10f,
                0.75f,
                0.90f,
                0.75f
            );

        conceptOutline.effectDistance =
            new Vector2(
                1.5f,
                -1.5f
            );


        conceptPopupText =
            CreateText(
                "ConceptPopupText",
                conceptPopupObject.transform,
                20f
            );

        conceptPopupText.alignment =
            TextAlignmentOptions.TopLeft;

        conceptPopupText.enableWordWrapping =
            true;

        conceptPopupText.lineSpacing =
            5f;

        conceptPopupText.color =
            new Color(
                0.78f,
                0.95f,
                1f,
                1f
            );

        RectTransform conceptTextRect =
            conceptPopupText.rectTransform;

        conceptTextRect.anchorMin =
            new Vector2(
                0f,
                0f
            );

        conceptTextRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        conceptTextRect.offsetMin =
            new Vector2(
                22f,
                20f
            );

        conceptTextRect.offsetMax =
            new Vector2(
                -22f,
                -20f
            );

        conceptPopupObject.SetActive(false);
        // ---------------------------------------------
        // TITLE
        // ---------------------------------------------

        analyzerTitleText =
            CreateText(
                "Title",
                panelObject.transform,
                25f
            );

        analyzerTitleText.richText =
            true;

        analyzerTitleText.raycastTarget =
            true;

        analyzerTitleText.text =
            "<link=\"tactical_scanner\">" +
            "TOPOLOGY ANALYZER" +
            "</link>";

        analyzerTitleText.fontStyle =
            FontStyles.Bold;

        analyzerTitleText.alignment =
            TextAlignmentOptions.Left;

        RectTransform titleRect =
            analyzerTitleText.rectTransform;

        titleRect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        titleRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        titleRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        titleRect.offsetMin =
            new Vector2(
                24f,
                -60f
            );

        titleRect.offsetMax =
            new Vector2(
                -24f,
                -18f
            );

        analyzerTitleText.ForceMeshUpdate();

        // Independent top-right control; does not alter locked layouts.
        CreateTraceToggle();

        // ---------------------------------------------
        // STATS
        // ---------------------------------------------

        statsText =
            CreateText(
                "Statistics",
                panelObject.transform,
                17f
            );

        statsText.alignment =
            TextAlignmentOptions.Left;

        RectTransform statsRect =
            statsText.rectTransform;

        statsRect.anchorMin =
            new Vector2(0f, 1f);

        statsRect.anchorMax =
            new Vector2(1f, 1f);

        statsRect.pivot =
            new Vector2(0.5f, 1f);

        statsRect.offsetMin =
    new Vector2(24f, -113f);

        statsRect.offsetMax =
            new Vector2(-24f, -67f);

        // ---------------------------------------------
        // OBJECTIVE STATE
        // ---------------------------------------------

        objectiveText =
            CreateText(
                "ObjectiveState",
                panelObject.transform,
                15f
            );

        objectiveText.fontStyle =
            FontStyles.Bold;

        objectiveText.alignment =
            TextAlignmentOptions.Left;

        objectiveText.color =
            new Color(
                1f,
                0.86f,
                0.22f,
                1f
            );

        RectTransform objectiveRect =
            objectiveText.rectTransform;

        objectiveRect.anchorMin =
            new Vector2(0f, 1f);

        objectiveRect.anchorMax =
            new Vector2(1f, 1f);

        objectiveRect.pivot =
            new Vector2(0.5f, 1f);

        objectiveRect.offsetMin =
    new Vector2(
        24f,
        -160f
    );

        objectiveRect.offsetMax =
            new Vector2(
                -24f,
                -116f
            );


        // ---------------------------------------------
        // GRAPH AREA
        // ---------------------------------------------

        GameObject graphObject =
            CreateUIObject(
                "GraphArea",
                panelObject.transform
            );

        graphArea =
            graphObject.GetComponent<RectTransform>();

        graphArea.anchorMin =
            new Vector2(0f, 0f);

        graphArea.anchorMax =
            new Vector2(1f, 1f);

        graphArea.offsetMin =
            new Vector2(
                28f,
                75f
            );

        graphArea.offsetMax =
            new Vector2(
                -28f,
                -175f
            );
        graphArea.anchoredPosition +=
            new Vector2(
                0f,
                35f
            );

        // ---------------------------------------------
        // LEGEND
        // ---------------------------------------------

        CreateLegendItem(
            panelObject.transform,
            "ActiveNodeLegend",
            "ACTIVE NODE",
            new Color(
                0.20f,
                0.82f,
                0.90f,
                1f
            ),
            false,
            new Vector2(
                34f,
                55f
            )
        );

        CreateLegendItem(
            panelObject.transform,
            "CriticalNodeLegend",
            "ARTICULATION POINT",
            new Color(
                1.0f,
                0.52f,
                0.12f,
                1f
            ),
            true,
            new Vector2(
                260f,
                55f
            )
        );


        TextMeshProUGUI footer =
            CreateText(
                "Footer",
                panelObject.transform,
                14f
            );

        footer.text =
     "HOVER // EXPLAIN   CLICK // DEBRIEF   ESC // CLOSE";

        footer.alignment =
            TextAlignmentOptions.Left;

        footer.color =
            new Color(
                0.65f,
                0.85f,
                0.90f,
                1f
            );

        RectTransform footerRect =
            footer.rectTransform;

        footerRect.anchorMin =
            new Vector2(0f, 0f);

        footerRect.anchorMax =
            new Vector2(1f, 0f);

        footerRect.pivot =
            new Vector2(0f, 0f);

        footerRect.offsetMin =
            new Vector2(
                24f,
                14f
            );

        footerRect.offsetMax =
            new Vector2(
                -24f,
                39f
            );


        panelObject.SetActive(false);
    }

    // =====================================================
    // PATH ANALYZER — PHASE 1: TRACE TOGGLE + ENDPOINT INPUT
    // =====================================================
    private void CreateTraceToggle()
    {
        GameObject toggle = CreateUIObject(
            "NetworkTraceToggle", panelObject.transform);

        RectTransform rect = toggle.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -22f);
        rect.sizeDelta = new Vector2(145f, 32f);

        traceToggleBackground = toggle.AddComponent<Image>();
        traceToggleBackground.raycastTarget = true;

        Button button = toggle.AddComponent<Button>();
        button.targetGraphic = traceToggleBackground;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(ToggleTraceMode);

        traceToggleLabel = CreateText(
            "TraceToggleLabel", toggle.transform, 14f);
        traceToggleLabel.fontStyle = FontStyles.Bold;
        traceToggleLabel.alignment = TextAlignmentOptions.Center;
        traceToggleLabel.raycastTarget = false;

        RectTransform labelRect = traceToggleLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        UpdateTraceToggleVisual();
    }

    private void ToggleTraceMode()
    {
        // The node-inspection workspace has its own locked controls.
        if (nodePipMode) return;

        traceModeEnabled = !traceModeEnabled;
        traceStartTile = null;
        traceTargetTile = null;

        if (traceModeEnabled)
            HideConceptPopup();

        UpdateTraceToggleVisual();
        Debug.Log("[PATH ANALYZER] TRACE " +
            (traceModeEnabled ? "ON — select START node." : "OFF."));
    }

    private void ResetTraceMode()
    {
        traceModeEnabled = false;
        traceStartTile = null;
        traceTargetTile = null;
        UpdateTraceToggleVisual();
    }

    private void UpdateTraceToggleVisual()
    {
        if (traceToggleBackground != null)
            traceToggleBackground.color = traceModeEnabled
                ? new Color(0.08f, 0.38f, 0.42f, 0.98f)
                : new Color(0.025f, 0.10f, 0.14f, 0.96f);

        if (traceToggleLabel != null)
        {
            traceToggleLabel.text = traceModeEnabled
                ? "TRACE  ON" : "TRACE  OFF";
            traceToggleLabel.color = traceModeEnabled
                ? new Color(0.94f, 0.90f, 0.40f)
                : new Color(0.36f, 0.84f, 0.90f);
        }
    }

    private void SelectTraceEndpoint(string tileName)
    {
        if (activeScanner == null ||
            !activeScanner.LastGraph.ContainsKey(tileName))
            return;

        if (traceStartTile == null || traceTargetTile != null)
        {
            traceStartTile = tileName;
            traceTargetTile = null;
            Debug.Log("[PATH ANALYZER] START = " + tileName +
                " | Select TARGET node.");
        }
        else if (tileName == traceStartTile)
        {
            Debug.Log("[PATH ANALYZER] START unchanged = " +
                tileName + " | Select a different TARGET node.");
        }
        else
        {
            traceTargetTile = tileName;
            Debug.Log("[PATH ANALYZER] ENDPOINTS READY | " +
                traceStartTile + " -> " + traceTargetTile +
                " | Path drawing not implemented in Phase 1.");
        }
    }

    // =====================================================
    // DRAW GRAPH
    // =====================================================

    private void DrawGraph(
        TopologyScanner scanner
    )
    {
        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
            return;


        Dictionary<string, Vector2> nodePositions =
            new Dictionary<string, Vector2>();


        // ---------------------------------------------
        // FIND WORLD BOUNDS
        // ---------------------------------------------

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        float minZ = float.MaxValue;
        float maxZ = float.MinValue;


        foreach (string tileName in scanner.LastGraph.Keys)
        {
            if (!map.tileGameObjectMap.TryGetValue(
                    tileName,
                    out GameObject tile))
            {
                continue;
            }

            if (tile == null)
                continue;

            Vector3 position =
                tile.transform.position;

            minX =
                Mathf.Min(
                    minX,
                    position.x
                );

            maxX =
                Mathf.Max(
                    maxX,
                    position.x
                );

            minZ =
                Mathf.Min(
                    minZ,
                    position.z
                );

            maxZ =
                Mathf.Max(
                    maxZ,
                    position.z
                );
        }


        float width =
            graphArea.rect.width;

        float height =
            graphArea.rect.height;

        float horizontalPadding =
            nodePipMode
                ? 18f
                : 35f;

        float verticalPadding =
            nodePipMode
                ? 18f
                : 35f;


        // ---------------------------------------------
        // CONVERT MAP POSITIONS INTO SCHEMATIC POSITIONS
        // ---------------------------------------------

        foreach (string tileName in scanner.LastGraph.Keys)
        {
            if (!map.tileGameObjectMap.TryGetValue(
                    tileName,
                    out GameObject tile))
            {
                continue;
            }

            if (tile == null)
                continue;


            Vector3 world =
                tile.transform.position;


            float normalizedX =
                Mathf.InverseLerp(
                    minX,
                    maxX,
                    world.x
                );

            float normalizedY =
                Mathf.InverseLerp(
                    minZ,
                    maxZ,
                    world.z
                );


            float x =
                Mathf.Lerp(
                    -width * 0.5f +
                    horizontalPadding,

                    width * 0.5f -
                    horizontalPadding,

                    normalizedX
                );


            float y =
                Mathf.Lerp(
                    -height * 0.5f +
                    verticalPadding,

                    height * 0.5f -
                    verticalPadding,

                    normalizedY
                );


            nodePositions[tileName] =
                new Vector2(
                    x,
                    y
                );
        }

        // =====================================================
        // CACHE TOPOLOGY POSITIONS FOR LIVE UNIT TRACKING
        // =====================================================

        realtimeTopologyNodePositions.Clear();

        foreach (
            KeyValuePair<string, Vector2> pair
            in nodePositions
        )
        {
            realtimeTopologyNodePositions[
                pair.Key
            ] =
                pair.Value;
        }


        // ---------------------------------------------
        // EDGES FIRST
        // ---------------------------------------------

        HashSet<string> drawnEdges =
            new HashSet<string>();

        string boxTileName =
            GetCurrentBoxTileName(
                map
    );
        PlayerUnit boxCarrier =
            GetBoxCarrier();

        PlayerUnit activeUnit =
            TurnManager.Instance != null
            ? TurnManager.Instance.current
            : null;

        string currentUnitTileName =
    null;

        if (activeUnit != null)
        {
            var resolvedUnitLocation =
                map.GetPlayerTileAndVertex(
                    activeUnit.gameObject
                );

            currentUnitTileName =
                resolvedUnitLocation.tileName;

            Debug.Log(
                $"[TACTICAL SCANNER] CURRENT UNIT | " +
                $"{activeUnit.UnitCode} | " +
                $"LogicalCoord={activeUnit.currentTileCoord} | " +
                $"ResolvedTile={currentUnitTileName} | " +
                $"Vertex={resolvedUnitLocation.vertexIndex}"
            );
        }

        lastLocatorUnit =
    activeUnit;

        if (activeUnit != null)
        {
            lastLocatorWorldPosition =
                activeUnit.transform.position;

            locatorSnapshotValid =
                true;
        }
        else
        {
            locatorSnapshotValid =
                false;
        }

        bool carrierIsEnemy =
            boxCarrier != null &&
            activeUnit != null &&
            boxCarrier.teamID != activeUnit.teamID;
        if (objectiveText != null)
        {
            // -------------------------------------------------
            // ENEMY TEAM HAS THE AI CORE
            // -------------------------------------------------

            if (boxCarrier != null &&
                carrierIsEnemy)
            {
                objectiveText.text =
                    $"OBJECTIVE : INTERCEPT ENEMY CARRIER\n" +
                    $"CARRIER : {boxCarrier.UnitCode}";

                objectiveText.color =
                    new Color(
                        1f,
                        0.42f,
                        0.18f,
                        1f
                    );
            }

            // -------------------------------------------------
            // ALLIED TEAM HAS THE AI CORE
            // -------------------------------------------------

            else if (boxCarrier != null)
            {
                objectiveText.text =
                    $"OBJECTIVE : DELIVER THE AI CORE TO THE A.I. STATION\n" +
                    $"CARRIER : {boxCarrier.UnitCode}";

                objectiveText.color =
                    new Color(
                        1f,
                        0.82f,
                        0.16f,
                        1f
                    );
            }

            // -------------------------------------------------
            // AI CORE IS FREE ON THE MAP
            // -------------------------------------------------

            else if (!string.IsNullOrEmpty(
                         boxTileName
                     ))
            {
                objectiveText.text =
                    $"OBJECTIVE : ACQUIRE AI CORE\n" +
                    $"AI CORE : {boxTileName}";

                objectiveText.color =
                    new Color(
                        1f,
                        0.86f,
                        0.22f,
                        1f
                    );
            }

            // -------------------------------------------------
            // FALLBACK
            // -------------------------------------------------

            else
            {
                objectiveText.text =
                    "OBJECTIVE : AI CORE POSITION UNAVAILABLE";

                objectiveText.color =
                    new Color(
                        1f,
                        0.86f,
                        0.22f,
                        1f
                    );
            }
        }
        Debug.Log(
            $"[TACTICAL SCANNER] OBJECTIVE | " +
            $"BOX={boxTileName ?? "UNKNOWN"} | " +
            $"DESTINATION=AIStation"
        );

        foreach (var pair in scanner.LastGraph)
        {
            string source =
                pair.Key;

            if (!nodePositions.ContainsKey(source))
                continue;


            foreach (string target in pair.Value)
            {
                if (!nodePositions.ContainsKey(target))
                    continue;


                string edgeKey =
                    string.CompareOrdinal(
                        source,
                        target
                    ) < 0

                    ? source + "|" + target
                    : target + "|" + source;


                if (!drawnEdges.Add(edgeKey))
                    continue;


                CreateEdge(
                    nodePositions[source],
                    nodePositions[target]
                );
            }
        }


        // ---------------------------------------------
        // NODES SECOND
        // ---------------------------------------------

        foreach (var pair in nodePositions)
        {
            bool critical =
                scanner.IsArticulationPoint(
                    pair.Key
                );

            CreateNode(
                pair.Key,
                pair.Value,
                critical
            );


            // ---------------------------------------------
            // BOX / CARRIER
            // ---------------------------------------------

            if (boxCarrier == null &&
                pair.Key == boxTileName)
            {
                CreateBoxMarker(
                    pair.Value
                );
            }


            // ---------------------------------------------
            // AI STATION
            // ---------------------------------------------

            if (pair.Key == "AIStation")
            {
                bool alliedCarrier =
                    boxCarrier != null &&
                    !carrierIsEnemy;

                CreateAIStationMarker(
                    pair.Value,
                    alliedCarrier
                );
            }


        }


        // ---------------------------------------------
        // STATISTICS
        // ---------------------------------------------

        int edgeCount = 0;

        foreach (var pair in scanner.LastGraph)
        {
            edgeCount +=
                pair.Value.Count;
        }

        edgeCount /= 2;


        statsText.text =
    analyzerBaseStatsText =
    $"<link=\"nodes\">NODES</link>  {scanner.LastGraph.Count}     " +
    $"<link=\"links\">LINKS</link>  {edgeCount}\n" +
    $"<link=\"components\">COMPONENTS</link>  {scanner.LastConnectedComponents}     " +
    $"<size=145%><b><color=#FF851F>" +
    $"<link=\"critical\">CRITICAL  {scanner.LastArticulationPoints.Count}</link>" +
    $"</color></b></size>";

        statsText.text =
            analyzerBaseStatsText;
    }


    // =====================================================
    // EDGE
    // =====================================================

    private void CreateEdge(
        Vector2 from,
        Vector2 to
    )
    {
        GameObject edge =
            CreateUIObject(
                "GraphEdge",
                graphArea
            );

        generatedObjects.Add(edge);


        Image image =
            edge.AddComponent<Image>();

        image.raycastTarget =
            false;

        image.color =
            new Color(
                0.18f,
                0.65f,
                0.75f,
                0.48f
            );


        RectTransform rect =
            edge.GetComponent<RectTransform>();

        Vector2 direction =
            to - from;

        float distance =
            direction.magnitude;


        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0f,
                0.5f
            );

        rect.anchoredPosition =
            from;

        rect.sizeDelta =
            new Vector2(
                distance,
                2f
            );


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;


        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    private void ShowMainTopologyProbe(
    string tileName
)
    {
        if (nodePipMode ||
            statsText == null ||
            activeScanner == null)
        {
            return;
        }

        if (!activeScanner.LastGraph.TryGetValue(
                tileName,
                out var neighbours))
        {
            return;
        }

        int degree =
            neighbours.Count;

        int componentsBefore =
            activeScanner.LastConnectedComponents;

        int componentsAfter =
            CountComponentsIgnoringNode(
                tileName
            );

        bool critical =
            activeScanner.IsArticulationPoint(
                tileName
            );

        string classification =
            critical
                ? "ARTICULATION POINT"
                : "STANDARD NODE";

        string classificationColor =
            critical
                ? "#FF851F"
                : "#62D6E8";

        string marginDisplay = "N/A";
        string marginColor = "#8CAEB8";

        if (nodeMarginVisuals.TryGetValue(
                tileName,
                out NodeMarginVisual marginVisual) &&
            marginVisual.Hex != null)
        {
            int margin =
                marginVisual.Hex.EffectiveCapacity -
                marginVisual.Hex.CurrentLoad;

            marginDisplay =
                (margin > 0 ? "+" : "") +
                margin.ToString();

            marginColor =
                margin > 0 ? "#62D6E8" :
                margin == 0 ? "#FFD84A" :
                "#FF8A32";
        }
        statsText.text =
            $"<color=#62D6E8><b>NODE {tileName}</b></color>     " +
            $"MARGIN <color={marginColor}><b>{marginDisplay}</b></color>     " +
            $"DEGREE <b>{degree}</b>\n" +
            $"REMOVAL <b>{componentsBefore} → {componentsAfter} COMPONENTS</b>     " +
            $"<color={classificationColor}><b>{classification}</b></color>";
    }


    private void RestoreMainTopologyStats()
    {
        if (nodePipMode ||
            statsText == null)
        {
            return;
        }

        statsText.text =
            analyzerBaseStatsText;
    }


    private int CountComponentsIgnoringNode(
        string ignoredNode
    )
    {
        if (activeScanner == null)
            return 0;

        HashSet<string> visited =
            new HashSet<string>();

        int componentCount = 0;


        foreach (
            var pair
            in activeScanner.LastGraph
        )
        {
            string start =
                pair.Key;

            if (start == ignoredNode ||
                visited.Contains(start))
            {
                continue;
            }

            componentCount++;

            Queue<string> frontier =
                new Queue<string>();

            frontier.Enqueue(start);
            visited.Add(start);


            while (frontier.Count > 0)
            {
                string current =
                    frontier.Dequeue();

                if (!activeScanner.LastGraph.TryGetValue(
                        current,
                        out var neighbours))
                {
                    continue;
                }

                foreach (
                    string next
                    in neighbours
                )
                {
                    if (next == ignoredNode ||
                        visited.Contains(next))
                    {
                        continue;
                    }

                    if (!activeScanner.LastGraph.ContainsKey(
                            next
                        ))
                    {
                        continue;
                    }

                    visited.Add(next);
                    frontier.Enqueue(next);
                }
            }
        }

        return componentCount;
    }


    private void CreateNodeMarginBadge(
        string tileName,
        Transform nodeTransform,
        bool critical
    )
    {
        if (nodeMarginMap == null)
            nodeMarginMap =
                FindFirstObjectByType<Map1HexGrid>();

        if (nodeMarginMap == null)
            return;

        if (!nodeMarginMap.tileGameObjectMap.TryGetValue(
                tileName,
                out GameObject tile) ||
            tile == null)
        {
            return;
        }

        HexCapacity hex =
            tile.GetComponent<HexCapacity>();

        if (hex == null)
            hex = tile.GetComponentInChildren<HexCapacity>(true);

        // Some graph locations may not have structural capacity.
        if (hex == null)
            return;

        GameObject badge = CreateUIObject(
            "NodeMargin_" + tileName,
            nodeTransform
        );

        RectTransform rect =
            badge.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Below the node, away from friendly/enemy markers.
        rect.anchoredPosition = new Vector2(
            0f,
            critical ? -26f : -19f
        );

        rect.sizeDelta = new Vector2(40f, 16f);

        Image background = badge.AddComponent<Image>();

        background.color =
            new Color(0.035f, 0.09f, 0.12f, 0.92f);

        background.raycastTarget = false;

        TextMeshProUGUI label = CreateText(
            "MarginValue",
            badge.transform,
            10f
        );

        label.text = "M?";
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.raycastTarget = false;

        RectTransform labelRect = label.rectTransform;

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        nodeMarginVisuals[tileName] =
            new NodeMarginVisual
            {
                Hex = hex,
                Badge = rect,
                Label = label
            };

        // Keep the smaller intervention PiP uncluttered.
        badge.SetActive(!nodePipMode);
    }


    private void UpdateNodeMarginBadges()
    {
        if (nodeMarginVisuals.Count == 0 ||
            graphArea == null)
        {
            return;
        }

        // Keep the labels upright as the graph rotates.
        foreach (var pair in nodeMarginVisuals)
        {
            NodeMarginVisual visual = pair.Value;

            if (visual.Badge == null)
                continue;

            visual.Badge.localRotation =
                Quaternion.Inverse(graphArea.localRotation);
        }

        // Refresh structural data four times per second.
        if (Time.unscaledTime < nextNodeMarginRefreshTime)
            return;

        nextNodeMarginRefreshTime =
            Time.unscaledTime + 0.25f;

        foreach (var pair in nodeMarginVisuals)
        {
            NodeMarginVisual visual = pair.Value;

            if (visual.Hex == null ||
                visual.Label == null ||
                visual.Badge == null)
            {
                continue;
            }

            HexLoadState state = visual.Hex.LoadState;

            // Full Analyzer: only structural warnings.
            // PiP: show the currently inspected node.
            bool showBadge = nodePipMode
                ? pair.Key == inspectedNodeTileName
                : (
                    state == HexLoadState.Full ||
                    state == HexLoadState.Overloaded ||
                    state == HexLoadState.Collapsing ||
                    state == HexLoadState.Collapsed
                );

            if (visual.Badge.gameObject.activeSelf != showBadge)
            {
                visual.Badge.gameObject.SetActive(showBadge);
            }

            if (state == HexLoadState.Collapsed)
            {
                visual.Label.text = "M×";
                visual.Label.color =
                    new Color(0.75f, 0.42f, 0.42f);

                continue;
            }

            int margin =
                visual.Hex.EffectiveCapacity -
                visual.Hex.CurrentLoad;

            visual.Label.text =
                "M" +
                (margin > 0 ? "+" : "") +
                margin.ToString();

            switch (state)
            {
                case HexLoadState.Safe:
                    visual.Label.color =
                        new Color(0.38f, 0.84f, 0.91f);
                    break;

                case HexLoadState.Full:
                    visual.Label.color =
                        new Color(1f, 0.85f, 0.29f);
                    break;

                case HexLoadState.Overloaded:
                    visual.Label.color =
                        new Color(1f, 0.54f, 0.20f);
                    break;

                case HexLoadState.Collapsing:
                    visual.Label.color =
                        new Color(1f, 0.29f, 0.29f);
                    break;

                default:
                    visual.Label.color = Color.white;
                    break;
            }
        }
    }


    // =====================================================
    // NODE
    // =====================================================

    private void CreateNode(
    string tileName,
    Vector2 position,
    bool critical
)
    {
        // =====================================================
        // INVISIBLE CLICK / HOVER TARGET
        // =====================================================

        GameObject node =
            CreateUIObject(
                $"Node_{tileName}",
                graphArea
            );

        generatedObjects.Add(
            node
        );


        RectTransform rect =
            node.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;


        // Keep a generous interaction zone.
        // This becomes especially important once
        // the graph is reduced into PiP mode.
        float hitSize =
            nodePipMode
                ? (
                    critical
                      ? 36f
                      : 32f
          )
        : (
            critical
                ? 46f
                : 42f
          );

        rect.sizeDelta =
            new Vector2(
                hitSize,
                hitSize
            );


        Image hitImage =
            node.AddComponent<Image>();

        hitImage.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );

        hitImage.raycastTarget =
            true;


        // =====================================================
        // VISIBLE NODE
        // =====================================================

        GameObject visual =
            CreateUIObject(
                $"NodeVisual_{tileName}",
                node.transform
            );


        RectTransform visualRect =
            visual.GetComponent<RectTransform>();

        visualRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        visualRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        visualRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        visualRect.anchoredPosition =
            Vector2.zero;


        float normalSize =
    nodePipMode
        ? (
            critical
                ? 18f
                : 8f
          )
        : (
            critical
                ? 24f
                : 9f
          );


        float hoverSize =
            nodePipMode
                ? (
                    critical
                        ? 28f
                        : 16f
                  )
                : (
                    critical
                        ? 32f
                        : 18f
                  );


        visualRect.sizeDelta =
            new Vector2(
                normalSize,
                normalSize
            );


        Image visualImage =
            visual.AddComponent<Image>();

        visualImage.raycastTarget =
            false;


        if (critical)
        {
            visualImage.color =
                new Color(
                    1f,
                    0.52f,
                    0.12f,
                    1f
                );

            // Rotate only the visible diamond.
            // The interaction target remains square.
            visualRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    45f
                );
        }
        else
        {
            visualImage.color =
                new Color(
                    0.20f,
                    0.82f,
                    0.90f,
                    0.90f
                );
        }

        // =====================================================
        // CURRENTLY INSPECTED NODE
        // =====================================================

        if (nodePipMode &&
            tileName == inspectedNodeTileName)
        {
            CreateSelectedNodeBrackets(
                node.transform,
                critical
            );
        }

        // =====================================================
        // BUTTON
        // =====================================================

        Button button =
            node.AddComponent<Button>();

        button.targetGraphic =
            hitImage;

        button.transition =
            Selectable.Transition.None;


        button.onClick.AddListener(
            () =>
            {
                if (traceModeEnabled)
                {
                    SelectTraceEndpoint(tileName);
                    return;
                }

                FocusMapNode(
                    tileName
                );

                ShowNodeDebrief(
    tileName
);




            }
        );


        // =====================================================
        // HOVER FEEDBACK
        // =====================================================

        EventTrigger trigger =
            node.AddComponent<EventTrigger>();

        trigger.triggers =
            new List<EventTrigger.Entry>();


        EventTrigger.Entry enterEntry =
            new EventTrigger.Entry();

        enterEntry.eventID =
            EventTriggerType.PointerEnter;

        enterEntry.callback.AddListener(
            (eventData) =>
            {
                visualRect.sizeDelta =
                    new Vector2(
                        hoverSize,
                        hoverSize
                    );

                visualImage.color =
                    critical
                        ? new Color(
                            1f,
                            0.68f,
                            0.22f,
                            1f
                        )
                        : new Color(
                            0.55f,
                            1f,
                            1f,
                            1f
                        );

                ShowMainTopologyProbe(
                    tileName
                );
            }
        );

        trigger.triggers.Add(
            enterEntry
        );


        EventTrigger.Entry exitEntry =
    new EventTrigger.Entry();

        exitEntry.eventID =
            EventTriggerType.PointerExit;

        exitEntry.callback.AddListener(
            (eventData) =>
            {
                visualRect.sizeDelta =
                    new Vector2(
                        normalSize,
                        normalSize
                    );

                visualImage.color =
                    critical
                        ? new Color(
                            1f,
                            0.52f,
                            0.12f,
                            1f
                        )
                        : new Color(
                            0.20f,
                            0.82f,
                            0.90f,
                            0.90f
                        );

                RestoreMainTopologyStats();
            }
        );


        trigger.triggers.Add(
            exitEntry
        );

        // PATH ANALYZER — structural margin readout.
        CreateNodeMarginBadge(
            tileName,
            node.transform,
            critical
        );
    }



    private void CreateSelectedNodeBrackets(
    Transform parent,
    bool critical
)
    {
        if (parent == null)
            return;


        Color bracketColor =
            new Color(
                0.72f,
                1f,
                1f,
                1f
            );


        float halfExtent =
            critical
                ? 23f
                : 16f;

        float armLength =
            7f;

        float thickness =
            2f;


        // TOP LEFT
        CreateSelectedBracketSegment(
            parent,
            "Selected_TL_H",
            new Vector2(
                -halfExtent + armLength * 0.5f,
                halfExtent
            ),
            new Vector2(
                armLength,
                thickness
            ),
            bracketColor
        );

        CreateSelectedBracketSegment(
            parent,
            "Selected_TL_V",
            new Vector2(
                -halfExtent,
                halfExtent - armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            ),
            bracketColor
        );


        // TOP RIGHT
        CreateSelectedBracketSegment(
            parent,
            "Selected_TR_H",
            new Vector2(
                halfExtent - armLength * 0.5f,
                halfExtent
            ),
            new Vector2(
                armLength,
                thickness
            ),
            bracketColor
        );

        CreateSelectedBracketSegment(
            parent,
            "Selected_TR_V",
            new Vector2(
                halfExtent,
                halfExtent - armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            ),
            bracketColor
        );


        // BOTTOM LEFT
        CreateSelectedBracketSegment(
            parent,
            "Selected_BL_H",
            new Vector2(
                -halfExtent + armLength * 0.5f,
                -halfExtent
            ),
            new Vector2(
                armLength,
                thickness
            ),
            bracketColor
        );

        CreateSelectedBracketSegment(
            parent,
            "Selected_BL_V",
            new Vector2(
                -halfExtent,
                -halfExtent + armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            ),
            bracketColor
        );


        // BOTTOM RIGHT
        CreateSelectedBracketSegment(
            parent,
            "Selected_BR_H",
            new Vector2(
                halfExtent - armLength * 0.5f,
                -halfExtent
            ),
            new Vector2(
                armLength,
                thickness
            ),
            bracketColor
        );

        CreateSelectedBracketSegment(
            parent,
            "Selected_BR_V",
            new Vector2(
                halfExtent,
                -halfExtent + armLength * 0.5f
            ),
            new Vector2(
                thickness,
                armLength
            ),
            bracketColor
        );
    }

    private void CreateSelectedBracketSegment(
    Transform parent,
    string objectName,
    Vector2 position,
    Vector2 size,
    Color color
)
    {
        GameObject segment =
            CreateUIObject(
                objectName,
                parent
            );


        RectTransform rect =
            segment.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;


        Image image =
            segment.AddComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;
    }
    // =====================================================
    // CLEANUP
    // =====================================================

    // =====================================================
    // GRAPH NODE → WORLD HEX
    // =====================================================

    // =====================================================
    // NODE ANALYSIS
    // =====================================================

    private void ShowNodeDebrief(
        string tileName
    )
    {

        currentConceptPage = null;
        conceptBackStack.Clear();
        inspectedNodeTileName =
        tileName;
        if (activeScanner == null ||
            !activeScanner.HasScanData)
        {
            return;
        }

        if (conceptPopupObject == null ||
            conceptPopupText == null ||
            conceptPopupRect == null)
        {
            return;
        }


        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
            return;


        if (!map.tileGameObjectMap.TryGetValue(
                tileName,
                out GameObject tile))
        {
            return;
        }

        if (tile == null)
            return;


        HexCapacity hex =
            tile.GetComponent<HexCapacity>();

        if (hex == null)
        {
            hex =
                tile.GetComponentInChildren<HexCapacity>(
                    true
                );
        }

        if (hex == null)
            return;


        // ---------------------------------------------
        // CURRENT STRUCTURAL STATE
        // ---------------------------------------------

        int currentLoad =
            hex.CurrentLoad;

        int currentCapacity =
            hex.EffectiveCapacity;

        int currentMargin =
            currentCapacity -
            currentLoad;


        // ---------------------------------------------
        // CURRENT TOPOLOGY STATE
        // ---------------------------------------------

        bool critical =
            activeScanner.IsArticulationPoint(
                tileName
            );

        int degree = 0;

        if (activeScanner.LastGraph.TryGetValue(
                tileName,
                out HashSet<string> neighbours))
        {
            degree =
                neighbours.Count;
        }


        int componentsBefore =
            activeScanner.LastConnectedComponents;

        int componentsAfter =
            CountComponentsWithoutNode(
                tileName
            );


        string topologyClass =
            critical
            ? "<color=#FF851F><b>ARTICULATION POINT</b></color>"
            : "<color=#62D6E8><b>ACTIVE NODE</b></color>";


        // ---------------------------------------------
        // BUILD DEBRIEF
        // ---------------------------------------------

        conceptPopupPinned =
            true;

        lastHoveredConcept =
            null;

        string stateText =
            hex.LoadState
                .ToString()
                .ToUpperInvariant();


        string stateColor;

        switch (hex.LoadState)
        {
            case HexLoadState.Safe:
                stateColor = "#58E36D";
                break;

            case HexLoadState.Full:
                stateColor = "#FFD84A";
                break;

            case HexLoadState.Overloaded:
                stateColor = "#FF8A32";
                break;

            case HexLoadState.Collapsing:
                stateColor = "#FF4A4A";
                break;

            case HexLoadState.Collapsed:
                stateColor = "#B86B6B";
                break;

            default:
                stateColor = "#FFFFFF";
                break;
        }


        string marginColor =
            currentMargin > 0
                ? "#58E36D"
                : currentMargin == 0
                    ? "#FFD84A"
                    : "#FF4A4A";


        string capacityColor =
            currentCapacity > currentLoad
                ? "#58E36D"
                : currentCapacity == currentLoad
                    ? "#FFD84A"
                    : "#FF4A4A";
        conceptPopupText.text =
    $"<b><size=120%>NODE : {tileName}</size></b>\n" +
    $"{topologyClass}\n\n" +

    "<color=#62D6E8><b>STRUCTURAL STATE</b></color>\n" +

    $"LOAD : <b>{currentLoad}</b>\n" +

    $"CAPACITY : " +
    $"<color={capacityColor}><b>{currentCapacity}</b></color>\n" +

    $"MARGIN : " +
    $"<color={marginColor}><b>{FormatSignedValue(currentMargin)}</b></color>\n" +

    $"STATE : " +
    $"<color={stateColor}><b>{stateText}</b></color>\n\n";


        ResizeConceptPopupToFitText(
            250f
        );

        CreateInterventionDial(
            tileName
        );

        EnterNodePipMode();
    }



    private string BuildInterventionProjection(
        string tileName,
        Map1HexGrid map,
        HexCapacity hex,
        int currentLoad,
        int currentCapacity,
        int currentMargin
    )
    {
        List<PlayerUnit> unitsOnNode =
            GetLivingUnitsOnNode(
                tileName,
                map
            );


        int alliedDampenerTargets = 0;
        int enemyBallastTargets = 0;


        foreach (PlayerUnit unit in unitsOnNode)
        {
            if (unit == null)
                continue;


            // -----------------------------------------
            // MASS DAMPENER CANDIDATE
            // -----------------------------------------

            if (unit.teamID == scanTeamID &&
                unit.EffectiveLoad > 1)
            {
                alliedDampenerTargets++;
            }


            // -----------------------------------------
            // BALLAST INJECTION CANDIDATE
            // -----------------------------------------

            if (unit.teamID != scanTeamID &&
                unit.EffectiveLoad < 5)
            {
                enemyBallastTargets++;
            }
        }


        string result =
            "<color=#FFE05A><b>TEAM INTERVENTION OPTIONS</b></color>\n";


        // =================================================
        // 1 — MASS DAMPENER
        // =================================================

        result +=
            "\n<b>[1] MASS DAMPENER</b>\n";


        if (alliedDampenerTargets > 0)
        {
            int projectedLoad =
                Mathf.Max(
                    0,
                    currentLoad - 1
                );

            int projectedMargin =
                currentCapacity -
                projectedLoad;

            result +=
                $"ELIGIBLE ALLIED TARGETS // {alliedDampenerTargets}\n" +
                $"LOAD {currentLoad} → {projectedLoad}     " +
                $"MARGIN {FormatSignedValue(currentMargin)} → " +
                $"{FormatSignedValue(projectedMargin)}\n" +
                $"RESULT // {GetProjectedStructuralState(projectedLoad, currentCapacity)}\n";
        }
        else
        {
            result +=
                "UNAVAILABLE HERE // NO ALLIED UNIT WITH REDUCIBLE LOAD\n";
        }


        // =================================================
        // 2 — BALLAST INJECTION
        // =================================================

        result +=
            "\n<b>[2] BALLAST INJECTION</b>\n";


        if (enemyBallastTargets > 0)
        {
            int projectedLoad =
                currentLoad + 1;

            int projectedMargin =
                currentCapacity -
                projectedLoad;

            result +=
                $"ELIGIBLE ENEMY TARGETS // {enemyBallastTargets}\n" +
                $"LOAD {currentLoad} → {projectedLoad}     " +
                $"MARGIN {FormatSignedValue(currentMargin)} → " +
                $"{FormatSignedValue(projectedMargin)}\n" +
                $"RESULT // {GetProjectedStructuralState(projectedLoad, currentCapacity)}\n";
        }
        else
        {
            result +=
                "UNAVAILABLE HERE // NO ENEMY UNIT WITH INCREASABLE LOAD\n";
        }


        // =================================================
        // 3 — STRUCTURAL REINFORCEMENT
        // =================================================

        result +=
            "\n<b>[3] STRUCTURAL REINFORCEMENT</b>\n";


        if (!hex.IsCollapseSequenceRunning)
        {
            int projectedCapacity =
                currentCapacity + 3;

            int projectedMargin =
                projectedCapacity -
                currentLoad;

            result +=
                $"CAPACITY {currentCapacity} → {projectedCapacity}     " +
                $"MARGIN {FormatSignedValue(currentMargin)} → " +
                $"{FormatSignedValue(projectedMargin)}\n" +
                $"RESULT // {GetProjectedStructuralState(currentLoad, projectedCapacity)}\n";
        }
        else
        {
            result +=
                "UNAVAILABLE // COLLAPSE SEQUENCE ALREADY RUNNING\n";
        }


        // =================================================
        // 4 — NETWORK STRESS
        // =================================================

        result +=
            "\n<b>[4] NETWORK STRESS</b>\n";


        if (!hex.IsCollapseSequenceRunning &&
            currentCapacity > 0)
        {
            int projectedCapacity =
                Mathf.Max(
                    0,
                    currentCapacity - 3
                );

            int projectedMargin =
                projectedCapacity -
                currentLoad;

            result +=
                $"CAPACITY {currentCapacity} → {projectedCapacity}     " +
                $"MARGIN {FormatSignedValue(currentMargin)} → " +
                $"{FormatSignedValue(projectedMargin)}\n" +
                $"RESULT // {GetProjectedStructuralState(currentLoad, projectedCapacity)}\n";
        }
        else
        {
            result +=
                "UNAVAILABLE // CAPACITY CANNOT BE REDUCED\n";
        }


        return result;
    }


    private List<PlayerUnit> GetLivingUnitsOnNode(
        string tileName,
        Map1HexGrid map
    )
    {
        List<PlayerUnit> result =
            new List<PlayerUnit>();


        if (map == null)
            return result;


        if (!map.tileNameToGrid.TryGetValue(
                tileName,
                out Vector2Int tileCoord))
        {
            return result;
        }


        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );


        foreach (PlayerUnit unit in units)
        {
            if (unit == null)
                continue;

            if (unit.IsDead)
                continue;

            if (unit.currentTileCoord !=
                tileCoord)
            {
                continue;
            }

            result.Add(
                unit
            );
        }


        return result;
    }


    private int CountComponentsWithoutNode(
        string removedNode
    )
    {
        if (activeScanner == null ||
            !activeScanner.HasScanData)
        {
            return 0;
        }


        IReadOnlyDictionary<string, HashSet<string>> graph =
            activeScanner.LastGraph;


        HashSet<string> visited =
            new HashSet<string>();


        int componentCount = 0;


        foreach (string node in graph.Keys)
        {
            if (node == removedNode)
                continue;

            if (visited.Contains(node))
                continue;


            componentCount++;


            Queue<string> queue =
                new Queue<string>();

            queue.Enqueue(
                node
            );

            visited.Add(
                node
            );


            while (queue.Count > 0)
            {
                string current =
                    queue.Dequeue();


                if (!graph.TryGetValue(
                        current,
                        out HashSet<string> neighbours))
                {
                    continue;
                }


                foreach (string neighbour in neighbours)
                {
                    if (neighbour == removedNode)
                        continue;

                    if (!graph.ContainsKey(neighbour))
                        continue;

                    if (!visited.Add(neighbour))
                        continue;


                    queue.Enqueue(
                        neighbour
                    );
                }
            }
        }


        return componentCount;
    }


    private string GetProjectedStructuralState(
        int load,
        int capacity
    )
    {
        if (load > capacity)
        {
            return
                "<color=#FF851F><b>OVERLOADED</b></color>";
        }

        if (load == capacity)
        {
            return
                "<color=#FFE05A><b>FULL</b></color>";
        }

        return
            "<color=#62D6E8><b>SAFE</b></color>";
    }


    private string FormatSignedValue(
        int value
    )
    {
        if (value > 0)
        {
            return
                "+" + value;
        }

        return
            value.ToString();
    }

    private void EnterNodePipMode()
    {
        if (panelObject == null ||
            graphArea == null ||
            conceptPopupObject == null ||
            conceptPopupRect == null ||
            activeScanner == null)
        {
            return;
        }


        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();


        if (panelRect == null)
            return;


        // =====================================================
        // SAVE NORMAL ANALYZER LAYOUT ONCE
        // =====================================================

        if (!pipLayoutSaved)
        {
            savedPanelAnchorMin =
                panelRect.anchorMin;

            savedPanelAnchorMax =
                panelRect.anchorMax;

            savedPanelPivot =
                panelRect.pivot;

            savedPanelSizeDelta =
                panelRect.sizeDelta;

            savedPanelAnchoredPosition =
                panelRect.anchoredPosition;


            savedGraphAnchorMin =
                graphArea.anchorMin;

            savedGraphAnchorMax =
                graphArea.anchorMax;

            savedGraphPivot =
                graphArea.pivot;

            savedGraphOffsetMin =
                graphArea.offsetMin;

            savedGraphOffsetMax =
                graphArea.offsetMax;

            savedGraphAnchoredPosition =
                graphArea.anchoredPosition;


            savedPopupAnchorMin =
                conceptPopupRect.anchorMin;

            savedPopupAnchorMax =
                conceptPopupRect.anchorMax;

            savedPopupPivot =
                conceptPopupRect.pivot;

            savedPopupSizeDelta =
                conceptPopupRect.sizeDelta;

            savedPopupAnchoredPosition =
                conceptPopupRect.anchoredPosition;


            pipLayoutSaved =
                true;
        }


        nodePipMode =
            true;


        // =====================================================
        // LARGE NODE WORKSPACE
        // Uses the position previously occupied by the analyzer.
        // =====================================================

        conceptPopupRect.anchorMin =
            new Vector2(
                1f,
                0.5f
            );

        conceptPopupRect.anchorMax =
            new Vector2(
                1f,
                0.5f
            );

        conceptPopupRect.pivot =
            new Vector2(
                1f,
                0.5f
            );

        conceptPopupRect.sizeDelta =
            new Vector2(
                570f,
                720f
            );

        conceptPopupRect.anchoredPosition =
            new Vector2(
                -35f,
                0f
            );


        // Keep node information in the upper-left,
        // leaving room for the topology inset.
        if (conceptPopupText != null)
        {
            RectTransform textRect =
                conceptPopupText.rectTransform;

            conceptPopupText.text =
                conceptPopupText.text
                    .Replace(
                        "CAPACITY :",
                        "\nCAPACITY :"
        )
        .Replace(
            "STATE :",
            "\nSTATE :"
        );

            textRect.anchorMin =
                new Vector2(
                    0f,
                    1f
                );

            textRect.anchorMax =
                new Vector2(
                    0f,
                    1f
                );

            textRect.pivot =
                new Vector2(
                    0f,
                    1f
                );

            textRect.anchoredPosition =
                new Vector2(
                    22f,
                    -22f
                );

            textRect.sizeDelta =
                new Vector2(
                    220f,
                    300f
                );
        }


        // =====================================================
        // GRAPH-ONLY PICTURE IN PICTURE
        // =====================================================

        panelRect.anchorMin =
            new Vector2(
                1f,
                1f
            );

        panelRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        panelRect.pivot =
            new Vector2(
                1f,
                1f
            );

        panelRect.sizeDelta =
            new Vector2(
                300f,
                300f
            );

        panelRect.anchoredPosition =
            new Vector2(
                -75f,
                -220f // this value makes the pip appear below the concept popup, but it can be adjusted as needed
            );


        // Hide every analyzer child except the graph.
        for (int i = 0;
             i < panelObject.transform.childCount;
             i++)
        {
            Transform child =
                panelObject.transform.GetChild(i);

            if (child == null)
                continue;

            child.gameObject.SetActive(
                child == graphArea
            );
        }


        // Make GraphArea fill the inset.
        graphArea.anchorMin =
            Vector2.zero;

        graphArea.anchorMax =
            Vector2.one;

        graphArea.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        graphArea.offsetMin =
            new Vector2(
                10f,
                10f
            );

        graphArea.offsetMax =
            new Vector2(
                -10f,
                -10f
            );

        graphArea.anchoredPosition =
            Vector2.zero;


        panelObject.SetActive(
            true
        );

        conceptPopupObject.SetActive(
            true
        );


        // Main workspace behind, PiP graph above.
        conceptPopupObject.transform.SetAsLastSibling();

        panelObject.transform.SetAsLastSibling();


        // Rebuild graph using the PiP dimensions.
        ClearGraph();

        Canvas.ForceUpdateCanvases();

        DrawGraph(
            activeScanner
        );

        Canvas.ForceUpdateCanvases();
    }


    private void DockPipToMassSelector()
    {
        if (panelObject == null ||
            conceptPopupRect == null)
        {
            return;
        }

        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();

        RectTransform parentRect =
            panelRect != null
                ? panelRect.parent as RectTransform
                : null;

        if (panelRect == null ||
            parentRect == null)
        {
            return;
        }


        Canvas.ForceUpdateCanvases();


        // =====================================================
        // MASS SELECTOR WORLD EDGES
        // =====================================================

        Vector3[] popupCorners =
            new Vector3[4];

        conceptPopupRect.GetWorldCorners(
            popupCorners
        );

        Vector3 popupBottomCenter =
            (
                popupCorners[0] +
                popupCorners[3]
            ) * 0.5f;

        Vector3 popupTopCenter =
            (
                popupCorners[1] +
                popupCorners[2]
            ) * 0.5f;


        // =====================================================
        // AVAILABLE SPACE IN PiP PARENT
        // =====================================================

        Vector3 popupBottomLocal =
            parentRect.InverseTransformPoint(
                popupBottomCenter
            );

        Vector3 popupTopLocal =
            parentRect.InverseTransformPoint(
                popupTopCenter
            );


        Vector3[] pipCorners =
            new Vector3[4];

        panelRect.GetWorldCorners(
            pipCorners
        );

        Vector3 pipBottomLocal =
            parentRect.InverseTransformPoint(
                pipCorners[0]
            );

        Vector3 pipTopLocal =
            parentRect.InverseTransformPoint(
                pipCorners[1]
            );

        float pipHeight =
            Mathf.Abs(
                pipTopLocal.y -
                pipBottomLocal.y
            );


        const float gap =
            8f;

        float spaceBelow =
            popupBottomLocal.y -
            parentRect.rect.yMin;

        float spaceAbove =
            parentRect.rect.yMax -
            popupTopLocal.y;


        bool dockBelow =
            spaceBelow >=
            pipHeight + gap;

        if (!dockBelow &&
            spaceAbove <
            pipHeight + gap)
        {
            // Neither side completely fits.
            // Use whichever side has more room.
            dockBelow =
                spaceBelow >=
                spaceAbove;
        }


        // Convert an 8 UI-unit gap into world space.
        Vector3 gapWorld =
            parentRect.TransformVector(
                new Vector3(
                    0f,
                    gap,
                    0f
                )
            );


        // =====================================================
        // MAGNETIC DOCK
        // =====================================================

        if (dockBelow)
        {
            // PiP top-center attaches to selector bottom-center.
            panelRect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );

            panelRect.position =
                popupBottomCenter -
                gapWorld;

            Debug.Log(
                "[TACTICAL SCANNER] PiP docked BELOW Mass selector."
            );
        }
        else
        {
            // PiP bottom-center attaches to selector top-center.
            panelRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );

            panelRect.position =
                popupTopCenter +
                gapWorld;

            Debug.Log(
                "[TACTICAL SCANNER] PiP docked ABOVE Mass selector."
            );
        }
    }
    private void RestoreAnalyzerFromPip()
    {
        if (!nodePipMode &&
            !pipLayoutSaved)
        {
            return;
        }


        nodePipMode =
            false;


        if (!pipLayoutSaved)
            return;


        if (panelObject != null)
        {
            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();

            if (panelRect != null)
            {
                panelRect.anchorMin =
                    savedPanelAnchorMin;

                panelRect.anchorMax =
                    savedPanelAnchorMax;

                panelRect.pivot =
                    savedPanelPivot;

                panelRect.sizeDelta =
                    savedPanelSizeDelta;

                panelRect.anchoredPosition =
                    savedPanelAnchoredPosition;
            }


            // Restore all normal analyzer children.
            for (int i = 0;
                 i < panelObject.transform.childCount;
                 i++)
            {
                Transform child =
                    panelObject.transform.GetChild(i);

                if (child != null)
                {
                    child.gameObject.SetActive(
                        true
                    );
                }
            }
        }


        if (graphArea != null)
        {
            graphArea.anchorMin =
                savedGraphAnchorMin;

            graphArea.anchorMax =
                savedGraphAnchorMax;

            graphArea.pivot =
                savedGraphPivot;

            graphArea.offsetMin =
                savedGraphOffsetMin;

            graphArea.offsetMax =
                savedGraphOffsetMax;

            graphArea.anchoredPosition =
                savedGraphAnchoredPosition;
        }


        if (conceptPopupRect != null)
        {
            conceptPopupRect.anchorMin =
                savedPopupAnchorMin;

            conceptPopupRect.anchorMax =
                savedPopupAnchorMax;

            conceptPopupRect.pivot =
                savedPopupPivot;

            conceptPopupRect.sizeDelta =
                savedPopupSizeDelta;

            conceptPopupRect.anchoredPosition =
                savedPopupAnchoredPosition;
        }


        // Restore the normal concept text layout.
        // PiP uses a narrow, fixed-size node readout.
        // Concept pages need the full popup width.

        if (conceptPopupText != null)
        {
            RectTransform textRect =
                conceptPopupText.rectTransform;

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot =
                new Vector2(0.5f, 0.5f);

            textRect.offsetMin =
                new Vector2(22f, 20f);

            textRect.offsetMax =
                new Vector2(-22f, -20f);
        }


        pipLayoutSaved =
            false;
    }
    private void FocusMapNode(
    string tileName
)
    {
        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
            return;

        if (!map.tileGameObjectMap.TryGetValue(
                tileName,
                out GameObject tile))
        {
            Debug.LogWarning(
                $"[TACTICAL SCANNER] Cannot locate map node {tileName}."
            );

            return;
        }

        if (tile == null)
            return;

        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow == null)
            return;

        if (!inspectingMapNode)
        {
            inspectionReturnTarget =
                cameraFollow.target;

            inspectingMapNode =
                true;
        }

        cameraFollow.SetTarget(
            tile.transform
        );

        Debug.Log(
            $"[TACTICAL SCANNER] Inspecting {tileName}. " +
            $"Camera remains focused until scanner is closed."
        );
    }


    private void CreateInterventionDial(
    string tileName
    )
    {
        DestroyInterventionDial();

        if (conceptPopupObject == null ||
            conceptPopupRect == null)
        {
            return;
        }

        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
            return;

        if (!map.tileGameObjectMap.TryGetValue(
                tileName,
                out GameObject tile))
        {
            return;
        }

        if (tile == null)
            return;

        HexCapacity capacity =
            tile.GetComponent<HexCapacity>();

        if (capacity == null)
        {
            capacity =
                tile.GetComponentInChildren<HexCapacity>();
        }

        if (capacity == null)
            return;


        // =====================================================
        // ACTIVE UNIT / TEAM
        // =====================================================

        PlayerUnit activeUnit =
            TurnManager.Instance != null
                ? TurnManager.Instance.current
                : null;

        if (activeUnit == null)
            return;


        // =====================================================
        // UNITS CURRENTLY ON THIS NODE
        // =====================================================

        List<PlayerUnit> nodeUnits =
            GetDialUnitsOnNode(
                map,
                tileName
            );

        PlayerUnit massTarget =
            null;

        PlayerUnit ballastTarget =
            null;

        int alliedUnitCount =
            0;

        int eligibleMassTargetCount =
            0;

        List<PlayerUnit> eligibleMassTargets =
            new List<PlayerUnit>();

        int enemyUnitCount =
            0;

        foreach (PlayerUnit unit in nodeUnits)
        {
            if (unit == null ||
                unit.IsDead)
            {
                continue;
            }

            if (unit.teamID ==
                activeUnit.teamID)
            {
                alliedUnitCount++;

                if (unit.EffectiveLoad > 1)
                {
                    eligibleMassTargetCount++;

                    eligibleMassTargets.Add(
                        unit
                    );

                    if (massTarget == null)
                    {
                        massTarget =
                            unit;
                    }
                }
            }
            else
            {
                enemyUnitCount++;

                if (ballastTarget == null &&
                    unit.EffectiveLoad < 5)
                {
                    ballastTarget =
                        unit;
                }
            }
        }


        // =====================================================
        // AVAILABILITY
        // =====================================================

        bool massAvailable =
            massTarget != null;

        bool ballastAvailable =
            ballastTarget != null;

        bool reinforcementAvailable =
            !capacity.IsCollapsed &&
            !capacity.IsCollapseSequenceRunning;

        bool stressAvailable =
            !capacity.IsCollapsed &&
            !capacity.IsCollapseSequenceRunning &&
            capacity.EffectiveCapacity > 0;


        // =====================================================
        // CURRENT STRUCTURAL STATE
        // =====================================================

        int currentLoad =
            capacity.CurrentLoad;

        int currentCapacity =
            capacity.EffectiveCapacity;

        int currentMargin =
            currentCapacity -
            currentLoad;

        string currentState =
            capacity.LoadState
                .ToString()
                .ToUpperInvariant();

        interventionDialDefaultText =
    $"MARGIN {FormatDialSigned(currentMargin)}\n" +
    currentState;


        // =====================================================
        // PROJECTIONS
        // =====================================================

        int massProjectedLoad =
            Mathf.Max(
                0,
                currentLoad - 1
            );

        int massProjectedMargin =
            currentCapacity -
            massProjectedLoad;

        string massProjectedState =
            GetDialProjectedState(
                massProjectedLoad,
                currentCapacity
            );


        int ballastProjectedLoad =
            currentLoad + 1;

        int ballastProjectedMargin =
            currentCapacity -
            ballastProjectedLoad;

        string ballastProjectedState =
            GetDialProjectedState(
                ballastProjectedLoad,
                currentCapacity
            );


        int reinforcementProjectedCapacity =
            currentCapacity + 3;

        int reinforcementProjectedMargin =
            reinforcementProjectedCapacity -
            currentLoad;

        string reinforcementProjectedState =
            GetDialProjectedState(
                currentLoad,
                reinforcementProjectedCapacity
            );


        int stressProjectedCapacity =
            Mathf.Max(
                0,
                currentCapacity - 3
            );

        int stressProjectedMargin =
            stressProjectedCapacity -
            currentLoad;

        string stressProjectedState =
            GetDialProjectedState(
                currentLoad,
                stressProjectedCapacity
            );


        // =====================================================
        // HOVER TEXT
        // =====================================================

        string massHoverText;

        if (massAvailable)
        {
            massHoverText =
                $"LOAD {currentLoad} → {massProjectedLoad}\n" +
                $"MARGIN {FormatDialSigned(currentMargin)} → " +
                $"{FormatDialSigned(massProjectedMargin)}\n" +
                massProjectedState;
        }
        else if (alliedUnitCount > 0)
        {
            massHoverText =
                "UNAVAILABLE\n" +
                "LOAD AT MINIMUM";
        }
        else
        {
            massHoverText =
                "UNAVAILABLE\n" +
                "NO ALLIED UNIT";
        }


        string ballastHoverText;

        if (ballastAvailable)
        {
            ballastHoverText =
                $"LOAD {currentLoad} → {ballastProjectedLoad}\n" +
                $"MARGIN {FormatDialSigned(currentMargin)} → " +
                $"{FormatDialSigned(ballastProjectedMargin)}\n" +
                ballastProjectedState;
        }
        else if (enemyUnitCount > 0)
        {
            ballastHoverText =
                "UNAVAILABLE\n" +
                "LOAD AT MAXIMUM";
        }
        else
        {
            ballastHoverText =
                "UNAVAILABLE\n" +
                "NO ENEMY UNIT";
        }


        string reinforcementHoverText;

        if (reinforcementAvailable)
        {
            reinforcementHoverText =
                $"CAP {currentCapacity} → " +
                $"{reinforcementProjectedCapacity}\n" +
                $"MARGIN {FormatDialSigned(currentMargin)} → " +
                $"{FormatDialSigned(reinforcementProjectedMargin)}\n" +
                reinforcementProjectedState;
        }
        else
        {
            reinforcementHoverText =
                "UNAVAILABLE\n" +
                "FAILURE IN PROGRESS";
        }

        string stressHoverText;

        if (stressAvailable)
        {
            stressHoverText =
                $"CAP {currentCapacity} → " +
                $"{stressProjectedCapacity}\n" +
                $"MARGIN {FormatDialSigned(currentMargin)} → " +
                $"{FormatDialSigned(stressProjectedMargin)}\n" +
                stressProjectedState;
        }
        else if (capacity.EffectiveCapacity <= 0)
        {
            stressHoverText =
                "UNAVAILABLE\n" +
                "CAPACITY AT MINIMUM";
        }
        else
        {
            stressHoverText =
                "UNAVAILABLE\n" +
                "FAILURE IN PROGRESS";
        }


        // =====================================================
        // SIZE NODE INSPECTION PANEL
        // =====================================================

        float textHeight =
     conceptPopupText != null
         ? conceptPopupText.preferredHeight
         : 0f;

        float dialHeight =
    300f;

        float verticalPadding =
            80f;

        conceptPopupRect.sizeDelta =
            new Vector2(
                540f,
                textHeight +
                dialHeight +
                verticalPadding
            );


        // =====================================================
        // DIAL ROOT
        // =====================================================

        interventionDialObject =
            CreateUIObject(
                "InterventionDial",
                conceptPopupObject.transform
            );

        RectTransform dialRect =
            interventionDialObject
                .GetComponent<RectTransform>();

        dialRect.anchorMin =
            new Vector2(
                0.5f,
                0f
            );

        dialRect.anchorMax =
            new Vector2(
                0.5f,
                0f
            );

        dialRect.pivot =
            new Vector2(
                0.5f,
                0f
            );

        dialRect.sizeDelta =
            new Vector2(
                500f,
                315f
            );

        dialRect.anchoredPosition =
            new Vector2(
            0f,
            18f
            );


        // =====================================================
        // HEADING
        // =====================================================

        TextMeshProUGUI heading =
            CreateText(
                "InterventionHeading",
                interventionDialObject.transform,
                20f
            );

        heading.text =
            "TACTICAL INTERVENTIONS";

        heading.fontStyle =
            FontStyles.Bold;

        heading.alignment =
            TextAlignmentOptions.Center;

        heading.characterSpacing =
            2f;

        heading.color =
            new Color(
                1f,
                0.86f,
                0.22f,
                1f
            );

        heading.raycastTarget =
            false;

        RectTransform headingRect =
            heading.rectTransform;

        headingRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        headingRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        headingRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        headingRect.anchoredPosition =
            new Vector2(
            0f,
            146f
            );

        headingRect.sizeDelta =
            new Vector2(
                380f,
                34f
            );

        // =====================================================
        // CENTER STATUS HUB
        // =====================================================

        GameObject hub =
            CreateUIObject(
                "InterventionHub",
                interventionDialObject.transform
            );

        RectTransform hubRect =
            hub.GetComponent<RectTransform>();

        hubRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        hubRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        hubRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        hubRect.anchoredPosition =
            Vector2.zero;

        hubRect.sizeDelta =
            new Vector2(
                120f,
                78f
           );

        Image hubImage =
            hub.AddComponent<Image>();

        hubImage.color =
            new Color(
                0.025f,
                0.055f,
                0.075f,
                0.96f
            );

        hubImage.raycastTarget =
            false;

        Outline hubOutline =
            hub.AddComponent<Outline>();

        hubOutline.effectColor =
            new Color(
                0.20f,
                0.82f,
                0.90f,
                0.85f
            );

        hubOutline.effectDistance =
            new Vector2(
                1.5f,
                -1.5f
            );


        interventionDialCenterText =
            CreateText(
                "InterventionHubText",
                hub.transform,
                14f
            );

        interventionDialCenterText.text =
            interventionDialDefaultText;

        interventionDialCenterText.alignment =
            TextAlignmentOptions.Center;

        interventionDialCenterText.enableWordWrapping =
            true;

        interventionDialCenterText.raycastTarget =
            false;

        RectTransform centerTextRect =
            interventionDialCenterText.rectTransform;

        centerTextRect.anchorMin =
            Vector2.zero;

        centerTextRect.anchorMax =
            Vector2.one;

        centerTextRect.offsetMin =
            new Vector2(
                6f,
                6f
            );

        centerTextRect.offsetMax =
            new Vector2(
                -6f,
                -6f
            );


        // =====================================================
        // VISUAL LANGUAGE
        //
        // VERTICAL   = LOAD
        // HORIZONTAL = CAPACITY
        //
        // TOP    +LOAD
        // BOTTOM -LOAD
        // LEFT   +CAP
        // RIGHT  -CAP
        // =====================================================

        Color supportColor =
            new Color(
                0.06f,
                0.43f,
                0.50f,
                0.95f
            );

        Color attackColor =
            new Color(
                0.58f,
                0.24f,
                0.08f,
                0.95f
            );


        // TOP
        Button ballastInjectionButton =
            CreateDialControl(
                "BallastInjectionControl",
                interventionDialObject.transform,
                "BALLAST\nINJECTION\n+LOAD",
                new Vector2(
                    0f,
                    84f
                ),
                new Vector2(
                    184f,
                    82f
                ),
                ballastAvailable,
                attackColor,
                ballastHoverText
            );

        if (ballastAvailable &&
            ballastInjectionButton != null)
        {
            EventTrigger ballastTrigger =
                ballastInjectionButton.GetComponent<EventTrigger>();

            if (ballastTrigger != null)
            {
                EventTrigger.Entry ballastSelectorEntry =
                    new EventTrigger.Entry();

                ballastSelectorEntry.eventID =
                    EventTriggerType.PointerDown;

                ballastSelectorEntry.callback.AddListener(
                    (eventData) =>
                    {
                        List<PlayerUnit> eligibleEnemies =
                            new List<PlayerUnit>();

                        foreach (PlayerUnit unit in nodeUnits)
                        {
                            if (unit == null ||
                                unit.IsDead)
                            {
                                continue;
                            }

                            if (unit.teamID ==
                                activeUnit.teamID)
                            {
                                continue;
                            }

                            if (unit.EffectiveLoad >= 5)
                                continue;

                            eligibleEnemies.Add(
                                unit
                            );
                        }

                        Debug.Log(
                            $"[TACTICAL SCANNER] " +
                            $"BALLAST INJECTION target selector | " +
                            $"Node={tileName} | " +
                            $"EligibleEnemies={eligibleEnemies.Count}"
                        );

                        StartCoroutine(
                            ShowBallastTargetSelectorNextFrame(
                                tileName,
                                new List<PlayerUnit>(
                                    eligibleEnemies
                                )
                            )
                        );
                    }
                );

                ballastTrigger.triggers.Add(
                    ballastSelectorEntry
                );
            }
        }


        // BOTTOM
        Button massDampenerButton =
            CreateDialControl(
                "MassDampenerControl",
                interventionDialObject.transform,
                "MASS\nDAMPENER\n-LOAD",
                new Vector2(
                    0f,
                    -84f
                ),
                new Vector2(
                    184f,
                    82f
                ),
                massAvailable,
                supportColor,
                massHoverText
            );

        if (massAvailable &&
            massDampenerButton != null)
        {
            EventTrigger massTrigger =
                massDampenerButton.GetComponent<EventTrigger>();

            if (massTrigger != null)
            {
                EventTrigger.Entry massSelectorEntry =
                    new EventTrigger.Entry();

                massSelectorEntry.eventID =
                    EventTriggerType.PointerDown;

                massSelectorEntry.callback.AddListener(
                    (eventData) =>
                    {
                        List<PlayerUnit> eligibleAllies =
                            new List<PlayerUnit>();

                        foreach (PlayerUnit unit in nodeUnits)
                        {
                            if (unit == null ||
                                unit.IsDead)
                            {
                                continue;
                            }

                            if (unit.teamID !=
                                activeUnit.teamID)
                            {
                                continue;
                            }

                            if (unit.EffectiveLoad <= 1)
                                continue;

                            eligibleAllies.Add(
                                unit
                            );
                        }

                        Debug.Log(
                            $"[TACTICAL SCANNER] " +
                            $"MASS DAMPENER target selector | " +
                            $"Node={tileName} | " +
                            $"EligibleAllies={eligibleAllies.Count}"
                        );

                        StartCoroutine(
                            ShowMassTargetSelectorNextFrame(
                                tileName,
                                new List<PlayerUnit>(
                                    eligibleAllies
                                )
                            )
                       );
                    }
                );

                massTrigger.triggers.Add(
                    massSelectorEntry
                );
            }
        }


        // LEFT
        Button structuralReinforcementButton =
            CreateDialControl(
                "StructuralReinforcementControl",
                interventionDialObject.transform,
                "STRUCTURAL\nREINFORCEMENT\n+CAP",
                new Vector2(
                    -155f,
                    0f
                ),
                new Vector2(
                    184f,
                    82f
                ),
                reinforcementAvailable,
                supportColor,
                reinforcementHoverText
            );

        if (reinforcementAvailable &&
            structuralReinforcementButton != null)
        {
            EventTrigger reinforcementTrigger =
                structuralReinforcementButton.GetComponent<EventTrigger>();

            if (reinforcementTrigger != null)
            {
                EventTrigger.Entry reinforcementExecuteEntry =
                    new EventTrigger.Entry();

                reinforcementExecuteEntry.eventID =
                    EventTriggerType.PointerDown;

                reinforcementExecuteEntry.callback.AddListener(
                    (eventData) =>
                    {
                        Debug.Log(
                            $"[TACTICAL SCANNER] " +
                            $"STRUCTURAL REINFORCEMENT COMMAND | " +
                            $"Node={tileName} | " +
                            $"CapacityBefore={capacity.EffectiveCapacity}"
                        );

                        ExecuteStructuralReinforcementFromDial(
                            tileName,
                            capacity
                        );
                    }
                );

                reinforcementTrigger.triggers.Add(
                    reinforcementExecuteEntry
                );
            }
        }


        // RIGHT
        Button networkStressButton =
            CreateDialControl(
                "NetworkStressControl",
                interventionDialObject.transform,
                "NETWORK\nSTRESS\n-CAP",
                new Vector2(
                    155f,
                    0f
                ),
                new Vector2(
                    184f,
                    82f
                ),
                stressAvailable,
                attackColor,
                stressHoverText
            );

        if (stressAvailable &&
            networkStressButton != null)
        {
            EventTrigger stressTrigger =
                networkStressButton.GetComponent<EventTrigger>();

            if (stressTrigger != null)
            {
                EventTrigger.Entry stressExecuteEntry =
                    new EventTrigger.Entry();

                stressExecuteEntry.eventID =
                    EventTriggerType.PointerDown;

                stressExecuteEntry.callback.AddListener(
                    (eventData) =>
                    {
                        Debug.Log(
                            $"[TACTICAL SCANNER] " +
                            $"NETWORK STRESS COMMAND | " +
                            $"Node={tileName} | " +
                            $"CapacityBefore={capacity.EffectiveCapacity}"
                        );

                        ExecuteNetworkStressFromDial(
                            tileName,
                            capacity
                        );
                    }
                );

                stressTrigger.triggers.Add(
                    stressExecuteEntry
                );
            }
        }
    }

    private void ExecuteStructuralReinforcementFromDial(
    string tileName,
    HexCapacity target
    )
    {
        if (target == null)
            return;

        if (subsystemController == null)
        {
            subsystemController =
                FindFirstObjectByType<SubsystemController>();
        }

        if (subsystemController == null)
        {
            Debug.LogWarning(
                "[TACTICAL SCANNER] SubsystemController not found."
            );

            return;
        }



        int capacityBefore =
            target.EffectiveCapacity;


        bool success =
            subsystemController
                .TryStructuralReinforcementOnHex(
                    target
                );

        if (!success)
        {
            if (interventionDialCenterText != null)
            {
                interventionDialCenterText.text =
                    "COMMAND FAILED";
            }

            return;
        }

        // Play VFX only when reinforcement succeeds.
        StructuralReinforcementVFX.Play(target);

        // Recalculate movement markers using the new capacity.
        TurnManager tm = TurnManager.Instance;

        if (tm != null && tm.current != null)
        {
            tm.ShowReachFor(tm.current.gameObject);
        }
        int capacityAfter =
            target.EffectiveCapacity;


        Debug.Log(
            $"[TACTICAL SCANNER] Structural Reinforcement executed on {tileName} | " +
            $"Capacity={capacityBefore} → {capacityAfter}"
        );


        // Rebuild the page first so all structural values
        // and intervention availability reflect the new state.
        ShowNodeDebrief(
            tileName
        );


        // Then override the center hub with immediate
        // confirmation of the executed command.
        if (interventionDialCenterText != null)
        {
            interventionDialCenterText.text =
                "APPLIED\n" +
                $"CAP {capacityBefore} → {capacityAfter}";
        }

        // Return camera to the current active unit
        // after the standardized tactical hold.
        StartCoroutine(
    RunInterventionCameraSequence(
        target.transform
    )
                      );
    }


    private void ExecuteNetworkStressFromDial(
    string tileName,
    HexCapacity target
    )
    {
        if (target == null)
            return;

        if (subsystemController == null)
        {
            subsystemController =
                FindFirstObjectByType<SubsystemController>();
        }

        if (subsystemController == null)
        {
            Debug.LogWarning(
                "[TACTICAL SCANNER] SubsystemController not found."
            );

            return;
        }


        int capacityBefore =
            target.EffectiveCapacity;


        bool success =
            subsystemController
                .TryNetworkStressOnHex(
                    target
                );

        if (!success)
        {
            if (interventionDialCenterText != null)
            {
                interventionDialCenterText.text =
                    "COMMAND FAILED";
            }

            return;
        }

        // NETWORK STRESS — STRUCTURAL FRACTURE VFX
        NetworkStressVFX.Play(
            target
        );


        int capacityAfter =
            target.EffectiveCapacity;


        Debug.Log(
            $"[TACTICAL SCANNER] Network Stress executed on {tileName} | " +
            $"Capacity={capacityBefore} → {capacityAfter}"
        );


        ShowNodeDebrief(
    tileName
);

        if (interventionDialCenterText != null)
        {
            interventionDialCenterText.text =
                "APPLIED\n" +
                $"CAP {capacityBefore} → {capacityAfter}";
        }

        // Same camera sequence as Structural Reinforcement.
        StartCoroutine(
            RunInterventionCameraSequence(
                target.transform
            )
        );
    }

    private void ExecuteMassDampenerFromDial(
    string tileName,
    PlayerUnit target
    )
    {
        if (target == null)
            return;

        if (subsystemController == null)
        {
            subsystemController =
                FindFirstObjectByType<SubsystemController>();
        }

        if (subsystemController == null)
        {
            Debug.LogWarning(
                "[TACTICAL SCANNER] SubsystemController not found."
            );

            return;
        }


        int loadBefore =
            target.EffectiveLoad;


        bool success =
            subsystemController
                .TryMassDampenerOnUnit(
                    target
                );

        if (!success)
        {
            if (interventionDialCenterText != null)
            {
                interventionDialCenterText.text =
                    "COMMAND FAILED";
            }

            return;
        }

        // MASS DAMPENER — GRAVITATIONAL RELEASE VFX
        MassDampenerVFX.Play(target);

        int loadAfter =
            target.EffectiveLoad;


        Debug.Log(
            $"[TACTICAL SCANNER] Mass Dampener executed on {target.name} | " +
            $"Node={tileName} | " +
            $"Load={loadBefore} → {loadAfter}"
        );


        ShowNodeDebrief(
            tileName
        );


        if (interventionDialCenterText != null)
        {
            interventionDialCenterText.text =
                "APPLIED\n" +
                $"LOAD {loadBefore} → {loadAfter}";
        }

        StartCoroutine(
    RunInterventionCameraSequence(
        target.transform
    )
);
    }



    private IEnumerator ShowMassTargetSelectorNextFrame(
        string tileName,
        List<PlayerUnit> targets
    )
    {
        // Allow the original MASS DAMPENER click
        // to finish completely.
        yield return null;


        // The normal concept-popup click handling can
        // deactivate the popup during that original click.
        // Re-open and pin it before building the selector.
        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(
                true
            );
        }

        conceptPopupPinned =
            true;


        if (interventionDialObject != null)
        {
            interventionDialObject.SetActive(
                true
            );
        }


        ShowMassTargetSelector(
            tileName,
            targets
        );

        Canvas.ForceUpdateCanvases();

        DockPipToMassSelector();
        // ShowMassTargetSelector reuses the current dial.
        // Make absolutely sure both levels remain active.
        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(
                true
            );
        }

        if (interventionDialObject != null)
        {
            interventionDialObject.SetActive(
                true
            );
        }


        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator ShowBallastTargetSelectorNextFrame(
    string tileName,
    List<PlayerUnit> targets
)
    {
        yield return null;

        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(
                true
            );
        }

        conceptPopupPinned =
            true;

        if (interventionDialObject != null)
        {
            interventionDialObject.SetActive(
                true
            );
        }

        ShowBallastTargetSelector(
            tileName,
            targets
        );

        Canvas.ForceUpdateCanvases();

        DockPipToMassSelector();

        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(
                true
            );
        }

        if (interventionDialObject != null)
        {
            interventionDialObject.SetActive(
                true
            );
        }
    }

    private void ShowBallastTargetSelector(
    string tileName,
    List<PlayerUnit> targets
    )
    {
        if (interventionDialObject == null ||
            targets == null ||
            targets.Count == 0)
        {
            return;
        }

        ballastTargetSelectorOpen =
             true;

        ballastTargetSelectorTileName =
            tileName;

        Debug.Log(
            $"[TACTICAL SCANNER] Ballast target selector | " +
            $"Node={tileName} | Targets={targets.Count}"
        );


        // =====================================================
        // TARGETING MODE
        // =====================================================

        interventionDialObject.name =
            "BallastTargetSelector";


        // Keep only the node identity from the debrief.
        if (conceptPopupText != null)
        {
            conceptPopupText.text =
                $"NODE : {tileName}";

            conceptPopupText.fontStyle =
                FontStyles.Bold;

            conceptPopupText.ForceMeshUpdate(
                true,
                true
            );
        }


        // =====================================================
        // TARGETING PANEL SIZE
        // =====================================================

        if (conceptPopupRect != null)
        {
            conceptPopupRect.sizeDelta =
                new Vector2(
                    500f,
                    390f
                );
        }


        RectTransform selectorRootRect =
            interventionDialObject.GetComponent<RectTransform>();

        if (selectorRootRect != null)
        {
            selectorRootRect.sizeDelta =
                new Vector2(
                    470f,
                    310f
                );

            selectorRootRect.anchoredPosition =
                new Vector2(
                    0f,
                    22f
                );
        }


        // =====================================================
        // HIDE NORMAL INTERVENTION DIAL
        // =====================================================

        for (int i = 0;
             i < interventionDialObject.transform.childCount;
             i++)
        {
            Transform child =
                interventionDialObject.transform.GetChild(i);

            if (child != null)
            {
                child.gameObject.SetActive(
                    false
                );
            }
        }


        // =====================================================
        // COMMAND TITLE
        // =====================================================

        TextMeshProUGUI commandTitle =
            CreateText(
                "BallastTargetTitle",
                interventionDialObject.transform,
                24f
            );

        commandTitle.text =
            "BALLAST INJECTION";

        commandTitle.fontStyle =
            FontStyles.Bold;

        commandTitle.alignment =
            TextAlignmentOptions.Center;

        commandTitle.characterSpacing =
            1.5f;

        commandTitle.color =
            new Color(
                1f,
                0.52f,
                0.12f,
                1f
            );

        commandTitle.raycastTarget =
            false;


        RectTransform titleRect =
            commandTitle.rectTransform;

        titleRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        titleRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        titleRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        titleRect.anchoredPosition =
            new Vector2(
                0f,
                125f
            );

        titleRect.sizeDelta =
            new Vector2(
                440f,
                34f
            );


        // =====================================================
        // COMMAND INSTRUCTION
        // =====================================================

        TextMeshProUGUI instruction =
            CreateText(
                "BallastTargetInstruction",
                interventionDialObject.transform,
                19f
            );

        instruction.text =
            "SELECT ENEMY UNIT";

        instruction.fontStyle =
            FontStyles.Bold;

        instruction.alignment =
            TextAlignmentOptions.Center;

        instruction.characterSpacing =
            1f;

        instruction.color =
            new Color(
                1f,
                0.72f,
                0.38f,
                1f
            );

        instruction.raycastTarget =
            false;


        RectTransform instructionRect =
            instruction.rectTransform;

        instructionRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        instructionRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        instructionRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        instructionRect.anchoredPosition =
            new Vector2(
                0f,
                96f
            );

        instructionRect.sizeDelta =
            new Vector2(
                440f,
                28f
            );


        // =====================================================
        // CONSISTENT UNIT ORDER
        // =====================================================

        List<PlayerUnit> orderedTargets =
            new List<PlayerUnit>(
                targets
            );

        orderedTargets.Sort(
            (a, b) =>
            {
                if (a == null &&
                    b == null)
                {
                    return 0;
                }

                if (a == null)
                    return 1;

                if (b == null)
                    return -1;

                return
                    a.unitID.CompareTo(
                        b.unitID
                    );
            }
        );


        // =====================================================
        // TARGET GRID
        // =====================================================

        Vector2[] positions =
        {
        new Vector2(-110f, 30f),
        new Vector2(110f, 30f),
        new Vector2(-110f, -66f),
        new Vector2(110f, -66f)
    };


        int count =
            Mathf.Min(
                orderedTargets.Count,
                positions.Length
            );


        for (int i = 0;
             i < count;
             i++)
        {
            PlayerUnit target =
                orderedTargets[i];

            if (target == null)
                continue;


            CreateBallastTargetChoice(
            tileName,
            target,
            positions[i]
        );
        }



        // =====================================================
        // BACK
        // =====================================================

        GameObject backObject =
            CreateUIObject(
                "BallastTargetBack",
                interventionDialObject.transform
            );

        RectTransform backRect =
            backObject.GetComponent<RectTransform>();

        backRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        backRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        backRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        backRect.anchoredPosition =
            new Vector2(
                0f,
                -137f
            );

        backRect.sizeDelta =
            new Vector2(
                230f,
                40f
            );


        Image backImage =
            backObject.AddComponent<Image>();

        backImage.color =
            new Color(
                0.05f,
                0.18f,
                0.21f,
                0.95f
            );

        backImage.raycastTarget =
            true;


        TextMeshProUGUI backText =
            CreateText(
                "BallastTargetBackText",
                backObject.transform,
                20f
            );

        backText.text =
            "← BACK // INTERVENTIONS";

        backText.alignment =
            TextAlignmentOptions.Center;

        backText.fontStyle =
            FontStyles.Bold;

        backText.raycastTarget =
            false;

        backText.rectTransform.anchorMin =
            Vector2.zero;

        backText.rectTransform.anchorMax =
            Vector2.one;

        backText.rectTransform.offsetMin =
            Vector2.zero;

        backText.rectTransform.offsetMax =
            Vector2.zero;


        EventTrigger backTrigger =
            backObject.AddComponent<EventTrigger>();

        backTrigger.triggers =
            new List<EventTrigger.Entry>();


        EventTrigger.Entry backEntry =
            new EventTrigger.Entry();

        backEntry.eventID =
            EventTriggerType.PointerDown;

        backEntry.callback.AddListener(
            (eventData) =>
            {
                StartCoroutine(
                    ReturnFromBallastTargetSelectorNextFrame()
                );
            }
        );

        backTrigger.triggers.Add(
            backEntry
        );

        Canvas.ForceUpdateCanvases();
    }


    private void CreateBallastTargetChoice(
    string tileName,
    PlayerUnit target,
    Vector2 position
    )
    {
        if (target == null ||
            interventionDialObject == null)
        {
            return;
        }


        int projectedLoad =
            Mathf.Min(
                5,
                target.EffectiveLoad + 1
            );


        string labelText =
            $"<size=38>{target.UnitCode}</size>\n" +
            $"<size=24>LOAD {target.EffectiveLoad} → {projectedLoad}</size>";


        string hoverText =
            $"{target.UnitCode}\n" +
            $"LOAD {target.EffectiveLoad} → {projectedLoad}";


        Button targetButton =
            CreateDialControl(
                "BallastTarget_" + target.UnitCode,
                interventionDialObject.transform,
                labelText,
                position,
                new Vector2(
                    210f,
                    88f
                ),
                true,
                new Color(
                    0.58f,
                    0.20f,
                    0.08f,
                    0.95f
                ),
                hoverText
            );


        if (targetButton == null)
            return;


        PlayerUnit selectedTarget =
            target;


        EventTrigger targetTrigger =
            targetButton.GetComponent<EventTrigger>();

        if (targetTrigger != null)
        {
            EventTrigger.Entry ballastTargetEntry =
                new EventTrigger.Entry();

            ballastTargetEntry.eventID =
                EventTriggerType.PointerDown;

            ballastTargetEntry.callback.AddListener(
     (eventData) =>
     {
         Debug.Log(
             $"[TACTICAL SCANNER] " +
             $"BALLAST INJECTION COMMAND | " +
             $"Node={tileName} | " +
             $"Target={selectedTarget.UnitCode} | " +
             $"LoadBefore={selectedTarget.EffectiveLoad}"
         );

         ExecuteBallastInjectionFromDial(
             tileName,
             selectedTarget
         );
     }


 );

            targetTrigger.triggers.Add(
                ballastTargetEntry
            );
        }
    }


    private void ExecuteBallastInjectionFromDial(
    string tileName,
    PlayerUnit target
    )
    {
        if (target == null)
            return;

        if (subsystemController == null)
        {
            subsystemController =
                FindFirstObjectByType<SubsystemController>();
        }

        if (subsystemController == null)
        {
            Debug.LogWarning(
                "[TACTICAL SCANNER] SubsystemController not found."
            );

            return;
        }


        int loadBefore =
            target.EffectiveLoad;


        bool success =
            subsystemController
                .TryBallastInjectionOnUnit(
                    target
                );


        if (!success)
        {
            if (interventionDialCenterText != null)
            {
                interventionDialCenterText.text =
                    "COMMAND FAILED";
            }

            return;
        }

        // BALLAST INJECTION — GRAVITATIONAL COMPRESSION VFX
        HostileUnitImpactVFX.Play(
            target
        );

        int loadAfter =
            target.EffectiveLoad;


        Debug.Log(
            $"[TACTICAL SCANNER] Ballast Injection executed on {target.name} | " +
            $"Node={tileName} | " +
            $"Load={loadBefore} → {loadAfter}"
        );

        ballastTargetSelectorOpen =
            false;

        ballastTargetSelectorTileName =
            null;

        ShowNodeDebrief(
            tileName
        );


        if (interventionDialCenterText != null)
        {
            interventionDialCenterText.text =
                "APPLIED\n" +
                $"LOAD {loadBefore} → {loadAfter}";
        }

        StartCoroutine(
    RunInterventionCameraSequence(
        target.transform
    )
);
    }

    private IEnumerator RunInterventionCameraSequence(
    Transform interventionTarget
)
    {
        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            yield break;
        }

        // Always show exactly what the intervention affected.
        if (interventionTarget != null)
        {
            cameraFollow.SetTarget(
                interventionTarget
            );
        }

        // Short visual confirmation beat.
        yield return new WaitForSeconds(
            1.4f
        );

        if (TurnManager.Instance == null ||
            TurnManager.Instance.current == null)
        {
            yield break;
        }

        PlayerUnit activeUnit =
            TurnManager.Instance.current;

        cameraFollow.SetTarget(
            activeUnit.transform
        );

        Debug.Log(
            $"[TACTICAL SCANNER] Intervention camera returned | " +
            $"Unit={activeUnit.UnitCode}"
        );
    }

    private void ShowMassTargetSelector(
    string tileName,
    List<PlayerUnit> targets
    )
    {

        massTargetSelectorOpen =
    true;

        massTargetSelectorTileName =
            tileName;


        if (interventionDialObject == null ||
            targets == null ||
            targets.Count == 0)
        {
            return;
        }


        Debug.Log(
            $"[TACTICAL SCANNER] Mass target selector | " +
            $"Node={tileName} | Targets={targets.Count}"
        );


        // =====================================================
        // TARGETING MODE
        // =====================================================

        interventionDialObject.name =
            "MassTargetSelector";


        // Keep only the node identity from the debrief.
        if (conceptPopupText != null)
        {
            conceptPopupText.text =
                $"NODE : {tileName}";

            conceptPopupText.fontStyle =
                FontStyles.Bold;

            conceptPopupText.ForceMeshUpdate(
                true,
                true
            );
        }


        // =====================================================
        // TARGETING PANEL SIZE
        // =====================================================

        if (conceptPopupRect != null)
        {
            conceptPopupRect.sizeDelta =
                new Vector2(
                    500f,
                    390f
                );
        }


        RectTransform selectorRootRect =
            interventionDialObject.GetComponent<RectTransform>();

        if (selectorRootRect != null)
        {
            selectorRootRect.sizeDelta =
                new Vector2(
                    470f,
                    310f
                );

            selectorRootRect.anchoredPosition =
                new Vector2(
                    0f,
                    22f
                );
        }


        // =====================================================
        // HIDE NORMAL INTERVENTION DIAL
        // =====================================================

        for (int i = 0;
             i < interventionDialObject.transform.childCount;
             i++)
        {
            Transform child =
                interventionDialObject.transform.GetChild(i);

            if (child != null)
            {
                child.gameObject.SetActive(
                    false
                );
            }
        }


        // =====================================================
        // COMMAND TITLE
        // =====================================================

        TextMeshProUGUI commandTitle =
            CreateText(
                "MassTargetTitle",
                interventionDialObject.transform,
                24f
            );

        commandTitle.text =
            "MASS DAMPENER";

        commandTitle.fontStyle =
            FontStyles.Bold;

        commandTitle.alignment =
            TextAlignmentOptions.Center;

        commandTitle.characterSpacing =
            1.5f;

        commandTitle.color =
            new Color(
                1f,
                0.86f,
                0.22f,
                1f
            );

        commandTitle.raycastTarget =
            false;


        RectTransform titleRect =
            commandTitle.rectTransform;

        titleRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        titleRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        titleRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        titleRect.anchoredPosition =
            new Vector2(
                0f,
                125f
            );

        titleRect.sizeDelta =
            new Vector2(
                440f,
                34f
            );


        // =====================================================
        // COMMAND INSTRUCTION
        // =====================================================

        TextMeshProUGUI instruction =
            CreateText(
                "MassTargetInstruction",
                interventionDialObject.transform,
                19f
            );

        instruction.text =
            "SELECT ALLIED UNIT";

        instruction.fontStyle =
            FontStyles.Bold;

        instruction.alignment =
            TextAlignmentOptions.Center;

        instruction.characterSpacing =
            1f;

        instruction.color =
            new Color(
                0.78f,
                0.95f,
                1f,
                1f
            );

        instruction.raycastTarget =
            false;


        RectTransform instructionRect =
            instruction.rectTransform;

        instructionRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        instructionRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        instructionRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        instructionRect.anchoredPosition =
            new Vector2(
                0f,
                96f
            );

        instructionRect.sizeDelta =
            new Vector2(
                440f,
                28f
            );


        // =====================================================
        // CONSISTENT UNIT ORDER
        // =====================================================

        List<PlayerUnit> orderedTargets =
            new List<PlayerUnit>(
                targets
            );

        orderedTargets.Sort(
            (a, b) =>
            {
                if (a == null &&
                    b == null)
                {
                    return 0;
                }

                if (a == null)
                    return 1;

                if (b == null)
                    return -1;

                return
                    a.unitID.CompareTo(
                        b.unitID
                    );
            }
        );


        // =====================================================
        // TARGET GRID
        //
        // B00   B01
        // B02   B03
        // =====================================================

        Vector2[] positions =
        {
        new Vector2(-110f, 30f),
        new Vector2(110f, 30f),
        new Vector2(-110f, -66f),
        new Vector2(110f, -66f)
    };


        int count =
            Mathf.Min(
                orderedTargets.Count,
                positions.Length
            );


        for (int i = 0;
             i < count;
             i++)
        {
            PlayerUnit target =
                orderedTargets[i];

            if (target == null)
                continue;


            CreateMassTargetChoice(
                tileName,
                target,
                positions[i]
            );
        }


        // =====================================================
        // BACK
        // =====================================================

        GameObject backObject =
            CreateUIObject(
                "MassTargetBack",
                interventionDialObject.transform
            );

        RectTransform backRect =
            backObject.GetComponent<RectTransform>();

        backRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        backRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        backRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        backRect.anchoredPosition =
            new Vector2(
                0f,
                -137f
            );

        backRect.sizeDelta =
            new Vector2(
                230f,
                40f
            );


        Image backImage =
            backObject.AddComponent<Image>();

        backImage.color =
            new Color(
                0.05f,
                0.18f,
                0.21f,
                0.95f
            );

        backImage.raycastTarget =
            true;


        TextMeshProUGUI backText =
            CreateText(
                "MassTargetBackText",
                backObject.transform,
                20f
            );

        backText.text =
            "← BACK // INTERVENTIONS";

        backText.alignment =
            TextAlignmentOptions.Center;

        backText.fontStyle =
            FontStyles.Bold;

        backText.raycastTarget =
            false;

        backText.rectTransform.anchorMin =
            Vector2.zero;

        backText.rectTransform.anchorMax =
            Vector2.one;

        backText.rectTransform.offsetMin =
            Vector2.zero;

        backText.rectTransform.offsetMax =
            Vector2.zero;


        EventTrigger backTrigger =
            backObject.AddComponent<EventTrigger>();

        backTrigger.triggers =
            new List<EventTrigger.Entry>();


        EventTrigger.Entry backEntry =
            new EventTrigger.Entry();

        backEntry.eventID =
            EventTriggerType.PointerDown;

        backEntry.callback.AddListener(
            (eventData) =>
            {
                StartCoroutine(
                    ReturnFromMassTargetSelectorNextFrame()
                );
            }
        );

        backTrigger.triggers.Add(
            backEntry
        );


        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator ReturnFromMassTargetSelectorNextFrame()
    {
        yield return null;

        ReturnFromMassTargetSelector();
    }


    private IEnumerator ReturnFromBallastTargetSelectorNextFrame()
    {
        yield return null;

        ReturnFromBallastTargetSelector();
    }

    private void ReturnFromBallastTargetSelector()
    {
        if (!ballastTargetSelectorOpen ||
            string.IsNullOrEmpty(
                ballastTargetSelectorTileName
            ))
        {
            return;
        }

        string tileName =
            ballastTargetSelectorTileName;

        ballastTargetSelectorOpen =
            false;

        ballastTargetSelectorTileName =
            null;

        Debug.Log(
            $"[TACTICAL SCANNER] " +
            $"Returning from Ballast selector to interventions | " +
            $"Node={tileName}"
        );

        ShowNodeDebrief(
            tileName
        );
    }
    private void ReturnFromMassTargetSelector()
    {
        if (!massTargetSelectorOpen ||
            string.IsNullOrEmpty(
                massTargetSelectorTileName
            ))
        {
            return;
        }

        string tileName =
            massTargetSelectorTileName;

        massTargetSelectorOpen =
            false;

        massTargetSelectorTileName =
            null;

        Debug.Log(
            $"[TACTICAL SCANNER] " +
            $"Returning from Mass selector to interventions | " +
            $"Node={tileName}"
        );

        ShowNodeDebrief(
            tileName
        );
    }

    private void CreateMassTargetChoice(
    string tileName,
    PlayerUnit target,
    Vector2 position
)
    {
        if (target == null ||
            interventionDialObject == null)
        {
            return;
        }

        int projectedLoad =
            Mathf.Max(
                1,
                target.EffectiveLoad - 1
            );

        Color targetColor =
            new Color(
                0.06f,
                0.43f,
                0.50f,
                0.95f
            );


        // Large-format selector:
        // unit identity dominates,
        // consequence remains immediately readable.
        string labelText =
            $"<size=38>{target.UnitCode}</size>\n" +
            $"<size=24>LOAD {target.EffectiveLoad} → {projectedLoad}</size>";


        string hoverText =
            $"{target.UnitCode}\n" +
            $"LOAD {target.EffectiveLoad} → {projectedLoad}";


        Button targetButton =
            CreateDialControl(
                "MassTarget_" + target.UnitCode,
                interventionDialObject.transform,
                labelText,
                position,
                new Vector2(
                    210f,
                    88f
                ),
                true,
                targetColor,
                hoverText
            );

        if (targetButton == null)
            return;

        PlayerUnit selectedTarget =
            target;

        EventTrigger targetTrigger =
            targetButton.GetComponent<EventTrigger>();

        if (targetTrigger != null)
        {
            EventTrigger.Entry massTargetExecuteEntry =
                new EventTrigger.Entry();

            massTargetExecuteEntry.eventID =
                EventTriggerType.PointerDown;

            massTargetExecuteEntry.callback.AddListener(
                (eventData) =>
                {
                    Debug.Log(
                        $"[TACTICAL SCANNER] " +
                        $"MASS DAMPENER COMMAND | " +
                        $"Node={tileName} | " +
                        $"Target={selectedTarget.UnitCode} | " +
                        $"LoadBefore={selectedTarget.EffectiveLoad}"
                    );

                    ExecuteMassDampenerFromDial(
                        tileName,
                        selectedTarget
                    );
                }
            );

            targetTrigger.triggers.Add(
                massTargetExecuteEntry
            );
        }

        // -----------------------------------------------------
        // LARGE SELECTOR TYPOGRAPHY
        // -----------------------------------------------------

        TextMeshProUGUI targetLabel =
            targetButton.GetComponentInChildren<TextMeshProUGUI>(
                true
            );

        if (targetLabel != null)
        {
            targetLabel.richText =
                true;

            targetLabel.enableAutoSizing =
                false;

            targetLabel.enableWordWrapping =
                false;

            targetLabel.lineSpacing =
                -8f;

            targetLabel.alignment =
                TextAlignmentOptions.Center;

            targetLabel.fontStyle =
                FontStyles.Bold;

            targetLabel.lineSpacing =
                -8f;
        }


        // -----------------------------------------------------
        // EXECUTION
        // -----------------------------------------------------

        EventTrigger trigger =
            targetButton.GetComponent<EventTrigger>();

        if (trigger == null)
            return;

        EventTrigger.Entry executeEntry =
            new EventTrigger.Entry();

        executeEntry.eventID =
            EventTriggerType.PointerDown;

        executeEntry.callback.AddListener(
            (eventData) =>
            {
                Debug.Log(
                    $"[TACTICAL SCANNER] MASS DAMPENER TARGET | " +
                    $"Node={tileName} | " +
                    $"Target={target.UnitCode} | " +
                    $"LoadBefore={target.EffectiveLoad}"
                );

                ExecuteMassDampenerFromDial(
                    tileName,
                    target
                );
            }
        );

        trigger.triggers.Add(
            executeEntry
        );
    }

    private void DebugMassTargetUIRaycast()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (interventionDialObject == null ||
            interventionDialObject.name != "MassTargetSelector")
        {
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning(
                "[TACTICAL SCANNER UI RAYCAST] EventSystem is NULL."
            );

            return;
        }


        PointerEventData pointerData =
            new PointerEventData(
                EventSystem.current
            );

        pointerData.position =
            Input.mousePosition;


        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results
        );


        Debug.Log(
            $"[TACTICAL SCANNER UI RAYCAST] " +
            $"Hits={results.Count} | " +
            $"Mouse={Input.mousePosition}"
        );


        int count =
            Mathf.Min(
                results.Count,
                10
            );


        for (int i = 0;
             i < count;
             i++)
        {
            RaycastResult hit =
                results[i];

            Debug.Log(
                $"[UI {i:00}] " +
                $"{hit.gameObject.name} | " +
                $"Depth={hit.depth} | " +
                $"Distance={hit.distance:0.00}"
            );
        }
    }

    private Button CreateDialControl(
    string objectName,
    Transform parent,
    string labelText,
    Vector2 position,
    Vector2 size,
    bool available,
    Color activeColor,
    string hoverText
)
    {
        // =====================================================
        // DEPTH SETTINGS
        // =====================================================

        float raisedDepth =
            available
                ? 7f
                : 2f;

        float hoverLift =
            available
                ? 2f
                : 0f;

        float pressDepth =
            available
                ? 5f
                : 0f;


        Vector2 restingPosition =
            position;

        Vector2 hoverPosition =
            position +
            new Vector2(
                0f,
                hoverLift
            );

        Vector2 pressedPosition =
            position +
            new Vector2(
                0f,
                -pressDepth
            );


        // =====================================================
        // LOWER EXTRUSION
        // =====================================================

        GameObject extrusion =
            CreateUIObject(
                objectName + "_Extrusion",
                parent
            );

        RectTransform extrusionRect =
            extrusion.GetComponent<RectTransform>();

        extrusionRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        extrusionRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        extrusionRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        extrusionRect.anchoredPosition =
            position +
            new Vector2(
                0f,
                -raisedDepth
            );

        extrusionRect.sizeDelta =
            size;


        Image extrusionImage =
            extrusion.AddComponent<Image>();

        if (available)
        {
            extrusionImage.color =
                new Color(
                    activeColor.r * 0.28f,
                    activeColor.g * 0.28f,
                    activeColor.b * 0.28f,
                    0.95f
                );
        }
        else
        {
            extrusionImage.color =
                new Color(
                    0.09f,
                    0.10f,
                    0.11f,
                    0.70f
                );
        }

        extrusionImage.raycastTarget =
            false;


        // =====================================================
        // MAIN CONTROL FACE
        // =====================================================

        GameObject control =
            CreateUIObject(
                objectName,
                parent
            );

        RectTransform rect =
            control.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            restingPosition;

        rect.sizeDelta =
            size;


        Image image =
            control.AddComponent<Image>();

        Color unavailableColor =
            new Color(
                0.17f,
                0.18f,
                0.19f,
                0.90f
            );

        Color normalColor =
            available
                ? activeColor
                : unavailableColor;

        Color hoverColor =
            available
                ? Color.Lerp(
                    activeColor,
                    Color.white,
                    0.20f
                )
                : new Color(
                    0.68f,
                    0.69f,
                    0.70f,
                    1f
                );

        Color pressedColor =
            available
                ? Color.Lerp(
                    activeColor,
                    Color.black,
                    0.12f
                )
                : unavailableColor;

        image.color =
            normalColor;

        image.raycastTarget =
            true;


        // =====================================================
        // OUTLINE
        // =====================================================

        Outline outline =
            control.AddComponent<Outline>();

        outline.effectColor =
            available
                ? new Color(
                    0.68f,
                    0.94f,
                    0.98f,
                    0.90f
                )
                : new Color(
                    0.34f,
                    0.35f,
                    0.36f,
                    0.50f
                );

        outline.effectDistance =
            new Vector2(
                1.2f,
                -1.2f
            );


        // =====================================================
        // TOP EDGE HIGHLIGHT
        // =====================================================

        GameObject topEdge =
            CreateUIObject(
                objectName + "_TopEdge",
                control.transform
            );

        RectTransform topEdgeRect =
            topEdge.GetComponent<RectTransform>();

        topEdgeRect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        topEdgeRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        topEdgeRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        topEdgeRect.anchoredPosition =
            Vector2.zero;

        topEdgeRect.sizeDelta =
            new Vector2(
                0f,
                3f
            );


        Image topEdgeImage =
            topEdge.AddComponent<Image>();

        topEdgeImage.color =
            available
                ? new Color(
                    0.82f,
                    0.97f,
                    1f,
                    0.75f
                )
                : new Color(
                    0.42f,
                    0.43f,
                    0.44f,
                    0.25f
                );

        topEdgeImage.raycastTarget =
            false;

        CreateDialDirectionTab(
            control.transform,
            objectName,
            activeColor,
            available
        );

        // =====================================================
        // BUTTON
        // =====================================================

        Button button =
            control.AddComponent<Button>();

        button.targetGraphic =
            image;

        button.transition =
            Selectable.Transition.None;

        button.interactable =
            true;

        // Execution intentionally remains disconnected
        // during this visual test pass.


        // =====================================================
        // LABEL
        // =====================================================

        TextMeshProUGUI label =
            CreateText(
                objectName + "_Text",
                control.transform,
                16f
            );

        label.text =
            labelText;

        label.alignment =
            TextAlignmentOptions.Center;

        label.enableWordWrapping =
            false;

        label.fontStyle =
            FontStyles.Bold;

        label.raycastTarget =
            false;

        label.color =
            available
                ? new Color(
                    0.94f,
                    1f,
                    1f,
                    1f
                )
                : new Color(
                    0.48f,
                    0.49f,
                    0.50f,
                    1f
                );

        RectTransform labelRect =
            label.rectTransform;

        labelRect.anchorMin =
            Vector2.zero;

        labelRect.anchorMax =
            Vector2.one;

        labelRect.offsetMin =
            new Vector2(
                5f,
                4f
            );

        labelRect.offsetMax =
            new Vector2(
                -5f,
                -4f
            );


        // =====================================================
        // HOVER AND PRESS EVENTS
        // =====================================================

        EventTrigger trigger =
            control.AddComponent<EventTrigger>();

        trigger.triggers =
            new List<EventTrigger.Entry>();


        // POINTER ENTER

        EventTrigger.Entry enterEntry =
            new EventTrigger.Entry();

        enterEntry.eventID =
            EventTriggerType.PointerEnter;

        enterEntry.callback.AddListener(
            (eventData) =>
            {
                image.color =
                    hoverColor;

                if (available)
                {
                    rect.anchoredPosition =
                        hoverPosition;

                    extrusionRect.anchoredPosition =
                        restingPosition +
                        new Vector2(
                            0f,
                            -raisedDepth
                        );
                }

                if (interventionDialCenterText != null)
                {
                    interventionDialCenterText.text =
                        hoverText;
                }
            }
        );

        trigger.triggers.Add(
            enterEntry
        );


        // POINTER EXIT

        EventTrigger.Entry exitEntry =
            new EventTrigger.Entry();

        exitEntry.eventID =
            EventTriggerType.PointerExit;

        exitEntry.callback.AddListener(
            (eventData) =>
            {
                image.color =
                    normalColor;

                rect.anchoredPosition =
                    restingPosition;

                extrusionRect.anchoredPosition =
                    restingPosition +
                    new Vector2(
                        0f,
                        -raisedDepth
                    );

                if (interventionDialCenterText != null)
                {
                    interventionDialCenterText.text =
                        interventionDialDefaultText;
                }
            }
        );

        trigger.triggers.Add(
            exitEntry
        );


        // POINTER DOWN

        EventTrigger.Entry downEntry =
            new EventTrigger.Entry();

        downEntry.eventID =
            EventTriggerType.PointerDown;

        downEntry.callback.AddListener(
          (eventData) =>
          {
              Debug.Log(
                  $"[TACTICAL SCANNER INPUT] DOWN | " +
                  $"Control={objectName} | " +
                  $"Available={available}"
              );

              if (!available)
                  return;

              image.color =
                  pressedColor;

              rect.anchoredPosition =
                  pressedPosition;
          }
      );

        trigger.triggers.Add(
            downEntry
        );


        // POINTER UP

        EventTrigger.Entry upEntry =
            new EventTrigger.Entry();

        upEntry.eventID =
            EventTriggerType.PointerUp;

        upEntry.callback.AddListener(
            (eventData) =>
            {
                if (!available)
                    return;

                image.color =
                    hoverColor;

                rect.anchoredPosition =
                    hoverPosition;
            }
        );

        trigger.triggers.Add(
            upEntry
        );

        return button;
    }


    private void CreateDialDirectionTab(
    Transform control,
    string objectName,
    Color color,
    bool available
)
    {
        GameObject tab =
            CreateUIObject(
                objectName + "_DirectionTab",
                control
            );

        RectTransform rect =
            tab.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                45f
            );


        if (objectName ==
            "BallastInjectionControl")
        {
            rect.anchoredPosition =
                new Vector2(
                    0f,
                    35f
                );

            rect.sizeDelta =
                new Vector2(
                    12f,
                    12f
                );
        }
        else if (objectName ==
                 "MassDampenerControl")
        {
            rect.anchoredPosition =
                new Vector2(
                    0f,
                    -35f
                );

            rect.sizeDelta =
                new Vector2(
                    12f,
                    12f
                );
        }
        else if (objectName ==
                 "StructuralReinforcementControl")
        {
            rect.anchoredPosition =
                new Vector2(
                    -70f,
                    0f
                );

            rect.sizeDelta =
                new Vector2(
                    14f,
                    14f
                );
        }
        else if (objectName ==
                 "NetworkStressControl")
        {
            rect.anchoredPosition =
                new Vector2(
                    70f,
                    0f
                );

            rect.sizeDelta =
                new Vector2(
                    14f,
                    14f
                );
        }
        else
        {
            Destroy(
                tab
            );

            return;
        }


        Image image =
            tab.AddComponent<Image>();

        if (available)
        {
            image.color =
                Color.Lerp(
                    color,
                    Color.white,
                    0.18f
                );
        }
        else
        {
            image.color =
                new Color(
                    0.30f,
                    0.31f,
                    0.32f,
                    0.60f
                );
        }

        image.raycastTarget =
            false;
    }

    private List<PlayerUnit> GetDialUnitsOnNode(
        Map1HexGrid map,
        string tileName
    )
    {
        List<PlayerUnit> result =
            new List<PlayerUnit>();

        if (map == null ||
            string.IsNullOrEmpty(tileName))
        {
            return result;
        }

        if (!map.tileNameToGrid.TryGetValue(
                tileName,
                out Vector2Int tileCoord))
        {
            return result;
        }

        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        foreach (PlayerUnit unit in units)
        {
            if (unit == null ||
                unit.IsDead)
            {
                continue;
            }

            if (unit.currentTileCoord ==
                tileCoord)
            {
                result.Add(
                    unit
                );
            }
        }

        return result;
    }


    private string GetDialProjectedState(
        int load,
        int capacity
    )
    {
        if (load > capacity)
        {
            return "OVERLOADED";
        }

        if (load == capacity)
        {
            return "FULL";
        }

        return "SAFE";
    }


    private string FormatDialSigned(
        int value
    )
    {
        if (value > 0)
        {
            return "+" +
                value.ToString();
        }

        return value.ToString();
    }


    private void DestroyInterventionDial()
    {
        interventionDialCenterText =
            null;

        interventionDialDefaultText =
            string.Empty;

        if (interventionDialObject == null)
        {
            return;
        }

        interventionDialObject.SetActive(
            false
        );

        Destroy(
            interventionDialObject
        );

        interventionDialObject =
            null;
    }
    // =====================================================
    // OBJECTIVE TRACKING
    // =====================================================

    private PlayerUnit GetBoxCarrier()
    {
        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        foreach (PlayerUnit unit in units)
        {
            if (unit == null)
                continue;

            if (unit.IsDead)
                continue;

            if (unit.IsCarryingBox)
            {
                return unit;
            }
        }

        return null;
    }
    private string GetCurrentBoxTileName(
        Map1HexGrid map
    )
    {
        // ---------------------------------------------
        // BOX CURRENTLY CARRIED BY A PLAYER
        // ---------------------------------------------

        PlayerUnit[] units =
            FindObjectsByType<PlayerUnit>(
                FindObjectsSortMode.None
            );

        foreach (PlayerUnit unit in units)
        {
            if (unit == null)
                continue;

            if (unit.IsDead)
                continue;

            if (!unit.IsCarryingBox)
                continue;

            return GetTileNameFromCoord(
                map,
                unit.currentTileCoord
            );
        }


        // ---------------------------------------------
        // BOX CURRENTLY ON THE BATTLEFIELD
        // ---------------------------------------------

        WorldBox[] boxes =
            FindObjectsByType<WorldBox>(
                FindObjectsSortMode.None
            );

        foreach (WorldBox box in boxes)
        {
            if (box == null)
                continue;

            if (!box.gameObject.activeInHierarchy)
                continue;

            Vector2Int boxCoord =
                new Vector2Int(
                    box.tileX,
                    box.tileY
                );

            return GetTileNameFromCoord(
                map,
                boxCoord
            );
        }


        return null;
    }


    private string GetTileNameFromCoord(
        Map1HexGrid map,
        Vector2Int coord
    )
    {
        foreach (var pair in map.tileNameToGrid)
        {
            if (pair.Value == coord)
            {
                return pair.Key;
            }
        }

        return null;
    }


    private void CreateCurrentUnitLocator(
    Vector2 position,
    string unitCode
)
    {
        if (string.IsNullOrEmpty(unitCode))
            return;


        // =====================================================
        // ROOT
        // Anchored exactly on the current unit's topology node.
        // =====================================================

        GameObject locatorRoot =
            CreateUIObject(
                "CurrentUnitLocator_" + unitCode,
                graphArea
            );

        generatedObjects.Add(
            locatorRoot
        );


        RectTransform rootRect =
            locatorRoot.GetComponent<RectTransform>();

        rootRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.anchoredPosition =
            position;

        rootRect.sizeDelta =
            Vector2.zero;


        // Counter-rotate immediately so the locator
        // stays upright on screen.
        rootRect.localRotation =
            graphArea != null
            ? Quaternion.Inverse(
                graphArea.localRotation
            )
            : Quaternion.identity;


        // =====================================================
        // ANIMATED CONTENT
        // =====================================================

        GameObject content =
            CreateUIObject(
                "CurrentUnitLocator_Content",
                locatorRoot.transform
            );


        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        contentRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        contentRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        contentRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        contentRect.anchoredPosition =
            Vector2.zero;

        contentRect.sizeDelta =
            Vector2.zero;


        CanvasGroup canvasGroup =
            content.AddComponent<CanvasGroup>();

        canvasGroup.interactable =
            false;

        canvasGroup.blocksRaycasts =
            false;


        Color locatorColor =
            new Color(
                0.72f,
                0.97f,
                1f,
                1f
            );


        // =====================================================
        // LEFT CHEVRON ARM
        // =====================================================

        GameObject leftArm =
            CreateUIObject(
                "CurrentUnitChevron_Left",
                content.transform
            );


        RectTransform leftRect =
            leftArm.GetComponent<RectTransform>();

        leftRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        leftRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        leftRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        leftRect.anchoredPosition =
            new Vector2(
                -4.5f,
                29f
            );

        leftRect.sizeDelta =
            new Vector2(
                12f,
                3f
            );

        leftRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -45f
            );


        Image leftImage =
            leftArm.AddComponent<Image>();

        leftImage.color =
            locatorColor;

        leftImage.raycastTarget =
            false;


        // =====================================================
        // RIGHT CHEVRON ARM
        // =====================================================

        GameObject rightArm =
            CreateUIObject(
                "CurrentUnitChevron_Right",
                content.transform
            );


        RectTransform rightRect =
            rightArm.GetComponent<RectTransform>();

        rightRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rightRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rightRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rightRect.anchoredPosition =
            new Vector2(
                4.5f,
                29f
            );

        rightRect.sizeDelta =
            new Vector2(
                12f,
                3f
            );

        rightRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                45f
            );


        Image rightImage =
            rightArm.AddComponent<Image>();

        rightImage.color =
            locatorColor;

        rightImage.raycastTarget =
            false;


        // =====================================================
        // UNIT CODE
        // =====================================================

        TextMeshProUGUI unitLabel =
            CreateText(
                "CurrentUnitCode",
                content.transform,
                17f
            );

        unitLabel.text =
            unitCode;

        unitLabel.fontStyle =
            FontStyles.Bold;

        unitLabel.alignment =
            TextAlignmentOptions.Center;

        unitLabel.color =
            locatorColor;

        unitLabel.raycastTarget =
            false;


        RectTransform labelRect =
            unitLabel.rectTransform;

        labelRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        labelRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        labelRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        labelRect.anchoredPosition =
            new Vector2(
                0f,
                48f
            );

        labelRect.sizeDelta =
            new Vector2(
                70f,
                24f
            );


        StartCoroutine(
            AnimateCurrentUnitLocator(
                locatorRoot,
                rootRect,
                contentRect,
                canvasGroup
            )
        );
    }


    private IEnumerator AnimateCurrentUnitLocator(
    GameObject locatorRoot,
    RectTransform rootRect,
    RectTransform contentRect,
    CanvasGroup canvasGroup
)
    {
        if (locatorRoot == null ||
            rootRect == null ||
            contentRect == null ||
            canvasGroup == null)
        {
            yield break;
        }


        while (locatorRoot != null &&
               rootRect != null &&
               contentRect != null &&
               canvasGroup != null)
        {
            // Keep the chevron and Bxx/Rxx text upright
            // while the topology graph rotates.
            if (graphArea != null)
            {
                rootRect.localRotation =
                    Quaternion.Inverse(
                        graphArea.localRotation
                    );
            }


            float motion =
                (
                    Mathf.Sin(
                        Time.unscaledTime * 5f
                    ) +
                    1f
                ) *
                0.5f;


            // Small vertical bob.
            // This is intentionally NOT the scaling pulse
            // used by objective markers.
            contentRect.anchoredPosition =
                new Vector2(
                    0f,
                    Mathf.Lerp(
                        0f,
                        4f,
                        motion
                    )
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    0.78f,
                    1f,
                    motion
                );


            yield return null;
        }
    }

    // =====================================================
    // BOX MARKER
    // =====================================================

    private void CreateBoxMarker(
    Vector2 position
)
    {
        GameObject marker =
            CreateUIObject(
                "Objective_Box",
                graphArea
            );

        generatedObjects.Add(
            marker
        );


        RectTransform rect =
            marker.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position +
            new Vector2(
                0f,
                17f
            );

        rect.sizeDelta =
            new Vector2(
                13f,
                13f
            );

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                45f
            );


        Image image =
            marker.AddComponent<Image>();

        image.color =
            new Color(
                1f,
                0.88f,
                0.10f,
                1f
            );

        image.raycastTarget =
            false;


        // Small connection line from node to AI CORE marker.
        CreateObjectiveLine(
            "BoxLink",
            position,
            new Vector2(
                position.x,
                position.y + 11f
            ),
            new Color(
                1f,
                0.88f,
                0.10f,
                0.8f
            ),
            2f
        );


        // AI CORE is currently free on the map,
        // therefore it is the team's primary objective.
        StartCoroutine(
            PulseAICoreMarker(
                marker,
                rect,
                image
            )
        );
    }

    private IEnumerator PulseAICoreMarker(
    GameObject marker,
    RectTransform rect,
    Image image
)
    {
        if (marker == null ||
            rect == null ||
            image == null)
        {
            yield break;
        }


        Vector2 minimumSize =
            new Vector2(
                13f,
                13f
            );

        Vector2 maximumSize =
            new Vector2(
                21f,
                21f
            );


        Color baseColor =
            new Color(
                1f,
                0.88f,
                0.10f,
                1f
            );


        while (marker != null &&
               rect != null &&
               image != null)
        {
            float pulse =
                (
                    Mathf.Sin(
                        Time.unscaledTime * 4f
                    ) +
                    1f
                ) *
                0.5f;


            rect.sizeDelta =
                Vector2.Lerp(
                    minimumSize,
                    maximumSize,
                    pulse
                );


            image.color =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    Mathf.Lerp(
                        0.68f,
                        1f,
                        pulse
                    )
                );


            yield return null;
        }
    }

    private void CreateCarrierMarker(
      Vector2 position,
      bool enemyCarrier
  )
    {
        Color carrierColor =
            enemyCarrier
                ? new Color(
                    1f,
                    0.18f,
                    0.12f,
                    1f
                )
                : new Color(
                    1f,
                    0.82f,
                    0.10f,
                    1f
                );


        float halfSize = 16f;
        float cornerLength = 9f;
        float thickness = 2.5f;


        // =====================================================
        // CARRIER MARKER ROOT
        // =====================================================

        GameObject carrierRoot =
            CreateUIObject(
                "CarrierMarkerRoot",
                graphArea
            );

        generatedObjects.Add(
            carrierRoot
        );


        RectTransform rootRect =
            carrierRoot.GetComponent<RectTransform>();

        rootRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.anchoredPosition =
            position;

        rootRect.sizeDelta =
            Vector2.zero;


        // =====================================================
        // TOP LEFT
        // =====================================================

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_TL_H",
            new Vector2(
                -halfSize + cornerLength * 0.5f,
                halfSize
            ),
            new Vector2(
                cornerLength,
                thickness
            ),
            carrierColor
        );

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_TL_V",
            new Vector2(
                -halfSize,
                halfSize - cornerLength * 0.5f
            ),
            new Vector2(
                thickness,
                cornerLength
            ),
            carrierColor
        );


        // =====================================================
        // TOP RIGHT
        // =====================================================

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_TR_H",
            new Vector2(
                halfSize - cornerLength * 0.5f,
                halfSize
            ),
            new Vector2(
                cornerLength,
                thickness
            ),
            carrierColor
        );

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_TR_V",
            new Vector2(
                halfSize,
                halfSize - cornerLength * 0.5f
            ),
            new Vector2(
                thickness,
                cornerLength
            ),
            carrierColor
        );


        // =====================================================
        // BOTTOM LEFT
        // =====================================================

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_BL_H",
            new Vector2(
                -halfSize + cornerLength * 0.5f,
                -halfSize
            ),
            new Vector2(
                cornerLength,
                thickness
            ),
            carrierColor
        );

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_BL_V",
            new Vector2(
                -halfSize,
                -halfSize + cornerLength * 0.5f
            ),
            new Vector2(
                thickness,
                cornerLength
            ),
            carrierColor
        );


        // =====================================================
        // BOTTOM RIGHT
        // =====================================================

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_BR_H",
            new Vector2(
                halfSize - cornerLength * 0.5f,
                -halfSize
            ),
            new Vector2(
                cornerLength,
                thickness
            ),
            carrierColor
        );

        CreateCarrierSegment(
            carrierRoot.transform,
            "Carrier_BR_V",
            new Vector2(
                halfSize,
                -halfSize + cornerLength * 0.5f
            ),
            new Vector2(
                thickness,
                cornerLength
            ),
            carrierColor
        );


        // =====================================================
        // ENEMY CARRIER = PRIMARY OBJECTIVE
        // =====================================================

        if (enemyCarrier)
        {
            CanvasGroup canvasGroup =
                carrierRoot.AddComponent<CanvasGroup>();

            canvasGroup.interactable =
                false;

            canvasGroup.blocksRaycasts =
                false;


            StartCoroutine(
                PulseEnemyCarrierMarker(
                    carrierRoot,
                    rootRect,
                    canvasGroup
                )
            );
        }
    }

    private void CreateCarrierSegment(
    Transform parent,
    string objectName,
    Vector2 localPosition,
    Vector2 size,
    Color color
)
    {
        GameObject segment =
            CreateUIObject(
                objectName,
                parent
            );


        RectTransform rect =
            segment.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            localPosition;

        rect.sizeDelta =
            size;


        Image image =
            segment.AddComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;
    }

    private IEnumerator PulseEnemyCarrierMarker(
    GameObject carrierRoot,
    RectTransform rootRect,
    CanvasGroup canvasGroup
)
    {
        if (carrierRoot == null ||
            rootRect == null ||
            canvasGroup == null)
        {
            yield break;
        }


        while (carrierRoot != null &&
               rootRect != null &&
               canvasGroup != null)
        {
            float pulse =
                (
                    Mathf.Sin(
                        Time.unscaledTime * 4f
                    ) +
                    1f
                ) *
                0.5f;


            float scale =
                Mathf.Lerp(
                    1f,
                    1.18f,
                    pulse
                );

            rootRect.localScale =
                new Vector3(
                    scale,
                    scale,
                    1f
                );


            canvasGroup.alpha =
                Mathf.Lerp(
                    0.68f,
                    1f,
                    pulse
                );


            yield return null;
        }
    }
    // =====================================================
    // AI STATION MARKER
    // =====================================================

    private void CreateAIStationMarker(
    Vector2 position,
    bool activeDestination
)
    {
        Color stationColor =
            activeDestination
                ? new Color(
                    0.55f,
                    1f,
                    0.62f,
                    1f
                )
                : new Color(
                    0.35f,
                    0.90f,
                    0.62f,
                    0.90f
                );


        float size =
            activeDestination
                ? 42f
                : 30f;

        float thickness =
            activeDestination
                ? 3.5f
                : 2.5f;


        // =====================================================
        // STATION ROOT
        // Allows the complete symbol to pulse as one object.
        // =====================================================

        GameObject stationRoot =
            CreateUIObject(
                "AIStation_MarkerRoot",
                graphArea
            );

        generatedObjects.Add(
            stationRoot
        );


        RectTransform rootRect =
            stationRoot.GetComponent<RectTransform>();

        rootRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rootRect.anchoredPosition =
            position;

        rootRect.sizeDelta =
            Vector2.zero;


        // =====================================================
        // HORIZONTAL ARM
        // =====================================================

        GameObject horizontal =
            CreateUIObject(
                "AIStation_H",
                stationRoot.transform
            );

        RectTransform horizontalRect =
            horizontal.GetComponent<RectTransform>();

        horizontalRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        horizontalRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        horizontalRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        horizontalRect.anchoredPosition =
            Vector2.zero;

        horizontalRect.sizeDelta =
            new Vector2(
                size,
                thickness
            );


        Image horizontalImage =
            horizontal.AddComponent<Image>();

        horizontalImage.color =
            stationColor;

        horizontalImage.raycastTarget =
            false;


        // =====================================================
        // VERTICAL ARM
        // =====================================================

        GameObject vertical =
            CreateUIObject(
                "AIStation_V",
                stationRoot.transform
            );

        RectTransform verticalRect =
            vertical.GetComponent<RectTransform>();

        verticalRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        verticalRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        verticalRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        verticalRect.anchoredPosition =
            Vector2.zero;

        verticalRect.sizeDelta =
            new Vector2(
                thickness,
                size
            );


        Image verticalImage =
            vertical.AddComponent<Image>();

        verticalImage.color =
            stationColor;

        verticalImage.raycastTarget =
            false;


        // =====================================================
        // CENTER CORE
        // =====================================================

        GameObject core =
            CreateUIObject(
                "AIStation_Core",
                stationRoot.transform
            );

        RectTransform coreRect =
            core.GetComponent<RectTransform>();

        coreRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        coreRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        coreRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        coreRect.anchoredPosition =
            Vector2.zero;

        coreRect.sizeDelta =
            activeDestination
                ? new Vector2(
                    10f,
                    10f
                )
                : new Vector2(
                    7f,
                    7f
                );

        coreRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                45f
            );


        Image coreImage =
            core.AddComponent<Image>();

        coreImage.color =
            stationColor;

        coreImage.raycastTarget =
            false;


        // =====================================================
        // PRIMARY OBJECTIVE PULSE
        // Only when THIS team carries the AI CORE.
        // =====================================================

        if (activeDestination)
        {
            StartCoroutine(
                PulseAIStationMarker(
                    stationRoot,
                    rootRect,
                    horizontalImage,
                    verticalImage,
                    coreImage,
                    stationColor
                )
            );
        }
    }

    private IEnumerator PulseAIStationMarker(
    GameObject stationRoot,
    RectTransform rootRect,
    Image horizontalImage,
    Image verticalImage,
    Image coreImage,
    Color baseColor
)
    {
        if (stationRoot == null ||
            rootRect == null)
        {
            yield break;
        }


        while (stationRoot != null &&
               rootRect != null)
        {
            float pulse =
                (
                    Mathf.Sin(
                        Time.unscaledTime * 4f
                    ) +
                    1f
                ) *
                0.5f;


            float scale =
                Mathf.Lerp(
                    1f,
                    1.18f,
                    pulse
                );

            rootRect.localScale =
                new Vector3(
                    scale,
                    scale,
                    1f
                );


            float alpha =
                Mathf.Lerp(
                    0.68f,
                    1f,
                    pulse
                );


            Color pulseColor =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    alpha
                );


            if (horizontalImage != null)
            {
                horizontalImage.color =
                    pulseColor;
            }

            if (verticalImage != null)
            {
                verticalImage.color =
                    pulseColor;
            }

            if (coreImage != null)
            {
                coreImage.color =
                    pulseColor;
            }


            yield return null;
        }
    }
    // =====================================================
    // LIVE ENEMY PRESENCE (NO GRAPH REBUILD PER FRAME)
    // =====================================================
    private void ClearLiveEnemyMarkers()
    {
        foreach (GameObject marker in liveEnemyMarkers.Values)
        {
            if (marker != null)
                Destroy(marker);
        }

        liveEnemyMarkers.Clear();
        liveEnemyLabels.Clear();
        liveEnemyCounts.Clear();
        liveEnemyRemoveBuffer.Clear();
    }

    private void UpdateLiveEnemyPresence()
    {
        if (graphArea == null || activeScanner == null ||
            scanTeamID < 0 || realtimeTopologyNodePositions.Count == 0)
            return;

        if (liveEnemyMap == null)
            liveEnemyMap = FindFirstObjectByType<Map1HexGrid>();

        if (liveEnemyMap == null)
            return;

        liveEnemyCounts.Clear();

        // Read the authoritative current unit state, including death/respawn.
        PlayerUnit[] units = FindObjectsByType<PlayerUnit>(
            FindObjectsSortMode.None
        );

        foreach (PlayerUnit unit in units)
        {
            if (unit == null || unit.IsDead || unit.teamID == scanTeamID)
                continue;

            // Settled topology tile is preferable to a world-position guess.
            // During movement, the current logical tile is used, so the marker
            // changes nodes as the unit's occupancy changes.
            string tileName = null;
            if (!unit.IsMoving &&
                !string.IsNullOrEmpty(unit.TopologySettledTile))
            {
                tileName = unit.TopologySettledTile;
            }
            else
            {
                var resolved = liveEnemyMap.GetPlayerTileAndVertex(unit.gameObject);
                tileName = resolved.tileName;
            }

            if (string.IsNullOrEmpty(tileName) ||
                !realtimeTopologyNodePositions.ContainsKey(tileName))
                continue;

            liveEnemyCounts.TryGetValue(tileName, out int count);
            liveEnemyCounts[tileName] = count + 1;
        }

        // Reuse badges; only add/remove them when occupancy changes.
        liveEnemyRemoveBuffer.Clear();
        foreach (var pair in liveEnemyMarkers)
        {
            if (!liveEnemyCounts.ContainsKey(pair.Key))
                liveEnemyRemoveBuffer.Add(pair.Key);
        }

        foreach (string tileName in liveEnemyRemoveBuffer)
        {
            if (liveEnemyMarkers[tileName] != null)
                Destroy(liveEnemyMarkers[tileName]);
            liveEnemyMarkers.Remove(tileName);
            liveEnemyLabels.Remove(tileName);
        }

        foreach (var pair in liveEnemyCounts)
        {
            if (!liveEnemyMarkers.TryGetValue(pair.Key, out GameObject marker) ||
                marker == null)
            {
                marker = CreateUIObject("EnemyPresence_" + pair.Key, graphArea);
                RectTransform rect = marker.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(14f, 14f);

                Image background = marker.AddComponent<Image>();
                background.color = new Color(0.96f, 0.22f, 0.18f, 0.95f);
                background.raycastTarget = false;

                TextMeshProUGUI label = CreateText(
                    "EnemyCount", marker.transform, 10f
                );
                label.alignment = TextAlignmentOptions.Center;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
                label.raycastTarget = false;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                liveEnemyMarkers[pair.Key] = marker;
                liveEnemyLabels[pair.Key] = label;
            }

            // Slightly offset from each node; avoid covering orange diamonds.
            marker.GetComponent<RectTransform>().anchoredPosition =
                realtimeTopologyNodePositions[pair.Key] + new Vector2(11f, 11f);
            liveEnemyLabels[pair.Key].text = pair.Value > 1
                ? pair.Value.ToString()
                : string.Empty;
            // Counter-rotate so the count remains upright as the map turns.
            marker.transform.localRotation = Quaternion.Inverse(graphArea.localRotation);
            // Keep markers legible on top of existing node visuals.
            marker.transform.SetAsLastSibling();
        }
    }


    private void ClearLiveFriendlyMarkers()
    {
        foreach (GameObject marker in liveFriendlyMarkers.Values)
        {
            if (marker != null)
                Destroy(marker);
        }

        liveFriendlyMarkers.Clear();
        liveFriendlyLabels.Clear();
        liveFriendlyCounts.Clear();
        liveFriendlyRemoveBuffer.Clear();
    }

    private void UpdateLiveFriendlyPresence()
    {
        if (graphArea == null ||
            activeScanner == null ||
            scanTeamID < 0 ||
            realtimeTopologyNodePositions.Count == 0)
        {
            return;
        }

        // Reuse the same authoritative map as enemy tracking.
        if (liveEnemyMap == null)
            liveEnemyMap = FindFirstObjectByType<Map1HexGrid>();

        if (liveEnemyMap == null)
            return;

        liveFriendlyCounts.Clear();

        PlayerUnit[] units = FindObjectsByType<PlayerUnit>(
            FindObjectsSortMode.None
        );

        // ---------------------------------------------
        // FIND FRIENDLY UNIT OCCUPANCY
        // ---------------------------------------------

        foreach (PlayerUnit unit in units)
        {
            if (unit == null ||
                unit.IsDead ||
                unit.teamID != scanTeamID)
            {
                continue;
            }


            string tileName = null;

            // 1. Prefer the settled topology tile when valid.
            if (!unit.IsMoving &&
                !string.IsNullOrEmpty(unit.TopologySettledTile) &&
                realtimeTopologyNodePositions.ContainsKey(
                    unit.TopologySettledTile))
            {
                tileName = unit.TopologySettledTile;
            }

            // 2. Try the existing live vertex resolution.
            if (string.IsNullOrEmpty(tileName))
            {
                var resolved =
                    liveEnemyMap.GetPlayerTileAndVertex(
                        unit.gameObject
                    );

                if (!string.IsNullOrEmpty(resolved.tileName) &&
                    realtimeTopologyNodePositions.ContainsKey(
                        resolved.tileName))
                {
                    tileName = resolved.tileName;
                }
            }

            // 3. Fallback to the unit's logical hex coordinate.
            // This remains useful while crossing between vertices.
            if (string.IsNullOrEmpty(tileName))
            {
                string logicalTile =
                    GetTileNameFromCoord(
                        liveEnemyMap,
                        unit.currentTileCoord
                    );

                if (!string.IsNullOrEmpty(logicalTile) &&
                    realtimeTopologyNodePositions.ContainsKey(
                        logicalTile))
                {
                    tileName = logicalTile;
                }
            }

            // No valid active topology node is available.
            if (string.IsNullOrEmpty(tileName))
            {
                continue;
            }


            liveFriendlyCounts.TryGetValue(
                tileName,
                out int count
            );

            liveFriendlyCounts[tileName] = count + 1;
        }

        // ---------------------------------------------
        // REMOVE MARKERS FROM VACATED NODES
        // ---------------------------------------------

        liveFriendlyRemoveBuffer.Clear();

        foreach (var pair in liveFriendlyMarkers)
        {
            if (!liveFriendlyCounts.ContainsKey(pair.Key))
                liveFriendlyRemoveBuffer.Add(pair.Key);
        }

        foreach (string tileName in liveFriendlyRemoveBuffer)
        {
            if (liveFriendlyMarkers[tileName] != null)
                Destroy(liveFriendlyMarkers[tileName]);

            liveFriendlyMarkers.Remove(tileName);
            liveFriendlyLabels.Remove(tileName);
        }

        // ---------------------------------------------
        // CREATE OR UPDATE FRIENDLY MARKERS
        // ---------------------------------------------

        foreach (var pair in liveFriendlyCounts)
        {
            if (!liveFriendlyMarkers.TryGetValue(
                    pair.Key,
                    out GameObject marker) ||
                marker == null)
            {
                marker = CreateUIObject(
                    "FriendlyPresence_" + pair.Key,
                    graphArea
                );

                RectTransform rect =
                    marker.GetComponent<RectTransform>();

                rect.anchorMin =
                    rect.anchorMax =
                        new Vector2(0.5f, 0.5f);

                rect.pivot =
                    new Vector2(0.5f, 0.5f);

                rect.sizeDelta =
                    new Vector2(14f, 14f);

                Image background =
                    marker.AddComponent<Image>();

                background.color =
                    new Color(0.18f, 0.82f, 0.91f, 0.95f);

                background.raycastTarget = false;

                TextMeshProUGUI label = CreateText(
                    "FriendlyCount",
                    marker.transform,
                    10f
                );

                label.alignment =
                    TextAlignmentOptions.Center;

                label.fontStyle =
                    FontStyles.Bold;

                label.color =
                    new Color(0.02f, 0.12f, 0.18f, 1f);

                label.raycastTarget = false;

                RectTransform labelRect =
                    label.rectTransform;

                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                liveFriendlyMarkers[pair.Key] = marker;
                liveFriendlyLabels[pair.Key] = label;
            }

            // Friendly = upper-left of node.
            // Enemy = upper-right of node.
            RectTransform markerRect =
                marker.GetComponent<RectTransform>();

            markerRect.anchoredPosition =
                realtimeTopologyNodePositions[pair.Key] +
                new Vector2(-11f, 11f);

            // Show a count only when units share a node.
            liveFriendlyLabels[pair.Key].text =
                pair.Value > 1
                    ? pair.Value.ToString()
                    : string.Empty;

            // Keep the badge upright as the graph rotates.
            marker.transform.localRotation =
                Quaternion.Inverse(graphArea.localRotation);

            marker.transform.SetAsLastSibling();
        }
    }


    private void ClearGraph()
    {

        nodeMarginVisuals.Clear();
        nextNodeMarginRefreshTime = 0f;
        ClearLiveEnemyMarkers();
        ClearLiveFriendlyMarkers();

        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        generatedObjects.Clear();
    }


    // =====================================================
    // HELPERS
    // =====================================================

    private void CreateLegendItem(
    Transform parent,
    string objectName,
    string label,
    Color markerColor,
    bool diamond,
    Vector2 position
)
    {
        GameObject marker =
            CreateUIObject(
                objectName + "_Marker",
                parent
            );

        RectTransform markerRect =
            marker.GetComponent<RectTransform>();

        markerRect.anchorMin =
            new Vector2(0f, 0f);

        markerRect.anchorMax =
            new Vector2(0f, 0f);

        markerRect.pivot =
            new Vector2(0.5f, 0.5f);

        markerRect.anchoredPosition =
            position;

        markerRect.sizeDelta =
            diamond
            ? new Vector2(17f, 17f)
            : new Vector2(9f, 9f);

        Image markerImage =
            marker.AddComponent<Image>();

        markerImage.color =
            markerColor;

        if (diamond)
        {
            markerRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    45f
                );
        }


        TextMeshProUGUI text =
            CreateText(
                objectName + "_Text",
                parent,
               diamond
               ? 20f
               : 14f
            );

        string conceptID =
    diamond
    ? "articulation_point"
    : "active_node";

        text.text =
            $"<link=\"{conceptID}\">{label}</link>";

        if (diamond)
        {
            text.fontStyle =
                FontStyles.Bold;

            text.color =
                new Color(
                    1f,
                    0.52f,
                    0.12f,
                    1f
                );
        }

        if (diamond)
        {
            articulationLegendText =
                text;
        }
        else
        {
            activeNodeLegendText =
                text;
        }

        text.alignment =
            TextAlignmentOptions.Left;

        text.color =
            new Color(
                0.72f,
                0.90f,
                0.93f,
                1f
            );

        RectTransform textRect =
            text.rectTransform;

        textRect.anchorMin =
            new Vector2(0f, 0f);

        textRect.anchorMax =
            new Vector2(0f, 0f);

        textRect.pivot =
            new Vector2(0f, 0.5f);

        textRect.anchoredPosition =
            position +
            new Vector2(
                13f,
                0f
            );

        textRect.sizeDelta =
            diamond
            ? new Vector2(
                220f,
                32f
            )
    : new Vector2(
        150f,
        24f
    );
    }

    private void CreateMarkerSegment(
    string objectName,
    Vector2 position,
    Vector2 size,
    Color color
)
    {
        GameObject segment =
            CreateUIObject(
                objectName,
                graphArea
            );

        generatedObjects.Add(
            segment
        );

        RectTransform rect =
            segment.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        Image image =
            segment.AddComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;
    }



    private void UpdateConceptInteraction()
    {
        string hoveredConcept =
            GetHoveredConcept();

        // ---------------------------------------------
        // CLICKED AN ANALYZER CONCEPT
        // ---------------------------------------------

        if (!string.IsNullOrEmpty(hoveredConcept) &&
            Input.GetMouseButtonDown(0))
        {
            // Remember the current page before navigating.
            if (currentConceptPage != null &&
                currentConceptPage != hoveredConcept)
            {
                // Following an existing BACK hyperlink
                // should not create another history entry.
                if (conceptBackStack.Count > 0 &&
                    conceptBackStack.Peek() == hoveredConcept)
                {
                    conceptBackStack.Pop();
                }
                else
                {
                    conceptBackStack.Push(
                        currentConceptPage
                    );
                }
            }

            currentConceptPage = hoveredConcept;

            conceptPopupPinned = true;
            lastHoveredConcept = null;

            ShowConceptDebrief(
                hoveredConcept
            );

            return;
        }

        // ---------------------------------------------
        // CLICKED ANYWHERE ELSE
        // ---------------------------------------------


        if (Input.GetMouseButtonDown(0))
        {
            // A click inside the concept popup must not
            // dismiss it merely because TMP missed a link.
            bool clickedInsidePopup =
                conceptPopupObject != null &&
                conceptPopupObject.activeInHierarchy &&
                conceptPopupRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    conceptPopupRect,
                    Input.mousePosition,
                    null
                );

            if (clickedInsidePopup)
            {
                return;
            }

            // Clicking the Analyzer background still
            // dismisses the concept popup.
            HideConceptPopup();
            return;
        }


        // ---------------------------------------------
        // PINNED DEBRIEF
        // ---------------------------------------------

        if (conceptPopupPinned)
        {
            return;
        }

        // ---------------------------------------------
        // NOTHING HOVERED
        // ---------------------------------------------

        if (string.IsNullOrEmpty(hoveredConcept))
        {
            HideConceptPopup();
            return;
        }

        // ---------------------------------------------
        // SAME CONCEPT — DO NOTHING
        // ---------------------------------------------

        if (hoveredConcept == lastHoveredConcept)
        {
            return;
        }

        // ---------------------------------------------
        // HOVER DEFINITION
        // ---------------------------------------------

        lastHoveredConcept = hoveredConcept;

        ShowConceptDefinition(
            hoveredConcept
        );
    }


    private void HideConceptPopup()
    {
        currentConceptPage = null;
        conceptBackStack.Clear();

        conceptPopupPinned =
            false;

        lastHoveredConcept =
            null;

        if (conceptPopupObject != null)
        {
            conceptPopupObject.SetActive(
                false
            );
        }
    }
    private string GetHoveredConcept()
    {
        string concept =
            GetHoveredLinkID(
                conceptPopupText
            );

        if (!string.IsNullOrEmpty(
                concept
            ))
        {
            return concept;
        }


        concept =
            GetHoveredLinkID(
                analyzerTitleText
            );

        if (!string.IsNullOrEmpty(
                concept
            ))
        {
            return concept;
        }


        concept =
            GetHoveredLinkID(
                statsText
            );

        if (!string.IsNullOrEmpty(
                concept
            ))
        {
            return concept;
        }


        concept =
            GetHoveredLinkID(
                activeNodeLegendText
            );

        if (!string.IsNullOrEmpty(
                concept
            ))
        {
            return concept;
        }


        concept =
            GetHoveredLinkID(
                articulationLegendText
            );

        return concept;
    }


    private string GetHoveredLinkID(
        TextMeshProUGUI text
    )
    {
        if (text == null)
            return null;

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                text,
                Input.mousePosition,
                null
            );

        if (linkIndex < 0)
            return null;

        if (linkIndex >=
            text.textInfo.linkCount)
        {
            return null;
        }

        return text.textInfo
            .linkInfo[linkIndex]
            .GetLinkID();
    }


    private void ShowConceptDefinition(
    string concept
)
    {
        if (conceptPopupObject == null ||
            conceptPopupText == null ||
            conceptPopupRect == null)

            // Concept explanations must not retain
            // intervention controls from node inspection.
            DestroyInterventionDial();
        {
            return;
        }

        conceptPopupPinned =
            false;

        conceptPopupText.text =
            $"<b><size=115%>{GetConceptTitle(concept)}</size></b>\n\n" +
            GetConceptDefinition(concept) +
            "\n\n<size=80%><color=#7FC7D4>" +
            "CLICK // SCENARIO DEBRIEF" +
            "</color></size>";

        ResizeConceptPopupToFitText(
            190f
        );
    }

    private void ResizeConceptPopupToFitText(
    float minimumHeight
)
    {
        if (conceptPopupObject == null ||
            conceptPopupRect == null ||
            conceptPopupText == null)
        {
            return;
        }

        // The popup must be active for TMP to calculate
        // the final wrapped text correctly.
        conceptPopupObject.SetActive(true);

        Canvas.ForceUpdateCanvases();

        // Force TMP to rebuild using the actual current
        // width, font size, wrapping and rich-text tags.
        conceptPopupText.ForceMeshUpdate(
            true,
            true
        );

        float requiredHeight =
            conceptPopupText.preferredHeight +
            60f;

        requiredHeight =
            Mathf.Max(
                minimumHeight,
                requiredHeight
            );

        conceptPopupRect.sizeDelta =
            new Vector2(
                430f,
                requiredHeight
            );

        Canvas.ForceUpdateCanvases();

        conceptPopupText.ForceMeshUpdate(
            true,
            true
        );
    }

    private string GetConceptFooter(
    string concept
)
    {
        switch (concept)
        {
            case "topology":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"tactical_scanner\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";

            case "structural_health":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"topology_advanced\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";

            case "current_load":
            case "effective_capacity":
            case "margin":
            case "structural_state":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"structural_health\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";

            case "critical_advanced":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"critical\">" +
                    "← BACK " +
                    "</link>" +
                    "</u></color>";

            case "nodes_advanced":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"nodes\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";

            case "links_advanced":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"links\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";

            case "components_advanced":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"components\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";


            case "topology_advanced":
                return
                    "<color=#62D6E8><u>" +
                    "<link=\"topology\">" +
                    "← BACK" +
                    "</link>" +
                    "</u></color>";


            default:
                return
                    "<size=80%><color=#7FC7D4>" +
                    "\"PRESS ESC TO GO BACK\"" +
                    "</color></size>";
        }


    }

    private void ShowConceptDebrief(
    string concept
)
    {
        if (conceptPopupObject == null ||
            conceptPopupText == null ||
            conceptPopupRect == null)
        {
            return;
        }

        DestroyInterventionDial();

        conceptPopupPinned =
            true;

        lastHoveredConcept =
            null;

        string definition =
            GetConceptDefinition(
                concept
            );

        string debrief =
            BuildConceptDebrief(
                concept
            );

        string footer =
            GetConceptFooter(
                concept
            );

        string content =
            $"<b><size=115%>{GetConceptTitle(concept)}</size></b>";

        if (!string.IsNullOrEmpty(
            definition
        ))
        {
            content +=
                "\n\n" +
                definition;
        }

        if (!string.IsNullOrEmpty(
            debrief
        ))
        {
            content +=
                "\n\n" +
                debrief;
        }

        if (!string.IsNullOrEmpty(
            footer
        ))
        {
            content +=
                "\n\n" +
                footer;
        }

        conceptPopupText.text =
            content;

        ResizeConceptPopupToFitText(
            concept == "topology" ? 190f : 320f
 );
    }


    private string GetConceptTitle(
    string concept
    )
    {
        switch (concept)
        {
            case "tactical_scanner":
                return "TACTICAL SCANNER";

            case "topology":
                return "TOPOLOGY";

            case "topology_advanced":
                return "TOPOLOGY";

            case "nodes":
                return "NODES";

            case "links":
                return "LINKS";

            case "components":
                return "CONNECTED COMPONENTS";

            case "critical":
                return "CRITICAL NODES";

            case "active_node":
                return "ACTIVE NODE";

            case "articulation_point":
                return "ARTICULATION POINT";

            case "structural_health":
                return "STRUCTURAL HEALTH";

            case "current_load":
                return "CURRENT LOAD";

            case "effective_capacity":
                return "EFFECTIVE CAPACITY";

            case "margin":
                return "MARGIN";

            case "structural_state":
                return "STRUCTURAL STATE";

            case "critical_advanced":
                return "CRITICAL NODES";

            case "nodes_advanced":
                return "NODES";

            case "links_advanced":
                return "LINKS";

            case "components_advanced":
                return "CONNECTED COMPONENTS";

            default:
                return "NETWORK ANALYSIS";
        }
    }


    private string GetConceptDefinition(
    string concept
)
    {
        switch (concept)
        {
            case "tactical_scanner":
                return
                    "A subsystem that examines the battlefield as a network, " +
                    "using <color=#62D6E8><u><link=\"topology\">TOPOLOGY</link></u></color> " +
                    "to show how active hexes are connected and how those connections " +
                    "can change when locations disappear.";


            case "topology":
                return
                    "Network topology describes how elements " +
                    "within a system are connected and organized.";

            case "topology_advanced":
                return string.Empty;


            case "nodes":
                return
                    "The active locations represented in the network. " +
                    "Collapsed hexes are no longer counted as nodes.";

            case "links":
                return
                    "Direct connections between nodes. " +
                    "A link means two locations are connected in the current topology.";

            case "components":
                return
                    "Separate connected regions of the network. " +
                    "Within one component, every node can reach every other node " +
                    "through some chain of links.";

            case "critical":
                return
                    "The number of articulation points currently detected. " +
                    "These are nodes whose removal would break part of the " +
                    "current network into separate connected regions.";

            case "active_node":
                return
                    "A hex currently present in the live network. " +
                    "Active means the location has not been removed by collapse.";

            case "articulation_point":
                return
                    "A node that holds parts of the network together. " +
                    "If it disappears, locations that are currently connected " +
                    "can become separated into different regions.";

            case "structural_health":
                return
                    "Structural health describes the physical condition of a hex through its " +

                    "<color=#62D6E8><u><link=\"current_load\">" +
                    "CURRENT LOAD" +
                    "</link></u></color>, " +

                    "<color=#62D6E8><u><link=\"effective_capacity\">" +
                    "EFFECTIVE CAPACITY" +
                    "</link></u></color>, " +

                    "<color=#62D6E8><u><link=\"margin\">" +
                    "MARGIN" +
                    "</link></u></color>, and " +

                    "<color=#62D6E8><u><link=\"structural_state\">" +
                    "STRUCTURAL STATE" +
                    "</link></u></color>.";

            case "current_load":
                return
                    "The total structural load currently imposed on a hex " +
                    "by the units occupying it.";

            case "effective_capacity":
                return
                    "The amount of structural load a hex can currently support " +
                    "after capacity modifications are applied.";

            case "margin":
                return
                    "The difference between a hex's Effective Capacity and Current Load. " +
                    "It shows how much structural tolerance remains.";

            case "structural_state":
                return
                    "The current structural condition of a hex, determined by " +
                    "its load, capacity, and failure progression.";

            case "critical_advanced":
                return string.Empty;

            case "nodes_advanced":
                return string.Empty;

            case "links_advanced":
                return string.Empty;
                return string.Empty;

            case "components_advanced":
                return string.Empty;

            default:
                return
                    "No definition is available.";
        }
    }


    private string BuildConceptDebrief(
        string concept
    )
    {
        if (activeScanner == null ||
            !activeScanner.HasScanData)
        {
            return
                "No current scan data is available.";
        }


        int nodeCount =
            activeScanner.LastGraph.Count;

        int componentCount =
            activeScanner.LastConnectedComponents;

        int criticalCount =
            activeScanner.LastArticulationPoints.Count;


        int edgeCount = 0;

        foreach (var pair in
                 activeScanner.LastGraph)
        {
            edgeCount +=
                pair.Value.Count;
        }

        edgeCount /= 2;


        switch (concept)
        {
            case "tactical_scanner":
                {
                    return
                        "The Topology Analyzer presents the scanner's current reading " +
                        "of battlefield connectivity.\n\n" +

                        "Here, you can select the terms on the analyzer to inspect individual network concepts, " +
                        "or select a graph node to examine its current structural and topological state.\n\n\n" +

                        "<size=80%><color=#7FC7D4>" +
                        "<b>SCANNER WARNING</b>\n" +
                        "This scanner only reveals evidence and consequences " +
                        "about the battlefield state, " +
                        "but it's you, the Analyst, who can decide which intervention to deploy." +
                        "</color></size>";
                }


            case "topology":
                {
                    return
                        "Those connections can change when " +
                        "locations disappear.\n\n" +

                        "<color=#62D6E8><u>" +
                        "<link=\"topology_advanced\">" +
                        "EXPANDED INFO" +
                        "</link>" +
                        "</u></color>";
                }


            case "topology_advanced":
                {
                    return
                        "<b>EXPANDED INFO</b>\n\n" +

                        "In graph theory, a network can be " +
                        "represented through <b>nodes</b> (elements) " +
                        "and <b>links</b> (connections between them).\n\n" +

                        "Network topology focuses on how these " +
                        "elements are connected, regardless of their " +
                        "physical appearance or distance.\n\n" +

                        "Understanding these relationships helps us " +
                        "identify alternative routes, connected " +
                        "regions, and critical points whose removal " +
                        "could divide a network.\n\n" +

                        "<b>IN HEXA_766</b>\n" +

                        "Active hexes are represented as nodes. " +
                        "When a hex collapses, its node and associated " +
                        "links disappear, potentially changing " +
                        "the network's structure.\n\n" +

                        "<color=#62D6E8><b>RELATED ANALYSIS</b></color>\n" +

                        "Network connectivity and structural stability " +
                        "describe different properties of a system. " +
                        "Explore " +

                        "<color=#62D6E8><u>" +
                        "<link=\"structural_health\">" +
                        "STRUCTURAL HEALTH" +
                        "</link></u></color> " +

                        "to examine the physical condition of a hex.";
                }


            case "nodes":
                {
                    return
                        $"The current scan contains <b>{nodeCount}</b> active nodes.\n\n" +
                        "Each node represents a battlefield hex that still participates " +
                        "\n\n" +

                        "<color=#62D6E8><u>" +
                        "<link=\"nodes_advanced\">" +
                        "EXPANDED INFO" +
                        "</link>" +
                        "</u></color>";
                }

            case "nodes_advanced":
                {
                    int nodesExpandedCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastGraph.Count
                            : 0;

                    return
                        "<b>EXPANDED INFO</b>\n\n" +

                        $"The current topology contains {nodesExpandedCount} active nodes.\n\n" +

                        "Each node represents an active battlefield hex. " +
                        "Its position in the graph represents its connections to other nodes, " +
                        "not its physical distance from them.\n\n" +

                        "When a hex collapses, it is removed from the active topology. " +
                        "Its node disappears together with the links that connected it " +
                        "to neighboring nodes.\n\n" +

                        "Removing nodes can therefore change the structure of the network, " +
                        "reduce available connections, create separate connected components, " +
                        "or make other nodes more important to connectivity.";
                }

            case "links":
                {
                    int linksBasicNodeCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastGraph.Count
                            : 0;

                    int linksBasicEdgeCount = 0;

                    if (activeScanner != null &&
                        activeScanner.HasScanData)
                    {
                        foreach (var pair in activeScanner.LastGraph)
                        {
                            linksBasicEdgeCount +=
                                pair.Value.Count;
                        }

                        linksBasicEdgeCount /= 2;
                    }

                    float linksBasicAverageDegree =
                        linksBasicNodeCount > 0
                            ? (2f * linksBasicEdgeCount) /
                              linksBasicNodeCount
                            : 0f;

                    return
                        $"The current topology contains <b>{linksBasicEdgeCount} links</b>.\n\n" +

                        $"The average node degree is <b>{linksBasicAverageDegree:F2}</b>, " +
                        "meaning each active node has about " +
                        $"{linksBasicAverageDegree:F2} direct connections on average.\n\n" +

                        "<color=#62D6E8><u>" +
                        "<link=\"links_advanced\">" +
                        "EXPANDED INFO" +
                        "</link>" +
                        "</u></color>";
                }

            case "links_advanced":
                {
                    return
                        "<b>EXPANDED INFO</b>\n\n" +

                        "A link represents a direct connection between two active nodes. " +
                        "If two nodes are linked, the network connects those locations directly.\n\n" +

                        "Each link contributes one connection to each of its two nodes. " +
                        "This is why average node degree is calculated using twice the number " +
                        "of links divided by the number of nodes.\n\n" +

                        "A higher average degree generally indicates a more densely connected " +
                        "network, but it does not show which individual nodes are critical.\n\n" +

                        "Links describe network connectivity, not recommended movement. " +
                        "Their importance depends on how they contribute to the structure " +
                        "of the network as a whole.";
                }
            case "components":
                {
                    int componentsBasicCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastConnectedComponents
                            : 0;

                    if (componentsBasicCount == 1)
                    {
                        return
                            "The current network contains <b>1 connected component</b>.\n\n" +

                            "All active nodes can currently reach one another through " +
                            "some chain of links.\n\n" +

                            "<color=#62D6E8><u>" +
                            "<link=\"components_advanced\">" +
                            "EXPANDED INFO" +
                            "</link>" +
                            "</u></color>";
                    }

                    return
                        $"The current network contains <b>{componentsBasicCount} connected components</b>.\n\n" +

                        "The battlefield network is fragmented into separate regions. " +
                        "Nodes in different components cannot currently reach one another " +
                        "through the network.\n\n" +

                        "<color=#62D6E8><u>" +
                        "<link=\"components_advanced\">" +
                        "EXPANDED INFO" +
                        "</link>" +
                        "</u></color>";
                }

            case "components_advanced":
                {
                    int componentsExpandedCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastConnectedComponents
                            : 0;

                    if (componentsExpandedCount == 1)
                    {
                        return
                            "<b>EXPANDED INFO</b>\n\n" +

                            "A connected component is a group of nodes in which every node " +
                            "can reach every other node through some chain of links.\n\n" +

                            "The current topology has one component, so the active network " +
                            "is still connected as a whole.\n\n" +

                            "However, being connected does not mean the network is equally " +
                            "resilient everywhere. Articulation points can still exist: " +
                            "removing one of them may divide this single component into " +
                            "multiple components.";
                    }

                    return
                        "<b>EXPANDED INFO</b>\n\n" +

                        "A connected component is a group of nodes in which every node " +
                        "can reach every other node through some chain of links.\n\n" +

                        $"The current topology contains {componentsExpandedCount} separate components, " +
                        "so the active network is no longer connected as a whole.\n\n" +

                        "A collapse can create additional components when removing a node " +
                        "and its links cuts the remaining network into separate regions.";
                }

            case "critical":
                {
                    int criticalPageNodeCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastGraph.Count
                            : 0;

                    int criticalPageCriticalCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastArticulationPoints.Count
                            : 0;

                    int criticalPagePercentage =
                        criticalPageNodeCount > 0
                            ? Mathf.RoundToInt(
                                criticalPageCriticalCount * 100f /
                                criticalPageNodeCount
                            )
                            : 0;

                    return
                        $"<b>CURRENT SCAN // {criticalPageCriticalCount} OF " +
                        $"{criticalPageNodeCount} NODES ({criticalPagePercentage}%)</b>\n\n" +

                        "<color=#62D6E8><u>" +
                        "<link=\"critical_advanced\">" +
                        "EXPAND ANALYSIS" +
                        "</link>" +
                        "</u></color>";
                }

            case "critical_advanced":
                {
                    int criticalAdvancedNodeCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastGraph.Count
                            : 0;

                    int criticalAdvancedCount =
                        activeScanner != null &&
                        activeScanner.HasScanData
                            ? activeScanner.LastArticulationPoints.Count
                            : 0;

                    int criticalAdvancedPercentage =
                        criticalAdvancedNodeCount > 0
                            ? Mathf.RoundToInt(
                                criticalAdvancedCount * 100f /
                                criticalAdvancedNodeCount
                            )
                            : 0;

                    return
                        "<b>EXPANDED INFO</b>\n\n" +

                        $"{criticalAdvancedCount} of {criticalAdvancedNodeCount} active nodes " +
                        $"({criticalAdvancedPercentage}%) are articulation points.\n\n" +

                        "The network may be connected now, but many locations can act " +
                        "as single-node bottlenecks. Removing one of these nodes would " +
                        "split part of the graph into separate connected regions.\n\n" +

                        "<b>CRITICAL means important to NETWORK CONNECTIVITY.</b>\n\n" +

                        "This is a <b>topological</b> property: it describes how a node " +
                        "helps hold the network together.\n\n" +

                        "It does not mean the hex is overloaded, damaged, " +
                        "or about to collapse.\n\n" +

                        "Tip:Not all critical nodes are equally important strategically.";
                }

            case "active_node":
                {
                    return
                        $"The analyzer currently represents <b>{nodeCount}</b> active nodes.\n\n" +
                        "An active node is simply a hex still participating in the graph. " +
                        "It may be structurally safe, full, overloaded, or topologically critical.";
                }


            case "articulation_point":
                {
                    return
                        "<b>AN ARTICULATION POINT HOLDS PARTS OF THE NETWORK TOGETHER.</b>\n\n" +

                        $"The current scan detects <b>{criticalCount}</b> articulation points.\n\n" +

                        "If one of these nodes is removed, the number of connected " +
                        "components increases. Parts of the battlefield that could " +
                        "previously reach one another may become separated.\n\n" +

                        "<b>This is a TOPOLOGICAL property.</b>\n" +
                        "It describes connectivity, not structural health.\n\n" +

                        "A structurally safe hex can still be an articulation point. " +
                        "Likewise, an overloaded hex is not necessarily topologically critical.";
                }

            case "current_load":
                {
                    return

                        "<b>CURRENT LOAD = SUM OF UNIT EFFECTIVE LOADS</b>";
                }


            case "effective_capacity":
                {
                    return
                        "Effective Capacity is the amount of load a hex can currently support.\n\n" +

                        "It begins with the hex's base capacity and includes any changes " +
                        "produced by structural Subsystems.\n\n" +

                        "STRUCTURAL REINFORCEMENT can increase it, while NETWORK STRESS " +
                        "can reduce it.\n\n" +

                        "<b>EFFECTIVE CAPACITY CANNOT FALL BELOW 0.</b>";
                }


            case "margin":
                {
                    return
                        "Margin compares the load placed on a hex with the capacity " +
                        "available to support it.\n\n" +

                        "<b>MARGIN = EFFECTIVE CAPACITY − CURRENT LOAD</b>\n\n" +

                        "A positive margin means capacity remains available. " +
                        "A margin of 0 means the hex is full. " +
                        "A negative margin means the hex is overloaded.";
                }


            case "structural_state":
                {
                    return
                        "Structural State describes the current condition of a hex.\n\n" +

                        "<b>SAFE</b> : Load is below Capacity.\n" +
                        "<b>FULL</b> : Load equals Capacity.\n" +
                        "<b>OVERLOADED</b> : Load exceeds Capacity.\n" +
                        "<b>COLLAPSING</b> : Persistent overload has progressed toward failure.\n" +
                        "<b>COLLAPSED</b> : The hex has failed and is removed from the active topology.\n\n" +

                        "Overload does not mean immediate collapse. " +
                        "There is an intervention window before persistent overload " +
                        "becomes structural failure.";
                }
            default:
                Debug.LogWarning(
                    $"No concept content defined for: {concept}"
                );

                return string.Empty;
        }
    }

    private void CreateObjectiveLine(
        string objectName,
        Vector2 from,
        Vector2 to,
        Color color,
        float thickness
    )
    {
        GameObject line =
            CreateUIObject(
                objectName,
                graphArea
            );

        generatedObjects.Add(
            line
        );

        RectTransform rect =
            line.GetComponent<RectTransform>();

        Vector2 direction =
            to - from;

        float distance =
            direction.magnitude;

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0f,
                0.5f
            );

        rect.anchoredPosition =
            from;

        rect.sizeDelta =
            new Vector2(
                distance,
                thickness
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        Image image =
            line.AddComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;
    }


    private GameObject CreateUIObject(
    string objectName,
    Transform parent
)
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj;
    }


    private TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        float fontSize
    )
    {
        GameObject obj =
            CreateUIObject(
                objectName,
                parent
            );

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        text.fontSize =
            fontSize;

        text.color =
            new Color(
                0.78f,
                0.95f,
                1f,
                1f
            );

        text.enableWordWrapping =
            false;

        return text;
    }

    private IEnumerator ReturnCameraToActiveUnitAfterDelay()
    {
        yield return new WaitForSeconds(
            1.0f
        );

        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow == null ||
            TurnManager.Instance == null ||
            TurnManager.Instance.current == null)
        {
            yield break;
        }

        PlayerUnit activeUnit =
            TurnManager.Instance.current;

        cameraFollow.SetTarget(
            activeUnit.transform
        );

        Debug.Log(
            $"[TACTICAL SCANNER] Camera returned to active unit | " +
            $"Unit={activeUnit.UnitCode}"
        );
    }
}