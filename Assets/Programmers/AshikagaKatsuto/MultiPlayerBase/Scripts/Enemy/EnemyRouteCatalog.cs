using System;
using System.Collections.Generic;
using Game.Enemies.Path;
using UnityEngine;

namespace Game.Enemies.Spawning
{
    [CreateAssetMenu(
        fileName = "ERC_EnemyRouteCatalog",
        menuName = "Game/Enemies/Enemy Route Catalog"
    )]
    public sealed class EnemyRouteCatalog :
        ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField]
            private string routeId;

            [SerializeField]
            private EnemyRouteDefinition
                routeDefinition;

            public string RouteId =>
                routeId;

            public EnemyRouteDefinition
                RouteDefinition =>
                    routeDefinition;
        }

        [SerializeField]
        private Entry[] entries =
            Array.Empty<Entry>();

        private readonly Dictionary<
            string,
            EnemyRouteDefinition
        > lookup =
            new Dictionary<
                string,
                EnemyRouteDefinition
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
                        entry.RouteId
                    )
                    ||
                    entry.RouteDefinition == null
                )
                {
                    continue;
                }

                string id =
                    entry.RouteId.Trim();

                if (lookup.ContainsKey(id))
                {
                    Debug.LogError(
                        $"[Enemy Route Catalog] "
                        + $"RouteIdが重複しています。"
                        + $" Id={id}",
                        this
                    );

                    continue;
                }

                lookup.Add(
                    id,
                    entry.RouteDefinition
                );
            }
        }

        public bool TryGetRoute(
            string routeId,
            out EnemyRouteDefinition route
        )
        {
            if (lookup.Count == 0)
            {
                BuildLookup();
            }

            string key =
                routeId != null
                    ? routeId.Trim()
                    : string.Empty;

            return lookup.TryGetValue(
                key,
                out route
            );
        }

        public bool Contains(
            string routeId
        )
        {
            return TryGetRoute(
                routeId,
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