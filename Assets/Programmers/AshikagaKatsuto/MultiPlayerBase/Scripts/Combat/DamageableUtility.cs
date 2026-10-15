using UnityEngine;

namespace Game.Combat
{
    public static class DamageableUtility
    {
        public static bool TryGetDamageable(
            Collider collider,
            out IDamageable damageable
        )
        {
            damageable = null;

            if (collider == null)
            {
                return false;
            }

            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<
                    MonoBehaviour
                >(
                    includeInactive: false
                );

            foreach (
                MonoBehaviour behaviour
                in behaviours
            )
            {
                if (
                    behaviour
                    is IDamageable foundDamageable
                )
                {
                    damageable =
                        foundDamageable;

                    return true;
                }
            }

            return false;
        }
    }
}