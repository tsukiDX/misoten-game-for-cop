using Game.Feedback.Rumble;
using UnityEngine;

namespace Game.Weapons.Vacuum
{
    [CreateAssetMenu(
        fileName = "WD_Vacuum",
        menuName = "Game/Weapons/Vacuum Weapon"
    )]
    public sealed class VacuumWeaponDefinition :
        WeaponDefinition
    {
        [Header("Target Search")]
        [Tooltip(
            "掃除機でターゲットとして検索する"
            + "敵のLayerです。"
        )]
        [SerializeField]
        private LayerMask targetLayerMask;

        [Tooltip(
            "レティクル周辺から敵を検索する"
            + "SphereCastの半径です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float targetingRadius =
            0.4f;

        [Tooltip(
            "カメラ位置から敵を検索する最大距離です。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float maximumTargetDistance =
            100.0f;

        [Tooltip(
            "敵の照準ColliderがTriggerの場合は"
            + "Collideを設定してください。"
        )]
        [SerializeField]
        private QueryTriggerInteraction
            targetQueryTriggerInteraction =
                QueryTriggerInteraction.Collide;

        [Header("Reticle Constraint")]
        [Tooltip(
            "ターゲット中にレティクルを制限する"
            + "円の半径です。"
            + "1920x1080の仮想座標単位で指定します。"
        )]
        [SerializeField]
        [Min(1.0f)]
        private float reticleConstraintRadius =
            180.0f;

        [Tooltip(
            "この値未満の1フレーム移動量は"
            + "吸引量へ加算しません。"
            + "小さなジャイロノイズの除去に使用します。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float minimumMovementThreshold =
            0.1f;

        [Tooltip(
            "1フレームで吸引量へ加算できる"
            + "最大移動量です。"
            + "瞬間的なジャイロ飛びを抑制します。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float maximumMovementPerFrame =
            50.0f;

        [Header("Suction Strength")]
        [Tooltip(
            "吸引段階を1段階上げるために必要な"
            + "レティクル累計移動量です。"
        )]
        [SerializeField]
        [Min(1.0f)]
        private float movementRequiredPerLevel =
            450.0f;

        [Tooltip(
            "吸引強度の最大段階です。"
            + "ターゲット直後は段階1から始まります。"
        )]
        [SerializeField]
        [Min(1)]
        private int maximumSuctionLevel =
            5;

        [Header("Spin Intensity")]
[Tooltip(
    "この値以上のレティクル移動量があると、"
    + "グルグル操作中として扱います。"
)]
[SerializeField]
[Min(0.0f)]
private float spinMovementThreshold =
    0.1f;

[Tooltip(
    "グルグル操作を続けた場合に、"
    + "Spin Effect Tが0から1になるまでの時間です。"
)]
[SerializeField]
[Min(0.01f)]
private float spinBuildDuration =
    2.0f;

[Tooltip(
    "操作を止めた場合に、"
    + "Spin Effect Tが1から0になるまでの時間です。"
)]
[SerializeField]
[Min(0.01f)]
private float spinDecayDuration =
    1.0f;

[Tooltip(
    "Spin Effect Tを、振動やダメージへ使用する"
    + "最終強度へ変換するカーブです。"
)]
[SerializeField]
private AnimationCurve spinIntensityCurve =
    new AnimationCurve(
        new Keyframe(
            0.0f,
            0.0f
        ),
        new Keyframe(
            0.3f,
            0.1f
        ),
        new Keyframe(
            0.6f,
            0.35f
        ),
        new Keyframe(
            0.8f,
            0.7f
        ),
        new Keyframe(
            1.0f,
            1.0f
        )
    );

[Header("Attack Pulse")]
[Tooltip(
    "Spin Effect Strengthが0のときの"
    + "アタック間隔です。"
)]
[SerializeField]
[Min(0.01f)]
private float maximumAttackInterval =
    1.0f;

[Tooltip(
    "Spin Effect Strengthが1のときの"
    + "アタック間隔です。"
)]
[SerializeField]
[Min(0.01f)]
private float minimumAttackInterval =
    0.15f;

[Tooltip(
    "一定間隔で発生するアタック時の"
    + "単発振動です。"
)]
[SerializeField]
private ControllerRumbleDefinition
    attackPulseRumble;

[Tooltip(
    "Spin Effect Strengthが0のときの"
    + "アタック振動強度です。"
)]
[SerializeField]
[Min(0.0f)]
private float minimumAttackRumbleIntensity =
    0.4f;

[Tooltip(
    "Spin Effect Strengthが1のときの"
    + "アタック振動強度です。"
)]
[SerializeField]
[Min(0.0f)]
private float maximumAttackRumbleIntensity =
    1.0f;

[Header("Damage Intensity")]
[Tooltip(
    "Spin Effect Strengthが0のときの"
    + "ダメージ倍率です。"
)]
[SerializeField]
[Min(0.0f)]
private float minimumDamageMultiplier =
    0.5f;

[Tooltip(
    "Spin Effect Strengthが1のときの"
    + "ダメージ倍率です。"
)]
[SerializeField]
[Min(0.0f)]
private float maximumDamageMultiplier =
    1.5f;

        [Header("Continuous Damage")]
        [Tooltip(
            "吸引段階1での"
            + "1回ごとのダメージです。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float baseDamage =
            3.0f;

        [Tooltip(
            "吸引段階が上がるたびに加算される"
            + "1回ごとのダメージです。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float damageIncreasePerLevel =
            2.0f;

        [Tooltip(
            "継続ダメージを与える間隔です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float damageInterval =
            0.5f;

        [Tooltip(
            "ターゲット成立直後に"
            + "最初のダメージを与えます。"
        )]
        [SerializeField]
        private bool damageImmediatelyOnTarget =
            true;

        [Header("Target VFX")]
        [Tooltip(
            "敵をターゲットしたときに生成する"
            + "吸引VFXのPrefabです。"
        )]
        [SerializeField]
        private VacuumTargetVFX
            targetVFXPrefab;

        [Tooltip(
            "ポインター移動速度をVFXのくねり入力へ"
            + "変換するときの倍率です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float vfxSteeringSensitivity =
            10.0f;

        [Header("Rumble Feedback")]
        [Tooltip(
            "敵を吸引している間に継続再生する振動です。"
            + "継続振動にする場合はRDのLoopを"
            + "有効にしてください。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            targetingLoopRumble;

        [Tooltip(
            "吸引段階が上がった瞬間に再生する"
            + "単発振動です。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            suctionLevelIncreasedRumble;

        [Tooltip(
            "吸引進行率から継続振動の"
            + "強度倍率を計算するカーブです。"
        )]
        [SerializeField]
        private AnimationCurve
            targetingRumbleIntensityCurve =
                new AnimationCurve(
                    new Keyframe(
                        0.0f,
                        0.25f
                    ),
                    new Keyframe(
                        1.0f,
                        1.0f
                    )
                );

        [Tooltip(
            "ターゲット中の継続振動へ掛ける"
            + "最終強度倍率です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float targetingRumbleIntensity =
            1.0f;

        [Tooltip(
            "吸引段階上昇時の単発振動へ掛ける"
            + "強度倍率です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float levelIncreasedRumbleIntensity =
            1.0f;

        [Header("Effects")]
        [Tooltip(
            "継続ダメージを与えた位置へ生成する"
            + "エフェクトです。"
        )]
        [SerializeField]
        private GameObject damageEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float damageEffectLifetime =
            1.0f;

        [Header("Debug")]
        [SerializeField]
        private bool drawTargetingCast =
            true;

        [SerializeField]
        private bool logTargetDetection;

        [SerializeField]
        private bool logSuctionProgress;

        public LayerMask TargetLayerMask =>
            targetLayerMask;

        public float TargetingRadius =>
            targetingRadius;

        public float MaximumTargetDistance =>
            maximumTargetDistance;

        public QueryTriggerInteraction
            TargetQueryTriggerInteraction =>
                targetQueryTriggerInteraction;

        public float ReticleConstraintRadius =>
            reticleConstraintRadius;

        public float MinimumMovementThreshold =>
            minimumMovementThreshold;

        public float MaximumMovementPerFrame =>
            maximumMovementPerFrame;

        public float MovementRequiredPerLevel =>
            movementRequiredPerLevel;

        public int MaximumSuctionLevel =>
            maximumSuctionLevel;

        public float BaseDamage =>
            baseDamage;

        public float DamageIncreasePerLevel =>
            damageIncreasePerLevel;

        public float DamageInterval =>
            damageInterval;

        public bool DamageImmediatelyOnTarget =>
            damageImmediatelyOnTarget;

        public VacuumTargetVFX TargetVFXPrefab =>
            targetVFXPrefab;

        public float VFXSteeringSensitivity =>
            vfxSteeringSensitivity;

        public ControllerRumbleDefinition
            TargetingLoopRumble =>
                targetingLoopRumble;

        public ControllerRumbleDefinition
            SuctionLevelIncreasedRumble =>
                suctionLevelIncreasedRumble;

        public float LevelIncreasedRumbleIntensity =>
            levelIncreasedRumbleIntensity;

        public GameObject DamageEffectPrefab =>
            damageEffectPrefab;

        public float DamageEffectLifetime =>
            damageEffectLifetime;

        public bool DrawTargetingCast =>
            drawTargetingCast;

        public bool LogTargetDetection =>
            logTargetDetection;

        public bool LogSuctionProgress =>
            logSuctionProgress;

        public float SpinMovementThreshold =>
    spinMovementThreshold;

public float SpinBuildDuration =>
    spinBuildDuration;

public float SpinDecayDuration =>
    spinDecayDuration;

public ControllerRumbleDefinition
    AttackPulseRumble =>
        attackPulseRumble;

public float MinimumAttackRumbleIntensity =>
    minimumAttackRumbleIntensity;

public float MaximumAttackRumbleIntensity =>
    maximumAttackRumbleIntensity;

public float MinimumDamageMultiplier =>
    minimumDamageMultiplier;

public float MaximumDamageMultiplier =>
    maximumDamageMultiplier;


        public float MaximumSuctionMovement
        {
            get
            {
                if (maximumSuctionLevel <= 1)
                {
                    return 0.0f;
                }

                return movementRequiredPerLevel
                    * (maximumSuctionLevel - 1);
            }
        }

        public float GetDamage(
            int suctionLevel
        )
        {
            int clampedLevel =
                Mathf.Clamp(
                    suctionLevel,
                    1,
                    maximumSuctionLevel
                );

            return baseDamage
                + damageIncreasePerLevel
                * (clampedLevel - 1);
        }

        public int GetSuctionLevel(
            float accumulatedMovement
        )
        {
            if (maximumSuctionLevel <= 1)
            {
                return 1;
            }

            int addedLevel =
                Mathf.FloorToInt(
                    Mathf.Max(
                        0.0f,
                        accumulatedMovement
                    )
                    / movementRequiredPerLevel
                );

            return Mathf.Clamp(
                1 + addedLevel,
                1,
                maximumSuctionLevel
            );
        }

        public float GetNormalizedSuction(
            float accumulatedMovement
        )
        {
            float maximumMovement =
                MaximumSuctionMovement;

            if (maximumMovement <= 0.0f)
            {
                return 1.0f;
            }

            return Mathf.Clamp01(
                accumulatedMovement
                / maximumMovement
            );
        }

        public float EvaluateSpinIntensity(
    float spinEffectT
)
{
    float clampedT =
        Mathf.Clamp01(
            spinEffectT
        );

    float curveValue =
        spinIntensityCurve != null
            ? spinIntensityCurve
                .Evaluate(
                    clampedT
                )
            : clampedT;

    return Mathf.Clamp01(
        curveValue
    );
}

public float GetAttackInterval(
    float spinEffectStrength
)
{
    float normalizedStrength =
        Mathf.Clamp01(
            spinEffectStrength
        );

    return Mathf.Lerp(
        maximumAttackInterval,
        minimumAttackInterval,
        normalizedStrength
    );
}

public float GetAttackRumbleIntensity(
    float spinEffectStrength
)
{
    return Mathf.Lerp(
        minimumAttackRumbleIntensity,
        maximumAttackRumbleIntensity,
        Mathf.Clamp01(
            spinEffectStrength
        )
    );
}

public float GetDamageMultiplier(
    float spinEffectStrength
)
{
    return Mathf.Lerp(
        minimumDamageMultiplier,
        maximumDamageMultiplier,
        Mathf.Clamp01(
            spinEffectStrength
        )
    );
}

        public float GetTargetingRumbleMultiplier(
            float normalizedSuction
        )
        {
            float normalized =
                Mathf.Clamp01(
                    normalizedSuction
                );

            float curveValue =
                targetingRumbleIntensityCurve
                    != null
                    ? targetingRumbleIntensityCurve
                        .Evaluate(
                            normalized
                        )
                    : normalized;

            return Mathf.Max(
                0.0f,
                curveValue
                * targetingRumbleIntensity
            );
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            spinMovementThreshold =
    Mathf.Max(
        0.0f,
        spinMovementThreshold
    );

spinBuildDuration =
    Mathf.Max(
        0.01f,
        spinBuildDuration
    );

spinDecayDuration =
    Mathf.Max(
        0.01f,
        spinDecayDuration
    );

maximumAttackInterval =
    Mathf.Max(
        0.01f,
        maximumAttackInterval
    );

minimumAttackInterval =
    Mathf.Clamp(
        minimumAttackInterval,
        0.01f,
        maximumAttackInterval
    );

minimumAttackRumbleIntensity =
    Mathf.Max(
        0.0f,
        minimumAttackRumbleIntensity
    );

maximumAttackRumbleIntensity =
    Mathf.Max(
        minimumAttackRumbleIntensity,
        maximumAttackRumbleIntensity
    );

minimumDamageMultiplier =
    Mathf.Max(
        0.0f,
        minimumDamageMultiplier
    );

maximumDamageMultiplier =
    Mathf.Max(
        minimumDamageMultiplier,
        maximumDamageMultiplier
    );

if (
    spinIntensityCurve == null
    ||
    spinIntensityCurve.length == 0
)
{
    spinIntensityCurve =
        new AnimationCurve(
            new Keyframe(
                0.0f,
                0.0f
            ),
            new Keyframe(
                0.3f,
                0.1f
            ),
            new Keyframe(
                0.6f,
                0.35f
            ),
            new Keyframe(
                0.8f,
                0.7f
            ),
            new Keyframe(
                1.0f,
                1.0f
            )
        );
}

            targetingRadius =
                Mathf.Max(
                    0.01f,
                    targetingRadius
                );

            maximumTargetDistance =
                Mathf.Max(
                    0.1f,
                    maximumTargetDistance
                );

            reticleConstraintRadius =
                Mathf.Max(
                    1.0f,
                    reticleConstraintRadius
                );

            minimumMovementThreshold =
                Mathf.Max(
                    0.0f,
                    minimumMovementThreshold
                );

            maximumMovementPerFrame =
                Mathf.Max(
                    0.01f,
                    maximumMovementPerFrame
                );

            minimumMovementThreshold =
                Mathf.Min(
                    minimumMovementThreshold,
                    maximumMovementPerFrame
                );

            movementRequiredPerLevel =
                Mathf.Max(
                    1.0f,
                    movementRequiredPerLevel
                );

            maximumSuctionLevel =
                Mathf.Max(
                    1,
                    maximumSuctionLevel
                );

            baseDamage =
                Mathf.Max(
                    0.0f,
                    baseDamage
                );

            damageIncreasePerLevel =
                Mathf.Max(
                    0.0f,
                    damageIncreasePerLevel
                );

            damageInterval =
                Mathf.Max(
                    0.01f,
                    damageInterval
                );

            vfxSteeringSensitivity =
                Mathf.Max(
                    0.0f,
                    vfxSteeringSensitivity
                );

            targetingRumbleIntensity =
                Mathf.Max(
                    0.0f,
                    targetingRumbleIntensity
                );

            levelIncreasedRumbleIntensity =
                Mathf.Max(
                    0.0f,
                    levelIncreasedRumbleIntensity
                );

            damageEffectLifetime =
                Mathf.Max(
                    0.0f,
                    damageEffectLifetime
                );

            if (
                targetingRumbleIntensityCurve
                == null
                ||
                targetingRumbleIntensityCurve.length
                == 0
            )
            {
                targetingRumbleIntensityCurve =
                    new AnimationCurve(
                        new Keyframe(
                            0.0f,
                            0.25f
                        ),
                        new Keyframe(
                            1.0f,
                            1.0f
                        )
                    );
            }
        }
    }
}