using UnityEngine;

namespace Game.Enemies.GruntA
{
    [CreateAssetMenu(
        fileName = "ED_GruntA",
        menuName = "Game/Enemies/Grunt A Definition"
    )]
    public sealed class GruntADefinition :
        ScriptableObject
    {
        [Header("Movement")]
        [SerializeField]
        [Min(0.0f)]
        private float moveSpeed =
            2.0f;

        [SerializeField]
        [Min(0.0f)]
        private float rotationSpeed =
            360.0f;

        [Tooltip(
            "DamagePointへこの距離まで近づくと"
            + "移動を停止します。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float stoppingDistance =
            1.0f;

        [Header("Targeting")]
        [Tooltip(
            "有効なターゲットがない場合や"
            + "ターゲットを失った場合の"
            + "再検索間隔です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float retargetInterval =
            0.25f;

        [Header("Attack")]
        [SerializeField]
        [Min(0.0f)]
        private float attackDamage =
            10.0f;

        [Tooltip(
            "DamagePointがこの距離以内にあると"
            + "攻撃を開始します。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float attackRange =
            1.2f;

        [Tooltip(
            "攻撃開始からダメージ発生までの"
            + "予備時間です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float attackWindup =
            0.4f;

        [Tooltip(
            "ダメージ発生後、次の攻撃を"
            + "開始できるまでの時間です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float attackInterval =
            1.5f;

        [Header("Debug")]
        [SerializeField]
        private bool drawTargetLine =
            true;

        [SerializeField]
        private bool drawAttackRange =
            true;

        [SerializeField]
        private bool logStateChanges;

        public float MoveSpeed =>
            moveSpeed;

        public float RotationSpeed =>
            rotationSpeed;

        public float StoppingDistance =>
            stoppingDistance;

        public float RetargetInterval =>
            retargetInterval;

        public float AttackDamage =>
            attackDamage;

        public float AttackRange =>
            attackRange;

        public float AttackWindup =>
            attackWindup;

        public float AttackInterval =>
            attackInterval;

        public bool DrawTargetLine =>
            drawTargetLine;

        public bool DrawAttackRange =>
            drawAttackRange;

        public bool LogStateChanges =>
            logStateChanges;

        private void OnValidate()
        {
            moveSpeed =
                Mathf.Max(
                    0.0f,
                    moveSpeed
                );

            rotationSpeed =
                Mathf.Max(
                    0.0f,
                    rotationSpeed
                );

            stoppingDistance =
                Mathf.Max(
                    0.0f,
                    stoppingDistance
                );

            retargetInterval =
                Mathf.Max(
                    0.01f,
                    retargetInterval
                );

            attackDamage =
                Mathf.Max(
                    0.0f,
                    attackDamage
                );

            attackRange =
                Mathf.Max(
                    stoppingDistance,
                    attackRange
                );

            attackWindup =
                Mathf.Max(
                    0.0f,
                    attackWindup
                );

            attackInterval =
                Mathf.Max(
                    0.01f,
                    attackInterval
                );
        }
    }
}