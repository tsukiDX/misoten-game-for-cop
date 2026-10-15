using UnityEngine;

namespace Game.Enemies.Debugging
{
    public sealed class EnemyDebugPoint :
        MonoBehaviour
    {
        [Header("Target Point")]
        [Tooltip(
            "敵の処理で使用する位置です。"
            + "敵の子EmptyObjectを設定してください。"
            + "未設定の場合はこのGameObjectの位置を使います。"
        )]
        [SerializeField]
        private Transform targetPoint;

        [Header("Display")]
        [Tooltip(
            "選択していないときも"
            + "Sceneビューに目印を表示します。"
        )]
        [SerializeField]
        private bool alwaysVisible =
            true;

        [Tooltip(
            "敵本体から目印まで線を表示します。"
        )]
        [SerializeField]
        private bool drawConnectionLine =
            true;

        [Tooltip(
            "目印の球を塗りつぶして表示します。"
        )]
        [SerializeField]
        private bool drawSolidSphere;

        [SerializeField]
        private Color pointColor =
            new Color(
                1.0f,
                0.2f,
                0.2f,
                1.0f
            );

        [SerializeField]
        private Color lineColor =
            new Color(
                1.0f,
                0.8f,
                0.0f,
                1.0f
            );

        [SerializeField]
        [Min(0.01f)]
        private float pointRadius =
            0.15f;

        [SerializeField]
        [Min(0.01f)]
        private float crossSize =
            0.3f;

        public Transform TargetPoint =>
            targetPoint;

        public Vector3 TargetPosition =>
            targetPoint != null
                ? targetPoint.position
                : transform.position;

        private void OnDrawGizmos()
        {
            if (!alwaysVisible)
            {
                return;
            }

            DrawDebugPoint();
        }

        private void OnDrawGizmosSelected()
        {
            if (alwaysVisible)
            {
                return;
            }

            DrawDebugPoint();
        }

        private void DrawDebugPoint()
        {
            Vector3 position =
                TargetPosition;

            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                pointColor;

            if (drawSolidSphere)
            {
                Gizmos.DrawSphere(
                    position,
                    pointRadius
                );
            }
            else
            {
                Gizmos.DrawWireSphere(
                    position,
                    pointRadius
                );
            }

            Gizmos.DrawLine(
                position - Vector3.right * crossSize,
                position + Vector3.right * crossSize
            );

            Gizmos.DrawLine(
                position - Vector3.up * crossSize,
                position + Vector3.up * crossSize
            );

            Gizmos.DrawLine(
                position - Vector3.forward * crossSize,
                position + Vector3.forward * crossSize
            );

            if (
                drawConnectionLine
                &&
                targetPoint != null
            )
            {
                Gizmos.color =
                    lineColor;

                Gizmos.DrawLine(
                    transform.position,
                    position
                );
            }

            Gizmos.color =
                previousColor;
        }

        private void OnValidate()
        {
            pointRadius =
                Mathf.Max(
                    0.01f,
                    pointRadius
                );

            crossSize =
                Mathf.Max(
                    0.01f,
                    crossSize
                );
        }
    }
}