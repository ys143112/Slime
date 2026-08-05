using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (교배 UI 로스터 그리드 항목) - 보유 슬라임 1마리를 원형 아이콘으로 표시한다.
    public sealed class RosterSlotButton : MonoBehaviour
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

            string tag = instance.mutantFlag ? " [돌연변이]" : instance.corruptedGeneFlag ? " [오염]" : "";
            _baseLabel = $"{instance.speciesId}{tag}\nHP {instance.currentHp}/{instance.baseStats.maxHp}";

            if (icon != null)
            {
                icon.color = instance.corruptedGeneFlag ? new Color(0.6f, 0.4f, 0.9f) :
                    instance.mutantFlag ? new Color(0.95f, 0.55f, 0.2f) : Color.white;
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
                label.text = selected ? $"▶ {_baseLabel}" : _baseLabel;
            }

            transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
        }
    }
}
