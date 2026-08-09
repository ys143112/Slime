using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-005 (이로치는 같은 spec 의 확장)
    public static class MutantRollService
    {
        private const float BaseChance = 0.05f;
        private const float ChancePerTier = 0.02f;
        private const int PityThreshold = 50;

        // 이로치는 돌연변이가 이미 뜬 개체 안에서 다시 굴린다 — "돌연변이 중
        // 색깔이 다른 것"이라는 정의를 그대로 옮긴 것이다. 그래서 이로치는
        // 항상 스탯 반전도 함께 가진다(그 역은 아니다).
        private const float ShinyChanceWithinMutant = 0.25f;

        private static int _pityCounter;

        /// <summary>지금 오염 티어에서 돌연변이가 뜰 확률(0~1).</summary>
        /// <remarks>
        /// 확률이 코드 안에만 있어 플레이어는 오염 티어를 올릴 이유를 알 수 없었다
        /// (팀 QA, 2026-08-09: "스탯·돌연변이 확률이 보이게"). 화면에 띄우려면
        /// 밖에서 읽을 수 있어야 한다 — 상수를 UI 쪽에 다시 적으면 둘이 어긋난다.
        /// </remarks>
        public static float MutantChance(int corruptionTier)
        {
            return Mathf.Clamp01(BaseChance + ChancePerTier * corruptionTier);
        }

        /// <summary>돌연변이가 뜬 개체가 이로치까지 될 확률(0~1).</summary>
        public static float ShinyChance => ShinyChanceWithinMutant;

        /// <summary>천장까지 남은 굴림 수. 0 이면 다음 포획은 무조건 돌연변이다.</summary>
        public static int PityRemaining => Mathf.Max(0, PityThreshold - _pityCounter);

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

        /// <summary>
        /// 돌연변이가 뜬 개체에 이로치 여부를 얹는다. <paramref name="biomeId"/> 는
        /// 색 결정에 쓴다 — 같은 종이라도 늪지면 초록, 용암지면 빨강처럼 장소마다
        /// 다른 이로치가 나오는 종이 있다.
        /// </summary>
        /// <remarks>
        /// 돌연변이가 아닌 개체에 부르면 아무 일도 하지 않는다. 이로치는 돌연변이의
        /// 하위 종류이므로 그 밖에서 켜지면 정의가 무너진다.
        /// </remarks>
        public static bool TryRollShiny(SlimeInstance instance, string biomeId)
        {
            if (instance == null || !instance.mutantFlag)
            {
                return false;
            }

            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(instance.speciesId);

            // 바이옴까지 본다 - 색 규칙이 없는 곳에서 켜면 "보이지 않는 이로치"가
            // 된다(파란 슬라임 @초원).
            if (species == null || !species.CanBeShinyIn(biomeId))
            {
                return false;
            }

            if (Random.value >= ShinyChanceWithinMutant)
            {
                return false;
            }

            instance.shinyFlag = true;
            instance.shinyTint = species.ResolveShinyTint(biomeId);
            Debug.Log($"shiny_rolled species={instance.speciesId} biome={biomeId} tint={instance.shinyTint}");
            RunLogWriter.AppendLine($"ShinyRolled species={instance.speciesId} biome={biomeId}");
            return true;
        }

        /// <summary>
        /// 돌연변이 굴림과 이로치 굴림을 한 번에. 포획·교배 양쪽이 같은 순서를
        /// 밟게 해 "한쪽에서만 이로치가 안 나오는" 어긋남을 막는다.
        /// </summary>
        public static bool RollMutantAndShiny(SlimeInstance instance, int corruptionTier, string biomeId)
        {
            if (!TryRollMutant(corruptionTier))
            {
                return false;
            }

            ApplyReversal(instance);
            TryRollShiny(instance, biomeId);
            return true;
        }
    }
}