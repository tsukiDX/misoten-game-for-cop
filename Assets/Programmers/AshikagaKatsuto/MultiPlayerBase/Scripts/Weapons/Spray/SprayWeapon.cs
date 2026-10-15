using System;
using Game.Combat;
using Game.Feedback.Rumble;
using UnityEngine;

namespace Game.Weapons.Spray
{
    public sealed class SprayWeapon :
        LocalPlayerWeapon
    {
        [Header("Definition")]
        [SerializeField]
        private SprayWeaponDefinition definition;

        [Header("Targeting")]
        [Tooltip(
            "敵を描画しているゲーム用カメラを"
            + "設定してください。"
        )]
        [SerializeField]
        private Camera aimCamera;

        [Tooltip(
            "弾を生成する位置です。"
            + "未設定の場合はこのGameObjectの位置を使います。"
        )]
        [SerializeField]
        private Transform muzzle;

        [Header("Feedback")]
        [Tooltip(
            "このプレイヤーのコントローラーへ"
            + "振動を出力するコンポーネントです。"
            + "未設定の場合は親から自動検索します。"
        )]
        [SerializeField]
        private ControllerRumblePlayer
            rumblePlayer;

        [Header("Runtime Charge")]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float currentCharge;

        [SerializeField]
        [Range(0, 3)]
        private int currentChargeLevel;

        [SerializeField]
        private bool isChargeFull;

        [SerializeField]
        private bool isShaking;

        [SerializeField]
        private bool isCharging;

        [SerializeField]
        private bool isHoldingCharge;

        [SerializeField]
        private float shakingTimer;

        [Header("Runtime Shake")]
        [SerializeField]
        private float shakeValue;

        [SerializeField]
        private bool shakeReady =
            true;

        [Header("Runtime Firing")]
        [SerializeField]
        private bool wasActivationActive;

        [SerializeField]
        private bool isReleaseBurstActive;

        [SerializeField]
        private bool isFiring;

        [SerializeField]
        private float projectileTimer;

        [SerializeField]
        private int firedProjectileCount;

        [Header("Runtime Aim")]
        [SerializeField]
        private Vector2 pointerScreenPosition;

        [SerializeField]
        private Vector3 currentAimPosition;

        [SerializeField]
        private bool aimHitCollider;

        private readonly RaycastHit[]
    aimAssistHitBuffer =
        new RaycastHit[
            AimAssistHitBufferSize
        ];

        private Ray currentAimRay;

        private SprayBeamDamageArea
    activeBeam;

        private const int AimAssistHitBufferSize =
    16;

        private ControllerRumbleDefinition
            requestedLoopRumble;

        private bool requestedRumbleStarted;

        public event Action<float>
            ChargeChanged;

        public event Action<int>
            ChargeLevelChanged;

        public event Action<int>
            ChargeLevelIncreased;

        public event Action<bool>
            ShakingStateChanged;

        public event Action<bool>
            ChargingStateChanged;

        public event Action<bool>
            HoldingChargeStateChanged;

        public event Action<bool>
            FiringStateChanged;

        public event Action<bool>
            ChargeFullStateChanged;

        public event Action
            ShotFired;

        public override WeaponDefinition Definition =>
            definition;

        public float CurrentCharge =>
            currentCharge;

        public float NormalizedCharge =>
            currentCharge;

        public int CurrentChargeLevel =>
            currentChargeLevel;

        public int MaximumChargeLevel =>
            definition != null
                ? definition.MaximumChargeLevel
                : 3;

        public bool HasCharge =>
            currentCharge > 0.0001f;

        public bool IsShaking =>
            isShaking;

        public bool IsCharging =>
            isCharging;

        public bool IsHoldingCharge =>
            isHoldingCharge;

        public bool IsFiring =>
            isFiring;

        public bool IsBurstFiring =>
            isFiring;

        public bool IsChargeFull =>
            isChargeFull;

        public Vector3 CurrentAimPosition =>
            currentAimPosition;

        public SprayTriggerMode TriggerMode =>
            definition != null
                ? definition.TriggerMode
                : SprayTriggerMode.FireWhileHeld;

        public bool IsReleaseBurstActive =>
            isReleaseBurstActive;

            public bool IsBeamActive =>
    activeBeam != null
    &&
    activeBeam.IsBeamActive;

public int BeamTargetCount =>
    activeBeam != null
        ? activeBeam.ActiveTargetCount
        : 0;

        private void Awake()
        {
            ResolveRumblePlayer();

            RefreshChargeState(
                false
            );
        }

        protected override void TickWeapon(
            float deltaTime,
            WeaponActivationState activation
        )
        {
            if (definition == null)
            {
                ResetWeaponState();
                return;
            }

            UpdateAim();

            bool activationActive =
                activation.IsActive;

            bool releasedThisFrame =
                !activationActive
                &&
                wasActivationActive;

            switch (definition.TriggerMode)
            {
                case SprayTriggerMode.FireOnRelease:
                    UpdateReleaseTriggerMode(
                        deltaTime,
                        activationActive,
                        releasedThisFrame
                    );
                    break;

                case SprayTriggerMode.FireWhileHeld:
                default:
                    UpdateHeldTriggerMode(
                        deltaTime,
                        activationActive
                    );
                    break;
            }

            UpdateHoldingState();

            UpdateRumble();

            wasActivationActive =
                activationActive;
        }

        protected override void OnInputUnavailable()
        {
            ResetWeaponState();
        }

        protected override void OnUnequipped()
        {
            ResetWeaponState();
        }

        private void OnDisable()
        {
            ResetWeaponState();
        }

        private void UpdateHeldTriggerMode(
            float deltaTime,
            bool activationActive
        )
        {
            isReleaseBurstActive =
                false;

            if (activationActive)
            {
                UpdateFiring(
                    deltaTime
                );

                return;
            }

            StopFiring();

            UpdateShakeCharge(
                deltaTime
            );
        }

        private void UpdateReleaseTriggerMode(
            float deltaTime,
            bool activationActive,
            bool releasedThisFrame
        )
        {
            if (isReleaseBurstActive)
            {
                UpdateFiring(
                    deltaTime
                );

                if (currentCharge <= 0.0001f)
                {
                    FinishReleaseBurst();
                }

                return;
            }

            if (releasedThisFrame)
            {
                StopShakeChargeState();

                TryStartReleaseBurst();

                if (isReleaseBurstActive)
                {
                    UpdateFiring(
                        deltaTime
                    );
                }
                else
                {
                    StopFiring();
                }

                return;
            }

            if (activationActive)
            {
                StopFiring();

                UpdateShakeCharge(
                    deltaTime
                );

                return;
            }

            StopFiring();

            StopShakeChargeState();
        }

        private void TryStartReleaseBurst()
        {
            if (
                definition == null
                ||
                currentCharge <= 0.0001f
            )
            {
                SetCharge(
                    0.0f,
                    false
                );

                isReleaseBurstActive =
                    false;

                return;
            }

            isReleaseBurstActive =
                true;

            projectileTimer =
                0.0f;

            if (definition.LogFiring)
            {
                Debug.Log(
                    $"[Spray Release Burst] "
                    + $"Started, "
                    + $"Charge={currentCharge:F2}, "
                    + $"Level={currentChargeLevel}"
                );
            }
        }

        private void FinishReleaseBurst()
        {
            isReleaseBurstActive =
                false;

            StopFiring();

            if (
                definition != null
                &&
                definition.LogFiring
            )
            {
                Debug.Log(
                    "[Spray Release Burst] Finished"
                );
            }
        }

        private void UpdateAim()
{
    if (
        aimCamera == null
        ||
        GyroPointer == null
        ||
        definition == null
    )
    {
        return;
    }

    pointerScreenPosition =
        GyroPointer
            .GetPointerScreenPosition();

    currentAimRay =
        aimCamera.ScreenPointToRay(
            pointerScreenPosition
        );

    if (
        TryFindEnemyAimPoint(
            out Vector3 enemyAimPosition
        )
    )
    {
        currentAimPosition =
            enemyAimPosition;

        aimHitCollider =
            true;
    }
    else if (
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
    }
    else
    {
        currentAimPosition =
            currentAimRay.GetPoint(
                definition.FallbackAimDistance
            );

        aimHitCollider =
            false;
    }

    if (definition.DrawAimRay)
    {
        Debug.DrawLine(
            currentAimRay.origin,
            currentAimPosition,
            aimHitCollider
                ? Color.cyan
                : Color.yellow
        );
    }
}
        private void UpdateShakeCharge(
            float deltaTime
        )
        {
            if (currentCharge >= 1.0f)
            {
                shakeValue =
                    0.0f;

                shakingTimer =
                    0.0f;

                SetShaking(
                    false
                );

                SetCharging(
                    false
                );

                return;
            }

            Vector3 acceleration =
                LocalPlayer.Acceleration;

            shakeValue =
                Vector3.Dot(
                    acceleration,
                    definition.ShakeAxis
                );

            float absoluteShake =
                Mathf.Abs(
                    shakeValue
                );

            if (
                !shakeReady
                &&
                absoluteShake
                <= definition.ShakeResetThreshold
            )
            {
                shakeReady =
                    true;
            }

            bool shakeDetected =
                shakeReady
                &&
                absoluteShake
                >= definition.ShakeThreshold;

            if (shakeDetected)
            {
                shakeReady =
                    false;

                shakingTimer =
                    definition.ShakingStateDuration;

                SetShaking(
                    true
                );

                float previousCharge =
                    currentCharge;

                SetCharge(
                    currentCharge
                    + definition.ChargePerShake,
                    true
                );

                SetCharging(
                    currentCharge
                    > previousCharge
                );
            }
            else
            {
                SetCharging(
                    false
                );
            }

            if (shakingTimer > 0.0f)
            {
                shakingTimer =
                    Mathf.Max(
                        0.0f,
                        shakingTimer - deltaTime
                    );
            }

            if (shakingTimer <= 0.0f)
            {
                SetShaking(
                    false
                );
            }
        }

        private void StopShakeChargeState()
        {
            shakeValue =
                0.0f;

            shakeReady =
                true;

            shakingTimer =
                0.0f;

            SetShaking(
                false
            );

            SetCharging(
                false
            );
        }

private void UpdateFiring(
    float deltaTime
)
{
    StopShakeChargeState();

    if (currentCharge <= 0.0001f)
    {
        SetCharge(
            0.0f,
            false
        );

        isReleaseBurstActive =
            false;

        StopFiring();

        return;
    }

    if (!EnsureBeamActive())
    {
        isReleaseBurstActive =
            false;

        StopFiring();

        return;
    }

    UpdateBeamTransform();

    SetFiring(
        true
    );

    float consumption =
        definition
            .BeamChargeConsumptionPerSecond
        * Mathf.Max(
            0.0f,
            deltaTime
        );

    SetCharge(
        currentCharge
        - consumption,
        false
    );

    if (currentCharge > 0.0001f)
    {
        return;
    }

    SetCharge(
        0.0f,
        false
    );

    isReleaseBurstActive =
        false;

    StopFiring();
}

private bool EnsureBeamActive()
{
    if (activeBeam != null)
    {
        return true;
    }

    if (
        definition == null
        ||
        definition.BeamPrefab == null
    )
    {
        Debug.LogError(
            "[Spray Weapon] "
            + "Beam Prefabが設定されていません。",
            this
        );

        return false;
    }

    Transform beamOrigin =
        muzzle != null
            ? muzzle
            : transform;

    Vector3 direction =
        GetCurrentBeamDirection(
            beamOrigin.position
        );

    Quaternion rotation =
        Quaternion.LookRotation(
            direction,
            Vector3.up
        );

    activeBeam =
        Instantiate(
            definition.BeamPrefab,
            beamOrigin.position,
            rotation,
            beamOrigin
        );

    activeBeam.transform.localPosition =
        Vector3.zero;

    activeBeam.Initialize(
        this,
        definition.BeamDamage,
        definition.BeamDamageInterval,
        definition.BeamTargetLayerMask,
        definition
            .BeamQueryTriggerInteraction,
        definition.BeamDamageImmediately
    );

    if (definition.LogFiring)
    {
        Debug.Log(
            "[Spray Beam] Started"
        );
    }

    return true;
}

private void UpdateBeamTransform()
{
    if (activeBeam == null)
    {
        return;
    }

    Transform beamOrigin =
        muzzle != null
            ? muzzle
            : transform;

    Vector3 direction =
        GetCurrentBeamDirection(
            beamOrigin.position
        );

    activeBeam.transform.position =
        beamOrigin.position;

    activeBeam.transform.rotation =
        Quaternion.LookRotation(
            direction,
            Vector3.up
        );
}

private Vector3 GetCurrentBeamDirection(
    Vector3 beamOrigin
)
{
    Vector3 direction =
        currentAimPosition
        - beamOrigin;

    if (
        direction.sqrMagnitude
        <= 0.0001f
    )
    {
        direction =
            currentAimRay.direction;
    }

    if (
        direction.sqrMagnitude
        <= 0.0001f
    )
    {
        direction =
            transform.forward;
    }

    return direction.normalized;
}

private void StopBeam()
{
    if (activeBeam == null)
    {
        return;
    }

    SprayBeamDamageArea beam =
        activeBeam;

    activeBeam =
        null;

    beam.StopBeam();

    Destroy(
        beam.gameObject
    );

    if (
        definition != null
        &&
        definition.LogFiring
    )
    {
        Debug.Log(
            "[Spray Beam] Stopped"
        );
    }
}
        private bool FireProjectile()
        {
            SprayProjectile projectilePrefab =
                definition.ProjectilePrefab;

            if (projectilePrefab == null)
            {
                Debug.LogError(
                    "[Spray Weapon] "
                    + "Projectile Prefabが設定されていません。",
                    this
                );

                return false;
            }

            Vector3 spawnPosition =
                muzzle != null
                    ? muzzle.position
                    : transform.position;

            Vector3 direction =
                currentAimPosition
                - spawnPosition;

            if (
                direction.sqrMagnitude
                <= 0.0001f
            )
            {
                direction =
                    currentAimRay.direction;
            }

            if (
                direction.sqrMagnitude
                <= 0.0001f
            )
            {
                direction =
                    transform.forward;
            }

            direction.Normalize();

            Quaternion rotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up
                );

            SprayProjectile projectile =
                Instantiate(
                    projectilePrefab,
                    spawnPosition,
                    rotation
                );

            projectile.Initialize(
                this,
                direction,
                definition.ProjectileSpeed,
                definition.ProjectileRadius,
                definition.ProjectileLifetime,
                definition
                    .ProjectileCollisionLayerMask,
                definition
                    .ProjectileQueryTriggerInteraction
            );

            SpawnMuzzleEffect(
                spawnPosition,
                rotation
            );

            firedProjectileCount++;

            ShotFired?.Invoke();

            if (definition.LogFiring)
            {
                Debug.Log(
                    $"[Spray Projectile] "
                    + $"Level={currentChargeLevel}, "
                    + $"Charge={currentCharge:F2}, "
                    + $"Count={firedProjectileCount}, "
                    + $"Direction={direction}"
                );
            }

            return true;
        }

