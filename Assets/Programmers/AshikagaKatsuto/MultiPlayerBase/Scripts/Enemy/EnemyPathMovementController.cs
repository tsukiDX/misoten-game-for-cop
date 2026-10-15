using System;
using UnityEngine;

namespace Game.Enemies.Path
{
    public sealed class EnemyPathMovementController :
        MonoBehaviour
    {
        private enum MovementState
        {
            Idle,
            MovingNormalRoute,
            WaitingAtNormalPoint,
            MovingToDamage,
            WaitingToAttack,
            AttackReady,
            Finished
        }

        [Header("References")]
        [SerializeField]
        private EnemyPathPointRegistry
            pathPointRegistry;

        [SerializeField]
        private EnemyRouteDefinition
            routeDefinition;

        [Header("Startup")]
        [Tooltip(
            "有効化されたときに自動的に"
            + "経路移動を開始します。"
        )]
        [SerializeField]
        private bool startOnEnable =
            true;

        [Header("Runtime Debug")]
        [SerializeField]
        private MovementState currentState =
            MovementState.Idle;

        [SerializeField]
        private int currentRouteStepIndex =
            -1;

        [SerializeField]
        private EnemyPathPoint currentTargetPoint;

        [SerializeField]
        private float elapsedTime;

        [SerializeField]
        private float waitTimer;

        [SerializeField]
        private bool deadlineReached;

        [SerializeField]
        private bool normalRouteCompleted;

        [SerializeField]
        private bool damageAttackTriggered;

        public event Action<
            EnemyPathPoint
        > TargetPointChanged;

        public event Action<
            EnemyPathPoint
        > PathPointReached;

        public event Action
            NormalRouteCompleted;

        public event Action
            DamageMovementStarted;

        public event Action<
            EnemyDamageArea
        > DamagePointReached;

        public event Action<
            EnemyDamageArea
        > DamageAttackRequested;

        public EnemyRouteDefinition RouteDefinition =>
            routeDefinition;

        public EnemyPathPoint CurrentTargetPoint =>
            currentTargetPoint;

        public int CurrentRouteStepIndex =>
            currentRouteStepIndex;

        public float ElapsedTime =>
            elapsedTime;

        public bool IsMovingToDamage =>
            currentState
            == MovementState.MovingToDamage;

        public bool IsAttackReady =>
            currentState
            == MovementState.AttackReady;

        private void Awake()
        {
            ResolveRegistry();
        }

        private void OnEnable()
        {
            if (startOnEnable)
            {
                StartRoute();
            }
        }

        private void Update()
        {
            if (
                routeDefinition == null
                ||
                pathPointRegistry == null
            )
            {
                return;
            }

            float deltaTime =
                Time.deltaTime;

            elapsedTime +=
                deltaTime;

            UpdateDeadline();

            switch (currentState)
            {
                case MovementState.MovingNormalRoute:
                    UpdateMovement(
                        deltaTime,
                        false
                    );
                    break;

                case MovementState.WaitingAtNormalPoint:
                    UpdateNormalWait(
                        deltaTime
                    );
                    break;

                case MovementState.MovingToDamage:
                    UpdateMovement(
                        deltaTime,
                        true
                    );
                    break;

                case MovementState.WaitingToAttack:
                    UpdateAttackWait(
                        deltaTime
                    );
                    break;
            }
        }

        [ContextMenu("Start Route")]
        public void StartRoute()
        {
            ResolveRegistry();

            ResetRuntimeState();

            if (routeDefinition == null)
            {
                Debug.LogError(
                    "[Enemy Path] "
                    + "Route Definitionが設定されていません。",
                    this
                );

                return;
            }

            if (pathPointRegistry == null)
            {
                Debug.LogError(
                    "[Enemy Path] "
                    + "EnemyPathPointRegistryが"
                    + "見つかりません。",
                    this
                );

                return;
            }

            pathPointRegistry.CollectPoints();

            if (
                routeDefinition.RouteStepCount
                <= 0
            )
            {
                normalRouteCompleted =
                    true;

                NormalRouteCompleted?.Invoke();

                TryBeginDamageMovement();

                return;
            }

            currentRouteStepIndex =
                0;

            if (
                !TrySetNormalRouteTarget(
                    currentRouteStepIndex
                )
            )
            {
                currentState =
                    MovementState.Idle;
            }
        }

        public void SetRouteDefinition(
            EnemyRouteDefinition definition,
            bool restartRoute
        )
        {
            routeDefinition =
                definition;

            if (restartRoute)
            {
                StartRoute();
            }
        }

        public void ForceMoveToDamage()
        {
            BeginDamageMovement();
        }

        public void StopMovement()
        {
            currentState =
                MovementState.Idle;

            SetTargetPoint(
                null
            );
        }

        public void MarkFinished()
        {
            currentState =
                MovementState.Finished;

            SetTargetPoint(
                null
            );
        }

        private void UpdateDeadline()
        {
            if (
                deadlineReached
                ||
                routeDefinition.AttackDeadline
                <= 0.0f
            )
            {
                return;
            }

            if (
                elapsedTime
                < routeDefinition.AttackDeadline
            )
            {
                return;
            }

            deadlineReached =
                true;

            if (
                routeDefinition.DamageTransitionMode
                == EnemyDamageTransitionMode
                    .AfterDeadline
                ||
                routeDefinition.DamageTransitionMode
                == EnemyDamageTransitionMode
                    .RouteCompletedOrDeadline
            )
            {
                BeginDamageMovement();
            }
        }

        private void UpdateMovement(
            float deltaTime,
            bool movingToDamage
        )
        {
            if (currentTargetPoint == null)
            {
                currentState =
                    MovementState.Idle;

                return;
            }

            Vector3 targetPosition =
                currentTargetPoint.Position;

            Vector3 difference =
                targetPosition
                - transform.position;

            float arrivalDistance =
                routeDefinition.ArrivalDistance;

            if (
                difference.sqrMagnitude
                <= arrivalDistance
                * arrivalDistance
            )
            {
                transform.position =
                    targetPosition;

                EnemyPathPoint reachedPoint =
                    currentTargetPoint;

                PathPointReached?.Invoke(
                    reachedPoint
                );

                if (movingToDamage)
                {
                    HandleDamagePointReached(
                        reachedPoint
                    );
                }
                else
                {
                    HandleNormalPointReached(
                        reachedPoint
                    );
                }

                return;
            }

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    routeDefinition.MoveSpeed
                    * deltaTime
                );

            UpdateRotation(
                difference,
                deltaTime
            );
        }

