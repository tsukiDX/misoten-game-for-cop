using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Game.Input.JSL
{
    [DefaultExecutionOrder(-2000)]
    public sealed class JslNativeLibraryBootstrap :
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
        private static extern IntPtr GetProcAddress(
            IntPtr moduleHandle,
            string procedureName
        );

        private static IntPtr moduleHandle =
            IntPtr.Zero;

        private static bool loadAttempted;

        public static bool IsLoaded =>
            moduleHandle != IntPtr.Zero;

        public static void EnsureLoaded()
        {
            if (IsLoaded)
            {
                return;
            }

            if (loadAttempted)
            {
                return;
            }

            loadAttempted = true;

#if UNITY_EDITOR_WIN
            string libraryPath =
                Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        "Plugins",
                        "x86_64",
                        "JoyShockLibrary.dll"
                    )
                );
#elif UNITY_STANDALONE_WIN
            string libraryPath =
                Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        "Plugins",
                        "x86_64",
                        "JoyShockLibrary.dll"
                    )
                );
#else
            Debug.LogWarning(
                "[JSL] 現在のプラットフォームでは"
                + "JoyShockLibrary.dllを"
                + "事前ロードしません。"
            );

            return;
#endif

            if (!File.Exists(libraryPath))
            {
                Debug.LogError(
                    "[JSL] JoyShockLibrary.dllが"
                    + "見つかりません。\n"
                    + $"Path={libraryPath}"
                );

                return;
            }

            moduleHandle =
                LoadLibraryW(
                    libraryPath
                );

            if (moduleHandle == IntPtr.Zero)
            {
                int errorCode =
                    Marshal.GetLastWin32Error();

                Debug.LogError(
                    "[JSL] JoyShockLibrary.dllの"
                    + "事前ロードに失敗しました。\n"
                    + $"Path={libraryPath}\n"
                    + $"Win32Error={errorCode}"
                );

                return;
            }

            bool hasConnect =
                HasExport(
                    "JslConnectDevices"
                );

            bool hasRumble =
                HasExport(
                    "JslSetRumble"
                );

            bool hasHdRumble =
                HasExport(
                    "JslSetHDRumble"
                );

            Debug.Log(
                "[JSL] JoyShockLibrary.dllの"
                + "事前ロードに成功しました。\n"
                + $"Path={libraryPath}\n"
                + $"JslConnectDevices={hasConnect}, "
                + $"JslSetRumble={hasRumble}, "
                + $"JslSetHDRumble={hasHdRumble}"
            );
        }

        private static bool HasExport(
            string functionName
        )
        {
            if (moduleHandle == IntPtr.Zero)
            {
                return false;
            }

            return GetProcAddress(
                moduleHandle,
                functionName
            ) != IntPtr.Zero;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType
                .BeforeSceneLoad
        )]
        private static void LoadBeforeScene()
        {
            EnsureLoaded();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void LoadInEditor()
        {
            UnityEditor.EditorApplication.delayCall +=
                EnsureLoaded;
        }
#endif
    }
}