        private void ConsumeChargeForShot()
        {
            if (definition == null)
            {
                StopFiring();
                return;
            }

            float consumption =
                Mathf.Max(
                    0.0001f,
                    definition
                        .ChargeConsumptionPerShot
                );

            float remainingCharge =
                currentCharge
                - consumption;

            if (remainingCharge <= 0.0001f)
            {
                remainingCharge =
                    0.0f;
            }

            SetCharge(
                remainingCharge,
                false
            );

            if (currentCharge <= 0.0001f)
            {
                StopFiring();
            }
        }

        public bool TryApplyProjectileDamage(
            Collider hitCollider,
            Vector3 hitPosition,
            Vector3 hitDirection
        )
        {
            if (
                hitCollider == null
                ||
                definition == null
            )
            {
                return false;
            }

            if (
                !DamageableUtility
                    .TryGetDamageable(
                        hitCollider,
                        out IDamageable damageable
                    )
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
                    definition.DamagePerProjectile,
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
                SpawnHitEffect(
                    hitPosition,
                    -normalizedDirection
                );

                PlayProjectileHitRumble();
            }

            return damageApplied;
        }

        private void SpawnMuzzleEffect(
            Vector3 position,
            Quaternion rotation
        )
        {
            GameObject prefab =
                definition.MuzzleEffectPrefab;

            if (prefab == null)
            {
                return;
            }

            GameObject instance =
                Instantiate(
                    prefab,
                    position,
                    rotation
                );

            if (
                definition.MuzzleEffectLifetime
                > 0.0f
            )
            {
                Destroy(
                    instance,
                    definition
                        .MuzzleEffectLifetime
                );
            }
        }

