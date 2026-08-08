using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-002
    public sealed class PlayerRoster : MonoBehaviour
    {
        private const string SaveKey = "player_roster";

        public static PlayerRoster Instance { get; private set; }

        // 로스터가 바뀔 때마다(포획·부화·교배장 배치/회수) 그리는 UI(인벤토리, 교배
        // 패널)가 다시 그리도록 알린다 - 직접 폴링하는 대신 이걸 구독한다.
        public event Action RosterChanged;

        public IReadOnlyList<SlimeInstance> Roster => _roster;

        private readonly List<SlimeInstance> _roster = new List<SlimeInstance>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _roster.AddRange(SaveSystem.Load(SaveKey, new List<SlimeInstance>()));
        }

        // 약화된 개체만 정상화한다. 포획은 HP 0·약화 상태로 들어오므로 어디선가는
        // 되돌려야 하고, 그 자리를 동행 배치 쪽에 두면 슬롯을 누를 때마다 풀피가
        // 되는 무한 회복이 된다(2026-08-08 실측).
        //
        // 다친 개체(약화 아님)는 손대지 않는다. 다치기만 한 슬라임까지 여기서
        // 채우면 교배장에 잠깐 넣었다 빼거나 휴식소에서 바로 회수하는 것만으로
        // 즉시 완치돼, 휴식소의 회복 시간이 아무 의미가 없어진다.
        public void Add(SlimeInstance instance)
        {
            if (instance != null && instance.weakened)
            {
                instance.weakened = false;
                instance.currentHp = Mathf.Max(1, instance.baseStats.maxHp);
            }

            _roster.Add(instance);
            SaveSystem.Save(SaveKey, _roster);
            RosterChanged?.Invoke();
        }

        // spec-008: 교배장에 내놓은 슬라임은 보유 목록에서 빠진다. 같은 개체가
        // 목록과 교배장에 동시에 있으면 한 마리로 두 번 교배할 수 있다.
        public bool Remove(SlimeInstance instance)
        {
            if (!_roster.Remove(instance))
            {
                return false;
            }

            SaveSystem.Save(SaveKey, _roster);
            RosterChanged?.Invoke();
            return true;
        }
    }
}