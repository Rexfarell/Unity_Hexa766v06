using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance { get; private set; }

    [Header("SCORE TEXT")]
    [SerializeField] private TMP_Text player1ScoreText;
    [SerializeField] private TMP_Text player2ScoreText;

    [Header("DELIVERY BANNER")]
    [SerializeField] private TMP_Text deliveryBannerText;
    [SerializeField] private CanvasGroup deliveryBannerGroup;
    [SerializeField] private float bannerDuration = 1.5f;

    [Header("VICTORY PANEL")]
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private CanvasGroup victoryPanelGroup;

    private int player1Score = 0;
    private int player2Score = 0;

    [Header("WIN CONDITION")]
    [SerializeField] private int scoreToWin = 3;

    public int ScoreToWin => scoreToWin;
    public int Player1Score => player1Score;
    public int Player2Score => player2Score;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshScoreUI();

        if (deliveryBannerGroup != null)
        {
            deliveryBannerGroup.alpha = 0f;
            deliveryBannerGroup.interactable = false;
            deliveryBannerGroup.blocksRaycasts = false;
        }

        if (victoryPanelGroup != null)
        {
            victoryPanelGroup.alpha = 0f;
            victoryPanelGroup.interactable = false;
            victoryPanelGroup.blocksRaycasts = false;
        }
    }

    public bool AddScore(int teamID, int amount)
    {
        Debug.Log(
            $"[SCORE UI] AddScore received | Team={teamID} | Amount={amount}"
        );

        int updatedScore;

        if (teamID == 1)
        {
            player1Score += amount;
            updatedScore = player1Score;

            Debug.Log(
                $"[SCORE UI] Player1 score = {player1Score}"
            );
        }
        else if (teamID == 2)
        {
            player2Score += amount;
            updatedScore = player2Score;

            Debug.Log(
                $"[SCORE UI] Player2 score = {player2Score}"
            );
        }
        else
        {
            Debug.LogError(
                $"[SCORE UI] INVALID TEAM ID = {teamID}"
            );

            return false;
        }

        RefreshScoreUI();
        ShowDeliveryBanner(teamID, amount);

        bool wonMatch =
            updatedScore >= scoreToWin;

        if (wonMatch)
        {
            Debug.Log(
                $"[MATCH WIN] Player {teamID} reached " +
                $"{updatedScore}/{scoreToWin} deliveries."
            );
        }

        return wonMatch;
    }

    private void RefreshScoreUI()
    {
        Debug.Log(
            $"[SCORE UI] RefreshScoreUI called | " +
            $"P1={player1Score} | P2={player2Score}"
        );

        if (player1ScoreText != null)
        {
            player1ScoreText.text =
                $"Blue: {player1Score}";

            player1ScoreText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError(
                "[SCORE UI] Player1ScoreText is NULL"
            );
        }

        if (player2ScoreText != null)
        {
            player2ScoreText.text =
                $"Red: {player2Score}";

            player2ScoreText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError(
                "[SCORE UI] Player2ScoreText is NULL"
            );
        }
    }

    private void ShowDeliveryBanner(int teamID, int amount)
    {
        string teamName =
     teamID == 1 ? "BLUE" : "RED";

        string msg =
            $"{teamName} DELIVERED\n+{amount}";

        Debug.Log(
            $"[DELIVERY BANNER] Showing: {msg}"
        );

        if (deliveryBannerText != null)
        {
            deliveryBannerText.text = msg;
            deliveryBannerText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError(
                "[DELIVERY BANNER] DeliveryBannerText is NULL."
            );
        }

        if (deliveryBannerGroup == null)
        {
            Debug.LogError(
                "[DELIVERY BANNER] DeliveryBannerGroup is NULL."
            );

            return;
        }

        deliveryBannerGroup.gameObject.SetActive(true);
        deliveryBannerGroup.alpha = 1f;
        deliveryBannerGroup.interactable = false;
        deliveryBannerGroup.blocksRaycasts = false;

        CancelInvoke(nameof(HideDeliveryBanner));
        Invoke(nameof(HideDeliveryBanner), bannerDuration);
    }

    private void HideDeliveryBanner()
    {
        if (deliveryBannerGroup != null)
            deliveryBannerGroup.alpha = 0f;

        Debug.Log(
            "[DELIVERY BANNER] Hidden."
        );
    }

    public void ShowVictory(int teamID)
    {
        Debug.Log(
            $"[VICTORY UI] Showing victory for Player {teamID}."
        );

        if (victoryText != null)
        {
            string teamName =
                teamID == 1 ? "BLUE" : "RED";

            victoryText.text =
                $"{teamName} VICTORIOUS";

            victoryText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError(
                "[VICTORY UI] VictoryText is NULL."
            );
        }

        if (victoryPanelGroup != null)
        {
            victoryPanelGroup.gameObject.SetActive(true);
            victoryPanelGroup.alpha = 1f;
            victoryPanelGroup.interactable = false;
            victoryPanelGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError(
                "[VICTORY UI] VictoryPanelGroup is NULL."
            );
        }
    }
}