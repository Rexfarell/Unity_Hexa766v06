using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrosionFeedback : MonoBehaviour
{
    [Header("PYRAMID CHARGE")]
    [SerializeField]
    private float chargeDuration = 0.22f;

    [SerializeField]
    private float chargeLightHeight = 0.55f;

    [SerializeField]
    private float chargeLightIntensity = 7.5f;

    [SerializeField]
    private float chargeLightRange = 3.5f;

    [Header("NETWORK IMPULSE")]
    [SerializeField]
    private float travelDuration = 0.54f;

    [SerializeField]
    private float groundHeight = 0.065f;

    [SerializeField]
    private float whiteWidth = 0.045f;

    [SerializeField]
    private float ghostWidth = 0.025f;

    [SerializeField]
    private float channelSeparation = 0.045f;

    [SerializeField]
    private float catastrophicSeparation = 0.20f;

    [SerializeField]
    private int segmentPoints = 6;

    [SerializeField]
    private float segmentJitter = 0.045f;

    [Header("IMPACT")]
    [SerializeField]
    private float impactDuration = 0.20f;

    [SerializeField]
    private float impactHeight = 0.9f;

    [SerializeField]
    private float robotJitter = 0.035f;

    [Header("GLITCH FRAGMENTS")]
    [SerializeField]
    private int fragmentCount = 10;

    [SerializeField]
    private float fragmentRadius = 0.40f;

    [Header("NETWORK IMPULSE AUDIO")]

    [SerializeField]
    private AudioClip chargeClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float chargeVolume = 0.65f;

    [SerializeField]
    private AudioClip impactClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float impactVolume = 0.9f;

    private AudioSource networkImpulseAudioSource;

    private void Awake()
    {
        networkImpulseAudioSource =
            gameObject.AddComponent<AudioSource>();

        networkImpulseAudioSource.playOnAwake =
            false;

        networkImpulseAudioSource.loop =
            false;

        networkImpulseAudioSource.spatialBlend =
            0f;
    }

    private void PlayNetworkImpulseSound(
    AudioClip clip,
    float volume
)
    {
        if (networkImpulseAudioSource == null ||
            clip == null)
        {
            return;
        }

        networkImpulseAudioSource.PlayOneShot(
            clip,
            volume
        );
    }
    public void Play(
        int shieldLost,
        int energyLost
    )
    {
        if (shieldLost <= 0 &&
            energyLost <= 0)
        {
            return;
        }

        StartCoroutine(
            WaitForMovementThenAttack()
        );
    }


    private IEnumerator WaitForMovementThenAttack()
    {
        PlayerUnit unit =
            GetComponent<PlayerUnit>();

        // Give SmoothMove time to begin.
        yield return null;

        if (unit != null)
        {
            while (unit.IsMoving)
            {
                yield return null;
            }
        }

        yield return StartCoroutine(
            PyramidAttackRoutine()
        );
    }


    private IEnumerator PyramidAttackRoutine()
    {
        Map1HexGrid map =
            FindFirstObjectByType<Map1HexGrid>();

        if (map == null)
        {
            Debug.LogWarning(
                "[PYRAMID ATTACK] Map1HexGrid not found."
            );

            yield break;
        }

        // Resolve the player's ACTUAL
        // destination vertex.
        var targetLocation =
            map.GetPlayerTileAndVertex(
                gameObject
            );

        if (string.IsNullOrEmpty(
            targetLocation.tileName))
        {
            Debug.LogWarning(
                "[PYRAMID ATTACK] Could not resolve player destination."
            );

            yield break;
        }

        // =========================================
        // FIND BEST NETWORK ROUTE
        // FROM PYRAMID TO PLAYER
        // =========================================

        List<Vector3> bestPath = null;

        for (int startVertex = 0;
             startVertex < 6;
             startVertex++)
        {
            List<Vector3> candidate =
                map.GetShortestVertexPath(
                    "PyramidCenter",
                    startVertex,
                    targetLocation.tileName,
                    targetLocation.vertexIndex
                );

            if (candidate == null ||
                candidate.Count == 0)
            {
                continue;
            }

            if (bestPath == null ||
                candidate.Count <
                bestPath.Count)
            {
                bestPath =
                    candidate;
            }
        }

        if (bestPath == null ||
            bestPath.Count == 0)
        {
            Debug.LogWarning(
                "[PYRAMID ATTACK] No vertex-network route found."
            );

            yield break;
        }

        // =========================================
        // PYRAMID CENTER
        // =========================================

        Vector3 pyramidCenter;

        if (!map.TryGetTileCenterPosition(
            "PyramidCenter",
            out pyramidCenter))
        {
            Debug.LogWarning(
                "[PYRAMID ATTACK] PyramidCenter not found."
            );

            yield break;
        }

        // =========================================
        // SHARED MATERIAL
        // =========================================

        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );

        Material lineMaterial = null;

        if (shader != null)
        {
            lineMaterial =
                new Material(shader);

            lineMaterial.color =
                Color.white;
        }

        // =========================================
        // PYRAMID CHARGE
        // =========================================

        yield return StartCoroutine(
            PyramidCharge(
                pyramidCenter,
                bestPath[0],
                lineMaterial
            )
        );

        // =========================================
        // NETWORK IMPULSE
        // =========================================

        yield return StartCoroutine(
            TravelNetwork(
                bestPath,
                lineMaterial
            )
        );

        // =========================================
        // PLAYER IMPACT
        // =========================================

        yield return StartCoroutine(
            PlayerImpact(
                bestPath[
                    bestPath.Count - 1
                ],
                lineMaterial
            )
        );

        if (lineMaterial != null)
        {
            Destroy(
                lineMaterial
            );
        }
    }


    // =====================================================
    // PYRAMID CHARGE
    // =====================================================

    private IEnumerator PyramidCharge(
        Vector3 pyramidCenter,
        Vector3 launchVertex,
        Material material
    )
    {
        PlayNetworkImpulseSound(
    chargeClip,
    chargeVolume
);
        GameObject lightObject =
            new GameObject(
                "PyramidChargeLight"
            );

        lightObject.transform.position =
            pyramidCenter +
            Vector3.up *
            chargeLightHeight;

        Light chargeLight =
            lightObject.AddComponent<Light>();

        chargeLight.type =
            LightType.Point;

        chargeLight.color =
            new Color(
                0.15f,
                0.85f,
                1.0f
            );

        chargeLight.range =
            chargeLightRange;

        chargeLight.intensity = 0f;


        LineRenderer white =
            CreateChannel(
                "PyramidCharge_White",
                material,
                Color.white,
                whiteWidth
            );

        LineRenderer blue =
            CreateChannel(
                "PyramidCharge_Blue",
                material,
                new Color(
                    0.15f,
                    0.35f,
                    1f,
                    0.75f
                ),
                ghostWidth
            );

        LineRenderer cyan =
            CreateChannel(
                "PyramidCharge_Cyan",
                material,
                new Color(
                    0f,
                    1f,
                    1f,
                    0.70f
                ),
                ghostWidth
            );


        Vector3 start =
            pyramidCenter +
            Vector3.up *
            0.12f;

        Vector3 end =
            launchVertex +
            Vector3.up *
            groundHeight;


        float elapsed = 0f;

        while (elapsed <
               chargeDuration)
        {
            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    chargeDuration
                );

            float flicker =
                Random.Range(
                    0.55f,
                    1.15f
                );

            chargeLight.intensity =
                chargeLightIntensity *
                t *
                flicker;

            bool catastrophic =
                t > 0.62f &&
                t < 0.78f;

            float separation =
                catastrophic
                    ? catastrophicSeparation
                    : channelSeparation;

            DrawGlitchSegment(
                white,
                start,
                end,
                0f,
                1f
            );

            DrawGlitchSegment(
                blue,
                start,
                end,
                separation,
                0.75f
            );

            DrawGlitchSegment(
                cyan,
                start,
                end,
                -separation,
                0.75f
            );

            white.enabled =
                Random.value > 0.08f;

            blue.enabled =
                Random.value > 0.30f;

            cyan.enabled =
                Random.value > 0.30f;

            yield return null;
        }

        // Sudden discharge.
        chargeLight.intensity = 0f;

        Destroy(
            white.gameObject
        );

        Destroy(
            blue.gameObject
        );

        Destroy(
            cyan.gameObject
        );

        Destroy(
            lightObject
        );
    }


    // =====================================================
    // SIGNAL TRAVELS THROUGH HEX VERTEX NETWORK
    // =====================================================

    private IEnumerator TravelNetwork(
        List<Vector3> path,
        Material material
    )
    {
        if (path.Count < 2)
            yield break;


        LineRenderer white =
            CreateChannel(
                "Network_White",
                material,
                Color.white,
                whiteWidth
            );

        LineRenderer blue =
            CreateChannel(
                "Network_Blue",
                material,
                new Color(
                    0.12f,
                    0.35f,
                    1f,
                    0.78f
                ),
                ghostWidth
            );

        LineRenderer cyan =
            CreateChannel(
                "Network_Cyan",
                material,
                new Color(
                    0f,
                    1f,
                    1f,
                    0.72f
                ),
                ghostWidth
            );


        int edgeCount =
            path.Count - 1;

        float edgeDuration =
            travelDuration /
            Mathf.Max(
                1,
                edgeCount
            );

        int catastrophicEdge =
            Random.Range(
                0,
                edgeCount
            );


        for (int edge = 0;
             edge < edgeCount;
             edge++)
        {
            Vector3 start =
                path[edge] +
                Vector3.up *
                groundHeight;

            Vector3 end =
                path[edge + 1] +
                Vector3.up *
                groundHeight;

            float elapsed = 0f;

            while (elapsed <
                   edgeDuration)
            {
                elapsed +=
                    Time.deltaTime;

                bool catastrophic =
                    edge ==
                    catastrophicEdge &&
                    elapsed >
                    edgeDuration * 0.35f;

                float separation =
                    catastrophic
                        ? catastrophicSeparation
                        : channelSeparation;

                DrawGlitchSegment(
                    white,
                    start,
                    end,
                    0f,
                    1f
                );

                DrawGlitchSegment(
                    blue,
                    start,
                    end,
                    separation,
                    0.80f
                );

                DrawGlitchSegment(
                    cyan,
                    start,
                    end,
                    -separation,
                    0.75f
                );

                // Random channel failure,
                // like the intro title glitch.
                white.enabled =
                    Random.value > 0.04f;

                blue.enabled =
                    Random.value > 0.28f;

                cyan.enabled =
                    Random.value > 0.28f;

                // Occasional perfect
                // synchronization.
                if (Random.value > 0.91f)
                {
                    blue.enabled = false;
                    cyan.enabled = false;
                    white.enabled = true;
                }

                yield return null;
            }
        }

        Destroy(
            white.gameObject
        );

        Destroy(
            blue.gameObject
        );

        Destroy(
            cyan.gameObject
        );
    }


    // =====================================================
    // PLAYER IMPACT
    // =====================================================

    private IEnumerator PlayerImpact(
        Vector3 finalVertex,
        Material material
    )
    {
        Animator animator =
            GetComponentInChildren<
                Animator
            >(true);

        Transform jitterTarget = null;

        Vector3 originalLocalPosition =
            Vector3.zero;

        if (animator != null)
        {
            jitterTarget =
                animator.transform;

            originalLocalPosition =
                jitterTarget.localPosition;
        }


        LineRenderer white =
            CreateChannel(
                "Impact_White",
                material,
                Color.white,
                whiteWidth * 1.25f
            );

        LineRenderer blue =
            CreateChannel(
                "Impact_Blue",
                material,
                new Color(
                    0.12f,
                    0.30f,
                    1f,
                    0.85f
                ),
                ghostWidth
            );

        LineRenderer cyan =
            CreateChannel(
                "Impact_Cyan",
                material,
                new Color(
                    0f,
                    1f,
                    1f,
                    0.80f
                ),
                ghostWidth
            );


        List<GameObject> fragments =
            CreateFragments(
                material
            );


        float elapsed = 0f;

        while (elapsed <
               impactDuration)
        {
            elapsed +=
                Time.deltaTime;

            Vector3 start =
                finalVertex +
                Vector3.up *
                groundHeight;

            Vector3 target =
                transform.position +
                new Vector3(
                    Random.Range(
                        -0.10f,
                        0.10f
                    ),

                    impactHeight +
                    Random.Range(
                        -0.15f,
                        0.15f
                    ),

                    Random.Range(
                        -0.10f,
                        0.10f
                    )
                );


            bool catastrophic =
                elapsed >
                impactDuration * 0.30f &&
                elapsed <
                impactDuration * 0.55f;

            float separation =
                catastrophic
                    ? catastrophicSeparation
                    : channelSeparation;


            DrawGlitchSegment(
                white,
                start,
                target,
                0f,
                1.4f
            );

            DrawGlitchSegment(
                blue,
                start,
                target,
                separation,
                1.3f
            );

            DrawGlitchSegment(
                cyan,
                start,
                target,
                -separation,
                1.3f
            );


            white.enabled =
                Random.value > 0.08f;

            blue.enabled =
                Random.value > 0.20f;

            cyan.enabled =
                Random.value > 0.20f;


            if (jitterTarget != null)
            {
                jitterTarget.localPosition =
                    originalLocalPosition +
                    new Vector3(
                        Random.Range(
                            -robotJitter,
                            robotJitter
                        ),

                        Random.Range(
                            -robotJitter,
                            robotJitter
                        ) * 0.25f,

                        Random.Range(
                            -robotJitter,
                            robotJitter
                        )
                    );
            }


            UpdateFragments(
                fragments
            );

            yield return null;
        }


        if (jitterTarget != null)
        {
            jitterTarget.localPosition =
                originalLocalPosition;
        }


        Destroy(
            white.gameObject
        );

        Destroy(
            blue.gameObject
        );

        Destroy(
            cyan.gameObject
        );


        foreach (
            GameObject fragment
            in fragments
        )
        {
            if (fragment != null)
            {
                Destroy(
                    fragment
                );
            }
        }
    }


    // =====================================================
    // GLITCH LINE
    // =====================================================

    private LineRenderer CreateChannel(
        string objectName,
        Material material,
        Color color,
        float width
    )
    {
        GameObject obj =
            new GameObject(
                objectName
            );

        LineRenderer line =
            obj.AddComponent<
                LineRenderer
            >();

        line.useWorldSpace = true;

        line.positionCount =
            segmentPoints;

        line.startWidth =
            width;

        line.endWidth =
            width;

        line.numCornerVertices = 0;
        line.numCapVertices = 0;

        if (material != null)
        {
            line.material =
                material;
        }

        line.startColor =
            color;

        line.endColor =
            color;

        return line;
    }


    private void DrawGlitchSegment(
        LineRenderer line,
        Vector3 start,
        Vector3 end,
        float channelOffset,
        float jitterMultiplier
    )
    {
        Vector3 direction =
            end - start;

        Vector3 perpendicular =
            Vector3.Cross(
                Vector3.up,
                direction.normalized
            );

        start +=
            perpendicular *
            channelOffset;

        end +=
            perpendicular *
            channelOffset;


        for (int i = 0;
             i < segmentPoints;
             i++)
        {
            float t =
                (float)i /
                (segmentPoints - 1);

            Vector3 point =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );

            if (i > 0 &&
                i < segmentPoints - 1)
            {
                float jitter =
                    segmentJitter *
                    jitterMultiplier;

                point +=
                    new Vector3(
                        Random.Range(
                            -jitter,
                            jitter
                        ),

                        Random.Range(
                            -jitter * 0.20f,
                            jitter * 0.20f
                        ),

                        Random.Range(
                            -jitter,
                            jitter
                        )
                    );
            }

            line.SetPosition(
                i,
                point
            );
        }
    }


    // =====================================================
    // DIGITAL IMPACT FRAGMENTS
    // =====================================================

    private List<GameObject> CreateFragments(
        Material material
    )
    {
        List<GameObject> fragments =
            new List<GameObject>();

        for (int i = 0;
             i < fragmentCount;
             i++)
        {
            GameObject fragment =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            fragment.name =
                $"PyramidGlitchFragment_{i}";

            Collider collider =
                fragment.GetComponent<
                    Collider
                >();

            if (collider != null)
            {
                Destroy(
                    collider
                );
            }

            MeshRenderer renderer =
                fragment.GetComponent<
                    MeshRenderer
                >();

            if (renderer != null &&
                material != null)
            {
                renderer.material =
                    material;
            }

            fragments.Add(
                fragment
            );
        }

        return fragments;
    }


    private void UpdateFragments(
        List<GameObject> fragments
    )
    {
        for (int i = 0;
             i < fragments.Count;
             i++)
        {
            GameObject fragment =
                fragments[i];

            if (fragment == null)
                continue;


            fragment.SetActive(
                Random.value >
                0.35f
            );

            if (!fragment.activeSelf)
                continue;


            float angle =
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

            float radius =
                Random.Range(
                    0.08f,
                    fragmentRadius
                );

            fragment.transform.position =
                transform.position +
                new Vector3(
                    Mathf.Cos(angle) *
                    radius,

                    Random.Range(
                        0.20f,
                        1.10f
                    ),

                    Mathf.Sin(angle) *
                    radius
                );


            float width =
                Random.Range(
                    0.035f,
                    0.095f
                );

            fragment.transform.localScale =
                new Vector3(
                    width *
                    Random.Range(
                        2f,
                        4f
                    ),

                    width *
                    Random.Range(
                        0.20f,
                        0.50f
                    ),

                    width *
                    0.35f
                );


            fragment.transform.rotation =
                Quaternion.Euler(
                    0f,
                    Random.Range(
                        0f,
                        360f
                    ),
                    Random.Range(
                        -20f,
                        20f
                    )
                );
        }
    }
}