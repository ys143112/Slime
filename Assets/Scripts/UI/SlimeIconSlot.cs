using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>코드로 만드는 슬라임 칸 하나 — 슬롯 틀 + 종 그림 + hover 쪽지.</summary>
    /// <remarks>
    /// 씬에 슬롯 프리팹이 있는 인벤토리(<see cref="InventorySlotView"/>)와 달리,
    /// 배낭은 바이옴 씬마다 따로 배선돼 있어 창을 코드로 만든다. 칸 안에 글자를
    /// 두지 않는 규칙은 인벤토리와 같다 — 이름·스탯은 <see cref="SlimeTooltipUI"/>
    /// 가 hover 로 말한다.
    /// </remarks>
    public sealed class SlimeIconSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SlimeInstance _instance;

        public static SlimeIconSlot Create(Transform parent, SlimeInstance instance, float iconSize)
        {
            GameObject cell = HudRoot.Slot("Slot", parent);

            // HudRoot.Panel 은 raycastTarget 을 꺼 두는데, 그러면 마우스를 올려도
            // 쪽지가 안 뜬다.
            cell.GetComponent<Image>().raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cell.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = SlimeSpeciesCatalog.Portrait(instance);
            icon.enabled = icon.sprite != null;

            // 이로치는 그림에 색이 실려 있으므로 tint 를 곱하지 않는다.
            icon.color = instance != null && instance.shinyFlag ? instance.shinyTint : Color.white;

            var slot = cell.AddComponent<SlimeIconSlot>();
            slot._instance = instance;
            return slot;
        }

        public void OnPointerEnter(PointerEventData eventData) => SlimeTooltipUI.Show(_instance);

        public void OnPointerExit(PointerEventData eventData) => SlimeTooltipUI.Hide(_instance);

        // 목록은 통째로 다시 만들어진다 — 띄운 채로 사라지면 가리키는 대상이
        // 없는 쪽지가 화면에 남는다.
        private void OnDisable() => SlimeTooltipUI.Hide(_instance);
    }
}
