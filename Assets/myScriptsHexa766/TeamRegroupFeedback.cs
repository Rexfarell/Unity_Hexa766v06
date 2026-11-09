using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamRegroupFeedback : MonoBehaviour
{
    public static TeamRegroupFeedback Instance
    {
        get;
        private set;
    }

    [Header("AUDIO")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip regroupClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float regroupVolume = 0.8f;

    [Header("CAMERA")]
    [SerializeField]
    private CameraFollow cameraFollow;

    [SerializeField]
    private float cameraFocusDuration = 1.6f;
    [Header("NETWORK VISUAL")]
    [SerializeField]
    private float effectDuration = 1.0f;

    [SerializeField]
    private float lineWidth = 0.025f;

    [SerializeField]
    private float effectHeight = 0.35f;


    private void Awake()
    {
        Instance = this;

        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }
    }


    public void ShowTeamRegroup(
        int teamID,
        List<PlayerUnit> teamUnits,
        Vector3 centerWorldPosition
    )
    {

        if (cameraFollow != null)
        {
            cameraFollow.BeginTemporaryFocus(
                centerWorldPosition,
                cameraFocusDuration
            );
        }
        // -----------------------------------------
        // AUDIO
        // -----------------------------------------
        if (audioSource != null &&
            regroupClip != null)
        {
            audioSource.PlayOneShot(
                regroupClip,
                regroupVolume
            );
        }


        // -----------------------------------------
        // VISUAL
        // -----------------------------------------
        if (teamUnits != null &&
            teamUnits.Count > 0)
        {
            StartCoroutine(
                PlayNetworkEffect(
                    teamID,
                    teamUnits,
                    centerWorldPosition
                )
            );
        }
    }


    private IEnumerator PlayNetworkEffect(
        int teamID,
        List<PlayerUnit> teamUnits,
        Vector3 centerWorldPosition
    )
    {
        Color teamColor;

        if (teamID == 1)
        {
            // Extremely bright electric cyan
            teamColor =
                new Color(
                    0.2f,
                    4.0f,
                    6.0f,
                    1f
                );
        }
        else
        {
            // Extremely bright hot red
            teamColor =
                new Color(
                    6.0f,
                    0.25f,
                    0.08f,
                    1f
                );
        }


        List<LineRenderer> lines =
            new List<LineRenderer>();


        // Shared synchronization point
        Vector3 center =
            centerWorldPosition +
            Vector3.up * effectHeight;


        foreach (PlayerUnit unit in teamUnits)
        {
            if (unit == null)
                continue;

            GameObject lineObject =
                new GameObject(
                    "TeamRegroupLink"
                );

            lineObject.transform.SetParent(
                transform,
                false
            );


            LineRenderer line =
                lineObject.AddComponent<LineRenderer>();

            line.positionCount = 2;
            line.useWorldSpace = true;

            line.startWidth =
                lineWidth;

            line.endWidth =
                lineWidth;


            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default"
                    );
            }

            if (shader != null)
            {
                line.material =
                    new Material(shader);

                line.material.color =
                    teamColor;
            }


            line.startColor =
                teamColor;

            line.endColor =
                teamColor;


            line.SetPosition(
                0,
                center
            );

            line.SetPosition(
                1,
                unit.transform.position +
                Vector3.up * effectHeight
            );


            lines.Add(line);
        }


        // -----------------------------------------
        // BRIGHTEN / PULSE
        // -----------------------------------------
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            elapsed +=
                Time.deltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    effectDuration
                );


            // Quick energy surge followed by fade.
            float pulse =
                Mathf.Sin(
                    normalized *
                    Mathf.PI
                );


            float width =
                Mathf.Lerp(
                    lineWidth * 0.4f,
                    lineWidth * 1.8f,
                    pulse
                );


            float alpha =
                1f - normalized;


            Color fadedColor =
                new Color(
                    teamColor.r,
                    teamColor.g,
                    teamColor.b,
                    alpha
                );


            foreach (
                LineRenderer line
                in lines
            )
            {
                if (line == null)
                    continue;

                line.startWidth =
                    width;

                line.endWidth =
                    width;

                line.startColor =
                    fadedColor;

                line.endColor =
                    fadedColor;
            }


            yield return null;
        }


        // -----------------------------------------
        // CLEANUP
        // -----------------------------------------
        foreach (
            LineRenderer line
            in lines
        )
        {
            if (line != null)
            {
                Destroy(
                    line.gameObject
                );
            }
        }
    }
}