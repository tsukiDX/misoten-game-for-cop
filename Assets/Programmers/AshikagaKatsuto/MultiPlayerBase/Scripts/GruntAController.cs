using Game.Combat;
using Game.Enemies.Targeting;
using UnityEngine;

namespace Game.Enemies.GruntA
{
    [DisallowMultipleComponent]
    public sealed class GruntAController :
        MonoBehaviour
    {
        private enum GruntAState
        {
            WaitingForTarget,
            Moving,
            AttackWindup,
            AttackCooldown,
            Dead
        }

        [Header("Definition")]
        [SerializeField]
        private GruntADefinition definition;

        [Header("References")]
        [SerializeField]
        private EnemyHealth enemyHealth;

        [Tooltip(
            "移動と距離判定の基準位置です。"
            + "未設定の場合は自身のTransformを使います。"
        )]
        [SerializeField]
        private Transform movementRoot;

        [Header("Runtime Target")]
        [SerializeField]
        private PlayerDamagePoint
            currentTarget;

        [SerializeField]
        private string currentTargetName;

        [SerializeField]
        private float currentHorizontalDistance;

        [Header("Runtime State")]
        [SerializeField]
        private GruntAState currentState =
            GruntAState.WaitingForTarget;

        [SerializeField]
        private float stateTimer;

        [SerializeField]
        private float retargetTimer;

        [SerializeField]
        private int successfulAttackCount;

        private PlayerDamagePoint[]
            damagePointBuffer =
                new PlayerDamagePoint[0];

        public bool IsDead =>
            currentState
            == GruntAState.Dead;

        public PlayerDamagePoint CurrentTarget =>
            currentTarget;

        private Transform MovementTransform =>
            movementRoot != null
                ? movementRoot
                : transform;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            SubscribeHealthEvents();

            ResetController();
        }

        private void OnDisable()
        {
            UnsubscribeHealthEvents();
        }

        private void Update()
        {
            if (
                definition == null
                ||
                enemyHealth == null
                ||
                !enemyHealth.IsAlive
                ||
                currentState
                == GruntAState.Dead
            )
            {
                return;
            }

            float deltaTime =
                Time.deltaTime;

            UpdateTargetValidity(
                deltaTime
            );

            switch (currentState)
            {
                case GruntAState
                    .WaitingForTarget:

                    UpdateWaitingForTarget(
                        deltaTime
                    );

                    break;

                case GruntAState.Moving:

                    UpdateMoving(
                        deltaTime
                    );

                    break;

                case GruntAState
                    .AttackWindup:

                    UpdateAttackWindup(
                        deltaTime
                    );

                    break;

                case GruntAState
                    .AttackCooldown:

                    UpdateAttackCooldown(
                        deltaTime
                    );

                    break;
            }
        }

        private void ResolveReferences()
        {
            if (enemyHealth == null)
            {
                enemyHealth =
                    GetComponent<EnemyHealth>();
            }

            if (movementRoot == null)
            {
                movementRoot =
                    transform;
            }
        }

        private void SubscribeHealthEvents()
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Defeated -=
                HandleDefeated;

