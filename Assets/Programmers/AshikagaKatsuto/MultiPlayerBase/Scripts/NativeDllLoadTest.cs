using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Game.Input.JSL
{
    public sealed class NativeDllLoadTest :
        MonoBehaviour
    {
        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true
        )]
        private static extern IntPtr LoadLibraryW(
            string libraryPath
        );

        [DllImport(
            "kernel32.dll",
            SetLastError = true
        )]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(
            IntPtr moduleHandle
        );

        [DllImport(
    "kernel32.dll",
    CharSet = CharSet.Ansi,
    SetLastError = true
)]
private static extern IntPtr GetProcAddress(
    IntPtr moduleHandle,
    string procedureName
);

        [ContextMenu("Debug/Load JoyShockLibrary")]
        private void LoadJoyShockLibrary()
        {
            string fullPath =
                System.IO.Path.GetFullPath(
                    "Assets/Plugins/x86_64/"
                    + "JoyShockLibrary.dll"
                );

            Debug.Log(
                $"[JSL DLL Test] Path={fullPath}",
                this
            );

            IntPtr moduleHandle =
                LoadLibraryW(
                    fullPath
                );

            if (moduleHandle == IntPtr.Zero)
            {
                int errorCode =
                    Marshal.GetLastWin32Error();

                string errorMessage =
                    new Win32Exception(
                        errorCode
                    ).Message;

                Debug.LogError(
                    $"[JSL DLL Test] "
                    + $"Load failed. "
                    + $"Win32Error={errorCode}, "
                    + $"Message={errorMessage}",
                    this
                );

                return;
            }

            Debug.Log(
                "[JSL DLL Test] "
                + "JoyShockLibrary.dllの"
                + "ロードに成功しました。",
                this
            );

            FreeLibrary(
                moduleHandle
            );

            IntPtr connectAddress =
    GetProcAddress(
        moduleHandle,
        "JslConnectDevices"
    );

IntPtr rumbleAddress =
    GetProcAddress(
        moduleHandle,
        "JslSetRumble"
    );

IntPtr hdRumbleAddress =
    GetProcAddress(
        moduleHandle,
        "JslSetHDRumble"
    );

IntPtr hdRumbleLrAddress =
    GetProcAddress(
        moduleHandle,
        "JslSetHDRumbleLR"
    );

Debug.Log(
    $"[JSL DLL Test] "
    + $"JslConnectDevices="
    + $"{connectAddress != IntPtr.Zero}, "
    + $"JslSetRumble="
    + $"{rumbleAddress != IntPtr.Zero}, "
    + $"JslSetHDRumble="
    + $"{hdRumbleAddress != IntPtr.Zero}, "
    + $"JslSetHDRumbleLR="
    + $"{hdRumbleLrAddress != IntPtr.Zero}",
    this
);


        }
    }
}