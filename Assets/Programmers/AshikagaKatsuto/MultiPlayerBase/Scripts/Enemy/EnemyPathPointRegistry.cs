using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies.Path
{
    public sealed class EnemyPathPointRegistry :
        MonoBehaviour
    {
        [Header("Collection")]
        [Tooltip(
            "有効化時に子階層からEPPを"
            + "自動収集します。"
        )]
        [SerializeField]
        private bool collectOnEnable =
            true;

        [Tooltip(
            "非アクティブな子オブジェクトも"
            + "収集対象に含めます。"
        )]
        [SerializeField]
        private bool includeInactive =
            true;

        [Header("Debug")]
        [SerializeField]
        private bool logCollection;

        [SerializeField]
        private int registeredPointCount;

        private readonly Dictionary<
            EnemyPathStage,
            Dictionary<
                EnemyFieldPosition,
                EnemyPathPoint
            >
        > normalPoints =
            new Dictionary<
                EnemyPathStage,
                Dictionary<
                    EnemyFieldPosition,
                    EnemyPathPoint
                >
            >();

        private readonly Dictionary<
            EnemyDamageArea,
            EnemyPathPoint
        > damagePoints =
            new Dictionary<
                EnemyDamageArea,
                EnemyPathPoint
            >();

        public int RegisteredPointCount =>
            registeredPointCount;

        private void Awake()
        {
            CollectPoints();
        }

        private void OnEnable()
        {
            if (collectOnEnable)
            {
                CollectPoints();
            }
        }

        [ContextMenu("Collect Path Points")]
        public void CollectPoints()
        {
            normalPoints.Clear();
            damagePoints.Clear();

            EnemyPathPoint[] points =
                GetComponentsInChildren<
                    EnemyPathPoint
                >(
                    includeInactive
                );

            for (
                int index = 0;
                index < points.Length;
                index++
            )
            {
                RegisterPoint(
                    points[index]
                );
            }

            registeredPointCount =
                points.Length;

            if (logCollection)
            {
                Debug.Log(
                    $"[Enemy Path Registry] "
                    + $"Collected="
                    + $"{registeredPointCount}",
                    this
                );
            }
        }

        public bool TryGetPoint(
            EnemyPathStage stage,
            EnemyFieldPosition position,
            out EnemyPathPoint point
        )
        {
            point =
                null;

            if (stage == EnemyPathStage.Damage)
            {
                return false;
            }

            if (
                !normalPoints.TryGetValue(
                    stage,
                    out Dictionary<
                        EnemyFieldPosition,
                        EnemyPathPoint
                    > stagePoints
                )
            )
            {
                return false;
            }

            return stagePoints.TryGetValue(
                position,
                out point
            );
        }

        public bool TryGetDamagePoint(
            EnemyDamageArea damageArea,
            out EnemyPathPoint point
        )
        {
            return damagePoints.TryGetValue(
                damageArea,
                out point
            );
        }

        public EnemyPathPoint GetPointOrNull(
            EnemyPathStage stage,
            EnemyFieldPosition position
        )
        {
            TryGetPoint(
                stage,
                position,
                out EnemyPathPoint point
            );

            return point;
        }

        public EnemyPathPoint GetDamagePointOrNull(
            EnemyDamageArea damageArea
        )
        {
            TryGetDamagePoint(
                damageArea,
                out EnemyPathPoint point
            );

            return point;
        }

        public void SetAllDebugMarkersVisible(
            bool visible
        )
        {
            EnemyPathPoint[] points =
                GetComponentsInChildren<
                    EnemyPathPoint
                >(
                    true
                );

            for (
                int index = 0;
                index < points.Length;
                index++
            )
            {
                if (points[index] != null)
                {
                    points[index]
                        .SetDebugVisible(
                            visible
                        );
                }
            }
        }

        private void RegisterPoint(
            EnemyPathPoint point
        )
        {
            if (point == null)
            {
                return;
            }

            if (point.IsDamagePoint)
            {
                RegisterDamagePoint(
                    point
                );

                return;
            }

            if (
                !normalPoints.TryGetValue(
                    point.Stage,
                    out Dictionary<
                        EnemyFieldPosition,
                        EnemyPathPoint
                    > stagePoints
                )
            )
            {
                stagePoints =
                    new Dictionary<
                        EnemyFieldPosition,
                        EnemyPathPoint
                    >();

                normalPoints.Add(
                    point.Stage,
                    stagePoints
                );
            }

            if (
                stagePoints.TryGetValue(
                    point.FieldPosition,
                    out EnemyPathPoint existing
                )
            )
            {
                Debug.LogError(
                    $"[Enemy Path Registry] "
                    + $"重複したEPPがあります。"
                    + $" Stage={point.Stage}, "
                    + $"Position="
                    + $"{point.FieldPosition}, "
                    + $"Existing={existing.name}, "
                    + $"Duplicate={point.name}",
                    point
                );

                return;
            }

            stagePoints.Add(
                point.FieldPosition,
                point
            );
        }

        private void RegisterDamagePoint(
            EnemyPathPoint point
        )
        {
            if (
                damagePoints.TryGetValue(
                    point.DamageArea,
                    out EnemyPathPoint existing
                )
            )
            {
                Debug.LogError(
                    $"[Enemy Path Registry] "
                    + $"重複したDamage EPPがあります。"
                    + $" Area={point.DamageArea}, "
                    + $"Existing={existing.name}, "
                    + $"Duplicate={point.name}",
                    point
                );

                return;
            }

            damagePoints.Add(
                point.DamageArea,
                point
            );
        }
    }
}