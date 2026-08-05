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

        public void Bind(SlimeInstance instance, Action<SlimeInstance> onClicked)
        {
            _instance = instance;
            _onClicked = onClicked;

            if (label != null)
            {
                label.text = instance.speciesId;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClicked?.Invoke(_instance));
            }
        }
    }
}
