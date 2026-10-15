using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies.Spawning
{
    [CreateAssetMenu(
        fileName = "EC_EnemyCatalog",
        menuName = "Game/Enemies/Enemy Catalog"
    )]
    public sealed class EnemyCatalog :
        ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField]
            private string enemyId;

            [SerializeField]
            private GameObject enemyPrefab;

            public string EnemyId =>
                enemyId;

            public GameObject EnemyPrefab =>
                enemyPrefab;
        }

        [SerializeField]
        private Entry[] entries =
            Array.Empty<Entry>();

        private readonly Dictionary<
            string,
            GameObject
        > lookup =
            new Dictionary<
                string,
                GameObject
            >(
                StringComparer.OrdinalIgnoreCase
            );

        public void BuildLookup()
        {
            lookup.Clear();

            if (entries == null)
            {
                return;
            }

            for (
                int index = 0;
                index < entries.Length;
                index++
            )
            {
                Entry entry =
                    entries[index];

                if (
                    entry == null
                    ||
                    string.IsNullOrWhiteSpace(
                        entry.EnemyId
                    )
                    ||
                    entry.EnemyPrefab == null
                )
                {
                    continue;
                }

                string id =
                    entry.EnemyId.Trim();

                if (lookup.ContainsKey(id))
                {
                    Debug.LogError(
                        $"[Enemy Catalog] "
                        + $"EnemyIdが重複しています。"
                        + $" Id={id}",
                        this
                    );

                    continue;
                }

                lookup.Add(
                    id,
                    entry.EnemyPrefab
                );
            }
        }

        public bool TryGetPrefab(
            string enemyId,
            out GameObject prefab
        )
        {
            if (lookup.Count == 0)
            {
                BuildLookup();
            }

            string key =
                enemyId != null
                    ? enemyId.Trim()
                    : string.Empty;

            return lookup.TryGetValue(
                key,
                out prefab
            );
        }

        public bool Contains(
            string enemyId
        )
        {
            return TryGetPrefab(
                enemyId,
                out _
            );
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }
    }
}