using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>기능: spec-011 — 왼쪽 위 배낭 개수와 B 로 여는 내용물 창.</summary>
    /// <remarks>
    /// 씬(바이옴 셋)에 배선돼 있던 글자 목록을 버리고 코드로 다시 만든다
    /// (사용자, 2026-08-09: 배낭도 UI 에셋으로). 이유가 두 가지다 — 같은 창이
    /// 씬 셋에 복제돼 있어 고칠 때마다 세 번 고쳐야 했고, 한 줄에 이름·태그·스탯을
    /// 다 적어 320px 판 밖으로 글자가 흘렀다.
    ///
    /// 인벤토리와 같은 규칙: 격자 + 그림, 글자는 <see cref="SlimeTooltipUI"/> 가
    /// hover 로 보여준다. 넘치는 줄은 <see cref="ScrollRect"/> 로 넘긴다.
    /// 씬에 남아 있는 옛 오브젝트는 <see cref="Awake"/> 가 꺼 버린다.
    /// </remarks>
    public sealed class SatchelCounterUI : MonoBehaviour
    {
        // 씬에 남아 있는 옛 배선. 값을 읽지는 않고 끄기만 한다 — 씬 셋을 손대지
        // 않고도 옛 UI 가 화면에 겹쳐 뜨는 것을 막는다.
        [SerializeField] private Text counterText;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private RectTransform detailListContent;
        [SerializeField] private SatchelSlotView detailSlotTemplate;

        // 칸이 뷰포트를 넘지 않게 계산해서 넣는다. 창 760 − 창 여백 28×2 = 704 가
        // 뷰포트 폭이고, 격자 안쪽 여백 16×2 를 빼면 672 가 남는다:
        // 5×120 + 4×12 = 648 ≤ 672. 예전 값(130/12)은 698 이라 다섯째 칸이
        // 오른쪽으로 삐져나왔다(사용자, 2026-08-09).
        private const int Columns = 5;
        private const float GridPadding = 16f;
        private static readonly Vector2 Cell = new Vector2(120f, 120f);
        private static readonly Vector2 Spacing = new Vector2(12f, 12f);
        private static readonly Vector2 WindowSize = new Vector2(760f, 520f);
        private const float WindowInset = 28f;

        public static SatchelCounterUI Instance { get; private set; }

        private GameObject _window;
        private Text _title;
        private Text _counter;
        private RectTransform _grid;
        private readonly List<GameObject> _cells = new List<GameObject>();
        private int _lastRenderedCount = -1;

        public bool IsOpen => _window != null && _window.activeSelf;

        private void Awake()
        {
            Instance = this;

            if (detailPanel != null) detailPanel.SetActive(false);
            if (detailSlotTemplate != null) detailSlotTemplate.gameObject.SetActive(false);

            // 글자만 끈다. **이 컴포넌트가 counterText 와 같은 GameObject 에 붙어
            // 있어서**(씬 배선) 오브젝트를 끄면 자기 Update 까지 멈춘다 — B 키가
            // 통째로 죽는다(2026-08-09 실측).
            if (counterText != null) counterText.enabled = false;

            HideLegacyHud();

            BuildCounter();
            BuildWindow();
            _window.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // 이 컴포넌트는 바이옴 씬과 함께 사라지지만 창은 씬을 넘어가는
            // HudRoot 캔버스 밑에 있다 — 같이 치우지 않으면 목장에 남는다.
            if (_window != null) Destroy(_window);
            if (_counter != null) Destroy(_counter.transform.parent.gameObject);
        }

        /// <summary>씬에 손배치돼 있던 옛 배낭 HUD 를 치운다.</summary>
        /// <remarks>
        /// 연한 사각형 판(<c>HudBackdrop</c>)과 그 위의 작은 아이콘 둘이 새 명패
        /// 뒤에 그대로 남아 있었다(사용자, 2026-08-09: 뒤에 있는 패널 의미 없으면
        /// 지워라). 판은 배낭 글자를 받치던 것이고 글자는 이제 명패가 들고 있다.
        /// 근접 무기 아이콘도 같이 뺀다 — 왼쪽 아래 쿨다운 막대와 조작 안내가
        /// 같은 말을 이미 하고 있다.
        ///
        /// 씬 셋(바이옴)을 각각 고치는 대신 여기서 끈다. 씬을 건드리면 같은
        /// 수정을 세 번 해야 하고 병합 사고 위험도 늘어난다.
        /// </remarks>
        private void HideLegacyHud()
        {
            Transform canvas = counterText != null ? counterText.transform.parent : null;
            if (canvas == null)
            {
                return;
            }

            foreach (string legacy in new[] { "HudBackdrop", "SatchelIcon", "MeleeToolIcon" })
            {
                Transform child = canvas.Find(legacy);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void Close()
        {
            if (_window != null)
            {
                _window.SetActive(false);
            }
        }

        private void Update()
        {
            RefreshCounter();

            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
            {
                Toggle();
            }

            // 열려 있는 동안 마릿수가 바뀌면(포획) 다시 그린다. 매 프레임 다시
            // 그리면 칸이 초당 60번 파괴·생성된다.
            if (IsOpen && RunSatchel.Count != _lastRenderedCount)
            {
                RefreshGrid();
            }
        }

        /// <summary>B 키와 검증 스크립트가 같이 쓰는 자리.</summary>
        public void Toggle()
        {
            bool next = !IsOpen;
            _window.SetActive(next);

            if (!next)
            {
                return;
            }

            // 다른 창과 상호 배타로 연다 — 겹쳐 뜨면 어느 쪽이 입력을 받는지 모른다.
            InventoryUI.Instance?.Close();
            BreedingUIPanel.Instance?.Close();
            BestiaryPanel.CloseIfOpen();

            _lastRenderedCount = -1;
        }

        // 왼쪽 위 작은 명패. 글자 길이에 맞춰 폭이 늘어난다.
        private void BuildCounter()
        {
            GameObject chip = HudRoot.Frame("SatchelCounter", HudRoot.Get());
            var rect = chip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            // 미니맵 바로 아래. 미니맵은 폭 240 에 지도 비율(120×90)이라 높이가
            // 약 191 이다 — 24 + 191 + 12.
            rect.anchoredPosition = new Vector2(24f, -227f);
            rect.sizeDelta = new Vector2(200f, 68f);

            _counter = HudRoot.Label("Text", chip.transform, 22);
            _counter.alignment = TextAnchor.MiddleCenter;

            var textRect = (RectTransform)_counter.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;

            // 창틀 테두리(위 16 / 나머지 10~12)보다 넓게 들여야 글자가 테두리를
            // 밟지 않는다 — ScreenInfoHud 와 같은 규칙이다.
            textRect.offsetMin = new Vector2(22f, 18f);
            textRect.offsetMax = new Vector2(-22f, -26f);
        }

        private void RefreshCounter()
        {
            if (_counter == null)
            {
                return;
            }

            string next = $"배낭 {RunSatchel.Count}마리";
            if (_counter.text == next)
            {
                return;
            }

            _counter.text = next;

            // 판을 글자에 맞춘다. 여백은 위에서 준 값을 도로 더한다.
            var panel = (RectTransform)_counter.transform.parent;
            var textRect = (RectTransform)_counter.transform;
            float padX = textRect.offsetMin.x - textRect.offsetMax.x;
            float padY = textRect.offsetMin.y - textRect.offsetMax.y;
            panel.sizeDelta = new Vector2(_counter.preferredWidth + padX, _counter.preferredHeight + padY);
        }

        private void BuildWindow()
        {
            _window = HudRoot.Frame("SatchelWindow", HudRoot.Get());
            var rect = _window.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = WindowSize;

            _title = HudRoot.Label("Title", _window.transform, 22);
            _title.alignment = TextAnchor.MiddleLeft;
            var titleRect = (RectTransform)_title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(30f, -76f);
            titleRect.offsetMax = new Vector2(-30f, -30f);

            // 뷰포트(잘라내기 + 스크롤) → 격자 순서. 마스크를 격자에 걸면 넘친
            // 줄을 볼 방법이 없어진다(교배창에서 같은 실수를 했다).
            GameObject viewGo = HudRoot.Slot("Viewport", _window.transform);
            viewGo.GetComponent<Image>().raycastTarget = true;
            viewGo.AddComponent<RectMask2D>();

            var view = viewGo.GetComponent<RectTransform>();
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(WindowInset, WindowInset);
            view.offsetMax = new Vector2(-WindowInset, -84f);

            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            _grid = gridGo.GetComponent<RectTransform>();
            _grid.SetParent(view, false);
            _grid.anchorMin = new Vector2(0f, 1f);
            _grid.anchorMax = new Vector2(1f, 1f);
            _grid.pivot = new Vector2(0.5f, 1f);
            _grid.anchoredPosition = Vector2.zero;
            _grid.sizeDelta = Vector2.zero;

            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = Cell;
            grid.spacing = Spacing;
            int pad = Mathf.RoundToInt(GridPadding);
            grid.padding = new RectOffset(pad, pad, pad, pad);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = gridGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewGo.AddComponent<ScrollRect>();
            scroll.content = _grid;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
        }

        private void RefreshGrid()
        {
            foreach (GameObject cell in _cells)
            {
                Destroy(cell);
            }

            _cells.Clear();
            _lastRenderedCount = RunSatchel.Count;
            _title.text = $"런 배낭   {RunSatchel.Count}마리   ·   살아서 추출해야 내 것이 된다";

            foreach (SlimeInstance instance in RunSatchel.Contents)
            {
                _cells.Add(SlimeIconSlot.Create(_grid, instance, 100f).gameObject);
            }
        }
    }
}
