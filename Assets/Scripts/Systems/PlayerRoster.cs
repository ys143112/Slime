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

        public void Add(SlimeInstance instance)
        {
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