        private void SpawnHitEffect(
            Vector3 position,
            Vector3 normal
        )
        {
            GameObject prefab =
                definition.HitEffectPrefab;

            if (prefab == null)
            {
                return;
            }

            Quaternion rotation =
                normal.sqrMagnitude
                > 0.0001f
                    ? Quaternion.LookRotation(
                        normal
                    )
                    : Quaternion.identity;

            GameObject instance =
                Instantiate(
                    prefab,
                    position,
                    rotation
                );

            if (
                definition.HitEffectLifetime
                > 0.0f
            )
            {
                Destroy(
                    instance,
                    definition
                        .HitEffectLifetime
                );
            }
        }

        private void SetCharge(
            float value,
            bool allowLevelIncreaseFeedback
        )
        {
            float clampedValue =
                Mathf.Clamp01(
                    value
                );

            if (clampedValue <= 0.0001f)
            {
                clampedValue =
                    0.0f;
            }

            float previousCharge =
                currentCharge;

            int previousLevel =
                currentChargeLevel;

            currentCharge =
                clampedValue;

            currentChargeLevel =
                definition.GetChargeLevel(
                    currentCharge
                );

            if (
                !Mathf.Approximately(
                    previousCharge,
                    currentCharge
                )
            )
            {
                ChargeChanged?.Invoke(
                    currentCharge
                );
            }

            if (
                previousLevel
                != currentChargeLevel
            )
            {
                ChargeLevelChanged?.Invoke(
                    currentChargeLevel
                );
            }

            if (
                allowLevelIncreaseFeedback
                &&
                currentChargeLevel
                > previousLevel
            )
            {
                for (
                    int level =
                        previousLevel + 1;
                    level <= currentChargeLevel;
                    level++
                )
                {
                    ChargeLevelIncreased?.Invoke(
                        level
                    );

                    PlayLevelReachedRumble(
                        level
                    );
                }
            }

            RefreshChargeState(
                true
            );
        }

