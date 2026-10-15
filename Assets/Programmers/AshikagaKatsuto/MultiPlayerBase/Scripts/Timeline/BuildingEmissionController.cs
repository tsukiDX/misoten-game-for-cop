using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BuildingEmissionController : MonoBehaviour
{
    private static readonly int EmissionColorId =
        Shader.PropertyToID("_EmissionColor");

    private static readonly int EmissionIntensityId =
        Shader.PropertyToID("_EmissionIntensity");

    [Header("Emission")]
    [SerializeField, ColorUsage(true, true)]
    private Color emissionColor = Color.white;

    [SerializeField, Min(0f)]
    private float emissionIntensity = 2f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    public Color EmissionColor
    {
        get => emissionColor;
        set
        {
            emissionColor = value;
            Apply();
        }
    }

    public float EmissionIntensity
    {
        get => emissionIntensity;
        set
        {
            emissionIntensity = Mathf.Max(0f, value);
            Apply();
        }
    }

    private void OnEnable()
    {
        Initialize();
        Apply();
    }

    private void OnValidate()
    {
        emissionIntensity = Mathf.Max(0f, emissionIntensity);

        Initialize();
        Apply();
    }

    private void Update()
    {
        // TimelineのEdit Modeプレビューを確実に反映するため
        Apply();
    }

    private void Initialize()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void Apply()
    {
        Initialize();

        if (spriteRenderer == null)
        {
            return;
        }

        // SpriteRendererが内部で使用している既存値を保持する
        spriteRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(
            EmissionColorId,
            emissionColor
        );

        propertyBlock.SetFloat(
            EmissionIntensityId,
            emissionIntensity
        );

        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    [ContextMenu("Clear Property Block")]
    private void ClearPropertyBlock()
    {
        Initialize();

        propertyBlock.Clear();
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}