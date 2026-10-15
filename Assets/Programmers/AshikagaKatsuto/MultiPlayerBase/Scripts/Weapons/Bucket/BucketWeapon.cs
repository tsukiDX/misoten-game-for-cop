using System;
using Game.Combat;
using Game.Feedback.Rumble;
using Game.Input.JSL;
using UnityEngine;

namespace Game.Weapons.Bucket
{
    public sealed class BucketWeapon :
        LocalPlayerWeapon
    {
        [Header("Definition")]
        [SerializeField]
        private BucketWeaponDefinition definition;

        [Header("Targeting")]
        [Tooltip(
            "ゲーム空間を描画しているMain Cameraを"
            + "設定してください。"
        )]
        [SerializeField]
        private Camera aimCamera;

        [Tooltip(
            "Boxの向きを決める基準です。"
            + "未設定の場合はAim Cameraを使います。"
        )]
        [SerializeField]
        private Transform attackRotationReference;

        [Tooltip(
    "波を生成する位置です。"
    + "未設定の場合はBucketWeaponの位置を使用します。"
)]
[SerializeField]
private Transform waveSpawnPoint;

        [Header("Feedback")]
        [Tooltip(
            "このプレイヤーのコントローラーへ"
            + "振動を出力するコンポーネントです。"
            + "未設定の場合は親から自動検索します。"
        )]
        [SerializeField]
        private ControllerRumblePlayer
            rumblePlayer;

        [Header("Runtime Water")]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float currentCharge;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float lockedAttackCharge;

        [SerializeField]
        private int availablePourCount;

        [SerializeField]
        private bool isWaterBatchLocked;

        [SerializeField]
        private bool isFillingWater;

        [SerializeField]
        private bool isWaterFull;

        [SerializeField]
        private bool isPouringWater;

        [Header("Runtime Input")]
        [SerializeField]
        private float chargeGravityValue;

        [SerializeField]
        private float swingValue;

        [SerializeField]
        private bool swingReady =
            true;

        [SerializeField]
        private float cooldownTimer;

        [Header("Runtime Targeting")]
        [SerializeField]
        private bool hasAimPosition;

        [SerializeField]
        private bool aimHitCollider;

        [SerializeField]
        private Vector3 currentAimPosition;

        [SerializeField]
        private Vector3 currentAttackCenter;

        [SerializeField]
        private Vector3 currentAttackSize;

        [Header("Runtime Damage")]

        [Header("Runtime Wave")]
[SerializeField]
private Vector3 currentWaveDirection =
    Vector3.forward;

[SerializeField]
private Vector3 currentWaveHitBoxSize =
    Vector3.one;

[SerializeField]
private Vector3 currentWaveVisualScale =
    Vector3.one;

[SerializeField]
private float currentWaveDamage;

[SerializeField]
private BucketWaveProjectile
    lastSpawnedWave;
        [SerializeField]

        private Quaternion currentAttackRotation =
            Quaternion.identity;

        private Ray currentAimRay;

        public event Action<float>
            WaterAmountChanged;

        public event Action<int, int>
            PourCountChanged;

        public event Action<bool>
            FillingWaterStateChanged;

        public event Action<bool>
            WaterFullStateChanged;

        public event Action<bool>
            PouringWaterStateChanged;

        public event Action
            WaterBecameFull;

        public event Action
            WaterPoured;

        public override WeaponDefinition Definition =>
            definition;

        public float CurrentCharge =>
            currentCharge;

        public float CurrentWaterAmount =>
            currentCharge;

        public float MaximumWaterAmount =>
            1.0f;

        public float NormalizedWaterAmount =>
            currentCharge;

        public int AvailablePourCount =>
            availablePourCount;

        public int MaximumPourCount =>
            definition != null
                ? definition.MaximumPourCount
                : 0;

        public bool HasWater =>
            availablePourCount > 0;

        public bool IsFillingWater =>
            isFillingWater;

        public bool IsWaterFull =>
            isWaterFull;

        public bool IsPouringWater =>
            isPouringWater;

        public bool IsWaterBatchLocked =>
            isWaterBatchLocked;

        public float CurrentAttackCharge =>
            isWaterBatchLocked
                ? lockedAttackCharge
                : GetPreviewAttackCharge();

        public float CurrentAttackRangeMultiplier =>
            CurrentAttackCharge;

        public Vector3 CurrentAimPosition =>
            currentAimPosition;

        private void Awake()
        {
            ResolveRumblePlayer();

            RefreshWaterState(
                false
            );

            RefreshAttackSize();
        }

        protected override void TickWeapon(
            float deltaTime,
            WeaponActivationState activation
        )
        {
            if (definition == null)
            {
                StopBucketRumble();

                SetFillingWater(
                    false
                );

                SetPouringWater(
                    false
                );

                return;
            }

            cooldownTimer =
                Mathf.Max(
                    0.0f,
                    cooldownTimer - deltaTime
                );

            UpdatePointerTarget();

            RefreshAttackSize();

            if (!activation.IsActive)
            {
                StopPreparationOnly();
                return;
            }

            UpdateCharge(
                deltaTime
            );

            RefreshAttackSize();

            UpdateSwingDetection();
        }

        protected override void OnInputUnavailable()
        {
            ResetAllState(
                true
            );
        }

        protected override void OnUnequipped()
        {
            ResetAllState(
                true
            );
        }

        private void OnDisable()
        {
            ResetAllState(
                true
            );
        }

        public void ResetWater()
        {
            SetWaterAmount(
                0.0f
            );

            lockedAttackCharge =
                0.0f;

            isWaterBatchLocked =
                false;

            SetAvailablePourCount(
                0
            );

            SetFillingWater(
                false
            );

            SetWaterFull(
                false
            );

            SetPouringWater(
                false
            );

            RefreshAttackSize();

            StopBucketRumble();
        }

        private void UpdatePointerTarget()
        {
            hasAimPosition =
                false;

            aimHitCollider =
                false;

            if (
                aimCamera == null
                ||
                GyroPointer == null
            )
            {
                return;
            }

            Vector2 screenPosition =
                GyroPointer
                    .GetPointerScreenPosition();

            currentAimRay =
                aimCamera.ScreenPointToRay(
                    screenPosition
                );

            if (definition.DrawAimRay)
            {
                Debug.DrawRay(
                    currentAimRay.origin,
                    currentAimRay.direction
                    * definition.MaximumAimDistance,
                    Color.yellow
                );
            }

            if (
                Physics.Raycast(
                    currentAimRay,
                    out RaycastHit hit,
                    definition.MaximumAimDistance,
                    definition.AimLayerMask,
                    definition
                        .AimQueryTriggerInteraction
                )
            )
            {
                currentAimPosition =
                    hit.point;

                aimHitCollider =
                    true;

                hasAimPosition =
                    true;
            }
            else
            {
                currentAimPosition =
                    currentAimRay.GetPoint(
                        definition.FallbackAimDistance
                    );

                aimHitCollider =
                    false;

                hasAimPosition =
                    true;
            }

            currentAttackRotation =
                CalculateAttackRotation();

            currentAttackCenter =
                currentAimPosition
                + currentAttackRotation
                * definition.PointerAttackOffset;
        }

        private Quaternion CalculateAttackRotation()
        {
            Transform rotationReference =
                attackRotationReference;

            if (
                rotationReference == null
                &&
                aimCamera != null
            )
            {
                rotationReference =
                    aimCamera.transform;
            }

            if (rotationReference == null)
            {
                return Quaternion.identity;
            }

            Vector3 horizontalForward =
                Vector3.ProjectOnPlane(
                    rotationReference.forward,
                    Vector3.up
                );

            if (
                horizontalForward.sqrMagnitude
                <= 0.0001f
            )
            {
                horizontalForward =
                    Vector3.forward;
            }

            return Quaternion.LookRotation(
                horizontalForward.normalized,
                Vector3.up
            );
        }

        private void UpdateCharge(
    float deltaTime
)
{
    JslInputSnapshot input =
        LocalPlayer.Input;

    chargeGravityValue =
        Vector3.Dot(
            input.Gravity,
            definition.ChargeGravityAxis
        );

    bool isTiltedForCharge =
        chargeGravityValue
        >= definition.ChargeTiltThreshold;

    bool canFill =
        isTiltedForCharge
        &&
        currentCharge < 1.0f;

    SetFillingWater(
        canFill
    );

    if (canFill)
    {
        float newCharge =
            Mathf.Clamp01(
                currentCharge
                + definition.ChargeSpeed
                * deltaTime
            );

        SetWaterAmount(
            newCharge
        );

        RefreshWaterBatchFromCurrentCharge();

        UpdateFillingRumble();

        return;
    }

    StopFillingRumble();

    if (
        isTiltedForCharge
        ||
        definition.ChargeDrainSpeed
        <= 0.0f
    )
    {
        return;
    }

    // 使用可能な水が残っている間は、
    // 給水角度から外れても自然減少させない。
    if (availablePourCount > 0)
    {
        return;
    }

    float drainedCharge =
        Mathf.Clamp01(
            currentCharge
            - definition.ChargeDrainSpeed
            * deltaTime
        );

    SetWaterAmount(
        drainedCharge
    );

    RefreshWaterBatchFromCurrentCharge();
}

        private void RefreshWaterBatchFromCurrentCharge()
{
    if (definition == null)
    {
        return;
    }

    int pourCount =
        definition.GetPourCount(
            currentCharge
        );

    SetAvailablePourCount(
        pourCount
    );

    if (pourCount <= 0)
    {
        isWaterBatchLocked =
            false;

        lockedAttackCharge =
            0.0f;

        RefreshAttackSize();

        return;
    }

    isWaterBatchLocked =
        true;

    lockedAttackCharge =
        definition
            .GetAttackChargeForPourCount(
                pourCount
            );

    RefreshAttackSize();
}
        private void UpdateSwingDetection()
        {
            Vector3 acceleration =
                LocalPlayer.Acceleration;

            swingValue =
                Vector3.Dot(
                    acceleration,
                    definition.UpwardSwingAxis
                );

            if (
                !swingReady
                &&
                Mathf.Abs(swingValue)
                <= definition.SwingResetThreshold
            )
            {
                swingReady =
                    true;
            }

            if (!swingReady)
            {
                return;
            }

            if (cooldownTimer > 0.0f)
            {
                return;
            }

            if (
                swingValue
                < definition.SwingThreshold
            )
            {
                return;
            }

            if (!hasAimPosition)
            {
                return;
            }

            if (!PrepareWaterBatchForAttack())
            {
                return;
            }

            swingReady =
                false;

            ExecuteWaterAttack();
        }

        private bool PrepareWaterBatchForAttack()
{
    if (definition == null)
    {
        return false;
    }

    RefreshWaterBatchFromCurrentCharge();

    if (availablePourCount <= 0)
    {
        return false;
    }

    SetFillingWater(
        false
    );

    StopFillingRumble();

    return true;
}

private void ExecuteWaterAttack()
{
    if (availablePourCount <= 0)
    {
        return;
    }

    if (
        definition == null
        ||
        definition.WaveProjectilePrefab
            == null
    )
    {
        Debug.LogError(
            "[Bucket Weapon] "
            + "Wave Projectile Prefabが"
            + "設定されていません。",
            this
        );

        return;
    }

    SetFillingWater(
        false
    );

    StopFillingRumble();

    SetPouringWater(
        true
    );

    float usedCharge =
        lockedAttackCharge;

    Transform spawnPoint =
        waveSpawnPoint != null
            ? waveSpawnPoint
            : transform;

    Vector3 spawnPosition =
        spawnPoint.position;

    Vector3 waveDirection =
        CalculateWaveDirection(
            spawnPosition
        );

    Quaternion waveRotation =
        Quaternion.LookRotation(
            waveDirection,
            Vector3.up
        );

    Vector3 visualScale =
        definition.GetWaveVisualScale(
            usedCharge
        );

    Vector3 hitBoxSize =
        definition.GetWaveHitBoxSize(
            usedCharge
        );

    float waveDamage =
        definition.GetWaveDamage(
            usedCharge
        );

    BucketWaveProjectile projectile =
        Instantiate(
            definition.WaveProjectilePrefab,
            spawnPosition,
            waveRotation
        );

    projectile.Initialize(
        this,
        waveDirection,
        definition.WaveSpeed,
        definition.WaveLifetime,
        waveDamage,
        hitBoxSize,
        visualScale,
        definition.WaveCollisionLayerMask,
        definition
            .WaveQueryTriggerInteraction,
        definition.KeepFixedWaveHeight,
        spawnPosition.y,
        definition.WaveGroundOffset
    );

    lastSpawnedWave =
        projectile;

    currentWaveDirection =
        waveDirection;

    currentWaveHitBoxSize =
        hitBoxSize;

    currentWaveVisualScale =
        visualScale;

    currentWaveDamage =
        waveDamage;

    currentAttackSize =
        hitBoxSize;

    ConsumeOnePour();

    WaterPoured?.Invoke();

    cooldownTimer =
        definition.AttackCooldown;

    SetPouringWater(
        false
    );

    if (definition.LogDamageDetection)
    {
        Debug.Log(
            $"[Bucket Wave Launch] "
            + $"Charge={usedCharge:F2}, "
            + $"Damage={waveDamage:F2}, "
            + $"HitBox={hitBoxSize}, "
            + $"VisualScale={visualScale}, "
            + $"Direction={waveDirection}"
        );
    }
}

private Vector3 CalculateWaveDirection(
    Vector3 spawnPosition
)
{
    Vector3 direction =
        currentAimPosition
        - spawnPosition;

    // 上下方向を除去する。
    // レティクルが空や敵の頭を指していても、
    // 波は水平に進む。
    direction =
        Vector3.ProjectOnPlane(
            direction,
            Vector3.up
        );

    if (
        direction.sqrMagnitude
        <= 0.0001f
    )
    {
        Transform directionReference =
            attackRotationReference;

        if (
            directionReference == null
            &&
            aimCamera != null
        )
        {
            directionReference =
                aimCamera.transform;
        }

        if (directionReference != null)
        {
            direction =
                Vector3.ProjectOnPlane(
                    directionReference.forward,
                    Vector3.up
                );
        }
    }

    if (
        direction.sqrMagnitude
        <= 0.0001f
    )
    {
        direction =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up
            );
    }

    if (
        direction.sqrMagnitude
        <= 0.0001f
    )
    {
        direction =
            Vector3.forward;
    }

    return direction.normalized;
}

