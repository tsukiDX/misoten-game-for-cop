using System;
using Game.Combat;
using Game.Feedback.Rumble;
using UnityEngine;

namespace Game.Weapons.Mop
{
    public sealed class MopWeapon :
        LocalPlayerWeapon
    {
        private const int HitBufferSize =
            16;

        [Header("Definition")]
        [SerializeField]
        private MopWeaponDefinition definition;

        [Header("Targeting")]
        [Tooltip(
            "敵を描画しているゲーム用カメラを"
            + "設定してください。"
        )]
        [SerializeField]
        private Camera aimCamera;

        [Header("Projectile")]
        [Tooltip(
            "投射用モップを生成する位置です。"
            + "未設定の場合はこのGameObjectの位置を使います。"
        )]
        [SerializeField]
        private Transform mopLaunchPoint;

        [Header("Optional Held Visual")]
        [Tooltip(
            "プレイヤーが通常時に持つ"
            + "モップモデルのルートです。"
            + "モデル未作成の場合は未設定で構いません。"
        )]
        [SerializeField]
        private GameObject heldMopVisual;

                [Header("Feedback")]
        [Tooltip(
            "このプレイヤーのコントローラーへ"
            + "振動を出力するコンポーネントです。"
            + "未設定の場合は親から自動検索します。"
        )]
        [SerializeField]
        private ControllerRumblePlayer
            rumblePlayer;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isAttacking;

        [SerializeField]
        private bool hasContactTarget;

        [SerializeField]
        private string contactTargetName;

        [SerializeField]
        private Vector2
            currentPointerScreenPosition;

        [SerializeField]
        private Vector3 currentHitPosition;

        [SerializeField]
        private Vector3 currentHitNormal =
            Vector3.up;

        [SerializeField]
        private float damageTimer;

        [SerializeField]
        private int currentHitCount;

        [Header("Runtime Lock On")]
        [SerializeField]
        private int currentChargeCount;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float normalizedCharge;

        [SerializeField]
        private bool hasChargeTarget;

        [SerializeField]
        private bool isLockOnComplete;

        [SerializeField]
        private string chargeTargetName;

        [SerializeField]
        private bool isProjectileActive;

        private readonly RaycastHit[] hitBuffer =
            new RaycastHit[HitBufferSize];

        private Ray currentAimRay;

        private bool wasActivationActive;

        private IDamageable currentContactDamageable;

        private Collider currentContactCollider;

        private IDamageable chargeTargetDamageable;

        private Collider chargeTargetCollider;

        private MopHomingProjectile
            activeProjectile;

        public event Action<float>
            ChargeChanged;

        public event Action<Collider>
            LockOnCompleted;

        public event Action
            LockOnCancelled;

        public event Action
            MopLaunched;

        public event Action
            MopProjectileFinished;

        public override WeaponDefinition Definition =>
            definition;

        public int CurrentChargeCount =>
            currentChargeCount;

        public int RequiredChargeCount =>
            definition != null
                ? definition.RequiredHitCount
                : 1;

        public float NormalizedCharge =>
            normalizedCharge;

        public bool HasChargeTarget =>
            hasChargeTarget;

        public bool IsLockOnComplete =>
            isLockOnComplete;

        public Collider ChargeTargetCollider =>
            chargeTargetCollider;

        public bool IsProjectileActive =>
            activeProjectile != null;

        public bool IsHeldMopVisible =>
            heldMopVisual == null
            ||
            heldMopVisual.activeSelf;

        public Vector3 CurrentHitPosition =>
            currentHitPosition;

        private void Awake()
        {
            ResolveRumblePlayer();
        }

        protected override void TickWeapon(
            float deltaTime,
            WeaponActivationState activation
        )
        {
            if (definition == null)
            {
                ResetAllState();
                return;
            }

            bool activationActive =
                activation.IsActive;

            bool releasedThisFrame =
                wasActivationActive
                &&
                !activationActive;

            isProjectileActive =
                activeProjectile != null;

            if (activeProjectile != null)
            {
                isAttacking = false;

                ClearContactTarget();

                if (releasedThisFrame)
                {
                    wasActivationActive =
                        false;
                }
                else
                {
                    wasActivationActive =
                        activationActive;
                }

                return;
            }

            if (
                aimCamera == null
                ||
                GyroPointer == null
            )
            {
                ClearContactTarget();

                if (releasedThisFrame)
                {
                    HandleActivationReleased();
                }

                wasActivationActive =
                    activationActive;

                return;
            }

            UpdateAimRay();

            isAttacking =
                activationActive;

            if (activationActive)
            {
                UpdateContactAttack(
                    deltaTime
                );
            }
            else
            {
                ClearContactTarget();
            }

            if (releasedThisFrame)
            {
                HandleActivationReleased();
            }

            wasActivationActive =
                activationActive;

            isProjectileActive =
                activeProjectile != null;
        }

        protected override void OnInputUnavailable()
        {
            CancelActiveProjectile();

            ResetAllState();

            if (rumblePlayer != null)
            {
                rumblePlayer.Stop();
            }
        }

        protected override void OnUnequipped()
        {
            CancelActiveProjectile();

            ResetAllState();

            if (rumblePlayer != null)
            {
                rumblePlayer.Stop();
            }
        }

        private void OnDisable()
        {
            CancelActiveProjectile();

            SetHeldMopVisible(
                true
            );

            if (rumblePlayer != null)
            {
                rumblePlayer.Stop();
            }
        }

        private void UpdateAimRay()
        {
            currentPointerScreenPosition =
                GyroPointer
                    .GetPointerScreenPosition();

            currentAimRay =
                aimCamera.ScreenPointToRay(
                    currentPointerScreenPosition
                );

            if (definition.DrawAimCast)
            {
                Debug.DrawRay(
                    currentAimRay.origin,
                    currentAimRay.direction
                    * definition.MaximumAimDistance,
                    isAttacking
                        ? Color.green
                        : Color.yellow
                );
            }
        }

        private void UpdateContactAttack(
            float deltaTime
        )
        {
            int hitCount =
                Physics.SphereCastNonAlloc(
                    currentAimRay,
                    definition.PointerRadius,
                    hitBuffer,
                    definition.MaximumAimDistance,
                    definition.TargetLayerMask,
                    definition
                        .TargetQueryTriggerInteraction
                );

            if (hitCount <= 0)
            {
                ClearContactTarget();
                return;
            }

            if (
                !TryFindNearestDamageable(
                    hitCount,
                    out RaycastHit nearestHit,
                    out IDamageable damageable
                )
            )
            {
                ClearContactTarget();
                return;
            }

            bool contactTargetChanged =
                damageable
                != currentContactDamageable;

            currentContactDamageable =
                damageable;

            currentContactCollider =
                nearestHit.collider;

            currentHitPosition =
                nearestHit.point;

            currentHitNormal =
                nearestHit.normal.sqrMagnitude
                > 0.0001f
                    ? nearestHit.normal.normalized
                    : -currentAimRay.direction;

            hasContactTarget =
                true;

            contactTargetName =
                currentContactCollider != null
                    ? currentContactCollider.name
                    : string.Empty;

            if (contactTargetChanged)
            {
                damageTimer =
                    definition
                        .DamageImmediatelyOnContact
                            ? 0.0f
                            : definition.DamageInterval;
            }
            else
            {
                damageTimer -=
                    deltaTime;
            }

            if (damageTimer > 0.0f)
            {
                return;
            }

            bool damageApplied =
                ApplyContactDamage(
                    currentContactDamageable,
                    currentContactCollider,
                    currentHitPosition,
                    currentHitNormal
                );

            damageTimer =
                definition.DamageInterval;

            if (!damageApplied)
            {
                return;
            }

            RegisterChargeHit(
                currentContactDamageable,
                currentContactCollider
            );
        }

        private bool TryFindNearestDamageable(
            int hitCount,
            out RaycastHit nearestHit,
            out IDamageable nearestDamageable
        )
        {
            nearestHit =
                default;

            nearestDamageable =
                null;

            float nearestDistance =
                float.PositiveInfinity;

            for (
                int index = 0;
                index < hitCount;
                index++
            )
            {
                RaycastHit hit =
                    hitBuffer[index];

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
                    hit.distance
                    >= nearestDistance
                )
                {
                    continue;
                }

                nearestDistance =
                    hit.distance;

                nearestHit =
                    hit;

                nearestDamageable =
                    damageable;
            }

            return nearestDamageable != null;
        }