            enemyHealth.Defeated +=
                HandleDefeated;
        }

        private void UnsubscribeHealthEvents()
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Defeated -=
                HandleDefeated;
        }

        private void ResetController()
        {
            currentTarget =
                null;

            currentTargetName =
                string.Empty;

            currentHorizontalDistance =
                0.0f;

            stateTimer =
                0.0f;

            retargetTimer =
                0.0f;

            successfulAttackCount =
                0;

            ChangeState(
                GruntAState
                    .WaitingForTarget
            );
        }

        private void UpdateTargetValidity(
            float deltaTime
        )
        {
            if (
                currentTarget != null
                &&
                currentTarget.IsTargetable
            )
            {
                return;
            }

            ClearTarget();

            retargetTimer -=
                deltaTime;

            if (retargetTimer > 0.0f)
            {
                return;
            }

            retargetTimer =
                definition.RetargetInterval;

            TryAcquireNearestTarget();
        }

        private void UpdateWaitingForTarget(
            float deltaTime
        )
        {
            retargetTimer -=
                deltaTime;

            if (retargetTimer > 0.0f)
            {
                return;
            }

            retargetTimer =
                definition.RetargetInterval;

            if (!TryAcquireNearestTarget())
            {
                return;
            }

            ChangeState(
                GruntAState.Moving
            );
        }

        private void UpdateMoving(
            float deltaTime
        )
        {
            if (!HasValidTarget())
            {
                ChangeState(
                    GruntAState
                        .WaitingForTarget
                );

                return;
            }

            Vector3 currentPosition =
                MovementTransform.position;

            Vector3 targetPosition =
                currentTarget.Position;

            targetPosition.y =
                currentPosition.y;

            Vector3 toTarget =
                targetPosition
                - currentPosition;

            currentHorizontalDistance =
                toTarget.magnitude;

            if (
                currentHorizontalDistance
                <= definition.AttackRange
            )
            {
                ChangeState(
                    GruntAState
                        .AttackWindup
                );

                return;
            }

            RotateTowardsTarget(
                toTarget,
                deltaTime
            );

            float stopDistance =
                Mathf.Max(
                    0.0f,
                    definition
                        .StoppingDistance
                );

            Vector3 movementTarget =
                targetPosition;

            if (
                currentHorizontalDistance
                > stopDistance
                &&
                stopDistance > 0.0f
            )
            {
                Vector3 targetDirection =
                    toTarget.normalized;

                movementTarget =
                    targetPosition
                    - targetDirection
                    * stopDistance;
            }

            MovementTransform.position =
                Vector3.MoveTowards(
                    currentPosition,
                    movementTarget,
                    definition.MoveSpeed
                    * deltaTime
                );
        }

        private void UpdateAttackWindup(
            float deltaTime
        )
        {
            if (!HasValidTarget())
            {
                ChangeState(
                    GruntAState
                        .WaitingForTarget
                );

                return;
            }

            UpdateDistanceToTarget();

            if (
                currentHorizontalDistance
                > definition.AttackRange
            )
            {
                ChangeState(
                    GruntAState.Moving
                );

                return;
            }

            RotateTowardsTarget(
                GetHorizontalDirectionToTarget(),
                deltaTime
            );

            stateTimer -=
                deltaTime;

            if (stateTimer > 0.0f)
            {
                return;
            }

            PerformAttack();

            ChangeState(
                GruntAState
                    .AttackCooldown
            );
        }

        private void UpdateAttackCooldown(
            float deltaTime
        )
        {
            if (!HasValidTarget())
            {
                ChangeState(
                    GruntAState
                        .WaitingForTarget
                );

                return;
            }

            UpdateDistanceToTarget();

            RotateTowardsTarget(
                GetHorizontalDirectionToTarget(),
                deltaTime
            );

            stateTimer -=
                deltaTime;

            if (stateTimer > 0.0f)
            {
                return;
            }

            if (
                currentHorizontalDistance
                <= definition.AttackRange
            )
            {
                ChangeState(
                    GruntAState
                        .AttackWindup
                );
            }
            else
            {
                ChangeState(
                    GruntAState.Moving
                );
            }
        }

        private bool TryAcquireNearestTarget()
        {
            damagePointBuffer =
                FindObjectsByType<
                    PlayerDamagePoint
                >(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            PlayerDamagePoint bestTarget =
                null;

            float bestSqrDistance =
                float.PositiveInfinity;

            Vector3 currentPosition =
                MovementTransform.position;

            for (
                int index = 0;
                index
                < damagePointBuffer.Length;
                index++
            )
            {
                PlayerDamagePoint candidate =
                    damagePointBuffer[index];

                if (
                    candidate == null
                    ||
                    !candidate.IsTargetable
                )
                {
                    continue;
                }

                Vector3 difference =
                    candidate.Position
                    - currentPosition;

                difference.y =
                    0.0f;

                float sqrDistance =
                    difference.sqrMagnitude;

                if (
                    sqrDistance
                    >= bestSqrDistance
                )
                {
                    continue;
                }

                bestSqrDistance =
                    sqrDistance;

                bestTarget =
                    candidate;
            }

            currentTarget =
                bestTarget;

            currentTargetName =
                currentTarget != null
                    ? currentTarget.name
                    : string.Empty;

            if (currentTarget == null)
            {
                return false;
            }

            UpdateDistanceToTarget();

            if (
                currentHorizontalDistance
                <= definition.AttackRange
            )
            {
                ChangeState(
                    GruntAState
                        .AttackWindup
                );
            }
            else
            {
                ChangeState(
                    GruntAState.Moving
                );
            }

            return true;
        }

        private void PerformAttack()
        {
            if (!HasValidTarget())
            {
                return;
            }

            Vector3 attackDirection =
                currentTarget.Position
                - MovementTransform.position;

            attackDirection.y =
                0.0f;

            if (
                attackDirection.sqrMagnitude
                <= 0.0001f
            )
            {
                attackDirection =
                    MovementTransform.forward;
            }

            bool damageApplied =
                currentTarget
                    .ApplyEnemyDamage(
                        definition
                            .AttackDamage,
                        currentTarget
                            .Position,
                        attackDirection
                            .normalized
                    );

            if (damageApplied)
            {
                successfulAttackCount++;
            }

            if (
                definition.LogStateChanges
            )
            {
                Debug.Log(
                    $"[Grunt A Attack] "
                    + $"Target="
                    + $"{currentTargetName}, "
                    + $"Damage="
                    + $"{definition.AttackDamage:F2}, "
                    + $"Applied="
                    + $"{damageApplied}",
                    this
                );
            }

            if (!currentTarget.IsTargetable)
            {
                ClearTarget();
            }
        }

        private bool HasValidTarget()
        {
            return
                currentTarget != null
                &&
                currentTarget.IsTargetable;
        }

        private void UpdateDistanceToTarget()
        {
            if (currentTarget == null)
            {
                currentHorizontalDistance =
                    0.0f;

                return;
            }

            Vector3 difference =
                currentTarget.Position
                - MovementTransform.position;

            difference.y =
                0.0f;

            currentHorizontalDistance =
                difference.magnitude;
        }

        private Vector3
            GetHorizontalDirectionToTarget()
        {
            if (currentTarget == null)
            {
                return Vector3.zero;
            }

            Vector3 direction =
                currentTarget.Position
                - MovementTransform.position;

            direction.y =
                0.0f;

            return direction;
        }

        private void RotateTowardsTarget(
            Vector3 direction,
            float deltaTime
        )
        {
            if (
                direction.sqrMagnitude
                <= 0.0001f
            )
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );

            MovementTransform.rotation =
                Quaternion.RotateTowards(
                    MovementTransform.rotation,
                    targetRotation,
                    definition
                        .RotationSpeed
                    * deltaTime
                );
        }

        private void ClearTarget()
        {
            currentTarget =
                null;

            currentTargetName =
                string.Empty;

            currentHorizontalDistance =
                0.0f;
        }

        private void ChangeState(
            GruntAState newState
        )
        {
            if (
                currentState
                == GruntAState.Dead
            )
            {
                return;
            }

            if (
                currentState
                == newState
            )
            {
                return;
            }

            GruntAState previousState =
                currentState;

            currentState =
                newState;

            switch (currentState)
            {
                case GruntAState
                    .WaitingForTarget:

                    stateTimer =
                        0.0f;

                    break;

                case GruntAState.Moving:

                    stateTimer =
                        0.0f;

                    break;

                case GruntAState
                    .AttackWindup:

                    stateTimer =
                        definition
                            .AttackWindup;

                    break;

                case GruntAState
                    .AttackCooldown:

                    stateTimer =
                        definition
                            .AttackInterval;

                    break;

                case GruntAState.Dead:

                    stateTimer =
                        0.0f;

                    break;
            }

            if (
                definition != null
                &&
                definition.LogStateChanges
            )
            {
                Debug.Log(
                    $"[Grunt A State] "
                    + $"{previousState} "
                    + $"-> {currentState}",
                    this
                );
            }
        }

        private void HandleDefeated(
            EnemyHealth health,
            DamageInfo damageInfo
        )
        {
            ClearTarget();

            currentState =
                GruntAState.Dead;

            stateTimer =
                0.0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null)
            {
                return;
            }

            Color previousColor =
                Gizmos.color;

            if (definition.DrawAttackRange)
            {
                Gizmos.color =
                    new Color(
                        1.0f,
                        0.2f,
                        0.2f,
                        0.9f
                    );

                Gizmos.DrawWireSphere(
                    MovementTransform.position,
                    definition.AttackRange
                );
            }

            if (
                definition.DrawTargetLine
                &&
                currentTarget != null
            )
            {
                Gizmos.color =
                    Color.yellow;

                Gizmos.DrawLine(
                    MovementTransform.position,
                    currentTarget.Position
                );
            }

            Gizmos.color =
                previousColor;
        }
    }
}