        private void ConsumeOnePour()
        {
            int remainingCount =
                Mathf.Max(
                    0,
                    availablePourCount - 1
                );

            SetAvailablePourCount(
                remainingCount
            );

            float remainingWater =
                definition.MaximumPourCount > 0
                    ? remainingCount
                    / (float)definition
                        .MaximumPourCount
                    : 0.0f;

            SetWaterAmount(
                remainingWater
            );

            if (remainingCount > 0)
            {
                RefreshAttackSize();
                return;
            }

            isWaterBatchLocked =
                false;

            lockedAttackCharge =
                0.0f;

            SetWaterFull(
                false
            );

            RefreshAttackSize();
        }

        private void SetWaterAmount(
            float value
        )
        {
            float clampedValue =
                Mathf.Clamp01(
                    value
                );

            if (
                Mathf.Approximately(
                    currentCharge,
                    clampedValue
                )
            )
            {
                RefreshWaterState(
                    false
                );

                return;
            }

            currentCharge =
                clampedValue;

            WaterAmountChanged?.Invoke(
                currentCharge
            );

            RefreshWaterState(
                true
            );
        }

        private void UpdatePreviewPourCount()
{
    RefreshWaterBatchFromCurrentCharge();
}

        private void SetAvailablePourCount(
            int value
        )
        {
            int maximumCount =
                definition != null
                    ? definition.MaximumPourCount
                    : 0;

            int clampedValue =
                Mathf.Clamp(
                    value,
                    0,
                    maximumCount
                );

            if (
                availablePourCount
                == clampedValue
            )
            {
                return;
            }

            availablePourCount =
                clampedValue;

            PourCountChanged?.Invoke(
                availablePourCount,
                maximumCount
            );
        }

