namespace Game.Enemies.Spawning
{
    public sealed class EnemyPatternEntry
    {
        public int SourceLineNumber
        {
            get;
        }

        public string EventId
        {
            get;
        }

        public float SpawnTime
        {
            get;
        }

        public string EnemyId
        {
            get;
        }

        public string SpawnPointId
        {
            get;
        }

        public string RouteId
        {
            get;
        }

        public EnemyPatternEntry(
            int sourceLineNumber,
            string eventId,
            float spawnTime,
            string enemyId,
            string spawnPointId,
            string routeId
        )
        {
            SourceLineNumber =
                sourceLineNumber;

            EventId =
                eventId;

            SpawnTime =
                spawnTime;

            EnemyId =
                enemyId;

            SpawnPointId =
                spawnPointId;

            RouteId =
                routeId;
        }
    }
}