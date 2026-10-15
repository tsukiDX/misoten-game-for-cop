using System;
using Game.Input.JSL;
using UnityEngine;

namespace Game.Weapons
{
    [Serializable]
    public sealed class WeaponActivationSettings
    {
        [Tooltip(
            "武器を攻撃準備状態にする"
            + "入力方式です。"
        )]
        [SerializeField]
        private WeaponActivationMode activationMode =
            WeaponActivationMode.RightTrigger;

        [Tooltip(
            "Activation ModeがButtonの場合に"
            + "使用するボタンです。"
        )]
        [SerializeField]
        private JslButton activationButton =
            JslButton.RightShoulder;

        [Tooltip(
            "トリガーを押していると判定する"
            + "最小入力値です。"
        )]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float triggerThreshold = 0.5f;

        public WeaponActivationMode ActivationMode =>
            activationMode;

        public JslButton ActivationButton =>
            activationButton;

        public float TriggerThreshold =>
            triggerThreshold;

        public WeaponActivationState Evaluate(
            LocalPlayerController localPlayer
        )
        {
            if (
                localPlayer == null
                ||
                !localPlayer.IsInputAvailable
            )
            {
                return
                    WeaponActivationState.Inactive;
            }

            switch (activationMode)
            {
                case WeaponActivationMode.None:
                    return
                        WeaponActivationState
                            .AlwaysActive;

                case WeaponActivationMode.Button:
                    return EvaluateButton(
                        localPlayer
                    );

                case WeaponActivationMode.LeftTrigger:
                    return EvaluateTrigger(
                        localPlayer,
                        useRightTrigger: false
                    );

                case WeaponActivationMode.RightTrigger:
                    return EvaluateTrigger(
                        localPlayer,
                        useRightTrigger: true
                    );

                default:
                    return
                        WeaponActivationState.Inactive;
            }
        }

        private WeaponActivationState EvaluateButton(
            LocalPlayerController localPlayer
        )
        {
            bool isActive =
                localPlayer.IsButtonPressed(
                    activationButton
                );

            bool started =
                localPlayer
                    .WasButtonPressedThisFrame(
                        activationButton
                    );

            bool released =
                localPlayer
                    .WasButtonReleasedThisFrame(
                        activationButton
                    );

            return new WeaponActivationState(
                isActive,
                started,
                released,
                isActive ? 1.0f : 0.0f
            );
        }

        private WeaponActivationState EvaluateTrigger(
            LocalPlayerController localPlayer,
            bool useRightTrigger
        )
        {
            JslControllerDevice controller =
                localPlayer.Controller;

            if (controller == null)
            {
                return
                    WeaponActivationState.Inactive;
            }

            JslInputSnapshot currentInput =
                controller.CurrentInput;

            JslInputSnapshot previousInput =
                controller.PreviousInput;

            float currentValue =
                useRightTrigger
                    ? currentInput.RightTrigger
                    : currentInput.LeftTrigger;

            float previousValue =
                useRightTrigger
                    ? previousInput.RightTrigger
                    : previousInput.LeftTrigger;

            bool isActive =
                currentValue >= triggerThreshold;

            bool wasActive =
                previousValue >= triggerThreshold;

            bool started =
                isActive && !wasActive;

            bool released =
                !isActive && wasActive;

            return new WeaponActivationState(
                isActive,
                started,
                released,
                currentValue
            );
        }
    }
}
