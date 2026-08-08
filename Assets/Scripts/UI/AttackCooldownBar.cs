using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>
    /// 화면 왼쪽 아래 공격 쿨다운 막대. 차오르면 때릴 수 있다.
    /// </summary>
    /// <remarks>
    /// 쿨다운이 생기기 전에는 연타가 그대로 먹혀서 표시가 필요 없었다. 이제는
    /// 눌러도 안 나가는 순간이 있는데, 화면에 아무 표시가 없으면 키가 씹힌
    /// 것인지 쿨다운인지 구분할 수 없다.
    /// </remarks>
    public sealed class AttackCooldownBar : MonoBehaviour
    {
        private const float BarWidth = 260f;
        private const float BarHeight = 14f;

        private static readonly Color Charging = new Color(0.35f, 0.55f, 0.9f, 1f);
        private static readonly Color Ready = new Color(0.95f, 0.85f, 0.35f, 1f);

        private static AttackCooldownBar _instance;

        private PlayerMeleeAttack _attack;
        private RectTransform _fill;
        private UnityEngine.UI.Image _fillImage;
        private Text _label;
        private GameObject _root;

        public static void Show(PlayerMeleeAttack attack)
        {
            if (attack == null)
            {
                return;
            }

            if (_instance == null)
            {
                _instance = HudRoot.Get().gameObject.AddComponent<AttackCooldownBar>();
                _instance.Build(HudRoot.Get());
            }

            _instance._attack = attack;
            _instance._root.SetActive(true);
        }

        private void Build(Transform parent)
        {
            _root = HudRoot.Panel("AttackCooldown", parent, new Color(0.07f, 0.07f, 0.1f, 0.8f));
            HudRoot.PinBottomLeft(_root, new Vector2(24f, 24f), new Vector2(BarWidth, BarHeight + 22f));

            GameObject track = HudRoot.Panel("Track", _root.transform, new Color(0.15f, 0.15f, 0.18f, 1f));
            HudRoot.PinBottomLeft(track, Vector2.zero, new Vector2(BarWidth, BarHeight));

            GameObject fill = HudRoot.Panel("Fill", track.transform, Charging);
            _fill = fill.GetComponent<RectTransform>();
            _fillImage = fill.GetComponent<UnityEngine.UI.Image>();
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = Vector2.one;
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;

            _label = HudRoot.Label("Label", _root.transform, 15);
            RectTransform labelRect = _label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, BarHeight + 2f);
            labelRect.sizeDelta = new Vector2(0f, 20f);
        }

        private void LateUpdate()
        {
            // 씬을 넘나들면 예전 플레이어가 파괴된다. 다시 붙을 때까지 숨긴다.
            if (_attack == null)
            {
                if (_root != null && _root.activeSelf)
                {
                    _root.SetActive(false);
                }

                return;
            }

            if (!_root.activeSelf)
            {
                _root.SetActive(true);
            }

            float ratio = _attack.CooldownRatio;
            _fill.anchorMax = new Vector2(ratio, 1f);
            _fillImage.color = ratio >= 1f ? Ready : Charging;
            _label.text = ratio >= 1f ? "Ready (Space)" : "Charging";
        }
    }
}
