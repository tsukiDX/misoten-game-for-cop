using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Weapons.Spray
{
    [RequireComponent(
        typeof(CapsuleCollider)
    )]
    [RequireComponent(
        typeof(Rigidbody)
    )]
    public sealed class SprayBeamDamageArea :
        MonoBehaviour
    {
        private sealed class TargetContactState
        {
            public IDamageable Damageable;

            public Collider RepresentativeCollider;

            public int ContactCount;
        }

        [Header("References")]
        [Tooltip(
            "泡ビームのParticle Systemです。"
            + "未設定の場合は子から検索します。"
        )]
        [SerializeField]
        private ParticleSystem beamParticleSystem;

        [Tooltip(
            "泡ビームの範囲を表すCapsuleColliderです。"
            + "未設定の場合は同じGameObjectから取得します。"
        )]
        [SerializeField]
        private CapsuleCollider beamCollider;

        [Tooltip(
            "Triggerイベントを発生させるRigidbodyです。"
            + "未設定の場合は同じGameObjectから取得します。"
        )]
        [SerializeField]
        private Rigidbody beamRigidbody;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool initialized;

        [SerializeField]
        private bool isBeamActive;

        [SerializeField]
        private float damage;

        [SerializeField]
        private float damageInterval;

        [SerializeField]
        private float damageTimer;

        [SerializeField]
        private int activeTargetCount;

        [SerializeField]
        private int lastDamagedTargetCount;

        [SerializeField]
        private LayerMask targetLayerMask;

        [SerializeField]
        private QueryTriggerInteraction
            queryTriggerInteraction =
                QueryTriggerInteraction.Collide;

        private readonly Dictionary<
            IDamageable,
            TargetContactState
        > targetStates =
            new Dictionary<
                IDamageable,
                TargetContactState
            >();

        private readonly List<IDamageable>
            removeTargets =
                new List<IDamageable>();

        private SprayWeapon owner;

        public bool IsBeamActive =>
            isBeamActive;

        public int ActiveTargetCount =>
            activeTargetCount;

        public int LastDamagedTargetCount =>
            lastDamagedTargetCount;

        public float Damage =>
            damage;

        public float DamageInterval =>
            damageInterval;

        private void Reset()
        {
            ResolveReferences();
            ConfigurePhysics();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigurePhysics();

            SetBeamActive(
                false
            );
        }

        private void OnValidate()
        {
            ResolveReferences();
            ConfigurePhysics();
        }

        private void ResolveReferences()
        {
            if (beamCollider == null)
            {
                beamCollider =
                    GetComponent<CapsuleCollider>();
            }

            if (beamRigidbody == null)
            {
                beamRigidbody =
                    GetComponent<Rigidbody>();
            }

            if (beamParticleSystem == null)
            {
                beamParticleSystem =
                    GetComponentInChildren<
                        ParticleSystem
                    >(
                        true
                    );
            }
        }

        private void ConfigurePhysics()
        {
            if (beamCollider != null)
            {
                beamCollider.isTrigger =
                    true;
            }

            if (beamRigidbody == null)
            {
                return;
            }

            beamRigidbody.useGravity =
                false;

            beamRigidbody.isKinematic =
                true;

            beamRigidbody.interpolation =
                RigidbodyInterpolation.None;

            beamRigidbody
                .collisionDetectionMode =
                    CollisionDetectionMode
                        .ContinuousSpeculative;

            beamRigidbody.constraints =
                RigidbodyConstraints.FreezeAll;
        }

        public void Initialize(
            SprayWeapon newOwner,
            float beamDamage,
            float interval,
            LayerMask newTargetLayerMask,
            QueryTriggerInteraction
                newQueryTriggerInteraction,
            bool damageImmediately
        )
        {
            ResolveReferences();
            ConfigurePhysics();

            owner =
                newOwner;

            damage =
                Mathf.Max(
                    0.0f,
                    beamDamage
                );

            damageInterval =
                Mathf.Max(
                    0.01f,
                    interval
                );

            targetLayerMask =
                newTargetLayerMask;

            queryTriggerInteraction =
                newQueryTriggerInteraction;

            damageTimer =
                damageImmediately
                    ? 0.0f
                    : damageInterval;

            targetStates.Clear();
            removeTargets.Clear();

            activeTargetCount =
                0;

            lastDamagedTargetCount =
                0;

            initialized =
                true;

            SetBeamActive(
                true
            );
        }

        private void Update()
        {
            if (
                !initialized
                ||
                !isBeamActive
            )
            {
                return;
            }

            CleanupInvalidTargets();

            damageTimer -=
                Time.deltaTime;

            if (damageTimer > 0.0f)
            {
                return;
            }

            ApplyDamageTick();

            damageTimer =
                damageInterval;
        }

        private void OnTriggerEnter(
            Collider other
        )
        {
            RegisterCollider(
                other
            );
        }

        private void OnTriggerStay(
            Collider other
        )
        {
            if (
                !initialized
                ||
                !isBeamActive
                ||
                other == null
            )
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
                targetStates.ContainsKey(
                    damageable
                )
            )
            {
                return;
            }

            RegisterCollider(
                other
            );
        }

        private void OnTriggerExit(
            Collider other
        )
        {
            UnregisterCollider(
                other
            );
        }

        private void RegisterCollider(
            Collider other
        )
        {
            if (
                !initialized
                ||
                !isBeamActive
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
                targetStates.TryGetValue(
                    damageable,
                    out TargetContactState state
                )
            )
            {
                state.ContactCount++;

                if (
                    state.RepresentativeCollider
                    == null
                )
                {
                    state.RepresentativeCollider =
                        other;
                }

                return;
            }

            TargetContactState newState =
                new TargetContactState
                {
                    Damageable =
                        damageable,

                    RepresentativeCollider =
                        other,

                    ContactCount =
                        1
                };

            targetStates.Add(
                damageable,
                newState
            );

            activeTargetCount =
                targetStates.Count;
        }

        private void UnregisterCollider(
            Collider other
        )
        {
            if (
                other == null
                ||
                !DamageableUtility
                    .TryGetDamageable(
                        other,
                        out IDamageable damageable
                    )
                ||
                damageable == null
            )
            {
                return;
            }

            if (
                !targetStates.TryGetValue(
                    damageable,
                    out TargetContactState state
                )
            )
            {
                return;
            }

            state.ContactCount =
                Mathf.Max(
                    0,
                    state.ContactCount - 1
                );

            if (state.ContactCount > 0)
            {
                if (
                    state.RepresentativeCollider
                    == other
                )
                {
                    state.RepresentativeCollider =
                        null;
                }

                return;
            }

            targetStates.Remove(
                damageable
            );

            activeTargetCount =
                targetStates.Count;
        }

        private void ApplyDamageTick()
        {
            if (
                owner == null
                ||
                targetStates.Count == 0
            )
            {
                lastDamagedTargetCount =
                    0;

                return;
            }

            lastDamagedTargetCount =
                0;

            removeTargets.Clear();

            foreach (
                KeyValuePair<
                    IDamageable,
                    TargetContactState
                > pair
                in targetStates
            )
            {
                TargetContactState state =
                    pair.Value;

                if (
                    state == null
                    ||
                    state.Damageable == null
                    ||
                    !state.Damageable.IsAlive
                )
                {
                    removeTargets.Add(
                        pair.Key
                    );

                    continue;
                }

                Collider targetCollider =
                    state.RepresentativeCollider;

                Vector3 hitPosition;

                if (targetCollider != null)
                {
                    Vector3 referencePosition =
                        beamCollider != null
                            ? beamCollider
                                .bounds
                                .center
                            : transform.position;

                    hitPosition =
                        targetCollider
                            .ClosestPoint(
                                referencePosition
                            );
                }
                else
                {
                    hitPosition =
                        transform.position
                        + transform.forward;
                }

                bool damageApplied =
                    owner.TryApplyBeamDamage(
                        state.Damageable,
                        targetCollider,
                        hitPosition,
                        transform.forward,
                        damage
                    );

                if (damageApplied)
                {
                    lastDamagedTargetCount++;
                }
            }

            for (
                int index = 0;
                index < removeTargets.Count;
                index++
            )
            {
                targetStates.Remove(
                    removeTargets[index]
                );
            }

            activeTargetCount =
                targetStates.Count;

            if (lastDamagedTargetCount > 0)
            {
                owner.NotifyBeamDamageTick();
            }
        }

        private void CleanupInvalidTargets()
        {
            if (targetStates.Count == 0)
            {
                activeTargetCount =
                    0;

                return;
            }

            removeTargets.Clear();

            foreach (
                KeyValuePair<
                    IDamageable,
                    TargetContactState
                > pair
                in targetStates
            )
            {
                TargetContactState state =
                    pair.Value;

                if (
                    state == null
                    ||
                    state.Damageable == null
                    ||
                    !state.Damageable.IsAlive
                    ||
                    state.ContactCount <= 0
                )
                {
                    removeTargets.Add(
                        pair.Key
                    );
                }
            }

            for (
                int index = 0;
                index < removeTargets.Count;
                index++
            )
            {
                targetStates.Remove(
                    removeTargets[index]
                );
            }

            activeTargetCount =
                targetStates.Count;
        }

        public void SetBeamActive(
            bool active
        )
        {
            isBeamActive =
                active;

            if (beamCollider != null)
            {
                beamCollider.enabled =
                    active;
            }

            if (beamParticleSystem != null)
            {
                if (active)
                {
                    if (
                        !beamParticleSystem
                            .isPlaying
                    )
                    {
                        beamParticleSystem.Play(
                            true
                        );
                    }
                }
                else
                {
                    beamParticleSystem.Stop(
                        true,
                        ParticleSystemStopBehavior
                            .StopEmittingAndClear
                    );
                }
            }

            if (active)
            {
                return;
            }

            targetStates.Clear();
            removeTargets.Clear();

            activeTargetCount =
                0;

            lastDamagedTargetCount =
                0;
        }

        public void StopBeam()
        {
            initialized =
                false;

            SetBeamActive(
                false
            );
        }

        private bool IsLayerIncluded(
            int layer
        )
        {
            int layerBit =
                1 << layer;

            return
                (
                    targetLayerMask.value
                    & layerBit
                )
                != 0;
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

        private void OnDisable()
        {
            initialized =
                false;

            targetStates.Clear();
            removeTargets.Clear();

            activeTargetCount =
                0;

            lastDamagedTargetCount =
                0;
        }
    }
}