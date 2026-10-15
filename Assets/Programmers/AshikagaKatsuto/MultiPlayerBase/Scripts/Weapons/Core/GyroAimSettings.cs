using System;
using Game.Input.JSL;
using UnityEngine;

namespace Game.Weapons
{
    [Serializable]
    public sealed class GyroAimSettings
    {
        [Header("Controller Hold")]
        [SerializeField]
        private ControllerHoldType holdType =
            ControllerHoldType.Horizontal;

        [Header("Gyro")]
        [SerializeField]
        private bool enableGyroAim = true;

        [SerializeField]
        [Range(0.01f, 100.0f)]
        private float gyroSensitivity = 3.0f;

        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float gyroDeadZone = 0.5f;

        [SerializeField]
        private Vector3 horizontalAxis =
            new Vector3(
                0.0f,
                -1.0f,
                0.0f
            );

        [SerializeField]
        private Vector3 verticalAxis =
            new Vector3(
                -1.0f,
                0.0f,
                0.0f
            );

        [Header("Stick")]
        [SerializeField]
        private bool enableStickAim = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float stickDeadZone = 0.2f;

        [SerializeField]
        [Range(10.0f, 9000.0f)]
        private float stickSpeed = 700.0f;

        [SerializeField]
        private bool invertStickHorizontal;

        [SerializeField]
        private bool invertStickVertical;

        [Header("Pointer")]
        [SerializeField]
        [Range(0.0f, 200.0f)]
        private float screenMargin = 10.0f;

        [SerializeField]
        [Range(0.0f, 50.0f)]
        private float smoothing = 12.0f;

        [SerializeField]
        private bool returnToCenter;

        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float returnSpeed = 3.0f;

        [SerializeField]
        private JslButton resetButton =
            JslButton.West;

        public ControllerHoldType HoldType =>
            holdType;

        public bool EnableGyroAim =>
            enableGyroAim;

        public float GyroSensitivity =>
            gyroSensitivity;

        public float GyroDeadZone =>
            gyroDeadZone;

        public Vector3 HorizontalAxis =>
            horizontalAxis;

        public Vector3 VerticalAxis =>
            verticalAxis;

        public bool EnableStickAim =>
            enableStickAim;

        public float StickDeadZone =>
            stickDeadZone;

        public float StickSpeed =>
            stickSpeed;

        public bool InvertStickHorizontal =>
            invertStickHorizontal;

        public bool InvertStickVertical =>
            invertStickVertical;

        public float ScreenMargin =>
            screenMargin;

        public float Smoothing =>
            smoothing;

        public bool ReturnToCenter =>
            returnToCenter;

        public float ReturnSpeed =>
            returnSpeed;

        public JslButton ResetButton =>
            resetButton;
    }
}