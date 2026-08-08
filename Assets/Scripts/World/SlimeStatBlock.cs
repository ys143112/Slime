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

        /// <summary>들어온 피해에서 방어를 뺀다. 최소 1 은 항상 들어간다.</summary>
        /// <remarks>
        /// defense 는 필드만 있고 아무도 안 읽어서, 방어 2.2배인 방어형 슬라임이
        /// 그냥 공격 못 하는 슬라임일 뿐이었다. 계산을 여기 한 곳에 두는 이유는
        /// 피해를 받는 쪽이 셋(야생·동행·플레이어)이라 각자 빼면 공식이 갈라지기
        /// 때문이다. 하한 1 이 없으면 방어가 공격을 넘는 순간 무적이 된다.
        /// </remarks>
        public int Mitigate(int amount)
        {
            return amount <= 0 ? 0 : Math.Max(1, amount - defense);
        }
    }
}