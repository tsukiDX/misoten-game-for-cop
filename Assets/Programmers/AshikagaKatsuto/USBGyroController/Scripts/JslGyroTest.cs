using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class JslGyroTest : MonoBehaviour
{
    private const string DllName = "JoyShockLibrary";

    // JSLで定義されているSwitch Proコントローラーの種類
    private const int SwitchProControllerType = 3;

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int JslConnectDevices();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int JslGetConnectedDeviceHandles(
        [Out] int[] deviceHandleArray,
        int size
    );

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int JslGetControllerType(int deviceId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool JslStillConnected(int deviceId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern float JslGetGyroX(int deviceId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern float JslGetGyroY(int deviceId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern float JslGetGyroZ(int deviceId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void JslDisconnectAndDisposeAll();

    [Header("ジャイロで回転させる対象")]
    [SerializeField]
    private Transform target;

    [Header("ジャイロ設定")]
    [SerializeField]
    private float sensitivity = 1.0f;

    [SerializeField]
    private float deadZone = 0.5f;

    [Header("再接続設定")]
    [SerializeField]
    private float reconnectInterval = 1.0f;

    private int deviceHandle = -1;

    private Vector3 gyro;

    private string status = "初期化前";

    private float reconnectTimer;

    private bool libraryStarted;

    private void Start()
    {
        if (target == null)
        {
            target = transform;
        }

        TryConnectController();
    }

    private void Update()
    {
        if (!IsControllerConnected())
        {
            UpdateReconnection();
            return;
        }

        ReadGyro();
        RotateTarget();
    }

    private bool IsControllerConnected()
    {
        if (deviceHandle < 0)
        {
            return false;
        }

        try
        {
            if (JslStillConnected(deviceHandle))
            {
                return true;
            }

            HandleDisconnection();
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "JSLの接続確認中にエラーが発生しました。\n" +
                exception
            );

            HandleDisconnection();
            return false;
        }
    }

    private void UpdateReconnection()
    {
        reconnectTimer -= Time.unscaledDeltaTime;

        if (reconnectTimer > 0.0f)
        {
            return;
        }

        reconnectTimer = reconnectInterval;
        TryConnectController();
    }

    private void TryConnectController()
    {
        try
        {
            status = "コントローラーを検索中";

            int deviceCount = JslConnectDevices();
            libraryStarted = true;

            if (deviceCount <= 0)
            {
                deviceHandle = -1;
                gyro = Vector3.zero;
                status = "未接続：USB接続を待っています";
                return;
            }

            int[] handles = new int[deviceCount];

            int handleCount = JslGetConnectedDeviceHandles(
                handles,
                handles.Length
            );

            for (int i = 0; i < handleCount; i++)
            {
                int currentHandle = handles[i];

                if (!JslStillConnected(currentHandle))
                {
                    continue;
                }

                int controllerType =
                    JslGetControllerType(currentHandle);

                if (controllerType != SwitchProControllerType)
                {
                    continue;
                }

                deviceHandle = currentHandle;
                gyro = Vector3.zero;

                status =
                    $"Switch Pro接続成功 Handle={deviceHandle}";

                Debug.Log($"JSL: {status}");
                return;
            }

            deviceHandle = -1;
            gyro = Vector3.zero;

            status =
                "デバイスはありますがSwitch Proが見つかりません";
        }
        catch (DllNotFoundException exception)
        {
            enabled = false;
            status = "JoyShockLibrary.dllを読み込めません";

            Debug.LogError(
                status + "\n" +
                "DLLの配置場所とx64設定を確認してください。\n" +
                exception
            );
        }
        catch (EntryPointNotFoundException exception)
        {
            enabled = false;

            status =
                "JslStillConnectedが見つかりません。JSL 3.0が必要です";

            Debug.LogError(
                status + "\n" +
                exception
            );
        }
        catch (Exception exception)
        {
            deviceHandle = -1;
            gyro = Vector3.zero;
            status = "JSLの接続処理に失敗しました";

            Debug.LogError(
                status + "\n" +
                exception
            );
        }
    }

    private void HandleDisconnection()
    {
        Debug.LogWarning(
            "JSL: Switch Proコントローラーが切断されました。"
        );

        deviceHandle = -1;
        gyro = Vector3.zero;
        reconnectTimer = 0.0f;

        status = "切断：再接続を待っています";
    }

    private void ReadGyro()
    {
        try
        {
            gyro = new Vector3(
                JslGetGyroX(deviceHandle),
                JslGetGyroY(deviceHandle),
                JslGetGyroZ(deviceHandle)
            );

            gyro.x = ApplyDeadZone(gyro.x);
            gyro.y = ApplyDeadZone(gyro.y);
            gyro.z = ApplyDeadZone(gyro.z);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "ジャイロ取得中にエラーが発生しました。\n" +
                exception
            );

            HandleDisconnection();
        }
    }

    private void RotateTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 rotationAmount =
            gyro *
            sensitivity *
            Time.deltaTime;

        target.Rotate(
            rotationAmount,
            Space.Self
        );
    }

    private float ApplyDeadZone(float value)
    {
        if (Mathf.Abs(value) < deadZone)
        {
            return 0.0f;
        }

        return value;
    }

    private void OnGUI()
    {
        GUI.Box(
            new Rect(10.0f, 10.0f, 540.0f, 155.0f),
            "JSLジャイロ・再接続テスト"
        );

        GUI.Label(
            new Rect(25.0f, 40.0f, 510.0f, 25.0f),
            $"状態: {status}"
        );

        GUI.Label(
            new Rect(25.0f, 65.0f, 510.0f, 25.0f),
            $"Device Handle: {deviceHandle}"
        );

        GUI.Label(
            new Rect(25.0f, 90.0f, 510.0f, 25.0f),
            $"Gyro X: {gyro.x:F2}"
        );

        GUI.Label(
            new Rect(25.0f, 110.0f, 510.0f, 25.0f),
            $"Gyro Y: {gyro.y:F2}"
        );

        GUI.Label(
            new Rect(25.0f, 130.0f, 510.0f, 25.0f),
            $"Gyro Z: {gyro.z:F2}"
        );
    }

    private void OnApplicationQuit()
    {
        DisposeLibrary();
    }

    private void OnDestroy()
    {
        DisposeLibrary();
    }

    private void DisposeLibrary()
    {
        if (!libraryStarted)
        {
            return;
        }

        try
        {
            JslDisconnectAndDisposeAll();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "JSLの終了処理中にエラーが発生しました。\n" +
                exception
            );
        }

        libraryStarted = false;
        deviceHandle = -1;
        gyro = Vector3.zero;
    }
}