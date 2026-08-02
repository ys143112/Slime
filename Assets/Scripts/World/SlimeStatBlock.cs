using System;

namespace Game.Gameplay
{
    // 기능: spec-007
    [Serializable]
    public class SlimeStatBlock
    {
        public int maxHp;
        public int attack;
        public int defense;
        public int speed;

        public SlimeStatBlock() { }

        public SlimeStatBlock(int maxHp, int attack, int defense, int speed)
        {
            this.maxHp = maxHp;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
        }
    }
}