using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class SwitchProGyroController : MonoBehaviour
{
    private static class JoyShockNative
    {
        private const string DllName = "JoyShockLibrary";

        public const int ControllerTypeJoyConLeft = 1;
        public const int ControllerTypeJoyConRight = 2;
        public const int ControllerTypeProController = 3;
        public const int ControllerTypeDualShock4 = 4;
        public const int ControllerTypeDualSense = 5;

        public const int GyroSpaceLocal = 0;
        public const int GyroSpaceWorld = 1;
        public const int GyroSpacePlayer = 2;

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JslConnectDevices();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JslGetConnectedDeviceHandles(
            [Out] int[] deviceHandleArray,
            int size
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslDisconnectAndDisposeAll();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool JslStillConnected(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JslGetControllerType(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float JslGetGyroX(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float JslGetGyroY(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float JslGetGyroZ(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslGetAndFlushAccumulatedGyro(
            int deviceId,
            out float gyroX,
            out float gyroY,
            out float gyroZ
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslSetGyroSpace(
            int deviceId,
            int gyroSpace
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslResetContinuousCalibration(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslStartContinuousCalibration(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslPauseContinuousCalibration(int deviceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JslSetAutomaticCalibration(
            int deviceId,
            [MarshalAs(UnmanagedType.I1)] bool enabled
        );
    }

    [Header("回転させる対象")]
    [SerializeField]
    private Transform rotationTarget;

    [Header("ジャイロ設定")]
    [SerializeField]
    private float sensitivity = 1.0f;

    [SerializeField]
    private Vector3 axisMultiplier = new Vector3(-1.0f, -1.0f, 1.0f);

    [SerializeField]
    private float deadZone = 0.5f;

    [SerializeField]
    private bool usePlayerSpace = true;

    [Header("キャリブレーション")]
    [SerializeField]
    private float calibrationSeconds = 2.0f;

    [SerializeField]
    private bool calibrateOnStart = true;

    [Header("再接続")]
    [SerializeField]
    private float reconnectInterval = 1.0f;

    [Header("デバッグ表示")]
    [SerializeField]
    private bool showDebugGUI = true;

    public bool IsConnected { get; private set; }

    public bool IsCalibrating { get; private set; }

    public Vector3 GyroDegreesPerSecond { get; private set; }

    private int deviceHandle = -1;
    private float reconnectTimer;
    private Coroutine calibrationCoroutine;
    private bool libraryInitialized;
    private bool applicationQuitting;

    private void Awake()
    {
        if (rotationTarget == null)
        {
            rotationTarget = transform;
        }
    }

    private void Start()
    {
        TryConnectController();
    }

    private void Update()
    {
        if (!IsConnected)
        {
            UpdateReconnect();
            return;
        }

        if (!IsDeviceStillConnected())
        {
            HandleDisconnection();
            return;
        }

        ReadGyro();

        if (!IsCalibrating)
        {
            ApplyGyroRotation();
        }

        // Cキーで手動キャリブレーション
        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCalibration();
        }
    }

    private void UpdateReconnect()
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
            int connectedDeviceCount = JoyShockNative.JslConnectDevices();
            libraryInitialized = true;

            if (connectedDeviceCount <= 0)
            {
                IsConnected = false;
                deviceHandle = -1;

                Debug.LogWarning(
                    "JSL: コントローラーが検出されませんでした。"
                    + " ProコントローラーがUSB接続されているか確認してください。"
                );

                return;
            }

            int[] deviceHandles = new int[connectedDeviceCount];

            int acquiredHandleCount =
                JoyShockNative.JslGetConnectedDeviceHandles(
                    deviceHandles,
                    deviceHandles.Length
                );

            deviceHandle = -1;

            for (int i = 0; i < acquiredHandleCount; i++)
            {
                int currentHandle = deviceHandles[i];
                int controllerType =
                    JoyShockNative.JslGetControllerType(currentHandle);

                if (controllerType ==
                    JoyShockNative.ControllerTypeProController)
                {
                    deviceHandle = currentHandle;
                    break;
                }
            }

            if (deviceHandle < 0)
            {
                IsConnected = false;

                Debug.LogWarning(
                    "JSL: デバイスは検出されましたが、"
                    + "Switch Proコントローラーは見つかりませんでした。"
                );

                return;
            }

            IsConnected = true;
            reconnectTimer = reconnectInterval;

            int gyroSpace = usePlayerSpace
                ? JoyShockNative.GyroSpacePlayer
                : JoyShockNative.GyroSpaceLocal;

            JoyShockNative.JslSetGyroSpace(deviceHandle, gyroSpace);

            // 展示中に勝手に補正値が変わらないよう、
            // 自動キャリブレーションは無効にする。
            JoyShockNative.JslSetAutomaticCalibration(
                deviceHandle,
                false
            );

            Debug.Log(
                $"JSL: Switch Proコントローラーを検出しました。"
                + $" Handle = {deviceHandle}"
            );

            if (calibrateOnStart)
            {
                StartCalibration();
            }
        }
        catch (DllNotFoundException exception)
        {
            IsConnected = false;
            enabled = false;

            Debug.LogError(
                "JoyShockLibrary.dllを読み込めませんでした。\n"
                + "Assets/Plugins/x86_64/JoyShockLibrary.dllに"
                + "64ビット版DLLが配置されているか確認してください。\n"
                + exception
            );
        }
        catch (EntryPointNotFoundException exception)
        {
            IsConnected = false;
            enabled = false;

            Debug.LogError(
                "JoyShockLibrary.dll内に必要な関数が見つかりません。\n"
                + "使用しているJSLのバージョンを確認してください。\n"
                + exception
            );
        }
        catch (Exception exception)
        {
            IsConnected = false;

            Debug.LogError(
                "JSLの初期化中にエラーが発生しました。\n"
                + exception
            );
        }
    }

    private bool IsDeviceStillConnected()
    {
        if (deviceHandle < 0)
        {
            return false;
        }

        try
        {
            return JoyShockNative.JslStillConnected(deviceHandle);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "JSLの接続確認中にエラーが発生しました。\n"
                + exception
            );

            return false;
        }
    }

    private void ReadGyro()
    {
        JoyShockNative.JslGetAndFlushAccumulatedGyro(
            deviceHandle,
            out float gyroX,
            out float gyroY,
            out float gyroZ
        );

        Vector3 rawGyro = new Vector3(
            gyroX,
            gyroY,
            gyroZ
        );

        rawGyro.x = ApplyDeadZone(rawGyro.x);
        rawGyro.y = ApplyDeadZone(rawGyro.y);
        rawGyro.z = ApplyDeadZone(rawGyro.z);

        GyroDegreesPerSecond = Vector3.Scale(
            rawGyro,
            axisMultiplier
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

    private void ApplyGyroRotation()
    {
        if (rotationTarget == null)
        {
            return;
        }

        Vector3 rotationDegrees =
            GyroDegreesPerSecond *
            sensitivity *
            Time.unscaledDeltaTime;

        Quaternion gyroRotation = Quaternion.Euler(
            rotationDegrees.x,
            rotationDegrees.y,
            rotationDegrees.z
        );

        rotationTarget.localRotation =
            rotationTarget.localRotation *
            gyroRotation;
    }

    public void StartCalibration()
    {
        if (!IsConnected || deviceHandle < 0)
        {
            Debug.LogWarning(
                "JSL: コントローラーが接続されていないため、"
                + "キャリブレーションを開始できません。"
            );

            return;
        }

        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
        }

        calibrationCoroutine =
            StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        IsCalibrating = true;
        GyroDegreesPerSecond = Vector3.zero;

        Debug.Log(
            $"JSL: キャリブレーションを開始します。"
            + $"コントローラーを{calibrationSeconds:F1}秒間、"
            + "平らな場所に置いて動かさないでください。"
        );

        JoyShockNative.JslResetContinuousCalibration(deviceHandle);
        JoyShockNative.JslStartContinuousCalibration(deviceHandle);

        float elapsedTime = 0.0f;

        while (elapsedTime < calibrationSeconds)
        {
            if (!IsDeviceStillConnected())
            {
                HandleDisconnection();
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        JoyShockNative.JslPauseContinuousCalibration(deviceHandle);

        IsCalibrating = false;
        calibrationCoroutine = null;

        Debug.Log("JSL: キャリブレーションが完了しました。");
    }

    private void HandleDisconnection()
    {
        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
            calibrationCoroutine = null;
        }

        IsCalibrating = false;
        IsConnected = false;
        deviceHandle = -1;
        GyroDegreesPerSecond = Vector3.zero;
        reconnectTimer = 0.0f;

        Debug.LogWarning(
            "JSL: Switch Proコントローラーが切断されました。"
            + "再接続を待機します。"
        );
    }

    private void OnGUI()
    {
        if (!showDebugGUI)
        {
            return;
        }

        const float width = 560.0f;
        const float height = 155.0f;

        GUI.Box(
            new Rect(10.0f, 10.0f, width, height),
            "Switch Pro Controller Gyro"
        );

        GUI.Label(
            new Rect(25.0f, 40.0f, width - 30.0f, 25.0f),
            $"接続状態: {(IsConnected ? "接続中" : "未接続")}"
        );

        GUI.Label(
            new Rect(25.0f, 65.0f, width - 30.0f, 25.0f),
            $"キャリブレーション: "
            + $"{(IsCalibrating ? "実行中" : "停止中")}"
        );

        GUI.Label(
            new Rect(25.0f, 90.0f, width - 30.0f, 25.0f),
            $"Gyro X: {GyroDegreesPerSecond.x:F2} dps  "
            + $"Y: {GyroDegreesPerSecond.y:F2} dps  "
            + $"Z: {GyroDegreesPerSecond.z:F2} dps"
        );

        GUI.Label(
            new Rect(25.0f, 115.0f, width - 30.0f, 25.0f),
            "Cキー: ジャイロを再キャリブレーション"
        );

        GUI.Label(
            new Rect(25.0f, 140.0f, width - 30.0f, 20.0f),
            IsCalibrating
                ? "コントローラーを平らな場所に置いてください"
                : "コントローラーを動かすと対象が回転します"
        );
    }

    private void OnApplicationQuit()
    {
        applicationQuitting = true;
        DisposeLibrary();
    }

    private void OnDestroy()
    {
        if (!applicationQuitting)
        {
            DisposeLibrary();
        }
    }

    private void DisposeLibrary()
    {
        if (!libraryInitialized)
        {
            return;
        }

        try
        {
            JoyShockNative.JslDisconnectAndDisposeAll();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "JSLの終了処理中にエラーが発生しました。\n"
                + exception
            );
        }

        libraryInitialized = false;
        IsConnected = false;
        deviceHandle = -1;
    }
}