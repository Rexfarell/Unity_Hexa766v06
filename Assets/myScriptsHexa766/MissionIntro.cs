using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;

public class MissionIntro : MonoBehaviour

{

    #region ===== INTRO PANELS =====

    [Header("Logo Glitch Settings")]

    // Timing
    public float glitchDuration = 0.45f;
    public float catastrophicTime = 0.18f;

    // Normal RGB separation
    public float normalBlueMin = 8f;
    public float normalBlueMax = 16f;

    public float normalCyanMin = -16f;
    public float normalCyanMax = -8f;

    // Burst RGB separation
    public float burstBlueMin = 18f;
    public float burstBlueMax = 28f;

    public float burstCyanMin = -28f;
    public float burstCyanMax = -18f;

    // Catastrophic glitch
    public float catastrophicBlueOffset = 40f;
    public float catastrophicCyanOffset = -35f;

    // Music
    public float introMusicDelay = 0.85f;
    public float introMusicFadeTime = 3f;
    [Header("Intro Panels")]
    public GameObject logoPanel;
    public GameObject storyPanel;
    public GameObject missionPanel;
    public GameObject fadePanel;
    [Header("Typewriter")]
    public float typewriterSpeed = 0.025f;

    [Header("Logo Hologram")]

    public TMP_Text logoTitle;
    public TMP_Text logoBlue;
    public TMP_Text logoCyan;

    #endregion


    #region ===== UI =====

    [Header("UI")]
    public TMP_Text skipCaption;

    [Header("Story UI")]
    public TMP_Text storyTitle;
    public TMP_Text storyBody;

    #endregion


    #region ===== AUDIO =====

    [Header("Audio")]
    public AudioSource introMusic;



    #endregion


    #region ===== GAMEPLAY =====

    [Header("Gameplay")]
    public TurnManager turnManager;

    #endregion


    #region ===== CAMERA =====



    [Header("Camera")]
    public CameraFollow cameraFollow;

    [Header("Camera Targets")]
    public Transform overviewPoint;
    public Transform overviewTarget;

    public Transform pyramidPoint;
    public Transform aiStationPoint;
    public Transform player1Point;
    public Transform player2Point;

    #endregion


    #region ===== WORLD OBJECTS =====

    [Header("Scene References")]
    public Transform pyramid;
    public Transform aiStation;
    public Transform player1;
    public Transform player2;

    #endregion
    

    #region ===== TIMING =====

    [Header("Timing")]
    public float moveDuration = 2f;
    public float pauseDuration = 1.5f;

    #endregion


    #region ===== INTERNAL STATE =====

    #region ===== INTRO CONSTANTS =====

    private const float LogoDisplayTime = 2.5f;
    private const float StoryDisplayTime = 5.0f;
    private const float MissionDisplayTime = 3.5f;

    private const float PanelFadeDuration = 0.75f;

    private const float BlackScreenPause = 1.80f;

    #endregion

    private Coroutine introCoroutine;
    private bool introSkipped = false;
    private bool waitingForMissionConfirmation = false;

    #endregion

