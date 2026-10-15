using UnityEngine;

namespace Game.Enemies.Path
{
    public enum EnemyPathStage
    {
        C,
        B,
        A,
        Damage
    }

    public enum EnemyFieldPosition
    {
        Position0 = 0,
        Position1 = 1,
        Position2 = 2,
        Position3 = 3
    }

    public enum EnemyDamageArea
    {
        Position0,
        Position1,
        Position2,
        Position3,
        Left,
        Right,
        All
    }

    public enum EnemyDamageTransitionMode
    {
        [InspectorName(
            "通常経路終了後にDamageへ移動"
        )]
        AfterRouteCompleted,

        [InspectorName(
            "制限時間経過後にDamageへ移動"
        )]
        AfterDeadline,

        [InspectorName(
            "経路終了または制限時間の早い方"
        )]
        RouteCompletedOrDeadline
    }
}