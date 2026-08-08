using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    // 기능: spec-003/spec-005 확장 검증 (종 편향과 그 역연산)
    // 교배는 부모 편향을 벗기고 섞은 뒤 자손 종 편향을 입힌다. 벗기는 쪽이
    // 어긋나면 세대를 거칠수록 스탯이 한 방향으로 밀린다 — 눈으로는 한참 뒤에야
    // 드러나므로 여기서 잡는다.
    public sealed class SpeciesBiasTests
    {
        private static SlimeSpecies MakeSpecies(float hp, float attack, float defense, float speed)
        {
            var species = ScriptableObject.CreateInstance<SlimeSpecies>();
            species.maxHpMultiplier = hp;
            species.attackMultiplier = attack;
            species.defenseMultiplier = defense;
            species.speedMultiplier = speed;
            return species;
        }

        [Test]
        public void Test_RemoveBias_Undoes_ApplyBias()
        {
            SlimeSpecies species = MakeSpecies(1.2f, 1.2f, 1f, 1f);
            var seed = new SlimeStatBlock(20, 5, 2, 3);

            SlimeStatBlock roundTrip = species.RemoveBias(species.ApplyBias(seed));

            Assert.AreEqual(seed.maxHp, roundTrip.maxHp, "편향을 벗겼는데 최대 체력이 원래 값으로 안 돌아왔습니다.");
            Assert.AreEqual(seed.attack, roundTrip.attack, "편향을 벗겼는데 공격이 원래 값으로 안 돌아왔습니다.");

            Object.DestroyImmediate(species);
        }

        [Test]
        public void Test_RemoveBias_Keeps_Zero_Attack_Species()
        {
            // 방어형은 공격 배율이 0 이라 나눌 수 없다. 0 을 그대로 두는 것이
            // 뜻으로도 맞다 — 공격 못 하는 부모는 공격을 물려줄 것이 없다.
            SlimeSpecies guard = MakeSpecies(1.4f, 0f, 2.2f, 0.8f);
            var seed = new SlimeStatBlock(20, 5, 2, 3);

            SlimeStatBlock biased = guard.ApplyBias(seed);
            Assert.AreEqual(0, biased.attack, "공격 배율 0 인 종의 공격이 0 이 아닙니다.");

            SlimeStatBlock roundTrip = guard.RemoveBias(biased);
            Assert.AreEqual(0, roundTrip.attack, "0 을 되돌리려다 값이 튀었습니다 — 0 으로 나누면 안 됩니다.");
            Assert.AreEqual(seed.maxHp, roundTrip.maxHp, "배율이 0 인 칸 때문에 다른 칸까지 어긋났습니다.");

            Object.DestroyImmediate(guard);
        }
    }
}