        private void RefreshChargeState(
            bool notifyFullState
        )
        {
            bool full =
                currentCharge
                >= 0.9999f;

            if (
                isChargeFull
                != full
            )
            {
                isChargeFull =
                    full;

                if (notifyFullState)
                {
                    ChargeFullStateChanged?.Invoke(
                        isChargeFull
                    );
                }
            }

            if (isChargeFull)
            {
                shakingTimer =
                    0.0f;

                SetShaking(
                    false
                );

                SetCharging(
                    false
                );
            }
        }

        private void UpdateHoldingState()
        {
            bool shouldHold =
                currentCharge > 0.0001f
                &&
                !isFiring
                &&
                !isShaking
                &&
                !isCharging;

            SetHoldingCharge(
                shouldHold
            );
        }

        private void UpdateRumble()
        {
            if (definition == null)
            {
                ClearRequestedLoopRumble();

                StopSprayRumble();

                return;
            }

            ResolveRumblePlayer();

            if (rumblePlayer == null)
            {
                ClearRequestedLoopRumble();

                return;
            }

            ControllerRumbleDefinition
                desiredDefinition =
                    null;

            float desiredMultiplier =
                1.0f;

            int rumbleLevel =
                Mathf.Max(
                    1,
                    currentChargeLevel
                );

            if (
                isFiring
                &&
                currentCharge > 0.0001f
            )
            {
                desiredDefinition =
                    definition.FiringRumble;

                desiredMultiplier =
                    definition
                        .GetFiringRumbleMultiplier(
                            rumbleLevel
                        );
            }
            else if (
                isHoldingCharge
                &&
                currentCharge > 0.0001f
            )
            {
                desiredDefinition =
                    definition.HoldingChargeRumble;

                desiredMultiplier =
                    definition
                        .GetHoldingRumbleMultiplier(
                            rumbleLevel
                        );
            }

            if (desiredDefinition == null)
            {
                ClearRequestedLoopRumble();

                StopLoopRumbleOnly();

                return;
            }

            bool requestChanged =
                requestedLoopRumble
                != desiredDefinition;

            if (requestChanged)
            {
                requestedLoopRumble =
                    desiredDefinition;

                requestedRumbleStarted =
                    false;
            }

            if (
                rumblePlayer.IsPlaying
                &&
                rumblePlayer.CurrentDefinition
                == desiredDefinition
            )
            {
                rumblePlayer
                    .SetIntensityMultiplier(
                        desiredMultiplier
                    );

                requestedRumbleStarted =
                    true;

                return;
            }

            if (
                requestedRumbleStarted
                &&
                !desiredDefinition.Loop
            )
            {
                return;
            }

            bool started =
                rumblePlayer.Play(
                    desiredDefinition,
                    desiredMultiplier
                );

            if (
                started
                ||
                (
                    rumblePlayer.IsPlaying
                    &&
                    rumblePlayer
                        .CurrentDefinition
                    == desiredDefinition
                )
            )
            {
                requestedRumbleStarted =
                    true;
            }
        }

