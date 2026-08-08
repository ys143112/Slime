using System;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-005 확장 (이로치). 바이옴 하나에 이로치 색 하나를 못박는 칸.
    // 파란 슬라임처럼 여러 바이옴에 나오는 종이 "늪지면 초록, 용암지면 빨강"
    // 처럼 장소에 따라 다른 이로치를 내도록 한다.
    [Serializable]
    public sealed class BiomeShinyTint
    {
        public string biomeId;
        public Color tint = Color.white;
    }

    // 기능: spec-005 확장 (종별 외형·이로치 규칙)
    // 슬라임 한 종의 그림과 색 규칙을 담는다. 스탯 편향까지 여기 두는 이유는,
    // "방어형은 방어 비율이 높다" 같은 규칙이 종의 성격이지 개체의 우연이
    // 아니기 때문이다 — 개체 무작위(TraitInheritanceTable)는 이 편향 위에 얹힌다.
    [CreateAssetMenu(fileName = "SlimeSpecies", menuName = "SlimeRanch/Slime Species")]
    public sealed class SlimeSpecies : ScriptableObject
    {
        [Header("식별")]
        public string speciesId = "slime_basic";
        public string displayName = "Slime";

        [Header("그림 — PNG 를 여기 끼운다")]
        [Tooltip("평상시 스프라이트.")]
        public Sprite defaultSprite;

        [Tooltip("이로치 전용 그림이 따로 있으면 여기. 비우면 defaultSprite 에 색만 입힌다.")]
        public Sprite shinySprite;

        [Tooltip("종 전용 애니메이터. 있으면 이쪽이 그림을 굴리고 defaultSprite 는 미리보기용으로만 남는다.")]
        public RuntimeAnimatorController animatorController;

        [Header("이로치")]
        [Tooltip("끄면 이 종은 이로치가 나오지 않는다 (무지개 슬라임처럼 단일 개체인 종).")]
        public bool canBeShiny = true;

        [Tooltip("바이옴별로 이로치 색이 정해진 종. 여기 걸리면 아래 shinyTints 보다 우선한다.")]
        public BiomeShinyTint[] biomeShinyTints = Array.Empty<BiomeShinyTint>();

        [Tooltip("바이옴이 이미 고정된 종은 이 목록에서 무작위로 하나 고른다.")]
        public Color[] shinyTints = Array.Empty<Color>();

        [Header("스탯 편향 — 종의 성격. 개체 무작위는 이 위에 얹힌다")]
        [Range(0.1f, 3f)] public float maxHpMultiplier = 1f;
        [Range(0f, 3f)] public float attackMultiplier = 1f;
        [Range(0.1f, 3f)] public float defenseMultiplier = 1f;
        [Range(0.1f, 3f)] public float speedMultiplier = 1f;

        [Header("패시브 — 종의 특수 능력")]
        public SpeciesPassiveKind passive = SpeciesPassiveKind.None;

        [Tooltip("패시브가 닿는 반경(유닛).")]
        [Range(0.5f, 8f)] public float passiveRadius = 3f;

        [Tooltip("패시브가 도는 간격(초).")]
        [Range(0.2f, 5f)] public float passiveInterval = 1.5f;

        [Tooltip("회복량·피해량. 둔화는 이 값을 이동 배율로 쓴다(0.5 면 절반 속도).")]
        public float passiveAmount = 2f;

        // 패시브가 실제로 누군가에게 걸린 순간 한 번. 종마다 다른 소리를 내야
        // 하므로 프리팹이 아니라 종이 들고 있다 — 야생·동행이 같은 프리팹을 쓴다.
        [Tooltip("패시브가 발동할 때 나는 소리. 용암 슬라임의 불길 소리 같은 것.")]
        public AudioClip passiveSfx;

        [Header("등장 규칙")]
        [Tooltip("켜면 야생에 스폰되지 않고 교배로만 나온다.")]
        public bool breedingOnly;

        // 교배 한 번마다 이 확률로 이 종이 나온다. 확률이 종마다 다르므로
        // (방패 25%, 무지개 5%) 후보를 균등 추첨하면 안 된다 — 흔한 쪽과
        // 귀한 쪽이 같은 빈도가 된다.
        [Tooltip("교배 한 번당 이 종이 나올 확률. breedingOnly 종에만 의미가 있다.")]
        [Range(0f, 1f)] public float breedingChance;

        // 바이옴마다 몸 색이 다른 종. 이로치와 달리 **평상시 개체 전부**에
        // 걸린다 — 늪지의 파란 슬라임이 늪지 색이 아니면 어느 바이옴인지
        // 그림으로 안 읽힌다(팀 QA, 2026-08-08).
        [Tooltip("바이옴별 평상시 몸 색. 이로치가 아닌 개체에도 걸린다.")]
        public BiomeShinyTint[] biomeTints = Array.Empty<BiomeShinyTint>();

        /// <summary>이 바이옴에서 이 종이 평상시에 띠는 색. 규칙이 없으면 흰색(=원본 그대로).</summary>
        public Color ResolveBiomeTint(string biomeId)
        {
            foreach (BiomeShinyTint entry in biomeTints)
            {
                if (entry != null && entry.biomeId == biomeId)
                {
                    return entry.tint;
                }
            }

            return Color.white;
        }

        /// <summary>
        /// 이 종이 <b>이 바이옴에서</b> 이로치가 될 수 있는가.
        /// </summary>
        /// <remarks>
        /// 색 규칙이 하나도 안 걸리는 바이옴에서는 이로치가 아니다. 이 검사가
        /// 없으면 파란 슬라임이 초원에서 "흰색 이로치"가 된다 — 플래그는 켜졌는데
        /// 눈으로는 평범한 개체와 구분이 안 되는, 있으나 마나 한 상태다.
        /// (기획: 파란 슬라임은 초원이 아닌 바이옴에서만 이로치가 나온다.)
        /// </remarks>
        public bool CanBeShinyIn(string biomeId)
        {
            if (!canBeShiny)
            {
                return false;
            }

            // 전용 그림이 있으면 색 규칙이 없어도 눈으로 구분된다.
            if (shinySprite != null)
            {
                return true;
            }

            foreach (BiomeShinyTint entry in biomeShinyTints)
            {
                if (entry != null && entry.biomeId == biomeId)
                {
                    return true;
                }
            }

            return shinyTints.Length > 0;
        }

        /// <summary>이 종·이 바이옴에서 쓸 이로치 색. 규칙이 없으면 흰색(=색 변화 없음).</summary>
        public Color ResolveShinyTint(string biomeId)
        {
            if (!canBeShiny)
            {
                return Color.white;
            }

            foreach (BiomeShinyTint entry in biomeShinyTints)
            {
                if (entry != null && entry.biomeId == biomeId)
                {
                    return entry.tint;
                }
            }

            if (shinyTints.Length > 0)
            {
                return shinyTints[UnityEngine.Random.Range(0, shinyTints.Length)];
            }

            return Color.white;
        }

        /// <summary>이 개체가 쓸 그림. 이로치 전용 그림이 있으면 그쪽이 우선한다.</summary>
        public Sprite ResolveSprite(bool shiny)
        {
            return shiny && shinySprite != null ? shinySprite : defaultSprite;
        }

        /// <summary>입힌 편향을 도로 벗긴다. 교배에서 부모 스탯을 견줄 때 쓴다.</summary>
        /// <remarks>
        /// 부모 스탯에는 이미 각자의 종 편향이 발려 있다. 그대로 섞으면 방어형
        /// 부모의 2.2배 방어가 자손 종과 무관하게 그대로 흘러들어간다 — 종을
        /// 물려받아도 성격은 부모 평균인 개체가 나온다.
        ///
        /// 배율이 0 이면 나눌 수 없어 값을 그대로 둔다. 방어형의 공격(배율 0,
        /// 저장값 0)이 그 경우이고, 0 이 그대로 남는 것이 뜻으로도 맞다 —
        /// 공격 못 하는 부모는 공격을 물려줄 것이 없다.
        /// </remarks>
        public SlimeStatBlock RemoveBias(SlimeStatBlock stats)
        {
            return new SlimeStatBlock(
                Unscale(stats.maxHp, maxHpMultiplier),
                Unscale(stats.attack, attackMultiplier),
                Unscale(stats.defense, defenseMultiplier),
                Unscale(stats.speed, speedMultiplier));
        }

        private static int Unscale(int value, float multiplier)
        {
            return multiplier <= 0.01f ? value : Mathf.RoundToInt(value / multiplier);
        }

        /// <summary>종의 성격을 스탯에 입힌다. 최소 1은 보장한다 — 0 이 되면 죽은 스탯이다.</summary>
        public SlimeStatBlock ApplyBias(SlimeStatBlock stats)
        {
            return new SlimeStatBlock(
                Mathf.Max(1, Mathf.RoundToInt(stats.maxHp * maxHpMultiplier)),
                // 공격은 0 을 허용한다 — "공격 불가" 종(방어형)이 성립해야 한다.
                Mathf.Max(0, Mathf.RoundToInt(stats.attack * attackMultiplier)),
                Mathf.Max(1, Mathf.RoundToInt(stats.defense * defenseMultiplier)),
                Mathf.Max(1, Mathf.RoundToInt(stats.speed * speedMultiplier)));
        }
    }
}
