using System.Collections;
using TMPro;
using UnityEngine;

public class SubsystemFeedbackUI : MonoBehaviour
{
    public static SubsystemFeedbackUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField]
    private TMP_Text feedbackText;

    [SerializeField]
    private CanvasGroup feedbackGroup;

    [Header("TIMING")]
    [SerializeField]
    private float displayDuration = 1.8f;

    private Coroutine currentRoutine;

    public bool IsShowing =>
    currentRoutine != null;


    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        ConfigureFeedbackText();
        HideImmediate();
    }

    // Presentation only. The existing CanvasGroup controls timing and fade.
    private void ConfigureFeedbackText()
    {
        if (feedbackText == null)
            return;

        feedbackText.richText = true;
        feedbackText.fontSize = 21f;
        feedbackText.enableAutoSizing = true;
        feedbackText.fontSizeMin = 16f;
        feedbackText.fontSizeMax = 21f;
        feedbackText.enableWordWrapping = true;
        feedbackText.overflowMode = TextOverflowModes.Ellipsis;
        feedbackText.alignment = TextAlignmentOptions.TopLeft;
        feedbackText.raycastTarget = false;
    }


    public void ShowMessage(string message)
    {
        if (feedbackText == null ||
            feedbackGroup == null)
        {
            Debug.LogWarning(
                "[SUBSYSTEM FEEDBACK] UI references are missing."
            );

            return;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(
                currentRoutine
            );
        }

        currentRoutine =
            StartCoroutine(
                ShowRoutine(message)
            );
    }

    public void ShowHostileMessage(string message)
    {
        // Compact heading and content hierarchy; preserve the caller's data.
        string[] lines = (message ?? string.Empty)
            .Replace("\r", string.Empty)
            .Trim()
            .Split('\n');

        System.Text.StringBuilder content =
            new System.Text.StringBuilder();

        content.Append(
            "<size=80%><color=#FF9861><b>" +
            "HOSTILE INTERVENTION" +
            "</b></color></size>\n"
        );

        if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
        {
            string action = lines[0].Trim();

            if (action.StartsWith(
                "ENEMY ", System.StringComparison.OrdinalIgnoreCase))
            {
                action = action.Substring(6);
            }

            content.Append("<size=107%><color=#FFE9B2><b>");
            content.Append(action);
            content.Append("</b></color></size>");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string detail = lines[i].Trim();
            if (detail.Length == 0)
                continue;

            content.Append("\n<size=84%><color=#D6E4E8>");
            content.Append(detail);
            content.Append("</color></size>");
        }

        ShowMessage(content.ToString());
    }

    private IEnumerator ShowRoutine(
        string message
    )
    {
        feedbackText.text =
            message;

        feedbackGroup.alpha =
            1f;

        feedbackGroup.interactable =
            false;

        feedbackGroup.blocksRaycasts =
            false;

        yield return new WaitForSeconds(
            displayDuration
        );

        feedbackGroup.alpha =
            0f;

        currentRoutine =
            null;
    }


    private void HideImmediate()
    {
        if (feedbackGroup != null)
        {
            feedbackGroup.alpha =
                0f;

            feedbackGroup.interactable =
                false;

            feedbackGroup.blocksRaycasts =
                false;
        }
    }

    private void ShowHexSubsystemFeedback(
    string subsystemName,
    string changeDescription,
    HexCapacity target
)
    {
        if (SubsystemFeedbackUI.Instance == null ||
            target == null)
        {
            return;
        }

        int capacity =
            target.EffectiveCapacity;

        int load =
            target.CurrentLoad;

        string stateLine;

        if (load > capacity)
        {
            stateLine =
                $"OVERLOAD: {load} > {capacity}";
        }
        else if (load == capacity)
        {
            stateLine =
                $"FULL: {load} / {capacity}";
        }
        else
        {
            stateLine =
                $"SAFE: {load} / {capacity}";
        }

        string message =
            $"{subsystemName}\n" +
            $"{target.name}\n" +
            $"{changeDescription}\n" +
            $"Current Capacity: {capacity}\n" +
            $"Current Load: {load}\n" +
            $"{stateLine}";

        SubsystemFeedbackUI.Instance.ShowMessage(
            message
        );
    }

    [ContextMenu("TEST — Show Hostile Feedback")]
    private void DebugShowHostileFeedback()
    {
        ShowHostileMessage(
            "ENEMY STRUCTURAL REINFORCEMENT\n" +
            "HEX : OriginRed\n" +
            "CAPACITY : 12 → 15\n" +
            "LOAD : 12\n" +
            "STATE : SAFE"
        );
    }

    [ContextMenu("TEST — Show Subsystem Feedback")]
    private void DebugShowFeedback()
    {
        ShowMessage(
            "NETWORK STRESS\n" +
            "Tile21\n" +
            "Capacity reduced: -3\n" +
            "Current Capacity: 3\n" +
            "Current Load: 6\n" +
            "OVERLOAD: 6 > 3"
        );
    }
}