using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>
    /// 화면 왼쪽 아래에 붙는 동행 체력바. 동행은 죽으면 로스터에서 영구히
    /// 사라지므로, 머리 위 막대만으로는 "지금 뺄지 말지" 를 판단하기 어렵다.
    /// </summary>
    /// <remarks>
    /// <b>씬에 배치하지 않고 코드로 만든다.</b> Boot 씬을 건드리면 병합 사고
    /// 위험이 있고, 그 씬의 <c>PersistentUICanvas</c> 는 아직 800×600 기준이라
    /// 다른 캔버스(1920×1080)와 크기가 어긋난다. 여기서 캔버스를 직접 만들면
    /// 둘 다 피하면서 기준 해상도도 나머지와 맞출 수 있다.
    /// </remarks>
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
                _instance = Create();
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

        private static CompanionHudBar Create()
        {
            var canvasObject = new GameObject("CompanionHud", typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(canvasObject);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 인벤토리·교배 창보다 위에 그린다. 체력은 창이 열려 있어도 보여야 한다.
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = canvasObject.AddComponent<CompanionHudBar>();
            bar.Build(canvasObject.transform);
            return bar;
        }

        private void Build(Transform parent)
        {
            _root = MakePanel("Root", parent, new Color(0.07f, 0.07f, 0.1f, 0.8f));
            RectTransform rootRect = _root.GetComponent<RectTransform>();

            // 왼쪽 아래 구석. 앵커를 그쪽에 붙여 두면 해상도가 바뀌어도 자리가 안 밀린다.
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(24f, 24f);
            rootRect.sizeDelta = new Vector2(BarWidth, BarHeight + 26f);

            GameObject track = MakePanel("Track", _root.transform, new Color(0.15f, 0.15f, 0.18f, 1f));
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.zero;
            trackRect.pivot = Vector2.zero;
            trackRect.anchoredPosition = new Vector2(0f, 0f);
            trackRect.sizeDelta = new Vector2(BarWidth, BarHeight);

            GameObject fill = MakePanel("Fill", track.transform, new Color(0.45f, 0.85f, 0.35f, 1f));
            _fill = fill.GetComponent<RectTransform>();

            // 비율은 폭이 아니라 오른쪽 앵커로 준다 — 부모 폭이 바뀌어도 따라간다.
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = Vector2.one;
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(_root.transform, false);
            _label = labelObject.GetComponent<Text>();

            // 빌트인 폰트가 없으면 글자만 비고 막대는 그대로 뜬다 — 체력 표시가
            // 폰트 하나 때문에 통째로 사라지지 않게 한다.
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 18;
            _label.color = Color.white;
            _label.alignment = TextAnchor.LowerLeft;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, BarHeight + 2f);
            labelRect.sizeDelta = new Vector2(0f, 24f);
        }

        private static GameObject MakePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);

            // 스프라이트를 안 넣은 Image 는 흰 사각형으로 그려진다 — 막대 하나
            // 때문에 자산을 만들 이유가 없다.
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
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
