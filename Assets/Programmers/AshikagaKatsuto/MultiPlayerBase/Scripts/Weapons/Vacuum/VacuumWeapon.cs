using System;
using Game.Combat;
using Game.Feedback.Rumble;
using UnityEngine;

namespace Game.Weapons.Vacuum
{
    public sealed class VacuumWeapon :
        LocalPlayerWeapon
    {
        private const int
            TargetHitBufferSize =
                32;

        [Header("Definition")]
        [SerializeField]
        private VacuumWeaponDefinition definition;

        [Header("Targeting")]
        [Tooltip(
            "敵を描画しているゲーム用カメラを"
            + "設定してください。"
        )]
        [SerializeField]
        private Camera aimCamera;

        [Tooltip(
            "吸引VFXを開始する掃除機側の位置です。"
        )]
        [SerializeField]
        private Transform vacuumVFXSpawnPoint;

        [Header("Feedback")]
        [Tooltip(
            "このプレイヤーのコントローラーへ"
            + "振動を出力するコンポーネントです。"
        )]
        [SerializeField]
        private ControllerRumblePlayer
            rumblePlayer;

        [Header("Runtime Target")]
        [SerializeField]
        private bool isTargetLocked;

        [SerializeField]
        private bool isVacuuming;

        [SerializeField]
        private string lockedTargetName;

        [SerializeField]
        private Collider lockedTargetCollider;

        [SerializeField]
        private Vector3 lockedTargetWorldPosition;

        [Header("Runtime Reticle")]
        [SerializeField]
        private Vector2
            constraintCenterScreenPosition;

        [SerializeField]
        private Vector2
            constraintCenterVirtualPosition;

        [SerializeField]
        private float constraintRadiusScreen;

        [SerializeField]
        private float pointerMovementThisFrame;

        [Header("Runtime Suction")]
        [Header("Runtime Spin Effect")]
[SerializeField]
[Range(0.0f, 1.0f)]
private float spinEffectT;

[SerializeField]
[Range(0.0f, 1.0f)]
private float spinEffectStrength;

[SerializeField]
private bool isSpinEffectBuilding;

[SerializeField]
private float currentAttackInterval;

[SerializeField]
private float attackTimer;

[SerializeField]
private int attackCount;
        [SerializeField]
        private float currentSuctionAmount;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float normalizedSuctionAmount;

        [SerializeField]
        private int currentSuctionLevel;

        [SerializeField]
        private float damageTimer;

        [Header("Runtime Feedback")]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float
            currentTargetingRumbleMultiplier;

        [SerializeField]
        private bool isTargetingRumbleActive;

        [SerializeField]
        private Vector2 currentVFXSteering;

        [Header("Runtime Debug")]
        [SerializeField]
        private Vector2 pointerScreenPosition;

        [SerializeField]
        private bool targetAcquiredThisFrame;

        private readonly RaycastHit[]
            targetHitBuffer =
                new RaycastHit[
                    TargetHitBufferSize
                ];

        private bool wasActivationActive;

        private IDamageable
            lockedTargetDamageable;

        private VacuumTargetVFX
            targetVFXInstance;

        public event Action<Collider>
            TargetLocked;

        public event Action
            TargetReleased;

        public event Action<float>
            SuctionAmountChanged;

        public event Action<int>
            SuctionLevelChanged;

        public override WeaponDefinition Definition =>
            definition;

        public bool IsTargetLocked =>
            isTargetLocked;

        public bool IsVacuuming =>
            isVacuuming;

        public float SpinEffectT =>
    spinEffectT;

public float SpinEffectStrength =>
    spinEffectStrength;

public bool IsSpinEffectBuilding =>
    isSpinEffectBuilding;

public float CurrentAttackInterval =>
    currentAttackInterval;

        public Collider LockedTargetCollider =>
            lockedTargetCollider;

        public Vector2
            ConstraintCenterScreenPosition =>
                constraintCenterScreenPosition;

        public Vector2
            ConstraintCenterVirtualPosition =>
                constraintCenterVirtualPosition;

        public float ConstraintRadiusScreen =>
            constraintRadiusScreen;

        public float CurrentSuctionAmount =>
            currentSuctionAmount;

        public float NormalizedSuctionAmount =>
            normalizedSuctionAmount;

        public int CurrentSuctionLevel =>
            currentSuctionLevel;

        public int MaximumSuctionLevel =>
            definition != null
                ? definition.MaximumSuctionLevel
                : 1;

        public float PointerMovementThisFrame =>
            pointerMovementThisFrame;

        private void Awake()
        {
            ResolveRumblePlayer();
        }

        
protected override void TickWeapon(
    float deltaTime,
    WeaponActivationState activation
)
{
    if (
        definition == null
        ||
        aimCamera == null
        ||
        GyroPointer == null
    )
    {
        ResetWeaponState();
        return;
    }

    bool activationActive =
        activation.IsActive;

    bool releasedThisFrame =
        !activationActive
        &&
        wasActivationActive;

    targetAcquiredThisFrame =
        false;

    if (activationActive)
    {
        if (isTargetLocked)
        {
            UpdateVacuumAttack(
                deltaTime
            );
        }
        else
        {
            // RZを押している間は、
            // 敵をロックするまで検索を継続する。
            TryBeginVacuumAttack();

            // このフレームでロックできた場合は、
            // すぐに吸引処理へ入る。
            if (isTargetLocked)
            {
                UpdateVacuumAttack(
                    deltaTime
                );
            }
            else
            {
                UpdateSpinEffectDecay(
                    deltaTime
                );
            }
        }
    }
    else
    {
        UpdateSpinEffectDecay(
            deltaTime
        );
    }

    if (releasedThisFrame)
    {
        EndVacuumAttack(
            preserveSpinEffect: true
        );
    }

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

        private void TryBeginVacuumAttack()
        {
            EndVacuumAttack();

            pointerScreenPosition =
                GyroPointer
                    .GetPointerScreenPosition();

            Ray aimRay =
                aimCamera.ScreenPointToRay(
                    pointerScreenPosition
                );

            if (
                definition.DrawTargetingCast
            )
            {
                Debug.DrawRay(
                    aimRay.origin,
                    aimRay.direction
                    * definition
                        .MaximumTargetDistance,
                    Color.cyan,
                    1.0f
                );
            }

            if (
                !TryFindTarget(
                    aimRay,
                    out IDamageable damageable,
                    out Collider targetCollider
                )
            )
            {
                if (
                    definition.LogTargetDetection
                )
                {
                    Debug.Log(
                        "[Vacuum Target] "
                        + "RZ押下時に対象が"
                        + "見つかりませんでした。"
                    );
                }

                return;
            }

            LockTarget(
                damageable,
                targetCollider
            );
        }

        private bool TryFindTarget(
            Ray aimRay,
            out IDamageable damageable,
            out Collider targetCollider
        )
        {
            damageable =
                null;

            targetCollider =
                null;

            int hitCount =
                Physics.SphereCastNonAlloc(
                    aimRay,
                    definition.TargetingRadius,
                    targetHitBuffer,
                    definition.MaximumTargetDistance,
                    definition.TargetLayerMask,
                    definition
                        .TargetQueryTriggerInteraction
                );

            if (hitCount <= 0)
            {
                return false;
            }

            float bestDistanceFromRay =
                float.PositiveInfinity;

            float bestDistanceAlongRay =
                float.PositiveInfinity;

            for (
                int index = 0;
                index < hitCount;
                index++
            )
            {
                RaycastHit hit =
                    targetHitBuffer[index];

                Collider candidateCollider =
                    hit.collider;

                if (candidateCollider == null)
                {
                    continue;
                }

                if (
                    !DamageableUtility
                        .TryGetDamageable(
                            candidateCollider,
                            out IDamageable
                                candidateDamageable
                        )
                )
                {
                    continue;
                }

                if (
                    candidateDamageable == null
                    ||
                    !candidateDamageable.IsAlive
                )
                {
                    continue;
                }

                Vector3 candidateCenter =
                    candidateCollider
                        .bounds
                        .center;

                Vector3 offset =
                    candidateCenter
                    - aimRay.origin;

                float distanceAlongRay =
                    Vector3.Dot(
                        offset,
                        aimRay.direction
                    );

                if (
                    distanceAlongRay <= 0.0f
                    ||
                    distanceAlongRay
                    > definition.MaximumTargetDistance
                )
                {
                    continue;
                }

                Vector3 closestPointOnRay =
                    aimRay.origin
                    + aimRay.direction
                    * distanceAlongRay;

                float distanceFromRay =
                    Vector3.Distance(
                        candidateCenter,
                        closestPointOnRay
                    );

                bool betterCandidate =
                    distanceFromRay
                    < bestDistanceFromRay;

                if (
                    Mathf.Approximately(
                        distanceFromRay,
                        bestDistanceFromRay
                    )
                )
                {
                    betterCandidate =
                        distanceAlongRay
                        < bestDistanceAlongRay;
                }

                if (!betterCandidate)
                {
                    continue;
                }

                bestDistanceFromRay =
                    distanceFromRay;

                bestDistanceAlongRay =
                    distanceAlongRay;

                damageable =
                    candidateDamageable;

                targetCollider =
                    candidateCollider;
            }

            return
                damageable != null
                &&
                targetCollider != null;
        }

        private void LockTarget(
            IDamageable damageable,
            Collider targetCollider
        )
        {
            lockedTargetDamageable =
                damageable;

            lockedTargetCollider =
                targetCollider;

            isTargetLocked =
                true;

            isVacuuming =
                true;

            targetAcquiredThisFrame =
                true;

            lockedTargetName =
                targetCollider.name;

            currentSuctionAmount =
                0.0f;

            normalizedSuctionAmount =
                0.0f;

            currentSuctionLevel =
                1;

            pointerMovementThisFrame =
                0.0f;

            currentVFXSteering =
                Vector2.zero;

            currentAttackInterval =
    definition.GetAttackInterval(
        spinEffectStrength
    );

attackTimer =
    definition.DamageImmediatelyOnTarget
        ? 0.0f
        : currentAttackInterval;

attackCount =
    0;

            if (!UpdateConstraintFromTarget())
{
    EndVacuumAttack();
    return;
}

GyroPointer
    .BeginCircularConstraint(
        constraintCenterVirtualPosition,
        definition
            .ReticleConstraintRadius
    );

            CreateTargetVFX();

            TargetLocked?.Invoke(
                lockedTargetCollider
            );

            SuctionAmountChanged?.Invoke(
                normalizedSuctionAmount
            );

            SuctionLevelChanged?.Invoke(
                currentSuctionLevel
            );

            if (
                definition.LogTargetDetection
            )
            {
                Debug.Log(
                    $"[Vacuum Target] "
                    + $"Locked={lockedTargetName}, "
                    + $"Level={currentSuctionLevel}"
                );
            }
        }


private void UpdateVacuumAttack(
    float deltaTime
)
{
    if (!IsLockedTargetValid())
    {
        EndVacuumAttack(
            preserveSpinEffect: true
        );

        return;
    }

    if (!UpdateConstraintFromTarget())
    {
        EndVacuumAttack(
            preserveSpinEffect: true
        );

        return;
    }

    GyroPointer
        .UpdateCircularConstraint(
            constraintCenterVirtualPosition,
            definition
                .ReticleConstraintRadius
        );

    UpdateSuctionMovement(
        deltaTime
    );

    UpdateSpinEffect(
        deltaTime
    );

    UpdateAttackPulse(
        deltaTime
    );

    UpdateTargetVFX();

    UpdateSpinningRumble();
}


private void UpdateSpinEffect(
    float deltaTime
)
{
    bool spinning =
        pointerMovementThisFrame
        >= definition
            .SpinMovementThreshold;

    isSpinEffectBuilding =
        spinning;

    if (spinning)
    {
        float buildSpeed =
            1.0f
            / Mathf.Max(
                0.01f,
                definition.SpinBuildDuration
            );

        spinEffectT =
            Mathf.MoveTowards(
                spinEffectT,
                1.0f,
                buildSpeed
                * deltaTime
            );
    }
    else
    {
        ReduceSpinEffect(
            deltaTime
        );
    }

    spinEffectStrength =
        definition
            .EvaluateSpinIntensity(
                spinEffectT
            );
}

private void UpdateSpinEffectDecay(
    float deltaTime
)
{
    isSpinEffectBuilding =
        false;

    ReduceSpinEffect(
        deltaTime
    );

    spinEffectStrength =
        definition
            .EvaluateSpinIntensity(
                spinEffectT
            );
}

private void ReduceSpinEffect(
    float deltaTime
)
{
    float decaySpeed =
        1.0f
        / Mathf.Max(
            0.01f,
            definition.SpinDecayDuration
        );

    spinEffectT =
        Mathf.MoveTowards(
            spinEffectT,
            0.0f,
            decaySpeed
            * deltaTime
        );

    if (spinEffectT <= 0.0001f)
    {
        spinEffectT =
            0.0f;
    }
}

private void UpdateAttackPulse(
    float deltaTime
)
{
    if (!IsLockedTargetValid())
    {
        return;
    }

    currentAttackInterval =
        definition
            .GetAttackInterval(
                spinEffectStrength
            );

    attackTimer -=
        deltaTime;

    if (attackTimer > 0.0f)
    {
        return;
    }

    PerformAttackPulse();

    attackTimer =
        currentAttackInterval;
}

private void PerformAttackPulse()
{
    if (!IsLockedTargetValid())
    {
        return;
    }

    attackCount++;

    PlayAttackPulseRumble();

    ApplyContinuousDamage();
}

        private bool IsLockedTargetValid()
        {
            return
                lockedTargetDamageable != null
                &&
                lockedTargetDamageable.IsAlive
                &&
                lockedTargetCollider != null;
        }

        private bool UpdateConstraintFromTarget()
{
    if (
        lockedTargetCollider == null
        ||
        aimCamera == null
        ||
        GyroPointer == null
    )
    {
        return false;
    }

    lockedTargetWorldPosition =
        lockedTargetCollider
            .bounds
            .center;

    Vector3 screenPosition =
        aimCamera.WorldToScreenPoint(
            lockedTargetWorldPosition
        );

    // 敵がカメラの後方へ移動した。
    if (screenPosition.z <= 0.0f)
    {
        return false;
    }

    constraintCenterScreenPosition =
        new Vector2(
            screenPosition.x,
            screenPosition.y
        );

    constraintCenterVirtualPosition =
        GyroPointer
            .ScreenToVirtualPosition(
                constraintCenterScreenPosition
            );

    constraintRadiusScreen =
        GyroPointer
            .ConvertVirtualRadiusToScreen(
                definition
                    .ReticleConstraintRadius
            );

    return true;
}

        private void UpdateSuctionMovement(
            float deltaTime
        )
        {
            Vector2 rawMovement =
                GyroPointer
                    .RawMovementThisFrame;

            float movement =
                rawMovement.magnitude;

            if (
                movement
                < definition
                    .MinimumMovementThreshold
            )
            {
                movement =
                    0.0f;
            }

            movement =
                Mathf.Min(
                    movement,
                    definition
                        .MaximumMovementPerFrame
                );

            pointerMovementThisFrame =
                movement;

            float safeDeltaTime =
                Mathf.Max(
                    0.0001f,
                    deltaTime
                );

            currentVFXSteering =
                Vector2.ClampMagnitude(
                    rawMovement
                    / safeDeltaTime
                    * definition
                        .VFXSteeringSensitivity
                    * 0.001f,
                    1.0f
                );

            if (movement <= 0.0f)
            {
                return;
            }

            float previousAmount =
                currentSuctionAmount;

            int previousLevel =
                currentSuctionLevel;

            float maximumMovement =
                definition
                    .MaximumSuctionMovement;

            if (maximumMovement > 0.0f)
            {
                currentSuctionAmount =
                    Mathf.Min(
                        currentSuctionAmount
                        + movement,
                        maximumMovement
                    );
            }
            else
            {
                currentSuctionAmount =
                    0.0f;
            }

            normalizedSuctionAmount =
                definition
                    .GetNormalizedSuction(
                        currentSuctionAmount
                    );

            currentSuctionLevel =
                definition
                    .GetSuctionLevel(
                        currentSuctionAmount
                    );

            if (
                !Mathf.Approximately(
                    previousAmount,
                    currentSuctionAmount
                )
            )
            {
                SuctionAmountChanged?.Invoke(
                    normalizedSuctionAmount
                );
            }

            if (
                currentSuctionLevel
                > previousLevel
            )
            {
                for (
                    int level =
                        previousLevel + 1;
                    level <= currentSuctionLevel;
                    level++
                )
                {
                    SuctionLevelChanged?.Invoke(
                        level
                    );

                    PlayLevelIncreasedRumble();
                }
            }

            if (
                definition.LogSuctionProgress
            )
            {
                Debug.Log(
                    $"[Vacuum Suction] "
                    + $"Movement={movement:F2}, "
                    + $"Amount="
                    + $"{currentSuctionAmount:F2}, "
                    + $"Normalized="
                    + $"{normalizedSuctionAmount:F2}, "
                    + $"Level="
                    + $"{currentSuctionLevel}"
                );
            }
        }

        private void ApplyContinuousDamage()
        {
            if (!IsLockedTargetValid())
            {
                return;
            }

            Vector3 hitPosition =
                lockedTargetCollider
                    .bounds
                    .center;

            Vector3 hitDirection =
                hitPosition
                - aimCamera.transform.position;

            if (
                hitDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                hitDirection =
                    aimCamera.transform.forward;
            }

            float baseAttackDamage =
    definition.GetDamage(
        currentSuctionLevel
    );

float damageMultiplier =
    definition
        .GetDamageMultiplier(
            spinEffectStrength
        );

float damage =
    baseAttackDamage
    * damageMultiplier;

            DamageInfo damageInfo =
                new DamageInfo(
                    damage,
                    hitPosition,
                    hitDirection.normalized,
                    LocalPlayer,
                    definition
                );

            bool damageApplied =
                TryApplyDamage(
                    lockedTargetDamageable,
                    damageInfo
                );

            if (damageApplied)
            {
                SpawnDamageEffect(
                    hitPosition,
                    -hitDirection.normalized
                );
            }

            if (
                definition.LogTargetDetection
            )
            {
                Debug.Log(
                    $"[Vacuum Damage] "
                    + $"Target={lockedTargetName}, "
                    + $"Level={currentSuctionLevel}, "
                    + $"Damage={damage}, "
                    + $"Applied={damageApplied}"
                );
            }
        }

        private void PlayAttackPulseRumble()
{
    if (
        definition == null
        ||
        definition.AttackPulseRumble
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

    float intensity =
        definition
            .GetAttackRumbleIntensity(
                spinEffectStrength
            );

    rumblePlayer.Play(
        definition.AttackPulseRumble,
        intensity
    );
}

        private void UpdateTargetVFX()
        {
            if (
                targetVFXInstance == null
                &&
                lockedTargetCollider != null
            )
            {
                CreateTargetVFX();
            }

            if (targetVFXInstance == null)
            {
                return;
            }

            targetVFXInstance
                .SetDamageStack(
                    currentSuctionLevel
                );

            targetVFXInstance
                .SetSteeringInput(
                    currentVFXSteering
                );
        }

        private void CreateTargetVFX()
        {
            DestroyTargetVFX();

            if (
                definition == null
                ||
                definition.TargetVFXPrefab
                    == null
                ||
                lockedTargetCollider == null
            )
            {
                return;
            }

            Transform spawnPoint =
                vacuumVFXSpawnPoint != null
                    ? vacuumVFXSpawnPoint
                    : transform;

            targetVFXInstance =
                Instantiate(
                    definition.TargetVFXPrefab,
                    spawnPoint.position,
                    spawnPoint.rotation
                );

            targetVFXInstance.Initialize(
                spawnPoint,
                lockedTargetCollider,
                currentSuctionLevel
            );
        }

        private void DestroyTargetVFX()
        {
            if (targetVFXInstance == null)
            {
                return;
            }

            Destroy(
                targetVFXInstance.gameObject
            );

            targetVFXInstance =
                null;
        }

        private void UpdateSpinningRumble()
{
    if (
        !isSpinEffectBuilding
        ||
        definition == null
    )
    {
        StopSpinningLoopRumble();
        return;
    }

    ControllerRumbleDefinition loopRumble =
        definition.TargetingLoopRumble;

    if (loopRumble == null)
    {
        return;
    }

    ResolveRumblePlayer();

    if (rumblePlayer == null)
    {
        return;
    }

    float intensity =
        definition
            .GetTargetingRumbleMultiplier(
                spinEffectStrength
            );

    currentTargetingRumbleMultiplier =
        intensity;

    if (
        rumblePlayer.IsPlaying
        &&
        rumblePlayer.CurrentDefinition
        == loopRumble
    )
    {
        rumblePlayer
            .SetIntensityMultiplier(
                intensity
            );

        isTargetingRumbleActive =
            true;

        return;
    }

    // 単発のアタック振動が再生されている間は、
    // 継続振動で即座に上書きしない。
    if (
        rumblePlayer.IsPlaying
        &&
        rumblePlayer.CurrentDefinition
        == definition.AttackPulseRumble
    )
    {
        isTargetingRumbleActive =
            false;

        return;
    }

    bool started =
        rumblePlayer.Play(
            loopRumble,
            intensity
        );

    isTargetingRumbleActive =
        started
        ||
        (
            rumblePlayer.IsPlaying
            &&
            rumblePlayer.CurrentDefinition
            == loopRumble
        );
}

private void StopSpinningLoopRumble()
{
    isTargetingRumbleActive =
        false;

    currentTargetingRumbleMultiplier =
        0.0f;

    if (
        rumblePlayer == null
        ||
        definition == null
    )
    {
        return;
    }

    if (
        rumblePlayer.CurrentDefinition
        == definition.TargetingLoopRumble
    )
    {
        rumblePlayer.Stop();
    }
}
        private void PlayLevelIncreasedRumble()
        {
            if (
                definition == null
                ||
                definition
                    .SuctionLevelIncreasedRumble
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
                definition
                    .SuctionLevelIncreasedRumble,
                definition
                    .LevelIncreasedRumbleIntensity
            );
        }

        private void StopVacuumRumble()
        {
            if (
                rumblePlayer == null
                ||
                definition == null
            )
            {
                return;
            }

            ControllerRumbleDefinition current =
                rumblePlayer.CurrentDefinition;

            bool isVacuumRumble =
    current
    == definition.TargetingLoopRumble
    ||
    current
    == definition
        .SuctionLevelIncreasedRumble
    ||
    current
    == definition.AttackPulseRumble;

            if (isVacuumRumble)
            {
                rumblePlayer.Stop();
            }
        }

        private void SpawnDamageEffect(
            Vector3 position,
            Vector3 direction
        )
        {
            GameObject prefab =
                definition.DamageEffectPrefab;

            if (prefab == null)
            {
                return;
            }

            Quaternion rotation =
                direction.sqrMagnitude
                > 0.0001f
                    ? Quaternion.LookRotation(
                        direction
                    )
                    : Quaternion.identity;

            GameObject instance =
                Instantiate(
                    prefab,
                    position,
                    rotation
                );

            if (
                definition.DamageEffectLifetime
                > 0.0f
            )
            {
                Destroy(
                    instance,
                    definition
                        .DamageEffectLifetime
                );
            }
        }

        private void EndVacuumAttack(
    bool preserveSpinEffect = true
)
        {
            bool hadTarget =
                isTargetLocked
                ||
                lockedTargetCollider != null;

            if (GyroPointer != null)
            {
                GyroPointer
                    .EndCircularConstraint();
            }

            StopVacuumRumble();

            DestroyTargetVFX();

            isTargetLocked =
                false;

            isVacuuming =
                false;

            lockedTargetDamageable =
                null;

            lockedTargetCollider =
                null;

            lockedTargetName =
                string.Empty;

            lockedTargetWorldPosition =
                Vector3.zero;

            constraintCenterScreenPosition =
                Vector2.zero;

            constraintCenterVirtualPosition =
                Vector2.zero;

            constraintRadiusScreen =
                0.0f;

            pointerMovementThisFrame =
                0.0f;

            currentSuctionAmount =
                0.0f;

            normalizedSuctionAmount =
                0.0f;

            currentSuctionLevel =
                0;

            damageTimer =
                0.0f;

            currentTargetingRumbleMultiplier =
                0.0f;

            isTargetingRumbleActive =
                false;

            currentVFXSteering =
                Vector2.zero;

            if (hadTarget)
            {
                TargetReleased?.Invoke();

                SuctionAmountChanged?.Invoke(
                    0.0f
                );

                SuctionLevelChanged?.Invoke(
                    0
                );
            }

            isSpinEffectBuilding =
    false;

currentAttackInterval =
    0.0f;

attackTimer =
    0.0f;

attackCount =
    0;

if (!preserveSpinEffect)
{
    spinEffectT =
        0.0f;

    spinEffectStrength =
        0.0f;
}
        }

        private void ResetWeaponState()
{
    EndVacuumAttack(
        preserveSpinEffect: false
    );

    wasActivationActive =
        false;

    targetAcquiredThisFrame =
        false;

    spinEffectT =
        0.0f;

    spinEffectStrength =
        0.0f;

    isSpinEffectBuilding =
        false;

    currentAttackInterval =
        0.0f;

    attackTimer =
        0.0f;

    attackCount =
        0;
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

        private void OnDrawGizmosSelected()
        {
            if (
                !Application.isPlaying
                ||
                lockedTargetCollider == null
            )
            {
                return;
            }

            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                Color.cyan;

            Gizmos.DrawWireSphere(
                lockedTargetCollider
                    .bounds
                    .center,
                0.25f
            );

            Gizmos.color =
                previousColor;
        }
    }
}