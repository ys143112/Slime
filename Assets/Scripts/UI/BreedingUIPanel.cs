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

            SetVisible(false);
            RefreshSlots();
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

        // 빈 칸일 때만 글자를 쓴다("Empty"). 채워지면 그림만 남기고 글자는 지운다
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
                label.text = instance == null ? "Empty"
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
