using System;
using Game.Combat;
using Game.Input.JSL;
using UnityEngine;

namespace Game.Weapons
{
    public abstract class LocalPlayerWeapon :
        MonoBehaviour
    {
        [Header("Runtime References")]
        [Tooltip(
            "この武器を所有している"
            + "LocalPlayerです。"
        )]
        [SerializeField]
        private LocalPlayerController localPlayer;

        [Tooltip(
            "このプレイヤーに対応する"
            + "ジャイロポインターです。"
        )]
        [SerializeField]
        private JslGyroPointer gyroPointer;

        [Header("Equipment")]
        [Tooltip(
            "シーンへ直接配置したテスト用武器を"
            + "Start時に自動装備します。"
        )]
        [SerializeField]
        private bool autoEquipOnStart = true;

        private bool isEquipped;

        protected LocalPlayerController LocalPlayer =>
            localPlayer;

        protected JslGyroPointer GyroPointer =>
            gyroPointer;

        protected WeaponActivationState Activation
        {
            get;
            private set;
        }

        public LocalPlayerController Owner =>
            localPlayer;

        public bool IsEquipped =>
            isEquipped;

        public abstract WeaponDefinition Definition
        {
            get;
        }

        public event Action<
            LocalPlayerWeapon,
            IDamageable,
            DamageInfo
        > DamageDealt;

        protected virtual void Start()
        {
            if (!autoEquipOnStart)
            {
                return;
            }

            Equip();
        }

        protected virtual void Update()
        {
            if (!isEquipped)
            {
                return;
            }

            if (
                localPlayer == null
                ||
                !localPlayer.IsInputAvailable
            )
            {
                Activation =
                    WeaponActivationState.Inactive;

                OnInputUnavailable();

                return;
            }

            WeaponDefinition definition =
                Definition;

            if (definition == null)
            {
                Activation =
                    WeaponActivationState.Inactive;

                return;
            }

            Activation =
                definition
                    .ActivationSettings
                    .Evaluate(
                        localPlayer
                    );

            TickWeapon(
                Time.deltaTime,
                Activation
            );
        }

        public void Initialize(
            LocalPlayerController owner,
            JslGyroPointer ownerPointer
        )
        {
            localPlayer =
                owner;

            gyroPointer =
                ownerPointer;
        }

        public void Equip()
        {
            if (isEquipped)
            {
                return;
            }

            if (localPlayer == null)
            {
                Debug.LogError(
                    $"{name}: LocalPlayerが"
                    + "設定されていません。"
                );

                return;
            }

            if (Definition == null)
            {
                Debug.LogError(
                    $"{name}: WeaponDefinitionが"
                    + "設定されていません。"
                );

                return;
            }

            isEquipped =
                true;

            ApplyGyroAimSettings();

            OnEquipped();
        }

        public void Unequip()
        {
            if (!isEquipped)
            {
                return;
            }

            isEquipped =
                false;

            Activation =
                WeaponActivationState.Inactive;

            OnUnequipped();
        }

        protected bool TryApplyDamage(
            IDamageable damageable,
            DamageInfo damageInfo
        )
        {
            if (damageable == null)
            {
                return false;
            }

            if (!damageable.IsAlive)
            {
                return false;
            }

            bool damageApplied =
                damageable.ApplyDamage(
                    damageInfo
                );

            if (!damageApplied)
            {
                return false;
            }

            DamageDealt?.Invoke(
                this,
                damageable,
                damageInfo
            );

            return true;
        }

        protected virtual void ApplyGyroAimSettings()
        {
            if (
                gyroPointer == null
                ||
                Definition == null
            )
            {
                return;
            }

            GyroAimSettings settings =
                Definition.GyroAimSettings;

            if (settings == null)
            {
                return;
            }

            gyroPointer.ApplySettings(
                settings
            );
        }

        protected virtual void OnEquipped()
        {
        }

        protected virtual void OnUnequipped()
        {
        }

        protected virtual void OnInputUnavailable()
        {
        }

        protected abstract void TickWeapon(
            float deltaTime,
            WeaponActivationState activation
        );
    }
}