using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-003 (알/부화 타이머)
    public sealed class EggIncubator : MonoBehaviour
    {
        private const string SaveKey = "egg_incubator";

        public static EggIncubator Instance { get; private set; }

        public event Action<SlimeEgg> EggAdded;
        public event Action<SlimeEgg> EggHatched;

        public IReadOnlyList<SlimeEgg> Eggs => _eggs;

        private readonly List<SlimeEgg> _eggs = new List<SlimeEgg>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _eggs.AddRange(SaveSystem.Load(SaveKey, new List<SlimeEgg>()));
        }

        private void Update()
        {
            if (_eggs.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            for (int i = _eggs.Count - 1; i >= 0; i--)
            {
                SlimeEgg egg = _eggs[i];
                if (!egg.IsReadyToHatch(now))
                {
                    continue;
                }

                _eggs.RemoveAt(i);
                Persist();

                if (PlayerRoster.Instance == null)
                {
                    Debug.LogError("EggIncubator: PlayerRoster 인스턴스가 없어 부화한 슬라임을 등록할 수 없습니다.");
                    continue;
                }

                PlayerRoster.Instance.Add(egg.ToOffspring());
                RunLogWriter.AppendLine($"EggHatched species={egg.speciesId}");
                EggHatched?.Invoke(egg);
            }
        }

        // spec-003: 교배 직후 알을 만든다 - 자손 데이터는 이미 확정된 채로 알에 담기고,
        // 부화 시각이 되면 Update() 가 그대로 PlayerRoster 에 등록한다.
        public SlimeEgg AddEgg(SlimeInstance offspring, float hatchDurationSeconds, string hatchConditionLabel)
        {
            DateTime hatchAt = DateTime.UtcNow.AddSeconds(Math.Max(0f, hatchDurationSeconds));
            var egg = new SlimeEgg(offspring, hatchConditionLabel, hatchAt);
            _eggs.Add(egg);
            Persist();
            EggAdded?.Invoke(egg);
            return egg;
        }

        private void Persist()
        {
            SaveSystem.Save(SaveKey, _eggs);
        }
    }
}
