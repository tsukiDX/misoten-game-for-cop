using UnityEngine;

namespace Game.Feedback.Rumble
{
    [CreateAssetMenu(
        fileName = "Rumble_",
        menuName = "Game/Feedback/Controller Rumble"
    )]
    public sealed class ControllerRumbleDefinition :
        ScriptableObject
    {
        [Header("Timing")]
        [Tooltip(
            "振動を再生する時間です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float duration =
            0.2f;

        [Tooltip(
            "有効にすると、停止されるまで"
            + "振動パターンを繰り返します。"
        )]
        [SerializeField]
        private bool loop;

        [Header("Intensity Curves")]
        [Tooltip(
            "重く低い振動の時間変化です。"
            + "横軸0～1が再生時間、"
            + "縦軸0～1が振動強度です。"
        )]
        [SerializeField]
        private AnimationCurve lowFrequencyCurve =
            AnimationCurve.EaseInOut(
                0.0f,
                1.0f,
                1.0f,
                0.0f
            );

        [Tooltip(
            "細かく高い振動の時間変化です。"
            + "横軸0～1が再生時間、"
            + "縦軸0～1が振動強度です。"
        )]
        [SerializeField]
        private AnimationCurve highFrequencyCurve =
            AnimationCurve.EaseInOut(
                0.0f,
                0.5f,
                1.0f,
                0.0f
            );

        [Header("Output")]
        [Tooltip(
            "カーブ全体へ掛ける強度です。"
        )]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float overallIntensity =
            1.0f;

        [Tooltip(
            "再生中の振動を置き換える際の"
            + "優先度です。"
            + "数値が大きい振動を優先します。"
        )]
        [SerializeField]
        private int priority;

        [Tooltip(
            "Time.timeScaleの影響を受けない"
            + "実時間で再生します。"
        )]
        [SerializeField]
        private bool useUnscaledTime =
            true;

        public float Duration =>
            duration;

        public bool Loop =>
            loop;

        public int Priority =>
            priority;

        public bool UseUnscaledTime =>
            useUnscaledTime;

        public AnimationCurve LowFrequencyCurve =>
            lowFrequencyCurve;

        public AnimationCurve HighFrequencyCurve =>
            highFrequencyCurve;

        public float OverallIntensity =>
            overallIntensity;

        public void Evaluate(
            float normalizedTime,
            float intensityMultiplier,
            out float lowFrequency,
            out float highFrequency
        )
        {
            float clampedTime =
                Mathf.Clamp01(
                    normalizedTime
                );

            float finalMultiplier =
                Mathf.Clamp01(
                    overallIntensity
                    * Mathf.Max(
                        0.0f,
                        intensityMultiplier
                    )
                );

            lowFrequency =
                Mathf.Clamp01(
                    EvaluateCurve(
                        lowFrequencyCurve,
                        clampedTime
                    )
                    * finalMultiplier
                );

            highFrequency =
                Mathf.Clamp01(
                    EvaluateCurve(
                        highFrequencyCurve,
                        clampedTime
                    )
                    * finalMultiplier
                );
        }

        private static float EvaluateCurve(
            AnimationCurve curve,
            float normalizedTime
        )
        {
            if (
                curve == null
                ||
                curve.length == 0
            )
            {
                return 0.0f;
            }

            return Mathf.Clamp01(
                curve.Evaluate(
                    normalizedTime
                )
            );
        }

        private void OnValidate()
        {
            duration =
                Mathf.Max(
                    0.01f,
                    duration
                );

            overallIntensity =
                Mathf.Clamp01(
                    overallIntensity
                );

            if (lowFrequencyCurve == null)
            {
                lowFrequencyCurve =
                    AnimationCurve.Linear(
                        0.0f,
                        1.0f,
                        1.0f,
                        0.0f
                    );
            }

            if (highFrequencyCurve == null)
            {
                highFrequencyCurve =
                    AnimationCurve.Linear(
                        0.0f,
                        0.5f,
                        1.0f,
                        0.0f
                    );
            }
        }
    }
}