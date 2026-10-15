using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ScreenTextureShakeController : MonoBehaviour
{
    private static readonly int OffsetId =
        Shader.PropertyToID("_Offset");

    private static readonly int ScaleId =
        Shader.PropertyToID("_Scale");

    private static readonly int RotationId =
        Shader.PropertyToID("_Rotation");

    [Header("Target Material")]
    [SerializeField]
    private Material screenTransformMaterial;

    [Header("Screen Transform")]
    [Tooltip("画面画像の移動量です。Xが左右、Yが上下です。")]
    [SerializeField]
    private Vector2 offset = Vector2.zero;

    [Tooltip("画面画像の拡大率です。通常状態は (1, 1) です。")]
    [SerializeField]
    private Vector2 scale = Vector2.one;

    [Tooltip("画面画像の回転角度です。単位は度です。")]
    [SerializeField]
    private float rotation;

    [Header("Safety")]
    [Tooltip("Scaleが0になってShader側で除算エラーになるのを防ぎます。")]
    [SerializeField, Min(0.0001f)]
    private float minimumScale = 0.0001f;

    public Vector2 Offset
    {
        get => offset;
        set
        {
            offset = value;
            Apply();
        }
    }

    public Vector2 Scale
    {
        get => scale;
        set
        {
            scale = ClampScale(value);
            Apply();
        }
    }

    public float Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            Apply();
        }
    }

    private void OnEnable()
    {
        ValidateValues();
        Apply();
    }

    private void OnValidate()
    {
        ValidateValues();
        Apply();
    }

    private void Update()
    {
        /*
         * TimelineはSerializeFieldを直接書き換えるため、
         * プロパティのsetterが呼ばれない場合があります。
         *
         * Updateで毎フレーム反映することで、
         * Edit ModeのTimelineプレビューにも対応します。
         */
        Apply();
    }

    private void OnDisable()
    {
        ResetMaterialValues();
    }

    private void ValidateValues()
    {
        minimumScale = Mathf.Max(0.0001f, minimumScale);
        scale = ClampScale(scale);
    }

    private Vector2 ClampScale(Vector2 value)
    {
        float safeMinimum = Mathf.Max(0.0001f, minimumScale);

        /*
         * 負のScaleは左右反転・上下反転として残します。
         * ただし0付近だけを回避します。
         */
        value.x = ClampScaleAxis(value.x, safeMinimum);
        value.y = ClampScaleAxis(value.y, safeMinimum);

        return value;
    }

    private static float ClampScaleAxis(float value, float minimum)
    {
        if (Mathf.Abs(value) >= minimum)
        {
            return value;
        }

        return value < 0f ? -minimum : minimum;
    }

    private void Apply()
    {
        if (screenTransformMaterial == null)
        {
            return;
        }

        screenTransformMaterial.SetVector(
            OffsetId,
            new Vector4(offset.x, offset.y, 0f, 0f)
        );

        screenTransformMaterial.SetVector(
            ScaleId,
            new Vector4(scale.x, scale.y, 0f, 0f)
        );

        screenTransformMaterial.SetFloat(
            RotationId,
            rotation
        );
    }

    private void ResetMaterialValues()
    {
        if (screenTransformMaterial == null)
        {
            return;
        }

        screenTransformMaterial.SetVector(
            OffsetId,
            new Vector4(0f, 0f, 0f, 0f)
        );

        screenTransformMaterial.SetVector(
            ScaleId,
            new Vector4(1f, 1f, 0f, 0f)
        );

        screenTransformMaterial.SetFloat(
            RotationId,
            0f
        );
    }

    [ContextMenu("Reset Screen Transform")]
    private void ResetScreenTransform()
    {
        offset = Vector2.zero;
        scale = Vector2.one;
        rotation = 0f;

        Apply();
    }

    [ContextMenu("Apply To Material")]
    private void ApplyToMaterial()
    {
        ValidateValues();
        Apply();
    }
}