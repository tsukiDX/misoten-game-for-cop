using System;
using Game.Input.JSL;
using UnityEngine;

namespace Game.Feedback.Rumble
{
    public sealed class ControllerRumblePlayer :
        MonoBehaviour
    {
        [Header("Player")]
        [Tooltip(
            "振動させるコントローラーが"
            + "割り当てられたLocalPlayerです。"
            + "未設定の場合は親から検索します。"
        )]
        [SerializeField]
        private LocalPlayerController localPlayer;

        [Header("Output")]
        [Tooltip(
            "すべての振動へ掛ける"
            + "最終的な強度倍率です。"
        )]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float masterIntensity =
            1.0f;

        [Tooltip(
            "振動出力を一時的に無効化します。"
        )]
        [SerializeField]
        private bool rumbleEnabled =
            true;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isPlaying;

        [SerializeField]
        private string currentDefinitionName;

        [SerializeField]
        private float elapsedTime;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float currentLowFrequency;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float currentHighFrequency;

        [SerializeField]
        private float currentIntensityMultiplier =
            1.0f;

        [SerializeField]
        private int currentPriority;

        private ControllerRumbleDefinition
            currentDefinition;

        public bool IsPlaying =>
            isPlaying;

        public ControllerRumbleDefinition
            CurrentDefinition =>
                currentDefinition;

        public LocalPlayerController LocalPlayer =>
            localPlayer;

        public event Action<
            ControllerRumbleDefinition
        > RumbleStarted;

        public event Action RumbleStopped;

        private void Awake()
        {
            ResolveLocalPlayer();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            if (
                currentDefinition == null
                ||
                !rumbleEnabled
            )
            {
                Stop();
                return;
            }

            if (
                localPlayer == null
                ||
                !localPlayer.IsInputAvailable
                ||
                localPlayer.Controller == null
            )
            {
                StopOutput();

                return;
            }

            float deltaTime =
                currentDefinition.UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsedTime +=
                deltaTime;

            float duration =
                Mathf.Max(
                    0.01f,
                    currentDefinition.Duration
                );

            if (
                currentDefinition.Loop
                &&
                elapsedTime >= duration
            )
            {
                elapsedTime =
                    Mathf.Repeat(
                        elapsedTime,
                        duration
                    );
            }
            else if (
                !currentDefinition.Loop
                &&
                elapsedTime >= duration
            )
            {
                Stop();
                return;
            }

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime
                    / duration
                );

            currentDefinition.Evaluate(
                normalizedTime,
                currentIntensityMultiplier
                * masterIntensity,
                out currentLowFrequency,
                out currentHighFrequency
            );

            ApplyOutput(
                currentLowFrequency,
                currentHighFrequency
            );
        }

        public bool Play(
            ControllerRumbleDefinition definition,
            float intensityMultiplier =
                1.0f
        )
        {
            if (
                definition == null
                ||
                !rumbleEnabled
            )
            {
                return false;
            }

            ResolveLocalPlayer();

            if (
                isPlaying
                &&
                currentDefinition != null
                &&
                definition.Priority
                < currentPriority
            )
            {
                return false;
            }

            currentDefinition =
                definition;

            currentDefinitionName =
                definition.name;

            currentIntensityMultiplier =
                Mathf.Max(
                    0.0f,
                    intensityMultiplier
                );

            currentPriority =
                definition.Priority;

            elapsedTime =
                0.0f;

            isPlaying =
                true;

            definition.Evaluate(
                0.0f,
                currentIntensityMultiplier
                * masterIntensity,
                out currentLowFrequency,
                out currentHighFrequency
            );

            ApplyOutput(
                currentLowFrequency,
                currentHighFrequency
            );

            RumbleStarted?.Invoke(
                definition
            );

            return true;
        }

        public void SetIntensityMultiplier(
    float intensityMultiplier
)
{
    currentIntensityMultiplier =
        Mathf.Max(
            0.0f,
            intensityMultiplier
        );
}

        public void Stop()
        {
            bool wasPlaying =
                isPlaying;

            isPlaying =
                false;

            currentDefinition =
                null;

            currentDefinitionName =
                string.Empty;

            elapsedTime =
                0.0f;

            currentLowFrequency =
                0.0f;

            currentHighFrequency =
                0.0f;

            currentIntensityMultiplier =
                1.0f;

            currentPriority =
                0;

            StopOutput();

            if (wasPlaying)
            {
                RumbleStopped?.Invoke();
            }
        }

        public void SetRumbleEnabled(
            bool enabled
        )
        {
            rumbleEnabled =
                enabled;

            if (!rumbleEnabled)
            {
                Stop();
            }
        }

        public void SetMasterIntensity(
            float value
        )
        {
            masterIntensity =
                Mathf.Clamp01(
                    value
                );
        }

        public void SetLocalPlayer(
            LocalPlayerController player
        )
        {
            if (
                isPlaying
                &&
                localPlayer != player
            )
            {
                Stop();
            }

            localPlayer =
                player;
        }

        private void ResolveLocalPlayer()
        {
            if (localPlayer != null)
            {
                return;
            }

            localPlayer =
                GetComponentInParent<
                    LocalPlayerController
                >();
        }

        private void ApplyOutput(
            float lowFrequency,
            float highFrequency
        )
        {
            if (
                localPlayer == null
                ||
                !localPlayer.IsInputAvailable
                ||
                localPlayer.Controller == null
            )
            {
                return;
            }

            try
            {
                // JslControllerDevice.SetRumbleは
                // smallMotor, largeMotorの順番。
                //
                // 高周波をsmallMotor、
                // 低周波をlargeMotorへ渡す。
                localPlayer.Controller.SetRumble(
                    highFrequency,
                    lowFrequency
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Player "
                    + $"{localPlayer.PlayerNumber}の"
                    + "振動出力に失敗しました。\n"
                    + exception,
                    this
                );

                Stop();
            }
        }

        private void StopOutput()
        {
            if (
                localPlayer == null
                ||
                localPlayer.Controller == null
            )
            {
                return;
            }

            try
            {
                localPlayer.Controller
                    .StopRumble();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "振動停止に失敗しました。\n"
                    + exception,
                    this
                );
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            StopOutput();
        }

        private void OnValidate()
        {
            masterIntensity =
                Mathf.Clamp01(
                    masterIntensity
                );
        }
    }
}