        private void PlayLevelReachedRumble(
            int level
        )
        {
            if (definition == null)
            {
                return;
            }

            ControllerRumbleDefinition
                rumbleDefinition =
                    definition
                        .GetLevelReachedRumble(
                            level
                        );

            if (rumbleDefinition == null)
            {
                return;
            }

            ResolveRumblePlayer();

            if (rumblePlayer == null)
            {
                return;
            }

            rumblePlayer.Play(
                rumbleDefinition
            );
        }

        private void PlayProjectileHitRumble()
        {
            if (
                definition == null
                ||
                definition.ProjectileHitRumble
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
                definition.ProjectileHitRumble
            );
        }

        private void ClearRequestedLoopRumble()
        {
            requestedLoopRumble =
                null;

            requestedRumbleStarted =
                false;
        }

        private void StopLoopRumbleOnly()
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
                current =
                    rumblePlayer
                        .CurrentDefinition;

            bool isLoopRumble =
                current
                == definition.HoldingChargeRumble
                ||
                current
                == definition.FiringRumble;

            if (isLoopRumble)
            {
                rumblePlayer.Stop();
            }
        }

        private void StopSprayRumble()
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
                current =
                    rumblePlayer
                        .CurrentDefinition;

            bool isSprayRumble =
                current
                == definition.HoldingChargeRumble
                ||
                current
                == definition.FiringRumble
                ||
                current
                == definition.ProjectileHitRumble
                ||
                current
                == definition
                    .GetLevelReachedRumble(1)
                ||
                current
                == definition
                    .GetLevelReachedRumble(2)
                ||
                current
                == definition
                    .GetLevelReachedRumble(3);

            if (isSprayRumble)
            {
                rumblePlayer.Stop();
            }
        }

        private void SetShaking(
            bool value
        )
        {
            if (isShaking == value)
            {
                return;
            }

            isShaking =
                value;

            ShakingStateChanged?.Invoke(
                isShaking
            );
        }

        private void SetCharging(
            bool value
        )
        {
            if (isCharging == value)
            {
                return;
            }

            isCharging =
                value;

            ChargingStateChanged?.Invoke(
                isCharging
            );
        }

        private void SetHoldingCharge(
            bool value
        )
        {
            if (isHoldingCharge == value)
            {
                return;
            }

            isHoldingCharge =
                value;

            HoldingChargeStateChanged?.Invoke(
                isHoldingCharge
            );
        }

        private void SetFiring(
            bool value
        )
        {
            if (isFiring == value)
            {
                return;
            }

            isFiring =
                value;

            FiringStateChanged?.Invoke(
                isFiring
            );
        }

        private void StopFiring()
{
    SetFiring(
        false
    );

    projectileTimer =
        0.0f;

    StopBeam();
}

