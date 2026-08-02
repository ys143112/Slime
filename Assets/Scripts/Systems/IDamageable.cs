namespace Game.Gameplay
{
    // 기능: spec-007
    public interface IDamageable
    {
        void ApplyDamage(int amount, object source);
    }
}