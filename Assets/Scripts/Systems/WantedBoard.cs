using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>현상수배 — 지금 노려야 할 강화 개체 한 마리.</summary>
    /// <remarks>
    /// 다이브에 목표가 없어 "아무 데나 들어가서 아무거나 잡는" 상태였다(팀 QA,
    /// 2026-08-09: 마그마 슬라임 강화판을 현상수배로). 수배 대상은 잿벌에 한
    /// 마리만 뜨고, 잡으면 다음 대상이 걸린다.
    ///
    /// <b>스탯을 여기서 계산하지 않는다.</b> 기본 스탯 공식은
    /// <see cref="WildSlimeAgent.Initialize(string,int)"/> 한 곳에 있어야 하므로,
    /// 보스는 그 결과에 배율을 한 번 더 얹는 <see cref="Promote"/> 로 만든다 —
    /// 공식을 복사하면 밸런스를 고칠 때 한쪽만 바뀐다.
    /// </remarks>
    public static class WantedBoard
    {
        private const string SaveKey = "wanted_cleared";

        /// <summary>수배 대상 종. 지금은 용암 슬라임 하나뿐이다.</summary>
        public const string TargetSpeciesId = "slime_lava";

        /// <summary>원래 서식지. 안내판 문구에만 쓴다.</summary>
        /// <remarks>
        /// 스폰 조건을 이 바이옴으로 묶지 않는다 — <see cref="BiomeDiveTrigger"/>
        /// 의 <c>forceDefaultBiome</c> 이 켜져 있어 지금 다이브는 전부 초원으로
        /// 간다(잿벌·늪지 지형이 아직 덜 다듬어져 내린 결정, 2026-08-08). 잿벌로
        /// 묶으면 수배가 영영 안 뜬다. "다른 바이옴에서 흘러들어온 놈" 으로 두면
        /// 목적지가 열릴 때까지 기다릴 필요가 없다.
        /// </remarks>
        public const string HomeBiomeId = "biome_ashfall";

        /// <summary>보스에게 얹는 오염 티어. 같은 방 다른 개체보다 확실히 세야 한다.</summary>
        public const int BossTierBonus = 2;

        /// <summary>티어 배율 위에 다시 곱하는 배율. 체력은 더, 속도는 덜 올린다.</summary>
        private const float BossHpMultiplier = 3f;
        private const float BossAttackMultiplier = 1.6f;
        private const float BossDefenseMultiplier = 1.5f;

        /// <summary>지금까지 잡은 수배 대상 수. 진행도로 쓴다.</summary>
        public static int ClearedCount
        {
            get => PlayerPrefs.GetInt(SaveKey, 0);
            private set => PlayerPrefs.SetInt(SaveKey, value);
        }

        /// <summary>이번 다이브에 수배 대상을 세울까. 스포너가 묻는다.</summary>
        /// <remarks>다이브 한 번에 한 마리. 지도 하나에 여럿 두면 목표가 아니라 그냥 적이다.</remarks>
        public static bool PendingForBiome(string biomeId)
        {
            return !string.IsNullOrEmpty(biomeId);
        }

        /// <summary>안내판에 적을 한 줄.</summary>
        public static string PosterLine()
        {
            string species = SlimeSpeciesCatalog.DisplayName(TargetSpeciesId);
            return $"수배  {species} (보스)  ·  처치 {ClearedCount}";
        }

        /// <summary>
        /// 이미 스탯이 정해진 야생 개체를 보스로 승격시킨다.
        /// </summary>
        /// <remarks>
        /// <see cref="WildSlimeAgent.Initialize(string,int)"/> 로 티어 배율·종
        /// 편향까지 끝난 <b>뒤에</b> 부른다. 스탯을 바꾼 다음 개체를 다시 심는
        /// 이유는 체력바와 그림이 그 시점에 갱신되기 때문이다.
        /// </remarks>
        public static void Promote(WildSlimeAgent agent)
        {
            if (agent == null || agent.Instance == null)
            {
                return;
            }

            SlimeInstance instance = agent.Instance;
            SlimeStatBlock stats = instance.baseStats;
            stats.maxHp = Mathf.RoundToInt(stats.maxHp * BossHpMultiplier);
            stats.attack = Mathf.RoundToInt(stats.attack * BossAttackMultiplier);
            stats.defense = Mathf.RoundToInt(stats.defense * BossDefenseMultiplier);
            instance.currentHp = stats.maxHp;
            instance.bossFlag = true;

            // 눈으로도 구분돼야 한다 — 같은 그림에 숫자만 다르면 어느 놈이
            // 수배 대상인지 붙어 보기 전에는 모른다.
            agent.transform.localScale *= 1.6f;
            WorldLabel.Attach(agent.transform, "수배", 1.1f);

            agent.Initialize(instance);
            Debug.Log($"wanted_boss_spawned species={instance.speciesId} hp={stats.maxHp} atk={stats.attack}");
        }

        /// <summary>수배 대상을 잡았다. 포획 경로가 부른다.</summary>
        public static void ReportCaptured(SlimeInstance instance)
        {
            if (instance == null || !instance.bossFlag)
            {
                return;
            }

            ClearedCount++;
            PlayerPrefs.Save();
            Debug.Log($"wanted_cleared count={ClearedCount}");
            RunLogWriter.AppendLine($"WantedCleared count={ClearedCount}");
        }
    }
}
