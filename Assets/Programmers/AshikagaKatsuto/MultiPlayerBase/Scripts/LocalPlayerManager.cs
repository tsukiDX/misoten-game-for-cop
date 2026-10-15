using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Input.JSL
{
    [DefaultExecutionOrder(-900)]
    public sealed class LocalPlayerManager :
        MonoBehaviour
    {
        public static LocalPlayerManager Instance
        {
            get;
            private set;
        }

        [SerializeField]
        private JslControllerManager
            controllerManager;

        [SerializeField]
        private LocalPlayerController[]
            localPlayers =
                new LocalPlayerController[4];

        public IReadOnlyList<LocalPlayerController>
            LocalPlayers => localPlayers;

        public event Action<LocalPlayerController>
            PlayerActivated;

        public event Action<LocalPlayerController>
            PlayerDeactivated;

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
        }

        private void Start()
        {
            if (controllerManager == null)
            {
                controllerManager =
                    JslControllerManager.Instance;
            }

            if (controllerManager == null)
            {
                Debug.LogError(
                    "JslControllerManagerが"
                    + "見つかりません。"
                );

                enabled = false;
                return;
            }

            SubscribeEvents();
            SynchronizeAllPlayers();
        }

        private void SubscribeEvents()
        {
            controllerManager.PlayerJoined +=
                OnPlayerJoined;

            controllerManager.PlayerLeft +=
                OnPlayerLeft;

            controllerManager
                .PlayerControllerDisconnected +=
                OnPlayerControllerDisconnected;
        }

        private void UnsubscribeEvents()
        {
            if (controllerManager == null)
            {
                return;
            }

            controllerManager.PlayerJoined -=
                OnPlayerJoined;

            controllerManager.PlayerLeft -=
                OnPlayerLeft;

            controllerManager
                .PlayerControllerDisconnected -=
                OnPlayerControllerDisconnected;
        }

        private void SynchronizeAllPlayers()
        {
            for (
                int playerIndex = 0;
                playerIndex < localPlayers.Length;
                playerIndex++
            )
            {
                LocalPlayerController localPlayer =
                    localPlayers[playerIndex];

                if (localPlayer == null)
                {
                    continue;
                }

                if (
                    controllerManager
                        .TryGetPlayerController(
                            playerIndex,
                            out JslControllerDevice controller
                        )
                )
                {
                    localPlayer.BindController(
                        controller
                    );
                }
                else
                {
                    localPlayer.LeavePlayer();
                }
            }
        }

        private void OnPlayerJoined(
            LocalPlayerSlot playerSlot
        )
        {
            int playerIndex =
                playerSlot.PlayerIndex;

            if (
                !TryGetLocalPlayer(
                    playerIndex,
                    out LocalPlayerController localPlayer
                )
            )
            {
                return;
            }

            if (
                !controllerManager
                    .TryGetPlayerController(
                        playerIndex,
                        out JslControllerDevice controller
                    )
            )
            {
                return;
            }

            localPlayer.BindController(
                controller
            );

            PlayerActivated?.Invoke(
                localPlayer
            );
        }

        private void OnPlayerLeft(
            LocalPlayerSlot playerSlot
        )
        {
            if (
                !TryGetLocalPlayer(
                    playerSlot.PlayerIndex,
                    out LocalPlayerController localPlayer
                )
            )
            {
                return;
            }

            localPlayer.LeavePlayer();

            PlayerDeactivated?.Invoke(
                localPlayer
            );
        }

        private void OnPlayerControllerDisconnected(
            LocalPlayerSlot playerSlot
        )
        {
            if (
                !TryGetLocalPlayer(
                    playerSlot.PlayerIndex,
                    out LocalPlayerController localPlayer
                )
            )
            {
                return;
            }

            // プレイヤー参加情報は残すが、
            // コントローラー入力は停止する。
            localPlayer.UnbindController(
                keepPlayerJoined: true
            );
        }

        public bool TryGetLocalPlayer(
            int playerIndex,
            out LocalPlayerController localPlayer
        )
        {
            localPlayer = null;

            if (
                playerIndex < 0
                ||
                playerIndex >= localPlayers.Length
            )
            {
                return false;
            }

            localPlayer =
                localPlayers[playerIndex];

            return localPlayer != null;
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}