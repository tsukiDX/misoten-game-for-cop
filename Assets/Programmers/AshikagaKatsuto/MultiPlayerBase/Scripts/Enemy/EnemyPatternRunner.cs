using System.Collections.Generic;
using Game.Enemies.Path;
using UnityEngine;

namespace Game.Enemies.Spawning
{
    public sealed class EnemyPatternRunner :
        MonoBehaviour
    {
        [Header("Pattern")]
        [Tooltip(
            "ExcelからCSV UTF-8形式で保存した"
            + "敵出現パターンを設定します。"
        )]
        [SerializeField]
        private TextAsset patternCsv;

        [Header("Catalogs")]
        [SerializeField]
        private EnemyCatalog enemyCatalog;

        [SerializeField]
        private EnemyRouteCatalog routeCatalog;

        [Header("Scene Registries")]
        [SerializeField]
        private EnemySpawnPointRegistry
            spawnPointRegistry;

        [SerializeField]
        private EnemyPathPointRegistry
            pathPointRegistry;

        [Header("Playback")]
        [SerializeField]
        private bool playOnStart =
            true;

        [SerializeField]
        private bool loopPattern;

        [SerializeField]
        [Min(0.0f)]
        private float startDelay;

        [SerializeField]
        [Min(0.0f)]
        private float playbackSpeed =
            1.0f;

        [SerializeField]
        private bool logSpawnEvents =
            true;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool isPlaying;

        [SerializeField]
        private float elapsedTime;

        [SerializeField]
        private int nextEventIndex;

        [SerializeField]
        private int loadedEventCount;

        [SerializeField]
        private int spawnedEnemyCount;

        [SerializeField]
        private int skippedEventCount;

        [SerializeField]
        private float nextSpawnTime =
            -1.0f;

        private readonly List<EnemyPatternEntry>
            entries =
                new List<EnemyPatternEntry>();

        public bool IsPlaying =>
            isPlaying;

        public float ElapsedTime =>
            elapsedTime;

        public int LoadedEventCount =>
            loadedEventCount;

        public int SpawnedEnemyCount =>
            spawnedEnemyCount;

        private void Awake()
        {
            ResolveSceneReferences();
        }

        private void Start()
        {
            if (!playOnStart)
            {
                return;
            }

            LoadAndStartPattern();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            float scaledDeltaTime =
                Time.deltaTime
                * playbackSpeed;

            elapsedTime +=
                scaledDeltaTime;

            float patternTime =
                elapsedTime
                - startDelay;

            if (patternTime < 0.0f)
            {
                return;
            }

            while (
                nextEventIndex < entries.Count
                &&
                entries[nextEventIndex]
                    .SpawnTime
                <= patternTime
            )
            {
                SpawnEntry(
                    entries[nextEventIndex]
                );

                nextEventIndex++;
            }

            UpdateNextSpawnTime();

            if (nextEventIndex < entries.Count)
            {
                return;
            }

            if (loopPattern)
            {
                RestartPattern();
            }
            else
            {
                isPlaying =
                    false;

                nextSpawnTime =
                    -1.0f;
            }
        }

        [ContextMenu("Load And Start Pattern")]
        public void LoadAndStartPattern()
        {
            if (!LoadPattern())
            {
                return;
            }

            StartPattern();
        }

        [ContextMenu("Load Pattern")]
        public bool LoadPattern()
        {
            entries.Clear();

            loadedEventCount =
                0;

            if (patternCsv == null)
            {
                Debug.LogError(
                    "[Enemy Pattern] "
                    + "Pattern CSVが設定されていません。",
                    this
                );

                return false;
            }

            bool parsed =
                EnemyPatternCsvParser.TryParse(
                    patternCsv.text,
                    out List<EnemyPatternEntry>
                        parsedEntries,
                    out List<string> errors
                );

            for (
                int index = 0;
                index < errors.Count;
                index++
            )
            {
                Debug.LogError(
                    $"[Enemy Pattern CSV] "
                    + errors[index],
                    this
                );
            }

            entries.AddRange(
                parsedEntries
            );

            ValidateReferences();

            loadedEventCount =
                entries.Count;

            UpdateNextSpawnTime();

            if (loadedEventCount <= 0)
            {
                Debug.LogError(
                    "[Enemy Pattern] "
                    + "実行可能なイベントがありません。",
                    this
                );

                return false;
            }

            return parsed
                || loadedEventCount > 0;
        }

        [ContextMenu("Start Pattern")]
        public void StartPattern()
        {
            if (entries.Count == 0)
            {
                if (!LoadPattern())
                {
                    return;
                }
            }

            elapsedTime =
                0.0f;

            nextEventIndex =
                0;

            spawnedEnemyCount =
                0;

            skippedEventCount =
                0;

            isPlaying =
                true;

            UpdateNextSpawnTime();
        }

        [ContextMenu("Stop Pattern")]
        public void StopPattern()
        {
            isPlaying =
                false;
        }

        [ContextMenu("Restart Pattern")]
        public void RestartPattern()
        {
            elapsedTime =
                0.0f;

            nextEventIndex =
                0;

            spawnedEnemyCount =
                0;

            skippedEventCount =
                0;

            isPlaying =
                entries.Count > 0;

            UpdateNextSpawnTime();
        }

        private void SpawnEntry(
            EnemyPatternEntry entry
        )
        {
            if (
                enemyCatalog == null
                ||
                !enemyCatalog.TryGetPrefab(
                    entry.EnemyId,
                    out GameObject prefab
                )
            )
            {
                LogSkippedEntry(
                    entry,
                    $"EnemyId「{entry.EnemyId}」"
                    + "が見つかりません。"
                );

                return;
            }

            if (
                spawnPointRegistry == null
                ||
                !spawnPointRegistry
                    .TryGetSpawnPoint(
                        entry.SpawnPointId,
                        out EnemySpawnPoint spawnPoint
                    )
            )
            {
                LogSkippedEntry(
                    entry,
                    $"SpawnPointId"
                    + $"「{entry.SpawnPointId}」"
                    + "が見つかりません。"
                );

                return;
            }

            if (
                routeCatalog == null
                ||
                !routeCatalog.TryGetRoute(
                    entry.RouteId,
                    out EnemyRouteDefinition route
                )
            )
            {
                LogSkippedEntry(
                    entry,
                    $"RouteId「{entry.RouteId}」"
                    + "が見つかりません。"
                );

                return;
            }

            GameObject instance =
                Instantiate(
                    prefab,
                    spawnPoint.Position,
                    spawnPoint.Rotation
                );

            instance.name =
                prefab.name
                + "_"
                + entry.EventId;

            EnemyPathMovementController
                movement =
                    instance.GetComponent<
                        EnemyPathMovementController
                    >();

            if (movement == null)
            {
                Debug.LogError(
                    $"[Enemy Pattern] "
                    + $"生成した敵「{instance.name}」に"
                    + "EnemyPathMovementControllerが"
                    + "ありません。",
                    instance
                );

                Destroy(
                    instance
                );

                skippedEventCount++;

                return;
            }

            movement.SetPathPointRegistry(
                pathPointRegistry
            );

            movement.SetRouteDefinition(
                route,
                false
            );

            movement.StartRoute();

            spawnedEnemyCount++;

            if (logSpawnEvents)
            {
                Debug.Log(
                    $"[Enemy Pattern Spawn] "
                    + $"Event={entry.EventId}, "
                    + $"Time={entry.SpawnTime:F2}, "
                    + $"Enemy={entry.EnemyId}, "
                    + $"Spawn={entry.SpawnPointId}, "
                    + $"Route={entry.RouteId}",
                    instance
                );
            }
        }

        private void ValidateReferences()
        {
            ResolveSceneReferences();

            enemyCatalog?.BuildLookup();
            routeCatalog?.BuildLookup();
            spawnPointRegistry
                ?.CollectSpawnPoints();
            pathPointRegistry
                ?.CollectPoints();

            for (
                int index = 0;
                index < entries.Count;
                index++
            )
            {
                EnemyPatternEntry entry =
                    entries[index];

                if (
                    enemyCatalog == null
                    ||
                    !enemyCatalog.Contains(
                        entry.EnemyId
                    )
                )
                {
                    Debug.LogError(
                        $"[Enemy Pattern Validate] "
                        + $"Line "
                        + $"{entry.SourceLineNumber}: "
                        + $"EnemyId「{entry.EnemyId}」"
                        + "がCatalogにありません。",
                        this
                    );
                }

                if (
                    routeCatalog == null
                    ||
                    !routeCatalog.Contains(
                        entry.RouteId
                    )
                )
                {
                    Debug.LogError(
                        $"[Enemy Pattern Validate] "
                        + $"Line "
                        + $"{entry.SourceLineNumber}: "
                        + $"RouteId「{entry.RouteId}」"
                        + "がCatalogにありません。",
                        this
                    );
                }

                if (
                    spawnPointRegistry == null
                    ||
                    !spawnPointRegistry.Contains(
                        entry.SpawnPointId
                    )
                )
                {
                    Debug.LogError(
                        $"[Enemy Pattern Validate] "
                        + $"Line "
                        + $"{entry.SourceLineNumber}: "
                        + $"SpawnPointId"
                        + $"「{entry.SpawnPointId}」"
                        + "がSceneにありません。",
                        this
                    );
                }
            }
        }

        private void ResolveSceneReferences()
        {
            if (spawnPointRegistry == null)
            {
                spawnPointRegistry =
                    FindFirstObjectByType<
                        EnemySpawnPointRegistry
                    >();
            }

            if (pathPointRegistry == null)
            {
                pathPointRegistry =
                    FindFirstObjectByType<
                        EnemyPathPointRegistry
                    >();
            }
        }

        private void UpdateNextSpawnTime()
        {
            nextSpawnTime =
                nextEventIndex >= 0
                &&
                nextEventIndex < entries.Count
                    ? entries[nextEventIndex]
                        .SpawnTime
                    : -1.0f;
        }

        private void LogSkippedEntry(
            EnemyPatternEntry entry,
            string reason
        )
        {
            skippedEventCount++;

            Debug.LogError(
                $"[Enemy Pattern] "
                + $"Line={entry.SourceLineNumber}, "
                + $"Event={entry.EventId}: "
                + reason,
                this
            );
        }

        private void OnValidate()
        {
            startDelay =
                Mathf.Max(
                    0.0f,
                    startDelay
                );

            playbackSpeed =
                Mathf.Max(
                    0.0f,
                    playbackSpeed
                );
        }
    }
}