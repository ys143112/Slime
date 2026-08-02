using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-006
    public sealed class LineageEvolutionChecker : MonoBehaviour
    {
        [SerializeField] private LineageEvolutionTable evolutionTable;

        private void OnEnable()
        {
            EventBus.Subscribe(GameEventId.CorruptionTierChanged, OnCorruptionTierChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe(GameEventId.CorruptionTierChanged, OnCorruptionTierChanged);
        }

        private void OnCorruptionTierChanged(GameEvent gameEvent)
        {
            if (evolutionTable == null || PlayerRoster.Instance == null)
            {
                Debug.LogError("LineageEvolutionChecker: 필요한 참조가 연결되지 않아 진화 판정을 건너뜁니다.");
                return;
            }

            int tier = (int)gameEvent.Payload;
            foreach (SlimeInstance instance in PlayerRoster.Instance.Roster)
            {
                if (!instance.corruptedGeneFlag || instance.capturedBiomeId != gameEvent.BiomeId)
                {
                    continue;
                }

                if (evolutionTable.TryGetEvolution(instance.speciesId, tier, out string evolvedSpeciesId))
                {
                    string oldSpeciesId = instance.speciesId;
                    instance.speciesId = evolvedSpeciesId;
                    Debug.Log($"lineage_evolved from={oldSpeciesId} to={evolvedSpeciesId}");
                }
            }
        }
    }
}