    void Start()
    {
        UnityEngine.Debug.Log("MISSION INTRO START " + GetInstanceID());
        CanvasGroup fade = fadePanel.GetComponent<CanvasGroup>();

        fade.alpha = 0f;

        fadePanel.SetActive(false);
       

        if (cameraFollow != null)
            cameraFollow.enabled = false;
        if (introMusic != null)
            introMusic.Play();

        introCoroutine = StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        Debug.Log("1 - Logo");
        yield return PlayLogo();

        Debug.Log("2 - Transition");
        yield return Transition();

        Debug.Log("3 - Camera");
        yield return MoveCamera(
            overviewPoint,
            overviewTarget,
            6f);

        Debug.Log("4 - Story");


        // ============================
        // STORY INTRO
        // ============================

        CanvasGroup storyGroup = GetGroup(storyPanel);

        storyPanel.SetActive(true);

        storyTitle.text = "RECOVERED ARCHIVE #766";

        yield return Fade(storyGroup, 0f, 1f, 0.35f);

        yield return TypeStory(
            "Loading historical records..."
        );

        yield return new WaitForSeconds(1.2f);

        yield return Fade(storyGroup, 1f, 0f, 0.35f);

        storyTitle.text = "YEAR 2268";

        yield return Fade(storyGroup, 0f, 1f, 0.35f);

        yield return TypeStory(
            "Humanity uncovers the remains\n" +
            "of an extraterrestrial civilization."
        );

        yield return new WaitForSeconds(1.4f);

        yield return Fade(storyGroup, 1f, 0f, 0.35f);

        storyTitle.text = "DISCOVERY";

        yield return Fade(storyGroup, 0f, 1f, 0.35f);

        yield return TypeStory(
            "Hidden beyond the\n" +
            "Holographic Cenotaph\n" +
            "lies something older than\n" +
            "recorded history..."
        );

        yield return new WaitForSeconds(1.5f);

        yield return Fade(storyGroup, 1f, 0f, 0.35f);

        storyTitle.text = "ARCHON";

        yield return Fade(storyGroup, 0f, 1f, 0.35f);

        yield return TypeStory(
            "The most advanced\n" +
            "non-human artificial intelligence\n" +
            "ever discovered."
        );

        yield return new WaitForSeconds(1.4f);

        yield return Fade(storyGroup, 1f, 0f, 0.35f);

        storyTitle.text = "ARCHIVE LOG";

        yield return Fade(storyGroup, 0f, 1f, 0.35f);

        yield return TypeStory(
            "Recovered records indicate\n" +
            "multiple expeditions converged\n" +
            "upon the monument shortly\n" +
            "after its discovery."
        );

        yield return new WaitForSeconds(1.6f);

        yield return Fade(storyGroup, 1f, 0f, 0.35f);

        //--------------------------------------------------
        // CARD 6
        //--------------------------------------------------

        storyTitle.text = "FINAL TRANSMISSION";

        yield return Fade(storyGroup, 0f, 1f, 0.35f);

        storyPanel.SetActive(false);

        yield return ShowMissionPanel();

        yield return new WaitForSeconds(pauseDuration);

        yield return OrbitToTarget(
            pyramidPoint,
            pyramid,
            4f,
            35f);

        yield return new WaitForSeconds(pauseDuration);

        yield return OrbitToTarget(
            aiStationPoint,
            aiStation,
            5f,
            25f);

        yield return new WaitForSeconds(pauseDuration);

        yield return OrbitToTarget(
            player1Point,
            player1,
            5f,
            12f);

        yield return new WaitForSeconds(1.5f);

        yield return OrbitToTarget(
            player2Point,
            player2,
            5f,
            12f);

        yield return new WaitForSeconds(0.5f);

        FinishIntro();

        yield return TypeStory(
            "\"Commander...\"\n\n" +
            "\"Restore ARCHON before\n" +
            "your rivals do.\""
        );

        yield return new WaitForSeconds(2.0f);

        yield return Fade(storyGroup, 1f, 0f, 0.35f);
    }

    IEnumerator PlayLogo()
    {
        Debug.Log("PLAY LOGO " + GetInstanceID());

        CanvasGroup g = GetGroup(logoPanel);

        logoPanel.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            logoPanel.GetComponent<RectTransform>());

        yield return null;

        // Start hidden
        g.alpha = 0f;

        logoBlue.alpha = 0f;
        logoCyan.alpha = 0f;

        //------------------------------------------------
        // Fade IN
        //------------------------------------------------

        yield return Fade(g, 0f, 1f, 0.75f);

        //------------------------------------------------
        // Hologram while visible
        //------------------------------------------------

        yield return StartCoroutine(HologramStabilize());

        //------------------------------------------------
        // Stay visible
        //------------------------------------------------

        yield return new WaitForSeconds(3f);

        //------------------------------------------------
        // Fade OUT
        //------------------------------------------------

        yield return Fade(g, 1f, 0f, 0.75f);

