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
            return new SlimeStatBlock(
                BlendStat(parentA.maxHp, parentB.maxHp, variance),
                BlendStat(parentA.attack, parentB.attack, variance),
                BlendStat(parentA.defense, parentB.defense, variance),
                BlendStat(parentA.speed, parentB.speed, variance));
        }

        private int BlendStat(int a, int b, float variance)
        {
            float baseValue = a * parentAWeight + b * (1f - parentAWeight);
            return Mathf.Max(1, Mathf.RoundToInt(baseValue * (1f + variance)));
        }
    }
}