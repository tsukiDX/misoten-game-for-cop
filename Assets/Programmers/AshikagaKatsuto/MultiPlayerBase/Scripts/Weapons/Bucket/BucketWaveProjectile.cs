using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Weapons.Bucket
{
    [RequireComponent(
        typeof(BoxCollider)
    )]
    [RequireComponent(
        typeof(Rigidbody)
    )]
    public sealed class BucketWaveProjectile :
        MonoBehaviour
    {
        [Header("References")]
        [Tooltip(
            "チャージ量に応じて拡大する"
            + "VFXのルートTransformです。"
            + "Projectile本体とは分けてください。"
        )]
        [SerializeField]
        private Transform visualRoot;

        [Tooltip(
            "波のダメージ判定に使用する"
            + "BoxColliderです。"
            + "未設定の場合は同じGameObjectから取得します。"
        )]
        [SerializeField]
        private BoxCollider hitBox;

        [Tooltip(
            "波を移動させるRigidbodyです。"
            + "未設定の場合は同じGameObjectから取得します。"
        )]
        [SerializeField]
        private Rigidbody waveRigidbody;

        [Header("Runtime Debug")]
        [SerializeField]
        private Vector3 movementDirection =
            Vector3.forward;

        [SerializeField]
        private Vector3 hitBoxSize =
            Vector3.one;

        [SerializeField]
        private Vector3 currentVisualScale =
            Vector3.one;

        [SerializeField]
        private float moveSpeed;

        [SerializeField]
        private float damage;

        [SerializeField]
        private float remainingLifetime;

        [SerializeField]
        private float elapsedTime;

        [SerializeField]
        private int damagedTargetCount;

        [SerializeField]
        private bool initialized;

        [SerializeField]
        private bool keepFixedGroundHeight;

        [SerializeField]
        private float fixedGroundHeight;

        [SerializeField]
        private LayerMask collisionLayerMask;

        [SerializeField]
        private QueryTriggerInteraction
            queryTriggerInteraction;

        private readonly HashSet<IDamageable>
            damagedTargets =
                new HashSet<IDamageable>();

        private BucketWeapon owner;

        public Vector3 HitBoxSize =>
            hitBoxSize;

        public Vector3 MovementDirection =>
            movementDirection;

        public Vector3 CurrentVisualScale =>
            currentVisualScale;

        public float MoveSpeed =>
            moveSpeed;

        public float Damage =>
            damage;

        public float RemainingLifetime =>
            remainingLifetime;

        public float ElapsedTime =>
            elapsedTime;

        public int DamagedTargetCount =>
            damagedTargetCount;

        public bool IsInitialized =>
            initialized;

        private void Reset()
        {
            ResolveReferences();
            ConfigureComponents();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureComponents();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ConfigureComponents();

            hitBoxSize =
                MakePositive(
                    hitBoxSize
                );

            if (hitBox != null)
            {
                hitBox.size =
                    hitBoxSize;
            }
        }

        private void ResolveReferences()
        {
            if (hitBox == null)
            {
                hitBox =
                    GetComponent<BoxCollider>();
            }

            if (waveRigidbody == null)
            {
                waveRigidbody =
                    GetComponent<Rigidbody>();
            }
        }

        private void ConfigureComponents()
        {
            if (hitBox != null)
            {
                hitBox.isTrigger =
                    true;
            }

            if (waveRigidbody == null)
            {
                return;
            }

            waveRigidbody.useGravity =
                false;

            waveRigidbody.isKinematic =
                true;

            waveRigidbody.interpolation =
                RigidbodyInterpolation.Interpolate;

            waveRigidbody.collisionDetectionMode =
                CollisionDetectionMode
                    .ContinuousSpeculative;

            waveRigidbody.constraints =
                RigidbodyConstraints.FreezeRotation;
        }

        public void Initialize(
            BucketWeapon newOwner,
            Vector3 direction,
            float speed,
            float lifetime,
            float projectileDamage,
            Vector3 newHitBoxSize,
            Vector3 visualScale,
            LayerMask newCollisionLayerMask,
            QueryTriggerInteraction
                newQueryTriggerInteraction,
            bool useFixedGroundHeight,
            float newGroundHeight,
            float newGroundOffset
        )
        {
            ResolveReferences();
            ConfigureComponents();

            owner =
                newOwner;

            Vector3 horizontalDirection =
                Vector3.ProjectOnPlane(
                    direction,
                    Vector3.up
                );

            if (
                horizontalDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                horizontalDirection =
                    Vector3.ProjectOnPlane(
                        transform.forward,
                        Vector3.up
                    );
            }

            if (
                horizontalDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                horizontalDirection =
                    Vector3.forward;
            }

            movementDirection =
                horizontalDirection.normalized;

            moveSpeed =
                Mathf.Max(
                    0.0f,
                    speed
                );

            remainingLifetime =
                Mathf.Max(
                    0.01f,
                    lifetime
                );

            damage =
                Mathf.Max(
                    0.0f,
                    projectileDamage
                );

            hitBoxSize =
                MakePositive(
                    newHitBoxSize
                );

            currentVisualScale =
                MakePositive(
                    visualScale
                );

            collisionLayerMask =
                newCollisionLayerMask;

            queryTriggerInteraction =
                newQueryTriggerInteraction;

            keepFixedGroundHeight =
                useFixedGroundHeight;

            fixedGroundHeight =
                newGroundHeight
                + newGroundOffset;

            transform.rotation =
                Quaternion.LookRotation(
                    movementDirection,
                    Vector3.up
                );

            if (keepFixedGroundHeight)
            {
                Vector3 position =
                    transform.position;

                position.y =
                    fixedGroundHeight;

                SetPositionImmediately(
                    position
                );
            }

            if (visualRoot != null)
            {
                visualRoot.localScale =
                    currentVisualScale;
            }

            if (hitBox != null)
            {
                hitBox.size =
                    hitBoxSize;

                hitBox.isTrigger =
                    true;

                hitBox.enabled =
                    true;
            }

            damagedTargets.Clear();

            damagedTargetCount =
                0;

            elapsedTime =
                0.0f;

            initialized =
                true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            float deltaTime =
                Time.deltaTime;

            remainingLifetime -=
                deltaTime;

            elapsedTime +=
                deltaTime;

            if (remainingLifetime <= 0.0f)
            {
                FinishProjectile();
            }
        }

        private void FixedUpdate()
        {
            if (!initialized)
            {
                return;
            }

            MoveProjectile(
                Time.fixedDeltaTime
            );
        }

        private void MoveProjectile(
            float deltaTime
        )
        {
            if (moveSpeed <= 0.0f)
            {
                return;
            }

            Vector3 currentPosition =
                waveRigidbody != null
                    ? waveRigidbody.position
                    : transform.position;

            Vector3 nextPosition =
                currentPosition
                + movementDirection
                * moveSpeed
                * deltaTime;

            if (keepFixedGroundHeight)
            {
                nextPosition.y =
                    fixedGroundHeight;
            }

            if (waveRigidbody != null)
            {
                waveRigidbody.MovePosition(
                    nextPosition
                );
            }
            else
            {
                transform.position =
                    nextPosition;
            }
        }

        private void OnTriggerEnter(
            Collider other
        )
        {
            if (
                !initialized
                ||
                other == null
            )
            {
                return;
            }

            if (
                !IsLayerIncluded(
                    other.gameObject.layer
                )
            )
            {
                return;
            }

            if (!ShouldAcceptCollider(
                other
            ))
            {
                return;
            }

            if (
                !DamageableUtility
                    .TryGetDamageable(
                        other,
                        out IDamageable damageable
                    )
            )
            {
                return;
            }

            if (
                damageable == null
                ||
                !damageable.IsAlive
            )
            {
                return;
            }

            if (
                damagedTargets.Contains(
                    damageable
                )
            )
            {
                return;
            }

            damagedTargets.Add(
                damageable
            );

            damagedTargetCount =
                damagedTargets.Count;

            Vector3 referencePosition =
                hitBox != null
                    ? hitBox.bounds.center
                    : transform.position;

            Vector3 hitPosition =
                other.ClosestPoint(
                    referencePosition
                );

            if (owner == null)
            {
                return;
            }

            owner.TryApplyWaveDamage(
                damageable,
                other,
                hitPosition,
                movementDirection,
                damage
            );
        }

        private bool ShouldAcceptCollider(
            Collider other
        )
        {
            if (other == null)
            {
                return false;
            }

            switch (queryTriggerInteraction)
            {
                case QueryTriggerInteraction.Ignore:
                    return !other.isTrigger;

                case QueryTriggerInteraction.Collide:
                    return true;

                case QueryTriggerInteraction.UseGlobal:
                default:
                    return
                        !other.isTrigger
                        ||
                        Physics.queriesHitTriggers;
            }
        }

        private bool IsLayerIncluded(
            int layer
        )
        {
            int layerBit =
                1 << layer;

            return
                (
                    collisionLayerMask.value
                    & layerBit
                )
                != 0;
        }

        private void SetPositionImmediately(
            Vector3 position
        )
        {
            transform.position =
                position;

            if (waveRigidbody != null)
            {
                waveRigidbody.position =
                    position;
            }
        }

        private void FinishProjectile()
        {
            if (!initialized)
            {
                return;
            }

            initialized =
                false;

            if (hitBox != null)
            {
                hitBox.enabled =
                    false;
            }

            Destroy(
                gameObject
            );
        }

        public void CancelProjectile()
        {
            if (initialized)
            {
                FinishProjectile();
                return;
            }

            Destroy(
                gameObject
            );
        }

        private void OnDisable()
        {
            initialized =
                false;

            damagedTargets.Clear();
        }

        private static Vector3 MakePositive(
            Vector3 value
        )
        {
            return new Vector3(
                Mathf.Max(
                    0.01f,
                    value.x
                ),
                Mathf.Max(
                    0.01f,
                    value.y
                ),
                Mathf.Max(
                    0.01f,
                    value.z
                )
            );
        }
    }
}