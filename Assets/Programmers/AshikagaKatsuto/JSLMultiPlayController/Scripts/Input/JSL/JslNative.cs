using System.Runtime.InteropServices;

namespace Game.Input.JSL
{
    internal static class JslNative
    {
        private const string LibraryName =
            "JoyShockLibrary";

        [StructLayout(LayoutKind.Sequential)]
        internal struct JoyShockState
        {
            public int Buttons;

            public float LeftTrigger;
            public float RightTrigger;

            public float LeftStickX;
            public float LeftStickY;

            public float RightStickX;
            public float RightStickY;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ImuState
        {
            public float AccelerationX;
            public float AccelerationY;
            public float AccelerationZ;

            public float GyroscopeX;
            public float GyroscopeY;
            public float GyroscopeZ;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MotionState
        {
            public float QuaternionW;
            public float QuaternionX;
            public float QuaternionY;
            public float QuaternionZ;

            public float AccelerationX;
            public float AccelerationY;
            public float AccelerationZ;

            public float GravityX;
            public float GravityY;
            public float GravityZ;
        }

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern int JslConnectDevices();

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern int
            JslGetConnectedDeviceHandles(
                [Out] int[] deviceHandleArray,
                int size
            );

        // [DllImport(
        //     LibraryName,
        //     CallingConvention = CallingConvention.Cdecl
        // )]
        // [return: MarshalAs(UnmanagedType.I1)]
        // internal static extern bool JslStillConnected(
        //     int deviceId
        // );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern int JslGetControllerType(
            int deviceId
        );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern int
            JslGetControllerSplitType(
                int deviceId
            );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern JoyShockState
            JslGetSimpleState(
                int deviceId
            );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern ImuState JslGetIMUState(
            int deviceId
        );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern MotionState
            JslGetMotionState(
                int deviceId
            );

        // [DllImport(
        //     LibraryName,
        //     CallingConvention = CallingConvention.Cdecl
        // )]
        // internal static extern void
        //     JslGetAndFlushAccumulatedGyro(
        //         int deviceId,
        //         out float gyroX,
        //         out float gyroY,
        //         out float gyroZ
        //     );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern float JslGetGyroX(
            int deviceId
        );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern float JslGetGyroY(
            int deviceId
        );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern float JslGetGyroZ(
            int deviceId
        );

        // [DllImport(
        //     LibraryName,
        //     CallingConvention = CallingConvention.Cdecl
        // )]
        // internal static extern float
        //     JslGetTimeSinceLastUpdate(
        //         int deviceId
        //     );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern void
            JslSetPlayerNumber(
                int deviceId,
                int playerNumber
            );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern void
            JslSetAutomaticCalibration(
                int deviceId,
                [MarshalAs(UnmanagedType.I1)]
                bool enabled
            );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern void JslSetRumble(
            int deviceId,
            int smallRumble,
            int bigRumble
        );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern void
            JslDisconnectAndDisposeAll();


        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern void JslSetHDRumble(
            int deviceId,
            float lowFrequency,
            float lowAmplitude,
            float highFrequency,
            float highAmplitude
        );

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl
        )]
        internal static extern void JslSetHDRumbleLR(
            int deviceId,
            float lowFrequencyLeft,
            float lowAmplitudeLeft,
            float highFrequencyLeft,
            float highAmplitudeLeft,
            float lowFrequencyRight,
            float lowAmplitudeRight,
            float highFrequencyRight,
            float highAmplitudeRight
        );
    }
}