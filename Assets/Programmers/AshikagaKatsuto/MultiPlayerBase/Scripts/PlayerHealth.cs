using System;
using Game.Input.JSL;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth :
        MonoBehaviour,
        IDamageable
    {
        [Header("References")]
        [SerializeField]
        private LocalPlayerController
            localPlayer;

        [Header("Health")]
        [SerializeField]
        [Min(1.0f)]
        private float maximumHealth =
            100.0f;

        [SerializeField]
        [Min(1.0f)]
        private float reviveHealth =
            50.0f;

        [SerializeField]
        private bool restoreHealthOnEnable =
            true;

        [Header("Revive Shake")]
        [Tooltip(
            "復帰用の振り判定に使用する"
            + "加速度軸です。"
        )]
        [SerializeField]
        private Vector3 reviveShakeAxis =
            Vector3.right;

        [Tooltip(
            "1回の振りとして認識する"
            + "加速度の最低値です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float reviveShakeThreshold =
            1.5f;

        [Tooltip(
            "次の振りを受け付けるために、"
            + "加速度がこの値以下まで戻る"
            + "必要があります。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float reviveShakeResetThreshold =
            0.4f;

        [Tooltip(
            "復帰に必要な振り回数です。"
        )]
        [SerializeField]
        [Min(1)]
        private int requiredReviveShakeCount =
            5;

        [Header("Damage Effects")]
        [SerializeField]
        private GameObject damageEffectPrefab;

        [SerializeField]
        private Transform damageEffectRoot;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onDamaged;

        [SerializeField]
        private UnityEvent onIncapacitated;

        [SerializeField]
        private UnityEvent onRevived;

        [SerializeField]
        private UnityEvent onReviveShakeAccepted;

        [Header("Runtime")]
        [SerializeField]
        private float currentHealth;

        [SerializeField]
        private bool isIncapacitated;

        [SerializeField]
        private float currentShakeValue;

        [SerializeField]
        private bool shakeReady =
            true;

        [SerializeField]
        private int currentReviveShakeCount;

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
            !isIncapacitated
            &&
            currentHealth > 0.0f;

        public bool IsIncapacitated =>
            isIncapacitated;

        public int CurrentReviveShakeCount =>
            currentReviveShakeCount;

        public int RequiredReviveShakeCount =>
            requiredReviveShakeCount;

        public event Action<
            PlayerHealth,
            DamageInfo
        > Damaged;

        public event Action<
            PlayerHealth,
            DamageInfo
        > Incapacitated;

        public event Action<
            PlayerHealth
        > Revived;

        public event Action<
            PlayerHealth,
            int,
            int
        > ReviveProgressChanged;

        private void Awake()
        {
            ResolveReferences();

            ResetHealth();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (restoreHealthOnEnable)
            {
                ResetHealth();
            }
        }

        private void Update()
        {
            if (!isIncapacitated)
            {
                return;
            }

            UpdateReviveShake();
        }

        private void ResolveReferences()
        {
            if (localPlayer == null)
            {
                localPlayer =
                    GetComponentInParent<
                        LocalPlayerController
                    >();
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
                EnterIncapacitatedState(
                    damageInfo
                );
            }

            return true;
        }

        public void ResetHealth()
        {
            currentHealth =
                maximumHealth;

            isIncapacitated =
                false;

            currentShakeValue =
                0.0f;

            shakeReady =
                true;

            currentReviveShakeCount =
                0;

            if (localPlayer != null)
            {
                localPlayer.SetActionDisabled(
                    false
                );
            }

            ReviveProgressChanged?.Invoke(
                this,
                currentReviveShakeCount,
                requiredReviveShakeCount
            );
        }

        private void EnterIncapacitatedState(
            DamageInfo damageInfo
        )
        {
            if (isIncapacitated)
            {
                return;
            }

            isIncapacitated =
                true;

            currentHealth =
                0.0f;

            currentShakeValue =
                0.0f;

            shakeReady =
                true;

            currentReviveShakeCount =
                0;

            if (localPlayer != null)
            {
                localPlayer.SetActionDisabled(
                    true
                );
            }

            onIncapacitated?.Invoke();

            Incapacitated?.Invoke(
                this,
                damageInfo
            );

            ReviveProgressChanged?.Invoke(
                this,
                currentReviveShakeCount,
                requiredReviveShakeCount
            );
        }

        private void UpdateReviveShake()
        {
            if (
                localPlayer == null
                ||
                !localPlayer.IsJoined
                ||
                !localPlayer.HasController
                ||
                localPlayer.Controller == null
            )
            {
                currentShakeValue =
                    0.0f;

                return;
            }

            Vector3 acceleration =
                localPlayer
                    .Controller
                    .CurrentInput
                    .Acceleration;

            Vector3 axis =
                reviveShakeAxis
                    .sqrMagnitude
                > 0.0001f
                    ? reviveShakeAxis
                        .normalized
                    : Vector3.right;

            currentShakeValue =
                Vector3.Dot(
                    acceleration,
                    axis
                );

            float absoluteShakeValue =
                Mathf.Abs(
                    currentShakeValue
                );

            if (
                !shakeReady
                &&
                absoluteShakeValue
                <= reviveShakeResetThreshold
            )
            {
                shakeReady =
                    true;
            }

            if (!shakeReady)
            {
                return;
            }

            if (
                absoluteShakeValue
                < reviveShakeThreshold
            )
            {
                return;
            }

            shakeReady =
                false;

            currentReviveShakeCount =
                Mathf.Min(
                    requiredReviveShakeCount,
                    currentReviveShakeCount + 1
                );

            onReviveShakeAccepted?.Invoke();

            ReviveProgressChanged?.Invoke(
                this,
                currentReviveShakeCount,
                requiredReviveShakeCount
            );

            if (
                currentReviveShakeCount
                >= requiredReviveShakeCount
            )
            {
                Revive();
            }
        }

        public void Revive()
        {
            if (!isIncapacitated)
            {
                return;
            }

            isIncapacitated =
                false;

            currentHealth =
                Mathf.Clamp(
                    reviveHealth,
                    1.0f,
                    maximumHealth
                );

            currentShakeValue =
                0.0f;

            shakeReady =
                true;

            currentReviveShakeCount =
                0;

            if (localPlayer != null)
            {
                localPlayer.SetActionDisabled(
                    false
                );
            }

            onRevived?.Invoke();

            Revived?.Invoke(
                this
            );

            ReviveProgressChanged?.Invoke(
                this,
                currentReviveShakeCount,
                requiredReviveShakeCount
            );
        }

        public void RestoreHealth(
            float amount
        )
        {
            if (
                amount <= 0.0f
                ||
                isIncapacitated
            )
            {
                return;
            }

            currentHealth =
                Mathf.Clamp(
                    currentHealth + amount,
                    0.0f,
                    maximumHealth
                );
        }

        private void SpawnDamageEffect(
            Vector3 position
        )
        {
            if (damageEffectPrefab == null)
            {
                return;
            }

            Instantiate(
                damageEffectPrefab,
                position,
                Quaternion.identity,
                damageEffectRoot
            );
        }

        private void OnValidate()
        {
            maximumHealth =
                Mathf.Max(
                    1.0f,
                    maximumHealth
                );

            reviveHealth =
                Mathf.Clamp(
                    reviveHealth,
                    1.0f,
                    maximumHealth
                );

            reviveShakeThreshold =
                Mathf.Max(
                    0.0f,
                    reviveShakeThreshold
                );

            reviveShakeResetThreshold =
                Mathf.Clamp(
                    reviveShakeResetThreshold,
                    0.0f,
                    reviveShakeThreshold
                );

            requiredReviveShakeCount =
                Mathf.Max(
                    1,
                    requiredReviveShakeCount
                );
        }
    }
}