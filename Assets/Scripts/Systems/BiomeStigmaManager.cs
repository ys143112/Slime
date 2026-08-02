using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-004
    public sealed class BiomeStigmaManager : MonoBehaviour
    {
        private const string SaveKey = "biome_stigma";
        private const int StacksPerTier = 3;
        private const int MaxTier = 5;

        [System.Serializable]
        private sealed class StigmaEntry
        {
            public string biomeId;
            public int stacks;
        }

        [System.Serializable]
        private sealed class StigmaSaveData
        {
            public List<StigmaEntry> entries = new List<StigmaEntry>();
        }

        public static BiomeStigmaManager Instance { get; private set; }

        private readonly Dictionary<string, int> _stigmaStacks = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StigmaSaveData saved = SaveSystem.Load(SaveKey, new StigmaSaveData());
            foreach (StigmaEntry entry in saved.entries)
            {
                _stigmaStacks[entry.biomeId] = entry.stacks;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe(GameEventId.RunEnded, OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe(GameEventId.RunEnded, OnRunEnded);
        }

        private void OnRunEnded(GameEvent gameEvent)
        {
            if (!(gameEvent.Payload is RunEndCause cause) || cause != RunEndCause.Death)
            {
                return;
            }

            int previousTier = GetCorruptionTier(gameEvent.BiomeId);
            _stigmaStacks.TryGetValue(gameEvent.BiomeId, out int stacks);
            _stigmaStacks[gameEvent.BiomeId] = stacks + 1;
            Persist();

            int newTier = GetCorruptionTier(gameEvent.BiomeId);
            if (newTier != previousTier)
            {
                Debug.Log($"corruption_tier_changed biome={gameEvent.BiomeId} tier={newTier}");
                EventBus.Publish(new GameEvent(GameEventId.CorruptionTierChanged, gameEvent.BiomeId, newTier));
            }
        }

        private void Persist()
        {
            var data = new StigmaSaveData();
            foreach (KeyValuePair<string, int> pair in _stigmaStacks)
            {
                data.entries.Add(new StigmaEntry { biomeId = pair.Key, stacks = pair.Value });
            }

            SaveSystem.Save(SaveKey, data);
        }

        public int GetCorruptionTier(string biomeId)
        {
            if (string.IsNullOrEmpty(biomeId) || !_stigmaStacks.TryGetValue(biomeId, out int stacks))
            {
                return 0;
            }

            return Mathf.Min(MaxTier, stacks / StacksPerTier);
        }

        public string HighestStigmaBiome(string fallbackBiomeId)
        {
            string best = fallbackBiomeId;
            int bestStacks = -1;
            foreach (KeyValuePair<string, int> entry in _stigmaStacks)
            {
                if (entry.Value > bestStacks)
                {
                    bestStacks = entry.Value;
                    best = entry.Key;
                }
            }

            return best;
        }
    }
}