        private void RefreshWaterState(
            bool notifyBecameFull
        )
        {
            bool full =
                currentCharge
                >= 0.9999f;

            bool wasFull =
                isWaterFull;

            SetWaterFull(
                full
            );

            if (
                notifyBecameFull
                &&
                !wasFull
                &&
                isWaterFull
            )
            {
                WaterBecameFull?.Invoke();
            }

            if (isWaterFull)
            {
                SetFillingWater(
                    false
                );

                StopFillingRumble();
            }
        }

        private void SetFillingWater(
            bool value
        )
        {
            if (
                isFillingWater
                == value
            )
            {
                return;
            }

            isFillingWater =
                value;

            FillingWaterStateChanged?.Invoke(
                isFillingWater
            );

            if (!isFillingWater)
            {
                StopFillingRumble();
            }
        }

        private void SetWaterFull(
            bool value
        )
        {
            if (
                isWaterFull
                == value
            )
            {
                return;
            }

            isWaterFull =
                value;

            WaterFullStateChanged?.Invoke(
                isWaterFull
            );
        }

        private void SetPouringWater(
            bool value
        )
        {
            if (
                isPouringWater
                == value
            )
            {
                return;
            }

            isPouringWater =
                value;

            PouringWaterStateChanged?.Invoke(
                isPouringWater
            );
        }

