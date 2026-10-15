using UnityEngine;

namespace Game.Input.JSL
{
    public sealed class LocalPlayerController :
        MonoBehaviour
    {
        [Header("プレイヤー")]
        [SerializeField]
        [Range(0, 3)]
        private int playerIndex;

        [Header("プレイヤー要素")]
        [SerializeField]
        private GameObject playerCharacter;

        [SerializeField]
        private GameObject gyroPointer;

        [SerializeField]
        private GameObject playerUi;

        [Header("Runtime State")]
        [SerializeField]
        private bool isActionDisabled;

        private JslControllerDevice controller;

        private JslInputSnapshot currentInput;

        public int PlayerIndex =>
            playerIndex;

        public int PlayerNumber =>
            playerIndex + 1;

        public bool IsJoined
        {
            get;
            private set;
        }

        public bool HasController =>
            controller != null
            &&
            controller.IsConnected;

        public bool IsActionDisabled =>
            isActionDisabled;

        public bool IsInputAvailable =>
            IsJoined
            &&
            HasController
            &&
            !isActionDisabled;

        public JslControllerDevice Controller =>
            controller;

        public JslInputSnapshot Input =>
            currentInput;

        public Vector2 LeftStick =>
            currentInput.LeftStick;

        public Vector2 RightStick =>
            currentInput.RightStick;

        public Vector3 Gyroscope =>
            currentInput.Gyroscope;

        public Vector3 Acceleration =>
            currentInput.Acceleration;

        public Vector3 Gravity =>
            currentInput.Gravity;

        private void Awake()
        {
            isActionDisabled =
                false;

            SetPlayerElementsActive(
                false
            );
        }

        private void Update()
        {
            if (!IsInputAvailable)
            {
                currentInput =
                    default;

                return;
            }

            currentInput =
                controller.CurrentInput;
        }

        public void BindController(
            JslControllerDevice newController
        )
        {
            if (newController == null)
            {
                return;
            }

            controller =
                newController;

            IsJoined =
                true;

            isActionDisabled =
                false;

            currentInput =
                default;

            SetPlayerElementsActive(
                true
            );
        }

        public void UnbindController(
            bool keepPlayerJoined
        )
        {
            controller =
                null;

            currentInput =
                default;

            if (!keepPlayerJoined)
            {
                IsJoined =
                    false;

                isActionDisabled =
                    false;

                SetPlayerElementsActive(
                    false
                );
            }
        }

        public void LeavePlayer()
        {
            controller =
                null;

            currentInput =
                default;

            IsJoined =
                false;

            isActionDisabled =
                false;

            SetPlayerElementsActive(
                false
            );
        }

        public void SetActionDisabled(
            bool disabled
        )
        {
            if (
                isActionDisabled
                == disabled
            )
            {
                return;
            }

            isActionDisabled =
                disabled;

            if (isActionDisabled)
            {
                currentInput =
                    default;
            }
        }

        public bool IsButtonPressed(
            JslButton button
        )
        {
            if (!IsInputAvailable)
            {
                return false;
            }

            return controller.IsButtonPressed(
                button
            );
        }

        public bool WasButtonPressedThisFrame(
            JslButton button
        )
        {
            if (!IsInputAvailable)
            {
                return false;
            }

            return controller
                .WasButtonPressedThisFrame(
                    button
                );
        }

        public bool WasButtonReleasedThisFrame(
            JslButton button
        )
        {
            if (!IsInputAvailable)
            {
                return false;
            }

            return controller
                .WasButtonReleasedThisFrame(
                    button
                );
        }

        private void SetPlayerElementsActive(
            bool active
        )
        {
            if (playerCharacter != null)
            {
                playerCharacter.SetActive(
                    active
                );
            }

            if (gyroPointer != null)
            {
                gyroPointer.SetActive(
                    active
                );
            }

            if (playerUi != null)
            {
                playerUi.SetActive(
                    active
                );
            }
        }

#if UNITY_EDITOR
        [ContextMenu(
            "Debug/Start Maximum Rumble"
        )]
        private void DebugStartMaximumRumble()
        {
            if (
                controller == null
                ||
                !controller.IsConnected
            )
            {
                Debug.LogWarning(
                    $"Player {PlayerNumber}: "
                    + "振動テスト可能なコントローラーが"
                    + "割り当てられていません。",
                    this
                );

                return;
            }

            Debug.Log(
                $"Player {PlayerNumber}: "
                + $"Rumble Start, "
                + $"Handle="
                + $"{controller.DeviceHandle}, "
                + $"Type="
                + $"{controller.ControllerType}",
                this
            );

            controller.SetRumble(
                1.0f,
                1.0f
            );
        }

        [ContextMenu(
            "Debug/Stop Rumble"
        )]
        private void DebugStopRumble()
        {
            if (controller == null)
            {
                return;
            }

            controller.StopRumble();

            Debug.Log(
                $"Player {PlayerNumber}: "
                + "Rumble Stop",
                this
            );
        }
#endif
    }
}