public bool TryApplyBeamDamage(
    IDamageable damageable,
    Collider hitCollider,
    Vector3 hitPosition,
    Vector3 hitDirection,
    float beamDamage
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
                beamDamage
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
        SpawnHitEffect(
            hitPosition,
            -normalizedDirection
        );
    }

    if (definition.LogFiring)
    {
        string targetName =
            hitCollider != null
                ? hitCollider.name
                : "Unknown";

        Debug.Log(
            $"[Spray Beam Damage] "
            + $"Target={targetName}, "
            + $"Damage={beamDamage:F2}, "
            + $"Applied={damageApplied}"
        );
    }

    return damageApplied;
}

public void NotifyBeamDamageTick()
{
    PlayProjectileHitRumble();
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

        private void ResetWeaponState()
        {

            StopBeam();
            ClearRequestedLoopRumble();

            StopSprayRumble();

            currentCharge =
                0.0f;

            currentChargeLevel =
                0;

            wasActivationActive =
                false;

            isReleaseBurstActive =
                false;

            shakeValue =
                0.0f;

            shakeReady =
                true;

            shakingTimer =
                0.0f;

            projectileTimer =
                0.0f;

            firedProjectileCount =
                0;

            SetShaking(
                false
            );

            SetCharging(
                false
            );

            SetHoldingCharge(
                false
            );

            SetFiring(
                false
            );

            if (isChargeFull)
            {
                isChargeFull =
                    false;

                ChargeFullStateChanged?.Invoke(
                    false
                );
            }

            ChargeChanged?.Invoke(
                currentCharge
            );

            ChargeLevelChanged?.Invoke(
                currentChargeLevel
            );
        }

        private void OnDrawGizmosSelected()
        {
            if (
                !Application.isPlaying
                ||
                aimCamera == null
            )
            {
                return;
            }

            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                aimHitCollider
                    ? Color.cyan
                    : Color.yellow;

            Gizmos.DrawSphere(
                currentAimPosition,
                0.1f
            );

            Gizmos.color =
                previousColor;
        }


        private bool TryFindEnemyAimPoint(
    out Vector3 aimPosition
)
{
    aimPosition =
        Vector3.zero;

    int hitCount =
        Physics.SphereCastNonAlloc(
            currentAimRay,
            definition.EnemyAimAssistRadius,
            aimAssistHitBuffer,
            definition.MaximumAimDistance,
            definition.EnemyAimLayerMask,
            definition
                .EnemyAimQueryTriggerInteraction
        );

    if (hitCount <= 0)
    {
        return false;
    }

    float nearestDistance =
        float.PositiveInfinity;

    Collider nearestCollider =
        null;

    for (
        int index = 0;
        index < hitCount;
        index++
    )
    {
        RaycastHit hit =
            aimAssistHitBuffer[index];

        if (hit.collider == null)
        {
            continue;
        }

        if (
            !DamageableUtility
                .TryGetDamageable(
                    hit.collider,
                    out IDamageable damageable
                )
        )
        {
            continue;
        }

        if (
            damageable == null
            ||
            !damageable.IsAlive
        )
        {
            continue;
        }

        Vector3 targetCenter =
            hit.collider.bounds.center;

        float distanceAlongRay =
            Vector3.Dot(
                targetCenter
                - currentAimRay.origin,
                currentAimRay.direction
            );

        if (distanceAlongRay < 0.0f)
        {
            continue;
        }

        if (
            distanceAlongRay
            >= nearestDistance
        )
        {
            continue;
        }

        nearestDistance =
            distanceAlongRay;

        nearestCollider =
            hit.collider;
    }

    if (nearestCollider == null)
    {
        return false;
    }

    aimPosition =
        nearestCollider
            .bounds
            .center;

    return true;
}
    }
}