        private float GetPreviewAttackCharge()
        {
            if (definition == null)
            {
                return 0.0f;
            }

            int previewCount =
                definition.GetPourCount(
                    currentCharge
                );

            return definition
                .GetAttackChargeForPourCount(
                    previewCount
                );
        }

        private void RefreshAttackSize()
{
    if (definition == null)
    {
        currentAttackSize =
            Vector3.zero;

        currentWaveHitBoxSize =
            Vector3.zero;

        currentWaveVisualScale =
            Vector3.zero;

        currentWaveDamage =
            0.0f;

        return;
    }

    float attackCharge =
        CurrentAttackCharge;

    currentWaveHitBoxSize =
        definition.GetWaveHitBoxSize(
            attackCharge
        );

    currentWaveVisualScale =
        definition.GetWaveVisualScale(
            attackCharge
        );

    currentWaveDamage =
        definition.GetWaveDamage(
            attackCharge
        );

    currentAttackSize =
        currentWaveHitBoxSize;
}


        private void UpdateFillingRumble()
        {
            if (
                !isFillingWater
                ||
                definition == null
                ||
                definition.FillingLoopRumble
                == null
            )
            {
                StopFillingRumble();
                return;
            }

            ResolveRumblePlayer();

            if (rumblePlayer == null)
            {
                return;
            }

            if (
                rumblePlayer.IsPlaying
                &&
                rumblePlayer.CurrentDefinition
                == definition.FillingLoopRumble
            )
            {
                return;
            }

            rumblePlayer.Play(
                definition.FillingLoopRumble,
                definition.FillingRumbleIntensity
            );
        }

