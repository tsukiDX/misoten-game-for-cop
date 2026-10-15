namespace Game.Combat
{
    public interface IDamageable
    {
        bool IsAlive
        {
            get;
        }

        bool ApplyDamage(
            DamageInfo damageInfo
        );
    }
}