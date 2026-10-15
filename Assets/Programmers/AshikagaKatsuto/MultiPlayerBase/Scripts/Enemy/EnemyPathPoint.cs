using UnityEngine;

namespace Game.Enemies.Path
{
    [ExecuteAlways]
    public sealed class EnemyPathPoint :
        MonoBehaviour
    {
        [Header("Point Identity")]
        [SerializeField]
        private EnemyPathStage stage =
            EnemyPathStage.C;

        [Tooltip(
            "C、B、Aの横位置です。"
            + "Damageポイントでは通常使用しません。"
        )]
        [SerializeField]
        private EnemyFieldPosition fieldPosition =
            EnemyFieldPosition.Position0;

        [Tooltip(
            "Damageポイントが対象とする"
            + "フィールド範囲です。"
            + "StageがDamageの場合だけ使用します。"
        )]
        [SerializeField]
        private EnemyDamageArea damageArea =
            EnemyDamageArea.Position0;

        [Header("Debug Display")]
        [Tooltip(
            "このEPPのデバッグ表示を有効にします。"
        )]
        [SerializeField]
        private bool showDebugMarker =
            true;

        [Tooltip(
            "SceneビューへGizmoを表示します。"
        )]
        [SerializeField]
        private bool showSceneGizmo =
            true;

        [Tooltip(
            "Gameカメラへ実体のマーカーを表示します。"
        )]
        [SerializeField]
        private bool showInGameView;

        [Tooltip(
            "Play中もGameカメラへ"
            + "デバッグマーカーを表示します。"
        )]
        [SerializeField]
        private bool showWhilePlaying =
            true;

        [Tooltip(
            "Gameカメラへ表示する子オブジェクトです。"
            + "Rendererを持つSphereなどを設定します。"
            + "Colliderは削除してください。"
        )]
        [SerializeField]
        private GameObject debugMarkerVisual;

        [Header("Gizmo Appearance")]
        [Tooltip(
            "Stageごとの自動色分けを使用します。"
        )]
        [SerializeField]
        private bool useAutomaticColor =
            true;

        [SerializeField]
        private Color customColor =
            Color.white;

        [SerializeField]
        [Min(0.01f)]
        private float pointRadius =
            0.15f;

        [SerializeField]
        [Min(0.01f)]
        private float crossSize =
            0.25f;

        [SerializeField]
        private bool drawSolidSphere;

        public EnemyPathStage Stage =>
            stage;

        public EnemyFieldPosition FieldPosition =>
            fieldPosition;

        public int PositionIndex =>
            (int)fieldPosition;

        public EnemyDamageArea DamageArea =>
            damageArea;

        public Vector3 Position =>
            transform.position;

        public bool IsDamagePoint =>
            stage == EnemyPathStage.Damage;

        public string DisplayName
        {
            get
            {
                if (IsDamagePoint)
                {
                    return $"Damage-{damageArea}";
                }

                return $"{stage}-{PositionIndex}";
            }
        }

        private void OnEnable()
        {
            RefreshMarkerVisibility();
        }

        private void Update()
        {
            RefreshMarkerVisibility();
        }

        public void SetDebugVisible(
            bool visible
        )
        {
            showDebugMarker =
                visible;

            RefreshMarkerVisibility();
        }

        public void SetGameViewVisible(
            bool visible
        )
        {
            showInGameView =
                visible;

            RefreshMarkerVisibility();
        }

        public void SetSceneGizmoVisible(
            bool visible
        )
        {
            showSceneGizmo =
                visible;
        }

        private void RefreshMarkerVisibility()
        {
            if (debugMarkerVisual == null)
            {
                return;
            }

            bool visible =
                showDebugMarker
                &&
                showInGameView
                &&
                (
                    !Application.isPlaying
                    ||
                    showWhilePlaying
                );

            if (
                debugMarkerVisual.activeSelf
                != visible
            )
            {
                debugMarkerVisual.SetActive(
                    visible
                );
            }
        }

        private void OnDrawGizmos()
        {
            if (
                !showDebugMarker
                ||
                !showSceneGizmo
            )
            {
                return;
            }

            DrawPointGizmo();
        }

        private void DrawPointGizmo()
        {
            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                GetDisplayColor();

            Vector3 position =
                transform.position;

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
                position
                - Vector3.right * crossSize,
                position
                + Vector3.right * crossSize
            );

            Gizmos.DrawLine(
                position
                - Vector3.up * crossSize,
                position
                + Vector3.up * crossSize
            );

            Gizmos.DrawLine(
                position
                - Vector3.forward * crossSize,
                position
                + Vector3.forward * crossSize
            );

            Gizmos.color =
                previousColor;
        }

        private Color GetDisplayColor()
        {
            if (!useAutomaticColor)
            {
                return customColor;
            }

            switch (stage)
            {
                case EnemyPathStage.C:
                    return new Color(
                        0.25f,
                        0.65f,
                        1.0f,
                        1.0f
                    );

                case EnemyPathStage.B:
                    return new Color(
                        0.25f,
                        1.0f,
                        0.45f,
                        1.0f
                    );

                case EnemyPathStage.A:
                    return new Color(
                        1.0f,
                        0.8f,
                        0.2f,
                        1.0f
                    );

                case EnemyPathStage.Damage:
                    if (
                        damageArea
                        == EnemyDamageArea.All
                    )
                    {
                        return new Color(
                            0.85f,
                            0.25f,
                            1.0f,
                            1.0f
                        );
                    }

                    if (
                        damageArea
                        == EnemyDamageArea.Left
                        ||
                        damageArea
                        == EnemyDamageArea.Right
                    )
                    {
                        return new Color(
                            1.0f,
                            0.45f,
                            0.1f,
                            1.0f
                        );
                    }

                    return new Color(
                        1.0f,
                        0.2f,
                        0.2f,
                        1.0f
                    );

                default:
                    return Color.white;
            }
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

            RefreshMarkerVisibility();
        }
    }
}