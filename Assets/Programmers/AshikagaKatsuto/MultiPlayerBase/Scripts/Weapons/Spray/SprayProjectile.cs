using UnityEngine;

namespace Game.Weapons.Spray
{
    public sealed class SprayProjectile :
        MonoBehaviour
    {
        private const int HitBufferSize =
            16;

        [Header("Runtime Debug")]
        [SerializeField]
        private Vector3 moveDirection =
            Vector3.forward;

        [SerializeField]
        private float moveSpeed;

        [SerializeField]
        private float collisionRadius;

        [SerializeField]
        private float remainingLifetime;

        [SerializeField]
        private bool initialized;

        private readonly RaycastHit[] hitBuffer =
            new RaycastHit[HitBufferSize];

        private SprayWeapon owner;

        private LayerMask collisionLayerMask;

        private QueryTriggerInteraction
            queryTriggerInteraction;

        public void Initialize(
            SprayWeapon projectileOwner,
            Vector3 direction,
            float speed,
            float radius,
            float lifetime,
            LayerMask layerMask,
            QueryTriggerInteraction triggerInteraction
        )
        {
            owner =
                projectileOwner;

            moveDirection =
                direction.sqrMagnitude
                > 0.0001f
                    ? direction.normalized
                    : transform.forward;

            moveSpeed =
                Mathf.Max(
                    0.0f,
                    speed
                );

            collisionRadius =
                Mathf.Max(
                    0.01f,
                    radius
                );

            remainingLifetime =
                Mathf.Max(
                    0.01f,
                    lifetime
                );

            collisionLayerMask =
                layerMask;

            queryTriggerInteraction =
                triggerInteraction;

            transform.rotation =
                Quaternion.LookRotation(
                    moveDirection,
                    Vector3.up
                );

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            float deltaTime =
                Time.deltaTime;

            remainingLifetime -=
                deltaTime;

            if (remainingLifetime <= 0.0f)
            {
                Destroy(gameObject);
                return;
            }

            float moveDistance =
                moveSpeed
                * deltaTime;

            if (moveDistance <= 0.0f)
            {
                return;
            }

            if (
                TryFindNearestHit(
                    moveDistance,
                    out RaycastHit nearestHit
                )
            )
            {
                transform.position =
                    nearestHit.point;

                if (owner != null)
                {
                    owner.TryApplyProjectileDamage(
                        nearestHit.collider,
                        nearestHit.point,
                        moveDirection
                    );
                }

                Destroy(gameObject);
                return;
            }

            transform.position +=
                moveDirection
                * moveDistance;
        }

        private bool TryFindNearestHit(
            float distance,
            out RaycastHit nearestHit
        )
        {
            nearestHit =
                default;

            int hitCount =
                Physics.SphereCastNonAlloc(
                    transform.position,
                    collisionRadius,
                    moveDirection,
                    hitBuffer,
                    distance,
                    collisionLayerMask,
                    queryTriggerInteraction
                );

            if (hitCount <= 0)
            {
                return false;
            }

            float nearestDistance =
                float.PositiveInfinity;

            bool found =
                false;

            for (
                int index = 0;
                index < hitCount;
                index++
            )
            {
                RaycastHit hit =
                    hitBuffer[index];

                if (hit.collider == null)
                {
                    continue;
                }

                if (
                    owner != null
                    &&
                    hit.collider.transform
                        .IsChildOf(
                            owner.transform.root
                        )
                )
                {
                    continue;
                }

                if (
                    hit.distance
                    >= nearestDistance
                )
                {
                    continue;
                }

                nearestDistance =
                    hit.distance;

                nearestHit =
                    hit;

                found = true;
            }

            return found;
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor =
                Gizmos.color;

            Gizmos.color =
                Color.green;

            Gizmos.DrawWireSphere(
                transform.position,
                Mathf.Max(
                    0.01f,
                    collisionRadius
                )
            );

            Gizmos.color =
                previousColor;
        }
    }
}