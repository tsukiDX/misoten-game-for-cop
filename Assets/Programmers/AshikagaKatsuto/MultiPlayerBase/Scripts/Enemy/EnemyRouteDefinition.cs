using System;
using UnityEngine;

namespace Game.Enemies.Path
{
    [CreateAssetMenu(
        fileName = "ERD_NewEnemyRoute",
        menuName = "Game/Enemies/Enemy Route"
    )]
    public sealed class EnemyRouteDefinition :
        ScriptableObject
    {
        [Serializable]
        public sealed class RouteStep
        {
            [Tooltip(
                "このステップで移動するEPPの段階です。"
                + "通常経路ではC、B、Aを使用してください。"
            )]
            [SerializeField]
            private EnemyPathStage stage =
                EnemyPathStage.C;

            [Tooltip(
                "このステップで移動する横位置です。"
            )]
            [SerializeField]
            private EnemyFieldPosition position =
                EnemyFieldPosition.Position0;

            [Tooltip(
                "このEPPへ到着後に待機する時間です。"
            )]
            [SerializeField]
            [Min(0.0f)]
            private float waitDuration;

            public EnemyPathStage Stage =>
                stage;

            public EnemyFieldPosition Position =>
                position;

            public float WaitDuration =>
                waitDuration;

            public void Validate()
            {
                waitDuration =
                    Mathf.Max(
                        0.0f,
                        waitDuration
                    );

                if (stage == EnemyPathStage.Damage)
                {
                    stage =
                        EnemyPathStage.A;
                }
            }
        }

        [Header("Normal Route")]
        [Tooltip(
            "敵が通常時に順番に移動するEPPです。"
            + "C、B、Aを組み合わせて設定します。"
        )]
        [SerializeField]
        private RouteStep[] routeSteps =
            Array.Empty<RouteStep>();

        [Header("Movement")]
        [SerializeField]
        [Min(0.01f)]
        private float moveSpeed =
            3.0f;

        [Tooltip(
            "EPPへ到着したと判定する距離です。"
        )]
        [SerializeField]
        [Min(0.001f)]
        private float arrivalDistance =
            0.05f;

        [Tooltip(
            "移動中にEPP方向へ向きを変えます。"
        )]
        [SerializeField]
        private bool rotateTowardDestination =
            true;

        [Tooltip(
            "敵が向きを変える速度です。"
            + "1秒あたりの回転角度です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float rotationSpeed =
            360.0f;

        [Header("Damage Transition")]
        [SerializeField]
        private EnemyDamageTransitionMode
            damageTransitionMode =
                EnemyDamageTransitionMode
                    .RouteCompletedOrDeadline;

        [Tooltip(
            "出現後、Damage EPPへ向かい始めるまでの"
            + "制限時間です。"
            + "AfterDeadlineを使用する場合に参照します。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float attackDeadline =
            10.0f;

        [Tooltip(
            "最後に移動するDamage EPPの種類です。"
        )]
        [SerializeField]
        private EnemyDamageArea damageArea =
            EnemyDamageArea.Position0;

        [Tooltip(
            "Damage EPPへ到着した後、"
            + "攻撃通知を出すまでの待機時間です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float damageAttackDelay =
            0.5f;

        public RouteStep[] RouteSteps =>
            routeSteps;

        public int RouteStepCount =>
            routeSteps != null
                ? routeSteps.Length
                : 0;

        public float MoveSpeed =>
            moveSpeed;

        public float ArrivalDistance =>
            arrivalDistance;

        public bool RotateTowardDestination =>
            rotateTowardDestination;

        public float RotationSpeed =>
            rotationSpeed;

        public EnemyDamageTransitionMode
            DamageTransitionMode =>
                damageTransitionMode;

        public float AttackDeadline =>
            attackDeadline;

        public EnemyDamageArea DamageArea =>
            damageArea;

        public float DamageAttackDelay =>
            damageAttackDelay;

        public RouteStep GetStep(
            int index
        )
        {
            if (
                routeSteps == null
                ||
                index < 0
                ||
                index >= routeSteps.Length
            )
            {
                return null;
            }

            return routeSteps[index];
        }

        private void OnValidate()
        {
            moveSpeed =
                Mathf.Max(
                    0.01f,
                    moveSpeed
                );

            arrivalDistance =
                Mathf.Max(
                    0.001f,
                    arrivalDistance
                );

            rotationSpeed =
                Mathf.Max(
                    0.0f,
                    rotationSpeed
                );

            attackDeadline =
                Mathf.Max(
                    0.0f,
                    attackDeadline
                );

            damageAttackDelay =
                Mathf.Max(
                    0.0f,
                    damageAttackDelay
                );

            if (routeSteps == null)
            {
                routeSteps =
                    Array.Empty<RouteStep>();

                return;
            }

            for (
                int index = 0;
                index < routeSteps.Length;
                index++
            )
            {
                routeSteps[index]?.Validate();
            }
        }
    }
}