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
    }
}
