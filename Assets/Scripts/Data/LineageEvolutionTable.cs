using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-006
    [CreateAssetMenu(fileName = "LineageEvolutionTable", menuName = "SlimeRanch/Lineage Evolution Table")]
    public sealed class LineageEvolutionTable : ScriptableObject
    {
        [Serializable]
        public sealed class EvolutionEntry
        {
            public string fromSpeciesId;
            public int corruptionTierThreshold;
            public string toSpeciesId;
        }

        [SerializeField] private List<EvolutionEntry> entries = new List<EvolutionEntry>();

        public bool TryGetEvolution(string speciesId, int corruptionTier, out string evolvedSpeciesId)
        {
            foreach (EvolutionEntry entry in entries)
            {
                if (entry.fromSpeciesId == speciesId && corruptionTier >= entry.corruptionTierThreshold)
                {
                    evolvedSpeciesId = entry.toSpeciesId;
                    return true;
                }
            }

            evolvedSpeciesId = null;
            return false;
        }
    }
}