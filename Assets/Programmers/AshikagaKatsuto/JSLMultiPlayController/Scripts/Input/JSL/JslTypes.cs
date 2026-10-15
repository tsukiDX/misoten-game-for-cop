using System;
using UnityEngine;

namespace Game.Input.JSL
{
    public enum JslControllerType
    {
        Unknown = 0,
        JoyConLeft = 1,
        JoyConRight = 2,
        SwitchPro = 3,
        DualShock4 = 4,
        DualSense = 5
    }

    public enum JslControllerSplitType
    {
        Unknown = 0,
        Left = 1,
        Right = 2,
        Full = 3
    }

    [Flags]
    public enum JslButton
    {
        None = 0,

        DpadUp = 0x000001,
        DpadDown = 0x000002,
        DpadLeft = 0x000004,
        DpadRight = 0x000008,

        Plus = 0x000010,
        Options = Plus,

        Minus = 0x000020,
        Share = Minus,

        LeftStickClick = 0x000040,
        RightStickClick = 0x000080,

        LeftShoulder = 0x000100,
        RightShoulder = 0x000200,

        LeftTriggerButton = 0x000400,
        RightTriggerButton = 0x000800,

        South = 0x001000,
        East = 0x002000,
        West = 0x004000,
        North = 0x008000,

        Home = 0x010000,
        Capture = 0x020000,
        Microphone = 0x040000,

        SideLeft = 0x080000,
        SideRight = 0x100000,

        FunctionLeft = 0x200000,
        FunctionRight = 0x400000
    }

    [Serializable]
    public struct JslInputSnapshot
    {
        public int Buttons;

        public Vector2 LeftStick;
        public Vector2 RightStick;

        public float LeftTrigger;
        public float RightTrigger;

        public Vector3 Gyroscope;
        public Vector3 Acceleration;
        public Vector3 Gravity;

        public Quaternion Orientation;

        public float TimeSinceLastUpdate;

        public bool IsPressed(
            JslButton button
        )
        {
            int mask =
                (int)button;

            return
                (Buttons & mask)
                == mask;
        }
    }

    [Serializable]
    public sealed class LocalPlayerSlot
    {
        [SerializeField]
        private int playerIndex;

        [SerializeField]
        private bool isJoined;

        [SerializeField]
        private int deviceHandle = -1;

        [SerializeField]
        private bool isWaitingForReconnect;

        public int PlayerIndex =>
            playerIndex;

        public int PlayerNumber =>
            playerIndex + 1;

        public bool IsJoined =>
            isJoined;

        public int DeviceHandle =>
            deviceHandle;

        public bool IsWaitingForReconnect =>
            isWaitingForReconnect;

        public bool HasController =>
            isJoined
            &&
            deviceHandle >= 0;

        public LocalPlayerSlot(
            int index
        )
        {
            playerIndex =
                index;

            isJoined =
                false;

            deviceHandle =
                -1;

            isWaitingForReconnect =
                false;
        }

        internal void Join(
            int handle
        )
        {
            isJoined =
                true;

            deviceHandle =
                handle;

            isWaitingForReconnect =
                false;
        }

        internal void Reconnect(
            int handle
        )
        {
            if (!isJoined)
            {
                return;
            }

            deviceHandle =
                handle;

            isWaitingForReconnect =
                false;
        }

        internal void BeginReconnectWait()
        {
            deviceHandle =
                -1;

            isWaitingForReconnect =
                true;
        }

        internal void Leave()
        {
            isJoined =
                false;

            deviceHandle =
                -1;

            isWaitingForReconnect =
                false;
        }
    }
}