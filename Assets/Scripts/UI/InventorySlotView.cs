using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-002 (인벤토리 슬롯 한 칸). 번식장·목장이 슬라임을 골라야 할 때도
    // 이 뷰를 그대로 재사용할 수 있도록 Instance 와 클릭 이벤트를 공개해 둔다.
    public sealed class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private Button button;

        public SlimeInstance Instance { get; private set; }

        public event Action<SlimeInstance> Clicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke(Instance));
            }
        }

        public void Bind(SlimeInstance instance)
        {
            Instance = instance;

            string tag = instance.mutantFlag ? " [MUTANT]" : instance.corruptedGeneFlag ? " [CORRUPT]" : "";

            // 지금 나가 있는 동행이 어느 개체인지 목록에서 바로 보여야 한다 —
            // 안 보이면 같은 종이 여럿일 때 누구를 내보냈는지 알 수 없다.
            bool isCompanion = CompanionAgent.Active != null && CompanionAgent.Active.Instance == instance;
            if (label != null)
            {
                // 스탯을 안 보여주면 교배 결과를 확인할 방법이 없다 — 종 편향이
                // 걸렸는지, 좋은 개체가 나왔는지가 전부 이 숫자에 있다.
                SlimeStatBlock stats = instance.baseStats;
                label.text = $"{(isCompanion ? "* " : "")}{SlimeSpeciesCatalog.DisplayName(instance.speciesId)}{tag}\n" +
                    $"HP {instance.currentHp}/{stats.maxHp}\n" +
                    $"ATK {stats.attack} DEF {stats.defense} SPD {stats.speed}";
            }

            if (icon != null)
            {
                // 종 그림을 띄운다. 예전에는 색만 칠해서 어느 슬라임인지 글자로만
                // 읽어야 했다(사용자, 2026-08-08). 배낭 슬롯과 같은 규칙이다.
                Sprite portrait = SlimeSpeciesCatalog.Portrait(instance);
                icon.sprite = portrait;
                icon.preserveAspect = true;
                icon.enabled = portrait != null;

                // 그림이 붙으면 색은 그림 자체를 물들인다 — 이로치 색이 이미
                // 실려 있어 태그 색까지 겹치면 알아볼 수 없다. 태그는 라벨의
                // [MUTANT]/[CORRUPT] 가 이미 말하고 있다.
                icon.color = portrait != null
                    ? (instance.shinyFlag ? instance.shinyTint : Color.white)
                    : instance.corruptedGeneFlag ? new Color(0.6f, 0.4f, 0.9f)
                    : instance.mutantFlag ? new Color(0.95f, 0.55f, 0.2f) : Color.white;
            }
        }
    }
}
