using UnityEngine;

namespace Game.Enemies.Spawning
{
    public sealed class EnemySpawnPoint :
        MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip(
            "CSVのSpawnPointIdから参照するIDです。"
            + "例: Spawn_0"
        )]
        [SerializeField]
        private string spawnPointId =
            "Spawn_0";

        [Header("Debug")]
        [SerializeField]
        private bool showDebugMarker =
            true;

        [SerializeField]
        private Color markerColor =
            new Color(
                0.2f,
                1.0f,
                1.0f,
                1.0f
            );

        [SerializeField]
        [Min(0.01f)]
        private float markerRadius =
            0.25f;

        [SerializeField]
        [Min(0.01f)]
        private float directionLength =
            1.0f;

        public string SpawnPointId =>
            spawnPointId;

        public Vector3 Position =>
            transform.position;

        public Quaternion Rotation =>
            transform.rotation;

        private void OnDrawGizmos()
        {
            if (!showDebugMarker)
            {
                return;
            }

            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                markerColor;

            Gizmos.DrawWireSphere(
                transform.position,
                markerRadius
            );

            Gizmos.DrawLine(
                transform.position,
                transform.position
                + transform.forward
                * directionLength
            );

            Gizmos.color =
                previousColor;
        }

        private void OnValidate()
        {
            spawnPointId =
                spawnPointId != null
                    ? spawnPointId.Trim()
                    : string.Empty;

            markerRadius =
                Mathf.Max(
                    0.01f,
                    markerRadius
                );

            directionLength =
                Mathf.Max(
                    0.01f,
                    directionLength
                );
        }
    }
}