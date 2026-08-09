using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (교배 UI 로스터 그리드 항목) - 보유 슬라임 1마리를 원형 아이콘으로 표시한다.
    // 스탯은 마우스를 올리면 뜨는 SlimeTooltipUI 가 보여준다 — 칸에 글자를 넣으면
    // 그림 위에 포개져 둘 다 안 읽힌다(2026-08-08 에 글자를 뺀 이유).
    public sealed class RosterSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private Button button;
        [SerializeField] private Image icon;

        private SlimeInstance _instance;
        private Action<SlimeInstance> _onClicked;
        private string _baseLabel = "";
        private static Sprite _circleSprite;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (icon == null)
            {
                icon = GetComponent<Image>();
            }

            // 별도 아이콘 스프라이트가 없으면 절차적으로 만든 원형 스프라이트로 둥글게 만든다.
            if (icon != null && icon.sprite == null)
            {
                icon.sprite = GetCircleSprite();
                icon.type = Image.Type.Simple;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SlimeTooltipUI.Show(_instance);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SlimeTooltipUI.Hide(_instance);
        }

        // 그리드는 로스터가 바뀔 때마다 다시 그려진다 — 띄운 채로 사라지면
        // 가리키는 대상이 없는 쪽지가 남는다.
        private void OnDisable()
        {
            SlimeTooltipUI.Hide(_instance);
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
            {
                return _circleSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _circleSprite;
        }

        // 인벤토리 슬롯과 같은 정보(종족·돌연변이/오염 태그·HP)를 보여준다 -
        // 교배 상대를 고를 때 인벤토리에서 본 그 슬라임인지 구별해야 한다.
        public void Bind(SlimeInstance instance, Action<SlimeInstance> onClicked)
        {
            _instance = instance;
            _onClicked = onClicked;

            // 글자는 안 쓴다. 64px 남짓한 칸에 종 이름과 HP 를 겹쳐 찍으니 그림
            // 위에 글자가 포개져 둘 다 안 읽혔다(사용자, 2026-08-08). 어느
            // 슬라임인지는 그림이, 고른 것은 확대·표식이 말한다.
            _baseLabel = string.Empty;

            if (icon != null)
            {
                // 종 그림을 띄운다. 예전에는 절차적으로 만든 흰 원뿐이라 어느
                // 슬라임인지 글자로만 읽어야 했다(팀 QA, 2026-08-08: "교배창
                // 가독성"). 그림이 없는 종만 원으로 남는다.
                Sprite portrait = SlimeSpeciesCatalog.Portrait(instance);
                icon.sprite = portrait != null ? portrait : GetCircleSprite();
                icon.preserveAspect = true;

                // 그림이 붙으면 색은 태그 표시가 아니라 그림 자체를 물들인다 —
                // 이로치 색이 이미 그림에 실려 있어 두 번 겹치면 알아볼 수 없다.
                // 태그는 라벨의 [MUTANT]/[CORRUPT] 가 이미 말하고 있다.
                icon.color = portrait != null
                    ? (instance.shinyFlag ? instance.shinyTint : Color.white)
                    : instance.corruptedGeneFlag ? new Color(0.6f, 0.4f, 0.9f)
                    : instance.mutantFlag ? new Color(0.95f, 0.55f, 0.2f) : Color.white;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClicked?.Invoke(_instance));
            }

            SetSelected(false);
        }

        // 교배 확정 전 골라둔 두 마리를 표시한다 - 눈에 안 보이면 연타로
        // 엉뚱한 두 마리가 짝지어져도 알아챌 수 없다. icon.color 는 돌연변이/오염
        // 태그 표시에 이미 쓰고 있어서 선택 표시는 라벨·크기로 따로 낸다.
        public void SetSelected(bool selected)
        {
            if (label != null)
            {
                // Kenney Pixel 에 없는 글자는 빈칸으로 나온다 — 선택 표시는 ASCII 로.
                label.text = selected ? ">" : _baseLabel;
            }

            transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
        }
    }
}
