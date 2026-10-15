using UnityEngine;

namespace Game.Weapons
{
    public abstract class WeaponDefinition :
        ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string weaponId;

        [SerializeField]
        private string displayName;

        [TextArea(2, 6)]
        [SerializeField]
        private string description;

        [Header("Prefab")]
        [Tooltip(
            "LocalPlayerのWeaponRootへ生成する"
            + "武器Prefabです。"
        )]
        [SerializeField]
        private LocalPlayerWeapon weaponPrefab;

        [Header("Activation")]
        [SerializeField]
        private WeaponActivationSettings
            activationSettings =
                new WeaponActivationSettings();

        [Header("Gyro Aim")]
        [SerializeField]
        private GyroAimSettings gyroAimSettings =
            new GyroAimSettings();

        public string WeaponId =>
            weaponId;

        public string DisplayName =>
            displayName;

        public string Description =>
            description;

        public LocalPlayerWeapon WeaponPrefab =>
            weaponPrefab;

        public WeaponActivationSettings
            ActivationSettings =>
                activationSettings;

        public GyroAimSettings GyroAimSettings =>
            gyroAimSettings;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                weaponId =
                    name
                        .Trim()
                        .ToLowerInvariant()
                        .Replace(" ", "_");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName =
                    name;
            }
        }
    }
}