        private bool ApplyContactDamage(
            IDamageable damageable,
            Collider targetCollider,
            Vector3 hitPosition,
            Vector3 hitNormal
        )
        {
            if (damageable == null)
            {
                return false;
            }

            Vector3 damageDirection =
                currentAimRay.direction;

            if (
                damageDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                damageDirection =
                    -hitNormal;
            }

            DamageInfo damageInfo =
                new DamageInfo(
                    definition.Damage,
                    hitPosition,
                    damageDirection.normalized,
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
                Vector3 effectDirection =
                    hitNormal.sqrMagnitude
                    > 0.0001f
                        ? hitNormal.normalized
                        : Vector3.up;

                Quaternion effectRotation =
                    Quaternion.LookRotation(
                        effectDirection,
                        Vector3.up
                    );

                SpawnEffect(
                    definition.HitEffectPrefab,
                    hitPosition,
                    effectRotation,
                    definition.HitEffectLifetime
                );

                PlayRumble(
                    definition.ContactHitRumble
                );
            }

            if (definition.LogDamageDetection)
            {
                string targetName =
                    targetCollider != null
                        ? targetCollider.name
                        : "Unknown";

                Debug.Log(
                    $"[Mop Contact] "
                    + $"Target={targetName}, "
                    + $"Applied={damageApplied}, "
                    + $"Damage={definition.Damage}"
                );
            }

            return damageApplied;
        }

