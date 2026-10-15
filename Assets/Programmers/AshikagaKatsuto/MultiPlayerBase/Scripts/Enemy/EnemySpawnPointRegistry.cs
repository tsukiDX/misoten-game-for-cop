using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies.Spawning
{
    public sealed class EnemySpawnPointRegistry :
        MonoBehaviour
    {
        [SerializeField]
        private bool includeInactive =
            true;

        [SerializeField]
        private bool collectOnEnable =
            true;

        [Header("Runtime Debug")]
        [SerializeField]
        private int registeredPointCount;

        private readonly Dictionary<
            string,
            EnemySpawnPoint
        > lookup =
            new Dictionary<
                string,
                EnemySpawnPoint
            >(
                StringComparer.OrdinalIgnoreCase
            );

        public int RegisteredPointCount =>
            registeredPointCount;

        private void Awake()
        {
            CollectSpawnPoints();
        }

        private void OnEnable()
        {
            if (collectOnEnable)
            {
                CollectSpawnPoints();
            }
        }

        [ContextMenu("Collect Spawn Points")]
        public void CollectSpawnPoints()
        {
            lookup.Clear();

            EnemySpawnPoint[] points =
                GetComponentsInChildren<
                    EnemySpawnPoint
                >(
                    includeInactive
                );

            for (
                int index = 0;
                index < points.Length;
                index++
            )
            {
                EnemySpawnPoint point =
                    points[index];

                if (
                    point == null
                    ||
                    string.IsNullOrWhiteSpace(
                        point.SpawnPointId
                    )
                )
                {
                    continue;
                }

                string id =
                    point.SpawnPointId.Trim();

                if (
                    lookup.TryGetValue(
                        id,
                        out EnemySpawnPoint existing
                    )
                )
                {
                    Debug.LogError(
                        $"[Enemy Spawn Registry] "
                        + $"SpawnPointIdが重複しています。"
                        + $" Id={id}, "
                        + $"Existing={existing.name}, "
                        + $"Duplicate={point.name}",
                        point
                    );

                    continue;
                }

                lookup.Add(
                    id,
                    point
                );
            }

            registeredPointCount =
                lookup.Count;
        }

        public bool TryGetSpawnPoint(
            string spawnPointId,
            out EnemySpawnPoint point
        )
        {
            if (lookup.Count == 0)
            {
                CollectSpawnPoints();
            }

            string key =
                spawnPointId != null
                    ? spawnPointId.Trim()
                    : string.Empty;

            return lookup.TryGetValue(
                key,
                out point
            );
        }

        public bool Contains(
            string spawnPointId
        )
        {
            return TryGetSpawnPoint(
                spawnPointId,
                out _
            );
        }
    }
}