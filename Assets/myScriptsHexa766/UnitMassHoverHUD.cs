
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitMassHoverHUD : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI massText;

    [Header("POSITION")]
    [SerializeField] private float verticalOffset = 18f;
    [SerializeField] private float fallbackHeadHeight = 1.2f;

    private CanvasGroup canvasGroup;
    private Camera gameplayCamera;

    private void Awake()
    {
        if (panel == null)
            panel = transform as RectTransform;

        if (uiCanvas == null)
            uiCanvas = GetComponentInParent<Canvas>();

        gameplayCamera = Camera.main;

        if (panel == null)
            return;

        canvasGroup = panel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // The information panel must not intercept clicks.
        Graphic[] graphics =
            panel.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        // Position relative to the Canvas center.
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0f);
    }

    private void LateUpdate()
    {
        if (canvasGroup == null ||
            uiCanvas == null ||
            massText == null)
        {
            return;
        }

        PlayerUnit unit =
            PlayerClickFeedback.HoveredPlayerUnit;

        if (unit == null || unit.IsDead)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gameplayCamera == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        // Find the top of the actual robot model.
        Vector3 headPosition =
            GetUnitHeadPosition(unit);

        Vector3 screenPosition =
            gameplayCamera.WorldToScreenPoint(headPosition);

        if (screenPosition.z <= 0f ||
            screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        // Display mass only. Unit names already exist
        // in the battlefield.
        massText.text =
            "<color=#8CAEB8>MASS</color>  " +
            $"<color=#61D8E8><b>{unit.EffectiveLoad}</b></color>";

        RectTransform canvasRect =
            uiCanvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        Camera uiCamera =
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : uiCanvas.worldCamera;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            panel.anchoredPosition =
                localPoint -
                canvasRect.rect.center +
                Vector2.up * verticalOffset;
        }

        canvasGroup.alpha = 1f;
    }

    private Vector3 GetUnitHeadPosition(PlayerUnit unit)
    {
        SkinnedMeshRenderer[] meshes =
            unit.GetComponentsInChildren<SkinnedMeshRenderer>();

        SkinnedMeshRenderer mainMesh = null;
        float largestSize = -1f;

        foreach (SkinnedMeshRenderer mesh in meshes)
        {
            if (mesh == null ||
                !mesh.enabled ||
                !mesh.gameObject.activeInHierarchy)
            {
                continue;
            }

            float size = mesh.bounds.size.sqrMagnitude;

            if (size > largestSize)
            {
                largestSize = size;
                mainMesh = mesh;
            }
        }

        if (mainMesh != null)
        {
            Bounds bounds = mainMesh.bounds;

            return new Vector3(
                bounds.center.x,
                bounds.max.y,
                bounds.center.z
            );
        }

        return unit.transform.position +
               Vector3.up * fallbackHeadHeight;
    }
}
