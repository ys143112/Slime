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

            string tag = instance.mutantFlag ? " [돌연변이]" : instance.corruptedGeneFlag ? " [오염]" : "";

            // 지금 나가 있는 동행이 어느 개체인지 목록에서 바로 보여야 한다 —
            // 안 보이면 같은 종이 여럿일 때 누구를 내보냈는지 알 수 없다.
            bool isCompanion = CompanionAgent.Active != null && CompanionAgent.Active.Instance == instance;
            if (label != null)
            {
                label.text = $"{(isCompanion ? "★ " : "")}{instance.speciesId}{tag}\n" +
                    $"HP {instance.currentHp}/{instance.baseStats.maxHp}";
            }

            if (icon != null)
            {
                icon.color = instance.corruptedGeneFlag ? new Color(0.6f, 0.4f, 0.9f) :
                    instance.mutantFlag ? new Color(0.95f, 0.55f, 0.2f) : Color.white;
            }
        }
    }
}
