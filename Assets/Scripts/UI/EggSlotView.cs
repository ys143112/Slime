using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (부화 대기 중인 알 목록 한 칸)
    public sealed class EggSlotView : MonoBehaviour
    {
        [SerializeField] private Text label;

        private SlimeEgg _egg;

        public void Bind(SlimeEgg egg)
        {
            _egg = egg;
            Tick(DateTime.UtcNow);
        }

        public void Tick(DateTime nowUtc)
        {
            if (_egg == null || label == null)
            {
                return;
            }

            string tag = _egg.mutantFlag ? " [돌연변이]" : _egg.corruptedGeneFlag ? " [오염]" : "";
            float remaining = _egg.RemainingSeconds(nowUtc);
            label.text = $"{SlimeSpeciesCatalog.DisplayName(_egg.speciesId)}{tag}\n{_egg.hatchConditionLabel} ({remaining:0.0}초)";
        }
    }
}
