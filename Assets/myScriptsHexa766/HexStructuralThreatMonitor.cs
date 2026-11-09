using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach once to the scene's Map1 object (the one with Map1HexGrid).
// No modifications to HexCapacity, HexCollapseFeedback, or TurnManager.
[RequireComponent(typeof(Map1HexGrid))]
public class HexStructuralThreatMonitor : MonoBehaviour
{
    [SerializeField, Range(0.1f, 1f)]
    private float refreshInterval = 0.2f;

    [SerializeField] private float outlineWidth = 0.065f;
    [SerializeField] private float surfaceOffset = 0.12f;

    private sealed class ThreatRing
    {
        public LineRenderer Line;
        public int Level;
    }

    private readonly Dictionary<HexCapacity, ThreatRing> rings =
        new Dictionary<HexCapacity, ThreatRing>();

    private Map1HexGrid map;
    private Material outlineMaterial;
    private float nextRefresh;

    private GameObject bannerObject;
    private Image bannerBackground;
    private Outline bannerBorder;
    private TextMeshProUGUI bannerText;
    private int activeWarningLevel;

    private RectTransform threatCanvasRect;
    private RectTransform threatBannerRect;
    private RectTransform scannerPopupRect;
    private RectTransform scannerPanelRect;

    private readonly Vector3[] popupCorners =
        new Vector3[4];

    private void Awake()
    {
        map = GetComponent<Map1HexGrid>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
            outlineMaterial = new Material(shader);
        else
            Debug.LogWarning("[STRUCTURAL THREAT] No line shader found.");

        CreateBanner();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + refreshInterval;
            RefreshThreats();
        }