        private void PlayDamageHitRumble()
        {
            if (
                definition == null
                ||
                definition.DamageHitRumble
                == null
            )
            {
                return;
            }

            ResolveRumblePlayer();

            if (rumblePlayer == null)
            {
                return;
            }

            rumblePlayer.Play(
                definition.DamageHitRumble,
                definition
                    .DamageHitRumbleIntensity
            );
        }

        private void StopFillingRumble()
        {
            if (
                rumblePlayer == null
                ||
                definition == null
                ||
                rumblePlayer.CurrentDefinition
                != definition.FillingLoopRumble
            )
            {
                return;
            }

            rumblePlayer.Stop();
        }

        private void StopBucketRumble()
        {
            if (
                rumblePlayer == null
                ||
                definition == null
            )
            {
                return;
            }

            ControllerRumbleDefinition
                currentDefinition =
                    rumblePlayer
                        .CurrentDefinition;

            bool isBucketRumble =
                currentDefinition
                == definition.FillingLoopRumble
                ||
                currentDefinition
                == definition.DamageHitRumble;

            if (isBucketRumble)
            {
                rumblePlayer.Stop();
            }
        }

        private void ResolveRumblePlayer()
        {
            if (rumblePlayer != null)
            {
                return;
            }

            rumblePlayer =
                GetComponentInParent<
                    ControllerRumblePlayer
                >();
        }

