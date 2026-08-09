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

        [SerializeField] private AudioClip hatchSfx;

        // 저장된 알 중 이미 시각이 지난 것들을 다 처리했나. 그 전까지는 부화
        // 소리를 내지 않는다 — 켠 순간의 몰아치기는 "지금 일어난 일" 이 아니다.
        private bool _caughtUp;

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
                _caughtUp = true;
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

                // 부화는 30초를 기다린 결과라 화면을 안 보고 있을 수 있다.
                // 소리가 없으면 언제 됐는지 알 방법이 교배창을 다시 여는 것뿐이다.
                //
                // 단, **켜자마자 몰아서 부화하는 것**은 조용히 넘긴다. 알은 저장돼
                // 있고 부화 시각은 실제 시계라, 게임을 껐다 켜면 그동안 지난 알이
                // 첫 프레임에 전부 터진다 — 부트 화면에서 아무것도 안 했는데
                // 교배 완료음이 울렸다(사용자 신고, 2026-08-08).
                if (_caughtUp && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfx(hatchSfx);
                }

                EggHatched?.Invoke(egg);
            }

            // 이번 프레임에 처리할 게 없었다면 밀린 알이 다 빠진 것이다.
            // 이제부터 터지는 알은 실제로 기다린 결과라 소리를 낸다.
            _caughtUp = true;
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
