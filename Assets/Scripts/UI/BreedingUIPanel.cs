using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (교배 UI 패널)
    public sealed class BreedingUIPanel : MonoBehaviour
    {
        public BreedingPen pen;
        public CanvasGroup canvasGroup;
        public RectTransform gridContent;
        public RosterSlotButton gridSlotTemplate;
        public Text slotAText;
        public Text slotBText;

        // 고른 두 마리를 그림으로 보여준다. 예전에는 종 이름만 글자로 떠 있어
        // 로스터에서 그림으로 고른 것과 교배기에 들어간 것이 눈으로 안 이어졌다
        // (사용자, 2026-08-08).
        public Image slotAIcon;
        public Image slotBIcon;
        public Button breedButton;
        public RectTransform eggListContent;
        public EggSlotView eggSlotTemplate;

        public static BreedingUIPanel Instance { get; private set; }

        private SlimeInstance _slotA;
        private SlimeInstance _slotB;
        private readonly List<RosterSlotButton> _spawnedButtons = new List<RosterSlotButton>();
        private readonly List<EggSlotView> _spawnedEggSlots = new List<EggSlotView>();
        private bool _visible;

        private void OnEnable()
        {
            Instance = this;

            if (breedButton != null)
            {
                breedButton.onClick.AddListener(OnBreedButtonClicked);
            }

            EnsureGridScroll();
            SetVisible(false);
            RefreshSlots();
        }

        // 로스터 격자 한 칸. 인벤토리(140)보다 조금 작게 잡아 일곱 칸이
        // 창 안쪽 폭(1040)에 들어가게 한다: 7×130 + 6×12 = 982.
        private const int GridColumns = 7;
        private static readonly Vector2 GridCell = new Vector2(130f, 130f);
        private static readonly Vector2 GridSpacing = new Vector2(12f, 12f);

        // 두 줄이 보이고 나머지는 스크롤로 넘긴다(130×2 + 12 = 272).
        private const float GridAreaHeight = 300f;
        private const float GridAreaBottom = 20f;

        /// <summary>로스터 격자를 스크롤 뷰포트 안에 넣는다.</summary>
        /// <remarks>
        /// 예전에는 격자가 창에 직접 붙어 있고 <see cref="RectMask2D"/> 로 넘치는
        /// 줄을 잘라냈다 — 잘린 슬라임은 볼 방법이 아예 없었다(사용자, 2026-08-09).
        /// 마스크를 격자가 아니라 <b>뷰포트</b>에 걸고 <see cref="ScrollRect"/> 로
        /// 격자를 밀어 올린다.
        ///
        /// 씬을 안 고치고 코드로 짜는 이유는 <see cref="InventoryUI.EnsureGrid"/>
        /// 와 같다 — Boot 씬은 병합 사고로 UI 를 통째로 잃은 전력이 있다.
        /// 알 목록은 그만큼 위로 물러난다(원래 격자 자리가 180px 였다).
        /// </remarks>
        private void EnsureGridScroll()
        {
            if (gridContent == null || gridContent.parent == null)
            {
                return;
            }

            var panel = (RectTransform)gridContent.parent;
            if (panel.GetComponent<ScrollRect>() != null || gridContent.GetComponentInParent<ScrollRect>() != null)
            {
                ApplyGridLayout();
                return;
            }

            // 창 안쪽이라 창틀(Window)을 또 두르면 테두리가 두 겹이 된다 — 슬롯 틀로 깐다.
            GameObject viewGo = HudRoot.Slot("RosterViewport", panel);
            viewGo.AddComponent<RectMask2D>();
            viewGo.AddComponent<ScrollRect>();
            var view = viewGo.GetComponent<RectTransform>();
            view.anchorMin = new Vector2(0f, 0f);
            view.anchorMax = new Vector2(1f, 0f);
            view.pivot = new Vector2(0.5f, 0f);
            view.anchoredPosition = new Vector2(0f, GridAreaBottom);
            view.sizeDelta = new Vector2(-60f, GridAreaHeight);

            // 판이 있어야 빈 곳을 끌어도 스크롤된다 — HudRoot.Panel 은
            // raycastTarget 을 꺼 두므로 여기서 되켠다.
            viewGo.GetComponent<Image>().raycastTarget = true;

            // 격자에 붙어 있던 마스크는 뗀다 — 뷰포트가 그 일을 대신하고,
            // 남겨두면 스크롤로 올린 줄이 격자 자기 영역 밖이라며 다시 잘린다.
            var oldMask = gridContent.GetComponent<RectMask2D>();
            if (oldMask != null)
            {
                Destroy(oldMask);
            }

            gridContent.SetParent(view, false);
            gridContent.anchorMin = new Vector2(0f, 1f);
            gridContent.anchorMax = new Vector2(1f, 1f);
            gridContent.pivot = new Vector2(0.5f, 1f);
            gridContent.anchoredPosition = Vector2.zero;
            gridContent.sizeDelta = Vector2.zero;

            // 줄이 늘어난 만큼 격자가 길어져야 스크롤할 거리가 생긴다.
            var fitter = gridContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = gridContent.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewGo.GetComponent<ScrollRect>();
            scroll.content = gridContent;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            ApplyGridLayout();
            PushEggListAbove(panel);
        }

        private void ApplyGridLayout()
        {
            var grid = gridContent.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = gridContent.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = GridCell;
            grid.spacing = GridSpacing;
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        // 격자가 180 → 300px 로 커진 만큼 알 목록을 위로 밀어 겹치지 않게 한다.
        // 위쪽 155px 는 부모·교배 버튼 줄이라 건드리지 않는다.
        private void PushEggListAbove(RectTransform panel)
        {
            if (eggListContent == null || eggListContent.parent == null || eggListContent.parent.parent == null)
            {
                return;
            }

            var eggScroll = eggListContent.parent.parent as RectTransform;
            if (eggScroll == null)
            {
                return;
            }

            float bottom = GridAreaBottom + GridAreaHeight + 10f;
            float top = panel.rect.height - 155f;

            eggScroll.anchorMin = new Vector2(0f, 0f);
            eggScroll.anchorMax = new Vector2(1f, 0f);
            eggScroll.pivot = new Vector2(0.5f, 0f);
            eggScroll.anchoredPosition = new Vector2(0f, bottom);
            eggScroll.sizeDelta = new Vector2(-60f, Mathf.Max(80f, top - bottom));
        }

        // spec-003: PlayerRoster.Instance 는 Awake 에서 세팅된다 - 같은 Boot 씬의
        // 영속 오브젝트끼리는 Awake 실행 순서가 보장되지 않으므로, OnEnable 에서
        // 구독하면 PlayerRoster 가 아직 null 일 때 조용히 스킵되고 다시는 구독되지
        // 않을 수 있다. Start 는 씬의 모든 Awake 가 끝난 뒤 실행이 보장된다.
        private void Start()
        {
            if (EggIncubator.Instance != null)
            {
                EggIncubator.Instance.EggAdded += OnEggListChanged;
                EggIncubator.Instance.EggHatched += OnEggListChanged;
            }

            if (PlayerRoster.Instance != null)
            {
                PlayerRoster.Instance.RosterChanged += RefreshGrid;
            }

            RefreshGrid();
            RefreshEggList();
        }

        private void OnDisable()
        {
            if (EggIncubator.Instance != null)
            {
                EggIncubator.Instance.EggAdded -= OnEggListChanged;
                EggIncubator.Instance.EggHatched -= OnEggListChanged;
            }

            if (PlayerRoster.Instance != null)
            {
                PlayerRoster.Instance.RosterChanged -= RefreshGrid;
            }

            if (breedButton != null)
            {
                breedButton.onClick.RemoveListener(OnBreedButtonClicked);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            {
                Toggle();
            }

            DateTime now = DateTime.UtcNow;
            foreach (EggSlotView slot in _spawnedEggSlots)
            {
                if (slot != null)
                {
                    slot.Tick(now);
                }
            }
        }

        /// <summary>U 키와 교배장 클릭이 같이 부르는 자리.</summary>
        /// <remarks>
        /// spec-003: 패널은 씬을 넘나드는 영구 오브젝트라 Hub 밖에서는
        /// <see cref="BreedingPen"/> 이 없다 — 그럴 땐 열리지 않는다.
        /// </remarks>
        public void Toggle()
        {
            if (!_visible && ResolvePen() == null)
            {
                return;
            }

            bool next = !_visible;
            if (next)
            {
                // 교배 UI 를 열 때 인벤토리(I)가 떠 있으면 같이 닫는다 — 두 창이
                // 겹치면 어느 쪽이 입력을 받는지 알 수 없다.
                InventoryUI.Instance?.Close();
                BestiaryPanel.CloseIfOpen();
                SatchelCounterUI.Instance?.Close();
            }

            SetVisible(next);
        }

        // spec-003: U 키로 패널을 켜고 끈다. 이 오브젝트 자체는 계속 활성 상태로
        // 둬야 Update() 가 다음 U 입력과 부화 카운트다운을 계속 감지한다.
        private void SetVisible(bool visible)
        {
            _visible = visible;

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>Esc 가 "지금 떠 있는 창부터 닫는다" 를 판단할 때 읽는다.</summary>
        public bool IsOpen => _visible;

        public void Close()
        {
            SetVisible(false);
        }

        // spec-003: 로스터가 바뀔 때마다(교배 완료 포함) 그리드를 다시 그린다.
        public void RefreshGrid()
        {
            foreach (RosterSlotButton button in _spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _spawnedButtons.Clear();

            if (gridSlotTemplate == null || gridContent == null || PlayerRoster.Instance == null)
            {
                return;
            }

            gridSlotTemplate.gameObject.SetActive(false);

            foreach (SlimeInstance instance in PlayerRoster.Instance.Roster)
            {
                RosterSlotButton spawned = Instantiate(gridSlotTemplate, gridContent);
                spawned.gameObject.SetActive(true);
                spawned.Bind(instance, OnRosterSlotClicked);
                spawned.SetSelected(instance == _slotA || instance == _slotB);
                _spawnedButtons.Add(spawned);
            }
        }

        // spec-003: 클릭은 고르고 되돌리기만 한다 - 실제 교배(소모)는 breedButton
        // 을 눌러야만 일어난다. 예전엔 두 마리가 차는 순간 바로 교배해버려서,
        // 그리드가 다시 그려지며 자리가 밀리는 와중에 연타하면 의도 안 한 조합이
        // 곧바로 소모돼 버렸다.
        private void OnRosterSlotClicked(SlimeInstance instance)
        {
            if (instance == _slotA)
            {
                _slotA = null;
            }
            else if (instance == _slotB)
            {
                _slotB = null;
            }
            else if (_slotA == null)
            {
                _slotA = instance;
            }
            else if (_slotB == null)
            {
                _slotB = instance;
            }

            RefreshSlots();
            RefreshGrid();
        }

        private void RefreshSlots()
        {
            ShowSlot(slotAIcon, slotAText, _slotA);
            ShowSlot(slotBIcon, slotBText, _slotB);

            if (breedButton != null)
            {
                breedButton.interactable = _slotA != null && _slotB != null;
            }
        }

        // 빈 칸일 때만 글자를 쓴다("빈 칸"). 채워지면 그림만 남기고 글자는 지운다
        // — 63px 칸에 둘을 같이 넣으면 글자가 그림을 덮는다.
        private static void ShowSlot(Image icon, Text label, SlimeInstance instance)
        {
            Sprite portrait = instance != null ? SlimeSpeciesCatalog.Portrait(instance) : null;

            if (icon != null)
            {
                icon.sprite = portrait;
                icon.preserveAspect = true;
                icon.color = instance != null && instance.shinyFlag ? instance.shinyTint : Color.white;
                icon.enabled = portrait != null;
            }

            if (label != null)
            {
                // 그림을 못 찾은 종은 이름이라도 보여야 빈 칸과 구분된다.
                label.text = instance == null ? "빈 칸"
                    : portrait != null ? string.Empty
                    : SlimeSpeciesCatalog.DisplayName(instance.speciesId);
            }
        }

        public void OnBreedButtonClicked()
        {
            Breed();
        }

        // spec-003: pen 은 Hub 씬에만 있는 오브젝트다. 이 패널은 DontDestroyOnLoad
        // 로 씬을 넘나들며 살아남으므로, Hub 가 다시 로드될 때마다 새 인스턴스를
        // 다시 찾아야 한다 - 캐시된 참조는 씬이 바뀌면 죽은 참조가 된다.
        private BreedingPen ResolvePen()
        {
            if (pen == null)
            {
                GameObject go = GameObject.Find("BreedingPen");
                pen = go != null ? go.GetComponent<BreedingPen>() : null;
            }

            return pen;
        }

        private void Breed()
        {
            BreedingPen activePen = ResolvePen();
            if (activePen == null || _slotA == null || _slotB == null)
            {
                return;
            }

            if (!activePen.TryPlace(_slotA))
            {
                return;
            }

            if (!activePen.TryPlace(_slotB))
            {
                activePen.WithdrawLast();
                return;
            }

            _slotA = null;
            _slotB = null;
            RefreshSlots();
            RefreshGrid();
        }

        private void OnEggListChanged(SlimeEgg egg)
        {
            RefreshEggList();
        }

        // spec-003: 부화 대기 중인 알을 전부 스크롤 목록으로 보여준다 - 마지막
        // 한 마리만 보이면 여러 마리를 동시에 교배시켰을 때 나머지는 진행 상황을
        // 알 길이 없다.
        private void RefreshEggList()
        {
            foreach (EggSlotView slot in _spawnedEggSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _spawnedEggSlots.Clear();

            if (eggSlotTemplate == null || eggListContent == null || EggIncubator.Instance == null)
            {
                return;
            }

            eggSlotTemplate.gameObject.SetActive(false);

            foreach (SlimeEgg egg in EggIncubator.Instance.Eggs)
            {
                EggSlotView spawned = Instantiate(eggSlotTemplate, eggListContent);
                spawned.gameObject.SetActive(true);
                spawned.Bind(egg);
                _spawnedEggSlots.Add(spawned);
            }
        }
    }
}