        private void StopPreparationOnly()
        {
            chargeGravityValue =
                0.0f;

            swingValue =
                0.0f;

            swingReady =
                true;

            SetFillingWater(
                false
            );

            SetPouringWater(
                false
            );

            StopFillingRumble();
        }

        private void ResetAllState(
            bool resetWater
        )
        {
            StopBucketRumble();

            chargeGravityValue =
                0.0f;

            swingValue =
                0.0f;

            swingReady =
                true;

            cooldownTimer =
                0.0f;

            SetFillingWater(
                false
            );

            SetPouringWater(
                false
            );

            if (resetWater)
            {
                currentCharge =
                    0.0f;

                lockedAttackCharge =
                    0.0f;

                availablePourCount =
                    0;

                isWaterBatchLocked =
                    false;

                SetWaterFull(
                    false
                );

                WaterAmountChanged?.Invoke(
                    currentCharge
                );

                PourCountChanged?.Invoke(
                    availablePourCount,
                    definition != null
                        ? definition.MaximumPourCount
                        : 0
                );
            }

            RefreshAttackSize();
        }

private void OnDrawGizmosSelected()
{
    if (
        definition == null
        ||
        !definition.DrawAttackArea
    )
    {
        return;
    }

    Transform spawnPoint =
        waveSpawnPoint != null
            ? waveSpawnPoint
            : transform;

    Vector3 previewPosition =
        spawnPoint.position;

    Vector3 previewDirection;

    if (
        Application.isPlaying
        &&
        hasAimPosition
    )
    {
        previewDirection =
            CalculateWaveDirection(
                previewPosition
            );
    }
    else
    {
        previewDirection =
            Vector3.ProjectOnPlane(
                spawnPoint.forward,
                Vector3.up
            );

        if (
            previewDirection.sqrMagnitude
            <= 0.0001f
        )
        {
            previewDirection =
                Vector3.forward;
        }

        previewDirection.Normalize();
    }

    Quaternion previewRotation =
        Quaternion.LookRotation(
            previewDirection,
            Vector3.up
        );

    Vector3 previewSize =
        definition.GetWaveHitBoxSize(
            CurrentAttackCharge
        );

    if (
        definition.KeepFixedWaveHeight
    )
    {
        previewPosition.y +=
            definition.WaveGroundOffset;
    }

    Matrix4x4 previousMatrix =
        Gizmos.matrix;

    Color previousColor =
        Gizmos.color;

    Gizmos.matrix =
        Matrix4x4.TRS(
            previewPosition,
            previewRotation,
            Vector3.one
        );

    Gizmos.color =
        new Color(
            0.0f,
            0.8f,
            1.0f,
            0.9f
        );

    Gizmos.DrawWireCube(
        Vector3.zero,
        previewSize
    );

    Gizmos.color =
        Color.magenta;

    Gizmos.DrawLine(
        Vector3.zero,
        Vector3.forward * 2.0f
    );

    Gizmos.matrix =
        previousMatrix;

    Gizmos.color =
        previousColor;
}
        public bool TryApplyWaveDamage(
    IDamageable damageable,
    Collider hitCollider,
    Vector3 hitPosition,
    Vector3 hitDirection,
    float waveDamage
)
{
    if (
        definition == null
        ||
        damageable == null
        ||
        !damageable.IsAlive
    )
    {
        return false;
    }

    Vector3 normalizedDirection =
        hitDirection.sqrMagnitude
        > 0.0001f
            ? hitDirection.normalized
            : transform.forward;

    DamageInfo damageInfo =
        new DamageInfo(
            Mathf.Max(
                0.0f,
                waveDamage
            ),
            hitPosition,
            normalizedDirection,
            LocalPlayer,
            definition
        );

    bool damageApplied =
        TryApplyDamage(
            damageable,
            damageInfo
        );

    if (damageApplied)
    {
        PlayDamageHitRumble();
    }

    if (definition.LogDamageDetection)
    {
        string targetName =
            hitCollider != null
                ? hitCollider.name
                : "Unknown";

        Debug.Log(
            $"[Bucket Wave Hit] "
            + $"Target={targetName}, "
            + $"Damage={waveDamage:F2}, "
            + $"Applied={damageApplied}"
        );
    }

    return damageApplied;
}
    }
}