        private void RegisterChargeHit(
            IDamageable damageable,
            Collider targetCollider
        )
        {
            if (
                damageable == null
                ||
                targetCollider == null
            )
            {
                return;
            }

            if (
                isLockOnComplete
                &&
                definition.KeepTargetAfterLockOn
            )
            {
                return;
            }

            bool targetChanged =
                chargeTargetDamageable != null
                &&
                chargeTargetDamageable
                != damageable;

            if (targetChanged)
            {
                if (
                    !definition
                        .ChangeTargetBeforeLockOn
                )
                {
                    return;
                }

                ResetChargeState(
                    true
                );
            }

            if (
                chargeTargetDamageable == null
            )
            {
                chargeTargetDamageable =
                    damageable;

                chargeTargetCollider =
                    targetCollider;

                hasChargeTarget =
                    true;

                chargeTargetName =
                    targetCollider.name;
            }
            else
            {
                chargeTargetCollider =
                    targetCollider;
            }

            currentChargeCount =
                Mathf.Min(
                    currentChargeCount + 1,
                    definition.RequiredHitCount
                );

            UpdateNormalizedCharge();

            if (definition.LogLockOn)
            {
                Debug.Log(
                    $"[Mop Charge] "
                    + $"Target={chargeTargetName}, "
                    + $"Charge={currentChargeCount}"
                    + $"/{definition.RequiredHitCount}, "
                    + $"Normalized={normalizedCharge:F2}"
                );
            }

            if (
                currentChargeCount
                < definition.RequiredHitCount
            )
            {
                return;
            }

            if (isLockOnComplete)
            {
                return;
            }

            isLockOnComplete =
                true;

            LockOnCompleted?.Invoke(
                chargeTargetCollider
            );

            if (definition.LogLockOn)
            {
                Debug.Log(
                    $"[Mop Lock On] "
                    + $"Completed={chargeTargetName}"
                );
            }
        }

        private void HandleActivationReleased()
        {
            isAttacking = false;

            ClearContactTarget();

            if (
                isLockOnComplete
                &&
                chargeTargetCollider != null
            )
            {
                LaunchHomingProjectile();
                return;
            }

            ResetChargeState(
                true
            );
        }

