using System;
using Game.Feedback.Rumble;
using UnityEngine;

namespace Game.Weapons.Spray
{
    public enum SprayTriggerMode
    {
        [InspectorName(
            "RZトリガーを押している間に発射"
        )]
        FireWhileHeld,

        [InspectorName(
            "RZトリガーを離したときに全量発射"
        )]
        FireOnRelease
    }

    [CreateAssetMenu(
        fileName = "WD_Spray",
        menuName = "Game/Weapons/Spray Weapon"
    )]
    public sealed class SprayWeaponDefinition :
        WeaponDefinition
    {
        [Serializable]
        public sealed class ChargeLevelSettings
        {
            [Tooltip(
                "この段階として扱うために必要な"
                + "最低チャージ量です。"
            )]
            [SerializeField]
            [Range(0.0f, 1.0f)]
            private float minimumCharge;

            [Tooltip(
                "チャージ保持中振動へ掛ける倍率です。"
            )]
            [SerializeField]
            [Min(0.0f)]
            private float holdingRumbleMultiplier =
                1.0f;

            [Tooltip(
                "発射中振動へ掛ける倍率です。"
            )]
            [SerializeField]
            [Min(0.0f)]
            private float firingRumbleMultiplier =
                1.0f;

            public float MinimumCharge =>
                minimumCharge;

            public float HoldingRumbleMultiplier =>
                holdingRumbleMultiplier;

            public float FiringRumbleMultiplier =>
                firingRumbleMultiplier;

            public void Validate()
            {
                minimumCharge =
                    Mathf.Clamp01(
                        minimumCharge
                    );

                holdingRumbleMultiplier =
                    Mathf.Max(
                        0.0f,
                        holdingRumbleMultiplier
                    );

                firingRumbleMultiplier =
                    Mathf.Max(
                        0.0f,
                        firingRumbleMultiplier
                    );
            }
        }

        [Header("Shake Charge")]
        [Tooltip(
            "縦振り判定に使用するコントローラーの"
            + "ローカル加速度軸です。"
        )]
        [SerializeField]
        private Vector3 shakeAxis =
            Vector3.up;

        [Tooltip(
            "1回の縦振りとして判定する"
            + "加速度の絶対値です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float shakeThreshold =
            1.0f;

        [Tooltip(
            "次の縦振りを受け付けるために、"
            + "加速度がこの値以下へ戻る必要があります。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float shakeResetThreshold =
            0.3f;

        [Tooltip(
            "縦振り1回で追加するチャージ量です。"
        )]
        [SerializeField]
        [Range(0.01f, 1.0f)]
        private float chargePerShake =
            0.15f;

        [Tooltip(
            "最後の振りを検出してから、"
            + "振っている状態を維持する時間です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float shakingStateDuration =
            0.2f;

        [Header("Charge Levels")]
        [Tooltip(
            "第1段階の最低チャージ量と"
            + "振動倍率の設定です。"
        )]
        [SerializeField]
        private ChargeLevelSettings level1 =
            new ChargeLevelSettings();

        [Tooltip(
            "第2段階の最低チャージ量と"
            + "振動倍率の設定です。"
        )]
        [SerializeField]
        private ChargeLevelSettings level2 =
            new ChargeLevelSettings();

        [Tooltip(
            "第3段階の最低チャージ量と"
            + "振動倍率の設定です。"
        )]
        [SerializeField]
        private ChargeLevelSettings level3 =
            new ChargeLevelSettings();

        [Header("Firing")]
        [Tooltip(
            "Rトリガーによる発射方式です。\n"
            + "Fire While Held: "
            + "事前に振ってチャージし、"
            + "押している間に発射します。\n"
            + "Fire On Release: "
            + "押している間に振ってチャージし、"
            + "離したときに全量発射します。"
        )]
        [SerializeField]
        private SprayTriggerMode triggerMode =
            SprayTriggerMode.FireWhileHeld;

        [Tooltip(
            "最大チャージから空になるまでに"
            + "発射できる基準弾数です。"
            + "Charge Consumption Per Shotが0の場合に"
            + "使用されます。"
        )]
        [SerializeField]
        [Min(1)]
        private int maximumProjectileCount =
            30;

        [Tooltip(
            "1発ごとに消費するチャージ量です。"
            + "0の場合はMaximum Projectile Countから"
            + "自動計算します。"
        )]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float chargeConsumptionPerShot =
            0.0f;

        [Tooltip(
            "すべてのチャージ段階で共通する"
            + "弾の発射間隔です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float projectileInterval =
            0.08f;

        [Header("Aiming")]
        [Tooltip(
            "ポインターから照準地点を検索するLayerです。"
            + "敵と背景、地面などを含めます。"
        )]
        [SerializeField]
        private LayerMask aimLayerMask =
            ~0;

        [Tooltip(
    "照準補助で検索する敵のLayerです。"
    + "背景や地面は含めないでください。"
)]
[SerializeField]
private LayerMask enemyAimLayerMask;

[Tooltip(
    "敵を検索するSphereCastの半径です。"
)]
[SerializeField]
[Min(0.01f)]
private float enemyAimAssistRadius =
    0.4f;

[SerializeField]
private QueryTriggerInteraction
    enemyAimQueryTriggerInteraction =
        QueryTriggerInteraction.Collide;

        [SerializeField]
        [Min(0.1f)]
        private float maximumAimDistance =
            100.0f;

        [SerializeField]
        [Min(0.1f)]
        private float fallbackAimDistance =
            20.0f;

        [SerializeField]
        private QueryTriggerInteraction
            aimQueryTriggerInteraction =
                QueryTriggerInteraction.Ignore;

        [Header("Bubble Beam")]
[Tooltip(
    "照射中に生成する泡ビームPrefabです。"
    + "SprayBeamDamageAreaを持つPrefabを設定します。"
)]
[SerializeField]
private SprayBeamDamageArea
    beamPrefab;

[Tooltip(
    "1秒あたりに消費するチャージ量です。"
)]
[SerializeField]
[Min(0.0001f)]
private float beamChargeConsumptionPerSecond =
    0.25f;

[Tooltip(
    "ビームが範囲内の敵へ与える"
    + "1回ごとのダメージです。"
)]
[SerializeField]
[Min(0.0f)]
private float beamDamage =
    3.0f;

[Tooltip(
    "ビームが範囲内の敵へ"
    + "ダメージを与える間隔です。"
)]
[SerializeField]
[Min(0.01f)]
private float beamDamageInterval =
    0.2f;

[Tooltip(
    "ビーム照射開始直後に"
    + "最初のダメージを与えます。"
)]
[SerializeField]
private bool beamDamageImmediately =
    true;

[Tooltip(
    "ビームがダメージ対象として検出するLayerです。"
)]
[SerializeField]
private LayerMask beamTargetLayerMask;

[Tooltip(
    "ビームがTrigger Colliderを"
    + "ダメージ対象として扱うかを指定します。"
)]
[SerializeField]
private QueryTriggerInteraction
    beamQueryTriggerInteraction =
        QueryTriggerInteraction.Collide;

        [Header("Projectile")]
        [Tooltip(
            "すべてのチャージ段階で共通して使用する"
            + "弾Prefabです。"
        )]
        [SerializeField]
        private SprayProjectile projectilePrefab;

        [Tooltip(
            "すべてのチャージ段階で共通する弾速です。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float projectileSpeed =
            20.0f;

        [Tooltip(
            "すべてのチャージ段階で共通する"
            + "弾の当たり判定半径です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float projectileRadius =
            0.12f;

        [SerializeField]
        [Min(0.1f)]
        private float projectileLifetime =
            3.0f;

        [Tooltip(
            "弾が衝突するLayerです。"
            + "Enemyと壁などを含めます。"
        )]
        [SerializeField]
        private LayerMask projectileCollisionLayerMask;

        [SerializeField]
        private QueryTriggerInteraction
            projectileQueryTriggerInteraction =
                QueryTriggerInteraction.Collide;

        [Header("Damage")]
        [SerializeField]
        [Min(0.0f)]
        private float damagePerProjectile =
            5.0f;

        [Header("Effects")]
        [Tooltip(
            "すべてのチャージ段階で共通して使用する"
            + "発射口エフェクトです。"
        )]
        [SerializeField]
        private GameObject muzzleEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float muzzleEffectLifetime =
            1.0f;

        [Tooltip(
            "弾が敵へダメージを与えた位置に"
            + "生成するエフェクトです。"
        )]
        [SerializeField]
        private GameObject hitEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float hitEffectLifetime =
            1.0f;

        [Header("Rumble Feedback")]
        [Tooltip(
            "弾が敵へ命中し、実際にダメージを"
            + "適用できた瞬間の振動です。"
            + "強度はこのRD内で調整します。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            projectileHitRumble;

        [Tooltip(
            "チャージ段階が1へ上がった瞬間の振動です。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            level1ReachedRumble;

        [Tooltip(
            "チャージ段階が2へ上がった瞬間の振動です。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            level2ReachedRumble;

        [Tooltip(
            "チャージ段階が3へ上がった瞬間の振動です。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            level3ReachedRumble;

        [Tooltip(
            "チャージ済みの状態で発射せず"
            + "保持している間の振動です。"
            + "継続振動にする場合はRDのLoopを有効にします。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            holdingChargeRumble;

        [Tooltip(
            "実際に弾を発射している間の振動です。"
            + "継続振動にする場合はRDのLoopを有効にします。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            firingRumble;

        [Header("Debug")]
        [SerializeField]
        private bool drawAimRay =
            true;

        [SerializeField]
        private bool logFiring;

        public Vector3 ShakeAxis =>
            shakeAxis.sqrMagnitude > 0.0001f
                ? shakeAxis.normalized
                : Vector3.up;

        public float ShakeThreshold =>
            shakeThreshold;

        public float ShakeResetThreshold =>
            shakeResetThreshold;

        public float ChargePerShake =>
            chargePerShake;

        public float ShakingStateDuration =>
            shakingStateDuration;

        public int MaximumChargeLevel =>
            3;

        public SprayTriggerMode TriggerMode =>
            triggerMode;

        public int MaximumProjectileCount =>
            maximumProjectileCount;

        public float ChargeConsumptionPerShot =>
            chargeConsumptionPerShot > 0.0f
                ? chargeConsumptionPerShot
                : 1.0f / maximumProjectileCount;

        public float ProjectileInterval =>
            projectileInterval;

        public LayerMask AimLayerMask =>
            aimLayerMask;

        public float MaximumAimDistance =>
            maximumAimDistance;

        public float FallbackAimDistance =>
            fallbackAimDistance;

        public QueryTriggerInteraction
            AimQueryTriggerInteraction =>
                aimQueryTriggerInteraction;

        public SprayProjectile ProjectilePrefab =>
            projectilePrefab;

        public float ProjectileSpeed =>
            projectileSpeed;

        public float ProjectileRadius =>
            projectileRadius;

        public float ProjectileLifetime =>
            projectileLifetime;

        public LayerMask ProjectileCollisionLayerMask =>
            projectileCollisionLayerMask;

        public QueryTriggerInteraction
            ProjectileQueryTriggerInteraction =>
                projectileQueryTriggerInteraction;

        public float DamagePerProjectile =>
            damagePerProjectile;

        public GameObject MuzzleEffectPrefab =>
            muzzleEffectPrefab;

        public float MuzzleEffectLifetime =>
            muzzleEffectLifetime;

        public GameObject HitEffectPrefab =>
            hitEffectPrefab;

        public float HitEffectLifetime =>
            hitEffectLifetime;

        public ControllerRumbleDefinition
            ProjectileHitRumble =>
                projectileHitRumble;

        public ControllerRumbleDefinition
            HoldingChargeRumble =>
                holdingChargeRumble;

        public ControllerRumbleDefinition
            FiringRumble =>
                firingRumble;

        public bool DrawAimRay =>
            drawAimRay;

        public bool LogFiring =>
            logFiring;

        public LayerMask EnemyAimLayerMask =>
    enemyAimLayerMask;

public float EnemyAimAssistRadius =>
    enemyAimAssistRadius;

public QueryTriggerInteraction
    EnemyAimQueryTriggerInteraction =>
        enemyAimQueryTriggerInteraction;

        public SprayBeamDamageArea BeamPrefab =>
    beamPrefab;

public float BeamChargeConsumptionPerSecond =>
    beamChargeConsumptionPerSecond;

public float BeamDamage =>
    beamDamage;

public float BeamDamageInterval =>
    beamDamageInterval;

public bool BeamDamageImmediately =>
    beamDamageImmediately;

public LayerMask BeamTargetLayerMask =>
    beamTargetLayerMask;

public QueryTriggerInteraction
    BeamQueryTriggerInteraction =>
        beamQueryTriggerInteraction;

        public int GetChargeLevel(
            float normalizedCharge
        )
        {
            float charge =
                Mathf.Clamp01(
                    normalizedCharge
                );

            if (charge <= 0.0001f)
            {
                return 0;
            }

            float level1Threshold =
                Mathf.Max(
                    0.0001f,
                    level1.MinimumCharge
                );

            float level2Threshold =
                Mathf.Max(
                    level1Threshold,
                    level2.MinimumCharge
                );

            float level3Threshold =
                Mathf.Max(
                    level2Threshold,
                    level3.MinimumCharge
                );

            if (charge >= level3Threshold)
            {
                return 3;
            }

            if (charge >= level2Threshold)
            {
                return 2;
            }

            if (charge >= level1Threshold)
            {
                return 1;
            }

            return 0;
        }

        public ChargeLevelSettings GetLevelSettings(
            int level
        )
        {
            switch (
                Mathf.Clamp(
                    level,
                    1,
                    3
                )
            )
            {
                case 3:
                    return level3;

                case 2:
                    return level2;

                default:
                    return level1;
            }
        }

        public float GetHoldingRumbleMultiplier(
            int level
        )
        {
            ChargeLevelSettings settings =
                GetLevelSettings(
                    level
                );

            return settings != null
                ? settings.HoldingRumbleMultiplier
                : 1.0f;
        }

        public float GetFiringRumbleMultiplier(
            int level
        )
        {
            ChargeLevelSettings settings =
                GetLevelSettings(
                    level
                );

            return settings != null
                ? settings.FiringRumbleMultiplier
                : 1.0f;
        }

        public ControllerRumbleDefinition
            GetLevelReachedRumble(
                int level
            )
        {
            switch (level)
            {
                case 3:
                    return level3ReachedRumble;

                case 2:
                    return level2ReachedRumble;

                case 1:
                    return level1ReachedRumble;

                default:
                    return null;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            beamChargeConsumptionPerSecond =
    Mathf.Max(
        0.0001f,
        beamChargeConsumptionPerSecond
    );

beamDamage =
    Mathf.Max(
        0.0f,
        beamDamage
    );

beamDamageInterval =
    Mathf.Max(
        0.01f,
        beamDamageInterval
    );

            enemyAimAssistRadius =
    Mathf.Max(
        0.01f,
        enemyAimAssistRadius
    );

            shakeThreshold =
                Mathf.Max(
                    0.0f,
                    shakeThreshold
                );

            shakeResetThreshold =
                Mathf.Clamp(
                    shakeResetThreshold,
                    0.0f,
                    shakeThreshold
                );

            chargePerShake =
                Mathf.Clamp(
                    chargePerShake,
                    0.01f,
                    1.0f
                );

            shakingStateDuration =
                Mathf.Max(
                    0.01f,
                    shakingStateDuration
                );

            maximumProjectileCount =
                Mathf.Max(
                    1,
                    maximumProjectileCount
                );

            chargeConsumptionPerShot =
                Mathf.Clamp01(
                    chargeConsumptionPerShot
                );

            projectileInterval =
                Mathf.Max(
                    0.01f,
                    projectileInterval
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

            projectileSpeed =
                Mathf.Max(
                    0.1f,
                    projectileSpeed
                );

            projectileRadius =
                Mathf.Max(
                    0.01f,
                    projectileRadius
                );

            projectileLifetime =
                Mathf.Max(
                    0.1f,
                    projectileLifetime
                );

            damagePerProjectile =
                Mathf.Max(
                    0.0f,
                    damagePerProjectile
                );

            muzzleEffectLifetime =
                Mathf.Max(
                    0.0f,
                    muzzleEffectLifetime
                );

            hitEffectLifetime =
                Mathf.Max(
                    0.0f,
                    hitEffectLifetime
                );

            if (level1 != null)
            {
                level1.Validate();
            }

            if (level2 != null)
            {
                level2.Validate();
            }

            if (level3 != null)
            {
                level3.Validate();
            }
        }
    }
}
