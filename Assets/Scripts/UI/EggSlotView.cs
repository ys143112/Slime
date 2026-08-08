using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (부화 대기 중인 알 목록 한 칸)
    public sealed class EggSlotView : MonoBehaviour
    {
        [SerializeField] private Text label;

        // 무엇이 부화 중인지 그림으로 먼저 보여준다. 글자만으로는 목록이 길어질수록
        // 어느 줄이 무엇인지 읽는 데 시간이 걸린다(사용자, 2026-08-08).
        [SerializeField] private Image icon;

        private SlimeEgg _egg;

        public void Bind(SlimeEgg egg)
        {
            _egg = egg;

            if (icon != null)
            {
                Sprite portrait = SlimeSpeciesCatalog.Portrait(
                    new SlimeInstance { speciesId = egg.speciesId, shinyFlag = egg.shinyFlag });
                icon.sprite = portrait;
                icon.preserveAspect = true;
                icon.color = egg.shinyFlag ? egg.shinyTint : Color.white;
                icon.enabled = portrait != null;
            }

            Tick(DateTime.UtcNow);
        }

        public void Tick(DateTime nowUtc)
        {
            if (_egg == null || label == null)
            {
                return;
            }

            string tag = _egg.mutantFlag ? " [MUTANT]" : _egg.corruptedGeneFlag ? " [CORRUPT]" : "";
            float remaining = _egg.RemainingSeconds(nowUtc);
            label.text = $"{SlimeSpeciesCatalog.DisplayName(_egg.speciesId)}{tag}  {_egg.hatchConditionLabel} ({remaining:0.0}s)";
        }
    }
}