        private void LaunchHomingProjectile()
        {
            MopHomingProjectile prefab =
                definition.HomingProjectilePrefab;

            if (prefab == null)
            {
                Debug.LogError(
                    "[Mop Weapon] "
                    + "Homing Projectile Prefabが"
                    + "設定されていません。",
                    this
                );

                ResetChargeState(
                    true
                );

                return;
            }

            if (chargeTargetCollider == null)
            {
                ResetChargeState(
                    true
                );

                return;
            }

            Transform launchPoint =
                mopLaunchPoint != null
                    ? mopLaunchPoint
                    : transform;

            Vector3 launchPosition =
                launchPoint.position;

            Vector3 targetPosition =
                chargeTargetCollider
                    .bounds
                    .center
                + definition.HomingTargetOffset;

            Vector3 launchDirection =
                targetPosition
                - launchPosition;

            if (
                launchDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                launchDirection =
                    launchPoint.forward;
            }

            Quaternion launchRotation =
                Quaternion.LookRotation(
                    launchDirection.normalized,
                    Vector3.up
                );

            MopHomingProjectile projectile =
                Instantiate(
                    prefab,
                    launchPosition,
                    launchRotation
                );

            activeProjectile =
                projectile;

            isProjectileActive =
                true;

            SetHeldMopVisible(
                false
            );

            projectile.Initialize(
                this,
                chargeTargetDamageable,
                chargeTargetCollider,
                launchDirection.normalized,
                definition.HomingSpeed,
                definition.HomingTurnSpeed,
                definition.HomingHitRadius,
                definition.HomingLifetime,
                definition.HomingCollisionLayerMask,
                definition
                    .HomingQueryTriggerInteraction,
                definition.HomingTargetOffset
            );

            SpawnEffect(
                definition.LaunchEffectPrefab,
                launchPosition,
                launchRotation,
                definition.LaunchEffectLifetime
            );

            PlayRumble(
                definition.HomingLaunchRumble
            );

            MopLaunched?.Invoke();

            if (definition.LogHomingAttack)
            {
                Debug.Log(
                    $"[Mop Launch] "
                    + $"Target={chargeTargetName}, "
                    + $"Position={launchPosition}"
                );
            }

            ResetChargeState(
                false
            );
        }

        public bool TryApplyHomingDamage(
            IDamageable expectedDamageable,
            Collider hitCollider,
            Vector3 hitPosition,
            Vector3 hitDirection
        )
        {
            if (
                definition == null
                ||
                expectedDamageable == null
            )
            {
                return false;
            }

            IDamageable damageable =
                expectedDamageable;

            if (
                hitCollider != null
                &&
                DamageableUtility
                    .TryGetDamageable(
                        hitCollider,
                        out IDamageable detected
                    )
            )
            {
                if (detected != expectedDamageable)
                {
                    return false;
                }

                damageable =
                    detected;
            }

            Vector3 direction =
                hitDirection.sqrMagnitude
                > 0.0001f
                    ? hitDirection.normalized
                    : transform.forward;

            DamageInfo damageInfo =
                new DamageInfo(
                    definition.HomingDamage,
                    hitPosition,
                    direction,
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
                Quaternion effectRotation =
                    Quaternion.LookRotation(
                        -direction,
                        Vector3.up
                    );

                SpawnEffect(
                    definition
                        .HomingHitEffectPrefab,
                    hitPosition,
                    effectRotation,
                    definition
                        .HomingHitEffectLifetime
                );
            }

            if (definition.LogHomingAttack)
            {
                Debug.Log(
                    $"[Mop Homing Hit] "
                    + $"Applied={damageApplied}, "
                    + $"Damage="
                    + $"{definition.HomingDamage}, "
                    + $"Position={hitPosition}"
                );
            }

            return damageApplied;
        }