        private void UpdateRotation(
            Vector3 direction,
            float deltaTime
        )
        {
            if (
                !routeDefinition
                    .RotateTowardDestination
                ||
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

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    routeDefinition.RotationSpeed
                    * deltaTime
                );
        }

        private void HandleNormalPointReached(
            EnemyPathPoint point
        )
        {
            EnemyRouteDefinition.RouteStep step =
                routeDefinition.GetStep(
                    currentRouteStepIndex
                );

            if (
                step != null
                &&
                step.WaitDuration > 0.0f
            )
            {
                waitTimer =
                    step.WaitDuration;

                currentState =
                    MovementState
                        .WaitingAtNormalPoint;

                return;
            }

            AdvanceNormalRoute();
        }

        private void UpdateNormalWait(
            float deltaTime
        )
        {
            waitTimer =
                Mathf.Max(
                    0.0f,
                    waitTimer - deltaTime
                );

            if (waitTimer > 0.0f)
            {
                return;
            }

            AdvanceNormalRoute();
        }

        private void AdvanceNormalRoute()
        {
            currentRouteStepIndex++;

            if (
                currentRouteStepIndex
                < routeDefinition.RouteStepCount
            )
            {
                TrySetNormalRouteTarget(
                    currentRouteStepIndex
                );

                return;
            }

            normalRouteCompleted =
                true;

            NormalRouteCompleted?.Invoke();

            TryBeginDamageMovement();
        }

        private void TryBeginDamageMovement()
        {
            switch (
                routeDefinition
                    .DamageTransitionMode
            )
            {
                case EnemyDamageTransitionMode
                    .AfterRouteCompleted:

                    if (normalRouteCompleted)
                    {
                        BeginDamageMovement();
                    }
                    break;

                case EnemyDamageTransitionMode
                    .AfterDeadline:

                    if (deadlineReached)
                    {
                        BeginDamageMovement();
                    }
                    else
                    {
                        currentState =
                            MovementState.Idle;

                        SetTargetPoint(
                            null
                        );
                    }
                    break;

                case EnemyDamageTransitionMode
                    .RouteCompletedOrDeadline:

                    if (
                        normalRouteCompleted
                        ||
                        deadlineReached
                    )
                    {
                        BeginDamageMovement();
                    }
                    break;
            }
        }

