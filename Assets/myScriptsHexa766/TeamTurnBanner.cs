using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamTurnBanner : MonoBehaviour
{
    [Header("TIMING")]
    [SerializeField]
    private float fadeInDuration = 0.20f;

    [SerializeField]
    private float holdDuration = 0.65f;

    [SerializeField]
    private float fadeOutDuration = 0.35f;

    [Header("TEXT")]
    [SerializeField]
    private float fontSize = 42f;

    private TurnManager turnManager;

    private TMP_Text bannerText;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private int lastTeamID = -1;

    private Coroutine bannerRoutine;

    private void Start()
    {
        turnManager =
            FindFirstObjectByType<TurnManager>();

        CreateBanner();
    }

    private void Update()
    {
        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();

            return;
        }

        PlayerUnit currentUnit =
            turnManager.current;

        if (currentUnit == null)
            return;

        int currentTeamID =
            currentUnit.teamID;

        if (currentTeamID != lastTeamID)
        {
            lastTeamID =
                currentTeamID;

            ShowTeamBanner(
                currentTeamID
            );
        }
    }

    private void CreateBanner()
    {
        Canvas canvas =
            FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning(
                "[TEAM BANNER] No Canvas found."
            );

            return;
        }

        GameObject bannerObject =
            new GameObject(
                "TeamTurnBanner",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(TextMeshProUGUI)
            );

        bannerObject.transform.SetParent(
            canvas.transform,
            false
        );

        rectTransform =
            bannerObject.GetComponent<RectTransform>();

        rectTransform.anchorMin =
            new Vector2(0.5f, 1f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 1f);

        rectTransform.pivot =
            new Vector2(0.5f, 1f);

        rectTransform.anchoredPosition =
            new Vector2(0f, -90f);

        rectTransform.sizeDelta =
            new Vector2(500f, 80f);

        bannerText =
            bannerObject.GetComponent<TextMeshProUGUI>();

        // Make sure TMP does not tint the team color.
        bannerText.enableVertexGradient = false;

        if (bannerText.fontSharedMaterial != null)
        {
            bannerText.fontMaterial =
                new Material(
                    bannerText.fontSharedMaterial
                );

            bannerText.fontMaterial.SetColor(
                ShaderUtilities.ID_FaceColor,
                Color.white
            );
        }

        bannerText.fontSize =
            fontSize;

        bannerText.fontStyle =
            FontStyles.Bold;

        bannerText.alignment =
            TextAlignmentOptions.Center;

        bannerText.raycastTarget =
            false;

        canvasGroup =
            bannerObject.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        rectTransform.localScale =
            Vector3.one * 0.85f;
    }

    private void ShowTeamBanner(
        int teamID
    )
    {
        if (bannerText == null)
            return;

        if (teamID == 1)
        {
            bannerText.text =
                "BLUE TURN";

            bannerText.color =
                new Color32(
                    25,
                    90,
                    255,
                    255
                );
        }
        else if (teamID == 2)
        {
            bannerText.text =
                "RED TURN";

            bannerText.color =
                new Color32(
                    255,
                    55,
                    55,
                    255
                );
        }

        else
        {
            return;
        }

        if (bannerRoutine != null)
        {
            StopCoroutine(
                bannerRoutine
            );
        }

        bannerRoutine =
            StartCoroutine(
                BannerRoutine()
            );
    }

    private IEnumerator BannerRoutine()
    {
        float elapsed = 0f;

        // Fade + expand in.
        while (elapsed < fadeInDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeInDuration
                );

            canvasGroup.alpha = t;

            rectTransform.localScale =
                Vector3.one *
                Mathf.Lerp(
                    0.85f,
                    1.0f,
                    t
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale =
            Vector3.one;

        yield return new WaitForSecondsRealtime(
            holdDuration
        );

        elapsed = 0f;

        // Fade out.
        while (elapsed < fadeOutDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeOutDuration
                );

            canvasGroup.alpha =
                1f - t;

            yield return null;
        }

        canvasGroup.alpha = 0f;

        bannerRoutine = null;
    }
}