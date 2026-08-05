using System;

namespace Game.Gameplay
{
    // 기능: spec-003 (알/부화 타이머)
    [Serializable]
    public class SlimeEgg
    {
        public string speciesId;
        public SlimeStatBlock offspringStats;
        public bool mutantFlag;
        public bool corruptedGeneFlag;
        public string hatchConditionLabel;
        public long hatchAtUtcTicks;

        public SlimeEgg() { }

        public SlimeEgg(SlimeInstance offspring, string hatchConditionLabel, DateTime hatchAtUtc)
        {
            speciesId = offspring.speciesId;
            offspringStats = offspring.baseStats;
            mutantFlag = offspring.mutantFlag;
            corruptedGeneFlag = offspring.corruptedGeneFlag;
            this.hatchConditionLabel = hatchConditionLabel;
            hatchAtUtcTicks = hatchAtUtc.Ticks;
        }

        public DateTime HatchAtUtc => new DateTime(hatchAtUtcTicks, DateTimeKind.Utc);

        public bool IsReadyToHatch(DateTime nowUtc) => nowUtc >= HatchAtUtc;

        public float RemainingSeconds(DateTime nowUtc)
        {
            double seconds = (HatchAtUtc - nowUtc).TotalSeconds;
            return seconds > 0 ? (float)seconds : 0f;
        }

        public SlimeInstance ToOffspring()
        {
            return new SlimeInstance(speciesId, offspringStats)
            {
                mutantFlag = mutantFlag,
                corruptedGeneFlag = corruptedGeneFlag,
            };
        }
    }
}
