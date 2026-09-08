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

    private int player1Score = 0;
    private int player2Score = 0;

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
    }

    public void AddScore(int teamID, int amount)
    {
        Debug.Log(
            $"[SCORE UI] AddScore received | Team={teamID} | Amount={amount}"
        );

        if (teamID == 1)
        {
            player1Score += amount;

            Debug.Log(
                $"[SCORE UI] Player1 score = {player1Score}"
            );
        }
        else if (teamID == 2)
        {
            player2Score += amount;

            Debug.Log(
                $"[SCORE UI] Player2 score = {player2Score}"
            );
        }
        else
        {
            Debug.LogError(
                $"[SCORE UI] INVALID TEAM ID = {teamID}"
            );

            return;
        }

        RefreshScoreUI();
        ShowDeliveryBanner(teamID, amount);
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
                $"P1 Delivered: {player1Score}";

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
                $"P2 Delivered: {player2Score}";

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
        string msg =
            $"PLAYER {teamID} DELIVERED A BOX!\n+{amount}";

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
}