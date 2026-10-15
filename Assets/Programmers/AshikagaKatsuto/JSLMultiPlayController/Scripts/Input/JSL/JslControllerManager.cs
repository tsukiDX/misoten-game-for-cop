using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Input.JSL
{
    [DefaultExecutionOrder(-1000)]
    public sealed class JslControllerManager : MonoBehaviour
    {
        public const int MaximumControllerCount = 4;
        public const int MaximumPlayerCount = 4;

        public static JslControllerManager Instance
        {
            get;
            private set;
        }

        [Header("接続検索")]
        [SerializeField]
        [Min(0.1f)]
        private float reconnectInterval = 1.0f;

        [Header("参加・離脱")]
        [SerializeField]
        private JslButton joinButton = JslButton.Plus;

        [SerializeField]
        private JslButton leaveButton = JslButton.Minus;

        [SerializeField]
        [Min(0.1f)]
        private float leaveHoldDuration = 2.0f;

        [Header("ジャイロ")]
        [SerializeField]
        private bool enableAutomaticCalibration = true;

        [Header("オブジェクト")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Header("デバッグ")]
        [SerializeField]
        private bool logConnectionChanges = true;

        private readonly Dictionary<int, JslControllerDevice>
            controllers = new();

        private readonly List<LocalPlayerSlot>
            playerSlots = new();

        private readonly Dictionary<int, float>
            leaveHoldTimers = new();

        private readonly List<int>
            disconnectedHandles = new();

        private float reconnectTimer;

        private bool libraryStarted;
        private bool libraryDisposed;
        private bool initializationFailed;

        public IReadOnlyDictionary<int, JslControllerDevice>
            Controllers => controllers;

        public IReadOnlyList<LocalPlayerSlot>
            PlayerSlots => playerSlots;

        public int ConnectedControllerCount =>
            controllers.Count;

        public int JoinedPlayerCount
        {
            get
            {
                int count = 0;

                foreach (
                    LocalPlayerSlot playerSlot
                    in playerSlots
                )
                {
                    if (playerSlot.IsJoined)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public event Action<JslControllerDevice>
            ControllerConnected;

        public event Action<int>
            ControllerDisconnected;

        public event Action<LocalPlayerSlot>
            PlayerJoined;

        public event Action<LocalPlayerSlot>
            PlayerLeft;

        public event Action<LocalPlayerSlot>
            PlayerControllerDisconnected;

        private void Awake()
        {
            if (
                Instance != null
                &&
                Instance != this
            )
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            CreatePlayerSlots();
        }

        private void Start()
        {
            TryInitializeLibrary();
        }

        private void Update()
        {
            if (initializationFailed)
            {
                return;
            }

            // 一旦、自動再検索を停止する。
             //UpdateReconnectSearch();

            UpdateControllers();
            UpdatePlayerJoinAndLeave();
        }

        private void CreatePlayerSlots()
        {
            playerSlots.Clear();

            for (
                int index = 0;
                index < MaximumPlayerCount;
                index++
            )
            {
                playerSlots.Add(
                    new LocalPlayerSlot(index)
                );
            }
        }

        private void TryInitializeLibrary()
        {
            JslNativeLibraryBootstrap
                .EnsureLoaded();

            if (
                !JslNativeLibraryBootstrap
                    .IsLoaded
            )
            {
                initializationFailed =
                    true;

                Debug.LogError(
                    "JoyShockLibrary.dllを"
                    + "ロードできなかったため、"
                    + "JSLの初期化を中止します。"
                );

                return;
            }

            try
            {
                SearchForControllers();

                libraryStarted = true;
                libraryDisposed = false;
            }
            catch (DllNotFoundException exception)
            {
                initializationFailed = true;

                Debug.LogError(
                    "JoyShockLibrary.dllを読み込めませんでした。\n"
                    + "Assets/Plugins/x86_64/"
                    + "JoyShockLibrary.dllを確認してください。\n"
                    + exception
                );
            }
            catch (EntryPointNotFoundException exception)
            {
                initializationFailed = true;

                Debug.LogError(
                    "JoyShockLibrary.dll内に必要な関数が"
                    + "見つかりませんでした。\n"
                    + "DLLのバージョンを確認してください。\n"
                    + exception
                );
            }
            catch (Exception exception)
            {
                initializationFailed = true;

                Debug.LogError(
                    "JSLの初期化に失敗しました。\n"
                    + exception
                );
            }
        }

        private void UpdateReconnectSearch()
        {
            reconnectTimer -= Time.unscaledDeltaTime;

            if (reconnectTimer > 0.0f)
            {
                return;
            }

            reconnectTimer = reconnectInterval;

            SearchForControllers();
        }

        public void SearchForControllers()
{
    int detectedDeviceCount =
        JslNative.JslConnectDevices();

    libraryStarted = true;
    libraryDisposed = false;

    if (detectedDeviceCount <= 0)
    {
        return;
    }

    int[] detectedHandles =
        new int[detectedDeviceCount];

    int returnedHandleCount =
        JslNative.JslGetConnectedDeviceHandles(
            detectedHandles,
            detectedHandles.Length
        );

    int usableHandleCount =
        Mathf.Min(
            returnedHandleCount,
            detectedHandles.Length
        );

    for (
        int index = 0;
        index < usableHandleCount;
        index++
    )
    {
        if (
            controllers.Count
            >= MaximumControllerCount
        )
        {
            break;
        }

        int deviceHandle =
            detectedHandles[index];

        if (
            controllers.ContainsKey(
                deviceHandle
            )
        )
        {
            continue;
        }

        // JoyShockLibrary-plus-HDRumble版には
        // JslStillConnectedが存在しないため、
        // JslConnectDevicesが返したハンドルを
        // そのまま登録する。
        RegisterController(
            deviceHandle
        );
    }
}

        private void RegisterController(
            int deviceHandle
        )
        {
            JslControllerType controllerType =
                (JslControllerType)
                JslNative.JslGetControllerType(
                    deviceHandle
                );

            JslControllerSplitType splitType =
                (JslControllerSplitType)
                JslNative.JslGetControllerSplitType(
                    deviceHandle
                );

            JslControllerDevice controller =
                new JslControllerDevice(
                    deviceHandle,
                    controllerType,
                    splitType
                );

            controllers.Add(
                deviceHandle,
                controller
            );

            leaveHoldTimers[deviceHandle] = 0.0f;

            try
            {
                JslNative.JslSetAutomaticCalibration(
                    deviceHandle,
                    enableAutomaticCalibration
                );
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogWarning(
                    "JslSetAutomaticCalibrationが"
                    + "見つからないため、"
                    + "自動キャリブレーション設定を"
                    + "スキップします。"
                );
            }

            if (logConnectionChanges)
            {
                Debug.Log(
                    "JSLコントローラー接続:"
                    + $" Handle={deviceHandle}"
                    + $" Type={controllerType}"
                    + $" Split={splitType}"
                );
            }

            ControllerConnected?.Invoke(controller);
        }

            public bool RequestControllerSearch()
            {
                if (initializationFailed)
                {
                    Debug.LogWarning(
                        "JSLの初期化に失敗しているため、"
                        + "コントローラーを検索できません。"
                    );

                    return false;
                }

                try
                {
                    SearchForControllers();
                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "JSLコントローラーの再検索に"
                        + "失敗しました。\n"
                        + exception
                    );

                    return false;
                }
            }

private void UpdateControllers()
{
    disconnectedHandles.Clear();

    foreach (
        KeyValuePair<
            int,
            JslControllerDevice
        > pair
        in controllers
    )
    {
        int deviceHandle =
            pair.Key;

        JslControllerDevice controller =
            pair.Value;

        if (controller == null)
        {
            disconnectedHandles.Add(
                deviceHandle
            );

            continue;
        }

        try
        {
            // plus-HDRumble版には
            // JslStillConnectedが存在しないため、
            // 入力取得が成功するかどうかで
            // 接続状態を判断する。
            controller.UpdateInput();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "JSL入力取得に失敗しました。"
                + $" Handle={deviceHandle}\n"
                + exception
            );

            disconnectedHandles.Add(
                deviceHandle
            );
        }
    }

    foreach (
        int deviceHandle
        in disconnectedHandles
    )
    {
        RemoveDisconnectedController(
            deviceHandle
        );
    }
}

        private void RemoveDisconnectedController(
            int deviceHandle
        )
        {
            if (
                !controllers.TryGetValue(
                    deviceHandle,
                    out JslControllerDevice controller
                )
            )
            {
                return;
            }

            controller.MarkDisconnected();

            if (controller.IsAssigned)
            {
                int playerIndex =
                    controller.AssignedPlayerIndex;

                if (
                    IsValidPlayerIndex(
                        playerIndex
                    )
                )
                {
                    LocalPlayerSlot playerSlot =
                        playerSlots[playerIndex];

                    playerSlot.BeginReconnectWait();

                    PlayerControllerDisconnected?.Invoke(
                        playerSlot
                    );
                }

                controller.RemovePlayerAssignment();
            }

            controllers.Remove(deviceHandle);
            leaveHoldTimers.Remove(deviceHandle);

            if (logConnectionChanges)
            {
                Debug.LogWarning(
                    "JSLコントローラー切断:"
                    + $" Handle={deviceHandle}"
                );
            }

            ControllerDisconnected?.Invoke(
                deviceHandle
            );

            reconnectTimer = 0.0f;
        }

        private void UpdatePlayerJoinAndLeave()
        {
            foreach (
                JslControllerDevice controller
                in controllers.Values
            )
            {
                if (!controller.IsAssigned)
                {
                    if (
                        controller
                            .WasButtonPressedThisFrame(
                                joinButton
                            )
                    )
                    {
                        TryJoinPlayer(controller);
                    }

                    continue;
                }

                UpdateLeaveInput(controller);
            }
        }

        private void UpdateLeaveInput(
            JslControllerDevice controller
        )
        {
            int deviceHandle =
                controller.DeviceHandle;

            if (
                controller.IsButtonPressed(
                    leaveButton
                )
            )
            {
                leaveHoldTimers[deviceHandle] +=
                    Time.unscaledDeltaTime;

                if (
                    leaveHoldTimers[deviceHandle]
                    >= leaveHoldDuration
                )
                {
                    LeavePlayerByController(
                        controller
                    );

                    leaveHoldTimers[deviceHandle] =
                        0.0f;
                }
            }
            else
            {
                leaveHoldTimers[deviceHandle] =
                    0.0f;
            }
        }

        private bool TryJoinPlayer(
    JslControllerDevice controller
)
{
    if (controller == null)
    {
        return false;
    }

    if (!controller.IsConnected)
    {
        return false;
    }

    if (controller.IsAssigned)
    {
        return false;
    }

    LocalPlayerSlot playerSlot =
        FindReconnectWaitingSlot();

    bool isReconnect =
        playerSlot != null;

    if (playerSlot == null)
    {
        playerSlot =
            FindAvailablePlayerSlot();
    }

    if (playerSlot == null)
    {
        Debug.LogWarning(
            "空いているプレイヤースロットが"
            + "ありません。"
        );

        return false;
    }

    if (isReconnect)
    {
        playerSlot.Reconnect(
            controller.DeviceHandle
        );
    }
    else
    {
        playerSlot.Join(
            controller.DeviceHandle
        );
    }

    controller.AssignPlayer(
        playerSlot.PlayerIndex
    );

    // プレイヤーランプ出力は、
    // 入力システムが安定するまで使用しない。
    //
    // TrySetControllerPlayerNumber(
    //     controller.DeviceHandle,
    //     playerSlot.PlayerNumber
    // );

    if (logConnectionChanges)
    {
        if (isReconnect)
        {
            Debug.Log(
                $"Player {playerSlot.PlayerNumber}"
                + " のコントローラーが"
                + "再接続されました。"
                + $" Handle={controller.DeviceHandle}"
            );
        }
        else
        {
            Debug.Log(
                $"Player {playerSlot.PlayerNumber}"
                + " が参加しました。"
                + $" Handle={controller.DeviceHandle}"
            );
        }
    }

    PlayerJoined?.Invoke(playerSlot);

    return true;
}

        private LocalPlayerSlot FindAvailablePlayerSlot()
        {
            foreach (
                LocalPlayerSlot playerSlot
                in playerSlots
            )
            {
                if (!playerSlot.IsJoined)
                {
                    return playerSlot;
                }
            }

            return null;
        }

        public bool TryJoinPlayer(
            int deviceHandle
        )
        {
            if (
                !controllers.TryGetValue(
                    deviceHandle,
                    out JslControllerDevice controller
                )
            )
            {
                return false;
            }

            if (controller.IsAssigned)
            {
                return false;
            }

            return TryJoinPlayer(controller);
        }

        public void LeavePlayer(
            int playerIndex
        )
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return;
            }

            LocalPlayerSlot playerSlot =
                playerSlots[playerIndex];

            if (!playerSlot.IsJoined)
            {
                return;
            }

            if (
                playerSlot.DeviceHandle >= 0
                &&
                controllers.TryGetValue(
                    playerSlot.DeviceHandle,
                    out JslControllerDevice controller
                )
            )
            {
                LeavePlayerByController(controller);
                return;
            }

            playerSlot.Leave();

            PlayerLeft?.Invoke(playerSlot);
        }

        private void LeavePlayerByController(
            JslControllerDevice controller
        )
        {
            int playerIndex =
                controller.AssignedPlayerIndex;

            if (!IsValidPlayerIndex(playerIndex))
            {
                controller.RemovePlayerAssignment();
                return;
            }

            LocalPlayerSlot playerSlot =
                playerSlots[playerIndex];

            controller.StopRumble();
            controller.RemovePlayerAssignment();

            playerSlot.Leave();

            if (logConnectionChanges)
            {
                Debug.Log(
                    $"Player {playerIndex + 1}"
                    + " が離脱しました。"
                );
            }

            PlayerLeft?.Invoke(playerSlot);
        }

        private void TrySetControllerPlayerNumber(
            int deviceHandle,
            int playerNumber
        )
        {
            try
            {
                JslNative.JslSetPlayerNumber(
                    deviceHandle,
                    playerNumber
                );
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogWarning(
                    "JslSetPlayerNumberが"
                    + "見つからないため、"
                    + "プレイヤーランプ設定を"
                    + "スキップします。"
                );
            }
        }

        public bool TryGetController(
            int deviceHandle,
            out JslControllerDevice controller
        )
        {
            return controllers.TryGetValue(
                deviceHandle,
                out controller
            );
        }

        public bool TryGetPlayerController(
            int playerIndex,
            out JslControllerDevice controller
        )
        {
            controller = null;

            if (!IsValidPlayerIndex(playerIndex))
            {
                return false;
            }

            LocalPlayerSlot playerSlot =
                playerSlots[playerIndex];

            if (!playerSlot.HasController)
            {
                return false;
            }

            return controllers.TryGetValue(
                playerSlot.DeviceHandle,
                out controller
            );
        }

                public JslInputSnapshot GetPlayerInput(
            int playerIndex
        )
        {
            if (
                TryGetPlayerController(
                    playerIndex,
                    out JslControllerDevice controller
                )
            )
            {
                return controller.CurrentInput;
            }

            return default;
        }

        public bool IsPlayerJoined(
            int playerIndex
        )
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return false;
            }

            return playerSlots[playerIndex].IsJoined;
        }

        public bool IsPlayerWaitingForReconnect(
            int playerIndex
        )
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return false;
            }

            return playerSlots[playerIndex]
                .IsWaitingForReconnect;
        }

        public void SetPlayerRumble(
            int playerIndex,
            float smallMotor,
            float largeMotor
        )
        {
            if (
                !TryGetPlayerController(
                    playerIndex,
                    out JslControllerDevice controller
                )
            )
            {
                return;
            }

            try
            {
                controller.SetRumble(
                    smallMotor,
                    largeMotor
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Player {playerIndex + 1}の"
                    + "振動設定に失敗しました。\n"
                    + exception
                );
            }
        }

        public void StopPlayerRumble(
            int playerIndex
        )
        {
            if (
                !TryGetPlayerController(
                    playerIndex,
                    out JslControllerDevice controller
                )
            )
            {
                return;
            }

            try
            {
                controller.StopRumble();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Player {playerIndex + 1}の"
                    + "振動停止に失敗しました。\n"
                    + exception
                );
            }
        }

        public void StopAllRumble()
        {
            foreach (
                JslControllerDevice controller
                in controllers.Values
            )
            {
                try
                {
                    controller.StopRumble();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "コントローラーの振動停止に"
                        + "失敗しました。"
                        + $" Handle={controller.DeviceHandle}\n"
                        + exception
                    );
                }
            }
        }

        private static bool IsValidPlayerIndex(
            int playerIndex
        )
        {
            return
                playerIndex >= 0
                &&
                playerIndex < MaximumPlayerCount;
        }

        private void OnApplicationQuit()
        {
            DisposeLibrary();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            DisposeLibrary();

            Instance = null;
        }

        private LocalPlayerSlot FindReconnectWaitingSlot()
        {
            foreach (
                LocalPlayerSlot playerSlot
                in playerSlots
            )
            {
                if (
                    playerSlot.IsJoined
                    &&
                    playerSlot.IsWaitingForReconnect
                )
                {
                    return playerSlot;
                }
            }

            return null;
        }

        private void DisposeLibrary()
        {
            if (
                !libraryStarted
                ||
                libraryDisposed
            )
            {
                return;
            }

            foreach (
                JslControllerDevice controller
                in controllers.Values
            )
            {
                try
                {
                    controller.StopRumble();
                }
                catch
                {
                    // アプリケーションの終了処理中なので、
                    // 個別の振動停止エラーは無視する。
                }
            }

            try
            {
                JslNative.JslDisconnectAndDisposeAll();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "JSLの終了処理に失敗しました。\n"
                    + exception
                );
            }

            controllers.Clear();
            leaveHoldTimers.Clear();
            disconnectedHandles.Clear();

            foreach (
                LocalPlayerSlot playerSlot
                in playerSlots
            )
            {
                playerSlot.Leave();
            }

            libraryDisposed = true;
            libraryStarted = false;
        }
    }
}