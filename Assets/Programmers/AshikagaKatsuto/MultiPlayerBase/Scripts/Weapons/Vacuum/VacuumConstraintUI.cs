using Game.Weapons.Vacuum;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Weapons
{
    public sealed class VacuumConstraintUI :
        MonoBehaviour
    {
        private static readonly int
            ShowParameter =
                Animator.StringToHash(
                    "Show"
                );

        private static readonly int
            HideParameter =
                Animator.StringToHash(
                    "Hide"
                );

        private static readonly int
            IsLockedParameter =
                Animator.StringToHash(
                    "IsLocked"
                );

        private static readonly int
            IntensityParameter =
                Animator.StringToHash(
                    "Intensity"
                );

        [Header("Source")]
        [Tooltip(
            "表示対象となる掃除機武器です。"
        )]
        [SerializeField]
        private VacuumWeapon vacuumWeapon;

        [Header("Canvas")]
        [Tooltip(
            "このUIが配置されているCanvasです。"
            + "未設定の場合は親から検索します。"
        )]
        [SerializeField]
        private Canvas targetCanvas;

        [Tooltip(
            "スクリーン座標をUI座標へ変換する際の"
            + "基準となるRectTransformです。"
            + "通常はCanvas直下のRectTransformです。"
        )]
        [SerializeField]
        private RectTransform canvasRect;

        [Header("UI References")]
        [Tooltip(
            "コードが位置と基準サイズを設定する"
            + "RectTransformです。"
            + "AnimatorでScaleを操作しないでください。"
        )]
        [SerializeField]
        private RectTransform
            positionAndSizeRoot;

        [Tooltip(
            "Animatorが拡大、縮小、回転などを行う"
            + "子オブジェクトです。"
        )]
        [SerializeField]
        private RectTransform animationRoot;

        [Tooltip(
            "用意した円形テクスチャを表示するImageです。"
        )]
        [SerializeField]
        private Image circleImage;

        [Tooltip(
            "円形UIをアニメーションさせるAnimatorです。"
        )]
        [SerializeField]
        private Animator animator;

        [Header("Display")]
        [Tooltip(
            "掃除機の制限範囲に対して、"
            + "UIの直径へ掛ける倍率です。"
            + "通常は1です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float diameterMultiplier =
            1.0f;

        [Tooltip(
            "敵の画面位置からUIをずらす量です。"
        )]
        [SerializeField]
        private Vector2 anchoredPositionOffset =
            Vector2.zero;

        [Tooltip(
            "Animatorへ渡すIntensityを"
            + "0から1の範囲へ制限します。"
        )]
        [SerializeField]
        private bool clampIntensity =
            true;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isVisible;

        [SerializeField]
        private Vector2 currentLocalPosition;

        [SerializeField]
        private float currentDiameter;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float currentIntensity;

        private bool eventsSubscribed;

        public bool IsVisible =>
            isVisible;

        public float CurrentDiameter =>
            currentDiameter;

        public float CurrentIntensity =>
            currentIntensity;

        private void Awake()
        {
            ResolveReferences();

            ApplyInitialState();
        }

        private void OnEnable()
        {
            ResolveReferences();

            SubscribeEvents();

            SynchronizeImmediateState();
        }

        private void LateUpdate()
        {
            if (
                vacuumWeapon == null
                ||
                !vacuumWeapon.IsTargetLocked
            )
            {
                UpdateAnimatorIntensity(
                    0.0f
                );

                return;
            }

            UpdatePositionAndSize();

            UpdateAnimatorIntensity(
                vacuumWeapon
                    .SpinEffectStrength
            );
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void OnValidate()
        {
            diameterMultiplier =
                Mathf.Max(
                    0.01f,
                    diameterMultiplier
                );

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (targetCanvas == null)
            {
                targetCanvas =
                    GetComponentInParent<Canvas>();
            }

            if (
                canvasRect == null
                &&
                targetCanvas != null
            )
            {
                canvasRect =
                    targetCanvas.transform
                    as RectTransform;
            }

            if (positionAndSizeRoot == null)
            {
                positionAndSizeRoot =
                    transform as RectTransform;
            }

            if (
                animationRoot == null
                &&
                positionAndSizeRoot != null
                &&
                positionAndSizeRoot.childCount > 0
            )
            {
                animationRoot =
                    positionAndSizeRoot
                        .GetChild(0)
                    as RectTransform;
            }

            if (
                circleImage == null
                &&
                animationRoot != null
            )
            {
                circleImage =
                    animationRoot
                        .GetComponentInChildren<Image>(
                            true
                        );
            }

            if (animator == null)
            {
                animator =
                    GetComponentInChildren<Animator>(
                        true
                    );
            }
        }

        private void ApplyInitialState()
        {
            isVisible =
                false;

            currentIntensity =
                0.0f;

            if (animator != null)
            {
                animator.SetBool(
                    IsLockedParameter,
                    false
                );

                animator.SetFloat(
                    IntensityParameter,
                    0.0f
                );
            }
        }

        private void SubscribeEvents()
        {
            if (
                eventsSubscribed
                ||
                vacuumWeapon == null
            )
            {
                return;
            }

            vacuumWeapon.TargetLocked +=
                HandleTargetLocked;

            vacuumWeapon.TargetReleased +=
                HandleTargetReleased;

            eventsSubscribed =
                true;
        }

        private void UnsubscribeEvents()
        {
            if (
                !eventsSubscribed
                ||
                vacuumWeapon == null
            )
            {
                return;
            }

            vacuumWeapon.TargetLocked -=
                HandleTargetLocked;

            vacuumWeapon.TargetReleased -=
                HandleTargetReleased;

            eventsSubscribed =
                false;
        }

        private void SynchronizeImmediateState()
        {
            if (vacuumWeapon == null)
            {
                return;
            }

            if (vacuumWeapon.IsTargetLocked)
            {
                ShowImmediate();
            }
            else
            {
                HideImmediate();
            }
        }

        private void HandleTargetLocked(
            Collider targetCollider
        )
        {
            Show();
        }

        private void HandleTargetReleased()
        {
            Hide();
        }

        private void Show()
        {
            isVisible =
                true;

            UpdatePositionAndSize();

            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(
                HideParameter
            );

            animator.SetBool(
                IsLockedParameter,
                true
            );

            animator.SetTrigger(
                ShowParameter
            );
        }

        private void Hide()
        {
            isVisible =
                false;

            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(
                ShowParameter
            );

            animator.SetBool(
                IsLockedParameter,
                false
            );

            animator.SetFloat(
                IntensityParameter,
                0.0f
            );

            animator.SetTrigger(
                HideParameter
            );
        }

        private void ShowImmediate()
        {
            isVisible =
                true;

            UpdatePositionAndSize();

            UpdateAnimatorIntensity(
                vacuumWeapon != null
                    ? vacuumWeapon
                        .SpinEffectStrength
                    : 0.0f
            );

            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(
                HideParameter
            );

            animator.SetBool(
                IsLockedParameter,
                true
            );
        }

        private void HideImmediate()
        {
            isVisible =
                false;

            currentIntensity =
                0.0f;

            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(
                ShowParameter
            );

            animator.SetBool(
                IsLockedParameter,
                false
            );

            animator.SetFloat(
                IntensityParameter,
                0.0f
            );
        }

        private void UpdatePositionAndSize()
        {
            if (
                vacuumWeapon == null
                ||
                positionAndSizeRoot == null
                ||
                canvasRect == null
            )
            {
                return;
            }

            Vector2 screenCenter =
                vacuumWeapon
                    .ConstraintCenterScreenPosition;

            if (
                !TryScreenToCanvasLocalPosition(
                    screenCenter,
                    out Vector2 localCenter
                )
            )
            {
                return;
            }

            currentLocalPosition =
                localCenter
                + anchoredPositionOffset;

            positionAndSizeRoot.anchoredPosition =
                currentLocalPosition;

            currentDiameter =
                CalculateLocalDiameter();

            positionAndSizeRoot.sizeDelta =
                new Vector2(
                    currentDiameter,
                    currentDiameter
                );
        }

        private bool
            TryScreenToCanvasLocalPosition(
                Vector2 screenPosition,
                out Vector2 localPosition
            )
        {
            localPosition =
                Vector2.zero;

            if (
                targetCanvas == null
                ||
                canvasRect == null
            )
            {
                return false;
            }

            Camera canvasCamera =
                targetCanvas.renderMode
                == RenderMode.ScreenSpaceOverlay
                    ? null
                    : targetCanvas.worldCamera;

            return RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    canvasCamera,
                    out localPosition
                );
        }

        private float CalculateLocalDiameter()
        {
            if (
                vacuumWeapon == null
                ||
                targetCanvas == null
            )
            {
                return 0.0f;
            }

            float screenDiameter =
                vacuumWeapon
                    .ConstraintRadiusScreen
                * 2.0f
                * diameterMultiplier;

            float scaleFactor =
                Mathf.Max(
                    0.0001f,
                    targetCanvas.scaleFactor
                );

            return screenDiameter
                / scaleFactor;
        }

        private void UpdateAnimatorIntensity(
            float value
        )
        {
            float intensity =
                clampIntensity
                    ? Mathf.Clamp01(value)
                    : value;

            currentIntensity =
                intensity;

            if (animator == null)
            {
                return;
            }

            animator.SetFloat(
                IntensityParameter,
                currentIntensity
            );
        }

        public void SetVacuumWeapon(
            VacuumWeapon newVacuumWeapon
        )
        {
            UnsubscribeEvents();

            vacuumWeapon =
                newVacuumWeapon;

            SubscribeEvents();

            SynchronizeImmediateState();
        }
    }
}