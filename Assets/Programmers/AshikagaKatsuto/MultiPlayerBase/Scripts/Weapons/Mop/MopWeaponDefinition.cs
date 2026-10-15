using UnityEngine;
using Game.Feedback.Rumble;

namespace Game.Weapons.Mop
{
    [CreateAssetMenu(
        fileName = "WD_Mop",
        menuName = "Game/Weapons/Mop Weapon"
    )]
    public sealed class MopWeaponDefinition :
        WeaponDefinition
    {
        [Header("Pointer Targeting")]
        [Tooltip(
            "ポインターのSphereCastが判定する"
            + "敵のLayerです。"
        )]
        [SerializeField]
        private LayerMask targetLayerMask;

        [Tooltip(
            "ポインターから敵を検索する最大距離です。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float maximumAimDistance =
            100.0f;

        [Tooltip(
            "ポインター判定の半径です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float pointerRadius =
            0.2f;

        [SerializeField]
        private QueryTriggerInteraction
            targetQueryTriggerInteraction =
                QueryTriggerInteraction.Collide;

        [Header("Contact Damage")]
        [Tooltip(
            "モップではたいたときの"
            + "1回ごとのダメージです。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float damage =
            5.0f;

        [Tooltip(
            "同じ敵へ連続して通常ダメージを"
            + "与える間隔です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float damageInterval =
            0.2f;

        [SerializeField]
        private bool damageImmediatelyOnContact =
            true;

        [Header("Lock On Charge")]
        [Tooltip(
            "ロックオンを完成させるために必要な"
            + "通常攻撃の命中回数です。"
        )]
        [SerializeField]
        [Min(1)]
        private int requiredHitCount =
            4;

        [Tooltip(
            "ロックオン完成前に別の敵を攻撃した場合、"
            + "新しい敵へチャージ対象を切り替えます。"
        )]
        [SerializeField]
        private bool changeTargetBeforeLockOn =
            true;

        [Tooltip(
            "ロックオン完成後は、別の敵をはたいても"
            + "ロックオン対象を固定します。"
        )]
        [SerializeField]
        private bool keepTargetAfterLockOn =
            true;

        [Header("Homing Projectile")]
        [Tooltip(
            "ロックオン完成後に生成する"
            + "投射用モップPrefabです。"
        )]
        [SerializeField]
        private MopHomingProjectile
            homingProjectilePrefab;

        [Tooltip(
            "ホーミング攻撃のダメージです。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float homingDamage =
            30.0f;

        [Tooltip(
            "投射モップの飛行速度です。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float homingSpeed =
            15.0f;

        [Tooltip(
            "1秒あたりに旋回できる角度です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float homingTurnSpeed =
            360.0f;

        [Tooltip(
            "投射モップの命中判定半径です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float homingHitRadius =
            0.3f;

        [Tooltip(
            "投射モップが存在できる最大時間です。"
        )]
        [SerializeField]
        [Min(0.1f)]
        private float homingLifetime =
            5.0f;

        [Tooltip(
            "投射モップが衝突するLayerです。"
            + "敵と壁などを含めます。"
        )]
        [SerializeField]
        private LayerMask
            homingCollisionLayerMask;

        [SerializeField]
        private QueryTriggerInteraction
            homingQueryTriggerInteraction =
                QueryTriggerInteraction.Collide;

        [Tooltip(
            "敵のCollider中心から加算する"
            + "ワールド座標オフセットです。"
        )]
        [SerializeField]
        private Vector3 homingTargetOffset =
            Vector3.zero;

        [Header("Rumble Feedback")]
        [Tooltip(
            "通常攻撃が敵へ命中した瞬間の"
            + "振動パターンです。"
            + "未設定でも攻撃は動作します。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            contactHitRumble;

        [Tooltip(
            "ホーミング投射物を発射した直後の"
            + "振動パターンです。"
            + "未設定でも発射は動作します。"
        )]
        [SerializeField]
        private ControllerRumbleDefinition
            homingLaunchRumble;

        [Header("Effects")]
        [Tooltip(
            "通常攻撃が敵へ当たったときの"
            + "エフェクトです。"
        )]
        [SerializeField]
        private GameObject hitEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float hitEffectLifetime =
            1.0f;

        [Tooltip(
            "投射モップを発射した位置へ"
            + "生成するエフェクトです。"
        )]
        [SerializeField]
        private GameObject launchEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float launchEffectLifetime =
            1.0f;

        [Tooltip(
            "ホーミング攻撃が命中した位置へ"
            + "生成するエフェクトです。"
        )]
        [SerializeField]
        private GameObject homingHitEffectPrefab;

        [SerializeField]
        [Min(0.0f)]
        private float homingHitEffectLifetime =
            1.0f;

        [Header("Debug")]
        [SerializeField]
        private bool drawAimCast =
            true;

        [SerializeField]
        private bool drawHitPosition =
            true;

        [SerializeField]
        private bool logDamageDetection;

        [SerializeField]
        private bool logLockOn;

        [SerializeField]
        private bool logHomingAttack;

        public LayerMask TargetLayerMask =>
            targetLayerMask;

        public float MaximumAimDistance =>
            maximumAimDistance;

        public float PointerRadius =>
            pointerRadius;

        public QueryTriggerInteraction
            TargetQueryTriggerInteraction =>
                targetQueryTriggerInteraction;

        public float Damage =>
            damage;

        public float DamageInterval =>
            damageInterval;

        public bool DamageImmediatelyOnContact =>
            damageImmediatelyOnContact;

        public int RequiredHitCount =>
            requiredHitCount;

        public bool ChangeTargetBeforeLockOn =>
            changeTargetBeforeLockOn;

        public bool KeepTargetAfterLockOn =>
            keepTargetAfterLockOn;

        public MopHomingProjectile
            HomingProjectilePrefab =>
                homingProjectilePrefab;

        public float HomingDamage =>
            homingDamage;

        public float HomingSpeed =>
            homingSpeed;

        public float HomingTurnSpeed =>
            homingTurnSpeed;

        public float HomingHitRadius =>
            homingHitRadius;

        public float HomingLifetime =>
            homingLifetime;

        public LayerMask HomingCollisionLayerMask =>
            homingCollisionLayerMask;

        public QueryTriggerInteraction
            HomingQueryTriggerInteraction =>
                homingQueryTriggerInteraction;

        public Vector3 HomingTargetOffset =>
            homingTargetOffset;

        public GameObject HitEffectPrefab =>
            hitEffectPrefab;

        public float HitEffectLifetime =>
            hitEffectLifetime;

        public GameObject LaunchEffectPrefab =>
            launchEffectPrefab;

        public float LaunchEffectLifetime =>
            launchEffectLifetime;

        public GameObject HomingHitEffectPrefab =>
            homingHitEffectPrefab;

        public float HomingHitEffectLifetime =>
            homingHitEffectLifetime;

        public bool DrawAimCast =>
            drawAimCast;

        public bool DrawHitPosition =>
            drawHitPosition;

        public bool LogDamageDetection =>
            logDamageDetection;

        public bool LogLockOn =>
            logLockOn;

        public bool LogHomingAttack =>
            logHomingAttack;

        public ControllerRumbleDefinition
            ContactHitRumble =>
                contactHitRumble;

        public ControllerRumbleDefinition
            HomingLaunchRumble =>
                homingLaunchRumble;

        protected override void OnValidate()
        {
            base.OnValidate();

            maximumAimDistance =
                Mathf.Max(
                    0.1f,
                    maximumAimDistance
                );

            pointerRadius =
                Mathf.Max(
                    0.01f,
                    pointerRadius
                );

            damage =
                Mathf.Max(
                    0.0f,
                    damage
                );

            damageInterval =
                Mathf.Max(
                    0.01f,
                    damageInterval
                );

            requiredHitCount =
                Mathf.Max(
                    1,
                    requiredHitCount
                );

            homingDamage =
                Mathf.Max(
                    0.0f,
                    homingDamage
                );

            homingSpeed =
                Mathf.Max(
                    0.1f,
                    homingSpeed
                );

            homingTurnSpeed =
                Mathf.Max(
                    0.0f,
                    homingTurnSpeed
                );

            homingHitRadius =
                Mathf.Max(
                    0.01f,
                    homingHitRadius
                );

            homingLifetime =
                Mathf.Max(
                    0.1f,
                    homingLifetime
                );

            hitEffectLifetime =
                Mathf.Max(
                    0.0f,
                    hitEffectLifetime
                );

            launchEffectLifetime =
                Mathf.Max(
                    0.0f,
                    launchEffectLifetime
                );

            homingHitEffectLifetime =
                Mathf.Max(
                    0.0f,
                    homingHitEffectLifetime
                );
        }
    }
}