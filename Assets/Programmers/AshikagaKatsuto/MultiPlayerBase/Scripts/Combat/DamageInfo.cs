using Game.Input.JSL;
using Game.Weapons;
using UnityEngine;

namespace Game.Combat
{
    public readonly struct DamageInfo
    {
        public float Damage
        {
            get;
        }

        public Vector3 HitPosition
        {
            get;
        }

        public Vector3 HitDirection
        {
            get;
        }

        public LocalPlayerController Attacker
        {
            get;
        }

        public WeaponDefinition Weapon
        {
            get;
        }

        public DamageInfo(
            float damage,
            Vector3 hitPosition,
            Vector3 hitDirection,
            LocalPlayerController attacker,
            WeaponDefinition weapon
        )
        {
            Damage =
                Mathf.Max(
                    0.0f,
                    damage
                );

            HitPosition =
                hitPosition;

            HitDirection =
                hitDirection;

            Attacker =
                attacker;

            Weapon =
                weapon;
        }
    }
}