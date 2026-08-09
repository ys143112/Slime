namespace Game.Gameplay
{
    // 기능: spec-007
    // 지금까지 "IDamageable 이면 때린다" 였다. 동행 슬라임이 들어오면 그게 곧
    // 아군 오사다 — 진영을 밝힐 자리가 필요하다.
    public enum Faction
    {
        Player,
        Wild,
    }

    // 피해를 주는 쪽도 진영을 밝혀야 걸러낼 수 있다. 공격자는 맞는 쪽이 아닐 수
    // 있어(PlayerMeleeAttack) IDamageable 과 나눠 둔다.
    public interface IFactionMember
    {
        Faction Faction { get; }
    }

    public interface IDamageable : IFactionMember
    {
        void ApplyDamage(int amount, object source);

        /// <summary>체력을 되돌린다. 무지개 슬라임의 범위 회복이 쓴다.</summary>
        /// <remarks>
        /// 피해와 짝이라 같은 인터페이스에 둔다. 회복을 받을 수 있는 것과 피해를
        /// 받을 수 있는 것이 갈린 적이 없다 — 갈릴 일이 생기면 그때 나눈다.
        /// </remarks>
        void Heal(int amount);
    }

    public static class Factions
    {
        // 진영을 안 밝히는 출처(테스트, 환경 피해)는 통과시킨다 — 막으면 피해가
        // 조용히 사라져 원인을 못 찾는다.
        public static bool IsFriendlyFire(object source, IDamageable target)
        {
            return target != null && source is IFactionMember member && member.Faction == target.Faction;
        }
    }
}