        private void BeginDamageMovement()
        {
            if (
                routeDefinition == null
                ||
                pathPointRegistry == null
            )
            {
                return;
            }

            if (
                currentState
                == MovementState.MovingToDamage
                ||
                currentState
                == MovementState.WaitingToAttack
                ||
                currentState
                == MovementState.AttackReady
                ||
                currentState
                == MovementState.Finished
            )
            {
                return;
            }

            if (
                !pathPointRegistry
                    .TryGetDamagePoint(
                        routeDefinition.DamageArea,
                        out EnemyPathPoint damagePoint
                    )
            )
            {
                Debug.LogError(
                    $"[Enemy Path] "
                    + $"Damage EPPが見つかりません。"
                    + $" Area="
                    + $"{routeDefinition.DamageArea}",
                    this
                );

                currentState =
                    MovementState.Idle;

                return;
            }

            currentState =
                MovementState.MovingToDamage;

            SetTargetPoint(
                damagePoint
            );

            DamageMovementStarted?.Invoke();
        }

        private void HandleDamagePointReached(
            EnemyPathPoint point
        )
        {
            SetTargetPoint(
                null
            );

            DamagePointReached?.Invoke(
                point.DamageArea
            );

            if (
                routeDefinition.DamageAttackDelay
                > 0.0f
            )
            {
                waitTimer =
                    routeDefinition
                        .DamageAttackDelay;

                currentState =
                    MovementState
                        .WaitingToAttack;

                return;
            }

            TriggerDamageAttack();
        }

        private void UpdateAttackWait(
            float deltaTime
        )
        {
            waitTimer =
                Mathf.Max(
                    0.0f,
                    waitTimer - deltaTime
                );

            if (waitTimer > 0.0f)
            {
                return;
            }

            TriggerDamageAttack();
        }

        private void TriggerDamageAttack()
        {
            if (damageAttackTriggered)
            {
                return;
            }

            damageAttackTriggered =
                true;

            currentState =
                MovementState.AttackReady;

            EnemyDamageArea area =
                routeDefinition.DamageArea;

            Debug.Log(
                $"[Enemy Damage Request] "
                + $"Enemy={name}, "
                + $"Area={area}",
                this
            );

            DamageAttackRequested?.Invoke(
                area
            );
        }

        private bool TrySetNormalRouteTarget(
            int stepIndex
        )
        {
            EnemyRouteDefinition.RouteStep step =
                routeDefinition.GetStep(
                    stepIndex
                );

            if (step == null)
            {
                Debug.LogError(
                    $"[Enemy Path] "
                    + $"Route Stepがありません。"
                    + $" Index={stepIndex}",
                    this
                );

                return false;
            }

            if (
                !pathPointRegistry.TryGetPoint(
                    step.Stage,
                    step.Position,
                    out EnemyPathPoint point
                )
            )
            {
                Debug.LogError(
                    $"[Enemy Path] "
                    + $"EPPが見つかりません。"
                    + $" Step={stepIndex}, "
                    + $"Stage={step.Stage}, "
                    + $"Position={step.Position}",
                    this
                );

                return false;
            }

            currentState =
                MovementState
                    .MovingNormalRoute;

            SetTargetPoint(
                point
            );

            return true;
        }

        private void SetTargetPoint(
            EnemyPathPoint point
        )
        {
            if (currentTargetPoint == point)
            {
                return;
            }

            currentTargetPoint =
                point;

            TargetPointChanged?.Invoke(
                currentTargetPoint
            );
        }

        private void ResolveRegistry()
        {
            if (pathPointRegistry != null)
            {
                return;
            }

            pathPointRegistry =
                FindFirstObjectByType<
                    EnemyPathPointRegistry
                >();
        }

        private void ResetRuntimeState()
        {
            currentState =
                MovementState.Idle;

            currentRouteStepIndex =
                -1;

            currentTargetPoint =
                null;

            elapsedTime =
                0.0f;

            waitTimer =
                0.0f;

            deadlineReached =
                false;

            normalRouteCompleted =
                false;

            damageAttackTriggered =
                false;
        }

        private void OnDrawGizmosSelected()
        {
            if (currentTargetPoint == null)
            {
                return;
            }

            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                Color.cyan;

            Gizmos.DrawLine(
                transform.position,
                currentTargetPoint.Position
            );

            Gizmos.DrawWireSphere(
                currentTargetPoint.Position,
                0.2f
            );

            Gizmos.color =
                previousColor;
        }

        public void SetPathPointRegistry(
            EnemyPathPointRegistry registry
        )
        {
            pathPointRegistry =
                registry;
        }
    }
    
}