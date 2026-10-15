using Game.Combat;
using UnityEngine;

namespace Game.Weapons.Mop
{
    public sealed class MopHomingProjectile :
        MonoBehaviour
    {
        private const int HitBufferSize =
            16;

        [Header("Visual Rotation")]
        [Tooltip(
            "投射モデルの向きが進行方向と合わない"
            + "場合に使用する回転補正です。"
        )]
        [SerializeField]
        private Vector3 rotationOffsetEuler;

        [Tooltip(
            "飛行中にモデルを回転させる速度です。"
            + "0なら回転させません。"
        )]
        [SerializeField]
        private float visualSpinSpeed =
            720.0f;

        [Tooltip(
            "回転させるローカル軸です。"
        )]
        [SerializeField]
        private Vector3 visualSpinAxis =
            Vector3.forward;

        [Tooltip(
            "回転させるモデルのTransformです。"
            + "未設定の場合は回転演出を行いません。"
        )]
        [SerializeField]
        private Transform spinningVisual;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool initialized;

        [SerializeField]
        private bool finished;

        [SerializeField]
        private Vector3 moveDirection =
            Vector3.forward;

        [SerializeField]
        private float moveSpeed;

        [SerializeField]
        private float turnSpeed;

        [SerializeField]
        private float hitRadius;

        [SerializeField]
        private float remainingLifetime;

        [SerializeField]
        private Vector3 currentTargetPosition;

        [SerializeField]
        private string targetName;

        private readonly RaycastHit[] hitBuffer =
            new RaycastHit[HitBufferSize];

        private MopWeapon owner;

        private IDamageable targetDamageable;

        private Collider targetCollider;

        private LayerMask collisionLayerMask;

        private QueryTriggerInteraction
            queryTriggerInteraction;

        private Vector3 targetOffset;

        private Quaternion rotationOffset =
            Quaternion.identity;

        public void Initialize(
            MopWeapon projectileOwner,
            IDamageable newTargetDamageable,
            Collider newTargetCollider,
            Vector3 initialDirection,
            float speed,
            float maximumTurnSpeed,
            float collisionRadius,
            float lifetime,
            LayerMask layerMask,
            QueryTriggerInteraction
                triggerInteraction,
            Vector3 newTargetOffset
        )
        {
            owner =
                projectileOwner;

            targetDamageable =
                newTargetDamageable;

            targetCollider =
                newTargetCollider;

            moveDirection =
                initialDirection.sqrMagnitude
                > 0.0001f
                    ? initialDirection.normalized
                    : transform.forward;

            moveSpeed =
                Mathf.Max(
                    0.1f,
                    speed
                );

            turnSpeed =
                Mathf.Max(
                    0.0f,
                    maximumTurnSpeed
                );

            hitRadius =
                Mathf.Max(
                    0.01f,
                    collisionRadius
                );

            remainingLifetime =
                Mathf.Max(
                    0.1f,
                    lifetime
                );

            collisionLayerMask =
                layerMask;

            queryTriggerInteraction =
                triggerInteraction;

            targetOffset =
                newTargetOffset;

            rotationOffset =
                Quaternion.Euler(
                    rotationOffsetEuler
                );

            targetName =
                targetCollider != null
                    ? targetCollider.name
                    : string.Empty;

            UpdateTargetPosition();

            UpdateRootRotation();

            initialized =
                owner != null
                &&
                targetDamageable != null
                &&
                targetCollider != null;
        }

        private void Update()
        {
            if (
                !initialized
                ||
                finished
            )
            {
                return;
            }

            float deltaTime =
                Time.deltaTime;

            remainingLifetime -=
                deltaTime;

            if (remainingLifetime <= 0.0f)
            {
                FinishProjectile();
                return;
            }

            if (
                targetCollider == null
                ||
                targetDamageable == null
            )
            {
                FinishProjectile();
                return;
            }

            UpdateTargetPosition();

            UpdateHomingDirection(
                deltaTime
            );

            UpdateRootRotation();

            UpdateVisualSpin(
                deltaTime
            );

            MoveProjectile(
                deltaTime
            );
        }

        private void UpdateTargetPosition()
        {
            if (targetCollider == null)
            {
                return;
            }

            currentTargetPosition =
                targetCollider.bounds.center
                + targetOffset;
        }

        private void UpdateHomingDirection(
            float deltaTime
        )
        {
            Vector3 desiredDirection =
                currentTargetPosition
                - transform.position;

            if (
                desiredDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                return;
            }

            desiredDirection.Normalize();

            if (turnSpeed <= 0.0f)
            {
                moveDirection =
                    desiredDirection;

                return;
            }

            moveDirection =
                Vector3.RotateTowards(
                    moveDirection,
                    desiredDirection,
                    Mathf.Deg2Rad
                    * turnSpeed
                    * deltaTime,
                    0.0f
                ).normalized;
        }

        private void UpdateRootRotation()
        {
            if (
                moveDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                return;
            }

            transform.rotation =
                Quaternion.LookRotation(
                    moveDirection,
                    Vector3.up
                )
                * rotationOffset;
        }

        private void UpdateVisualSpin(
            float deltaTime
        )
        {
            if (
                spinningVisual == null
                ||
                Mathf.Abs(visualSpinSpeed)
                <= 0.0001f
            )
            {
                return;
            }

            Vector3 spinAxis =
                visualSpinAxis.sqrMagnitude
                > 0.0001f
                    ? visualSpinAxis.normalized
                    : Vector3.forward;

            spinningVisual.Rotate(
                spinAxis,
                visualSpinSpeed
                * deltaTime,
                Space.Self
            );
        }

        private void MoveProjectile(
            float deltaTime
        )
        {
            float moveDistance =
                moveSpeed
                * deltaTime;

            if (moveDistance <= 0.0f)
            {
                return;
            }

            if (
                TryFindNearestHit(
                    moveDistance,
                    out RaycastHit nearestHit
                )
            )
            {
                transform.position =
                    nearestHit.point;

                HandleCollision(
                    nearestHit
                );

                return;
            }

            float targetDistance =
                Vector3.Distance(
                    transform.position,
                    currentTargetPosition
                );

            if (
                targetDistance
                <= hitRadius + moveDistance
            )
            {
                transform.position =
                    currentTargetPosition;

                HandleTargetReached();

                return;
            }

            transform.position +=
                moveDirection
                * moveDistance;
        }

        private bool TryFindNearestHit(
            float distance,
            out RaycastHit nearestHit
        )
        {
            nearestHit =
                default;

            int hitCount =
                Physics.SphereCastNonAlloc(
                    transform.position,
                    hitRadius,
                    moveDirection,
                    hitBuffer,
                    distance,
                    collisionLayerMask,
                    queryTriggerInteraction
                );

            if (hitCount <= 0)
            {
                return false;
            }

            float nearestDistance =
                float.PositiveInfinity;

            bool found =
                false;

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
                    owner != null
                    &&
                    hit.collider.transform
                        .IsChildOf(
                            owner.transform.root
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

                found =
                    true;
            }

            return found;
        }

        private void HandleCollision(
            RaycastHit hit
        )
        {
            if (
                hit.collider == null
            )
            {
                FinishProjectile();
                return;
            }

            if (
                DamageableUtility
                    .TryGetDamageable(
                        hit.collider,
                        out IDamageable hitDamageable
                    )
                &&
                hitDamageable
                == targetDamageable
            )
            {
                ApplyTargetDamage(
                    hit.collider,
                    hit.point
                );

                FinishProjectile();
                return;
            }

            // 壁や別の敵に衝突した場合は、
            // ダメージを与えず投射物を終了する。
            FinishProjectile();
        }

        private void HandleTargetReached()
        {
            Vector3 hitPosition =
                targetCollider != null
                    ? targetCollider
                        .ClosestPoint(
                            transform.position
                        )
                    : currentTargetPosition;

            ApplyTargetDamage(
                targetCollider,
                hitPosition
            );

            FinishProjectile();
        }

        private void ApplyTargetDamage(
            Collider hitCollider,
            Vector3 hitPosition
        )
        {
            if (owner == null)
            {
                return;
            }

            owner.TryApplyHomingDamage(
                targetDamageable,
                hitCollider,
                hitPosition,
                moveDirection
            );
        }

        public void CancelFromOwner()
        {
            if (finished)
            {
                return;
            }

            finished = true;

            owner = null;

            Destroy(
                gameObject
            );
        }

        private void FinishProjectile()
        {
            if (finished)
            {
                return;
            }

            finished = true;

            MopWeapon previousOwner =
                owner;

            owner =
                null;

            if (previousOwner != null)
            {
                previousOwner
                    .NotifyProjectileFinished(
                        this
                    );
            }

            Destroy(
                gameObject
            );
        }

        private void OnDestroy()
        {
            if (
                finished
                ||
                owner == null
            )
            {
                return;
            }

            MopWeapon previousOwner =
                owner;

            owner =
                null;

            finished =
                true;

            previousOwner
                .NotifyProjectileFinished(
                    this
                );
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                Color.cyan;

            Gizmos.DrawWireSphere(
                transform.position,
                Mathf.Max(
                    0.01f,
                    hitRadius
                )
            );

            if (
                Application.isPlaying
                &&
                targetCollider != null
            )
            {
                Gizmos.color =
                    Color.red;

                Gizmos.DrawLine(
                    transform.position,
                    currentTargetPosition
                );

                Gizmos.DrawWireSphere(
                    currentTargetPosition,
                    Mathf.Max(
                        0.01f,
                        hitRadius
                    )
                );
            }

            Gizmos.color =
                previousColor;
        }

        private void OnValidate()
        {
            hitRadius =
                Mathf.Max(
                    0.01f,
                    hitRadius
                );
        }
    }
}