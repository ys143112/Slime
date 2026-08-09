using System;
using UnityEngine;

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

        // 이로치는 교배 시점에 정해진다. 알이 이 둘을 안 실어 나르면 부화하는
        // 순간 색이 사라져, 이로치가 나와도 플레이어는 영영 못 본다.
        public bool shinyFlag;
        public Color shinyTint = Color.white;

        public SlimeEgg() { }

        public SlimeEgg(SlimeInstance offspring, string hatchConditionLabel, DateTime hatchAtUtc)
        {
            speciesId = offspring.speciesId;
            offspringStats = offspring.baseStats;
            mutantFlag = offspring.mutantFlag;
            corruptedGeneFlag = offspring.corruptedGeneFlag;
            shinyFlag = offspring.shinyFlag;
            shinyTint = offspring.shinyTint;
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
                shinyFlag = shinyFlag,
                shinyTint = shinyTint,
            };
        }
    }
}