        AnimateWarnings();
    }

    private void RefreshThreats()
    {
        if (map == null)
            return;

        // Mark all old warning outlines inactive before checking the live hexes.
        foreach (ThreatRing ring in rings.Values)
        {
            if (ring.Line != null)
                ring.Line.enabled = false;
        }

        int strongestLevel = 0;
        int lowestMargin = int.MaxValue;
        string selectedTile = null;

        foreach (var pair in map.tileGameObjectMap)
        {
            GameObject tile = pair.Value;
            if (tile == null)
                continue;

            HexCapacity hex = tile.GetComponent<HexCapacity>();
            if (hex == null)
                hex = tile.GetComponentInChildren<HexCapacity>(true);

            if (hex == null || hex.IsCollapsed)
                continue;

            int margin = hex.EffectiveCapacity - hex.CurrentLoad;
            bool collapsing = hex.IsCollapseSequenceRunning;

            // No warning for a healthy hex, unless collapse is already committed.
            if (margin >= 0 && !collapsing)
                continue;

            int level = collapsing ? 3 :
                hex.ConsecutiveOverloadedRoundChecks >= 1 ? 2 : 1;

            ThreatRing ring = GetOrCreateRing(tile, hex);
            if (ring != null && ring.Line != null)
            {
                ring.Level = level;
                ring.Line.enabled = true;
            }

            if (level > strongestLevel ||
                (level == strongestLevel && margin < lowestMargin))
            {
                strongestLevel = level;
                lowestMargin = margin;
                selectedTile = pair.Key;
            }
        }

        activeWarningLevel = strongestLevel;

        if (bannerObject == null)
            return;

        bannerObject.SetActive(strongestLevel > 0);
        if (strongestLevel == 0)
            return;


        string title = strongestLevel == 3
            ? "STRUCTURAL FAILURE"
            : strongestLevel == 2
                ? "CRITICAL COLLAPSE RISK"
                : "STRUCTURAL OVERLOAD";

        string status = strongestLevel == 3
            ? "COLLAPSE ACTIVE - UNITS AT RISK"
            : strongestLevel == 2
                ? "NEXT CHECK MAY TRIGGER COLLAPSE"
                : "STABILIZE THE HEX";

        string marginText =
            lowestMargin > 0
                ? "+" + lowestMargin
                : lowestMargin.ToString();

        bannerText.text =
            $"<b>{title}</b>  //  {selectedTile}\n" +
            $"MARGIN {marginText}  |  {status}";

        AlignThreatBanner();

    }

    private ThreatRing GetOrCreateRing(GameObject tile, HexCapacity hex)
    {
        if (rings.TryGetValue(hex, out ThreatRing existing) &&
            existing.Line != null)
            return existing;

        MeshFilter filter = tile.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return null;

        Vector3[] vertices = filter.sharedMesh.vertices;
        if (vertices.Length < 7)
            return null;

        // Sibling of the tile, NOT a child: collapse glitch geometry stays intact.
        GameObject ringObject = new GameObject("ThreatOutline_" + tile.name);
        ringObject.transform.SetParent(transform, false);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 6;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = outlineMaterial;

        // Reuse the actual hex mesh perimeter; no guessed radius/orientation.
        for (int i = 0; i < 6; i++)
        {
            Vector3 point = tile.transform.TransformPoint(vertices[i + 1]);
            line.SetPosition(i, point + Vector3.up * surfaceOffset);
        }

        ThreatRing ring = new ThreatRing { Line = line, Level = 1 };
        rings[hex] = ring;
        return ring;
    }

    private void AnimateWarnings()
    {
        float t = Time.unscaledTime;

        foreach (ThreatRing ring in rings.Values)
        {
            if (ring.Line == null || !ring.Line.enabled)
                continue;

            bool severe = ring.Level >= 2;
            float pulse = 0.5f + 0.5f *
                Mathf.Sin(t * (severe ? 11f : 5f));

            Color color = severe
                ? new Color(1f, 0.13f, 0.11f, Mathf.Lerp(0.55f, 1f, pulse))
                : new Color(1f, 0.53f, 0.12f, Mathf.Lerp(0.5f, 1f, pulse));

            ring.Line.startColor = color;
            ring.Line.endColor = color;
            ring.Line.widthMultiplier = outlineWidth *
                Mathf.Lerp(0.9f, severe ? 1.55f : 1.25f, pulse);
        }

        if (bannerObject == null || !bannerObject.activeSelf)
            return;

        bool urgent = activeWarningLevel >= 2;
        float bannerPulse = 0.5f + 0.5f *
            Mathf.Sin(t * (urgent ? 9f : 4f));

        bannerBorder.effectColor = urgent
            ? new Color(1f, 0.17f, 0.12f, Mathf.Lerp(0.55f, 1f, bannerPulse))
            : new Color(1f, 0.62f, 0.17f, Mathf.Lerp(0.45f, 1f, bannerPulse));

        bannerBackground.color = new Color(
            0.045f, 0.055f, 0.07f,
            Mathf.Lerp(0.83f, 0.96f, bannerPulse));

        bannerText.color = urgent
            ? new Color(1f, 0.46f, 0.37f)
            : new Color(1f, 0.8f, 0.38f);
    }

    private void CreateBanner()
    {
        GameObject canvasObject = new GameObject(
            "StructuralThreatCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));

        threatCanvasRect =
            canvasObject.GetComponent<RectTransform>();

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        bannerObject = new GameObject(
            "StructuralThreatBanner", typeof(RectTransform),
            typeof(Image), typeof(Outline));
        bannerObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = bannerObject.GetComponent<RectTransform>();
        threatBannerRect = rect;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -45f);
        rect.sizeDelta = new Vector2(560f, 95f);

        bannerBackground = bannerObject.GetComponent<Image>();
        bannerBackground.raycastTarget = false;
        bannerBackground.color = new Color(0.08f, 0.06f, 0.06f, 0.92f);

        bannerBorder = bannerObject.GetComponent<Outline>();
        bannerBorder.effectDistance = new Vector2(2f, -2f);


        GameObject textObject = new GameObject(
            "WarningText",
            typeof(RectTransform)
        );

        textObject.transform.SetParent(
            bannerObject.transform,
            false
        );

        // Explicitly create the TextMeshPro component.
        bannerText = textObject.AddComponent<TextMeshProUGUI>();

        if (bannerText == null)
        {
            Debug.LogError(
                "[STRUCTURAL THREAT] Failed to create warning text."
            );

            bannerObject.SetActive(false);
            return;
        }


        bannerText.fontSize = 18f;
        bannerText.enableAutoSizing = true;
        bannerText.fontSizeMin = 13f;
        bannerText.fontSizeMax = 18f;

        bannerText.alignment =
            TextAlignmentOptions.MidlineLeft;

        bannerText.enableWordWrapping = true;
        bannerText.overflowMode =
            TextOverflowModes.Overflow;

        bannerText.raycastTarget = false;
        bannerText.richText = true;

        // Fill the available banner area.
        RectTransform textRect =
            bannerText.rectTransform;

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        textRect.pivot =
            new Vector2(0.5f, 0.5f);

        textRect.offsetMin =
            new Vector2(20f, 8f);

        textRect.offsetMax =
            new Vector2(-20f, -8f);


    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
            Destroy(outlineMaterial);

        if (bannerObject != null)
            Destroy(bannerObject.transform.parent.gameObject);
    }



    private void AlignThreatBanner()
    {
        if (threatBannerRect == null ||
            threatCanvasRect == null)
            return;

        const float bannerHeight = 92f;
        const float gap = 8f;

        // Default position when the Scanner is closed.
        threatBannerRect.anchorMin = Vector2.one;
        threatBannerRect.anchorMax = Vector2.one;
        threatBannerRect.pivot = Vector2.one;

        threatBannerRect.sizeDelta =
            new Vector2(560f, bannerHeight);

        threatBannerRect.anchoredPosition =
            new Vector2(-28f, -35f);

        // Find existing Scanner panels.
        if (scannerPanelRect == null)
        {
            GameObject panel =
                GameObject.Find("TopologyAnalyzerPanel");

            if (panel != null)
                scannerPanelRect =
                    panel.GetComponent<RectTransform>();
        }

        if (scannerPopupRect == null)
        {
            GameObject popup =
                GameObject.Find("TopologyConceptPopup");

            if (popup != null)
                scannerPopupRect =
                    popup.GetComponent<RectTransform>();
        }

        RectTransform target = null;

        // Node inspection: follow the large PiP workspace.
        if (scannerPopupRect != null &&
            scannerPopupRect.gameObject.activeInHierarchy &&
            scannerPopupRect.rect.width >= 500f)
        {
            target = scannerPopupRect;
        }
        // Normal Analyzer: follow the main panel.
        else if (scannerPanelRect != null &&
                 scannerPanelRect.gameObject.activeInHierarchy)
        {
            target = scannerPanelRect;
        }

        if (target == null)
            return;

        // Read the actual screen-space panel boundaries.
        target.GetWorldCorners(popupCorners);

        Vector3 topLeft =
            threatCanvasRect.InverseTransformPoint(
                popupCorners[1]);

        Vector3 topRight =
            threatCanvasRect.InverseTransformPoint(
                popupCorners[2]);

        float width = topRight.x - topLeft.x;

        float bannerTop =
            topLeft.y + bannerHeight + gap;

        if (width <= 0f ||
            bannerTop > threatCanvasRect.rect.yMax - 12f)
            return;

        // Align both horizontal edges with the Scanner.
        threatBannerRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        threatBannerRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        threatBannerRect.pivot =
            new Vector2(0f, 1f);

        threatBannerRect.sizeDelta =
            new Vector2(width, bannerHeight);

        threatBannerRect.anchoredPosition =
            new Vector2(
                topLeft.x - threatCanvasRect.rect.center.x,
                bannerTop - threatCanvasRect.rect.center.y
            );
    }


}
