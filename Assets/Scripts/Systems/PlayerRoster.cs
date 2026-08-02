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
    }
}