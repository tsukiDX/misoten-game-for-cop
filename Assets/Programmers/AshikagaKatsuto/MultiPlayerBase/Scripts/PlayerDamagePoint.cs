using Game.Combat;
using Game.Enemies.Path;
using Game.Input.JSL;
using UnityEngine;

namespace Game.Enemies.Targeting
{
    [DisallowMultipleComponent]
    public sealed class PlayerDamagePoint :
        MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField]
        private LocalPlayerController
            ownerPlayer;

        [SerializeField]
        private PlayerHealth ownerHealth;

        [Header("Existing Damage EPP")]
        [SerializeField]
        private EnemyPathPoint
            enemyPathPoint;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isTargetable;

        public LocalPlayerController OwnerPlayer =>
            ownerPlayer;

        public PlayerHealth OwnerHealth =>
            ownerHealth;

        public EnemyPathPoint EnemyPathPoint =>
            enemyPathPoint;

        public Vector3 Position =>
            transform.position;

        public bool IsTargetable
        {
            get
            {
                RefreshTargetableState();

                return isTargetable;
            }
        }

        private void Awake()
        {
            ResolveReferences();

            RefreshTargetableState();
        }

        private void OnEnable()
        {
            ResolveReferences();

            RefreshTargetableState();
        }

        private void Update()
        {
            RefreshTargetableState();
        }

        private void OnValidate()
        {
            ResolveReferences();

            RefreshTargetableState();
        }

        private void ResolveReferences()
        {
            if (enemyPathPoint == null)
            {
                enemyPathPoint =
                    GetComponent<EnemyPathPoint>();
            }

            if (
                ownerHealth == null
                &&
                ownerPlayer != null
            )
            {
                ownerHealth =
                    ownerPlayer
                        .GetComponentInChildren<
                            PlayerHealth
                        >(
                            true
                        );
            }

            if (
                ownerPlayer == null
                &&
                ownerHealth != null
            )
            {
                ownerPlayer =
                    ownerHealth
                        .GetComponentInParent<
                            LocalPlayerController
                        >();
            }
        }

        private void RefreshTargetableState()
        {
            isTargetable =
                isActiveAndEnabled
                &&
                gameObject.activeInHierarchy
                &&
                ownerPlayer != null
                &&
                ownerPlayer.IsJoined
                &&
                ownerHealth != null
                &&
                ownerHealth.IsAlive
                &&
                (
                    enemyPathPoint == null
                    ||
                    enemyPathPoint.IsDamagePoint
                );
        }

        public bool ApplyEnemyDamage(
            float damage,
            Vector3 hitPosition,
            Vector3 hitDirection
        )
        {
            if (
                !IsTargetable
                ||
                ownerHealth == null
                ||
                damage <= 0.0f
            )
            {
                return false;
            }

            DamageInfo damageInfo =
                new DamageInfo(
                    damage,
                    hitPosition,
                    hitDirection,
                    attacker: null,
                    weapon: null
                );

            return ownerHealth.ApplyDamage(
                damageInfo
            );
        }
    }
}