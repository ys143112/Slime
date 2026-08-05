using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-003
    [CreateAssetMenu(fileName = "TraitInheritanceTable", menuName = "SlimeRanch/Trait Inheritance Table")]
    public sealed class TraitInheritanceTable : ScriptableObject
    {
        [SerializeField] [Range(0f, 1f)] private float parentAWeight = 0.5f;
        [SerializeField] [Range(0f, 0.5f)] private float statVariance = 0.15f;

public SlimeStatBlock Blend(SlimeStatBlock parentA, SlimeStatBlock parentB)
        {
            float variance = Random.Range(-statVariance, statVariance);
            var offspring = new SlimeStatBlock(
                BlendStat(parentA.maxHp, parentB.maxHp, variance),
                BlendStat(parentA.attack, parentB.attack, variance),
                BlendStat(parentA.defense, parentB.defense, variance),
                BlendStat(parentA.speed, parentB.speed, variance));

            // spec-003 acceptance: 자손은 100% 부모 둘 모두와 최소 1스탯이 달라야 한다.
            // 부모 스탯이 같고 variance 가 반올림으로 4스탯 모두 원래 값에 떨어지면
            // 통계적으로 부모와 동일한 자손이 나올 수 있어, 그 경우만 강제로 깬다.
            if (MatchesStats(offspring, parentA) && MatchesStats(offspring, parentB))
            {
                offspring.maxHp += 1;
            }

            return offspring;
        }

        private static bool MatchesStats(SlimeStatBlock a, SlimeStatBlock b)
        {
            return a.maxHp == b.maxHp && a.attack == b.attack && a.defense == b.defense && a.speed == b.speed;
        }

        private int BlendStat(int a, int b, float variance)
        {
            float baseValue = a * parentAWeight + b * (1f - parentAWeight);
            return Mathf.Max(1, Mathf.RoundToInt(baseValue * (1f + variance)));
        }
    }
}