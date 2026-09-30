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


    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        HideImmediate();
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