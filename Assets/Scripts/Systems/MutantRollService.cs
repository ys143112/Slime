using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-005
    public static class MutantRollService
    {
        private const float BaseChance = 0.05f;
        private const float ChancePerTier = 0.02f;
        private const int PityThreshold = 50;

        private static int _pityCounter;

        public static bool TryRollMutant(int corruptionTier)
        {
            float chance = BaseChance + ChancePerTier * corruptionTier;
            bool guaranteed = _pityCounter >= PityThreshold;
            bool hit = guaranteed || Random.value < chance;

            if (hit)
            {
                if (guaranteed)
                {
                    Debug.Log("mutant_pity_triggered: 천장 카운터로 돌연변이가 보장되었습니다.");
                }

                _pityCounter = 0;
            }
            else
            {
                _pityCounter++;
            }

            return hit;
        }

        public static void ApplyReversal(SlimeInstance instance)
        {
            SlimeStatBlock stats = instance.baseStats;
            int[] values = { stats.maxHp, stats.attack, stats.defense, stats.speed };
            int minIndex = 0;
            int maxIndex = 0;
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < values[minIndex]) minIndex = i;
                if (values[i] > values[maxIndex]) maxIndex = i;
            }

            (values[minIndex], values[maxIndex]) = (values[maxIndex], values[minIndex]);
            stats.maxHp = values[0];
            stats.attack = values[1];
            stats.defense = values[2];
            stats.speed = values[3];

            instance.mutantFlag = true;
        }
    }
}