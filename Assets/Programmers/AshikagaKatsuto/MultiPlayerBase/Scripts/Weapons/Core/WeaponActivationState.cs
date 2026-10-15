namespace Game.Weapons
{
    public readonly struct WeaponActivationState
    {
        public bool IsActive
        {
            get;
        }

        public bool Started
        {
            get;
        }

        public bool Released
        {
            get;
        }

        public float Value
        {
            get;
        }

        public WeaponActivationState(
            bool isActive,
            bool started,
            bool released,
            float value
        )
        {
            IsActive = isActive;
            Started = started;
            Released = released;
            Value = value;
        }

        public static WeaponActivationState Inactive =>
            new WeaponActivationState(
                false,
                false,
                false,
                0.0f
            );

        public static WeaponActivationState AlwaysActive =>
            new WeaponActivationState(
                true,
                false,
                false,
                1.0f
            );
    }
}