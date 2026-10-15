using System;
using UnityEngine;

namespace Game.Input.JSL
{
    public sealed class JslControllerDevice
    {
        public int DeviceHandle
        {
            get;
        }

        public JslControllerType ControllerType
        {
            get;
        }

        public JslControllerSplitType SplitType
        {
            get;
        }

        public bool IsConnected
        {
            get;
            private set;
        }

        public int AssignedPlayerIndex
        {
            get;
            private set;
        } = -1;

        public bool IsAssigned =>
            AssignedPlayerIndex >= 0;

        public JslInputSnapshot CurrentInput
        {
            get;
            private set;
        }

        public JslInputSnapshot PreviousInput
        {
            get;
            private set;
        }

        public JslControllerDevice(
            int deviceHandle,
            JslControllerType controllerType,
            JslControllerSplitType splitType
        )
        {
            DeviceHandle =
                deviceHandle;

            ControllerType =
                controllerType;

            SplitType =
                splitType;

            IsConnected =
                true;

            CurrentInput =
                default;

            PreviousInput =
                default;
        }

internal void UpdateInput()
{
    PreviousInput =
        CurrentInput;

    JslNative.JoyShockState simpleState =
        JslNative.JslGetSimpleState(
            DeviceHandle
        );

    JslNative.ImuState imuState =
        JslNative.JslGetIMUState(
            DeviceHandle
        );

    JslNative.MotionState motionState =
        JslNative.JslGetMotionState(
            DeviceHandle
        );

    // JoyShockLibrary-plus-HDRumble版には
    // JslGetTimeSinceLastUpdateが存在しないため、
    // Unity側のフレーム間隔を使用する。
    float timeSinceLastUpdate =
        Time.unscaledDeltaTime;

    CurrentInput =
        new JslInputSnapshot
        {
            Buttons =
                simpleState.Buttons,

            LeftStick =
                new Vector2(
                    simpleState.LeftStickX,
                    simpleState.LeftStickY
                ),

            RightStick =
                new Vector2(
                    simpleState.RightStickX,
                    simpleState.RightStickY
                ),

            LeftTrigger =
                simpleState.LeftTrigger,

            RightTrigger =
                simpleState.RightTrigger,

            Gyroscope =
                new Vector3(
                    imuState.GyroscopeX,
                    imuState.GyroscopeY,
                    imuState.GyroscopeZ
                ),

            Acceleration =
                new Vector3(
                    motionState.AccelerationX,
                    motionState.AccelerationY,
                    motionState.AccelerationZ
                ),

            Gravity =
                new Vector3(
                    motionState.GravityX,
                    motionState.GravityY,
                    motionState.GravityZ
                ),

            Orientation =
                new Quaternion(
                    motionState.QuaternionX,
                    motionState.QuaternionY,
                    motionState.QuaternionZ,
                    motionState.QuaternionW
                ),

            TimeSinceLastUpdate =
                timeSinceLastUpdate
        };
}

        public bool IsButtonPressed(
            JslButton button
        )
        {
            return CurrentInput.IsPressed(
                button
            );
        }

        public bool WasButtonPressed(
            JslButton button
        )
        {
            return PreviousInput.IsPressed(
                button
            );
        }

        public bool WasButtonPressedThisFrame(
            JslButton button
        )
        {
            return
                IsButtonPressed(button)
                &&
                !WasButtonPressed(button);
        }

        public bool WasButtonReleasedThisFrame(
            JslButton button
        )
        {
            return
                !IsButtonPressed(button)
                &&
                WasButtonPressed(button);
        }

        internal void AssignPlayer(
            int playerIndex
        )
        {
            AssignedPlayerIndex =
                playerIndex;
        }

        internal void RemovePlayerAssignment()
        {
            AssignedPlayerIndex =
                -1;
        }

internal void MarkDisconnected()
{
    if (IsConnected)
    {
        try
        {
            StopRumble();
        }
        catch
        {
            // 切断中または終了中なので、
            // 振動停止エラーは無視する。
        }
    }

    IsConnected =
        false;

    PreviousInput =
        default;

    CurrentInput =
        default;
}
        public void SetRumble(
            float smallMotor,
            float largeMotor
        )
        {
            if (!IsConnected)
            {
                return;
            }

            float highAmplitude =
                Mathf.Clamp01(
                    smallMotor
                );

            float lowAmplitude =
                Mathf.Clamp01(
                    largeMotor
                );

            if (IsNintendoController())
            {
                SetNintendoHdRumble(
                    lowAmplitude,
                    highAmplitude
                );

                return;
            }

            SetSonyRumble(
                highAmplitude,
                lowAmplitude
            );
        }

        public void StopRumble()
        {
            if (!IsConnected)
            {
                return;
            }

            if (IsNintendoController())
            {
                StopNintendoHdRumble();
                return;
            }

            JslNative.JslSetRumble(
                DeviceHandle,
                0,
                0
            );
        }

        private void SetSonyRumble(
            float highAmplitude,
            float lowAmplitude
        )
        {
            int smallValue =
                ConvertRumbleValue(
                    highAmplitude
                );

            int largeValue =
                ConvertRumbleValue(
                    lowAmplitude
                );

            JslNative.JslSetRumble(
                DeviceHandle,
                smallValue,
                largeValue
            );
        }

        private void SetNintendoHdRumble(
            float lowAmplitude,
            float highAmplitude
        )
        {
            const float LowFrequency =
                160.0f;

            const float HighFrequency =
                320.0f;

            JslNative.JslSetHDRumble(
                DeviceHandle,
                LowFrequency,
                lowAmplitude,
                HighFrequency,
                highAmplitude
            );
        }

        private void StopNintendoHdRumble()
        {
            const float LowFrequency =
                160.0f;

            const float HighFrequency =
                320.0f;

            JslNative.JslSetHDRumble(
                DeviceHandle,
                LowFrequency,
                0.0f,
                HighFrequency,
                0.0f
            );
        }

        private bool IsNintendoController()
        {
            int controllerTypeValue =
                (int)ControllerType;

            return
                controllerTypeValue >= 1
                &&
                controllerTypeValue <= 3;
        }

        private static int ConvertRumbleValue(
            float value
        )
        {
            float clampedValue =
                Mathf.Clamp01(
                    value
                );

            return Mathf.RoundToInt(
                clampedValue * 255.0f
            );
        }
    }
}