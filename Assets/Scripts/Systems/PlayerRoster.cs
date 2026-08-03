using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-002
    public sealed class PlayerRoster : MonoBehaviour
    {
        private const string SaveKey = "player_roster";

        public static PlayerRoster Instance { get; private set; }

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
            return true;
        }
    }
}