        logoPanel.SetActive(false);
    }

    IEnumerator FadeToBlack(float from, float to, float duration)
    {
        CanvasGroup cg = fadePanel.GetComponent<CanvasGroup>();

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }
    
    IEnumerator BlackPause(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
    IEnumerator MoveCamera(Transform viewPoint, Transform lookTarget, float duration)
    {
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        Vector3 endPos = viewPoint.position;
        Quaternion endRot = viewPoint.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float s = t * t * (3f - 2f * t);

            Camera.main.transform.position =
                Vector3.Lerp(startPos, endPos, s);

            Camera.main.transform.rotation =
                Quaternion.Slerp(startRot, endRot, s);

            elapsed += Time.deltaTime;

            yield return null;
        }

        Camera.main.transform.position = endPos;
        Camera.main.transform.rotation = endRot;
    }

    IEnumerator OrbitToTarget(Transform viewPoint, Transform lookTarget, float duration, float orbitAngle = 35f)
    {
        Debug.Log($"[Orbit] ViewPoint = {viewPoint.name}");
        Debug.Log($"[Orbit] Position = {viewPoint.position}");
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        Vector3 center = lookTarget.position;

        // Final camera position
        Vector3 endPos = viewPoint.position;

        // Build an arc around the target
        Vector3 startDir = (startPos - center).normalized;
        Vector3 endDir = (endPos - center).normalized;

        float startRadius = Vector3.Distance(startPos, center);
        float endRadius = Vector3.Distance(endPos, center);

        float elapsed = 0f;

        Debug.Log($"Angle = {Vector3.Angle(startDir, endDir)}");
        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Smooth ease in/out
            float s = t * t * (3f - 2f * t);

            Quaternion rot =
                Quaternion.AngleAxis(
                    Mathf.Lerp(orbitAngle, 0f, s),
                    Vector3.up);

            Vector3 dir =
                Vector3.Slerp(
                    rot * startDir,
                    endDir,
                    s);

            float radius =
                Mathf.Lerp(startRadius, endRadius, s);

            Camera.main.transform.position =
                center + dir * radius;

            Camera.main.transform.LookAt(lookTarget.position);

            elapsed += Time.deltaTime;

            yield return null;
        }

        Camera.main.transform.position = endPos;
        Camera.main.transform.LookAt(lookTarget.position);
    }

    void Update()
    {
        if (introSkipped || waitingForMissionConfirmation)
            return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Debug.Log("[INTRO] Skipped by player.");

            introSkipped = true;

            if (introCoroutine != null)
                StopCoroutine(introCoroutine);

            FinishIntro();
        }
    }


    IEnumerator TypeStory(string message)
    {
        storyBody.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            storyBody.text += message[i];

            yield return new WaitForSeconds(typewriterSpeed);
        }

        UnityEngine.Debug.Log("TYPEWRITER DONE");
    }

    void FinishIntro()
    {
        if (skipCaption != null)
            Destroy(skipCaption.gameObject);

        if (cameraFollow != null && player1 != null)
        {
            cameraFollow.SetTarget(player1);
            cameraFollow.enabled = true;
        }

        if (turnManager != null)
            turnManager.BeginMatch();

        StartCoroutine(FinishIntroCleanup());
    }


    IEnumerator FinishIntroCleanup()
    {
        if (introMusic != null)
            yield return StartCoroutine(FadeOutMusic(3f));

        Destroy(this);
    }

    IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = introMusic.volume;

        float t = 0f;

        while (t < duration)
        {
            introMusic.volume = Mathf.Lerp(startVolume, 0f, t / duration);

            t += Time.deltaTime;

            yield return null;
        }

        introMusic.volume = 0f;
        introMusic.Stop();

        // Restore volume for next time
        introMusic.volume = startVolume;
    }

    CanvasGroup GetGroup(GameObject panel)
    {
        return panel.GetComponent<CanvasGroup>();
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        group.alpha = from;

        float t = 0f;

        while (t < duration)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);

            t += Time.deltaTime;

            yield return null;
        }

        group.alpha = to;
    }

   

    IEnumerator FadeBlack(float from, float to)
    {
        CanvasGroup cg = fadePanel.GetComponent<CanvasGroup>();

        yield return Fade(cg, from, to, 0.75f);
    }

    IEnumerator Transition(float blackTime = 0.25f)
    {
        CanvasGroup fade = fadePanel.GetComponent<CanvasGroup>();

        fadePanel.SetActive(true);

        fade.alpha = 0f;

        yield return Fade(fade, 0f, 1f, 0.75f);

        yield return new WaitForSeconds(blackTime);

        yield return Fade(fade, 1f, 0f, 0.75f);

        fadePanel.SetActive(false);
    }

    IEnumerator ShowMissionPanel()
    {
        CanvasGroup mission = GetGroup(missionPanel);
        CanvasGroup fade = fadePanel.GetComponent<CanvasGroup>();

        missionPanel.SetActive(true);
        fadePanel.SetActive(true);

        waitingForMissionConfirmation = true;

        mission.alpha = 0f;
        fade.alpha = 0f;

        yield return Fade(fade, 0f, 0.80f, 0.5f);
        yield return Fade(mission, 0f, 1f, 0.5f);

        while (!Input.GetKeyDown(KeyCode.Return) &&
               !Input.GetKeyDown(KeyCode.KeypadEnter))
            yield return null;

        yield return Fade(mission, 1f, 0f, 0.5f);
        yield return Fade(fade, 0.45f, 0f, 0.5f);

        waitingForMissionConfirmation = false;

        missionPanel.SetActive(false);
        fadePanel.SetActive(false);
    }

    IEnumerator HologramStabilize()
    {
        Vector2 basePos = logoTitle.rectTransform.anchoredPosition;

        float timer = 0f;

        bool catastrophicDone = false;

        while (timer < 0.45f)
        {
            // ------------------------------------------
            // Decide if this frame is a BIG glitch burst
            // ------------------------------------------

            bool burst = Random.value > 0.78f;

            float blueX = burst ?
                Random.Range(18f, 28f) :
                Random.Range(8f, 16f);

            float cyanX = burst ?
                Random.Range(-28f, -18f) :
                Random.Range(-16f, -8f);

            float blueY = Random.Range(-2f, 2f);
            float cyanY = Random.Range(-2f, 2f);

            // ------------------------------------------
            // Catastrophic glitch (only once)
            // ------------------------------------------

            if (!catastrophicDone && timer > 0.18f)
            {
                catastrophicDone = true;

                logoBlue.rectTransform.anchoredPosition =
                    basePos + new Vector2(80f, 3f);

                logoCyan.rectTransform.anchoredPosition =
                    basePos + new Vector2(-35f, -2f);

                logoBlue.alpha = 1f;
                logoCyan.alpha = 1f;

                yield return null;

                yield return null;
            }
            else
            {
                logoBlue.rectTransform.anchoredPosition =
                    basePos + new Vector2(blueX, blueY);

                logoCyan.rectTransform.anchoredPosition =
                    basePos + new Vector2(cyanX, cyanY);
            }

            // ------------------------------------------
            // Randomly show / hide each color channel
            // ------------------------------------------

            logoBlue.alpha =
                (Random.value > 0.35f)
                    ? Random.Range(0.25f, 0.55f)
                    : 0f;

            logoCyan.alpha =
                (Random.value > 0.35f)
                    ? Random.Range(0.25f, 0.55f)
                    : 0f;

            // ------------------------------------------
            // Occasionally lock perfectly for one frame
            // ------------------------------------------

            if (Random.value > 0.90f)
            {
                logoBlue.alpha = 0f;
                logoCyan.alpha = 0f;

                logoBlue.rectTransform.anchoredPosition =
                    basePos + new Vector2(2f, 1f);

                logoCyan.rectTransform.anchoredPosition =
                    basePos + new Vector2(-2f, -1f);
            }

            timer += Time.deltaTime;

            yield return null;
        }

        // ------------------------------------------
        // Final perfectly aligned state
        // ------------------------------------------

        logoTitle.rectTransform.anchoredPosition = basePos;

        logoBlue.rectTransform.anchoredPosition =
            basePos + new Vector2(2f, 1f);

        logoCyan.rectTransform.anchoredPosition =
            basePos + new Vector2(-2f, -1f);

        logoBlue.alpha = 0f;
        logoCyan.alpha = 0f;
    }

    void SetTMPAlpha(TMP_Text txt, float a)
    {
        Color c = txt.color;
        c.a = a;
        txt.color = c;
    }
}