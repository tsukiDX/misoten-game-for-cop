using UnityEngine;

namespace Game.Input.JSL
{
    [DefaultExecutionOrder(0)]
    public sealed class JslGyroPointer :
        MonoBehaviour
    {
        [Header("Player")]
        [SerializeField]
        private LocalPlayerController localPlayer;

        [Header("UI References")]
        [SerializeField]
        private RectTransform pointerRect;

        [Header("Virtual Resolution")]
        [Tooltip(
            "ポインター位置を管理する固定の仮想解像度です。"
            + "画面サイズが変わっても、この値は変更しません。"
        )]
        [SerializeField]
        private Vector2 virtualResolution =
            new Vector2(
                1920.0f,
                1080.0f
            );

        [Header("Gyro Aim")]
        [SerializeField]
        private bool enableGyroAim = true;

        [Tooltip("ジャイロ照準の感度です。")]
        [SerializeField]
        [Range(0.01f, 100.0f)]
        private float gyroSensitivity = 3.0f;

        [Tooltip(
            "各ジャイロ軸について、"
            + "この値未満の入力を無視します。"
        )]
        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float gyroDeadZone = 0.5f;

        [Tooltip(
            "左右移動に使用するジャイロ軸です。"
        )]
        [SerializeField]
        private Vector3 horizontalAxis =
            new Vector3(
                0.0f,
                -1.0f,
                0.0f
            );

        [Tooltip(
            "上下移動に使用するジャイロ軸です。"
        )]
        [SerializeField]
        private Vector3 verticalAxis =
            new Vector3(
                -1.0f,
                0.0f,
                0.0f
            );

        [Header("Stick Aim")]
        [SerializeField]
        private bool enableStickAim = true;

        [Tooltip(
            "右スティック照準のデッドゾーンです。"
        )]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float stickDeadZone = 0.2f;

        [Tooltip(
            "右スティック最大入力時の"
            + "仮想座標上の移動速度です。"
        )]
        [SerializeField]
        [Range(10.0f, 3000.0f)]
        private float stickSpeed = 700.0f;

        [SerializeField]
        private bool invertStickHorizontal;

        [SerializeField]
        private bool invertStickVertical;

        [Header("Movement")]
        [Tooltip(
            "ポインターと仮想画面端との余白です。"
        )]
        [SerializeField]
        [Range(0.0f, 200.0f)]
        private float screenMargin = 10.0f;

        [Tooltip(
            "表示位置の補間速度です。"
            + "0の場合は補間しません。"
        )]
        [SerializeField]
        [Range(0.0f, 50.0f)]
        private float smoothing = 12.0f;

        [Tooltip(
            "入力がないときに中央へ戻します。"
        )]
        [SerializeField]
        private bool returnToCenter;

        [Tooltip(
            "中央へ戻る速度です。"
        )]
        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float returnSpeed = 3.0f;

        [Header("Reset")]
        [SerializeField]
        private JslButton resetButton =
            JslButton.RightStickClick;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLog;

        private Vector3 currentGyro =
            Vector3.zero;

        // 左下を(0, 0)、右上を(1920, 1080)とする
        // 固定仮想座標上の目標位置。
        private Vector2 targetVirtualPosition;

        // 補間後の仮想座標。
        private Vector2 displayedVirtualPosition;

        private bool initialized;
        private Vector2 rawMovementThisFrame =
            Vector2.zero;

        private bool circularConstraintActive;

        private Vector2 circularConstraintCenter;

        private float circularConstraintRadius;

        public LocalPlayerController LocalPlayer =>
            localPlayer;

        public Vector3 Gyro =>
            currentGyro;

        public Vector2 VirtualPosition =>
            targetVirtualPosition;

        public Vector2 DisplayedVirtualPosition =>
            displayedVirtualPosition;

        public Vector2 RawMovementThisFrame =>
    rawMovementThisFrame;

public bool IsCircularConstraintActive =>
    circularConstraintActive;

public Vector2 CircularConstraintCenter =>
    circularConstraintCenter;

public float CircularConstraintRadius =>
    circularConstraintRadius;

        private void Reset()
        {
            pointerRect =
                transform as RectTransform;
        }

        private void Awake()
        {
            if (pointerRect == null)
            {
                pointerRect =
                    transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void Update()
{
    if (!initialized)
    {
        InitializePointer();
    }

    if (!initialized)
    {
        return;
    }

    rawMovementThisFrame =
        Vector2.zero;

    if (
        localPlayer == null
        ||
        !localPlayer.IsInputAvailable
    )
    {
        currentGyro =
            Vector3.zero;

        return;
    }

    ProcessResetInput();

    Vector2 positionBeforeInput =
        targetVirtualPosition;

    bool receivedInput =
        false;

    if (ProcessGyroAim())
    {
        receivedInput =
            true;
    }

    if (ProcessStickAim())
    {
        receivedInput =
            true;
    }

    rawMovementThisFrame =
        targetVirtualPosition
        - positionBeforeInput;

    if (
        returnToCenter
        &&
        !receivedInput
        &&
        !circularConstraintActive
    )
    {
        MoveTargetTowardCenter();
    }

    targetVirtualPosition =
        ClampVirtualPosition(
            targetVirtualPosition
        );

    if (circularConstraintActive)
    {
        targetVirtualPosition =
            ClampToCircularConstraint(
                targetVirtualPosition
            );
    }

    UpdateDisplayedPosition();

    if (showDebugLog)
    {
        Debug.Log(
            $"Player {localPlayer.PlayerNumber}"
            + $" Gyro={currentGyro}"
            + $" Target={targetVirtualPosition}"
            + $" Displayed={displayedVirtualPosition}"
            + $" RawMovement={rawMovementThisFrame}"
            + $" CircularConstraint="
            + $"{circularConstraintActive}"
        );
    }
}
        private void InitializePointer()
        {
            if (pointerRect == null)
            {
                return;
            }

            if (
                virtualResolution.x <= 0.0f
                ||
                virtualResolution.y <= 0.0f
            )
            {
                Debug.LogError(
                    "Virtual Resolutionには"
                    + "0より大きい値を設定してください。"
                );

                return;
            }

            ResetToCenter();

            initialized = true;
        }

        private bool ProcessGyroAim()
        {
            currentGyro =
                Vector3.zero;

            if (!enableGyroAim)
            {
                return false;
            }

            currentGyro =
                ApplyGyroDeadZone(
                    localPlayer.Gyroscope
                );

            float horizontalInput =
                Vector3.Dot(
                    currentGyro,
                    horizontalAxis
                );

            float verticalInput =
                Vector3.Dot(
                    currentGyro,
                    verticalAxis
                );

            Vector2 movement =
                new Vector2(
                    horizontalInput,
                    verticalInput
                );

            if (
                movement.sqrMagnitude
                <= 0.000001f
            )
            {
                return false;
            }

            targetVirtualPosition +=
                movement
                * gyroSensitivity
                * Time.unscaledDeltaTime;

            return true;
        }

        private bool ProcessStickAim()
        {
            if (!enableStickAim)
            {
                return false;
            }

            Vector2 stick =
                ApplyStickDeadZone(
                    localPlayer.RightStick
                );

            if (invertStickHorizontal)
            {
                stick.x *= -1.0f;
            }

            if (invertStickVertical)
            {
                stick.y *= -1.0f;
            }

            if (
                stick.sqrMagnitude
                <= 0.000001f
            )
            {
                return false;
            }

            targetVirtualPosition +=
                stick
                * stickSpeed
                * Time.unscaledDeltaTime;

            return true;
        }

        private void ProcessResetInput()
        {
            if (
                localPlayer == null
                ||
                !localPlayer.IsInputAvailable
            )
            {
                return;
            }

            if (
                localPlayer.WasButtonPressedThisFrame(
                    resetButton
                )
            )
            {
                ResetPointerToCenter();
            }
        }

        [ContextMenu("Reset Pointer To Center")]
        public void ResetPointerToCenter()
        {
EndCircularConstraint();

rawMovementThisFrame =
    Vector2.zero;

            Vector2 center =
                GetVirtualCenter();

            targetVirtualPosition =
                center;

            displayedVirtualPosition =
                center;

            ApplyVirtualPositionToRect(
                displayedVirtualPosition
            );
        }


        private void MoveTargetTowardCenter()
        {
            Vector2 center =
                GetVirtualCenter();

            if (returnSpeed <= 0.0f)
            {
                targetVirtualPosition =
                    center;

                return;
            }

            float interpolationWeight =
                1.0f
                - Mathf.Exp(
                    -returnSpeed
                    * Time.unscaledDeltaTime
                );

            targetVirtualPosition =
                Vector2.Lerp(
                    targetVirtualPosition,
                    center,
                    interpolationWeight
                );
        }

        private void UpdateDisplayedPosition()
        {
            if (smoothing <= 0.0f)
            {
                displayedVirtualPosition =
                    targetVirtualPosition;
            }
            else
            {
                float interpolationWeight =
                    1.0f
                    - Mathf.Exp(
                        -smoothing
                        * Time.unscaledDeltaTime
                    );

                displayedVirtualPosition =
                    Vector2.Lerp(
                        displayedVirtualPosition,
                        targetVirtualPosition,
                        interpolationWeight
                    );
            }

            ApplyVirtualPositionToRect(
                displayedVirtualPosition
            );
        }

        private void ApplyVirtualPositionToRect(
            Vector2 virtualPosition
        )
        {
            if (pointerRect == null)
            {
                return;
            }

            // PlayerPointerのアンカーを親中央に固定している前提。
            //
            // 仮想座標:
            // 左下 = (0, 0)
            // 中央 = (960, 540)
            // 右上 = (1920, 1080)
            //
            // anchoredPosition:
            // 左下方向 = 負
            // 中央 = (0, 0)
            // 右上方向 = 正
            Vector2 centeredPosition =
                virtualPosition
                - GetVirtualCenter();

            pointerRect.anchoredPosition =
                centeredPosition;
        }

        private Vector2 ClampVirtualPosition(
            Vector2 position
        )
        {
            Vector2 pointerSize =
                GetPointerVirtualSize();

            Vector2 pointerPivot =
                pointerRect != null
                    ? pointerRect.pivot
                    : new Vector2(
                        0.5f,
                        0.5f
                    );

            float minimumX =
                pointerSize.x
                * pointerPivot.x
                + screenMargin;

            float maximumX =
                virtualResolution.x
                - pointerSize.x
                * (1.0f - pointerPivot.x)
                - screenMargin;

            float minimumY =
                pointerSize.y
                * pointerPivot.y
                + screenMargin;

            float maximumY =
                virtualResolution.y
                - pointerSize.y
                * (1.0f - pointerPivot.y)
                - screenMargin;

            maximumX =
                Mathf.Max(
                    maximumX,
                    minimumX
                );

            maximumY =
                Mathf.Max(
                    maximumY,
                    minimumY
                );

            position.x =
                Mathf.Clamp(
                    position.x,
                    minimumX,
                    maximumX
                );

            position.y =
                Mathf.Clamp(
                    position.y,
                    minimumY,
                    maximumY
                );

            return position;
        }

        private Vector2 GetPointerVirtualSize()
        {
            if (pointerRect == null)
            {
                return Vector2.zero;
            }

            Vector2 scale =
                new Vector2(
                    Mathf.Abs(
                        pointerRect.localScale.x
                    ),
                    Mathf.Abs(
                        pointerRect.localScale.y
                    )
                );

            return Vector2.Scale(
                pointerRect.rect.size,
                scale
            );
        }

        private Vector2 GetVirtualCenter()
        {
            return virtualResolution * 0.5f;
        }

        private Vector3 ApplyGyroDeadZone(
            Vector3 value
        )
        {
            Vector3 result =
                value;

            if (
                Mathf.Abs(result.x)
                < gyroDeadZone
            )
            {
                result.x = 0.0f;
            }

            if (
                Mathf.Abs(result.y)
                < gyroDeadZone
            )
            {
                result.y = 0.0f;
            }

            if (
                Mathf.Abs(result.z)
                < gyroDeadZone
            )
            {
                result.z = 0.0f;
            }

            return result;
        }

        private Vector2 ApplyStickDeadZone(
            Vector2 value
        )
        {
            float magnitude =
                value.magnitude;

            if (
                magnitude <= stickDeadZone
                ||
                magnitude <= 0.000001f
            )
            {
                return Vector2.zero;
            }

            float remappedMagnitude =
                Mathf.InverseLerp(
                    stickDeadZone,
                    1.0f,
                    Mathf.Clamp01(
                        magnitude
                    )
                );

            return
                value.normalized
                * remappedMagnitude;
        }

        [ContextMenu("Reset To Center")]
        public void ResetToCenter()
        {
            EndCircularConstraint();

rawMovementThisFrame =
    Vector2.zero;

            Vector2 center =
                GetVirtualCenter();

            targetVirtualPosition =
                center;

            displayedVirtualPosition =
                center;

            ApplyVirtualPositionToRect(
                displayedVirtualPosition
            );
        }

        public Vector2 GetPointerScreenPosition()
        {
            if (pointerRect == null)
            {
                return Vector2.zero;
            }

            Canvas canvas =
                pointerRect
                    .GetComponentInParent<
                        Canvas
                    >();

            Camera canvasCamera =
                null;

            if (
                canvas != null
                &&
                canvas.renderMode
                    != RenderMode.ScreenSpaceOverlay
            )
            {
                canvasCamera =
                    canvas.worldCamera;
            }

            return RectTransformUtility
                .WorldToScreenPoint(
                    canvasCamera,
                    pointerRect.position
                );
        }

        public void SetLocalPlayer(
            LocalPlayerController newLocalPlayer
        )
        {
            localPlayer =
                newLocalPlayer;

            initialized = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!initialized)
            {
                return;
            }

            // 画面サイズが変わっても仮想座標は変更しない。
            // 同じ仮想座標を再度UIへ反映するだけ。
            ApplyVirtualPositionToRect(
                displayedVirtualPosition
            );
        }

        public void ApplySettings(
    Game.Weapons.GyroAimSettings settings
)
{
    if (settings == null)
    {
        return;
    }

    enableGyroAim =
        settings.EnableGyroAim;

    gyroSensitivity =
        settings.GyroSensitivity;

    gyroDeadZone =
        settings.GyroDeadZone;

    horizontalAxis =
        settings.HorizontalAxis;

    verticalAxis =
        settings.VerticalAxis;

    enableStickAim =
        settings.EnableStickAim;

    stickDeadZone =
        settings.StickDeadZone;

    stickSpeed =
        settings.StickSpeed;

    invertStickHorizontal =
        settings.InvertStickHorizontal;

    invertStickVertical =
        settings.InvertStickVertical;

    screenMargin =
        settings.ScreenMargin;

    smoothing =
        settings.Smoothing;

    returnToCenter =
        settings.ReturnToCenter;

    returnSpeed =
        settings.ReturnSpeed;

    resetButton =
        settings.ResetButton;

    ResetPointerToCenter();
}

public void BeginCircularConstraint(
    Vector2 virtualCenter,
    float virtualRadius
)
{
    circularConstraintCenter =
        ClampVirtualPosition(
            virtualCenter
        );

    circularConstraintRadius =
        Mathf.Max(
            1.0f,
            virtualRadius
        );

    circularConstraintActive =
        true;

    targetVirtualPosition =
        ClampVirtualPosition(
            targetVirtualPosition
        );

    targetVirtualPosition =
        ClampToCircularConstraint(
            targetVirtualPosition
        );

    displayedVirtualPosition =
        targetVirtualPosition;

    rawMovementThisFrame =
        Vector2.zero;

    ApplyVirtualPositionToRect(
        displayedVirtualPosition
    );
}

public void UpdateCircularConstraint(
    Vector2 virtualCenter,
    float virtualRadius
)
{
    Vector2 newCenter =
        ClampVirtualPosition(
            virtualCenter
        );

    float newRadius =
        Mathf.Max(
            1.0f,
            virtualRadius
        );

    if (!circularConstraintActive)
    {
        BeginCircularConstraint(
            newCenter,
            newRadius
        );

        return;
    }

    // 敵が移動したことで円の中心が動いた量。
    Vector2 centerMovement =
        newCenter
        - circularConstraintCenter;

    circularConstraintCenter =
        newCenter;

    circularConstraintRadius =
        newRadius;

    // レティクルの目標位置を敵の移動量だけ動かす。
    // 敵を中心とした相対位置を維持する。
    targetVirtualPosition +=
        centerMovement;

    // 表示中のレティクルも同じ量だけ動かす。
    // smoothingによる遅れで、敵からレティクルが
    // 取り残されることを防ぐ。
    displayedVirtualPosition +=
        centerMovement;

    targetVirtualPosition =
        ClampVirtualPosition(
            targetVirtualPosition
        );

    targetVirtualPosition =
        ClampToCircularConstraint(
            targetVirtualPosition
        );

    displayedVirtualPosition =
        ClampVirtualPosition(
            displayedVirtualPosition
        );

    displayedVirtualPosition =
        ClampToCircularConstraint(
            displayedVirtualPosition
        );

    ApplyVirtualPositionToRect(
        displayedVirtualPosition
    );
}

public void EndCircularConstraint()
{
    circularConstraintActive =
        false;

    circularConstraintCenter =
        Vector2.zero;

    circularConstraintRadius =
        0.0f;
}

public Vector2 ScreenToVirtualPosition(
    Vector2 screenPosition
)
{
    if (pointerRect == null)
    {
        return GetVirtualCenter();
    }

    Canvas canvas =
        pointerRect.GetComponentInParent<Canvas>();

    if (canvas == null)
    {
        return GetVirtualCenter();
    }

    RectTransform canvasRect =
        canvas.transform as RectTransform;

    if (canvasRect == null)
    {
        return GetVirtualCenter();
    }

    Camera canvasCamera =
        canvas.renderMode
        == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

    if (
        !RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPoint
            )
    )
    {
        return GetVirtualCenter();
    }

    Rect canvasRectangle =
        canvasRect.rect;

    float normalizedX =
        canvasRectangle.width > 0.0f
            ? Mathf.InverseLerp(
                canvasRectangle.xMin,
                canvasRectangle.xMax,
                localPoint.x
            )
            : 0.5f;

    float normalizedY =
        canvasRectangle.height > 0.0f
            ? Mathf.InverseLerp(
                canvasRectangle.yMin,
                canvasRectangle.yMax,
                localPoint.y
            )
            : 0.5f;

    return new Vector2(
        normalizedX * virtualResolution.x,
        normalizedY * virtualResolution.y
    );
}

public float ConvertVirtualRadiusToScreen(
    float virtualRadius
)
{
    if (pointerRect == null)
    {
        return 0.0f;
    }

    Canvas canvas =
        pointerRect.GetComponentInParent<Canvas>();

    if (canvas == null)
    {
        return 0.0f;
    }

    RectTransform canvasRect =
        canvas.transform as RectTransform;

    if (
        canvasRect == null
        ||
        virtualResolution.x <= 0.0f
    )
    {
        return 0.0f;
    }

    float canvasScale =
        canvasRect.rect.width
        / virtualResolution.x;

    return virtualRadius
        * canvasScale
        * canvas.scaleFactor;
}

private Vector2 ClampToCircularConstraint(
    Vector2 position
)
{
    if (!circularConstraintActive)
    {
        return position;
    }

    Vector2 offset =
        position
        - circularConstraintCenter;

    float radius =
        Mathf.Max(
            1.0f,
            circularConstraintRadius
        );

    if (
        offset.sqrMagnitude
        <= radius * radius
    )
    {
        return position;
    }

    return circularConstraintCenter
        + offset.normalized
        * radius;
}

    }
}