        public void NotifyProjectileFinished(
            MopHomingProjectile projectile
        )
        {
            if (
                activeProjectile != projectile
            )
            {
                return;
            }

            activeProjectile =
                null;

            isProjectileActive =
                false;

            SetHeldMopVisible(
                true
            );

            MopProjectileFinished?.Invoke();

            if (
                definition != null
                &&
                definition.LogHomingAttack
            )
            {
                Debug.Log(
                    "[Mop Projectile] Finished"
                );
            }
        }

        private void ClearContactTarget()
        {
            hasContactTarget =
                false;

            currentContactDamageable =
                null;

            currentContactCollider =
                null;

            contactTargetName =
                string.Empty;

            damageTimer =
                0.0f;

            currentHitCount =
                0;
        }

        private void ResetChargeState(
            bool notifyCancellation
        )
        {
            bool hadCharge =
                chargeTargetDamageable != null
                ||
                currentChargeCount > 0
                ||
                isLockOnComplete;

            chargeTargetDamageable =
                null;

            chargeTargetCollider =
                null;

            currentChargeCount =
                0;

            normalizedCharge =
                0.0f;

            hasChargeTarget =
                false;

            isLockOnComplete =
                false;

            chargeTargetName =
                string.Empty;

            ChargeChanged?.Invoke(
                normalizedCharge
            );

            if (
                notifyCancellation
                &&
                hadCharge
            )
            {
                LockOnCancelled?.Invoke();
            }
        }

        private void UpdateNormalizedCharge()
        {
            normalizedCharge =
                definition.RequiredHitCount
                > 0
                    ? Mathf.Clamp01(
                        currentChargeCount
                        / (float)definition
                            .RequiredHitCount
                    )
                    : 0.0f;

            ChargeChanged?.Invoke(
                normalizedCharge
            );
        }

        private void CancelActiveProjectile()
        {
            if (activeProjectile == null)
            {
                SetHeldMopVisible(
                    true
                );

                return;
            }

            MopHomingProjectile projectile =
                activeProjectile;

            activeProjectile =
                null;

            isProjectileActive =
                false;

            projectile.CancelFromOwner();

            SetHeldMopVisible(
                true
            );
        }

        private void ResetAllState()
        {
            isAttacking =
                false;

            wasActivationActive =
                false;

            ClearContactTarget();

            ResetChargeState(
                true
            );

            SetHeldMopVisible(
                true
            );

            isProjectileActive =
                activeProjectile != null;
        }

        private void SetHeldMopVisible(
            bool visible
        )
        {
            if (heldMopVisual == null)
            {
                return;
            }

            heldMopVisual.SetActive(
                visible
            );
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

        private void PlayRumble(
            ControllerRumbleDefinition
                rumbleDefinition,
            float intensityMultiplier =
                1.0f
        )
        {
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
                rumbleDefinition,
                intensityMultiplier
            );
        }

        private void SpawnEffect(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            float lifetime
        )
        {
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

            if (lifetime > 0.0f)
            {
                Destroy(
                    instance,
                    lifetime
                );
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (
                definition == null
                ||
                !Application.isPlaying
            )
            {
                return;
            }

            Color previousColor =
                Gizmos.color;

            if (hasContactTarget)
            {
                Gizmos.color =
                    Color.green;

                Gizmos.DrawWireSphere(
                    currentHitPosition,
                    definition.PointerRadius
                );

                if (
                    definition.DrawHitPosition
                )
                {
                    Gizmos.color =
                        Color.magenta;

                    Gizmos.DrawSphere(
                        currentHitPosition,
                        0.1f
                    );

                    Gizmos.DrawLine(
                        currentHitPosition,
                        currentHitPosition
                        + currentHitNormal
                        * 0.5f
                    );
                }
            }

            if (chargeTargetCollider != null)
            {
                Gizmos.color =
                    isLockOnComplete
                        ? Color.red
                        : Color.yellow;

                Gizmos.DrawWireSphere(
                    chargeTargetCollider
                        .bounds
                        .center,
                    definition.HomingHitRadius
                );
            }

            Gizmos.color =
                previousColor;
        }
    }
}