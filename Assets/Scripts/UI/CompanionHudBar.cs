using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>
    /// 화면 왼쪽 아래에 붙는 동행 체력바. 동행은 죽으면 로스터에서 영구히
    /// 사라지므로, 머리 위 막대만으로는 "지금 뺄지 말지" 를 판단하기 어렵다.
    /// </summary>
    public sealed class CompanionHudBar : MonoBehaviour
    {
        private const float BarWidth = 260f;
        private const float BarHeight = 22f;

        private static CompanionHudBar _instance;

        private CompanionAgent _agent;
        private RectTransform _fill;
        private Text _label;
        private GameObject _root;

        public static void Show(CompanionAgent agent)
        {
            if (agent == null)
            {
                return;
            }

            if (_instance == null)
            {
                _instance = HudRoot.Get().gameObject.AddComponent<CompanionHudBar>();
                _instance.Build(HudRoot.Get());
            }

            _instance._agent = agent;
            _instance._root.SetActive(true);
            _instance.Redraw();
        }

        public static void Hide()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._agent = null;
            _instance._root.SetActive(false);
        }

        private void Build(Transform parent)
        {
            _root = HudRoot.Panel("CompanionBar", parent, new Color(0.07f, 0.07f, 0.1f, 0.8f));

            // 쿨다운 바(y=24) 위에 얹는다.
            HudRoot.PinBottomLeft(_root, new Vector2(24f, 76f), new Vector2(BarWidth, BarHeight + 26f));

            GameObject track = HudRoot.Panel("Track", _root.transform, new Color(0.15f, 0.15f, 0.18f, 1f));
            HudRoot.PinBottomLeft(track, Vector2.zero, new Vector2(BarWidth, BarHeight));

            GameObject fill = HudRoot.Panel("Fill", track.transform, new Color(0.45f, 0.85f, 0.35f, 1f));
            _fill = fill.GetComponent<RectTransform>();

            // 비율은 폭이 아니라 오른쪽 앵커로 준다 — 부모 폭이 바뀌어도 따라간다.
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = Vector2.one;
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;

            _label = HudRoot.Label("Label", _root.transform, 18);
            RectTransform labelRect = _label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, BarHeight + 2f);
            labelRect.sizeDelta = new Vector2(0f, 24f);
        }

        private void LateUpdate()
        {
            if (_agent == null || _agent.Instance == null)
            {
                // 동행이 죽거나 거둬지면 스스로 사라진다.
                if (_root != null && _root.activeSelf)
                {
                    Hide();
                }

                return;
            }

            Redraw();
        }

        private void Redraw()
        {
            SlimeInstance instance = _agent.Instance;
            float ratio = Mathf.Clamp01((float)instance.currentHp / Mathf.Max(1, instance.baseStats.maxHp));
            _fill.anchorMax = new Vector2(ratio, 1f);

            if (_label != null)
            {
                _label.text = $"동행 {instance.speciesId}  {instance.currentHp}/{instance.baseStats.maxHp}";
            }
        }
    }
}
