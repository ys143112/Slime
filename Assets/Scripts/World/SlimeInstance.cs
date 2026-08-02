using System;

namespace Game.Gameplay
{
    // 기능: spec-007
    [Serializable]
    public class SlimeInstance
    {
        public string speciesId;
        public SlimeStatBlock baseStats;
        public int currentHp;
        public bool weakened;
        public bool mutantFlag;
        public bool corruptedGeneFlag;
        public string capturedBiomeId;

        public SlimeInstance() { }

        public SlimeInstance(string speciesId, SlimeStatBlock baseStats)
        {
            this.speciesId = speciesId;
            this.baseStats = baseStats;
            currentHp = baseStats.maxHp;
            weakened = false;
            mutantFlag = false;
            corruptedGeneFlag = false;
            capturedBiomeId = string.Empty;
        }
    }
}