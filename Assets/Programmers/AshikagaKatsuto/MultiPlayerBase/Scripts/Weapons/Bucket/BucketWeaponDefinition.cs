using Game.Feedback.Rumble;
using UnityEngine;

namespace Game.Weapons.Bucket
{
    [CreateAssetMenu(
        fileName = "WD_Bucket",
        menuName = "Game/Weapons/Bucket Weapon"
    )]
    public sealed class BucketWeaponDefinition :
        WeaponDefinition
    {
        [Header("Water Charge")]
        [Tooltip(
            "給水を開始するために必要な"
            + "重力軸の入力値です。"
        )]
        [SerializeField]
        [Range(-1.0f, 1.0f)]
        private float chargeTiltThreshold =
            0.5f;

        [Tooltip(
            "給水判定に使用する"
            + "コントローラーのローカル重力軸です。"
        )]
        [SerializeField]
        private Vector3 chargeGravityAxis =
            new Vector3(
                0.0f,
                0.0f,
                1.0f
            );

        [Tooltip(
            "1秒あたりに増加する水量です。"
            + "水量は0から1の範囲です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float chargeSpeed =
            0.5f;

        [Tooltip(
            "給水角度から外れたときに水量を"
            + "減少させる速度です。"
            + "0の場合、水量は自然減少しません。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float chargeDrainSpeed;

        [Tooltip(
            "満タン時に使用できる"
            + "最大攻撃回数です。"
        )]
        [SerializeField]
        [Min(1)]
        private int maximumPourCount =
            3;

        [Tooltip(
            "1回以上の攻撃回数として認識するために必要な"
            + "最低水量です。"
            + "通常は1÷最大攻撃回数が使用されます。"
        )]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float minimumAttackCharge =
            0.0f;

        [Header("Swing")]
        [Tooltip(
            "下から上への振り判定に使用する"
            + "加速度軸です。"
        )]
        [SerializeField]
        private Vector3 upwardSwingAxis =
            new Vector3(
                1.0f,
                0.0f,
                0.0f
            );

        [SerializeField]
        [Min(0.0f)]
        private float swingThreshold =
            1.0f;

        [SerializeField]
        [Min(0.0f)]
        private float swingResetThreshold =
            0.3f;

        [SerializeField]
        [Min(0.0f)]
        private float attackCooldown =
            0.5f;

        [Header("Pointer Targeting")]
        [Tooltip(
            "ポインターから飛ばしたRayで"
            + "照準地点として扱うLayerです。"
            + "敵や床のLayerを含めます。"
        )]
        [SerializeField]
        private LayerMask aimLayerMask =
            ~0;

        [Tooltip(
            "ポインターのRaycastで検索する"
            + "最大距離です。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float maximumAimDistance =
            100.0f;

        [Tooltip(
            "Rayが何にも当たらなかった場合に、"
            + "カメラからこの距離の地点を"
            + "仮の照準位置にします。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float fallbackAimDistance =
            10.0f;

        [Tooltip(
            "照準地点から攻撃Box中心までの"
            + "ローカルオフセットです。"
        )]
        [SerializeField]
        private Vector3 pointerAttackOffset =
            Vector3.zero;

        [Tooltip(
            "照準用RayがTrigger Colliderへ"
            + "当たるかどうかです。"
        )]
        [SerializeField]
        private QueryTriggerInteraction
            aimQueryTriggerInteraction =
                QueryTriggerInteraction.Ignore;

        [Header("Wave Projectile")]
[Tooltip(
    "上振り時に生成する波ProjectileのPrefabです。"
)]
[SerializeField]
private BucketWaveProjectile
    waveProjectilePrefab;

[Tooltip(
    "波が1秒間に進む距離です。"
)]
[SerializeField]
[Min(0.0f)]
private float waveSpeed =
    12.0f;

[Tooltip(
    "波が生成されてから消滅するまでの時間です。"
)]
[SerializeField]
[Min(0.01f)]
private float waveLifetime =
    3.0f;

[Tooltip(
    "最低攻撃段階でのVFX表示サイズです。"
)]
[SerializeField]
private Vector3 minimumWaveVisualScale =
    Vector3.one;

[Tooltip(
    "最大給水時のVFX表示サイズです。"
)]
[SerializeField]
private Vector3 maximumWaveVisualScale =
    new Vector3(
        2.0f,
        2.0f,
        2.0f
    );

[Tooltip(
    "最低攻撃段階での波のBox判定サイズです。"
)]
[SerializeField]
private Vector3 minimumWaveHitBoxSize =
    new Vector3(
        2.0f,
        1.5f,
        2.0f
    );

[Tooltip(
    "最大給水時の波のBox判定サイズです。"
)]
[SerializeField]
private Vector3 maximumWaveHitBoxSize =
    new Vector3(
        6.0f,
        3.0f,
        3.0f
    );

[Tooltip(
    "最低攻撃段階での波のダメージです。"
)]
[SerializeField]
[Min(0.0f)]
private float minimumWaveDamage =
    10.0f;

[Tooltip(
    "最大給水時の波のダメージです。"
)]
[SerializeField]
[Min(0.0f)]
private float maximumWaveDamage =
    40.0f;

[Tooltip(
    "正規化されたチャージ量から、"
    + "波のサイズとダメージの強化率を求めるカーブです。"
)]
[SerializeField]
private AnimationCurve waveChargeCurve =
    AnimationCurve.Linear(
        0.0f,
        0.0f,
        1.0f,
        1.0f
    );

[Tooltip(
    "波が敵を検索するLayerです。"
)]
[SerializeField]
private LayerMask waveCollisionLayerMask;

[SerializeField]
private QueryTriggerInteraction
    waveQueryTriggerInteraction =
        QueryTriggerInteraction.Collide;

[Tooltip(
    "波のY座標を発射時の高さへ固定します。"
    + "波が照準の上下方向へ浮くことを防ぎます。"
)]
[SerializeField]
private bool keepFixedWaveHeight =
    true;

[Tooltip(
    "波の固定Y座標へ加算する高さです。"
    + "VFXの原点が地面より下や上にある場合に"
    + "調整してください。"
)]
[SerializeField]
private float waveGroundOffset;

        [Header("Damage")]
        [SerializeField]
        [Min(0.0f)]
        private float damage =
            20.0f;

        [Tooltip(
            "攻撃Boxがダメージを与える"
            + "対象Layerです。"
        )]
        [SerializeField]
        private LayerMask targetLayerMask;

        [SerializeField]
        private QueryTriggerInteraction
            damageQueryTriggerInteraction =
                QueryTriggerInteraction.Collide;

        [Header("Attack Area")]
        [Tooltip(
            "最低攻撃段階のBox判定サイズです。"
        )]
        [SerializeField]
        private Vector3 minimumAttackSize =
            new Vector3(
                2.0f,
                1.5f,
                3.0f
            );

        [Tooltip(
            "最大給水時のBox判定サイズです。"
        )]
        [SerializeField]
        private Vector3 maximumAttackSize =
            new Vector3(
                6.0f,
                3.0f,
                5.0f
            );

        [Header("Rumble Feedback")]
        [Tooltip(
            "実際に給水している間に再生する"
            + "ループ振動です。"
            + "Loopを有効にした定義を設定してください。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            fillingLoopRumble;

        [Tooltip(
            "1回の水攻撃で1体以上へ"
            + "ダメージを適用できたときの振動です。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            damageHitRumble;

        [Tooltip(
            "給水中ループ振動へ掛ける"
            + "強度倍率です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float fillingRumbleIntensity =
            1.0f;

        [Tooltip(
            "命中振動へ掛ける強度倍率です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float damageHitRumbleIntensity =
            1.0f;

        [Header("Effects")]
        [SerializeField]
        private GameObject waterEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float effectLifetime =
            3.0f;

        [Header("Debug")]
        [SerializeField]
        private bool drawAttackArea =
            true;

        [SerializeField]
        private bool drawAimRay =
            true;

        [SerializeField]
        private bool logDamageDetection;

        public float ChargeTiltThreshold =>
            chargeTiltThreshold;

        public Vector3 ChargeGravityAxis =>
            chargeGravityAxis.sqrMagnitude > 0.0f
                ? chargeGravityAxis.normalized
                : Vector3.forward;

        public float ChargeSpeed =>
            chargeSpeed;

        public float ChargeDrainSpeed =>
            chargeDrainSpeed;

        public int MaximumPourCount =>
            maximumPourCount;

        public float MinimumAttackCharge =>
            minimumAttackCharge > 0.0f
                ? minimumAttackCharge
                : 1.0f / maximumPourCount;

        public Vector3 UpwardSwingAxis =>
            upwardSwingAxis.sqrMagnitude > 0.0f
                ? upwardSwingAxis.normalized
                : Vector3.right;

        public float SwingThreshold =>
            swingThreshold;

        public float SwingResetThreshold =>
            swingResetThreshold;

        public float AttackCooldown =>
            attackCooldown;

        public LayerMask AimLayerMask =>
            aimLayerMask;

        public float MaximumAimDistance =>
            maximumAimDistance;

        public float FallbackAimDistance =>
            fallbackAimDistance;

        public Vector3 PointerAttackOffset =>
            pointerAttackOffset;

        public QueryTriggerInteraction
            AimQueryTriggerInteraction =>
                aimQueryTriggerInteraction;

        public float Damage =>
            damage;

        public LayerMask TargetLayerMask =>
            targetLayerMask;

        public QueryTriggerInteraction
            DamageQueryTriggerInteraction =>
                damageQueryTriggerInteraction;

        public Vector3 MinimumAttackSize =>
            minimumAttackSize;

        public Vector3 MaximumAttackSize =>
            maximumAttackSize;

        public ControllerRumbleDefinition
            FillingLoopRumble =>
                fillingLoopRumble;

        public ControllerRumbleDefinition
            DamageHitRumble =>
                damageHitRumble;

        public float FillingRumbleIntensity =>
            fillingRumbleIntensity;

        public float DamageHitRumbleIntensity =>
            damageHitRumbleIntensity;

        public GameObject WaterEffectPrefab =>
            waterEffectPrefab;

        public float EffectLifetime =>
            effectLifetime;

        public bool DrawAttackArea =>
            drawAttackArea;

        public bool DrawAimRay =>
            drawAimRay;

        public bool LogDamageDetection =>
            logDamageDetection;

            public BucketWaveProjectile
    WaveProjectilePrefab =>
        waveProjectilePrefab;

public float WaveSpeed =>
    waveSpeed;

public float WaveLifetime =>
    waveLifetime;

public LayerMask WaveCollisionLayerMask =>
    waveCollisionLayerMask;

public QueryTriggerInteraction
    WaveQueryTriggerInteraction =>
        waveQueryTriggerInteraction;

public bool KeepFixedWaveHeight =>
    keepFixedWaveHeight;

public float WaveGroundOffset =>
    waveGroundOffset;

        public int GetPourCount(
            float normalizedCharge
        )
        {
            float clampedCharge =
                Mathf.Clamp01(
                    normalizedCharge
                );

            if (
                clampedCharge
                < MinimumAttackCharge
            )
            {
                return 0;
            }

            int result =
                Mathf.FloorToInt(
                    clampedCharge
                    * maximumPourCount
                    + 0.0001f
                );

            return Mathf.Clamp(
                result,
                1,
                maximumPourCount
            );
        }

        public float GetAttackChargeForPourCount(
            int pourCount
        )
        {
            if (pourCount <= 0)
            {
                return 0.0f;
            }

            return Mathf.Clamp01(
                pourCount
                / (float)maximumPourCount
            );
        }

        public Vector3 GetAttackSize(
            float normalizedCharge
        )
        {
            return Vector3.Lerp(
                minimumAttackSize,
                maximumAttackSize,
                Mathf.Clamp01(
                    normalizedCharge
                )
            );
        }

        public float EvaluateWaveCharge(
    float normalizedCharge
)
{
    float charge =
        Mathf.Clamp01(
            normalizedCharge
        );

    float curveValue =
        waveChargeCurve != null
            ? waveChargeCurve.Evaluate(
                charge
            )
            : charge;

    return Mathf.Clamp01(
        curveValue
    );
}

public Vector3 GetWaveVisualScale(
    float normalizedCharge
)
{
    float evaluatedCharge =
        EvaluateWaveCharge(
            normalizedCharge
        );

    return Vector3.Lerp(
        minimumWaveVisualScale,
        maximumWaveVisualScale,
        evaluatedCharge
    );
}

public Vector3 GetWaveHitBoxSize(
    float normalizedCharge
)
{
    float evaluatedCharge =
        EvaluateWaveCharge(
            normalizedCharge
        );

    return Vector3.Lerp(
        minimumWaveHitBoxSize,
        maximumWaveHitBoxSize,
        evaluatedCharge
    );
}

public float GetWaveDamage(
    float normalizedCharge
)
{
    float evaluatedCharge =
        EvaluateWaveCharge(
            normalizedCharge
        );

    return Mathf.Lerp(
        minimumWaveDamage,
        maximumWaveDamage,
        evaluatedCharge
    );
}

        protected override void OnValidate()
        {
            base.OnValidate();

            waveSpeed =
    Mathf.Max(
        0.0f,
        waveSpeed
    );

waveLifetime =
    Mathf.Max(
        0.01f,
        waveLifetime
    );

minimumWaveVisualScale =
    MakePositive(
        minimumWaveVisualScale
    );

maximumWaveVisualScale =
    MakePositive(
        maximumWaveVisualScale
    );

minimumWaveHitBoxSize =
    MakePositive(
        minimumWaveHitBoxSize
    );

maximumWaveHitBoxSize =
    MakePositive(
        maximumWaveHitBoxSize
    );

minimumWaveDamage =
    Mathf.Max(
        0.0f,
        minimumWaveDamage
    );

maximumWaveDamage =
    Mathf.Max(
        minimumWaveDamage,
        maximumWaveDamage
    );

if (
    waveChargeCurve == null
    ||
    waveChargeCurve.length == 0
)
{
    waveChargeCurve =
        AnimationCurve.Linear(
            0.0f,
            0.0f,
            1.0f,
            1.0f
        );
}

            chargeSpeed =
                Mathf.Max(
                    0.0f,
                    chargeSpeed
                );

            chargeDrainSpeed =
                Mathf.Max(
                    0.0f,
                    chargeDrainSpeed
                );

            maximumPourCount =
                Mathf.Max(
                    1,
                    maximumPourCount
                );

            minimumAttackCharge =
                Mathf.Clamp01(
                    minimumAttackCharge
                );

            swingThreshold =
                Mathf.Max(
                    0.0f,
                    swingThreshold
                );

            swingResetThreshold =
                Mathf.Max(
                    0.0f,
                    swingResetThreshold
                );

            attackCooldown =
                Mathf.Max(
                    0.0f,
                    attackCooldown
                );

            maximumAimDistance =
                Mathf.Max(
                    0.1f,
                    maximumAimDistance
                );

            fallbackAimDistance =
                Mathf.Max(
                    0.1f,
                    fallbackAimDistance
                );

            damage =
                Mathf.Max(
                    0.0f,
                    damage
                );

            fillingRumbleIntensity =
                Mathf.Max(
                    0.0f,
                    fillingRumbleIntensity
                );

            damageHitRumbleIntensity =
                Mathf.Max(
                    0.0f,
                    damageHitRumbleIntensity
                );

            effectLifetime =
                Mathf.Max(
                    0.0f,
                    effectLifetime
                );

            minimumAttackSize =
                MakePositive(
                    minimumAttackSize
                );

            maximumAttackSize =
                MakePositive(
                    maximumAttackSize
                );
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