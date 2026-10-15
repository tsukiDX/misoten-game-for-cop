using System.Text;
using UnityEngine;

namespace Game.Input.JSL
{
    public sealed class JslControllerDebugView :
        MonoBehaviour
    {
        [SerializeField]
        private JslControllerManager controllerManager;

        [SerializeField]
        private bool showDebugView = true;

        [SerializeField]
        private int fontSize = 18;

        [SerializeField]
        private Vector2 position =
            new Vector2(
                20.0f,
                20.0f
            );

        [SerializeField]
        private float width = 900.0f;

        [SerializeField]
        private float height = 900.0f;

        private readonly StringBuilder builder =
            new StringBuilder();

        private GUIStyle labelStyle;

        private void Awake()
        {
            if (controllerManager == null)
            {
                controllerManager =
                    FindFirstObjectByType<
                        JslControllerManager
                    >();
            }
        }

        private void OnGUI()
        {
            if (
                !showDebugView
                ||
                controllerManager == null
            )
            {
                return;
            }

            InitializeStyle();

            builder.Clear();

            builder.AppendLine(
                "JSL Controller Debug"
            );

            builder.AppendLine(
                "Connected: "
                + controllerManager
                    .ConnectedControllerCount
                + "/4"
            );

            builder.AppendLine(
                "Joined: "
                + controllerManager
                    .JoinedPlayerCount
                + "/4"
            );

            builder.AppendLine(
                "Plus / Options: Join"
            );

            builder.AppendLine(
                "Minus / Share: Hold to Leave"
            );

            builder.AppendLine();

            for (
                int playerIndex = 0;
                playerIndex
                < JslControllerManager
                    .MaximumPlayerCount;
                playerIndex++
            )
            {
                AppendPlayerInformation(
                    playerIndex
                );
            }

            GUI.Label(
                new Rect(
                    position.x,
                    position.y,
                    width,
                    height
                ),
                builder.ToString(),
                labelStyle
            );
        }

        private void AppendPlayerInformation(
            int playerIndex
        )
        {
            LocalPlayerSlot playerSlot =
                controllerManager
                    .PlayerSlots[playerIndex];

            builder.AppendLine(
                "--- Player "
                + (playerIndex + 1)
                + " ---"
            );

            if (!playerSlot.IsJoined)
            {
                builder.AppendLine(
                    "Status: Not Joined"
                );

                builder.AppendLine();

                return;
            }

            if (
                playerSlot
                    .IsWaitingForReconnect
            )
            {
                builder.AppendLine(
                    "Status: Waiting for Reconnect"
                );

                builder.AppendLine();

                return;
            }

            if (
                !controllerManager
                    .TryGetPlayerController(
                        playerIndex,
                        out JslControllerDevice
                            controller
                    )
            )
            {
                builder.AppendLine(
                    "Status: Controller Not Found"
                );

                builder.AppendLine();

                return;
            }

            JslInputSnapshot input =
                controller.CurrentInput;

            builder.AppendLine(
                "Status: Connected"
            );

            builder.AppendLine(
                "Handle: "
                + controller.DeviceHandle
            );

            builder.AppendLine(
                "Type: "
                + controller.ControllerType
            );

            builder.AppendLine(
                "Split: "
                + controller.SplitType
            );

            builder.AppendLine(
                $"Buttons: 0x{input.Buttons:X6}"
            );

            builder.AppendLine(
                "Left Stick: "
                + FormatVector2(
                    input.LeftStick
                )
            );

            builder.AppendLine(
                "Right Stick: "
                + FormatVector2(
                    input.RightStick
                )
            );

            builder.AppendLine(
                "Triggers: "
                + $"L={input.LeftTrigger:F2} "
                + $"R={input.RightTrigger:F2}"
            );

            builder.AppendLine(
                "Gyro X: "
                + input.Gyroscope.x.ToString(
                    "F3"
                )
            );

            builder.AppendLine(
                "Gyro Y: "
                + input.Gyroscope.y.ToString(
                    "F3"
                )
            );

            builder.AppendLine(
                "Gyro Z: "
                + input.Gyroscope.z.ToString(
                    "F3"
                )
            );

            builder.AppendLine(
                "Gyro Vector: "
                + FormatVector3(
                    input.Gyroscope
                )
            );

            builder.AppendLine(
                "Acceleration: "
                + FormatVector3(
                    input.Acceleration
                )
            );

            builder.AppendLine(
                "Gravity: "
                + FormatVector3(
                    input.Gravity
                )
            );

            builder.AppendLine(
                "Orientation: "
                + FormatQuaternion(
                    input.Orientation
                )
            );

            builder.AppendLine(
                "Last Update: "
                + input.TimeSinceLastUpdate
                    .ToString("F4")
                + "s"
            );

            builder.AppendLine();
        }

        private void InitializeStyle()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            labelStyle.fontSize =
                fontSize;

            labelStyle.normal.textColor =
                Color.white;
        }

        private static string FormatVector2(
            Vector2 value
        )
        {
            return
                $"({value.x:F3}, "
                + $"{value.y:F3})";
        }

        private static string FormatVector3(
            Vector3 value
        )
        {
            return
                $"({value.x:F3}, "
                + $"{value.y:F3}, "
                + $"{value.z:F3})";
        }

        private static string FormatQuaternion(
            Quaternion value
        )
        {
            return
                $"({value.x:F3}, "
                + $"{value.y:F3}, "
                + $"{value.z:F3}, "
                + $"{value.w:F3})";
        }
    }
}