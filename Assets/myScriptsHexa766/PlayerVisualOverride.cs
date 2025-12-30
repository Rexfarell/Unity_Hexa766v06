using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class PlayerVisualOverride : MonoBehaviour
{
    [Header("ESCALA")]
    public float scale = 0.1f;

    [Header("ROTACIÓN FIJA")]
    public Vector3 rotation = Vector3.zero;

    [Header("COLOR DEL EQUIPO")]
    public Color teamColor = Color.white;
    public bool applyInEditor = true;

    private Renderer _renderer;
    private Transform _cachedTransform;

    private void Awake()
    {
        CacheComponents();
        ApplyAll();
    }

    private void OnEnable()
    {
        CacheComponents();
        ApplyAll();
    }

    private void Update()
    {
        if (Application.isPlaying)
            ApplyAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (applyInEditor && !Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) ApplyAll();
            };
        }
    }
#endif

    private void CacheComponents()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (_cachedTransform == null) _cachedTransform = transform;
    }

    private void ApplyAll()
    {
        ApplyScale();
        ApplyRotation();
        ApplyColor();
    }

    private void ApplyScale()
    {
        if (_cachedTransform != null)
            _cachedTransform.localScale = Vector3.one * scale;
    }

    private void ApplyRotation()
    {
        if (_cachedTransform != null)
            _cachedTransform.rotation = Quaternion.Euler(rotation);
    }

    private void ApplyColor()
    {
        if (_renderer == null) return;

        if (Application.isPlaying)
            _renderer.material.color = teamColor;
        else if (applyInEditor)
            _renderer.sharedMaterial.color = teamColor;
    }

    public void SetScale(float s) { scale = s; ApplyScale(); }
    public void SetRotation(Vector3 r) { rotation = r; ApplyRotation(); }
    public void SetColor(Color c) { teamColor = c; ApplyColor(); }
}