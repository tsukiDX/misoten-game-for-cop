using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth :
        MonoBehaviour,
        IDamageable
    {
        [Header("Health")]
        [SerializeField]
        [Min(1.0f)]
        private float maximumHealth =
            100.0f;

        [SerializeField]
        private bool restoreHealthOnEnable =
            true;

        [Header("Damage Effects")]
        [SerializeField]
        private GameObject damageEffectPrefab;

        [SerializeField]
        private Transform damageEffectRoot;

        [Header("Defeat")]
        [SerializeField]
        private GameObject defeatEffectPrefab;

        [SerializeField]
        private bool destroyOnDefeat =
            true;

        [SerializeField]
        [Min(0.0f)]
        private float destroyDelay =
            0.0f;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onDamaged;

        [SerializeField]
        private UnityEvent onDefeated;

        private float currentHealth;

        private bool isDefeated;

        public float MaximumHealth =>
            maximumHealth;

        public float CurrentHealth =>
            currentHealth;

        public float NormalizedHealth =>
            maximumHealth > 0.0f
                ? currentHealth
                    / maximumHealth
                : 0.0f;

        public bool IsAlive =>
            !isDefeated
            &&
            currentHealth > 0.0f;

        public event Action<
            EnemyHealth,
            DamageInfo
        > Damaged;

        public event Action<
            EnemyHealth,
            DamageInfo
        > Defeated;

        private void Awake()
        {
            ResetHealth();
        }

        private void OnEnable()
        {
            if (restoreHealthOnEnable)
            {
                ResetHealth();
            }
        }

        public bool ApplyDamage(
            DamageInfo damageInfo
        )
        {
            if (!IsAlive)
            {
                return false;
            }

            if (damageInfo.Damage <= 0.0f)
            {
                return false;
            }

            currentHealth =
                Mathf.Max(
                    0.0f,
                    currentHealth
                    - damageInfo.Damage
                );

            SpawnDamageEffect(
                damageInfo.HitPosition
            );

            onDamaged?.Invoke();

            Damaged?.Invoke(
                this,
                damageInfo
            );

            if (currentHealth <= 0.0f)
            {
                Defeat(
                    damageInfo
                );
            }

            return true;
        }

        public void ResetHealth()
        {
            currentHealth =
                maximumHealth;

            isDefeated =
                false;
        }

        private void Defeat(
            DamageInfo damageInfo
        )
        {
            if (isDefeated)
            {
                return;
            }

            isDefeated =
                true;

            currentHealth =
                0.0f;

            SpawnDefeatEffect();

            onDefeated?.Invoke();

            Defeated?.Invoke(
                this,
                damageInfo
            );

            if (destroyOnDefeat)
            {
                Destroy(
                    gameObject,
                    destroyDelay
                );
            }
        }

        private void SpawnDamageEffect(
            Vector3 position
        )
        {
            if (damageEffectPrefab == null)
            {
                return;
            }

            Transform effectParent =
                damageEffectRoot;

            Instantiate(
                damageEffectPrefab,
                position,
                Quaternion.identity,
                effectParent
            );
        }

        private void SpawnDefeatEffect()
        {
            if (defeatEffectPrefab == null)
            {
                return;
            }

            Instantiate(
                defeatEffectPrefab,
                transform.position,
                transform.rotation
            );
        }

        private void OnValidate()
        {
            maximumHealth =
                Mathf.Max(
                    1.0f,
                    maximumHealth
                );

            destroyDelay =
                Mathf.Max(
                    0.0f,
                    destroyDelay
                );
        }
    }
}