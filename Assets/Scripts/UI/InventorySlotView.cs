using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-002 (인벤토리 슬롯 한 칸). 번식장·목장이 슬라임을 골라야 할 때도
    // 이 뷰를 그대로 재사용할 수 있도록 Instance 와 클릭 이벤트를 공개해 둔다.
    //
    // 칸을 격자로 좁히면서 글자를 뺐다 — 숫자는 마우스를 올리면 뜨는
    // SlimeTooltipUI 가 맡는다(사용자, 2026-08-09).
    public sealed class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image icon;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            SlimeTooltipUI.Show(Instance);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SlimeTooltipUI.Hide(Instance);
        }

        // 슬롯은 목록이 바뀔 때마다 통째로 다시 만들어진다. 쪽지를 띄운 채로
        // 사라지면 가리키는 대상이 없는 쪽지가 화면에 남는다.
        private void OnDisable()
        {
            SlimeTooltipUI.Hide(Instance);
        }

        public void Bind(SlimeInstance instance)
        {
            Instance = instance;

            // 칸에는 글자를 두지 않는다 — 격자 한 칸(140px)에 그림과 글자를 같이
            // 넣으면 서로 포개져 둘 다 안 읽힌다(사용자, 2026-08-09). 종 이름·
            // 스탯·태그·동행 여부는 전부 SlimeTooltipUI 가 말한다.
            if (icon != null)
            {
                // 종 그림을 띄운다. 예전에는 색만 칠해서 어느 슬라임인지 글자로만
                // 읽어야 했다(사용자, 2026-08-08). 배낭 슬롯과 같은 규칙이다.
                Sprite portrait = SlimeSpeciesCatalog.Portrait(instance);
                icon.sprite = portrait;
                icon.preserveAspect = true;
                icon.enabled = portrait != null;

                // 그림이 붙으면 색은 그림 자체를 물들인다 — 이로치 색이 이미
                // 실려 있어 태그 색까지 겹치면 알아볼 수 없다. 돌연변이·오염
                // 여부는 쪽지가 글자로 말한다.
                icon.color = portrait != null
                    ? (instance.shinyFlag ? instance.shinyTint : Color.white)
                    : instance.corruptedGeneFlag ? new Color(0.6f, 0.4f, 0.9f)
                    : instance.mutantFlag ? new Color(0.95f, 0.55f, 0.2f) : Color.white;
            }
        }
    }
}
