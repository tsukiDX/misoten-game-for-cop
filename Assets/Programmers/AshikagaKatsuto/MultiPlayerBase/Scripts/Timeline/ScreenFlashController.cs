using UnityEngine;

[ExecuteAlways]
public sealed class ScreenFlashController : MonoBehaviour
{
    private static readonly int IntensityId =
        Shader.PropertyToID("_Intensity");

    private static readonly int FlashColorId =
        Shader.PropertyToID("_FlashColor");

    [Header("Target")]
    [SerializeField]
    private Material flashMaterial;

    [Header("Parameters")]
    [SerializeField, Range(0f, 1f)]
    private float intensity;

    [SerializeField]
    private Color flashColor = Color.white;

    public float Intensity
    {
        get => intensity;
        set
        {
            intensity = Mathf.Clamp01(value);
            Apply();
        }
    }

    public Color FlashColor
    {
        get => flashColor;
        set
        {
            flashColor = value;
            Apply();
        }
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        intensity = Mathf.Clamp01(intensity);
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void OnDisable()
    {
        if (flashMaterial != null)
        {
            flashMaterial.SetFloat(IntensityId, 0f);
        }
    }

    private void Apply()
    {
        if (flashMaterial == null)
        {
            return;
        }

        flashMaterial.SetFloat(IntensityId, intensity);
        flashMaterial.SetColor(FlashColorId, flashColor);
    }
}
