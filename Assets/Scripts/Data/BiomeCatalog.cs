using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-009
    [Serializable]
    public sealed class BiomeEntry
    {
        public string biomeId;
        public string displayName;
        public string sceneName;
        public string[] speciesPool = Array.Empty<string>();
    }

    // 기능: spec-009
    // 바이옴 목록. 낙인이 가리키는 biomeId 가 실제로 다른 씬·다른 종을 뜻하게
    // 하는 표다 — 이 표가 없으면 어떤 바이옴을 골라도 같은 씬이 열린다.
    [CreateAssetMenu(fileName = "BiomeCatalog", menuName = "SlimeRanch/Biome Catalog")]
    public sealed class BiomeCatalog : ScriptableObject
    {
        [SerializeField] private List<BiomeEntry> entries = new List<BiomeEntry>();

        public IReadOnlyList<BiomeEntry> Entries => entries;

        public BiomeEntry Find(string biomeId)
        {
            if (string.IsNullOrEmpty(biomeId))
            {
                return null;
            }

            foreach (BiomeEntry entry in entries)
            {
                if (entry.biomeId == biomeId)
                {
                    return entry;
                }
            }

            return null;
        }

        public string DisplayNameOf(string biomeId)
        {
            BiomeEntry entry = Find(biomeId);
            return entry != null ? entry.displayName : biomeId;
        }

        /// <summary>이 바이옴에 야생으로 뽑아도 되는 종. 교배 전용 종은 걸러진다.</summary>
        /// <remarks>
        /// 뽑는 자리가 둘이다(<see cref="SlimeSpawner"/> 와
        /// <see cref="WildSlimeAgent"/> 의 손배치 경로). 거르는 일을 호출부에
        /// 두면 새 스폰 경로가 생길 때마다 빠뜨린다 — 표를 읽는 이 자리에서 막는다.
        ///
        /// 지금은 표에 무지개가 없어 결과가 같지만, 표에 실수로 넣는 순간
        /// "교배로만 나오는 종" 이라는 규칙이 조용히 깨진다(팀 QA, 2026-08-08).
        /// </remarks>
        public string[] WildSpeciesPool(string biomeId)
        {
            BiomeEntry entry = Find(biomeId);
            if (entry == null || entry.speciesPool == null)
            {
                return Array.Empty<string>();
            }

            var wild = new List<string>(entry.speciesPool.Length);
            foreach (string speciesId in entry.speciesPool)
            {
                SlimeSpecies species = SlimeSpeciesCatalog.Lookup(speciesId);
                if (species != null && species.breedingOnly)
                {
                    Debug.LogWarning($"BiomeCatalog: {biomeId} 풀의 {speciesId} 는 교배 전용이라 야생 스폰에서 뺍니다.");
                    continue;
                }

                wild.Add(speciesId);
            }

            return wild.ToArray();
        }
    }
}
