using UnityEngine;

namespace Game.Weapons.Vacuum
{
    public sealed class VacuumTargetVFX :
        MonoBehaviour
    {
        [Header("VFX Root")]
        [Tooltip(
            "敵方向へ向けるVFX全体の支点です。"
            + "通常はこのコンポーネントと同じTransformです。"
        )]
        [SerializeField]
        private Transform directionRoot;

        [Tooltip(
            "距離に応じて伸縮させるTransformです。"
            + "ボーン付きモデルの親を設定してください。"
        )]
        [SerializeField]
        private Transform lengthScaleRoot;

        [Tooltip(
            "モデル固有の向き補正です。"
            + "VFXが敵方向を向かない場合に調整します。"
        )]
        [SerializeField]
        private Vector3 rotationOffsetEuler;

        [Header("Length")]
        [Tooltip(
            "モデルがScale 1のときに想定する"
            + "吸引エフェクトの基準長です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float referenceLength =
            5.0f;

        [Tooltip(
            "エフェクトを伸縮させるローカル軸です。"
            + "一般的にはZ軸を使用します。"
        )]
        [SerializeField]
        private Vector3 lengthScaleAxis =
            Vector3.forward;

        [SerializeField]
        [Min(0.01f)]
        private float minimumLengthScale =
            0.1f;

        [SerializeField]
        [Min(0.01f)]
        private float maximumLengthScale =
            10.0f;

        [Header("Bone Bending")]
        [Tooltip(
            "くねりに使用するボーンを"
            + "掃除機側から敵側の順に設定してください。"
        )]
        [SerializeField]
        private Transform[] bendBones;

        [Tooltip(
            "未設定のときSkinnedMeshRendererから"
            + "ボーンを自動取得します。"
        )]
        [SerializeField]
        private SkinnedMeshRenderer
            sourceSkinnedMeshRenderer;

        [Tooltip(
            "ボーンを曲げるローカル回転軸です。"
        )]
        [SerializeField]
        private Vector3 localBendAxis =
            Vector3.up;

        [Tooltip(
            "通常時のくねりの最大角度です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float baseBendAngle =
            8.0f;

        [Tooltip(
            "ポインター移動によって追加される"
            + "くねりの最大角度です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float pointerBendAngle =
            18.0f;

        [Tooltip(
            "スタック1段階ごとに追加される"
            + "くねり角度です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float bendAnglePerStack =
            2.0f;

        [Tooltip(
            "くねりの速度です。"
        )]
        [SerializeField]
        [Min(0.0f)]
        private float waveSpeed =
            4.0f;

        [Tooltip(
            "ボーンごとの波の位相差です。"
        )]
        [SerializeField]
        private float phaseOffsetPerBone =
            0.7f;

        [Tooltip(
            "入力の急変を滑らかにする速度です。"
        )]
        [SerializeField]
        [Min(0.01f)]
        private float steeringSmoothSpeed =
            8.0f;

        [Header("Tracking")]
        [Tooltip(
            "敵方向への回転追従速度です。"
            + "0以下なら即座に追従します。"
        )]
        [SerializeField]
        private float rotationFollowSpeed =
            20.0f;

        [Tooltip(
            "距離変化への追従速度です。"
            + "0以下なら即座に追従します。"
        )]
        [SerializeField]
        private float lengthFollowSpeed =
            15.0f;

        [Header("Target Position")]
        [Tooltip(
            "敵Collider中心から加算する"
            + "ワールド座標オフセットです。"
        )]
        [SerializeField]
        private Vector3 targetWorldOffset;

        [Header("Runtime Debug")]
        [SerializeField]
        private bool initialized;

        [SerializeField]
        private int currentDamageStack =
            1;

        [SerializeField]
        private float currentDistance;

        [SerializeField]
        private Vector2 currentSteering;

        [SerializeField]
        private Vector3 currentTargetPosition;

        [SerializeField]
        private Transform originPoint;

        [SerializeField]
        private Collider targetCollider;

        private Quaternion[]
            initialBoneLocalRotations;

        private Vector3 initialLengthScale =
            Vector3.one;

        private Quaternion rotationOffset =
            Quaternion.identity;

        private float currentLengthMultiplier =
            1.0f;

        public bool IsValid =>
            initialized
            &&
            originPoint != null
            &&
            targetCollider != null;

        public Collider TargetCollider =>
            targetCollider;

        public void Initialize(
            Transform newOriginPoint,
            Collider newTargetCollider,
            int damageStack
        )
        {
            originPoint =
                newOriginPoint;

            targetCollider =
                newTargetCollider;

            currentDamageStack =
                Mathf.Max(
                    1,
                    damageStack
                );

            if (directionRoot == null)
            {
                directionRoot =
                    transform;
            }

            if (lengthScaleRoot == null)
            {
                lengthScaleRoot =
                    directionRoot;
            }

            rotationOffset =
                Quaternion.Euler(
                    rotationOffsetEuler
                );

            initialLengthScale =
                lengthScaleRoot.localScale;

            ResolveBones();

            CacheBoneRotations();

            currentSteering =
                Vector2.zero;

            UpdateTargetPosition();

            SnapToOrigin();

            UpdateDirection(
                true
            );

            UpdateLength(
                true
            );

            initialized =
                originPoint != null
                &&
                targetCollider != null;
        }

        public void SetDamageStack(
            int damageStack
        )
        {
            currentDamageStack =
                Mathf.Max(
                    1,
                    damageStack
                );
        }

        public void SetSteeringInput(
            Vector2 steeringInput
        )
        {
            Vector2 clampedInput =
                Vector2.ClampMagnitude(
                    steeringInput,
                    1.0f
                );

            float interpolation =
                1.0f
                - Mathf.Exp(
                    -steeringSmoothSpeed
                    * Time.deltaTime
                );

            currentSteering =
                Vector2.Lerp(
                    currentSteering,
                    clampedInput,
                    interpolation
                );
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            if (
                originPoint == null
                ||
                targetCollider == null
            )
            {
                Destroy(gameObject);
                return;
            }

            UpdateTargetPosition();

            SnapToOrigin();

            UpdateDirection(
                false
            );

            UpdateLength(
                false
            );

            UpdateBoneBending();
        }

        private void UpdateTargetPosition()
        {
            if (targetCollider == null)
            {
                return;
            }

            currentTargetPosition =
                targetCollider.bounds.center
                + targetWorldOffset;
        }

        private void SnapToOrigin()
        {
            if (
                originPoint == null
                ||
                directionRoot == null
            )
            {
                return;
            }

            directionRoot.position =
                originPoint.position;
        }

        private void UpdateDirection(
            bool snapImmediately
        )
        {
            if (
                directionRoot == null
                ||
                originPoint == null
                ||
                targetCollider == null
            )
            {
                return;
            }

            Vector3 direction =
                currentTargetPosition
                - originPoint.position;

            if (
                direction.sqrMagnitude
                <= 0.0001f
            )
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                )
                * rotationOffset;

            if (
                snapImmediately
                ||
                rotationFollowSpeed
                <= 0.0f
            )
            {
                directionRoot.rotation =
                    targetRotation;

                return;
            }

            float interpolation =
                1.0f
                - Mathf.Exp(
                    -rotationFollowSpeed
                    * Time.deltaTime
                );

            directionRoot.rotation =
                Quaternion.Slerp(
                    directionRoot.rotation,
                    targetRotation,
                    interpolation
                );
        }

        private void UpdateLength(
            bool snapImmediately
        )
        {
            if (
                originPoint == null
                ||
                targetCollider == null
                ||
                lengthScaleRoot == null
            )
            {
                return;
            }

            currentDistance =
                Vector3.Distance(
                    originPoint.position,
                    currentTargetPosition
                );

            float targetMultiplier =
                currentDistance
                / Mathf.Max(
                    0.01f,
                    referenceLength
                );

            targetMultiplier =
                Mathf.Clamp(
                    targetMultiplier,
                    minimumLengthScale,
                    maximumLengthScale
                );

            if (
                snapImmediately
                ||
                lengthFollowSpeed
                <= 0.0f
            )
            {
                currentLengthMultiplier =
                    targetMultiplier;
            }
            else
            {
                float interpolation =
                    1.0f
                    - Mathf.Exp(
                        -lengthFollowSpeed
                        * Time.deltaTime
                    );

                currentLengthMultiplier =
                    Mathf.Lerp(
                        currentLengthMultiplier,
                        targetMultiplier,
                        interpolation
                    );
            }

            Vector3 normalizedAxis =
                new Vector3(
                    Mathf.Abs(lengthScaleAxis.x),
                    Mathf.Abs(lengthScaleAxis.y),
                    Mathf.Abs(lengthScaleAxis.z)
                );

            if (
                normalizedAxis.sqrMagnitude
                <= 0.0001f
            )
            {
                normalizedAxis =
                    Vector3.forward;
            }

            normalizedAxis.Normalize();

            Vector3 scaleMultiplier =
                Vector3.one
                + normalizedAxis
                * (
                    currentLengthMultiplier
                    - 1.0f
                );

            lengthScaleRoot.localScale =
                Vector3.Scale(
                    initialLengthScale,
                    scaleMultiplier
                );
        }

        private void UpdateBoneBending()
        {
            if (
                bendBones == null
                ||
                initialBoneLocalRotations == null
            )
            {
                return;
            }

            int usableBoneCount =
                Mathf.Min(
                    bendBones.Length,
                    initialBoneLocalRotations.Length
                );

            if (usableBoneCount <= 0)
            {
                return;
            }

            Vector3 bendAxis =
                localBendAxis.sqrMagnitude
                > 0.0001f
                    ? localBendAxis.normalized
                    : Vector3.up;

            float stackAngle =
                bendAnglePerStack
                * Mathf.Max(
                    0,
                    currentDamageStack - 1
                );

            float steeringStrength =
                currentSteering.magnitude;

            float steeringDirection =
                Mathf.Abs(currentSteering.x)
                >= Mathf.Abs(currentSteering.y)
                    ? Mathf.Sign(
                        currentSteering.x
                    )
                    : Mathf.Sign(
                        currentSteering.y
                    );

            if (
                Mathf.Abs(steeringDirection)
                < 0.001f
            )
            {
                steeringDirection =
                    1.0f;
            }

            for (
                int index = 0;
                index < usableBoneCount;
                index++
            )
            {
                Transform bone =
                    bendBones[index];

                if (bone == null)
                {
                    continue;
                }

                float normalizedIndex =
                    usableBoneCount <= 1
                        ? 0.0f
                        : index
                        / (float)(
                            usableBoneCount - 1
                        );

                float middleInfluence =
                    Mathf.Sin(
                        normalizedIndex
                        * Mathf.PI
                    );

                float phase =
                    Time.time
                    * waveSpeed
                    - index
                    * phaseOffsetPerBone;

                float idleWave =
                    Mathf.Sin(
                        phase
                    )
                    * baseBendAngle;

                float steeringWave =
                    Mathf.Sin(
                        phase
                        + normalizedIndex
                        * Mathf.PI
                    )
                    * pointerBendAngle
                    * steeringStrength
                    * steeringDirection;

                float targetAngle =
                    (
                        idleWave
                        + steeringWave
                        + stackAngle
                        * Mathf.Sin(phase)
                    )
                    * middleInfluence;

                Quaternion bendRotation =
                    Quaternion.AngleAxis(
                        targetAngle,
                        bendAxis
                    );

                bone.localRotation =
                    initialBoneLocalRotations[index]
                    * bendRotation;
            }
        }

        private void ResolveBones()
        {
            if (
                bendBones != null
                &&
                bendBones.Length > 0
            )
            {
                return;
            }

            if (
                sourceSkinnedMeshRenderer
                == null
            )
            {
                sourceSkinnedMeshRenderer =
                    GetComponentInChildren<
                        SkinnedMeshRenderer
                    >();
            }

            if (
                sourceSkinnedMeshRenderer
                == null
            )
            {
                bendBones =
                    new Transform[0];

                return;
            }

            bendBones =
                sourceSkinnedMeshRenderer
                    .bones;
        }

        private void CacheBoneRotations()
        {
            if (bendBones == null)
            {
                initialBoneLocalRotations =
                    new Quaternion[0];

                return;
            }

            initialBoneLocalRotations =
                new Quaternion[
                    bendBones.Length
                ];

            for (
                int index = 0;
                index < bendBones.Length;
                index++
            )
            {
                Transform bone =
                    bendBones[index];

                initialBoneLocalRotations[index] =
                    bone != null
                        ? bone.localRotation
                        : Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            RestoreBoneRotations();
        }

        private void RestoreBoneRotations()
        {
            if (
                bendBones == null
                ||
                initialBoneLocalRotations
                    == null
            )
            {
                return;
            }

            int usableBoneCount =
                Mathf.Min(
                    bendBones.Length,
                    initialBoneLocalRotations.Length
                );

            for (
                int index = 0;
                index < usableBoneCount;
                index++
            )
            {
                Transform bone =
                    bendBones[index];

                if (bone == null)
                {
                    continue;
                }

                bone.localRotation =
                    initialBoneLocalRotations[index];
            }
        }

        private void OnValidate()
        {
            referenceLength =
                Mathf.Max(
                    0.01f,
                    referenceLength
                );

            minimumLengthScale =
                Mathf.Max(
                    0.01f,
                    minimumLengthScale
                );

            maximumLengthScale =
                Mathf.Max(
                    minimumLengthScale,
                    maximumLengthScale
                );

            baseBendAngle =
                Mathf.Max(
                    0.0f,
                    baseBendAngle
                );

            pointerBendAngle =
                Mathf.Max(
                    0.0f,
                    pointerBendAngle
                );

            bendAnglePerStack =
                Mathf.Max(
                    0.0f,
                    bendAnglePerStack
                );

            waveSpeed =
                Mathf.Max(
                    0.0f,
                    waveSpeed
                );

            steeringSmoothSpeed =
                Mathf.Max(
                    0.01f,
                    steeringSmoothSpeed
                );

            rotationFollowSpeed =
                Mathf.Max(
                    0.0f,
                    rotationFollowSpeed
                );

            lengthFollowSpeed =
                Mathf.Max(
                    0.0f,
                    lengthFollowSpeed
                );
        }
    }
}